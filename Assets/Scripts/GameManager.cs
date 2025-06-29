using System.Collections;
using System.Collections.Generic;
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
    public static bool IsGameStarted;
    public static int CurrentScore;
    public static float StageSpeed;
    public int targetScore = CurrentScore + 50;
    public static int scoreMult = 1;
    public float timer = 10; 
    [SerializeField] public static bool ceilinigDestroyed = false;
    
    [Header("Paddle")]
    public static float DisableTimer;
    public static float MagnetOffset;
    public static float DecelerationRate;

    [Header("Balls")]
    public GameObject ballPrefab;
    public static float BallSpeed = 3f;
    public static bool CanSpawnBall = false;
    public static List<GameObject> ActiveBalls = new List<GameObject>(); //balls
    [SerializeField] private List<GameObject> ballsInEditor; //_balls

    [Header("Bricks")]
    public GameObject brickContainer;
    public static int brickCount;
    public int initialBrickCount;
    public int publicBrickCount = 0; // For debugging in Inspector

    [Header("Level")]
    public static string nextScene;
    public string publicNextScene; // For debugging in Inspector
    public float blankTransitionTime;
    public bool hasLoadedNextLevel;

    [Header("Power Ups")]
    public bool brickThu = false;
    public bool fireBall = false;
    public bool superShrink = false;
    public bool gravityBall = false;
    public int paddleSizeMod = 0;
    public static int PaddleSizeMod = 1;
    public static bool BrickThu = false;
    public static bool FireBall = false;
    public static bool SuperShrink = false;
    public static bool GravityBall = false;

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    public AudioClip backgroundMusic;
    public AudioClip ballBounceSound;
    public AudioClip paddleBounceSound;
    public AudioClip perfectFlipSound;
    public AudioClip flipSound;

    private void Awake() {
        HandleSingleton();
        InitializeAudio();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start() {
        ceilinigDestroyed = false;
        InitializeGame();
        StartCoroutine(CountBricksAfterLoad());
    }

    private void Update() {
        //Debug.Log("Balls: " + ActiveBalls.Count);
        UpdateDebugValues();
        UpdateGameState();
        HandleScore();
        CheckGameState();

        if (brickCount <= 0 && !hasLoadedNextLevel && !IsGameStarted) {
            LoadNextLevel();
        }

        
        if (scoreMult != 1 && ActiveBalls.Count > 0 && !IsGameStarted) {
            timer -= (1 * Time.deltaTime);
            Debug.Log("x2 timer: " + timer);
        }
        
        if (timer <= 0) {
            scoreMult = 1;
            timer = 10;
        }

        BrickThu = brickThu;
        FireBall = fireBall;
        SuperShrink = superShrink;
        PaddleSizeMod = paddleSizeMod;
        GravityBall = gravityBall;

        if (Input.GetButtonDown("Jump")) {
            SpawnEightBall();
        }
    }

    public void expandPaddle() {
        Debug.Log("EXPANDED");
        GameObject paddle = GameObject.Find("Paddle");
        Vector3 scale = paddle.transform.localScale;
        scale.x *= PaddleSizeMod;
        paddle.transform.localScale = scale;
        //paddle.GetComponent<PaddleMove>().XBoundry -= 0.85f;
    }

    public void shrinkPaddle() {
        GameObject paddle = GameObject.Find("Paddle");
    }

    public void superShrinkPaddle() {
        scoreMult = 2;
        GameObject paddle = GameObject.Find("Paddle"); 
        paddle.transform.localScale = new Vector3(0.76f, 1.8f, 0.54922f);
    }

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
        GameObject newBall = Instantiate(ballPrefab, Vector3.zero, Quaternion.identity);
        newBall.GetComponent<BallMovement>().InitializeBall(new Vector3(Random.Range(-5f, 5f), 1, 0));
    }

    public void SpawnEightBall() {
        Vector2 centerPos = ActiveBalls[0].transform.position;

        for (int i = 0; i < 8; i++) {
            float angleDeg = i * 45f; // 360° / 8 = 45°
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector2 offset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * 1;
            Vector2 spawnPos = centerPos + offset;
            Vector2 direction = offset.normalized;

            GameObject newBall;

            if (i == 0) { // Use the original ball
                newBall = ActiveBalls[0];
                newBall.transform.position = spawnPos;
            } else {
                // Instantiate new balls
                newBall = Instantiate(ballPrefab, spawnPos, Quaternion.identity);
            }

            newBall.GetComponent<BallMovement>().moveDir = direction;
            newBall.GetComponent<BallMovement>().currentSpeed = 5;
            newBall.GetComponent<BallMovement>().ceilingBreak = ActiveBalls[0].GetComponent<BallMovement>().ceilingBreak;
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
    public void CountBricks() {
        if (brickContainer == null)
            brickContainer = GameObject.Find("BlockList");

        // Fallback: Find bricks by tag if container is missing
        if (brickContainer == null) {
            GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
            brickCount = bricks.Length;
            initialBrickCount = brickCount;  // Set initial count
            //Debug.Log($"Counted {brickCount} bricks by tag (no container found).");
            return;
        }

        // Count active bricks in container
        brickCount = 0;
        foreach (Transform child in brickContainer.transform)
            if (child.gameObject.activeInHierarchy)
                brickCount++;

        // Only set initial count if this is the first count
        if (initialBrickCount == 0) {
            initialBrickCount = brickCount;
        }

        //Debug.Log($"Initial bricks: {initialBrickCount} | Current: {brickCount}");
    }

    private void UpdateGameState() {
        ballsInEditor = new List<GameObject>(ActiveBalls);

        if (scoreText == null)
            scoreText = FindObjectOfType<TMP_Text>();

        if (scoreText != null)
            scoreText.text = CurrentScore.ToString();
    }

    private void HandleScore() {
        if (CurrentScore > 0 && CurrentScore >= targetScore && !CanSpawnBall && brickCount > 0 && brickCount < initialBrickCount && !IsGameStarted) { // Ball Spawn conditions
            SpawnBall();
            CanSpawnBall = true;
            targetScore += 50;
        }
    }

    private void CheckGameState() {
        if (ActiveBalls.Count <= 0 && !IsGameStarted) {
            GameOver();
        }
    }

    public void GameOver() {
        Debug.Log("GAME OVER: " + ActiveBalls.Count);
    }

    public void LoadNextLevel() {
        if (!string.IsNullOrEmpty(nextScene)) {
            SceneManager.LoadScene(nextScene);
        } else {
            ceilinigDestroyed = true;
            StartCoroutine(DestroyCeiling(blankTransitionTime));
        }
    }

    private IEnumerator DestroyCeiling(float waitTime) {
        yield return new WaitForSeconds(waitTime);
        GameObject barrier = GameObject.Find("Ceiling");
        if (barrier != null)
            Destroy(barrier);
    }
    #endregion



    #region Scene Management
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        initialBrickCount = 0;
        brickCount = 0;
        hasLoadedNextLevel = false;
        brickContainer = null;
        scoreText = null;
        //ActiveBalls = new List<GameObject>();
        StartCoroutine(CountBricksAfterLoad());
    }

    private IEnumerator CountBricksAfterLoad() {
        yield return null; // Wait one frame
        CountBricks();
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
    }
    #endregion
}