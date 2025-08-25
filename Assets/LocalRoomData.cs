using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalRoomData : MonoBehaviour {

    public Vector4 localLevelData;
    public int initialBoardPosition;
    public int numberOfBricks = 0;

    void Start() {
        if (GameManager.currentLevelData == null) {
            Debug.LogError("currentLevelData is null!");
            return;
        }

        if (GameManager.currentLevelData.LevelRooms == null) {
            Debug.LogError("LevelRooms list is null!");
            return;
        }

        if (initialBoardPosition < 0 || initialBoardPosition >= GameManager.currentLevelData.LevelRooms.Count) {
            Debug.LogError("initialBoardPosition out of range!");
            return;
        }

        //Debug.Log("LEVEL DATA: initialBP: " + initialBoardPosition + ", localLVLData: " + GameManager.currentLevelData.LevelRooms[initialBoardPosition]);
        localLevelData = GameManager.currentLevelData.LevelRooms[initialBoardPosition];
    }

    public void Update() {
        CountBricks();
    }

    public void CountBricks() {
        
        GameObject currentLayer = GameManager.levelLayers[GameManager.currentBoard];
        if (currentLayer == null) {
            Debug.LogError("levelLayers[" + GameManager.currentBoard + "] is null.");
            return;
        }

        if (GameManager.brickContainer == null) {
            Transform blockListTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("BlockList"));

            if (blockListTransform != null) {
                GameManager.brickContainer = blockListTransform.gameObject;
            } else {
                Debug.LogWarning("BlockList not found under " + currentLayer.name);
            }
        }

        numberOfBricks = 0;
        if (GameManager.brickContainer == null) {
            GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
            foreach (GameObject brick in bricks) {
                ObjHealth health = brick.GetComponent<ObjHealth>();
                if (brick.activeInHierarchy && health != null && !health.Invincibility) {
                    numberOfBricks++;
                }
            }
            return;
        }

        foreach (Transform child in GameManager.brickContainer.transform) {
            ObjHealth health = child.GetComponent<ObjHealth>();
            if (child.gameObject.activeInHierarchy && health != null && !health.Invincibility) {
                numberOfBricks++;
            }
        }
    }
}