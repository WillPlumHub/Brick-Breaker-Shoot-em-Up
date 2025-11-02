using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class CameraFollow : MonoBehaviour {

    public float movementSpeed = 10;
    public float followTolerance = 0.5f; // How close the camera must be before stopping movement
    public float smoothTime = 0.2f;      // Smoothing factor
    private float targetY;
    private Vector3 velocity = Vector3.zero;

    public static bool RoomTransitioning = false;
    public bool roomTransitioning = false;

    void LateUpdate() {


        roomTransitioning = RoomTransitioning;
        /*if (RoomTransitioning) {
            StartCoroutine(Transition());
            Debug.Log("WEIRHWOEUFHAOFOASJDFGE");
        }*/
        var currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) return;
        GameObject currentRoom = GameManager.GetLayer(GameManager.currentBoardRow, GameManager.currentBoardColumn);
        if (currentRoom == null) return;
        LocalRoomData currentRoomData = currentLayer.GetComponent<LocalRoomData>();
        if (currentRoomData == null) return;

        float roomYOffset = currentRoomData.localRoomData.y;
        float halfHeight = (10f + roomYOffset) / 2f;
        float roomBottomY = GameManager.currentRoofHeight - 10 - roomYOffset + 5;
        float roomTopY = GameManager.currentRoofHeight - 5;

        /*float roomBottomY = currentRoom.transform.position.y - (currentRoom.transform.localScale.y / 2f);
        
        float roomTopY = currentRoom.transform.position.y + (currentRoom.transform.localScale.y / 2f);*/
        Debug.Log($"[4 SCROLL] RoomYOffset: {roomYOffset}, Bottom: {roomBottomY}, Top: {roomTopY}, CurrentRoomPos: {currentRoom.transform.position.y}");

        /*
        if (roomData.localRoomData.z < 10 || (int)(roomData.localRoomData.z / 10) % 10 == 1) {
            mod = roomData.localRoomData.y;
        }
         */

        if (((int)GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.z < 10 || (int)(GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.z / 10) % 10 == 0) && /*!GameManager.cameraMoving &&*/ !RoomTransitioning) {
            //roomYOffset = GameManager.CurrentRoomData.localRoomData.y;
            if (GameManager.ActiveBalls.Count > 0 && GameManager.ActiveBalls[0] != null && !GameManager.IsGameStart) {
                Debug.Log("[4 SCROLL] Ball Focus: Bottom: " + roomBottomY + ", currentRoofHeight: " + roomYOffset + ", " + 15 + ", roomYOffset: " + GameManager.CurrentRoomData.localRoomData.y + " Top: " + roomTopY);
                Vector3 ballPos = GameManager.ActiveBalls[0].transform.position;

                Vector3 target = new Vector3(currentRoom.transform.position.x, ballPos.y, transform.position.z);
                Vector3 newPos = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
                float clampedY = Mathf.Clamp(newPos.y, roomBottomY, roomTopY);
                transform.position = new Vector3(newPos.x, clampedY, newPos.z);

                /*if (Vector3.Distance(new Vector3(transform.position.x, transform.position.y, 0), new Vector3(target.x, target.y, 0)) > followTolerance) {
                    GameManager.cameraMoving = true;
                } else {
                    GameManager.cameraMoving = false;
                }*/
            }
        } else {
            Debug.Log("[4 SCROLL] PADDLE FOCUS:");
            var levelLayers = GameManager.levelLayers;
            int currentBoardRow = GameManager.currentBoardRow;
            int currentBoardColumn = GameManager.currentBoardColumn;
            

            if (levelLayers == null || currentBoardRow < 0 || currentBoardRow >= levelLayers.GetLength(0) || currentBoardColumn < 0 || currentBoardColumn >= levelLayers.GetLength(1) || GameManager.Paddle == null) {
                return;
            }

            GameObject mainRoom = GameManager.GetLayer(currentBoardRow, 1); // Main room is always column 1
            targetY = GameManager.Paddle.GetComponent<PaddleMove>().baseYPos + 4.5f; // Offset above the floor

            // Horizontal target is just the room's X position
            float targetX = currentRoom.transform.position.x;

            // Smooth movement to target position
            Vector3 target = new Vector3(targetX, targetY, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * Time.deltaTime);

            if (Vector3.Distance(new Vector3(transform.position.x, transform.position.y, transform.position.z), target) > 0.1f) {
                GameManager.cameraMoving = true;
            } else {
                GameManager.cameraMoving = false;
            }
        }
    }

    public static IEnumerator Transition(float waitTime) {
        yield return new WaitForSeconds(waitTime);
        RoomTransitioning = false;
    }
}