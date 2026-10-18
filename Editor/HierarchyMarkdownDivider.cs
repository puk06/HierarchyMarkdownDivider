using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyMarkdownDivider
{
    private static readonly Color[] Accents =
    {
        new Color(0.25f, 0.55f, 1.00f),
        new Color(0.10f, 0.70f, 0.40f),
        new Color(1.00f, 0.62f, 0.15f),
    };

    private static readonly Color[] Backgrounds =
    {
        new Color(0.13f, 0.28f, 0.55f),
        new Color(0.05f, 0.35f, 0.22f),
        new Color(0.40f, 0.24f, 0.05f),
    };

    private static readonly int[] BarWidths = { 4, 3, 3 };

    private const float ToggleSize = 14f;
    private const float ToggleMargin = 4f;
    private const string FoldStatePrefsPrefix = "HierarchyMarkdownDivider_Fold_";

    /// <summary>
    /// 開閉状態を保持する辞書（キーは GlobalObjectId の文字列）。
    /// </summary>
    private static readonly Dictionary<string, bool> FoldStates = new Dictionary<string, bool>();

    static HierarchyMarkdownDivider()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
    }

    private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (!(obj is GameObject go)) return;

        var name = go.name;
        int level = GetDividerLevel(name);
        if (level == 0) return;

        int index = level - 1;
        var accent = Accents[index];
        var bg = Backgrounds[index];
        int barWidth = BarWidths[index];

        EditorGUI.DrawRect(selectionRect, bg);
        EditorGUI.DrawRect(new Rect(selectionRect.x, selectionRect.y, barWidth, selectionRect.height), accent);

        if (level == 1)
            EditorGUI.DrawRect(new Rect(selectionRect.x, selectionRect.yMax - 1f, selectionRect.width, 1f), accent);

        // ラベルの左側に開閉チェックボックスを描画
        bool expanded = DrawLeftToggle(go, level, selectionRect, barWidth);

        int baseSize = Mathf.Clamp(Mathf.RoundToInt(selectionRect.height) - 5, 9, 13);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            fontSize = level == 1 ? baseSize + 2 : level == 2 ? baseSize + 1 : baseSize,
        };

        float textX = selectionRect.x + barWidth + ToggleSize + ToggleMargin + (level - 1) * 7f;
        float textWidth = Mathf.Max(0f, selectionRect.xMax - textX);
        var textRect = new Rect(textX, selectionRect.y, textWidth, selectionRect.height);

        var normalColor = GUI.contentColor;
        GUI.contentColor = level == 3 ? new Color(0.92f, 0.92f, 0.92f) : Color.white;
        GUI.Label(textRect, name.Substring(level + 1), style);
        GUI.contentColor = normalColor;
    }

    /// <summary>
    /// オブジェクト名から Divider レベルを取得する。
    /// </summary>
    private static int GetDividerLevel(string name)
    {
        if (name.StartsWith("### ")) return 3;
        if (name.StartsWith("## ")) return 2;
        if (name.StartsWith("# ")) return 1;
        return 0;
    }

    /// <summary>
    /// Divider のラベル左側に開閉チェックボックスを描画する。
    /// </summary>
    private static bool DrawLeftToggle(GameObject divider, int level, Rect selectionRect, int barWidth)
    {
        string key = GetFoldStateKey(divider);
        bool expanded = GetFoldState(key);

        var toggleRect = new Rect(
            selectionRect.x + barWidth + 2f,
            selectionRect.y + (selectionRect.height - ToggleSize) * 0.5f,
            ToggleSize,
            ToggleSize
        );

        var prevColor = GUI.color;
        GUI.color = Color.white;

        bool newExpanded = GUI.Toggle(toggleRect, expanded, GUIContent.none, EditorStyles.toggle);

        GUI.color = prevColor;

        if (newExpanded != expanded)
        {
            SetFoldState(key, newExpanded);
            ApplySectionVisibility(divider, level, newExpanded);
            EditorApplication.RepaintHierarchyWindow();
            return newExpanded;
        }

        return expanded;
    }

    /// <summary>
    /// Divider から次の Divider（同じか上位のレベル）までの同じ階層のオブジェクトを表示/非表示にする。
    /// </summary>
    private static void ApplySectionVisibility(GameObject divider, int level, bool expanded)
    {
        var siblings = CollectSectionSiblings(divider, level);
        if (siblings.Count == 0) return;

        Undo.RecordObjects(siblings.ToArray(), expanded ? "Expand Markdown Divider" : "Collapse Markdown Divider");

        foreach (var go in siblings)
        {
            if (go == null) continue;

            if (expanded)
                go.hideFlags &= ~HideFlags.HideInHierarchy;
            else
                go.hideFlags |= HideFlags.HideInHierarchy;
        }
    }

    /// <summary>
    /// Divider の直後から、同じ親・同じ深さで次の Divider（同じか上位レベル）が現れるまでの
    /// ゲームオブジェクトを収集する。
    /// </summary>
    private static List<GameObject> CollectSectionSiblings(GameObject divider, int level)
    {
        var result = new List<GameObject>();
        Transform parent = divider.transform.parent;
        int startIndex = divider.transform.GetSiblingIndex() + 1;

        if (parent != null)
        {
            for (int i = startIndex; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                int childLevel = GetDividerLevel(child.name);

                // 同じか上位（レベル番号が小さい）の Divider が来たらここで区切り終了
                if (childLevel > 0 && childLevel <= level)
                    break;

                result.Add(child.gameObject);
            }
        }
        else
        {
            var scene = divider.scene;
            if (scene.IsValid())
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = startIndex; i < roots.Length; i++)
                {
                    GameObject root = roots[i];
                    int rootLevel = GetDividerLevel(root.name);

                    if (rootLevel > 0 && rootLevel <= level)
                        break;

                    result.Add(root);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Divider 用の永続キーを取得する。
    /// </summary>
    private static string GetFoldStateKey(GameObject go)
    {
        var globalId = GlobalObjectId.GetGlobalObjectIdSlow(go);
        return FoldStatePrefsPrefix + globalId.ToString();
    }

    private static bool GetFoldState(string key)
    {
        if (!FoldStates.TryGetValue(key, out bool value))
        {
            value = EditorPrefs.GetBool(key, true); // デフォルトは展開状態
            FoldStates[key] = value;
        }
        return value;
    }

    private static void SetFoldState(string key, bool expanded)
    {
        FoldStates[key] = expanded;
        EditorPrefs.SetBool(key, expanded);
    }
}
