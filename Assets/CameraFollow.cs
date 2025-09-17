using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public float mod = -1.56f;
    public float movementSpeed = 10;
    private float targetY;

    void LateUpdate() {
        var levelLayers = GameManager.levelLayers;
        int currentBoardRow = GameManager.currentBoardRow;
        int currentBoardColumn = GameManager.currentBoardColumn;
        GameObject paddle = GameObject.Find("Paddle");

        if (levelLayers == null || currentBoardRow < 0 || currentBoardRow >= levelLayers.GetLength(0) || currentBoardColumn < 0 || currentBoardColumn >= levelLayers.GetLength(1) || paddle == null) {
            return;
        }

        GameObject currentRoom = GameManager.GetLayer(currentBoardRow, currentBoardColumn);
        if (currentRoom == null) return;

        GameObject mainRoom = GameManager.GetLayer(currentBoardRow, 1); // Main room is always column 1
        targetY = paddle.GetComponent<PaddleMove>().baseYPos + 4.5f; // Offset above the floor
        
        // Horizontal target is just the room's X position
        float targetX = currentRoom.transform.position.x;

        // Smooth movement to target position
        Vector3 target = new Vector3(targetX, targetY, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * Time.deltaTime);
    }
}