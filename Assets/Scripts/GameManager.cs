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
    public static bool IsGameStart;
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
    //public int paddleSizeMod = 0;
    public int PaddleSizeMod = 1;
    public static bool BrickThu = false;
    public static bool FireBall = false;
    public static bool GravityBall = false;
    public List<PowerUpData> powerUps; // Populate in Inspector

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

        if (brickCount <= 0 && !hasLoadedNextLevel && !IsGameStart) {
            LoadNextLevel();
        }

        
        if (scoreMult != 1 && ActiveBalls.Count > 0 && !IsGameStart) {
            timer -= (1 * Time.deltaTime);
            //Debug.Log("x2 timer: " + timer);
        }
        
        if (timer <= 0) {
            scoreMult = 1;
            timer = 10;
        }

        BrickThu = brickThu;
        FireBall = fireBall;
        //PaddleSizeMod = paddleSizeMod;
        GravityBall = gravityBall;

        /*if (Input.GetButtonDown("Jump")) {
            expandPaddle();
        }
        if (Input.GetButtonDown("Fire3")) {
            shrinkPaddle();
        }*/
    }

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
        if (scale.x > 0.76f) {
            scale.x /= PaddleSizeMod;
            paddle.transform.localScale = scale;
        }

    }

    public void superShrinkPaddle() {
        GameObject paddle = GameObject.Find("Paddle"); 
        paddle.transform.localScale = new Vector3(0.76f, 1.8f, 0.54922f);
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
                availableBricks[index].GetComponent<ObjHealth>().TakeDamage((int)availableBricks[index].GetComponent<ObjHealth>().health, 1);
            }
            availableBricks.RemoveAt(index);
        }
    }

    public void PowerUpSpawn(float odds, Vector3 spawnPos) {
        float cumulative = 0f;
        foreach (PowerUpData powerUp in powerUps) {

            cumulative += powerUp.dropRate;

            if (odds < cumulative) {
                Instantiate(powerUp.prefab, spawnPos, Quaternion.identity);
                Debug.Log($"Spawned PowerUp: {powerUp.itemName}");
                return;
            }
        }
        Debug.Log("No Power-up Spawned.");
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
    public void CountBricks() {
        if (brickContainer == null) {
            brickContainer = GameObject.Find("BlockList");
        }

        brickCount = 0;

        if (brickContainer == null) { // Fallback in case no container is found
            GameObject[] bricks = GameObject.FindGameObjectsWithTag("Brick");
            foreach (GameObject brick in bricks) {
                if (brick.activeInHierarchy && !brick.GetComponent<ObjHealth>().Invincibility) {
                    brickCount++;
                }
            }
            
            initialBrickCount = (initialBrickCount ==0) ? brickCount : initialBrickCount;  // Set initial count
            return;
        }

        foreach (Transform child in brickContainer.transform) { // Count active bricks in container
            if (child.gameObject.activeInHierarchy && !child.GetComponent<ObjHealth>().Invincibility) {
                brickCount++;
            }
        }

        if (initialBrickCount == 0) { // Only set initial count if this is the first count
            initialBrickCount = brickCount;
        }
    }

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


    #region Powerups

    public void powerUpSpawn() {

    }

    #endregion

    #region Debug
    private void UpdateDebugValues() {
        publicBrickCount = brickCount;
        publicNextScene = nextScene;
    }
    #endregion
}