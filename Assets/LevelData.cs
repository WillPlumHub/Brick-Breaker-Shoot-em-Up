using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class RoomRow {
    public Vector4 leftRoom;    // Column 0  (x = wall offset, y = roof offset, z = transition height ratio 0–1)
    public Vector4 mainRoom;    // Column 1
    public Vector4 rightRoom;   // Column 2

    public RoomRow(Vector4[] array = null) {
        if (array != null && array.Length >= 3) {
            leftRoom = array[0];
            mainRoom = array[1];
            rightRoom = array[2];
        } else {
            leftRoom = mainRoom = rightRoom = Vector4.zero;
        }
    }

    public Vector4[] ToArray() => new[] { leftRoom, mainRoom, rightRoom };
}

[System.Serializable]
public class GameObjectRow {
    public List<GameObject> gameObjects = new List<GameObject>();

    public GameObjectRow(int columnCount = 0) {
        Resize(columnCount);
    }

    public int Count => gameObjects.Count;

    public GameObject this[int i] {
        get => (i >= 0 && i < gameObjects.Count) ? gameObjects[i] : null;
        set {
            if (i >= 0 && i < gameObjects.Count)
                gameObjects[i] = value;
        }
    }

    public void Resize(int newColumnCount) {
        newColumnCount = Mathf.Max(0, newColumnCount);
        while (gameObjects.Count < newColumnCount)
            gameObjects.Add(null);
        while (gameObjects.Count > newColumnCount)
            gameObjects.RemoveAt(gameObjects.Count - 1);
    }
}

[System.Serializable]
public class GameObjectGrid {
    [SerializeField] private List<GameObjectRow> rows = new List<GameObjectRow>();

    public int RowCount => rows.Count;
    public int ColumnCount => rows.Count > 0 ? rows[0].Count : 0;

    public GameObject this[int row, int col] {
        get {
            if (row < 0 || row >= rows.Count) return null;
            if (col < 0 || col >= rows[row].Count) return null;
            return rows[row][col];
        }
        set {
            if (row < 0 || row >= rows.Count) return;
            if (col < 0 || col >= rows[row].Count) return;
            rows[row][col] = value;
        }
    }

    public void Resize(int newRowCount, int newColCount) {
        newRowCount = Mathf.Max(0, newRowCount);
        newColCount = Mathf.Max(0, newColCount);

        while (rows.Count < newRowCount)
            rows.Add(new GameObjectRow(newColCount));
        while (rows.Count > newRowCount)
            rows.RemoveAt(rows.Count - 1);

        foreach (var row in rows)
            row.Resize(newColCount);
    }

    public void Clear() => rows.Clear();
}

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData/Create New LevelData")]
public class LevelData : ScriptableObject {

    [Header("Level Info")]
    public string StageName;
    public int difficulty;
    public int HighScore;
    [TextArea] public string description;
    public Vector2 MapCoordinates;

    [Header("Visuals")]
    public Tilemap TileMap;
    public GameObject BackgroundObject;
    public Sprite Background;

    [Header("Level Layout")]
    public List<RoomRow> LevelRoomsInspector = new List<RoomRow>();
    public List<Vector4[]> LevelRooms = new List<Vector4[]>();

    [Header("GameObject Grid")]
    public GameObjectGrid gameObjectGrid = new GameObjectGrid();

    [Header("Grid Settings")]
    public int gridRows = 1;
    public int gridColumns = 1;
    public Vector3 gridStartPosition = new Vector3(-33.6f, -4.501f, 0f);
    public Vector2 cellSize = new Vector2(0.7f, 0.36f);
    public Vector3 levelWorldOffset = new Vector3(-33.6f, -4.501f, 0f);

    [Header("Leaderboard")]
    public List<string> LeaderboardNames;
    public List<string> LeaderboardScores;

    private void OnValidate() {
        difficulty = Mathf.Clamp(difficulty, 0, 5);
        SyncGameObjectGrid();
        Validate();
        SyncLevelRooms();
    }

