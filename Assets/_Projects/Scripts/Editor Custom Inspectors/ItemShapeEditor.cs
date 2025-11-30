using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(InventoryShape))]
public class ItemShapeEditor : Editor
{
    private InventoryShape shape;
    private const int CellSize = 20;

    void OnEnable()
    {
        shape = (InventoryShape)target;
        shape.EnsureSize();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw width/height with change detection (and enforce >=1)
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Width", shape.width);
        int newHeight = EditorGUILayout.IntField("Height", shape.height);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(shape, "Change shape size");
            shape.width = Mathf.Max(1, newWidth);
            shape.height = Mathf.Max(1, newHeight);
            shape.EnsureSize();
            EditorUtility.SetDirty(shape);
        }

        GUILayout.Space(8);

        // Draw grid of toggles
        int w = Mathf.Max(1, shape.width);
        int h = Mathf.Max(1, shape.height);

        GUILayout.Label("Mask (true = occupied):");
        for (int y = 0; y < h; y++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            for (int x = 0; x < w; x++)
            {
                bool current = shape.Get(x, y) == 1;
                bool changed = GUILayout.Toggle(current, GUIContent.none, GUILayout.Width(CellSize), GUILayout.Height(CellSize));
                if (changed != current)
                {
                    Undo.RecordObject(shape, "Toggle cell");
                    shape.Set(x, y, changed);
                    EditorUtility.SetDirty(shape);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(8);

        // Quick tools
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear")) { Undo.RecordObject(shape, "Clear mask"); shape.Clear(); EditorUtility.SetDirty(shape); }
        if (GUILayout.Button("Fill")) { Undo.RecordObject(shape, "Fill mask"); shape.Fill(); EditorUtility.SetDirty(shape); }
        if (GUILayout.Button("Invert")) { Undo.RecordObject(shape, "Invert mask"); shape.Invert(); EditorUtility.SetDirty(shape); }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif