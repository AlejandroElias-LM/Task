using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AdvancedInventoryShape))]
public class AdvancedInventoryShapeEditor : Editor
{
    private AdvancedInventoryShape matrix;
    private const int CellSize = 22;

    void OnEnable()
    {
        matrix = (AdvancedInventoryShape)target;
        matrix.EnsureSize();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw size + max value
        EditorGUI.BeginChangeCheck();
        int newWidth = EditorGUILayout.IntField("Width", matrix.width);
        int newHeight = EditorGUILayout.IntField("Height", matrix.height);
        int newMax = EditorGUILayout.IntField("Max Value", matrix.maxValue);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(matrix, "Change Matrix Settings");
            matrix.width = Mathf.Max(1, newWidth);
            matrix.height = Mathf.Max(1, newHeight);
            matrix.maxValue = Mathf.Max(1, newMax);
            matrix.EnsureSize();
            EditorUtility.SetDirty(matrix);
        }

        GUILayout.Space(8);

        // --- Buttons for bulk operations ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Matrix"))
        {
            BulkModifyAllCells((x, y, cur) => 0, "Clear Matrix");
        }

        if (GUILayout.Button("Add 1 to All"))
        {
            BulkModifyAllCells((x, y, cur) => Mathf.Min(matrix.maxValue, cur + 1), "Add 1 to All");
        }

        if (GUILayout.Button("Remove 1 from All"))
        {
            BulkModifyAllCells((x, y, cur) => Mathf.Max(0, cur - 1), "Remove 1 from All");
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);

        int w = matrix.width;
        int h = matrix.height;

        GUILayout.Label("Matrix:");

        Event e = Event.current;
        bool anyChange = false; // track if we changed anything so we can repaint/save

        for (int y = 0; y < h; y++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            for (int x = 0; x < w; x++)
            {
                Rect rect = GUILayoutUtility.GetRect(CellSize, CellSize);

                int currentValue = matrix.Get(x, y);

                // Detect clicks BEFORE drawing the button so the event isn't consumed by GUI.Button
                if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
                {
                    // Left click -> increment
                    if (e.button == 0)
                    {
                        Undo.RecordObject(matrix, "Increment Cell Value");
                        matrix.Set(x, y, Mathf.Min(matrix.maxValue, currentValue + 1));
                        EditorUtility.SetDirty(matrix);
                        anyChange = true;
                        e.Use();
                    }
                    // Right click -> decrement
                    else if (e.button == 1)
                    {
                        Undo.RecordObject(matrix, "Decrement Cell Value");
                        matrix.Set(x, y, Mathf.Max(0, currentValue - 1));
                        EditorUtility.SetDirty(matrix);
                        anyChange = true;
                        e.Use();
                    }
                }

                // Draw button with number (visual only)
                GUIStyle style = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter };
                GUI.Button(rect, currentValue.ToString(), style);
            }

            EditorGUILayout.EndHorizontal();
        }

        if (anyChange)
        {
            Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Helper to modify every cell with a provided func: (x, y, currentValue) => newValue
    private void BulkModifyAllCells(System.Func<int, int, int, int> modifier, string undoMessage)
    {
        if (matrix == null) return;

        Undo.RecordObject(matrix, undoMessage);

        int w = matrix.width;
        int h = matrix.height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int cur = matrix.Get(x, y);
                int next = modifier(x, y, cur);
                // safety clamp
                next = Mathf.Clamp(next, 0, matrix.maxValue);
                matrix.Set(x, y, next);
            }
        }

        EditorUtility.SetDirty(matrix);
        Repaint();
    }
}
