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
    public float decellerate = 3f; // Amount to decelerate ball(s) by when magnetizing them

    [Header("Power Ups")]
    public bool lazerPaddle = false;
    public GameObject lazerPaddleProjectile;
    public int bulletCount = 3;
    public bool grabPaddle = false;
    public bool anyBallStuck = false;

    [HideInInspector]
    public bool isTransitioning = false;

    void Start() {
        GameManager.IsGameStart = true;
        // Initializing GameManager values in case they aren't already
        if (GameManager.DisableTimer == 0) GameManager.DisableTimer = disableMovement;
        if (GameManager.MagnetOffset == 0) GameManager.MagnetOffset = magnetOffset;
        if (GameManager.DecelerationRate == 0) GameManager.DecelerationRate = decellerate;
    }
    
    void Awake() {
        currentYPos = baseYPos;
        maxFlipHeight = baseYPos + 1.0f;
        GameManager.IsGameStart = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    void Update() {
        if (!isTransitioning) {
            HandleInput();
            PaddleMovement();
            flip();
            sizeUpdate();
        }
        //InputCheck();
    }


    #region Controller Management
    public void InputCheck() {
        for (int i = 0; i <= 19; i++) {
            if (Input.GetKeyDown((KeyCode)(330 + i))) {
                Debug.Log("Joystick Button Pressed: " + i);
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

        movementSpeedMod();

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
    
    public void movementSpeedMod() {
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
    #endregion

    #region Movement
    void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.CompareTag("Lazer")) { // Disable on hit
            disableMovement = col.gameObject.GetComponent<Lazer>().disableTime;
        }
    }

    void PaddleMovement() {
        if (disableMovement > 0) {
            disableMovement -= Time.deltaTime;
            return;
        }

        if (GameManager.levelLayers == null || GameManager.currentBoardRow < 0 || GameManager.currentBoardRow >= GameManager.levelLayers.GetLength(0) || GameManager.currentBoardColumn < 0 || GameManager.currentBoardColumn >= GameManager.levelLayers.GetLength(1)) {
            return;
        }
        GameObject currentLayer = GameManager.levelLayers[GameManager.currentBoardRow, GameManager.currentBoardColumn];
        if (currentLayer == null) return;

        // Find the actual wall positions in the scene (accounting for stretching)
        Transform leftWall = null;
        Transform rightWall = null;

        foreach (Transform child in currentLayer.transform) {
            if (child.name.StartsWith("LWall")) {
                leftWall = child;
            } else if (child.name.StartsWith("RWall")) {
                rightWall = child;
            }
        }

        float leftBoundary, rightBoundary;

        if (leftWall != null && rightWall != null) { // Calculate boundaries: wall position + half wall width + half paddle width
            float leftWallHalfWidth = leftWall.localScale.x / 2f;
            float rightWallHalfWidth = rightWall.localScale.x / 2f;
            float paddleHalfWidth = transform.localScale.x / 2f;

            leftBoundary = leftWall.position.x + leftWallHalfWidth + paddleHalfWidth;
            rightBoundary = rightWall.position.x - rightWallHalfWidth - paddleHalfWidth;
        } else { // Fallback: use camera edges
            float roomCenterX = currentLayer.transform.position.x;
            float camHalfHeight = Camera.main.orthographicSize;
            float camHalfWidth = camHalfHeight * Camera.main.aspect;
            float paddleHalfWidth = transform.localScale.x / 2f;

            leftBoundary = roomCenterX - camHalfWidth + paddleHalfWidth;
            rightBoundary = roomCenterX + camHalfWidth - paddleHalfWidth;
        }

        // Paddle scale modifier (keep your existing logic)
        float paddleScale = transform.localScale.x;
        if (paddleScale < 0.5f) modf = 0.01f; // 0.4938271f
        else if (paddleScale < 0.8f) modf = 0.05f; // 0.740740740...f
        else if (paddleScale < 1.2f) modf = 0.1f; // 1.111...f
        else if (paddleScale < 1.7f) modf = 0.2f; // 1.6666...f
        else if (paddleScale < 2.51f) modf = 0.3f;    // Base, 2.5f
        else if (paddleScale < 3.751f) modf = 0.45f; // 3.75f
        else if (paddleScale < 5.6251f) modf = 0.65f; // 5.625f
        else if (paddleScale < 8.4385f) modf = 1.0f; // 8.4375f
        else if (paddleScale < 12.6563f) modf = 1.59f;// 12.65625f

        float targetX = transform.position.x;
        switch (controlType) {
            case ControlType.Mouse:
                mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePosition.z = 0f;
                targetX = Mathf.Clamp(mousePosition.x, leftBoundary - modf, rightBoundary + modf);
                break;

            case ControlType.Keyboard:
            case ControlType.Gamepad:
                moveInput = Input.GetAxisRaw("Horizontal");
                float moveSpeed = 10f;
                float newX = transform.position.x + moveInput * moveSpeed * mod * Time.deltaTime;
                // Use the same clamping logic as mouse movement with modf adjustment
                targetX = Mathf.Clamp(newX, leftBoundary - modf, rightBoundary + modf);
                break;
        }

        transform.position = new Vector3(targetX, currentYPos, 0f);

        if (Input.GetKeyDown(KeyCode.Space)) {
            Debug.Log($"[XBOUNDRY] TargetX: {targetX}, LeftBound: {leftBoundary}, RightBound: {rightBoundary}");
            if (leftWall != null && rightWall != null) {
                Debug.Log($"[WALLS] LeftWallPos: {leftWall.position.x}, LeftWallWidth: {leftWall.localScale.x}");
                Debug.Log($"[WALLS] RightWallPos: {rightWall.position.x}, RightWallWidth: {rightWall.localScale.x}");
            }
        }
    }
    #endregion

    #region Paddle Abilities
    void MagnetPull() { // Magnet pull
        if (GameManager.IsGameStart || GameManager.ActiveBalls == null) return; // Don't bother conditions

        foreach (GameObject ball in GameManager.ActiveBalls) { // Iterate through balls
            if (ball == null) continue;
            BallMovement ballMovement = ball.GetComponent<BallMovement>();
            if (ballMovement == null /*|| ballMovement.isStuckToPaddle*/) continue;

            float range = Mathf.Clamp01(1f - (Mathf.Abs(ball.transform.position.x - transform.position.x) / magnetOffset)); // Check if given ball is within magnet range
            bool isWithinOffset = range > 0f;

            if (isWithinOffset) { // Actually activate the magnet effect
                if (ballMovement.isStuckToPaddle || (ballMovement.stickTarget != null && ballMovement.stickTarget != gameObject)) {
                    ballMovement.ReleaseFromStick();
                }
                if (ballMovement.moveDir.y > 0) {
                    if (ballMovement.currentSpeed > 13.5f) {
                        ballMovement.currentSpeed -= (decellerate/3);
                    } else {
                        ballMovement.currentSpeed -= decellerate;
                    }   
                } else if (ballMovement.moveDir.y < 0) {
                    ballMovement.currentSpeed += (decellerate/3);
                } else {
                    ballMovement.moveDir.y = -1;
                }
            }
        }
    }

    void flip() {
        float targetHeight = flipping ? maxFlipHeight : baseYPos;
        currentYPos = Mathf.MoveTowards(currentYPos, targetHeight, flipSpeed * Time.deltaTime);
    }
    #endregion

    #region Power Ups
    void sizeUpdate() {
        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) return;

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
            float leftWallHalfWidth = leftWall.localScale.x / 2f;
            float rightWallHalfWidth = rightWall.localScale.x / 2f;
            float playableWidth = Mathf.Abs((rightWall.position.x - rightWallHalfWidth) - (leftWall.position.x + leftWallHalfWidth));
            Vector3 scale = transform.localScale;
            while (scale.x > playableWidth - 0.5f && scale.x > 0.4938271f) {
                scale.x /= 1.5f;
                if (scale.x < 0.4938271f) {
                    scale.x = 0.4938271f;
                    break;
                }
            }
            transform.localScale = scale;
        }
    }

    void firePaddleLaser() {
        if (bulletCount > 0) {
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x - transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x + transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            bulletCount-=2;
        }
    }
    #endregion

    #region Gizmos
    void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        DrawMagnetZone();
        DrawEffectLines();
    }

    void DrawMagnetZone() {
        Vector3 paddlePos = transform.position;
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);

        // Draw vertical rectangle edges
        Vector3 topLeft = paddlePos + new Vector3(-magnetOffset, 10f, 0);
        Vector3 topRight = paddlePos + new Vector3(magnetOffset, 10f, 0);
        Vector3 bottomLeft = paddlePos + new Vector3(-magnetOffset, 0, 0);
        Vector3 bottomRight = paddlePos + new Vector3(magnetOffset, 0, 0);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(bottomLeft, bottomRight);
    }

    void DrawEffectLines() {
        if (GameManager.ActiveBalls == null) return;

        foreach (GameObject ball in GameManager.ActiveBalls) {
            if (ball == null) continue;

            if (ball.transform.position.y > transform.position.y) {
                float horizontalDistance = Mathf.Abs(ball.transform.position.x - transform.position.x);

                if (horizontalDistance < magnetOffset) {
                    float range = Mathf.Clamp01(1f - (horizontalDistance / magnetOffset));
                    Gizmos.color = Color.Lerp(Color.yellow, Color.red, range);

                    Gizmos.DrawLine(ball.transform.position, transform.position);
                }
            }
        }
    }
    #endregion
}