using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
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

    /*[Header("Circular Paddle")]
    public bool circularPaddle = false;
    public float orbitRadiusX = 7f;   // Horizontal radius
    public float orbitRadiusY = 4f;   // Vertical radius
    public float orbitSpeed = 90f;    // Degrees per second
    private float currentAngle = 0f;  // In degrees

    [Header("Circular Flip")]
    public float circularFlipRadiusIncrease = 1.5f; // How much the radius increases during flip
    public float circularFlipRecoilSpeed = 8f;      // Speed to return to normal radius
    private float currentOrbitRadiusX;              // Current dynamic radius
    private float currentOrbitRadiusY;              // Current dynamic radius
    private bool wasCircularPaddle = false;         // Track previous state
    */
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
        maxFlipHeight = baseYPos + 0.5f;
        GameManager.IsGameStart = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    void Update() {
        /*if ((int)GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.z == 5) {
            circularPaddle = true;
        } else {
            circularPaddle = false;
        }*/

        if (!isTransitioning) {
            HandleInput();
            sizeUpdate();

            /*if (circularPaddle) {
                CircularMovement();

                CircularFlip(); // Add circular flip handling
            } else {*/
            PaddleMovement();
            //if (!GameManager.IsGameStart) {
            flip();
            //}
        }
        InputCheck();
    }


    #region Controller Management
    public void InputCheck() {
        for (int i = 0; i <= 20; i++) {
            if (Input.GetKeyDown((KeyCode)(330 + i))) {
                Debug.Log("Joystick Button Pressed: " + i);
            }
        }

        // Also check axis-based D-pad input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal > 0.5f) Debug.Log("D-pad Right (Axis)");
        if (horizontal < -0.5f) Debug.Log("D-pad Left (Axis)");
        if (vertical > 0.5f) Debug.Log("D-pad Up (Axis)");
        if (vertical < -0.5f) Debug.Log("D-pad Down (Axis)");
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
          JoystickButton12 = Home Button
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
                /*if (circularPaddle) {
                    Debug.Log("[CIRCULAR PADDLE] Circular Magnet Pull");
                } else {*/
                    MagnetPull();
                //}
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

        transform.rotation = Quaternion.identity;
        transform.position = new Vector3(transform.position.x, baseYPos, 0f);
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

            /*case ControlType.Keyboard:
            case ControlType.Gamepad:
                // Get stick input
                float stickInput = Input.GetAxisRaw("Horizontal");

                // Get D-pad input (using axes)
                float dpadInput = Input.GetAxisRaw("DPadX");

                // Prioritize D-pad if actively used, otherwise use stick
                moveInput = (Mathf.Abs(dpadInput) > 0.1f) ? dpadInput : stickInput;
                moveInput = Mathf.Clamp(moveInput, -1f, 1f);

                float moveSpeed = 10f;
                float newX = transform.position.x + moveInput * moveSpeed * mod * Time.deltaTime;
                targetX = Mathf.Clamp(newX, leftBoundary - modf, rightBoundary + modf);
                break;*/

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


    /*void CircularMovement() {
        if (disableMovement > 0) {
            disableMovement -= Time.deltaTime;
            return;
        }

        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) return;
        Vector2 roomCenter = new Vector2(currentLayer.transform.position.x, currentLayer.transform.position.y + 0.5f);

        // Handle input
        float input = 0f;
        switch (controlType) {
            case ControlType.Mouse:
                Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 dir = (mouseWorld - (Vector3)roomCenter).normalized;
                currentAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                break;

            case ControlType.Keyboard:
            case ControlType.Gamepad:
                input = Input.GetAxisRaw("Horizontal");
                currentAngle += input * orbitSpeed * Time.deltaTime;
                break;
        }

        // Convert angle to oval coordinates
        float rad = currentAngle * Mathf.Deg2Rad;
        float x = roomCenter.x + Mathf.Cos(rad) * orbitRadiusX;
        float y = roomCenter.y + Mathf.Sin(rad) * orbitRadiusY;

        transform.position = new Vector3(x, y, 0f);

        Vector2 lookDir = roomCenter - (Vector2)transform.position;
        float angleToCenter = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angleToCenter - 90f);
    }


    void CircularFlip() {
        // Handle circular flip radius changes
        if (flipping) {
            // Increase radius during flip
            currentOrbitRadiusX = Mathf.MoveTowards(currentOrbitRadiusX, orbitRadiusX + circularFlipRadiusIncrease, flipSpeed * Time.deltaTime);
            currentOrbitRadiusY = Mathf.MoveTowards(currentOrbitRadiusY, orbitRadiusY + circularFlipRadiusIncrease, flipSpeed * Time.deltaTime);
        } else {
            // Return to normal radius
            currentOrbitRadiusX = Mathf.MoveTowards(currentOrbitRadiusX, orbitRadiusX, circularFlipRecoilSpeed * Time.deltaTime);
            currentOrbitRadiusY = Mathf.MoveTowards(currentOrbitRadiusY, orbitRadiusY, circularFlipRecoilSpeed * Time.deltaTime);
        }
    }*/



    #endregion

    #region Paddle Abilities
 void MagnetPull() {
    if (GameManager.IsGameStart || GameManager.ActiveBalls == null) return;

    foreach (GameObject ball in GameManager.ActiveBalls) {
        if (ball == null) continue;
        BallMovement ballMovement = ball.GetComponent<BallMovement>();
        if (ballMovement == null) continue;

        float range = Mathf.Clamp01(1f - (Mathf.Abs(ball.transform.position.x - transform.position.x) / magnetOffset));
        bool isWithinOffset = range > 0f;

        if (isWithinOffset) {
            /*Collider2D ballCollider = ball.GetComponent<Collider2D>();
            if (ballCollider == null) continue; // Use continue instead of return!
            
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true; // This is the important line!
            filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
            filter.useLayerMask = true;

            Collider2D[] overlappingColliders = new Collider2D[10];
            int count = Physics2D.OverlapCollider(ballCollider, filter, overlappingColliders);


            Debug.Log($"Found {count} overlapping colliders for ball {ball.name}");

            bool foundPlayerCollider = false;
            for (int i = 0; i < count; i++) {
                if (overlappingColliders[i] != null) {
                    Debug.Log($"Collider {i}: {overlappingColliders[i].name} with tag: {overlappingColliders[i].tag}");
                    if (overlappingColliders[i].CompareTag("Player")) {
                        Debug.Log("WORKED!!!!!!!!!!! Found Player tag!");
                        foundPlayerCollider = true;
                        break;
                    }
                }
            }

            if (foundPlayerCollider) {
                continue; // Skip to next ball
            }*/

            if (ballMovement.curPaddleMagnetVolume != null) {
                    continue;
                }
                Debug.Log("[Magnet Check] Main Paddle Worked");

                // Rest of magnet logic
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