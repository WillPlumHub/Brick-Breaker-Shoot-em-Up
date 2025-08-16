using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour {

    public static GameManager Instance { get; private set; } // Singleton instance

    [Header("Game state")]
    public int score;
    public TMP_Text scoreText;
    public static bool IsGameStart;
    public static int CurrentScore;
    public int targetScore = CurrentScore + 50;
    public static int scoreMult = 1;
    public float timer = 10; 
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
        levelLayers.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
    }
    #endregion

    private void Start() {
        ceilinigDestroyed = false;
        InitializeGame();
    }

    private void Update() {
        UpdateDebugValues();
        UpdateGameState();
        HandleScore();
        CheckGameState();
        CountBricks();
        if (!isShiftingDown && !isRemovingBoard)
        {
            transitionCheck();
        }

        if (scoreMult != 1 && ActiveBalls.Count > 0 && !IsGameStart) {
            timer -= (1 * Time.deltaTime);
            //Debug.Log("x2 timer: " + timer);
        }
        
        if (timer <= 0) {
            scoreMult = 1;
            timer = 10;
        }
    }





    public void transitionCheck() {
        if (!hasLoadedNextLevel && !IsGameStart && !isRemovingBoard /*&& Input.GetButtonDown("Jump")*/) {
            if (currentBoard < 0 || currentBoard >= levelLayers.Count || isShiftingDown) {
                Debug.LogWarning("Current board does not exist or is shifting. Aborting board logic.");
                return;
            }

            GameObject currentLayer = levelLayers[currentBoard];
            Transform roofTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));
            if (roofTransform == null) return;

            float heightLimit = -4.5f + (10f * (currentBoard + 1f));
            float roofDelta = roofTransform.position.y - heightLimit;

            // Scroll roof if needed
            if (roofDelta > 0.01f && !isShiftingDown) {
                List<Transform> layersToMove = GetLayersFromIndex(currentBoard);
                StartCoroutine(MoveLayersDown(layersToMove, referenceTransform: roofTransform, targetY: heightLimit, speed: moveSpeed));
            }

            // Brick-less transition ONLY when bricks are gone AND scrolling is done AND roof is at target
            if (brickCount <= 0) {
                if (roofDelta <= 0.01f && !isShiftingDown) {
                    if (levelLayers.Count > 1 && currentBoard < levelLayers.Count - 1 && currentLevelData.LevelRooms[currentBoard].z == 0) {
                        isRemovingBoard = true;
                        StartCoroutine(BoardRemovalSequence());
                    }
                    else if (levelLayers.Count > 1 && currentBoard < levelLayers.Count - 1 && currentLevelData.LevelRooms[currentBoard].z != 0) {
                        currentColumn = 0;
                        Debug.Log("WWWWORKED: " + currentColumn);
                    } else {
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
                layersToMove.Add(levelLayers[i].transform);
            }
        }
        return layersToMove;
    }

    private IEnumerator MoveLayersDown(List<Transform> layers, float? fixedDistance = null, float? duration = null, Transform referenceTransform = null, float? targetY = null, float speed = 5f) {

        isShiftingDown = true;

        if (fixedDistance.HasValue && duration.HasValue) {
            // Fixed-distance move using Lerp
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
        } else if (referenceTransform != null && targetY.HasValue) {
            // Move until reference transform reaches targetY (previously MoveLayersToTargetY)
            while (referenceTransform.position.y > targetY.Value) {
                float step = speed * Time.deltaTime;
                foreach (Transform layer in layers) {
                    layer.position -= new Vector3(0, step, 0);
                }
                yield return null;
            }
            float overshoot = referenceTransform.position.y - targetY.Value;
            foreach (Transform layer in layers) {
                layer.position -= new Vector3(0, overshoot, 0);
            }
        }
        isShiftingDown = false;
    }

    public IEnumerator BoardRemovalSequence() {
        RemoveCurrentBoard();
        yield return new WaitForSeconds(0.6f); // Allow movement to complete
        isRemovingBoard = false;
    }

    private void RemoveCurrentBoard() {
        if (currentBoard < 0 || currentBoard >= levelLayers.Count) {
            Debug.LogWarning("Current board index out of bounds. Aborting removal.");
            return;
        }

        isShiftingDown = true;

        // Cache transforms to move
        List<Transform> layersToMove = GetLayersFromIndex(currentBoard + 1);

        Debug.Log("WORKED" + currentColumn);
        if (currentColumn != 0)
        {
            Debug.Log("Resetting Ball POS");
            foreach (var ball in GameManager.ActiveBalls)
            {
                if (ball != null)
                {
                    ball.transform.position = Vector3.zero;
                }
            }
        }

        // Remove and destroy current board
        GameObject removedLayer = levelLayers[currentBoard];
        levelLayers.RemoveAt(currentBoard);
        numberOfBoards--;
        Destroy(removedLayer);

        // Update board index
        if (currentBoard >= levelLayers.Count) {
            currentBoard = Mathf.Max(0, levelLayers.Count - 1);
        }

        // Move layers down smoothly
        StartCoroutine(MoveLayersDown(layersToMove, fixedDistance: 10f, duration: 0.5f));

        

        // Final checks
        if (currentBoard >= 0 && currentBoard < levelLayers.Count) {
            CountBricks();
        } else {
            hasLoadedNextLevel = true;
            LoadNextLevel();
        }
        //isShiftingDown = false;
    }











    public void CountBricks()
    {
        if (levelLayers == null || levelLayers.Count == 0)
        {
            Debug.LogError("levelLayers is null or empty.");
            return;
        }

        if (currentBoard < 0)
        {
            Debug.LogError("currentBoard index is out of bounds: " + currentBoard);
            currentBoard = 0;
        }
        if (currentBoard >= levelLayers.Count)
        {
            Debug.LogError("currentBoard index is out of bounds: " + currentBoard);
            currentBoard = levelLayers.Count - 1;
        }

        GameObject currentLayer = levelLayers[currentBoard];
        if (currentLayer == null)
        {
            Debug.LogError("levelLayers[" + currentBoard + "] is null.");
            return;
        }

        if (brickContainer == null)
        {
            Transform blockListTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("BlockList"));

            if (blockListTransform != null)
            {
                brickContainer = blockListTransform.gameObject;
            }
            else
            {
                Debug.LogWarning("BlockList not found under " + currentLayer.name);
            }
        }

        brickCount = 0;
        if (brickContainer == null)
        {
            GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
            foreach (GameObject brick in bricks)
            {
                ObjHealth health = brick.GetComponent<ObjHealth>();
                if (brick.activeInHierarchy && health != null && !health.Invincibility)
                {
                    brickCount++;
                }
            }
            if (initialBrickCount == 0)
            {
                initialBrickCount = brickCount;
            }
            return;
        }

        foreach (Transform child in brickContainer.transform)
        {
            ObjHealth health = child.GetComponent<ObjHealth>();
            if (child.gameObject.activeInHierarchy && health != null && !health.Invincibility)
            {
                brickCount++;
            }
        }

        if (initialBrickCount == 0)
        {
            initialBrickCount = brickCount;
        }

        // If side room has no bricks left, remove it and return to center
        // If side room has no bricks left, remove it and return to center
        if (brickCount <= 0 && currentcolumn != 0)
        {
            Debug.Log("Side room cleared. Returning to central room and removing this board.");
            RemoveAndReturnToMostRecentCentralRoom();
        }

    }

    private void RemoveAndReturnToMostRecentCentralRoom()
    {
        if (currentBoard < 0 || currentBoard >= levelLayers.Count)
            return;

        // Remove current side room
        GameObject removedLayer = levelLayers[currentBoard];
        levelLayers.RemoveAt(currentBoard);
        Destroy(removedLayer);
        numberOfBoards--;

        // Search backward for the most recent central board (z != 0)
        int centralIndex = -1;
        for (int i = currentBoard - 1; i >= 0; i--)
        {
            Debug.Log("Returning to central board. Int i = " + i);
            if (currentLevelData.LevelRooms[i].z == 0)
            {
                centralIndex = i;
                break;
            }
        }

        // If found, set currentBoard to that index; otherwise fallback to first board
        if (centralIndex >= 0)
            currentBoard = centralIndex;
        else
            currentBoard = Mathf.Max(0, levelLayers.Count - 1);

        currentColumn = 0;

        // Reset ball positions
        foreach (var ball in ActiveBalls)
        {
            if (ball != null)
                ball.transform.position = Vector3.zero;
        }

        // Reset brick container for the new board
        brickContainer = null;
        CountBricks();
    }





    #region PowerUps
    public void expandPaddle() {
        GameObject paddle = GameObject.Find("Paddle");        
        Vector3 scale = paddle.transform.localScale;
        if (scale.x < 18.4f) {
            scale.x *= PaddleSizeMod;
            paddle.transform.localScale = scale;
        }
    }

    public void shrinkPaddle() {
        GameObject paddle = GameObject.Find("Paddle");
        Vector3 scale = paddle.transform.localScale;
        if (scale.x > 0.625f) {
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
            int index = Random.Range(0, availableBricks.Count);
            if (availableBricks[index].GetComponent<ObjHealth>() != null) {
                // Lightning Strike animation
                availableBricks[index].GetComponent<ObjHealth>().TakeDamage((int)availableBricks[index].GetComponent<ObjHealth>().health, 1, 0);
            }
            availableBricks.RemoveAt(index);
        }
    }

    public void PowerUpSpawn(float odds, Vector3 spawnPos) {
        float cumulative = 0f;
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

        float direction = grabbed ? Random.Range(-1f, 1f) : 1f;
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

            Vector2 direction = grabbed ? new Vector2(Random.Range(-1f, 1f), 1f).normalized : offset.normalized;

            GameObject newBall;

            if (i == 0) {
                // Use the original ball
                newBall = ActiveBalls[0];
                newBall.transform.position = spawnPos;
            } else {
                // Instantiate new balls
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
        publicBrickCount = brickCount;
        LevelLayers = levelLayers;
        brickcontainer = brickContainer;
        shiftingDown = isShiftingDown;
        currentcolumn = currentColumn;
    }
    #endregion
}