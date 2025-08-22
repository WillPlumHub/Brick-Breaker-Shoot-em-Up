using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "LevelData/Create New LevelData")]
public class LevelData : ScriptableObject {
    // Level Name
    public string StageName;
    
    // Approx. Level Difficulty
    public int difficulty;
    
    // Level Description
    public string description;
    
    // List of rooms in the level.
    // Each Vector4 represents a room:
    //      X = Wall's width offset
    //      Y = Roof's height offset
    //      Z = Side Room direction (-1 left, 0 main, 1 right). It's decimal determines the XTransition's height in the Main Room
    //      W = 
    public List<Vector4> LevelRooms;

    // Called automatically in the Editor when the ScriptableObject is modified
    private void OnValidate() {
        Validate(); // Remember to call boardStats.Validate(); whenever changing LevelRooms at runtime
    }

    // Ensures LevelRooms follow rules for spacing, side room limits, and proper sequences
    public void Validate() {
        int consecutiveSideRooms = 0;
        Vector4 lastMainRoom = Vector4.zero;

        for (int i = 0; i < LevelRooms.Count; i++) {
            Vector4 room = LevelRooms[i];

            // Clamp room parameters to allowed ranges
            room.x = Mathf.Clamp(room.x, -4.3f, 2.7f);   // Wall width offset
            room.y = Mathf.Clamp(room.y, 0f, 100f);      // Roof height offset
            room.z = Mathf.Clamp(room.z, -1.9f, 1.9f);
            if (room.z > 1f && room.z < 1.2f) {
                room.z = 1.2f;
            } else if (room.z < -1f && room.z > -1.2f) {
                room.z = -1.2f;
            } else if (room.z > 0f && room.z < 1f) {
                room.z = 1f;
            } else if (room.z > -1f && room.z < 0f) {
                room.z = -1f;
            }
            room.w = Mathf.Clamp(room.w, 0f, 5f);

            //room.z = Mathf.Clamp(room.z, -1.64f, 1.64f); // Side direction & XTransition entrance height
            LevelRooms[i] = room; // assign back at the end

            // Track consecutive horizontal rooms
            if (room.z != 0f) {

                if (room.y > lastMainRoom.y) {
                    room.y = lastMainRoom.y;
                    Debug.LogWarning($"Room {i}: Side room Y value cannot be greater than preceding main room. Clamped to {lastMainRoom.y}.");
                }

                consecutiveSideRooms++;
                // Prevent more than 2 Side Rooms in a row
                if (consecutiveSideRooms > 2) {
                    Debug.LogWarning($"Room {i}: More than 2 consecutive horizontal (z != 0) rooms — forcing z = 0.");
                    room.z = 0f; // Force a Main Room
                    consecutiveSideRooms = 0; // Then reset
                }
            } else {
                consecutiveSideRooms = 0; // reset on Main Room
                lastMainRoom = room;
            }
            // Save the adjusted room back into the list
            LevelRooms[i] = room;
        }

        // Ensure no invalid sequences of side rooms occur
        for (int i = 0; i < LevelRooms.Count - 1; i++) {
            Vector4 currentRoom = LevelRooms[i];
            Vector4 nextRoom = LevelRooms[i + 1];
            
            if (currentRoom.z == 0) {
                lastMainRoom = currentRoom;
            }

            if (currentRoom.z != 0 && currentRoom.y >= lastMainRoom.y) {
                currentRoom.y = lastMainRoom.y;
            }

            // Prevent starting with a side room
            if (i == 0 && currentRoom.z != 0f) {
                Debug.LogWarning($"Cannot start with a side room. Changed to a main room.");
                nextRoom.z = 0f; // Change to a main room
                LevelRooms[i] = nextRoom;
            }
            // Prevent two consecutive z = 1 rooms
            if (currentRoom.z == 1f && nextRoom.z == 1f) {
                Debug.LogWarning($"Rooms {i} and {i + 1}: Consecutive z = 1 rooms. Forcing a z = 0 in between.");
                nextRoom.z = -1f; // Insert a Main Room between
                LevelRooms[i + 1] = nextRoom;
            }
            // Prevent two consecutive z = -1 rooms
            else if (currentRoom.z == -1f && nextRoom.z == -1f) {
                Debug.LogWarning($"Rooms {i} and {i + 1}: Consecutive z = -1 rooms. Forcing a z = 0 in between.");
                nextRoom.z = 0f; // Insert a Main Room between
                LevelRooms[i + 1] = nextRoom;
            }
            // Ensure proper ordering: z = 1 must come before z = -1 if both appear consecutively
            else if (currentRoom.z == -1f && nextRoom.z == 1f) {
                Debug.LogWarning($"Rooms {i} and {i + 1}: z = -1 should not come before z = 1. Swapping.");
                LevelRooms[i] = new Vector4(currentRoom.x, currentRoom.y, 1f, currentRoom.w);
                LevelRooms[i + 1] = new Vector4(nextRoom.x, nextRoom.y, -1f, nextRoom.w);
            }
        }
    }
}