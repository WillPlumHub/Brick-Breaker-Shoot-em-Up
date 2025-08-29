using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
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
    public GameObject brickcontainer; 
    public static GameObject brickContainer;
    
    public static int brickCount;
    public int initialBrickCount;
    public int publicBrickCount = 0; // Debug

    [Header("Level")]
    public static List<GameObject> levelLayers;
    public List<GameObject> LevelLayers;
    public static Stack<int> RoomHistory;
    public Stack<int> roomHistory;
    private bool isRemovingBoard = false;
    public static int numberOfBoards;
    public int boardAmount = 1;
    public static int currentBoard = 0;
    public int currentboard = 0;
    public static int currentColumn = 0;
    public int currentcolumn = 0;
    public static string nextScene;
    public string publicNextScene; // Debug
    public float blankTransitionTime;
    public bool hasLoadedNextLevel;

    public float moveSpeed;
    public static bool isShiftingDown = false;
    public bool shiftingDown = false;

    public static LevelData currentLevelData;
    public static int lastMainBoard;
    public int lastmainmoard;


    [Header("Power Ups")]
    public bool brickThu = false;
    public bool fireBall = false;
    public bool superShrink = false;
    public bool gravityBall = false;
    //public int paddleSizeMod = 0;
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


    #region Set up
    private void Awake() {
        GameObject levelManagerObj = GameObject.Find("LevelManager");
        if (levelManagerObj == null) {
            Debug.LogWarning("LevelManager GameObject not found.");
        } else {
            LevelBuilder levelBuilder = levelManagerObj.GetComponent<LevelBuilder>();
            if (levelBuilder == null) {
                Debug.LogWarning("LevelBuilder component not found on LevelManager.");
            } else if (levelBuilder.boardStats == null) {
                Debug.LogWarning("No LevelData (boardStats) assigned in LevelBuilder.");
            }
        }
                
        numberOfBoards = GameObject.Find("LevelManager").GetComponent<LevelBuilder>().boardStats.LevelRooms.Count;
        //Debug.Log("NumBoards: " + GameObject.Find("LevelManager").GetComponent<LevelBuilder>().boardStats.LevelRooms.Count);
        HandleSingleton();
        InitializeAudio();
        SceneManager.sceneLoaded += OnSceneLoaded;

        RebuildLevelLayers(); // Replaces manual object scan
        boardAmount = levelLayers.Count;

        
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
        if (levelLayers == null) levelLayers = new List<GameObject>();
        else levelLayers.Clear();

        LevelBuilder levelBuilder = GameObject.Find("LevelManager")?.GetComponent<LevelBuilder>();
        if (levelBuilder == null || levelBuilder.CreatedBoards == null) {
            Debug.LogError("LevelBuilder or CreatedBoards is missing.");
            return;
        }
        levelLayers = new List<GameObject>(levelBuilder.CreatedBoards);
        //levelLayers.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
    }
    #endregion

    private void Start() {
        ceilinigDestroyed = false;
        InitializeGame();
        if (RoomHistory == null) {
            RoomHistory = new Stack<int>();
        }

        RoomHistory.Push(0);
        //Debug.Log("[Room History] First addition: " + RoomHistory);
        int[] historyArray = RoomHistory.ToArray();
        Debug.Log($"  [{0}]: Room {historyArray[0]}");
    }

    private void Update() {
        UpdateDebugValues();
        UpdateGameState();
        HandleScore();
        CheckGameState();
        CountBricks();
        debugOptions();

        if (!isShiftingDown && !isRemovingBoard) {
            transitionCheck();
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
            PrintRoomHistory();
        }
    }


    // Method to print all entries in the RoomHistory stack
    public static void PrintRoomHistory() {
        if (RoomHistory == null) {
            Debug.Log("RoomHistory is null");
            return;
        }
        if (RoomHistory.Count == 0) {
            Debug.Log("RoomHistory is empty");
            return;
        }

        // Convert stack to array to preserve order for printing
        int[] historyArray = RoomHistory.ToArray();
        for (int i = historyArray.Length - 1; i >= 0; i--) {
            Debug.Log($"  [{i}]: Room {historyArray[i]}");
        }
    }


    public void transitionCheck() {
        if (!hasLoadedNextLevel && !IsGameStart && !isRemovingBoard /*&& Input.GetButtonDown("Jump")*/) {
            if (currentBoard < 0 || currentBoard >= levelLayers.Count || isShiftingDown) {
                Debug.LogWarning("Current board does not exist or is shifting. Aborting board logic.");
                return;
            }

            // Find current board's roof
            GameObject currentLayer = levelLayers[currentBoard];
            Transform roofTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));
            if (roofTransform == null) return;
            float roofDelta = roofTransform.position.y - (-4.5f + (10f * (currentBoard + 1f)));

            //Debug.Log("[ROOF MOVE] Checking for movement");
            if (roofDelta > 0.01f && !isShiftingDown) { // Scroll roof if needed
                //Debug.Log($"[ROOF MOVE] Current roof is at: {roofTransform.position.y}, but it should be at: {(-4.5f + (10f * (currentBoard + 1f)))}, so the difference is {roofDelta}. Moving all layers from {currentboard} to {levelLayers.Count} at speed of {moveSpeed}"); 
                List<Transform> layersToMove = GetLayersFromIndex(currentBoard);
                StartCoroutine(MoveLayersDown(layersToMove, referenceTransform: roofTransform, speed: moveSpeed));
            }

            // Brick-less transition ONLY when bricks are gone AND scrolling is done AND roof is at target
            if (levelLayers[currentBoard].GetComponent<LocalRoomData>().numberOfBricks <= 0) {
                Debug.Log("SIDE RESET: brickCount <= 0");
                if (roofDelta <= 0.01f && !isShiftingDown) {
                    Debug.Log("SIDE RESET: Starting start of board removal seq");
                    if (levelLayers.Count > 1 && currentBoard < levelLayers.Count - 1 && levelLayers[currentBoard].GetComponent<LocalRoomData>().localLevelData.z == 0) { // If in Main Room
                        isRemovingBoard = true;
                        Debug.Log("SIDE RESET: Starting 1st board removal IEnum. In room: " + levelLayers[currentBoard].GetComponent<LocalRoomData>().localLevelData.z);
                        StartCoroutine(BoardRemovalSequence());
                    } else if (levelLayers.Count > 1 && currentBoard < levelLayers.Count - 1 && levelLayers[currentBoard].GetComponent<LocalRoomData>().localLevelData.z != 0) { // If in Side Room
                        RemoveAndReturnToMostRecentCentralRoom();
                        currentColumn = 0;
                        Debug.Log("WWWWORKED: " + currentColumn);
                    } else { // If final board, go to next level
                        hasLoadedNextLevel = true;
                        StopAllCoroutines();
                        LoadNextLevel();
                    }
                } else {
                    Debug.Log("Bricks are gone, but roof is still descending or scrolling active. Transition delayed.");
                }
            }
        }
    }

    private List<Transform> GetLayersFromIndex(int startIndex) {
        List<Transform> layersToMove = new List<Transform>();
        for (int i = startIndex; i < levelLayers.Count; i++) {
            if (levelLayers[i] != null) {
                Debug.Log($"[ROOF MOVE] Can confirm, moving levelLayer from: {startIndex} to {levelLayers.Count}");
                layersToMove.Add(levelLayers[i].transform);
            }
        }
        return layersToMove;
    }

    private IEnumerator MoveLayersDown(List<Transform> layers, float? fixedDistance = null, float? duration = null, Transform referenceTransform = null, float speed = 5f) {
        isShiftingDown = true;

        if (fixedDistance.HasValue && duration.HasValue) { // Fixed-distance move using Lerp
            float elapsed = 0f;
            List<Vector3> startPositions = layers.Select(l => l.position).ToList();
            List<Vector3> endPositions = startPositions.Select(pos => pos - new Vector3(0, fixedDistance.Value, 0)).ToList();

            while (elapsed < duration.Value) {
                for (int i = 0; i < layers.Count; i++) {
                    layers[i].position = Vector3.Lerp(startPositions[i], endPositions[i], elapsed / duration.Value);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            for (int i = 0; i < layers.Count; i++) {
                layers[i].position = endPositions[i];
            }
        } 

        float moveDistance = 0f;
        if (referenceTransform != null && currentColumn == 0) { // Roof out of range scroll
            if (currentLevelData != null && currentLevelData.LevelRooms.Count > currentBoard) {
                // Expected roof Y for this board, using base formula + vertical offset
                float expectedRoofY = -4.5f + (10f * (currentBoard + 1f)) + currentLevelData.LevelRooms[currentBoard].y;
                moveDistance = currentLevelData.LevelRooms[currentBoard].y; // vertical offset
                if (moveDistance < 0f) {
                    moveDistance = Mathf.Abs(moveDistance); // ensure positive downward movement
                }
            } else {
                Debug.LogWarning("[ROOF MOVE] Cannot find current room's vertical offset. Defaulting to 0.");
                moveDistance = 0f;
            }

            Debug.Log($"[ROOF MOVE] Moving layers down by {moveDistance} (room vertical offset). Reference roof: {referenceTransform.name}");
            float totalMoved = 0f;

            while (totalMoved < moveDistance) {
                float step = speed * Time.deltaTime;
                step = Mathf.Min(step, moveDistance - totalMoved); // avoid overshoot
                foreach (Transform layer in layers) {
                    layer.position -= new Vector3(0, step, 0);
                }
                totalMoved += step;
                if (layers.Count > 0) {
                    Debug.Log($"[MOVE] Step {step}, top layer {layers[0].name} now at {layers[0].position.y}");
                }
                yield return null;
            }
            Debug.Log($"[MOVE] Completed move down by {moveDistance} units.");
        }
        isShiftingDown = false;
    }




    public IEnumerator BoardRemovalSequence() {
        Debug.Log("SIDE RESET: Starting 2nd board removal IEnum");
        StartCoroutine(RemoveCurrentBoard());
        yield return new WaitForSeconds(0.6f); // Allow movement to complete
        isRemovingBoard = false;
    }


    private IEnumerator RemoveCurrentBoard() {
        if (currentBoard < 0 || currentBoard >= levelLayers.Count) {
            Debug.LogWarning("Current board index out of bounds. Aborting removal.");
            yield break;
        }

        isShiftingDown = true;
        float yOffset = levelLayers[currentBoard].GetComponent<LocalRoomData>().localLevelData.y;

        // First, collect all indices to remove (current main room + its side rooms)
        List<int> indicesToRemove = new List<int>();
        indicesToRemove.Add(currentBoard);
        Debug.Log($"SIDE RESET: Adding main room {currentBoard} to remove list (z = {currentLevelData.LevelRooms[currentBoard].z})");

        // Find all side rooms that belong to this main room
        // Side rooms always come immediately after their main room
        int i = currentBoard + 1;
        Debug.Log($"SIDE RESET: Starting to check for side rooms from index {i}");
        while (i < levelLayers.Count && levelLayers[i].GetComponent<LocalRoomData>().localLevelData.z != 0) {
            // This is a side room connected to the current main room
            Debug.Log($"SIDE RESET: Adding side room {i} to remove list (z = {currentLevelData.LevelRooms[i].z})");
            indicesToRemove.Add(i);
            i++;
        }
        // Log if we stopped because we found a main room or reached the end
        if (i < levelLayers.Count) {
            Debug.Log($"SIDE RESET: Stopped at index {i} (found main room with z = {currentLevelData.LevelRooms[i].z})");
        } else {
            Debug.Log($"SIDE RESET: Stopped at end of level (reached index {i})");
        }
        // Cache transforms to move (start from the first index after the last room to be removed)
        int firstIndexToMove = indicesToRemove[indicesToRemove.Count - 1] + 1;
        List<Transform> layersToMove = GetLayersFromIndex(firstIndexToMove);

        // Store the current board before removal for reference
        int originalCurrentBoard = currentBoard;

        Debug.Log($"SIDE RESET: Removing {indicesToRemove.Count} rooms: main room at {currentBoard} + {indicesToRemove.Count - 1} side rooms");
        Debug.Log($"SIDE RESET: First index to move: {firstIndexToMove}");

        //if (currentColumn != 0) {
        if (levelLayers[currentBoard].GetComponent<LocalRoomData>().localLevelData.z != 0) {
            Debug.Log("[XTRANSITION] Resetting Ball POS");
            XTransition xTransition = levelLayers[currentBoard].GetComponentInChildren<XTransition>(true);
            if (xTransition != null) {
                Debug.Log("[XTRANSITION] Starting side room transition...");
                yield return StartCoroutine(xTransition.DoTransition());
                Debug.Log("[XTRANSITION] Transition complete.");
            } else {
                Debug.LogWarning("SIDE RESET: XTransition component not found in current side room");
            }
            currentColumn = 0;
        }
        // Remove rooms in reverse order to avoid index shifting issues
        Debug.Log("SIDE RESET: Removing rooms in reverse order:");
        for (int j = indicesToRemove.Count - 1; j >= 0; j--) {
            int indexToRemove = indicesToRemove[j];
            Debug.Log($"SIDE RESET: Removing room {indexToRemove}");
            if (indexToRemove < levelLayers.Count) {
                GameObject removedLayer = levelLayers[indexToRemove];
                levelLayers.RemoveAt(indexToRemove);
                numberOfBoards--;
                Destroy(removedLayer);
            } else {
                Debug.LogWarning($"SIDE RESET: Index {indexToRemove} is out of bounds (levelLayers count: {levelLayers.Count})");
            }
        }
        // Update board index
        if (currentBoard >= levelLayers.Count) {
            currentBoard = Mathf.Max(0, levelLayers.Count - 1);
            Debug.Log($"SIDE RESET: Updated currentBoard to {currentBoard}");
        }
        // Move layers down smoothly
        StartCoroutine(MoveLayersDown(layersToMove, fixedDistance: 10f + yOffset, duration: 0.5f));
        // Final checks
        if (currentBoard >= 0 && currentBoard < levelLayers.Count) {
            CountBricks();
        } else {
            hasLoadedNextLevel = true;
            LoadNextLevel();
        }
    }











    public void CountBricks() {
        if (levelLayers == null || levelLayers.Count == 0) {
            Debug.LogError("levelLayers is null or empty.");
            return;
        }

        if (currentBoard < 0) {
            Debug.LogError("currentBoard index is out of bounds: " + currentBoard);
            currentBoard = 0;
        }
        if (currentBoard >= levelLayers.Count) {
            Debug.LogError("currentBoard index is out of bounds: " + currentBoard);
            currentBoard = levelLayers.Count - 1;
        }

        GameObject currentLayer = levelLayers[currentBoard];
        if (currentLayer == null) {
            Debug.LogError("levelLayers[" + currentBoard + "] is null.");
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

    public void RemoveAndReturnToMostRecentCentralRoom() {
        if (currentBoard < 0 || currentBoard >= levelLayers.Count) return;

        isShiftingDown = true;

        Transform currentSideRoomXTrans = null;

        int mainRoomIndex = RoomHistory.ToArray()[RoomHistory.Count - 1];
        Debug.Log("Room to go to: " + mainRoomIndex);

        if (RoomHistory.Count > 1) {
            // Convert stack to array to access elements by index (bottom is at index 0)
            int[] roomArray = RoomHistory.ToArray();
            int secondFromBottomIndex = roomArray[RoomHistory.Count - 2]; // Second from bottom
            Debug.Log("Second from bottom index: " + secondFromBottomIndex);
            if (secondFromBottomIndex >= 0 && secondFromBottomIndex < levelLayers.Count) { // Get reference to the main room's Xtransition object
                currentSideRoomXTrans = levelLayers[secondFromBottomIndex].GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("XTransition"));
            }
        }

        if (currentSideRoomXTrans == null) {
            Debug.LogWarning("No XTransition found in current side room!");
            return;
        }

        GameObject mainRoomXTrans = currentSideRoomXTrans.GetComponent<XTransition>().partnerTransition;
        Debug.Log("Main room XTrans: " + mainRoomXTrans);

        // Pop from RoomHistory until only 1 remains
        while (RoomHistory.Count > 1) {
            int index = RoomHistory.Pop();
            Debug.Log("Room History index to remove: " + index);

            if (index >= 0 && index < levelLayers.Count) {
                GameObject removedLayer = levelLayers[index];
                levelLayers.RemoveAt(index);
                Destroy(removedLayer);
                numberOfBoards--;
            }
        }
                
        // If found, set currentBoard to that index; otherwise fallback to first board
        if (mainRoomIndex >= 0) {
            Debug.Log("Room to go to: " + mainRoomIndex);
            currentBoard = mainRoomIndex;
            if (mainRoomXTrans != null) {
                Destroy(mainRoomXTrans);
            }
        } else {
            Debug.Log("Room to go to: 0");
            currentBoard = 0;
        }

        currentColumn = 0;

        // Reset ball positions
        int ballCount = ActiveBalls.Count(ball => ball != null && ball.gameObject != null);
        float radius = 1.0f;
        Debug.Log("There are " + ballCount + " balls to relocate in a circle of radius: " + radius);
        for (int i = 0; i < ballCount; i++) {
            var ball = ActiveBalls[i];
            Debug.Log("Current ball: " + ball);
            if (ball != null && ball.gameObject != null) {
                // Calculate circular position
                float angleDeg = i * (360f / ballCount);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;

                Vector3 boardPosition = levelLayers[lastMainBoard].transform.position;
                ball.transform.position = new Vector3(boardPosition.x + offset.x, boardPosition.y + offset.y, boardPosition.z);

                BallMovement ballMovement = ball.GetComponent<BallMovement>();
                if (ballMovement != null) {
                    ballMovement.currentSpeed = 0.1f; // Set direction outward from center or upward
                    ballMovement.moveDir = new Vector2(offset.x, offset.y).normalized;
                }

                if (!ball.gameObject.activeSelf) {
                    Debug.Log("This ball isn't active");
                    ball.gameObject.SetActive(true);
                    Debug.Log("This ball is active");
                }
            }
        }
        // Reset brick container for the new board
        Debug.Log("Resetting brickContainer");
        brickContainer = null;
        CountBricks();
        StartCoroutine(ResetShiftingDownNextFrame());
    }

    private IEnumerator ResetShiftingDownNextFrame() {
        yield return 10; // wait 1 frame
        isShiftingDown = false;
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
        if (scale.x > 0.625f) {
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
        GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
        if (bricks.Length == 0) return;
        List<GameObject> availableBricks = new List<GameObject>(bricks);

        int strikes = Mathf.Min(3, availableBricks.Count);
        for (int x = 0; x < strikes; x++) {
            int index = UnityEngine.Random.Range(0, availableBricks.Count);
            if (availableBricks[index].GetComponent<ObjHealth>() != null) {
                // Lightning Strike animation
                availableBricks[index].GetComponent<ObjHealth>().TakeDamage((int)availableBricks[index].GetComponent<ObjHealth>().health, 1, 0);
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
        foreach (var ball in GameManager.ActiveBalls) {
            if (ball != null) {
                var ballMovement = ball.GetComponent<BallMovement>();
                if (ballMovement != null && ballMovement.isStuckToPaddle) {
                    grabbed = true;
                    ballMovement.isStuckToPaddle = false;
                }
            }
        }
        float direction = grabbed ? UnityEngine.Random.Range(-1f, 1f) : 1f;
        GameObject newBall = Instantiate(ballPrefab, new Vector2(GameManager.ActiveBalls[0].transform.position.x + 0.5f, GameManager.ActiveBalls[0].transform.position.y), Quaternion.identity);
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
        foreach (var ball in GameManager.ActiveBalls) {
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

        if (scoreText == null)
            scoreText = FindObjectOfType<TMP_Text>();

        if (scoreText != null)
            scoreText.text = CurrentScore.ToString();
    }

    private void HandleScore() {
        if (CurrentScore > 0 && CurrentScore >= targetScore && !CanSpawnBall && brickCount > 0 && brickCount < initialBrickCount && !IsGameStart) { // Ball Spawn conditions
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
            SceneManager.LoadScene(nextScene);
            numberOfBoards = GameObject.Find("LevelManager").GetComponent<LevelBuilder>().boardStats.LevelRooms.Count;
        }
        /*else if (numberOfBoards == 1)
        {
            ceilinigDestroyed = true;
            if (CeilingDestroy.Instance != null) {
                CeilingDestroy.Instance.StartDestroyCeiling(blankTransitionTime);
            } else {
                Debug.LogWarning("CeilingManager.Instance is null. Cannot destroy ceiling.");
            }
        }*/

    }


    /*private IEnumerator DestroyCeiling(float waitTime) {
        yield return new WaitForSeconds(waitTime);
        GameObject barrier = GameObject.Find("Ceiling");
        if (barrier != null)
            Destroy(barrier);
    }*/
    #endregion



    #region Scene Management
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        initialBrickCount = 0;
        brickCount = 0;
        hasLoadedNextLevel = false;
        brickContainer = null;
        scoreText = null;
        RebuildLevelLayers();
        currentBoard = 0;
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
        //PaddleSizeMod = paddleSizeMod;
        GravityBall = gravityBall;
        currentboard = currentBoard;
        LevelLayers = levelLayers;
        brickcontainer = brickContainer;
        shiftingDown = isShiftingDown;
        currentcolumn = currentColumn;
        lastmainmoard = lastMainBoard;
        roomHistory = RoomHistory;
    }

    public void debugOptions() {
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

        /*if (Input.GetKeyDown(KeyCode.V)) {
            // Spawn random power-up at paddle position
            GameObject paddle = GameObject.Find("Paddle");
            if (paddle != null) {
                float randomOdds = Random.Range(0f, 1f);
                PowerUpSpawn(randomOdds, new Vector3(paddle.transform.position.x, paddle.transform.position.y + 5f, 0f));
                Debug.Log("Spawned random power-up above paddle");
            }
        }*/

        // Game Flow Debugging
        if (Input.GetKeyDown(KeyCode.G)) {
            IsGameStart = !IsGameStart;
            Debug.Log($"Game Start: {IsGameStart}");
        }

        if (Input.GetKeyDown(KeyCode.H)) {
            // Force board transition
            if (levelLayers.Count > 0 && currentBoard < levelLayers.Count) {
                levelLayers[currentBoard].GetComponent<LocalRoomData>().numberOfBricks = 0;
                Debug.Log("Force cleared bricks for transition");
            }
        }

        if (Input.GetKeyDown(KeyCode.Y)) {
            XTransition[] xTransitions = levelLayers[currentBoard].GetComponentsInChildren<XTransition>(true);
            if (xTransitions.Length == 0) {
                Debug.LogWarning("No XTransition components found in current room");
                return;
            }

            XTransition leftTransition = null;
            foreach (XTransition xTransition in xTransitions) {
                if (currentColumn == 0) {
                    if (xTransition != null && xTransition.transition == -1) {
                        leftTransition = xTransition;
                        break;
                    }
                } else if (currentColumn == 1) {
                    if (xTransition != null && xTransition.transition == 0) {
                        leftTransition = xTransition;
                        break;
                    }
                }
            }

            if (leftTransition != null) {
                Debug.Log("[XTRANSITION] Starting Left Hand Side Room transition...");
                ActiveBalls[0].transform.position = leftTransition.transform.position;
            } else {
                Debug.LogWarning("No XTransition with transition value -1 found in current room");
            }
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            XTransition[] xTransitions = levelLayers[currentBoard].GetComponentsInChildren<XTransition>(true);
            if (xTransitions.Length == 0) {
                Debug.LogWarning("No XTransition components found in current room");
                return;
            }

            XTransition rightTransition = null;
            foreach (XTransition xTransition in xTransitions) {
                if (currentColumn == 0) {
                    if (xTransition != null && xTransition.transition == 1) {
                        rightTransition = xTransition;
                        break;
                    }
                } else if (currentColumn == -1) {
                    if (xTransition != null && xTransition.transition == 0) {
                        rightTransition = xTransition;
                        break;
                    }
                }
            }

            if (rightTransition != null) {
                Debug.Log("[XTRANSITION] Starting Left Hand Side Room transition...");
                ActiveBalls[0].transform.position = rightTransition.transform.position;
            } else {
                Debug.LogWarning("No XTransition with transition value -1 found in current room");
            }
        }

        if (Input.GetKeyDown(KeyCode.J) && currentBoard < levelLayers.Count - 1) {
            if (currentColumn == 0) {
                PaddleMove paddleMove = GameObject.Find("Paddle").GetComponent<PaddleMove>();
                ActiveBalls[0].transform.position = new Vector3(ActiveBalls[0].transform.position.x, paddleMove.baseYPos + 11f, 0f);
            }
        }

        if (Input.GetKeyDown(KeyCode.K) && currentBoard > 0) {
            if (currentColumn == 0) {
                PaddleMove paddleMove = GameObject.Find("Paddle").GetComponent<PaddleMove>();
                ActiveBalls[0].transform.position = new Vector3(ActiveBalls[0].transform.position.x, paddleMove.baseYPos - 2f, 0f);
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

        // Information Display
        if (Input.GetKeyDown(KeyCode.I)) {
            Debug.Log("=== GAME STATE INFO ===");
            Debug.Log($"Current Board: {currentBoard}/{levelLayers.Count - 1}");
            Debug.Log($"Bricks: {brickCount}/{initialBrickCount}");
            Debug.Log($"Active Balls: {ActiveBalls.Count}");
            Debug.Log($"Score: {CurrentScore} (Multiplier: x{scoreMult})");
            Debug.Log($"Next Scene: {nextScene}");
            Debug.Log($"Is Shifting Down: {isShiftingDown}");
            Debug.Log($"Current Column: {currentColumn}");
        }
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
    #endregion
}