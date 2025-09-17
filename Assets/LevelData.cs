using System.Collections.Generic;
using UnityEngine;

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
            leftRoom = mainRoom = rightRoom = Vector3.zero;
        }
    }
    public Vector4[] ToArray() => new[] { leftRoom, mainRoom, rightRoom };
}

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData/Create New LevelData")]
public class LevelData : ScriptableObject {
    public string StageName;
    public int difficulty;
    [TextArea] public string description;

    // Inspector-friendly grid (rows = progression steps; cols: left/main/right)
    public List<RoomRow> LevelRoomsInspector = new List<RoomRow>();
    // Optional original format if you still need it anywhere else
    public List<Vector4[]> LevelRooms = new List<Vector4[]>();

    private void OnValidate() {
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

            // First row must always have a main
            /*if (step == 0 && row.mainRoom == Vector4.zero) {
                row.mainRoom = new Vector4(0f, 0f, 0.1f, 0f);
            }*/

            




            if (row.mainRoom.y > 0f) {
                //row.mainRoom.z += (3 - ((int)Mathf.Floor(row.mainRoom.z)) % 10);
                row.mainRoom.z = 3.1f;
                row.leftRoom.z = 3.1f;
                row.rightRoom.z = 3.1f;
            }
            if (row.leftRoom.y > 0f) {
                //row.leftRoom.z += (3 - ((int)Mathf.Floor(row.leftRoom.z)) % 10);
                row.mainRoom.z = 3.1f;
                row.leftRoom.z = 3.1f;
                row.rightRoom.z = 3.1f;
            }
            if (row.rightRoom.y > 0f) {
                //row.rightRoom.z += (3 - ((int)Mathf.Floor(row.rightRoom.z)) % 10);
                row.mainRoom.z = 3.1f;
                row.leftRoom.z = 3.1f;
                row.rightRoom.z = 3.1f;
            }

            if (step == 0) {
                if (row.mainRoom.z <= 0f) {
                    row.mainRoom.z = 0.1f;
                }
                float decimalPart = row.mainRoom.z - Mathf.Floor(row.mainRoom.z);
                if (Mathf.Approximately(decimalPart, 0f)) {
                    row.mainRoom.z = Mathf.Floor(row.mainRoom.z) + 0.1f;
                }
                row.mainRoom.z = Mathf.Clamp(row.mainRoom.z, 0.1f, 3.9f);
            }

            // Prevent fully empty rows in the middle of a level
            int filledCount = CountNonZero(row.ToArray());
            if (filledCount == 0 && step > 0 && step < LevelRoomsInspector.Count - 1) {
                // auto-inject a minimal main to block an illegal empty row
                row.mainRoom = new Vector4(0f, 0f, 0.1f, 0f);
            }

            // Force W=0 if no valid room directly above (z == 0 means room doesn’t exist)
            if (step < LevelRoomsInspector.Count - 1) {
                var above = LevelRoomsInspector[step + 1];
                if (above != null) {
                    if (above.leftRoom.z == 0f) row.leftRoom.w = 0f;
                    if (above.mainRoom.z == 0f) row.mainRoom.w = 0f;
                    if (above.rightRoom.z == 0f) row.rightRoom.w = 0f;
                } else {
                    // Above row itself is null, force all W=0
                    row.leftRoom.w = 0f;
                    row.mainRoom.w = 0f;
                    row.rightRoom.w = 0f;
                }
            } else {
                // Top row has nothing above
                row.leftRoom.w = 0f;
                row.mainRoom.w = 0f;
                row.rightRoom.w = 0f;
            }
        }
    }

    private Vector4 ClampRoom(Vector4 room) {
        room.x = Mathf.Clamp(room.x, -4.3f, 2.7f); // wall offset
        room.y = Mathf.Clamp(room.y, 0f, 100f);    // roof offset
        room.z = Mathf.Clamp(room.z, 0f, 3.9f);    // transition height ratio
        if (room.z > 0f && room.z < 0.2f) room.z = 0.1f;
        room.w = Mathf.Clamp(room.w, 0f, 100000f); // custom (free to define!)
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

    public static float TruncateWValue(float wValue) {
        if (wValue == 0f) return 0f;

        // Split integer and decimal parts
        long intPart = (long)Mathf.Floor(wValue);
        float decimalPart = Mathf.Abs(wValue - intPart);

        string intStr = intPart.ToString();
        int onesDigit = intStr[intStr.Length - 1] - '0'; // ones position
        
        // Determine how many digits can remain to the left of ones place
        if (intStr.Length - 1 > onesDigit) {
            Debug.Log("[W Fix] Trimming integer part from " + (intStr.Length - 1) + " to last " + onesDigit + " digits.");
            intStr = intStr.Substring((intStr.Length - 1) - onesDigit);
        }

        // Trim decimal part to at most "onesDigit" digits
        string decStr = "";
        if (decimalPart > 0f) {
            string decimalFullStr = decimalPart.ToString("0.##########").Split('.')[1];
            decStr = decimalFullStr;
            if (decStr.Length -1 > onesDigit) {
                decStr = decStr.Substring(0, onesDigit);
            }
        }

        // Recombine integer and decimal parts
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
}