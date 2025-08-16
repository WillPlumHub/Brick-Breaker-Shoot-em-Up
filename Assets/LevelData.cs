using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData/Create New LevelData")]
public class LevelData : ScriptableObject {
    public string StageName;
    public int difficulty;
    public string description;
    public List<Vector3> LevelRooms;

    private void OnValidate() {
        Validate(); // Remember to call boardStats.Validate(); whenever changing LevelRooms at runtime
    }

    public void Validate() {
        int consecutiveSideRooms = 0;

        for (int i = 0; i < LevelRooms.Count; i++) {
            Vector3 room = LevelRooms[i];

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
    }

}