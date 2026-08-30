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

    private static readonly int[] BarWidths = { 4, 3, 2 };

    static HierarchyMarkdownDivider()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
    }

    private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj == null) return;

        var name = obj.name;
        int level = 0;

        if (name.StartsWith("### "))
            level = 3;
        else if (name.StartsWith("## "))
            level = 2;
        else if (name.StartsWith("# "))
            level = 1;

        if (level == 0) return;

        int index = level - 1;
        var accent = Accents[index];
        var bg = Backgrounds[index];
        int barWidth = BarWidths[index];

        EditorGUI.DrawRect(selectionRect, bg);

        EditorGUI.DrawRect(new Rect(selectionRect.x, selectionRect.y, barWidth, selectionRect.height), accent);

        if (level == 1)
            EditorGUI.DrawRect(new Rect(selectionRect.x, selectionRect.yMax - 1f, selectionRect.width, 1f), accent);

        int baseSize = Mathf.Clamp(Mathf.RoundToInt(selectionRect.height) - 5, 9, 13);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = level == 3 ? FontStyle.Normal : FontStyle.Bold,
            fontSize = level == 1 ? baseSize + 2 : level == 2 ? baseSize + 1 : baseSize,
        };

        float textX = selectionRect.x + barWidth + 4f + (level - 1) * 7f;
        var textRect = new Rect(textX, selectionRect.y, selectionRect.width - (textX - selectionRect.x), selectionRect.height);

        var normalColor = GUI.contentColor;
        GUI.contentColor = level == 3 ? new Color(0.92f, 0.92f, 0.92f) : Color.white;
        GUI.Label(textRect, name.Substring(level + 1), style);
        GUI.contentColor = normalColor;
    }
}