    public void Validate() {
        for (int step = 0; step < LevelRoomsInspector.Count; step++) {
            var row = LevelRoomsInspector[step] ?? new RoomRow();
            LevelRoomsInspector[step] = row;
            // --- Clamp values for each room ---
            row.leftRoom = ClampRoom(row.leftRoom);
            row.mainRoom = ClampRoom(row.mainRoom);
            row.rightRoom = ClampRoom(row.rightRoom);

            // Truncate W values according to ones-digit rule
            row.leftRoom.w = TruncateWValue(row.leftRoom.w);
            row.mainRoom.w = TruncateWValue(row.mainRoom.w);
            row.rightRoom.w = TruncateWValue(row.rightRoom.w);

            /*if (row.mainRoom.y > 0f) {
                row.mainRoom.z = 3.1f;
                row.leftRoom.z = 3.1f;
                row.rightRoom.z = 3.1f;
            }
            if (row.leftRoom.y > 0f) {
                row.mainRoom.z = 3.1f;
                row.leftRoom.z = 3.1f;
                row.rightRoom.z = 3.1f;
            }
            if (row.rightRoom.y > 0f) {
                row.mainRoom.z = 3.1f;
                row.leftRoom.z = 3.1f;
                row.rightRoom.z = 3.1f;
            }*/

            if (step == 0) {
                if (row.mainRoom.z <= 0f) {
                    row.mainRoom.z = 0.1f;
                }
                float decimalPart = row.mainRoom.z - Mathf.Floor(row.mainRoom.z);
                if (Mathf.Approximately(decimalPart, 0f)) {
                    row.mainRoom.z = Mathf.Floor(row.mainRoom.z) + 0.1f;
                }
                row.mainRoom.z = Mathf.Clamp(row.mainRoom.z, 0.1f, 103.9f);
            }

            // Prevent fully empty rows in the middle of a level
            int filledCount = CountNonZero(row.ToArray());
            if (filledCount == 0 && step > 0 && step < LevelRoomsInspector.Count - 1) {
                row.mainRoom = new Vector4(0f, 0f, 0.1f, 0f);
            }

            // Force W=0 if no valid room directly above (z == 0 means room doesn't exist)
            if (step < LevelRoomsInspector.Count - 1) {
                var above = LevelRoomsInspector[step + 1];
                if (above != null) {
                    if (above.leftRoom.z == 0f) row.leftRoom.w = 0f;
                    if (above.mainRoom.z == 0f) row.mainRoom.w = 0f;
                    if (above.rightRoom.z == 0f) row.rightRoom.w = 0f;
                } else {
                    row.leftRoom.w = 0f;
                    row.mainRoom.w = 0f;
                    row.rightRoom.w = 0f;
                }
            } else {
                row.leftRoom.w = 0f;
                row.mainRoom.w = 0f;
                row.rightRoom.w = 0f;
            }
        }
    }

    private Vector4 ClampRoom(Vector4 room) {
        room.x = Mathf.Clamp(room.x, -4.3f, 2.7f); // wall offset
        room.y = Mathf.Clamp(room.y, 0f, 100f);    // roof offset
        room.z = Mathf.Clamp(room.z, 0f, 103.9f);    // transition height ratio
        if (room.z > 0f && room.z < 0.2f) room.z = 0.1f;
        room.w = Mathf.Clamp(room.w, 0f, 100000f);
        return room;
    }

    private int CountNonZero(Vector4[] row) {
        int count = 0;
        foreach (var r in row) {
            if (r != Vector4.zero) count++;
        }
        return count;
    }

    private void SyncLevelRooms() {
        LevelRooms.Clear();
        foreach (var row in LevelRoomsInspector) {
            LevelRooms.Add(row.ToArray());
        }
    }

    public void InitializeFromArrays() {
        LevelRoomsInspector.Clear();
        foreach (var arr in LevelRooms) {
            LevelRoomsInspector.Add(new RoomRow(arr));
        }
        SyncLevelRooms();
    }

    public List<Vector4[]> GetLevelRoomsAsArrays() {
        SyncLevelRooms();
        return LevelRooms;
    }

    // NEW METHOD: Check if a grid position overlaps with room walls
    public bool IsGridPositionOverlappingWall(int gridRow, int gridCol) {
        if (LevelRoomsInspector.Count == 0) return false;

        // Convert grid position to world position
        Vector3 worldPos = GetWorldPosition(gridRow, gridCol);

        // Each room row in LevelRoomsInspector represents a vertical progression step
        // Each Vector4 in the room represents: x=wall offset, y=roof offset, z=transition height, w=custom

        for (int roomStep = 0; roomStep < LevelRoomsInspector.Count; roomStep++) {
            var roomRow = LevelRoomsInspector[roomStep];
            if (roomRow == null) continue;

            // Check left room (column 0)
            if (roomRow.leftRoom.z > 0) { // z > 0 means room exists
                float roomBottom = roomStep * 3f; // Assuming each room step is 3 units high
                float roomTop = roomBottom + roomRow.leftRoom.z * 3f; // z is transition height ratio
                float roomLeft = roomRow.leftRoom.x; // x is wall offset
                float roomRight = roomLeft + 2.7f; // Assuming room width

                if (worldPos.y >= roomBottom && worldPos.y <= roomTop &&
                    worldPos.x >= roomLeft && worldPos.x <= roomRight) {
                    return true;
                }
            }

            // Check main room (column 1)
            if (roomRow.mainRoom.z > 0) {
                float roomBottom = roomStep * 3f;
                float roomTop = roomBottom + roomRow.mainRoom.z * 3f;
                float roomLeft = roomRow.mainRoom.x;
                float roomRight = roomLeft + 2.7f;

                if (worldPos.y >= roomBottom && worldPos.y <= roomTop &&
                    worldPos.x >= roomLeft && worldPos.x <= roomRight) {
                    return true;
                }
            }

            // Check right room (column 2)
            if (roomRow.rightRoom.z > 0) {
                float roomBottom = roomStep * 3f;
                float roomTop = roomBottom + roomRow.rightRoom.z * 3f;
                float roomLeft = roomRow.rightRoom.x;
                float roomRight = roomLeft + 2.7f;

                if (worldPos.y >= roomBottom && worldPos.y <= roomTop &&
                    worldPos.x >= roomLeft && worldPos.x <= roomRight) {
                    return true;
                }
            }
        }

        return false;
    }

