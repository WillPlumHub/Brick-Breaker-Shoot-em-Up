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

        List<int> finalBoards = new List<int>();

        // Find first occurrences of z=0, z=1, and z=-1 boards
        bool foundZ0 = false, foundZ1 = false, foundZMinus1 = false;

        for (int i = boardStats.LevelRooms.Count - 1; i > 0; i--) {
            float currentZ = boardStats.LevelRooms[i].z;

            if (!foundZ0 && currentZ == 0f) {
                finalBoards.Add(i);
                foundZ0 = true;
                Debug.Log($"[INITIAL] Added main room (z=0) at index {i}");
            } else if (!foundZ1 && (int)currentZ == 1f) {
                finalBoards.Add(i);
                foundZ1 = true;
                Debug.Log($"[INITIAL] Added right side room (z=1) at index {i}");
            } else if (!foundZMinus1 && (int)currentZ == -1f) {
                finalBoards.Add(i);
                foundZMinus1 = true;
                Debug.Log($"[INITIAL] Added left side room (z=-1) at index {i}");
            }
            // Early exit if we've found all three
            if (foundZ0 && foundZ1 && foundZMinus1) {
                break;
            }
        }

        boardStats.Validate();

        for (int i = 0; i < boardStats.LevelRooms.Count; i++) {

            if (boardStats.LevelRooms[i].z == 0f && boardStats.LevelRooms[i].y != 0f) { // Add roof if gap between board rows
                if (i + 1 < boardStats.LevelRooms.Count && boardStats.LevelRooms[i + 1].z != 0f && boardStats.LevelRooms[i + 1].y != boardStats.LevelRooms[i].y && !finalBoards.Contains(i + 1)) {
                    finalBoards.Add(i + 1);
                }
                if (i + 2 < boardStats.LevelRooms.Count && boardStats.LevelRooms[i + 2].z != 0f && boardStats.LevelRooms[i + 2].y != boardStats.LevelRooms[i].y && !finalBoards.Contains(i + 2)) {
                    finalBoards.Add(i + 2);
                }
            }

            int zValue = (int)boardStats.LevelRooms[i].z;

            // Only care about z == 1 or z == -1
            if (zValue == 1 || zValue == -1) {
                string side = zValue == 1 ? "Right Side Room" : "Left Side Room";
                Debug.Log($"[{zValue} ROOF CHECK] Found {side} at index {i}");

                int zeroCount = 0;
                int nextSameZIndex = -1;

                // Look forward for the next room with the same z value and count zeros in between
                for (int j = i + 1; j < boardStats.LevelRooms.Count; j++) {
                    int currentZ = (int)boardStats.LevelRooms[j].z;

                    if (currentZ == 0) {
                        Debug.Log($"[{zValue} ROOF CHECK]   Found Main Side Room at index {j}, adding to zeroCount: {zeroCount + 1}");
                        zeroCount++;
                    } else if (currentZ == zValue) {
                        Debug.Log($"[{zValue} ROOF CHECK]   Found another {side} at index {j} from {i}");

                        if (zeroCount >= 2 && !finalBoards.Contains(i)) {
                            finalBoards.Add(i);
                            Debug.Log($"[{zValue} ROOF CHECK] Added index {i} (zeros between: {zeroCount}, next same z at {j})");
                        }
                        nextSameZIndex = j;
                        break;
                    }
                }
            }
        }

        // Final output
        /*for (int i = 0; i < finalBoards.Count; i++) {
            int roomIndex = finalBoards[i];
            var room = boardStats.LevelRooms[roomIndex];
            Debug.Log($"[FINAL ROOMS] Room #{i + 1}: Room {roomIndex} gets a roof");
        }*/



        Vector3 verticalStackPosition = new Vector3(-1.56f, 0f, 0f);
        Vector3 lastVerticalPosition = verticalStackPosition;
        Vector3 transitionPos = Vector3.zero;
        lastVerticalBoard = null;

        for (int i = 0; i < boardStats.LevelRooms.Count; i++) {
            Vector3 spawnPosition;
            GameObject currentBoard = null;

            if (boardStats.LevelRooms[i].z != 0f) {
                // Side rooms
                float direction = Mathf.Sign(boardStats.LevelRooms[i].z); // Left (-1) or Right (1)
                float horizontalOffset = direction * 23.6f; // Fixed distance from main room
                spawnPosition = lastVerticalPosition + new Vector3(horizontalOffset, 0f, 0f);
                float wallOffset = (7.3f + boardStats.LevelRooms[i].x) * direction;
                float baseOffset = -1.56f;

                // Instantiate room
                currentBoard = RoomAdjust(spawnPosition, i, finalBoards);

                // Handle XTransition proportional along wall
                float decimalZ = Mathf.Abs(boardStats.LevelRooms[i].z) - Mathf.Floor(Mathf.Abs(boardStats.LevelRooms[i].z));
                Transform sideWall = currentBoard.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("LWall") || t.name.StartsWith("RWall"));
                Transform mainWall = lastVerticalBoard?.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => direction > 0 ? t.name.StartsWith("RWall") : t.name.StartsWith("LWall"));

                if (sideWall != null && mainWall != null && decimalZ > 0f) {
                    
                    // Fixed proportional position near the bottom (equivalent to Z = 0.2)
                    float fixedDecimalZ = 0.2f;

                    // Calculate position on side wall (20% from bottom)
                    float sideWallBottomY = sideWall.position.y - (sideWall.localScale.y / 2f);
                    float sideWallHeight = sideWall.localScale.y;
                    float transitionYOnSideWall = sideWallBottomY + fixedDecimalZ * sideWallHeight;

                    // Calculate proportional position on main wall
                    float mainWallBottomY = mainWall.position.y - (mainWall.localScale.y / 2f);
                    float mainWallHeight = mainWall.localScale.y;
                    float transitionYOnMainWall = mainWallBottomY + decimalZ * mainWallHeight;

                    // XTransition in side room back to main room
                    Vector3 transitionPos1 = new Vector3(spawnPosition.x - wallOffset - baseOffset, transitionYOnSideWall, 0f);
                    GameObject trans1 = Instantiate(XTransition, transitionPos1, Quaternion.identity);
                    trans1.GetComponent<XTransition>().transition = 0;
                    trans1.transform.SetParent(currentBoard.transform);

                    // XTransition in main room to side room
                    Vector3 transitionPos2 = new Vector3((7.34f + transitionPos.x) * direction, transitionYOnMainWall, 0f);
                    GameObject trans2 = Instantiate(XTransition, transitionPos2, Quaternion.identity);
                    trans2.GetComponent<XTransition>().transition = direction > 0 ? 1 : -1;
                    if (lastVerticalBoard != null) trans2.transform.SetParent(lastVerticalBoard.transform);

                    trans1.GetComponent<XTransition>().partnerTransition = trans2;
                    trans2.GetComponent<XTransition>().partnerTransition = trans1;
                }
            } else {
                // Main vertical rooms
                spawnPosition = verticalStackPosition;
                lastVerticalPosition = verticalStackPosition;
                transitionPos.x = boardStats.LevelRooms[i].x;

                verticalStackPosition += new Vector3(0f, 10f + boardStats.LevelRooms[i].y, 0f);

                currentBoard = RoomAdjust(spawnPosition, i, finalBoards);
                lastVerticalBoard = currentBoard;
            }
        }

        GameManager.currentLevelData = boardStats;
    }

    public GameObject RoomAdjust(Vector3 spawnPosition, int i, List<int> finalBoards) {

        GameObject board = finalBoards.Contains(i) ? Instantiate(finalBoardTemplate, spawnPosition, Quaternion.identity) : Instantiate(boardTemplate, spawnPosition, Quaternion.identity);
        CreatedBoards.Add(board);
        board.GetComponent<LocalRoomData>().initialBoardPosition = i;

        // Walls
        Transform LWall = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("LWall"));
        Transform RWall = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("RWall"));
        Transform Roof = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));

        float baseY = board.transform.position.y; // floor
        float roofY = Roof != null ? Roof.position.y + boardStats.LevelRooms[i].y : baseY + 10f + boardStats.LevelRooms[i].y;

        if (LWall != null) { // Adjust Left Wall
            LWall.position = new Vector3(LWall.position.x - boardStats.LevelRooms[i].x, LWall.position.y, LWall.position.z);
            float wallBottomY = LWall.position.y - (LWall.localScale.y / 2f);
            float newHeight = roofY - wallBottomY;
            LWall.localScale = new Vector3(LWall.localScale.x, newHeight, LWall.localScale.z);
            LWall.position = new Vector3(LWall.position.x, wallBottomY + newHeight / 2f, LWall.position.z);
        }

        if (RWall != null) { // Adjust Right Wall
            RWall.position = new Vector3(RWall.position.x + boardStats.LevelRooms[i].x, RWall.position.y, RWall.position.z);
            float wallBottomY = RWall.position.y - (RWall.localScale.y / 2f);
            float newHeight = roofY - wallBottomY;
            RWall.localScale = new Vector3(RWall.localScale.x, newHeight, RWall.localScale.z);
            RWall.position = new Vector3(RWall.position.x, wallBottomY + newHeight / 2f, RWall.position.z);
        }

        if (Roof != null && finalBoards.Contains(i)) { // Adjust Roof - only for final boards
            // Calculate the distance between walls
            float leftWallX = LWall != null ? LWall.position.x : board.transform.position.x - 7.3f;
            float rightWallX = RWall != null ? RWall.position.x : board.transform.position.x + 7.3f;
            float roofWidth = rightWallX - leftWallX;

            // Position roof at midpoint between walls
            float roofX = (leftWallX + rightWallX) / 2f;

            // Scale roof to span between walls
            Roof.localScale = new Vector3(roofWidth, Roof.localScale.y, Roof.localScale.z);
            Roof.position = new Vector3(roofX, roofY, Roof.position.z);
        } else if (Roof != null) { // Regular board roof adjustment (original behavior)
            Roof.position = new Vector3(Roof.position.x, roofY, Roof.position.z);
        }

        return board;
    }
}