using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public float mod = -1.56f;
    public float movementSpeed = 10;
    private float targetY; // Track target Y separately

    void LateUpdate() {
        var levelLayers = GameManager.levelLayers;
        var LevelRooms = GameManager.currentLevelData.LevelRooms;
        int currentBoard = GameManager.currentBoard;

        // Find the appropriate Y position based on room type
        if (LevelRooms[currentBoard].z == 0f) { // Main room 
            targetY = levelLayers[currentBoard].transform.position.y;
            Debug.Log("Main room Y: " + targetY);
        } else { // Side room
            int mainRoomIndex = FindAssociatedMainRoom(currentBoard);
            targetY = levelLayers[mainRoomIndex].transform.position.y;
            Debug.Log($"Side room {currentBoard} using main room {mainRoomIndex} Y: {targetY}");
        }

        // Calculate horizontal position
        CalculateHorizontalOffset();
        float targetX = mod + (GameManager.currentColumn * 25.10178f);

        // Smooth movement to target position
        Vector3 target = new Vector3(targetX, targetY + 0.5f, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * Time.deltaTime);
    }

    void CalculateHorizontalOffset() {
        switch (GameManager.currentColumn) {
            case 0: mod = 0f; break;
            case 1: mod = -1.56f; break;
            case -1: mod = 1.56f; break;
        }
    }

    private int FindAssociatedMainRoom(int currentBoard) {
        // Search downward for nearest main room
        for (int i = currentBoard; i >= 0; i--) {
            if (GameManager.currentLevelData.LevelRooms[i].z == 0f)
                return i;
        }
        // Search upward if not found below
        for (int i = currentBoard; i < GameManager.currentLevelData.LevelRooms.Count; i++) {
            if (GameManager.currentLevelData.LevelRooms[i].z == 0f)
                return i;
        }
        return currentBoard; // Fallback
    }
}