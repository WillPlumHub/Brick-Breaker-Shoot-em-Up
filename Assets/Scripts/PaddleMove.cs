using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class PaddleMove : MonoBehaviour {

    public enum ControlType { Mouse, Keyboard, Gamepad }
    [Header("Control Settings")]
    private ControlType controlType = ControlType.Mouse;
    private float lastInputCheckTime = 0f;
    private float inputCheckCooldown = 0.1f;
    private float moveInput = 0f;

    [Header("Movement")]
    public float mmmmmmmmmmmmmmmmmmmmmmmmmmmm;
    public float modf;
    public float mod = 1;
    public float modDefault = 1;
    public float baseYPos = -4f; // Resting position
    public float currentYPos;   // Animated Y position
    public float disableMovement = 0f; // Time left disabled when hit
    public float XBoundry = 7.6f;
    private Vector3 mousePosition = new Vector3(0, 0, 0);

    [Header("Flip")]
    public bool flipping = false; 
    public float flipSpeed = 20f;
    public float maxFlipHeight = -3.5f; // Max extent paddle will move up to when flipping
    public float recoilSpeed;
    public AnimationCurve recoilCurve;
    private float normalizedHeight;
    GameManager gameManager;

    [Header("Magnet Pull")]
    public float magnetOffset = 3f; // X range of magnet ability
    public float decellerate = 0.7f; // Amount to decelerate ball(s) by when magnetizing them

    [Header("Power Ups")]
    public bool lazerPaddle = false;
    public GameObject lazerPaddleProjectile;
    public int bulletCount = 3;
    public bool grabPaddle = false;
    public bool anyBallStuck = false;
        
    void Start() {
        GameManager.IsGameStart = true;

        // Initializing GameManager values in case they aren't already
        if (GameManager.DisableTimer == 0) GameManager.DisableTimer = disableMovement;
        if (GameManager.MagnetOffset == 0) GameManager.MagnetOffset = magnetOffset;
        if (GameManager.DecelerationRate == 0) GameManager.DecelerationRate = decellerate;
    }
    
    void Awake() {
        currentYPos = baseYPos;
        maxFlipHeight = baseYPos + 0.5f;
        GameManager.IsGameStart = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();

        UpdateXBoundary();
    }

    void Update() {
        HandleInput();
        PaddleMovement();
        flip();
        variableUpdate();
        sizeUpdate();

        for (int i = 0; i <= 19; i++) {
            if (Input.GetKeyDown((KeyCode)(330 + i))) {
                Debug.Log("Joystick Button Pressed: " + i);
            }
        }
    }

    void sizeUpdate() {
        if (GameManager.levelLayers != null && GameManager.currentBoard < GameManager.levelLayers.Count) {
            GameObject currentLayer = GameManager.levelLayers[GameManager.currentBoard];

            Transform leftWall = null;
            Transform rightWall = null;

            foreach (Transform child in currentLayer.transform) {
                if (child.name.StartsWith("LWall")) {
                    leftWall = child;
                } else if (child.name.StartsWith("RWall")) {
                    rightWall = child;
                }
            }

            if (leftWall != null && rightWall != null) {
                float widthBetweenWalls = Mathf.Abs(rightWall.position.x - leftWall.position.x);
                Vector3 scale = transform.localScale;
                if (transform.localScale.x > widthBetweenWalls) {
                    Debug.Log("Shrinking");
                    scale.x /= 2;
                    transform.localScale = scale;
                }
            }
        }
    }

    void DetectControlType() {
        if (Time.time - lastInputCheckTime < inputCheckCooldown) return;

        if (Input.GetMouseButtonDown(0) || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) {
            controlType = ControlType.Mouse;
        } else if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0 || Input.GetButton("Jump")) {
            if (Input.GetJoystickNames().Length > 0) {
                controlType = ControlType.Gamepad;
            } else {
                controlType = ControlType.Keyboard;
            }
        }
        lastInputCheckTime = Time.time;
    }

    void HandleInput() {
        DetectControlType();

        // --- Mouse Flip ---
        bool flipDown = Input.GetMouseButtonDown(0);
        bool flipUp = Input.GetMouseButtonUp(0);
        bool flipHeld = Input.GetMouseButton(0);
        // --- Keyboard Flip ---
        flipDown |= Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        flipUp |= Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
        flipHeld |= Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        // --- Gamepad Flip (Cross, Circle, L2) ---
        flipDown |= Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton6);
        flipUp |= Input.GetKeyUp(KeyCode.JoystickButton2) || Input.GetKeyUp(KeyCode.JoystickButton1) || Input.GetKeyUp(KeyCode.JoystickButton6);
        flipHeld |= Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.JoystickButton6);

        // --- Mouse Magnet ---
        bool magnetDown = Input.GetMouseButtonDown(1);
        // --- Keyboard Magnet ---
        magnetDown |= Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        // --- Gamepad Magnet (Square, Triangle, R2) ---
        magnetDown |= Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.JoystickButton7);

        movementMod();

        /*JoystickButton0 = △
          JoystickButton1 = ⭘
          JoystickButton2 = ✕
          JoystickButton3 = ⬜
          JoystickButton4 = L1
          JoystickButton5 = R1
          JoystickButton6 = L2
          JoystickButton7 = R2
          JoystickButton8 = Select
          JoystickButton9 = Start
          JoystickButton10 = L3
          JoystickButton11 = R3
        */


        if (flipDown && GameManager.IsGameStart) { // Start game on first click
            GameManager.IsGameStart = false;
            return;
        } else if (!GameManager.IsGameStart) {
            if (!lazerPaddle) {
                //anyBallStuck = false;

                foreach (var ball in GameManager.ActiveBalls) { // Check if any balls are stuck
                    if (ball != null && ball.GetComponent<BallMovement>().isStuckToPaddle) {
                        anyBallStuck = true;
                        //return;
                        break;
                    }
                }
                if (!anyBallStuck) {
                    if (flipDown) {
                        gameManager.PlaySFX(gameManager.flipSound);
                        flipping = true;
                    } else if (flipUp) {
                        flipping = false;
                    }

                    if (flipHeld && transform.position.y < maxFlipHeight) {
                        float currentY = transform.position.y;
                        normalizedHeight = Mathf.InverseLerp(baseYPos, maxFlipHeight, currentY);
                        recoilSpeed = recoilCurve.Evaluate(normalizedHeight);
                    } else {
                        recoilSpeed = 5;
                    }
                } else {
                    if (flipDown) {
                        anyBallStuck = false;
                    }
                }
            } else {
                if (flipDown) { // W/ lazer paddle, fire lazer projectile
                    firePaddleLaser();
                }
            }
            if (magnetDown) {
                MagnetPull();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.CompareTag("Lazer")) { // Disable on hit
            disableMovement = col.gameObject.GetComponent<Lazer>().disableTime;
        }
    }

    public void movementMod() {
        bool increasePressed = Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.LeftShift);
        bool decreasePressed = Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.LeftControl);

        if (increasePressed) {
            if (modDefault > 1f) { // Wrap 1.8 → 0
                mod = (modDefault >= 1.8f) ? 1f : mod + 1.8f;
            } else {
                mod = Mathf.Min(mod + 1.8f, 1.8f);
            }
        } else if (decreasePressed) {
            if (modDefault == 0.3f) { // Wrap 0.3 → 2
                mod = (modDefault <= 0.3f) ? 1f : mod - 1.7f;
            } else {
                mod = Mathf.Max(mod - 1.7f, 0.3f);
            }
        } else { // Neither button pressed — reset to default
            mod = modDefault;
        }
    }

  
            
                                


    void MagnetPull() { // Magnet pull
        if (GameManager.IsGameStart || GameManager.ActiveBalls == null) return; // Don't bother conditions

        foreach (GameObject ball in GameManager.ActiveBalls) { // Iterate through balls
            if (ball == null) continue;
            BallMovement ballMovement = ball.GetComponent<BallMovement>();
            if (ballMovement == null /*|| ballMovement.isStuckToPaddle*/) continue;

            float range = Mathf.Clamp01(1f - (Mathf.Abs(ball.transform.position.x - transform.position.x) / magnetOffset)); // Check if given ball is within magnet range
            bool isWithinOffset = range > 0f;


            /*if (ballMovement.isStuckToPaddle) {
                //Debug.Log("Skipping stuck ball: " + ball.name);
                continue;
            }*/

            //Debug.Log("Applying magnet to: " + ball.name);

            
            if (isWithinOffset) { // Actually activate the magnet effect
                if (ballMovement.isStuckToPaddle || (ballMovement.stickTarget != null && ballMovement.stickTarget != gameObject)) {
                    ballMovement.ReleaseFromStick();
                }
                if (ballMovement.moveDir.y == 1) {
                    ballMovement.currentSpeed -= decellerate;
                } else if (ballMovement.moveDir.y == -1) {
                    ballMovement.currentSpeed += decellerate;
                } else {
                    ballMovement.moveDir.y = -1;
                }
            }
        }
    }

    /*void PaddleMovement() {
        UpdateXBoundary(); // Get accurate boundary based on room and paddle size
        float targetX = transform.position.x;
        float paddleWidth = transform.localScale.x;

        if (disableMovement > 0) {
            disableMovement -= Time.deltaTime;
            return;
        }

        switch (controlType) {
            case ControlType.Mouse:
                mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePosition.z = Camera.main.transform.position.z + Camera.main.nearClipPlane;
                targetX = Mathf.Clamp(mousePosition.x, -XBoundry + (paddleWidth / 2), XBoundry - (paddleWidth / 2));
                break;
            case ControlType.Keyboard:
            case ControlType.Gamepad:
                moveInput = Input.GetAxisRaw("Horizontal");
                float moveSpeed = 10f;
                targetX += moveInput * (moveSpeed * mod) * Time.deltaTime;
                targetX = Mathf.Clamp(targetX, -XBoundry + (paddleWidth / 2), XBoundry - (paddleWidth / 2));
                break;
        }
        transform.position = new Vector3(targetX, currentYPos, 0f);
    }*/

    /*void UpdateXBoundary() {
        if (gameManager.currentLevelData != null && gameManager.currentLevelData.LevelRooms.Count > GameManager.currentBoard) {
            float mod = 0;

            if (transform.localScale.x < 1.12f) { // Update bounds based on size
                mod = 0.9698f;
                GameManager.scoreMult = 2;
            } else if (transform.localScale.x < 1.666668f) {
                mod = 0.986f;
            } else if (transform.localScale.x < 2.51f) { // Base size
                mod = 1;
            } else if (transform.localScale.x < 3.751f) {
                mod = 1.024f;
            } else if (transform.localScale.x < 5.6251f) {
                mod = 1.05222f;
            } else if (transform.localScale.x < 8.43751f) {
                mod = 1.10164f;
            } else if (transform.localScale.x < 12.656251f) {
                mod = 1.17456f;
            } else if (transform.localScale.x < 18.984381f) {
                mod = 1.211566f;
            }
            
            float roomHalfWidth = gameManager.currentLevelData.LevelRooms[GameManager.currentBoard].x + (7.6f * mod);
            Debug.Log("Size is: " + mod + ", Limit: " + (roomHalfWidth - (transform.localScale.x / 2)));

            float paddleHalfWidth = transform.localScale.x / 2f;

            XBoundry = roomHalfWidth;

            //Debug.Log($"[Paddle] Room Half Width: {roomHalfWidth}, Paddle Half Width: {paddleHalfWidth}, XBoundry: {XBoundry}");
        } else {
            Debug.LogWarning("[Paddle] Invalid room index or missing LevelData.");
        }
    }*/


    /*void UpdateXBoundary()
    {
        if (GameManager.levelLayers != null && GameManager.currentBoard < GameManager.levelLayers.Count)
        {
            GameObject currentLayer = GameManager.levelLayers[GameManager.currentBoard];

            Transform leftWall = null;
            Transform rightWall = null;

            foreach (Transform child in currentLayer.transform)
            {
                if (child.name.StartsWith("LWall"))
                {
                    leftWall = child;
                }
                else if (child.name.StartsWith("RWall"))
                {
                    rightWall = child;
                }
            }

            if (leftWall != null && rightWall != null)
            {
                float widthBetweenWalls = (Mathf.Abs(rightWall.position.x - leftWall.position.x) / 2f) + mmmmmmmmmmmmmmmmmmmmmmmmmmmm;

                // Apply size-based mod like before
                //modf = 1f;
                float paddleScale = transform.localScale.x;

                if (paddleScale < 1.12f) modf = 0.9698f;
                else if (paddleScale < 1.666668f) modf = 0.986f;
                else if (paddleScale < 2.51f) modf = 1f;
                else if (paddleScale < 3.751f) modf = 1.02f;
                else if (paddleScale < 5.6251f) modf = 1.045f;/*
                else if (paddleScale < 8.43751f) modf = 1.10164f;
                else if (paddleScale < 12.656251f) modf = 1.17456f;
                else if (paddleScale < 18.984381f) modf = 1.211566f;

                float adjustedHalfWidth = widthBetweenWalls * modf;
                XBoundry = adjustedHalfWidth;

                Debug.Log($"[Paddle] Width between walls: {widthBetweenWalls}, Mod: {modf}, XBoundry: {XBoundry}");
            }
            else
            {
                Debug.LogWarning("[Paddle] Could not find L Wall and/or R Wall in current level layer.");
            }
        }
        else
        {
            Debug.LogWarning("[Paddle] Invalid levelLayers reference or board index.");
        }
    }*/

    void UpdateXBoundary()
    {
        if (GameManager.levelLayers != null && GameManager.currentBoard < GameManager.levelLayers.Count)
        {
            GameObject currentLayer = GameManager.levelLayers[GameManager.currentBoard];

            // Get current column from GameManager (assuming you've added this)
            int currentColumn = GameManager.currentColumn;
            float columnOffset = currentColumn * 23.6f; // Same offset used in LevelBuilder

            Transform leftWall = null;
            Transform rightWall = null;

            foreach (Transform child in currentLayer.transform)
            {
                if (child.name.StartsWith("LWall"))
                {
                    leftWall = child;
                }
                else if (child.name.StartsWith("RWall"))
                {
                    rightWall = child;
                }
            }

            if (leftWall != null && rightWall != null)
            {
                // Adjust wall positions by column offset
                float leftWallPos = leftWall.position.x + columnOffset;
                float rightWallPos = rightWall.position.x + columnOffset;

                float widthBetweenWalls = (Mathf.Abs(rightWallPos - leftWallPos) / 2f) + mmmmmmmmmmmmmmmmmmmmmmmmmmmm;

                // Apply size-based mod
                float paddleScale = transform.localScale.x;
                if (paddleScale < 1.12f) modf = 0.9698f;
                else if (paddleScale < 1.666668f) modf = 0.986f;
                else if (paddleScale < 2.51f) modf = 1f;
                else if (paddleScale < 3.751f) modf = 1.02f;
                else if (paddleScale < 5.6251f) modf = 1.045f;

                float adjustedHalfWidth = widthBetweenWalls * modf;
                XBoundry = adjustedHalfWidth;

                //Debug.Log($"[Paddle] Column: {currentColumn}, Width between walls: {widthBetweenWalls}, XBoundry: {XBoundry}");
            }
            else
            {
                Debug.LogWarning("[Paddle] Could not find walls in current level layer.");
            }
        }
        else
        {
            Debug.LogWarning("[Paddle] Invalid levelLayers reference or board index.");
        }
    }

    void PaddleMovement()
    {
        UpdateXBoundary();
        float targetX = transform.position.x;
        float paddleWidth = transform.localScale.x;

        if (disableMovement > 0)
        {
            disableMovement -= Time.deltaTime;
            return;
        }

        switch (controlType)
        {
            case ControlType.Mouse:
                mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePosition.z = Camera.main.transform.position.z + Camera.main.nearClipPlane;
                // Apply column offset to mouse position
                float columnOffset = GameManager.currentColumn * 23.6f;
                float mouseX = mousePosition.x - columnOffset;
                targetX = Mathf.Clamp(mouseX, -XBoundry + (paddleWidth / 2), XBoundry - (paddleWidth / 2));
                break;

            case ControlType.Keyboard:
            case ControlType.Gamepad:
                moveInput = Input.GetAxisRaw("Horizontal");
                float moveSpeed = 10f;
                targetX += moveInput * (moveSpeed * mod) * Time.deltaTime;
                targetX = Mathf.Clamp(targetX, -XBoundry + (paddleWidth / 2), XBoundry - (paddleWidth / 2));
                break;
        }

        // Apply column offset to final position
        float finalX = targetX + (GameManager.currentColumn * 23.6f);
        transform.position = new Vector3(finalX, currentYPos, 0f);
    }




    void variableUpdate() {
        magnetOffset = GameManager.MagnetOffset;
        decellerate = GameManager.DecelerationRate;
    }

    void flip() {
        float targetHeight = flipping ? maxFlipHeight : baseYPos;
        currentYPos = Mathf.MoveTowards(currentYPos, targetHeight, flipSpeed * Time.deltaTime);
    }

    void firePaddleLaser() {
        if (bulletCount > 0) {
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x - transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x + transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            bulletCount-=2;
        }
    }
}