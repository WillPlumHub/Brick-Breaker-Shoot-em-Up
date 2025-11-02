using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class LevelBuilder : MonoBehaviour {

    [Header("Data")]
    public LevelData levelData;

    [Header("Prefabs")]
    public GameObject finalBoardTemplate;
    public GameObject roofPeice;
    public GameObject XTransition;

    [Header("Layout")]
    public float sideRoomXDistance = 23.6f;
    public float baseRoomHeight = 10f;
    public float baseHalfWidth = 7.3f; // Half-width from room center to each wall BEFORE horizontal wall offsets (x)

    public GameObject[,] CreatedBoards; // [row, col] where col: 0 = left,1 = main,2 = right

    private Vector3 nextMainSpawn;
    public float levelHeight = 0;

    private void Awake() {
        if (!ValidateInputs()) {
            return;
        }

        int rows = levelData.LevelRoomsInspector.Count; // Number of rows in level
        CreatedBoards = new GameObject[rows, 3]; // Set up CreatedBoards with 3 columns
        nextMainSpawn = Vector3.zero;

        // Build rooms
        for (int r = 0; r < rows; r++) {

            var row = levelData.LevelRoomsInspector[r];

            // If main room data is missing, create a default zero vector
            if (row.mainRoom == Vector4.zero) {
                row.mainRoom = new Vector4(0f, 0f, 0f, 0f);
            }

            levelData.LevelRoomsInspector[r] = row; // Save the potentially modified row back

            var rowData = levelData.LevelRoomsInspector[r];

            // Main Room
            Vector3 mainSpawn = nextMainSpawn;
            GameObject mainBoard = null;
            GameObject leftBoard = null;
            GameObject rightBoard = null;

            // Always create main on first row, otherwise force z >= 0.1f
            int mainFirstDecimal = (int)(Mathf.Abs(rowData.mainRoom.z * 10f + 0.0001f)) % 10;
            if (mainFirstDecimal != 0) {                           // Main Room
                mainBoard = CreateRoom(r, 1, mainSpawn, rowData.mainRoom, true);
                CreatedBoards[r, 1] = mainBoard;
                GenerateRoofsFromW(mainBoard, rowData.mainRoom.w); // Apply roof W value to main room
            }

            // Side Rooms
            int leftFirstDecimal = (int)(Mathf.Abs(rowData.leftRoom.z * 10f + 0.0001f)) % 10;
            if (rowData.leftRoom != Vector4.zero && leftFirstDecimal != 0) { // Left Room
                var leftPos = mainSpawn + new Vector3(-sideRoomXDistance, 0f, 0f);
                leftBoard = CreateRoom(r, 0, leftPos, rowData.leftRoom, false);
                CreatedBoards[r, 0] = leftBoard;
                GenerateRoofsFromW(leftBoard, rowData.leftRoom.w);

                if (mainBoard != null) {
                    float decimalPart = rowData.leftRoom.z - Mathf.Floor(rowData.leftRoom.z);
                    if (decimalPart >= 0.2f - 0.0001f && decimalPart <= 0.9f + 0.0001f) {
                        CreateSideXTransition(leftBoard, mainBoard, -1, rowData.leftRoom.z);
                    }
                }
            }
            int rightFirstDecimal = (int)(Mathf.Abs(rowData.rightRoom.z * 10f + 0.0001f)) % 10;
            if (rowData.rightRoom != Vector4.zero && rightFirstDecimal != 0) { // Right Room
                var rightPos = mainSpawn + new Vector3(sideRoomXDistance, 0f, 0f);
                rightBoard = CreateRoom(r, 2, rightPos, rowData.rightRoom, false);
                CreatedBoards[r, 2] = rightBoard;
                GenerateRoofsFromW(rightBoard, rowData.rightRoom.w);

                if (mainBoard != null) {
                    float decimalPart = rowData.rightRoom.z - Mathf.Floor(rowData.rightRoom.z);
                    if (decimalPart >= 0.2f - 0.0001f && decimalPart <= 0.9f + 0.0001f) {
                        CreateSideXTransition(rightBoard, mainBoard, 1, rowData.rightRoom.z);
                    }
                }
            }

            // Increment nextMainSpawn using tallest Y of whatever rooms exist
            float mainRoomY;
            if (rowData.mainRoom != Vector4.zero && rowData.mainRoom.z >= 0.1f) {
                mainRoomY = rowData.mainRoom.y;
            } else {
                mainRoomY = 0f;
            }

            float leftRoomY;
            if (rowData.leftRoom != Vector4.zero && rowData.leftRoom.z >= 0.1f) {
                leftRoomY = rowData.leftRoom.y;
            } else {
                leftRoomY = 0f;
            }

            float rightRoomY;
            if (rowData.rightRoom != Vector4.zero && rowData.rightRoom.z >= 0.1f) {
                rightRoomY = rowData.rightRoom.y;
            } else {
                rightRoomY = 0f;
            }

            float tallestY = Mathf.Max(mainRoomY, leftRoomY, rightRoomY);
            levelHeight += (tallestY/10) + 1;
            Debug.Log("[BACKGROUND] TALLEST Y: " + tallestY + " + " + 1 + " = " + levelHeight);
            nextMainSpawn = new Vector3(0f, mainSpawn.y + baseRoomHeight + tallestY, 0f);

            if (r == (rows - 1)) {
                var background = Instantiate(levelData.BackgroundObject, mainSpawn, Quaternion.identity);
                background.name = "Level Background";
                
                background.transform.position = new Vector3(0, (mainBoard.transform.position.y / 2), 1); ;
                background.transform.localScale = new Vector3(7, levelHeight + 1, 1);
                background.GetComponent<SpriteRenderer>().sprite = levelData.Background;
                background.transform.SetParent(mainBoard.transform);
                Debug.Log("[BACKGROUND] background position: " + (mainBoard.transform.position.y / 2) + ", levelHeight: " + levelHeight);
            }
        }

        GameManager.currentLevelData = levelData;
    }

    public void WValueToDigitLists(float wValue, out List<int> intDigits, out List<int> decimalDigits) {
        intDigits = new List<int>();
        decimalDigits = new List<int>();

        // Integer part
        long intPart = (long)Mathf.Floor(wValue);
        string intStr = intPart.ToString();
        for (int i = intStr.Length - 1; i >= 0; i--) {
            intDigits.Add(intStr[i] - '0');
            //Debug.Log("[Roof Gap] Integer digit " + (intStr.Length - 1 - i) + " : " + intStr[i]);
        }

        // Decimal part
        float decimalPart = Mathf.Abs(wValue - intPart);
        if (decimalPart > 0f) {
            string decimalStr = decimalPart.ToString("0.##########").Split('.')[1];
            for (int i = decimalStr.Length - 1; i >= 0; i--) {
                decimalDigits.Add(decimalStr[i] - '0');
                //Debug.Log("[Roof Gap] Decimal digit " + (decimalStr.Length - 1 - i) + " : " + decimalStr[i]);
            }
        }
    }

    private void GenerateRoofsFromW(GameObject mainBoard, float wValue) {
        if (mainBoard == null || roofPeice == null) return;

        // Find roof
        Transform roof = mainBoard.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));
        if (roof == null) return;

        // Check if this room has Y > 0 (needs one-way collision in gaps)
        LocalRoomData roomData = mainBoard.GetComponent<LocalRoomData>();
        bool hasOneWayCollision = roomData != null && roomData.localRoomData.y > 0;

        // Extract int digits
        WValueToDigitLists(wValue, out List<int> intDigits, out _);
        if (intDigits.Count == 0) return;

        // Number of roof pieces
        int onesDigit = intDigits[0];
        int roofPieces = onesDigit + 1;

        // Build gap widths
        int numGaps = roofPieces - 1;
        float[] gaps = new float[numGaps];

        for (int i = 0; i < numGaps; i++) {
            if (i + 1 < intDigits.Count) {
                gaps[i] = intDigits[i + 1];
            } else {
                gaps[i] = 1f; // fallback if not enough digits
            }
        }

        // Destroy original roof
        Destroy(roof.gameObject);

        // Fit gaps into roof width
        float totalWidth = roof.localScale.x;
        float totalGapWidth = gaps.Sum();

        // If gaps + pieces don't fit: all gaps = 1
        if (totalGapWidth >= totalWidth) {
            for (int i = 0; i < numGaps; i++) {
                gaps[i] = 1f;
            }
            totalGapWidth = gaps.Sum();
        }

        float pieceWidth = (totalWidth - totalGapWidth) / roofPieces;
        if (pieceWidth < 0.1f) pieceWidth = 1f; // hard minimum fallback

        // Spawn roof pieces
        float cursor = -totalWidth / 2f;
        for (int i = 0; i < roofPieces; i++) {
            float pieceCenterX = cursor + pieceWidth / 2f;

            GameObject roofPiece = Instantiate(roofPeice, roof.parent);
            roofPiece.name = $"RoofPiece_{i}";

            roofPiece.transform.localPosition = new Vector3(pieceCenterX, roof.localPosition.y, roof.localPosition.z);
            roofPiece.transform.localScale = new Vector3(pieceWidth, 0.2f, 0f);

            cursor += pieceWidth;

            // Create one-way collision pieces in the gaps for Y > 0 rooms
            if (hasOneWayCollision && i < gaps.Length && gaps[i] > 0) {
                CreateOneWayGapPiece(roof.parent, cursor, gaps[i], roof.localPosition);
                cursor += gaps[i];
            } else if (i < gaps.Length) {
                cursor += gaps[i];
            }
        }
    }

    private void CreateOneWayGapPiece(Transform parent, float gapStartX, float gapWidth, Vector3 roofPosition) {
        float gapCenterX = gapStartX + gapWidth / 2f;

        GameObject gapPiece = new GameObject("OneWayGapPiece");
        gapPiece.transform.SetParent(parent);
        gapPiece.transform.localPosition = new Vector3(gapCenterX, roofPosition.y, roofPosition.z);
        gapPiece.transform.localScale = new Vector3(gapWidth, 0.2f, 0f);
        
        // Add one-way collision (BoxCollider2D should remain ENABLED)
        AddOneWayCollision(gapPiece);
    }

    private void AddOneWayCollision(GameObject gapPiece) {
        // Add PlatformEffector2D for one-way collision
        PlatformEffector2D effector = gapPiece.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
        effector.surfaceArc = 180; // Only collide from the top side

        Debug.Log($"Created one-way gap piece: {gapPiece.name} with width {gapPiece.transform.localScale.x}");
    }

    private bool ValidateInputs() {
        if (levelData == null || levelData.LevelRoomsInspector == null || levelData.LevelRoomsInspector.Count == 0) {
            Debug.LogError("LevelData missing or empty.");
            return false;
        }
        if (finalBoardTemplate == null || XTransition == null) {
            Debug.LogError("Assign finalBoardTemplate and XTransition prefab.");
            return false;
        }
        return true;
    }

    private GameObject CreateRoom(int row, int col, Vector3 spawnPos, Vector4 roomData, bool isMain) {
        var board = Instantiate(finalBoardTemplate, spawnPos, Quaternion.identity);
        string roomType;
        if (isMain) {
            roomType = "Main";
        } else {
            if (col == 0) {
                roomType = "Left";
            } else {
                roomType = "Right";
            }
        }
        board.name = $"Room [{row}, {col}] {roomType}";

        var localRoomData = board.GetComponent<LocalRoomData>();
        if (localRoomData != null) { // Give LocalLevelData it's personal info
            localRoomData.initialRoomPosition = new Vector2(col, row);
            localRoomData.localRoomData = roomData;
        }

        AdjustRoomGeometry(board, roomData);
        return board;
    }
        
    private void AdjustRoomGeometry(GameObject board, Vector4 roomData) {
        // Walls
        Transform LWall = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("LWall"));
        Transform RWall = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("RWall"));
        Transform Roof = board.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));

        // Calculate camera horizontal half-size
        float camHalfHeight = Camera.main.orthographicSize;
        float camHalfWidth = camHalfHeight * Camera.main.aspect;

        // World Y Positions
        float baseY = board.transform.position.y; // floor
        float roofY;
        if (Roof != null) {
            roofY = Roof.position.y + roomData.y;
        } else {
            roofY = baseY + 10f + roomData.y;
        }

        // LEFT WALL - position halfway and scale to camera edge
        if (LWall != null) {
            // Current wall position with roomData.x offset
            float currentX = board.transform.position.x - baseHalfWidth - roomData.x;

            // Camera left edge
            float cameraLeftEdge = board.transform.position.x - camHalfWidth;

            // Position halfway between current wall position and camera edge
            float halfwayX = (currentX + cameraLeftEdge) / 2f;

            // Calculate how much to scale the wall to reach camera edge
            float distanceToEdge = Mathf.Abs(halfwayX - cameraLeftEdge);
            float scaleX = distanceToEdge * 2f; // Double the distance since scale is total width

            float wallBottomY = LWall.position.y - (LWall.localScale.y / 2f);
            float newHeight = roofY - wallBottomY;

            LWall.position = new Vector3(halfwayX, wallBottomY + newHeight / 2f, LWall.position.z);
            LWall.localScale = new Vector3(scaleX, newHeight + 0.2f, LWall.localScale.z);
        }

        // RIGHT WALL - position halfway and scale to camera edge
        if (RWall != null) {
            // Current wall position with roomData.x offset
            float currentX = board.transform.position.x + baseHalfWidth + roomData.x;

            // Camera right edge
            float cameraRightEdge = board.transform.position.x + camHalfWidth;

            // Position halfway between current wall position and camera edge
            float halfwayX = (currentX + cameraRightEdge) / 2f;

            // Calculate how much to scale the wall to reach camera edge
            float distanceToEdge = Mathf.Abs(halfwayX - cameraRightEdge);
            float scaleX = distanceToEdge * 2f; // Double the distance since scale is total width

            float wallBottomY = RWall.position.y - (RWall.localScale.y / 2f);
            float newHeight = roofY - wallBottomY;

            RWall.position = new Vector3(halfwayX, wallBottomY + newHeight / 2f, RWall.position.z);
            RWall.localScale = new Vector3(scaleX, newHeight + 0.2f, RWall.localScale.z);
        }

        // ROOF - adjust to match new wall positions
        if (Roof != null) {
            float leftWallX;
            if (LWall != null) {
                leftWallX = LWall.position.x;
            } else {
                leftWallX = board.transform.position.x - camHalfWidth;
            }

            float rightWallX;
            if (RWall != null) {
                rightWallX = RWall.position.x;
            } else {
                rightWallX = board.transform.position.x + camHalfWidth;
            }

            float roofWidth = rightWallX - leftWallX;
            float roofX = (leftWallX + rightWallX) / 2f;

            Roof.localScale = new Vector3(roofWidth, Roof.localScale.y, Roof.localScale.z);
            Roof.position = new Vector3(roofX, roofY, Roof.position.z);
        }
    }

    private Transform FindPart(GameObject root, string startsWith) {
        var childTransform = root.transform.Find(startsWith);
        if (childTransform != null) return childTransform;
        return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name.StartsWith(startsWith));
    }

    private void CreateSideXTransition(GameObject sideBoard, GameObject mainBoard, int direction, float heightRatioZ) {
        if (sideBoard == null || mainBoard == null) {
            Debug.LogWarning("Side or Main board is null.");
            return;
        }

        // Get walls, but don't exit if missing
        bool sideIsLeft = direction < 0;
        Transform sideWall;
        if (sideIsLeft) {
            sideWall = FindPart(sideBoard, "RWall");
        } else {
            sideWall = FindPart(sideBoard, "LWall");
        }

        Transform mainWall;
        if (sideIsLeft) {
            mainWall = FindPart(mainBoard, "LWall");
        } else {
            mainWall = FindPart(mainBoard, "RWall");
        }

        if (sideWall == null) {
            Debug.LogWarning($"Missing side wall on {sideBoard.name} for transition.");
        }

        if (mainWall == null) {
            Debug.LogWarning($"Missing main wall on {mainBoard.name} for transition.");
        }

        float decimalZ = heightRatioZ - Mathf.Floor(heightRatioZ);
        if (decimalZ <= 0f) {
            decimalZ = 1f; // fallback for Z < 1
        }

        // Side Y
        float sideY;
        if (sideWall != null) {
            sideY = sideWall.position.y - sideWall.localScale.y / 2f + 0.2f * sideWall.localScale.y;
        } else {
            sideY = sideBoard.transform.position.y;
        }

        // Main Y
        float mainY;
        if (mainWall != null) {
            mainY = mainWall.position.y - mainWall.localScale.y / 2f + decimalZ * mainWall.localScale.y;
        } else {
            mainY = mainBoard.transform.position.y;
        }

        // Calculate the edge positions of the walls
        float sideWallEdgeX;
        if (sideWall != null) {
            if (sideIsLeft) {
                sideWallEdgeX = sideWall.position.x - sideWall.localScale.x / 2f;
            } else {
                sideWallEdgeX = sideWall.position.x + sideWall.localScale.x / 2f;
            }
        } else {
            sideWallEdgeX = sideBoard.transform.position.x;
        }

        float mainWallEdgeX;
        if (mainWall != null) {
            if (sideIsLeft) {
                mainWallEdgeX = mainWall.position.x + mainWall.localScale.x / 2f;
            } else {
                mainWallEdgeX = mainWall.position.x - mainWall.localScale.x / 2f;
            }
        } else {
            mainWallEdgeX = mainBoard.transform.position.x;
        }

        // Position XTrans at the edge of the walls with a fixed offset to make them poke out
        float pokeOutOffset = 0.0f; // Just leaving this here just in case
        
        float xSide;
        float xMain;
        if (sideIsLeft) {
            xSide = sideWallEdgeX - pokeOutOffset;
            xMain = mainWallEdgeX + pokeOutOffset;
        } else {
            xSide = sideWallEdgeX + pokeOutOffset;
            xMain = mainWallEdgeX - pokeOutOffset;
        }

        // Instantiate transitions
        var sideTrans = Instantiate(XTransition, new Vector3(xSide, sideY, 0f), Quaternion.identity);
        sideTrans.transform.SetParent(sideBoard.transform, true);
        sideTrans.GetComponent<XTransition>().transition = 0;

        var mainTrans = Instantiate(XTransition, new Vector3(xMain, mainY, 0f), Quaternion.identity);
        mainTrans.transform.SetParent(mainBoard.transform, true);
        mainTrans.GetComponent<XTransition>().transition = direction;

        sideTrans.GetComponent<XTransition>().partnerTransition = mainTrans;
        mainTrans.GetComponent<XTransition>().partnerTransition = sideTrans;

        //Debug.Log($"Spawned XTransition: side={sideBoard.name}, main={mainBoard.name}, Z={heightRatioZ}, decimal={decimalZ:F2}");
    }
}