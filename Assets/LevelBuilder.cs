using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelBuilder : MonoBehaviour {

    public LevelData boardStats;
    public GameObject boardTemplate;
    public GameObject finalBoardTemplate;
    public GameObject XTransition;

    public int finalBoardIndex = 0;
    public List<GameObject> CreatedBoards { get; private set; } = new List<GameObject>();

    private GameObject lastVerticalBoard;

    private void Awake() {
        if (boardStats == null) return;

        for (int i = boardStats.LevelRooms.Count - 1; i >= 0; i--) {
            if (boardStats.LevelRooms[i].z == 0f) {
                finalBoardIndex = i;
                if (boardStats.LevelRooms[i - 1].z != 0) {
                    finalBoardIndex--;
                }
                if (boardStats.LevelRooms[i - 2].z != 0) {
                    finalBoardIndex--;
                }
                break;
            }
        }

        boardStats.Validate();

        Vector3 verticalStackPosition = new Vector3(-1.56f, 0f, 0f);
        Vector3 lastVerticalPosition = verticalStackPosition;
        Vector3 transitionPos = Vector3.zero;

        for (int i = 0; i < boardStats.LevelRooms.Count; i++) {
            Vector3 spawnPosition;
            GameObject currentBoard = null;

            if (boardStats.LevelRooms[i].z != 0f) { // Side rooms
                float direction = Mathf.Sign(boardStats.LevelRooms[i].z);
                float horizontalOffset = direction * 23.6f;
                spawnPosition = lastVerticalPosition + new Vector3(horizontalOffset, 0f, 0f);
                float wallOffset = (7.3f + boardStats.LevelRooms[i].x) * direction;
                float baseOffset = -1.56f;

                currentBoard = RoomAdjust(spawnPosition, i);

                // Main/Side room XTransition pair
                Vector3 transitionPos1 = new Vector3(spawnPosition.x - wallOffset - baseOffset, spawnPosition.y, 0f);
                GameObject trans1 = Instantiate(XTransition, transitionPos1, Quaternion.identity);
                trans1.GetComponent<XTransition>().transition = 0; // entering main column
                if (currentBoard != null) trans1.transform.SetParent(currentBoard.transform);

                Vector3 transitionPos2 = new Vector3((7.34f + transitionPos.x) * direction, spawnPosition.y, 0f);
                GameObject trans2 = Instantiate(XTransition, transitionPos2, Quaternion.identity);
                trans2.GetComponent<XTransition>().transition = direction > 0 ? 1 : -1; // going to right or left side room
                if (lastVerticalBoard != null) trans2.transform.SetParent(lastVerticalBoard.transform);

                trans1.GetComponent<XTransition>().partnerTransition = trans2;
                trans2.GetComponent<XTransition>().partnerTransition = trans1;
            } else { // Main central rooms
                spawnPosition = verticalStackPosition;
                lastVerticalPosition = verticalStackPosition;
                transitionPos.x = boardStats.LevelRooms[i].x;

                verticalStackPosition += new Vector3(0f, 10f + boardStats.LevelRooms[i].y, 0f);

                currentBoard = RoomAdjust(spawnPosition, i);
                lastVerticalBoard = currentBoard;
            }
        }

        GameManager.currentLevelData = boardStats;
    }

    public GameObject RoomAdjust(Vector3 spawnPosition, int i) {
        GameObject board;

        if (i >= finalBoardIndex) {
            board = Instantiate(finalBoardTemplate, spawnPosition, Quaternion.identity);
        } else {
            board = Instantiate(boardTemplate, spawnPosition, Quaternion.identity);
        }

        CreatedBoards.Add(board);

        // Adjust L/RWalls' X based on LevelRoom's X value
        Transform LWall = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("LWall"));
        if (LWall != null) LWall.position = new Vector3(LWall.position.x - boardStats.LevelRooms[i].x, LWall.position.y, LWall.position.z);

        Transform RWall = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("RWall"));
        if (RWall != null) RWall.position = new Vector3(RWall.position.x + boardStats.LevelRooms[i].x, RWall.position.y, RWall.position.z);

        // Adjust Roof's Y based on LevelRoom's Y value
        Transform Roof = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));
        if (Roof != null) Roof.position = new Vector3(Roof.position.x, Roof.position.y + boardStats.LevelRooms[i].y, Roof.position.z);

        return board;
    }
}
