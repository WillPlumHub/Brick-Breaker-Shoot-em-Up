using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.Table;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class GameManager : MonoBehaviour {

    public static GameManager Instance { get; private set; } // Singleton instance

    [Header("Game state")]
    public int score;
    public TMP_Text scoreText;
    public static bool IsGameStart;
    public static int CurrentScore;
    public int targetScore = CurrentScore + 50;
    public static int scoreMult = 1;
    public float scoreMultTimer = 10;
    public static bool ceilinigDestroyed = false;

    [Header("Paddle")]
    public static float DisableTimer;
    public static float MagnetOffset;
    public static float DecelerationRate;

    [Header("Balls")]
    public GameObject ballPrefab;
    public static float BallSpeed = 3f;
    public static bool CanSpawnBall = false;
    public static List<GameObject> ActiveBalls = new List<GameObject>(); //balls
    [SerializeField] private List<GameObject> ballsInEditor; //_balls Debug

    [Header("Bricks")]
    public static float brickLimit = 10;
    public float BrickLimit = 10;
    public GameObject brickcontainer; // Debug
    public static GameObject brickContainer;

    public static int brickCount;
    public int initialBrickCount;
    public int publicBrickCount = 0; // Debug

    [Header("Level")]
    public Vector2 currentPosition;
    public GameObject[,] LevelLayers; // Debug
    public static GameObject[,] levelLayers;
    public static Queue<Vector2> RoomHistory = new Queue<Vector2>();
    public Queue<Vector2> roomHistory = new Queue<Vector2>(); // Debug
    public static bool isRemovingBoard = false;
    public static int numberOfBoards;
    public int boardAmount = 1; // Debug


    public static int currentBoardRow = 0;
    public static int currentBoardColumn = 1; // Track column (0=left, 1=main, 2=right)
    public static string nextScene;
    public string publicNextScene; // Debug
    public float blankTransitionTime;
    public static bool hasLoadedNextLevel;

    public static float roofScrollSpeedModifier = 1f; // Normal speed
    public static float lastHorizontalTransitionTime = 0f;
    public static bool needsFastRoofScroll = false;
    public static float moveSpeed = 5f; // Your original move speed

    public static bool isShiftingDown = false;
    public bool shiftingDown = false; // Debug
    public static Coroutine roofCoroutine;
    public static bool isTransitioning = false;
    public bool istransitioning = false; // Debug

    public static LevelData currentLevelData;
    public static int lastMainBoard;
    public int lastmainmoard; // Debug


    [Header("Power Ups")]
    public bool brickThu = false;
    public bool fireBall = false;
    public bool superShrink = false;
    public bool gravityBall = false;
    public float PaddleSizeMod = 1;
    public static bool BrickThu = false;
    public static bool FireBall = false;
    public static bool GravityBall = false;
    public List<PowerUpData> powerUps;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    public AudioClip backgroundMusic;
    public AudioClip ballBounceSound;
    public AudioClip paddleBounceSound;
    public AudioClip perfectFlipSound;
    public AudioClip flipSound;

    [Header("Debug")]
    public bool DebugMode = false;
    public TMP_Text DebugInfo;

    #region Set up
    private void Awake() {
        GameObject levelManagerObj = GameObject.Find("LevelManager");
        if (levelManagerObj == null) {
            Debug.LogWarning("LevelManager GameObject not found.");
        } else {
            LevelBuilder levelBuilder = levelManagerObj.GetComponent<LevelBuilder>();
            if (levelBuilder == null) {
                Debug.LogWarning("LevelBuilder component not found on LevelManager.");
            } else if (levelBuilder.levelData == null) {
                Debug.LogWarning("No LevelData assigned in LevelBuilder.");
            }
        }

        HandleSingleton();
        InitializeAudio();
        SceneManager.sceneLoaded += OnSceneLoaded;

        boardAmount = 0; // Temporary until we rebuild
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void RebuildLevelLayers() {
        LevelBuilder levelBuilder = GameObject.Find("LevelManager")?.GetComponent<LevelBuilder>();

        if (levelBuilder == null || levelBuilder.CreatedBoards == null) {
            Debug.LogError("LevelBuilder or CreatedBoards is missing");
            return;
        }
        levelLayers = levelBuilder.CreatedBoards;
        numberOfBoards = GetTotalBoardCount();
    }

    private int GetTotalBoardCount() {
        if (levelLayers == null) return 0;
        int count = 0;
        for (int row = 0; row < levelLayers.GetLength(0); row++) {
            for (int col = 0; col < levelLayers.GetLength(1); col++) {
                if (levelLayers[row, col] != null) {
                    count++;
                }
            }
        }
        return count;
    }

    public static GameObject GetCurrentLayer() {
        if (levelLayers == null || currentBoardRow < 0 || currentBoardRow >= levelLayers.GetLength(0) || currentBoardColumn < 0 || currentBoardColumn >= levelLayers.GetLength(1)) {
            return null;
        }
        return levelLayers[currentBoardRow, currentBoardColumn];
    }

    public static GameObject GetLayer(int row, int column) {
        if (levelLayers == null || row < 0 || row >= levelLayers.GetLength(0) || column < 0 || column >= levelLayers.GetLength(1)) {
            return null;
        }
        return levelLayers[row, column];
    }
    #endregion

    private void Start() {
        ceilinigDestroyed = false;
        InitializeGame();

        // Rebuild level layers NOW, after LevelBuilder has finished in Awake()
        RebuildLevelLayers();
        boardAmount = GetTotalBoardCount();

        if (RoomHistory == null) {
            RoomHistory = new Queue<Vector2>();
        }

        RoomHistory.Enqueue(new Vector2(1, 0));
        Vector2[] historyArray = RoomHistory.ToArray();
        Debug.Log($"  [{0}]: Room {historyArray[0]}");
    }

    private void Update() {

        if (isTransitioning) {
            Debug.Log("HRT!!!!!!!!!!");
        }
        UpdateDebugValues();
        UpdateGameState();
        HandleScore();
        CheckGameState();
        debugOptions();

        if (!isShiftingDown && !isRemovingBoard) {
            transitionCheck();
            //CountBricks();
        }

        if (scoreMult != 1 && ActiveBalls.Count > 0 && !IsGameStart) {
            scoreMultTimer -= (1 * Time.deltaTime);
            //Debug.Log("x2 timer: " + timer);
        }

        if (scoreMultTimer <= 0) {
            scoreMult = 1;
            scoreMultTimer = 10;
        }

        if (DebugMode) {
            Debug.Log("DebugDisabling Score Saving");
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            //PrintRoomHistory();
            LocalRoomData roomData = GetCurrentLayer().GetComponent<LocalRoomData>();
            Debug.Log("[CASE 3] Brick Count == " + roomData.numberOfBricks);
        }
    }

    public void transitionCheck() {
        if (hasLoadedNextLevel || IsGameStart || isRemovingBoard) {
            Debug.Log($"Transition blocked - hasLoadedNextLevel: {hasLoadedNextLevel}, IsGameStart: {IsGameStart}, isRemovingBoard: {isRemovingBoard}");
            return;
        }

        GameObject currentLayer = GetCurrentLayer();
        if (currentLayer == null) {
            Debug.Log("Transition blocked - currentLayer is null");
            return;
        }

        if (isShiftingDown) {
            Debug.Log("Transition blocked - isShiftingDown is true");
            return;
        }

        LocalRoomData roomData = currentLayer.GetComponent<LocalRoomData>();
        if (roomData == null) {
            Debug.Log("Transition blocked - roomData is null");
            return;
        }

        // Only allow roof scrolling if layers aren't already moving down
        if (!isShiftingDown && !isTransitioning) {
            Transform roofTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));
            if (roofTransform != null) {
                float expectedRoofY = -4.5f + (10f * (currentBoardRow + 1f));
                float roofDelta = roofTransform.position.y - expectedRoofY;

                if (Mathf.Abs(roofDelta) > 0.1f) {
                    // CHECK IF WE SHOULD USE FASTER SCROLLING (only if camera is far from current room)
                    roofScrollSpeedModifier = 1f; // Normal speed


                    List<Transform> layersToMove = GetLayersFromRow(currentBoardRow);
                    if (roofCoroutine != null) StopCoroutine(roofCoroutine);

                    isShiftingDown = true;
                    //roofCoroutine = StartCoroutine(MovePlayerAndCameraUp(roofDelta, moveSpeed));
                    StartCoroutine(MoveLayersDown(layersToMove, roofDelta, moveSpeed));

                    // Reset the flag after starting the scroll
                    needsFastRoofScroll = false;

                    return; // exit early since we're moving layers
                }
            }
        }

        // Only trigger board removal / next level if no bricks remain
        if (roomData.numberOfBricks <= 0f) {
            Debug.Log($"[CASE 3] Board cleared at Row {currentBoardRow}, Column {currentBoardColumn}");
            float boardZ = roomData.localRoomData.z;
            int onesZ = ((int)Mathf.Floor(boardZ)) % 10;

            switch (onesZ) {
                case 0: // Move up a row, delete previous row
                    Debug.Log("[CASE 3] Case 0 triggered. Brick Count == " + roomData.numberOfBricks);
                    StartCoroutine(RemoveCurrentBoard(ZCase: 0));
                    break;

                case 1: // Delete current board, reset paddle/balls to previous board or main/side room
                    Debug.Log("[CASE 3] Case 1 triggered. Brick Count == " + roomData.numberOfBricks);
                    StartCoroutine(RemoveCurrentBoard(ZCase: 1));
                    break;

                case 2: // Load next level when bricks cleared
                    StartCoroutine(RemoveCurrentBoard(ZCase: 2));
                    break;

                case 3:
                    Debug.Log("[CASE 3] Case 3 triggered. Brick Count == " + roomData.numberOfBricks);
                    break;
            }
        } else {
            Debug.Log($"Brick count: {roomData.numberOfBricks} - not ready for transition");
        }
    }

    private List<Transform> GetLayersFromRow(int row) {
        List<Transform> layersToMove = new List<Transform>();
        if (levelLayers == null) return layersToMove;

        // Get ALL layers from ALL rows (not just current row and above)
        for (int currentRow = 0; currentRow < levelLayers.GetLength(0); currentRow++) {
            for (int col = 0; col < levelLayers.GetLength(1); col++) {
                if (levelLayers[currentRow, col] != null && levelLayers[currentRow, col].transform != null) {
                    layersToMove.Add(levelLayers[currentRow, col].transform);
                }
            }
        }
        return layersToMove;
    }

    private IEnumerator MoveLayersDown(List<Transform> layers, float distance, float speed = 5f) {
        isShiftingDown = true; // Use static field

        Debug.Log($"[CASE 0] Starting MoveLayersDown | Layers: {layers.Count}, Distance: {distance}, Speed: {speed}");

        // APPLY SPEED MODIFIER to the passed speed parameter
        float effectiveSpeed = speed * roofScrollSpeedModifier;

        float totalMoved = 0f;
        float duration = Mathf.Abs(distance) / effectiveSpeed;
        List<Vector3> startPositions = layers.Select(l => l.position).ToList();
        List<Vector3> endPositions = startPositions.Select(pos => pos - new Vector3(0, distance, 0)).ToList();

        for (int i = 0; i < layers.Count; i++) {
            Debug.Log($"[CASE 0] Layer[{i}] {layers[i].name} Start: {startPositions[i]}, Target: {endPositions[i]}");
        }

        while (totalMoved < duration && isShiftingDown) {
            // Check if there are no active balls and break out of the loop
            if (ActiveBalls == null || ActiveBalls.Count <= 0) {
                Debug.Log("Stopping scroll - no active balls");
                break;
            }

            totalMoved += Time.deltaTime;
            float t = Mathf.Clamp01(totalMoved / duration);

            for (int i = 0; i < layers.Count; i++) {
                if (layers[i] != null) {
                    layers[i].position = Vector3.Lerp(startPositions[i], endPositions[i], t);
                }
            }
            yield return null;
        }

        // Only complete the movement if we weren't canceled and there are still active balls
        if (isShiftingDown && ActiveBalls != null && ActiveBalls.Count > 0) {
            // Ensure final positions are exact
            for (int i = 0; i < layers.Count; i++) {
                if (layers[i] != null) {
                    layers[i].position = endPositions[i];
                }
                Debug.Log($"[CASE 0] Layer[{i}] Final Position: {endPositions[i]}");
            }
        } else {
            Debug.Log("[CASE 0] Scroll interrupted or no active balls - not completing movement");
        }

        // RESET SPEED MODIFIER AFTER SCROLL COMPLETES OR INTERRUPTS
        roofScrollSpeedModifier = 1f;
        isShiftingDown = false; // Use static field
        Debug.Log("[CASE 0] Finished MoveLayersDown");
    }


    public IEnumerator RemoveCurrentBoard(int ZCase) {

        int rows = levelLayers.GetLength(0);
        int cols = levelLayers.GetLength(1);

        GameObject currentBoard = GetLayer(currentBoardRow, currentBoardColumn);
        if (currentBoard == null) yield break;

        LocalRoomData roomData = currentBoard.GetComponent<LocalRoomData>();
        float boardZ = roomData?.localRoomData.z ?? 0f;

        // Z = 2: Load Next Level
        if (ZCase == 2) {
            while (roomData != null && roomData.numberOfBricks > 0) yield return null;
            hasLoadedNextLevel = true;
            currentBoardColumn = 1;
            currentBoardRow = 0;
            LoadNextLevel();
            yield break;
        }

        // Z = 1: Delete current board & reset back to the Previous Room
        if (ZCase == 1) {
            Debug.Log("[CASE 3] ZCase == 1. brickCount == " + roomData.numberOfBricks);
            // Store current position before destruction
            int originalRow = currentBoardRow;
            int originalColumn = currentBoardColumn;

            levelLayers[currentBoardRow, currentBoardColumn] = null;
            Destroy(currentBoard);

            // Use RoomHistory to find the previous room
            if (RoomHistory != null && RoomHistory.Count > 0) {
                // Remove the current room (which we're destroying) from history
                Vector2 currentRoom = new Vector2(originalColumn, originalRow);
                Queue<Vector2> cleanedHistory = new Queue<Vector2>();

                foreach (Vector2 room in RoomHistory) {
                    if (room != currentRoom) {
                        cleanedHistory.Enqueue(room);
                    }
                }
                RoomHistory = cleanedHistory;

                // Get the previous room (oldest in queue)
                if (RoomHistory.Count > 0) {
                    Vector2 previousRoom = RoomHistory.Peek(); // Oldest room is the previous one
                    int previousRow = (int)previousRoom.y;
                    int previousColumn = (int)previousRoom.x;

                    Debug.Log($"Returning to previous room from history: [{previousColumn}, {previousRow}] (was at [{originalColumn}, {originalRow}])");

                    // Calculate vertical movement needed for paddle
                    int rowDifference = previousRow - originalRow;

                    // Update current position
                    currentBoardRow = previousRow;
                    currentBoardColumn = previousColumn;

                    // Adjust paddle position based on vertical movement
                    GameObject paddle = GameObject.Find("Paddle");
                    PaddleMove paddleMove = paddle.GetComponent<PaddleMove>();
                    if (paddleMove != null) {
                        if (rowDifference > 0) {
                            // Moving up - add paddle height
                            paddleMove.baseYPos += 10 * rowDifference;
                            paddleMove.currentYPos += 10 * rowDifference;
                            paddleMove.maxFlipHeight += 10 * rowDifference;
                            Debug.Log($"Moving paddle UP by {10 * rowDifference} units");
                        } else if (rowDifference < 0) {
                            // Moving down - subtract paddle height
                            paddleMove.baseYPos += 10 * rowDifference; // rowDifference is negative
                            paddleMove.currentYPos += 10 * rowDifference;
                            paddleMove.maxFlipHeight += 10 * rowDifference;
                            Debug.Log($"Moving paddle DOWN by {10 * -rowDifference} units");
                        }

                        // Center paddle in the new room
                        GameObject newRoom = levelLayers[currentBoardRow, currentBoardColumn];
                        if (newRoom != null) {
                            Transform leftWall = null;
                            Transform rightWall = null;

                            foreach (Transform child in newRoom.transform) {
                                if (child.name.StartsWith("LWall")) leftWall = child;
                                else if (child.name.StartsWith("RWall")) rightWall = child;
                            }
                            if (leftWall != null && rightWall != null) {
                                float roomCenterX = (leftWall.position.x + rightWall.position.x) / 2f;
                                paddleMove.transform.position = new Vector3(roomCenterX, paddleMove.currentYPos, 0f);
                                Debug.Log($"Centered paddle at X: {roomCenterX}, Y: {paddleMove.currentYPos}");
                            }
                        }
                    }
                } else {
                    // Fallback: default to main room
                    currentBoardRow = 0;
                    currentBoardColumn = 1;
                    Debug.Log("RoomHistory empty, defaulting to [1, 0]");
                }
            } else {
                // Fallback: default to main room
                currentBoardRow = 0;
                currentBoardColumn = 1;
                Debug.Log("No RoomHistory, defaulting to [1, 0]");
            }

            // Reset balls to the new room
            GameObject targetRoomObj = levelLayers[currentBoardRow, currentBoardColumn];
            if (targetRoomObj != null) {
                Vector3 roomPos = targetRoomObj.transform.position;

                foreach (var ball in ActiveBalls) {
                    if (ball == null) continue;
                    ball.transform.position = roomPos;

                    BallMovement ballMove = ball.GetComponent<BallMovement>();
                    if (ballMove != null) {
                        ballMove.currentSpeed = 0.1f;
                        ballMove.moveDir = Vector2.right;
                    }
                    ball.gameObject.SetActive(true);
                }
            }
        }

        // --- Z = 0: Delete entire row
        if (ZCase == 0) {
            int rowToRemove = currentBoardRow;

            Debug.Log("[CASE 0] ZCase == 0. rowToRemove == " + rowToRemove);
            // 1) Cache GameObjects in this row before clearing
            List<GameObject> roomsInDeletedRow = new List<GameObject>();
            for (int columnIndex = 0; columnIndex < cols; columnIndex++) {
                var room = levelLayers[rowToRemove, columnIndex];
                if (room != null) {
                    roomsInDeletedRow.Add(room);
                    Debug.Log("[CASE 0] Room To Remove == " + room.name + " at " + rowToRemove + ", " + columnIndex);
                }
            }

            float yOffset = 0f;
            if (roomsInDeletedRow.Count > 0) {
                for (int i = 0; i < roomsInDeletedRow.Count; i++) {
                    if (roomsInDeletedRow[i] != null) {
                        var data = roomsInDeletedRow[i].GetComponent<LocalRoomData>();
                        if (data != null && data.localRoomData.y > yOffset) {
                            yOffset = data.localRoomData.y;
                        }
                    }
                }
            }
            Debug.Log("[Roof Scroll] yOffset: " + yOffset + " + 10 = " + (yOffset + 10));

            List<Transform> roomsToMove = new List<Transform>();
            for (int rowIndex = rowToRemove + 1; rowIndex < rows; rowIndex++) {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++) {
                    GameObject room = levelLayers[rowIndex, columnIndex];
                    if (room != null) {
                        Debug.Log("[CASE 0] Going to move room: " + room + " currently at: " + rowIndex + ", " + columnIndex);
                        roomsToMove.Add(room.transform);
                    }
                }
            }

            // 2) Shift rows above down
            for (int sourceRow = rowToRemove + 1; sourceRow < rows; sourceRow++) {
                for (int columnIndex = 0; columnIndex < cols; columnIndex++) {
                    levelLayers[sourceRow - 1, columnIndex] = levelLayers[sourceRow, columnIndex];
                    Debug.Log("[CASE 0]  moving: " + sourceRow + ", " + columnIndex + " To: " + (sourceRow - 1) + ", " + columnIndex);
                }
            }

            // 3) Clear top row
            for (int columnIndex = 0; columnIndex < cols; columnIndex++) {
                levelLayers[rows - 1, columnIndex] = null;
                Debug.Log("[CASE 0] Clearing old: " + (rows - 1) + ", " + columnIndex + " slots from 2D array");
            }

            // 4) Destroy the removed row’s objects
            foreach (GameObject room in roomsInDeletedRow) {
                if (room != null) {
                    Debug.Log("[CASE 0] Removing from roomsInDeletedRow cache: " + room);
                    Destroy(room);
                }
            }

            // 5) Update currentBoardRow to be the row just below what we deleted
            Debug.Log("[CASE 0] Old CurrentBoardRow: " + currentBoardRow);
            currentBoardRow = Mathf.Max(0, rowToRemove);
            Debug.Log("[CASE 0] New CurrentBoardRow: " + currentBoardRow);

            // 6) Try to pick the new current room in the same column, otherwise fallback to first available
            GameObject newCurrentRoom = levelLayers[currentBoardRow, currentBoardColumn];
            if (newCurrentRoom == null) {
                for (int c = 0; c < cols; c++) {
                    if (levelLayers[currentBoardRow, c] != null) {
                        currentBoardColumn = c;
                        newCurrentRoom = levelLayers[currentBoardRow, c];
                        Debug.Log("[CASE 0] New CurrentBoardColumn: " + c);
                        Debug.Log("[CASE 0] So, sending player to position: " + currentBoardRow + ", " + c);
                        break;
                    }
                }
            }

            if (newCurrentRoom != null) {
                GameObject paddle = GameObject.Find("Paddle");
                float paddleBaseY = paddle.GetComponent<PaddleMove>().baseYPos;
                Vector3 spawnPos = new Vector3(newCurrentRoom.transform.position.x, paddleBaseY + 4, newCurrentRoom.transform.position.z);

                foreach (var ball in ActiveBalls) {
                    if (ball == null) continue;

                    ball.transform.position = spawnPos;

                    BallMovement ballMove = ball.GetComponent<BallMovement>();
                    if (ballMove != null) {
                        ballMove.currentSpeed = 0.1f;
                        ballMove.moveDir = Vector2.down;
                    }
                    ball.gameObject.SetActive(true);
                }
            }


            if (roomsToMove.Count > 0) {
                float distance = 10f + yOffset;
                float calculatedSpeed = distance / 0.5f;
                Debug.Log("[CASE 0] Starting Layer Move Coroutine: " + roomsToMove + ", " + distance + ", " + calculatedSpeed);
                yield return StartCoroutine(MoveLayersDown(roomsToMove, distance, calculatedSpeed));
            }
        }

        // Final housekeeping
        brickContainer = null;
        CountBricks();

        // If player falls below first row, load next level as a fallback
        if (currentBoardRow < 0) {
            hasLoadedNextLevel = true;
            LoadNextLevel();                                                                                 // FIX
            yield break;
        }
        yield break;
    }

    public void CountBricks() {
        // Skip brick counting entirely if between levels or during board removal
        if (hasLoadedNextLevel || isRemovingBoard || isShiftingDown) {
            brickCount = 0;
            return;
        }

        GameObject currentLayer = GetCurrentLayer();
        if (currentLayer == null) {
            brickCount = 0;
            return;
        }

        if (brickContainer == null) {
            Transform blockListTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("BlockList"));
            if (blockListTransform != null) {
                brickContainer = blockListTransform.gameObject;
            } else {
                Debug.LogWarning("BlockList not found under " + currentLayer.name);
            }
        }

        brickCount = 0;
        if (brickContainer == null) {
            GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
            foreach (GameObject brick in bricks) {
                ObjHealth health = brick.GetComponent<ObjHealth>();
                if (brick.activeInHierarchy && health != null && !health.Invincibility) {
                    brickCount++;
                }
            }
            if (initialBrickCount == 0) {
                initialBrickCount = brickCount;
            }
            return;
        }

        foreach (Transform child in brickContainer.transform) {
            ObjHealth health = child.GetComponent<ObjHealth>();
            if (child.gameObject.activeInHierarchy && health != null && !health.Invincibility) {
                brickCount++;
            }
        }

        if (initialBrickCount == 0) {
            initialBrickCount = brickCount;
        }
    }




    #region PowerUps
    public void expandPaddle() {
        GameObject paddle = GameObject.Find("Paddle");
        Vector3 scale = paddle.transform.localScale;
        if (scale.x < 18.4f) {
            paddle.GetComponent<PaddleMove>().magnetOffset *= 1.2f;
            scale.x *= PaddleSizeMod;
            paddle.transform.localScale = scale;
        }
    }

    public void shrinkPaddle() {
        GameObject paddle = GameObject.Find("Paddle");
        Vector3 scale = paddle.transform.localScale;
        if (scale.x > 0.5f) {
            paddle.GetComponent<PaddleMove>().magnetOffset /= 1.2f;
            scale.x /= PaddleSizeMod;
            paddle.transform.localScale = scale;
        }

    }

    public void superShrinkPaddle() {
        GameObject paddle = GameObject.Find("Paddle");
        paddle.transform.localScale = new Vector3(0.7407407f, 1.8f, 0.54922f);
    }



    public void BrickZap() {
        GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
        foreach (GameObject brick in bricks) {
            if (brick.GetComponent<ObjHealth>().health > 1) {
                // Lightning Strike animation
                brick.GetComponent<ObjHealth>().health = 1;
            }
            if (brick.GetComponent<ObjHealth>().Invincibility) {
                brick.GetComponent<ObjHealth>().Invincibility = false;
            }
        }
    }

    public void Lightning() {
        if (brickContainer == null) {
            Debug.LogWarning("LocalRoomData or brickContainer not found, falling back to global search");
            return;
        }

        // Get bricks only from this room's brickContainer
        List<GameObject> availableBricks = new List<GameObject>();
        foreach (Transform child in brickContainer.transform) {
            ObjHealth health = child.GetComponent<ObjHealth>();
            if (child.gameObject.activeInHierarchy && health != null && !health.Invincibility) {
                availableBricks.Add(child.gameObject);
            }
        }
        if (availableBricks.Count == 0) return;

        int strikes = Mathf.Min(3, availableBricks.Count);
        for (int x = 0; x < strikes; x++) {
            int index = UnityEngine.Random.Range(0, availableBricks.Count);
            GameObject brick = availableBricks[index];
            if (brick.GetComponent<ObjHealth>() != null) {
                // Lightning Strike animation
                brick.GetComponent<ObjHealth>().TakeDamage((int)brick.GetComponent<ObjHealth>().health, 1, 0);
            }
            availableBricks.RemoveAt(index);
        }
    }

    public void PowerUpSpawn(float odds, Vector3 spawnPos) {
        float cumulative = 0f;

        if (powerUps.Count <= 0) {
            Debug.Log("No powerups assigned. Remember to add them to Game Manager!");
        }

        foreach (PowerUpData powerUp in powerUps) {

            cumulative += powerUp.dropRate;

            if (odds <= cumulative) {
                Instantiate(powerUp.prefab, spawnPos, Quaternion.identity);
                //Debug.Log($"Spawned PowerUp: {powerUp.itemName}");
                return;
            }
        }
        //Debug.Log("No Power-up Spawned.");
    }
    #endregion



    #region Initialization
    private void HandleSingleton() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void InitializeGame() {
        CurrentScore = 0;
        ActiveBalls ??= new List<GameObject>();

        if (brickContainer == null) {
            brickContainer = GameObject.Find("BlockList");
        }
    }

    private void InitializeAudio() {
        if (musicSource == null) {
            musicSource = GetComponent<AudioSource>();
        }
        if (musicSource != null && !musicSource.enabled) {
            musicSource.enabled = true;
        }
        if (backgroundMusic != null && musicSource != null) {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        } else {
            Debug.LogWarning("Missing audio references!");
        }
    }
    #endregion



    #region Ball Management
    public void SpawnBall() {
        bool grabbed = false;
        foreach (var ball in ActiveBalls) {
            if (ball != null) {
                var ballMovement = ball.GetComponent<BallMovement>();
                if (ballMovement != null && ballMovement.isStuckToPaddle) {
                    grabbed = true;
                    ballMovement.isStuckToPaddle = false;
                }
            }
        }
        float direction = grabbed ? UnityEngine.Random.Range(-1f, 1f) : 1f;
        GameObject newBall = Instantiate(ballPrefab, new Vector2(ActiveBalls[0].transform.position.x + 0.5f, ActiveBalls[0].transform.position.y), Quaternion.identity);
        newBall.GetComponent<BallMovement>().isStuckToPaddle = false;
        newBall.GetComponent<BallMovement>().InitializeBall(new Vector2(direction, 1));
    }

    public void SpawnEightBall() {
        if (ActiveBalls == null || ActiveBalls.Count == 0 || ActiveBalls[0] == null) {
            Debug.LogWarning("No active balls available to spawn from.");
            return;
        }
        Vector2 centerPos = ActiveBalls[0].transform.position;
        bool grabbed = false;
        // Unstick any balls that are stuck to the paddle
        foreach (var ball in ActiveBalls) {
            if (ball != null) {
                var ballMovement = ball.GetComponent<BallMovement>();
                if (ballMovement != null && ballMovement.isStuckToPaddle) {
                    grabbed = true;
                    ballMovement.isStuckToPaddle = false;
                }
            }
        }

        for (int i = 0; i < 8; i++) {
            float angleDeg = i * 45f; // 360° / 8 = 45°
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector2 spawnPos = centerPos + offset;
            Vector2 direction = grabbed ? new Vector2(UnityEngine.Random.Range(-1f, 1f), 1f).normalized : offset.normalized;
            GameObject newBall;
            if (i == 0) { // Use the original ball
                newBall = ActiveBalls[0];
                newBall.transform.position = spawnPos;
            } else { // Instantiate new balls
                newBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
            }

            var ballMovement = newBall.GetComponent<BallMovement>();
            if (ballMovement != null) {
                ballMovement.moveDir = direction;
                ballMovement.currentSpeed = 5f;
                ballMovement.ceilingBreak = ActiveBalls[0].GetComponent<BallMovement>().ceilingBreak;
            }
        }
    }


    public static void RegisterBall(GameObject ball) {
        ActiveBalls ??= new List<GameObject>();
        if (!ActiveBalls.Contains(ball)) {
            ActiveBalls.Add(ball);
        }
    }

    public static void UnregisterBall(GameObject ball) {
        if (ActiveBalls != null && ActiveBalls.Contains(ball)) {
            ActiveBalls.Remove(ball);
        }
    }
    #endregion



    #region Game Logic
    private void UpdateGameState() {
        ballsInEditor = new List<GameObject>(ActiveBalls);

        if (scoreText == null) {
            TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
            foreach (TMP_Text txt in texts) {
                if (txt.name.EndsWith("ScoreText")) {
                    scoreText = txt;
                    break;
                }
            }
        }

        if (scoreText != null) {
            scoreText.text = CurrentScore.ToString();
        }

        if (DebugInfo == null) {
            TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
            foreach (TMP_Text txt in texts) {
                if (txt.name.EndsWith("DebugText")) {
                    DebugInfo = txt;
                    break;
                }
            }
        }
    }

    private void HandleScore() {
        LocalRoomData roomData = GetCurrentLayer().GetComponent<LocalRoomData>();
        if (roomData == null) {
            Debug.Log("Transition blocked - roomData is null");
            return;
        }
        if (CurrentScore > 0 && CurrentScore >= targetScore && !CanSpawnBall && roomData.numberOfBricks > 0 && roomData.numberOfBricks < roomData.initialNumberOfBricks && !IsGameStart) { // Ball Spawn conditions
            SpawnBall();
            CanSpawnBall = true;
            targetScore += 50;
        }
    }

    private void CheckGameState() {
        if (ActiveBalls.Count <= 0 && !IsGameStart) {
            GameOver();
        }
    }

    public void GameOver() {
        Debug.Log("GAME OVER: " + ActiveBalls.Count);
    }

    public void LoadNextLevel() {
        if (!string.IsNullOrEmpty(nextScene)) {
            hasLoadedNextLevel = true;
            StopAllCoroutines(); // Stop coroutines using old scene data
            currentPosition = new Vector2(1, 0);
            currentBoardColumn = 1;
            currentBoardRow = 0;
            RoomHistory = new Queue<Vector2>();
            RoomHistory.Enqueue(new Vector2(1, 0));
            SceneManager.LoadScene(nextScene);
            numberOfBoards = GameObject.Find("LevelManager").GetComponent<LevelBuilder>().levelData.LevelRooms.Count;
        }
    }
    #endregion



    #region Scene Management
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        initialBrickCount = 0;
        brickCount = 0;
        hasLoadedNextLevel = false;
        brickContainer = null;
        scoreText = null;
        RebuildLevelLayers();
    }
    #endregion



    #region Audio
    public void PlaySFX(AudioClip clip) {
        if (sfxSource != null && clip != null) {
            sfxSource.PlayOneShot(clip);
        }
    }
    #endregion



    #region Debug
    private void UpdateDebugValues() {
        publicBrickCount = brickCount;
        publicNextScene = nextScene;
        boardAmount = numberOfBoards;
        BrickThu = brickThu;
        FireBall = fireBall;
        GravityBall = gravityBall;
        LevelLayers = levelLayers;
        brickcontainer = brickContainer;
        shiftingDown = isShiftingDown;
        lastmainmoard = lastMainBoard;
        roomHistory = RoomHistory;
        isTransitioning = istransitioning;
        currentPosition.y = currentBoardRow;
        currentPosition.x = currentBoardColumn;
        brickLimit = BrickLimit;
    }

    public void debugOptions() {
        LocalRoomData roomData = null;
        GameObject currentLayer = GetCurrentLayer();
        if (currentLayer == null) {
            Debug.LogWarning("[DEBUG] Current Layer is null for some reason");
        }
        roomData = currentLayer.GetComponent<LocalRoomData>();
        if (roomData == null) {
            Debug.LogWarning("[DEBUG] RoomData is null for some reason");
        }

        if (Input.GetKeyDown(KeyCode.BackQuote)) {
            DebugMode = !DebugMode;
            Debug.Log($"Debug Mode: {(DebugMode ? "ON" : "OFF")}");
        }

        if (!DebugMode) return;

        // Game State Debugging
        if (Input.GetKeyDown(KeyCode.Q)) {
            CurrentScore += 1000;
            Debug.Log($"Added 1000 points. Total: {CurrentScore}");
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            scoreMult = scoreMult == 1 ? 2 : 1;
            Debug.Log($"Score multiplier: x{scoreMult}");
        }

        // Ball Management Debugging
        if (Input.GetKeyDown(KeyCode.B)) {
            SpawnBall();
            Debug.Log("Spawned additional ball");
        }

        if (Input.GetKeyDown(KeyCode.N)) {
            SpawnEightBall();
            Debug.Log("Spawned 8-ball cluster");
        }

        if (Input.GetKeyDown(KeyCode.M)) {
            foreach (var ball in ActiveBalls.ToArray()) {
                if (ball != null) {
                    ball.GetComponent<BallMovement>().currentSpeed *= 1.5f;
                }
            }
            Debug.Log("Increased ball speed by 50%");
        }

        // Brick Debugging
        if (Input.GetKeyDown(KeyCode.R)) {
            BrickZap();
            Debug.Log("Brick Zap activated - all bricks reduced to 1 health");
        }

        if (Input.GetKeyDown(KeyCode.T)) {
            Lightning();
            Debug.Log("Lightning strike - 3 random bricks destroyed");
        }

        // Board/Level Debugging
        if (Input.GetKeyDown(KeyCode.L)) {
            LoadNextLevel();
            Debug.Log("Force loading next level");
        }

        // Power-up Debugging
        if (Input.GetKeyDown(KeyCode.Z)) {
            expandPaddle();
            Debug.Log("Paddle expanded");
        }

        if (Input.GetKeyDown(KeyCode.X)) {
            shrinkPaddle();
            Debug.Log("Paddle shrunk");
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            superShrinkPaddle();
            Debug.Log("Paddle super shrunk");
        }

        if (Input.GetKeyDown(KeyCode.V)) {
            // Spawn random power-up at paddle position
            GameObject paddle = GameObject.Find("Paddle");
            if (paddle != null) {
                float randomOdds = UnityEngine.Random.Range(0f, 1f);
                PowerUpSpawn(randomOdds, new Vector3(paddle.transform.position.x, paddle.transform.position.y + 5f, 0f));
                Debug.Log("Spawned random power-up above paddle");
            }
        }

        // Toggle Game Start
        if (Input.GetKeyDown(KeyCode.G)) {
            IsGameStart = !IsGameStart;
            Debug.Log($"Game Start: {IsGameStart}");
        }

        // Clear all bricks in current room
        if (Input.GetKeyDown(KeyCode.H)) {
            if (roomData != null) {
                roomData.numberOfBricks = 0;
                Debug.Log("[DEBUG] Force cleared bricks for transition in current room");
            }
        }

        // Force a Left Transition
        if (Input.GetKeyDown(KeyCode.Y)) {
            // Find all XTransition objects in this room
            XTransition[] xTransitions = currentLayer.GetComponentsInChildren<XTransition>(true);
            if (xTransitions == null || xTransitions.Length == 0) {
                Debug.LogWarning("[DEBUG] No XTransition components found in current room.");
                return;
            }

            // Determine target transition based on current column
            int targetTransitionValue = -1;
            if (currentBoardColumn == 1) {
                targetTransitionValue = 0;
            } else if (currentBoardColumn == 2) {
                targetTransitionValue = 1;
            }

            if (targetTransitionValue != -1 && ActiveBalls.Count > 0) {
                Debug.Log("[DEBUG] Starting Left Hand Side Room transition...");
                foreach (GameObject ball in ActiveBalls) {
                    ball.transform.position = levelLayers[currentBoardRow, targetTransitionValue].transform.position;
                    ball.GetComponent<BallMovement>().moveDir.y = 1;
                }
                currentBoardColumn = targetTransitionValue;
            } else {
                Debug.LogWarning("No appropriate left XTransition found in current room");
            }
        }

        // Force a Right Transition
        if (Input.GetKeyDown(KeyCode.U)) {
            if (currentLayer == null) {
                Debug.LogWarning("[DEBUG] Current layer is null. Cannot perform left transition.");
                return;
            }
            // Find all XTransition objects in this room
            XTransition[] xTransitions = currentLayer.GetComponentsInChildren<XTransition>(true);
            if (xTransitions == null || xTransitions.Length == 0) {
                Debug.LogWarning("[DEBUG] No XTransition components found in current room.");
                return;
            }

            // Determine target transition based on current column
            int targetTransitionValue = -1;
            if (currentBoardColumn == 0) {
                targetTransitionValue = 1;
            } else if (currentBoardColumn == 1) {
                targetTransitionValue = 2;
            }

            if (targetTransitionValue != -1 && ActiveBalls.Count > 0) {
                Debug.Log("[DEBUG] Starting Left Hand Side Room transition...");
                foreach (GameObject ball in ActiveBalls) {
                    ball.transform.position = levelLayers[currentBoardRow, targetTransitionValue].transform.position;
                    ball.GetComponent<BallMovement>().moveDir.y = 1;
                }
                currentBoardColumn = targetTransitionValue;
            } else {
                Debug.LogWarning("No appropriate left XTransition found in current room");
            }
        }

        // Force an Upward Transition
        if (Input.GetKeyDown(KeyCode.J) && currentBoardRow < levelLayers.GetLength(0) - 1) {
            int nextRow = currentBoardRow + 1;
            if (levelLayers != null && nextRow < levelLayers.GetLength(0) && levelLayers[nextRow, currentBoardColumn] != null && ActiveBalls.Count > 0) {
                PaddleMove paddleMove = GameObject.Find("Paddle").GetComponent<PaddleMove>();
                foreach (GameObject ball in ActiveBalls) {
                    ball.transform.position = levelLayers[currentBoardRow + 1, currentBoardColumn].transform.position;
                }
                Debug.Log($"[DEBUG] Forced upward transition to row {nextRow} in column {currentBoardColumn}");
                currentBoardRow = nextRow;
            }
        }

        // Force a Downward Transition
        if (Input.GetKeyDown(KeyCode.K) && currentBoardRow > 0) {
            int prevRow = currentBoardRow - 1;
            if (levelLayers != null && prevRow >= 0 && levelLayers[prevRow, currentBoardColumn] != null && ActiveBalls.Count > 0) {
                PaddleMove paddleMove = GameObject.Find("Paddle").GetComponent<PaddleMove>();
                foreach (GameObject ball in ActiveBalls) {
                    ball.transform.position = levelLayers[currentBoardRow - 1, currentBoardColumn].transform.position;
                }
                Debug.Log($"[DEBUG] Forced downward transition to row {prevRow} in column {currentBoardColumn}");
                currentBoardRow = prevRow;
            }
        }

        // OPFTV Are left

        // Number keys for power-up spawning
        if (Input.GetKeyDown(KeyCode.Alpha1)) SpawnPowerUpByIndex(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SpawnPowerUpByIndex(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SpawnPowerUpByIndex(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SpawnPowerUpByIndex(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SpawnPowerUpByIndex(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SpawnPowerUpByIndex(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SpawnPowerUpByIndex(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SpawnPowerUpByIndex(7);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SpawnPowerUpByIndex(8);
        if (Input.GetKeyDown(KeyCode.Alpha0)) SpawnPowerUpByIndex(9);
        if (DebugInfo != null && roomData != null) {
            debugText(roomData);
        } else {
            Debug.LogWarning("DebugInfo TMP_Text is not assigned!");
        }
    }

    public void debugText(LocalRoomData roomData) {
        string activeBallsSummary = "None";
        if (ActiveBalls != null && ActiveBalls.Count > 0) {
            var aliveBalls = ActiveBalls
                .Where(b => b != null) // filters destroyed objects
                .Take(5)
                .Select(b => b ? b.name : "null"); // safe access using conditional
            activeBallsSummary = string.Join(", ", aliveBalls);
            if (ActiveBalls.Count > 5) activeBallsSummary += "...";
            if (!aliveBalls.Any()) activeBallsSummary = "None";
        }


        // RoomHistory summary (skip invalid/destroyed entries)
        string roomHistorySummary = "Empty";
        if (RoomHistory != null && RoomHistory.Count > 0) {
            var validRooms = RoomHistory
                .Where(r => r != null) // for GameObjects, or just skip if Vector2
                .Take(5)
                .Select(r => r.ToString()); // Vector2 is safe, for GameObjects use: r ? r.name : "null"
            roomHistorySummary = string.Join(", ", validRooms);
            if (RoomHistory.Count > 5) roomHistorySummary += "...";
        }


        int rows = 0;
        int cols = 0;
        if (levelLayers != null) {
            rows = levelLayers.GetLength(0);
            cols = levelLayers.GetLength(1);
        }
        int bricks = 0;
        int initialBricks = 0;
        if (roomData != null) {
            bricks = roomData.numberOfBricks;
            initialBricks = roomData.initialNumberOfBricks;
        }
        string brickContainerName = "N/A";
        if (brickContainer != null) {
            brickContainerName = brickContainer.name;
        }
        int activeBallCount = 0;
        if (ActiveBalls != null) {
            activeBallCount = ActiveBalls.Count;
        }
        string activeBallsText = "N/A";
        if (activeBallsSummary != null) {
            activeBallsText = activeBallsSummary;
        }
        int historyCount = 0;
        if (RoomHistory != null) {
            historyCount = RoomHistory.Count;
        }
        string historyText = "N/A";
        if (roomHistorySummary != null) {
            historyText = roomHistorySummary;
        }
        string nextSceneName = "N/A";
        if (nextScene != null) {
            nextSceneName = nextScene;
        }
        string currentLevel = "N/A";
        if (currentLevelData != null) {
            currentLevel = currentLevelData.ToString();
        }
        string powerUpsText = "N/A";
        if (powerUps != null) {
            powerUpsText = powerUps.ToString();
        }

        DebugInfo.text =
            "=== GAME STATE INFO ===\n" +
            "IsGameStart: " + IsGameStart + "\n" +
            "Current Board: Row " + currentBoardRow + ", Column " + currentBoardColumn + "\n" +
            "Total Rows: " + rows + ", Columns: " + cols + "\n" +
            "Bricks: " + bricks + "/" + initialBricks + " from " + brickContainerName + " Z: " + roomData.localRoomData.z + "\n\n" +
            "Active Balls (" + activeBallCount + "): " + activeBallsText + "\n\n" +
            "Room History (" + historyCount + "): " + historyText + "\n\n" +
            "Target Score: " + targetScore + ", Can Spawn Ball: " + CanSpawnBall + "\n" +
            "Score: " + CurrentScore + " (Multiplier: x" + scoreMult + " - " + scoreMultTimer + "s)\n" +
            "Next Scene: " + nextSceneName + ", Has Loaded Next Level: " + hasLoadedNextLevel + "\n" +
            "Is Shifting Down: " + isShiftingDown + ", Is Transitioning: " + isTransitioning + "\n" +
            "Is Removing Board: " + isRemovingBoard + ", Blank Transition Time: " + blankTransitionTime + "\n" +
            "Move Speed: " + moveSpeed + ", Paddle Size Mod: " + PaddleSizeMod + "\n" +
            "Current Level Data: " + currentLevel + "\n" +
            "BrickThu: " + brickThu + ", FireBall: " + fireBall + ", GravityBall: " + gravityBall + ", SuperShrink: " + superShrink + "\n" +
            "PowerUps: " + powerUpsText;

        SetDebugOutline(0.4f, Color.black);

    }

    private void SpawnPowerUpByIndex(int index) {
        if (powerUps == null || index < 0 || index >= powerUps.Count || powerUps[index].prefab == null) {
            Debug.LogWarning($"Cannot spawn power-up at index {index}");
            return;
        }
        GameObject paddle = GameObject.Find("Paddle");
        Instantiate(powerUps[index].prefab, new Vector3(paddle.transform.position.x, paddle.transform.position.y + 5f), Quaternion.identity);
        Debug.Log($"Spawned {powerUps[index].itemName} at position {new Vector3(paddle.transform.position.x, paddle.transform.position.y + 5f)}");
    }

    public void SetDebugOutline(float thickness, Color color) {
        if (DebugInfo == null) return;

        if (!DebugInfo.fontMaterial.name.EndsWith("(Instance)")) {
            DebugInfo.fontMaterial = new Material(DebugInfo.fontMaterial);
        }

        DebugInfo.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Clamp01(thickness));
        DebugInfo.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, color);
        DebugInfo.SetMaterialDirty();
    }
    #endregion
}