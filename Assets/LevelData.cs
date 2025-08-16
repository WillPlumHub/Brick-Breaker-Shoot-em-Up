using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData/Create New LevelData")]
public class LevelData : ScriptableObject {
    public string StageName;
    public int difficulty;
    public string description;
    public List<Vector4> LevelRooms;

    private void OnValidate() {
        Validate(); // Remember to call boardStats.Validate(); whenever changing LevelRooms at runtime
    }

    public void Validate() {
        int consecutiveSideRooms = 0;

        for (int i = 0; i < LevelRooms.Count; i++) {
            Vector4 room = LevelRooms[i];

            room.x = Mathf.Clamp(room.x, -4.3f, 4.3f);
            room.z = Mathf.Clamp(room.z, -1f, 1f);

            if (room.z != 0f) {
                consecutiveSideRooms++;

                if (consecutiveSideRooms > 2) {
                    Debug.LogWarning($"Room {i}: More than 2 consecutive horizontal (z != 0) rooms — forcing z = 0.");
                    room.z = 0f;
                    consecutiveSideRooms = 0; // reset after forcing vertical
                }
            } else {
                consecutiveSideRooms = 0; // reset on vertical
            }
            LevelRooms[i] = room;
        }

        // Ensure no consecutive z = 1 or z = -1 without a z = 0 in between
        for (int i = 0; i < LevelRooms.Count - 1; i++) {
            Vector4 currentRoom = LevelRooms[i];
            Vector4 nextRoom = LevelRooms[i + 1];

            if (currentRoom.z == 1f && nextRoom.z == 1f) { // Can't have two z = 1 rooms in a row
                Debug.LogWarning($"Rooms {i} and {i + 1}: Consecutive z = 1 rooms. Forcing a z = 0 in between.");
                nextRoom.z = 0f;
                LevelRooms[i + 1] = nextRoom;
            } else if (currentRoom.z == -1f && nextRoom.z == -1f) { // Can't have two z = -1 rooms in a row
                Debug.LogWarning($"Rooms {i} and {i + 1}: Consecutive z = -1 rooms. Forcing a z = 0 in between.");
                nextRoom.z = 0f;
                LevelRooms[i + 1] = nextRoom;
            } else if (currentRoom.z == -1f && nextRoom.z == 1f) { // z = 1 must come before z = -1
                Debug.LogWarning($"Rooms {i} and {i + 1}: z = -1 should not come before z = 1. Swapping.");
                LevelRooms[i] = nextRoom;
                LevelRooms[i + 1] = currentRoom;
            }
        }
    }
}