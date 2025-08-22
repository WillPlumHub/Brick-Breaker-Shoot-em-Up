using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalRoomData : MonoBehaviour {

    public Vector4 localLevelData;
    public int initialBoardPosition;

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

        Debug.Log("LEVEL DATA: initialBP: " + initialBoardPosition + ", localLVLData: " + GameManager.currentLevelData.LevelRooms[initialBoardPosition]);
        localLevelData = GameManager.currentLevelData.LevelRooms[initialBoardPosition];
    }
}