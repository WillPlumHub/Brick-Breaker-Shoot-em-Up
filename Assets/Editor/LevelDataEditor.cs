#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor {
    private Vector2 gridScroll = Vector2.zero;

    // For control-click range selection
    private Vector2Int? firstCtrlClick = null;
    private Vector2Int? secondCtrlClick = null;
    private HashSet<Vector2Int> highlightedCells = new HashSet<Vector2Int>();

    public override void OnInspectorGUI() {
        serializedObject.Update();
        LevelData data = (LevelData)target;

        // Draw all fields except the grid
        DrawPropertiesExcluding(serializedObject, "gameObjectGrid");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("GameObject Grid", EditorStyles.boldLabel);

        data.gridRows = EditorGUILayout.IntField("Rows", data.gridRows);
        data.gridColumns = EditorGUILayout.IntField("Columns", data.gridColumns);

        EditorGUILayout.Space();

        // Scrollable grid view
        gridScroll = EditorGUILayout.BeginScrollView(
            gridScroll,
            true,
            false,
            GUILayout.Height(70 * Mathf.Min(data.gridRows, 5))
        );

        for (int r = 0; r < data.gridRows; r++) {
            EditorGUILayout.BeginHorizontal();

            for (int c = 0; c < data.gridColumns; c++) {
                DrawGridCell(data, r, c);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGridCell(LevelData data, int row, int col) {
        float cellSize = 70f;
        Rect rect = GUILayoutUtility.GetRect(cellSize, 18f, GUILayout.Width(cellSize));

        Event e = Event.current;
        bool isControl = e.control || e.command;
        bool mouseDown = e.type == EventType.MouseDown && e.button == 0;

        Vector2Int cell = new Vector2Int(row, col);

        // --- Handle clicking logic ---
        if (rect.Contains(e.mousePosition) && mouseDown) {
            if (isControl) {
                if (!firstCtrlClick.HasValue) {
                    firstCtrlClick = cell;
                } else if (!secondCtrlClick.HasValue) {
                    secondCtrlClick = cell;
                    highlightedCells.Clear();

                    int minR = Mathf.Min(firstCtrlClick.Value.x, secondCtrlClick.Value.x);
                    int maxR = Mathf.Max(firstCtrlClick.Value.x, secondCtrlClick.Value.x);
                    int minC = Mathf.Min(firstCtrlClick.Value.y, secondCtrlClick.Value.y);
                    int maxC = Mathf.Max(firstCtrlClick.Value.y, secondCtrlClick.Value.y);

                    for (int r = minR; r <= maxR; r++)
                        for (int c = minC; c <= maxC; c++)
                            highlightedCells.Add(new Vector2Int(r, c));
                } else {
                    // Reset selection
                    firstCtrlClick = cell;
                    secondCtrlClick = null;
                    highlightedCells.Clear();
                }

                e.Use();
            }
        }

        // --- Determine colors ---
        bool isSelected = highlightedCells.Contains(cell);
        bool isFirst = firstCtrlClick.HasValue && firstCtrlClick.Value == cell;
        bool isSecond = secondCtrlClick.HasValue && secondCtrlClick.Value == cell;
        
        Color fillColor = Color.clear;
        Color borderColor = Color.clear;
                
        if (isSelected) {
            fillColor = new Color(0.2f, 0.4f, 1f, 0.25f); // blue fill for selected area
            borderColor = new Color(0.2f, 0.4f, 1f, 1f);  // blue border
        }

        if (isFirst) {
            fillColor = new Color(1f, 0.5f, 0f, 0.35f); // orange
            borderColor = new Color(1f, 0.5f, 0f, 1f);
        }

        if (isSecond) {
            fillColor = new Color(0f, 1f, 0f, 0.35f); // green
            borderColor = new Color(0f, 1f, 0f, 1f);
        }

        // --- Draw fill ---
        if (fillColor != Color.clear)
            EditorGUI.DrawRect(rect, fillColor);

        // --- Object field ---
        GameObject current = data.GetGameObjectAt(row, col);
        GameObject newObj = (GameObject)EditorGUI.ObjectField(rect, current, typeof(GameObject), false);

        if (newObj != current) {
            Undo.RecordObject(data, "Set Grid Object");
            data.SetGameObjectAt(row, col, newObj);
            EditorUtility.SetDirty(data);

            // If this is part of a highlighted region, copy it to others
            if (highlightedCells.Count > 0) {
                foreach (var hc in highlightedCells) {
                    if (hc != cell)
                        data.SetGameObjectAt(hc.x, hc.y, newObj);
                }
                EditorUtility.SetDirty(data);
            }
        }

        // --- Draw border (always on top) ---
        if (borderColor != Color.clear) {
            Handles.color = borderColor;
            float t = 2f;
            Vector3 tl = new Vector3(rect.xMin, rect.yMin);
            Vector3 tr = new Vector3(rect.xMax, rect.yMin);
            Vector3 br = new Vector3(rect.xMax, rect.yMax);
            Vector3 bl = new Vector3(rect.xMin, rect.yMax);
            Handles.DrawAAPolyLine(t, tl, tr, br, bl, tl);
        }
    }
}
#endif