    // NEW METHOD: Get all wall-overlapping grid positions
    public List<Vector2Int> GetWallOverlappingGridPositions() {
        List<Vector2Int> overlappingPositions = new List<Vector2Int>();

        for (int row = 0; row < gridRows; row++) {
            for (int col = 0; col < gridColumns; col++) {
                if (IsGridPositionOverlappingWall(row, col)) {
                    overlappingPositions.Add(new Vector2Int(row, col));
                }
            }
        }

        return overlappingPositions;
    }

    public static float TruncateWValue(float wValue) {
        if (wValue == 0f) return 0f;
        long intPart = (long)Mathf.Floor(wValue);
        float decimalPart = Mathf.Abs(wValue - intPart);
        string intStr = intPart.ToString();
        int onesDigit = intStr[intStr.Length - 1] - '0';

        if (intStr.Length - 1 > onesDigit) {
            Debug.Log("[W Fix] Trimming integer part from " + (intStr.Length - 1) + " to last " + onesDigit + " digits.");
            intStr = intStr.Substring((intStr.Length - 1) - onesDigit);
        }

        string decStr = "";
        if (decimalPart > 0f) {
            string decimalFullStr = decimalPart.ToString("0.##########").Split('.')[1];
            decStr = decimalFullStr;
            if (decStr.Length - 1 > onesDigit) {
                decStr = decStr.Substring(0, onesDigit);
            }
        }

        string combined = intStr;
        if (!string.IsNullOrEmpty(decStr)) {
            combined += "." + decStr;
        }

        if (float.TryParse(combined, out float result)) {
            return result;
        } else {
            return wValue;
        }
    }

    private void SyncGameObjectGrid() {
        gridRows = Mathf.Max(1, gridRows);
        gridColumns = Mathf.Max(1, gridColumns);
        gameObjectGrid.Resize(gridRows, gridColumns);
    }

    public GameObject GetGameObjectAt(int row, int col) {
        return gameObjectGrid[row, col];
    }

    public void SetGameObjectAt(int row, int col, GameObject go) {
        gameObjectGrid[row, col] = go;
    }

    public void ResizeGameObjectGrid(int newRows, int newColumns) {
        gridRows = Mathf.Max(1, newRows);
        gridColumns = Mathf.Max(1, newColumns);
        SyncGameObjectGrid();
    }

    public void ClearGameObjectGrid() {
        gameObjectGrid.Clear();
        gridRows = 1;
        gridColumns = 1;
    }

    public Vector3 GetWorldPosition(int row, int col) {
        return new Vector3(
            gridStartPosition.x + (col * cellSize.x),
            gridStartPosition.y + (row * cellSize.y),
            gridStartPosition.z
        );
    }
    
    public void InstantiateGridObjects(Transform levelParent = null) {
        GameObject levelContainer = null;
        if (levelParent == null) {
            levelContainer = new GameObject($"{StageName}_LevelObjects");
            levelContainer.transform.position = levelWorldOffset;
        } else {
            levelContainer = levelParent.gameObject;
        }

        for (int row = 0; row < gameObjectGrid.RowCount; row++) {
            for (int col = 0; col < gameObjectGrid.ColumnCount; col++) {
                GameObject prefab = GetGameObjectAt(row, col);
                if (prefab == null) continue;

                Vector3 localPosition = GetWorldPosition(row, col);
                GameObject instance = Instantiate(prefab, localPosition, Quaternion.identity, levelContainer.transform);
            }
        }
    }
}