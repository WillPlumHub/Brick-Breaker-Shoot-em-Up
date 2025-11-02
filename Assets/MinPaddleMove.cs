using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinPaddleMove : MonoBehaviour {
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
    public float baseYPos; // Resting position
    public float currentFlipHeight;   // Current flip height in local up direction
    public float disableMovement = 0f; // Time left disabled when hit
    public float xBoundary = 3f; // Custom X movement limit
    private Vector3 mousePosition = new Vector3(0, 0, 0);

    [Header("Flip")]
    public bool flipping = false;
    public float flipSpeed = 20f;
    public float maxFlipHeightLocal = 0.5f; // Flip height in local up direction
    public float recoilSpeed;
    public AnimationCurve recoilCurve;
    private float normalizedHeight;
    GameManager gameManager;

    [Header("Magnet Pull")]
    public float magnetOffset = 3f; // X range of magnet ability
    public float decelerate = 3f; // Amount to decelerate ball(s) by when magnetizing them

    [Header("Power Ups")]
    public bool lazerPaddle = false;
    public GameObject lazerPaddleProjectile;
    public int bulletCount = 3;
    public bool grabPaddle = false;
    public bool anyBallStuck = false;

    [Header("Custom Settings")]
    public Transform customBoundaryReference; // Optional: use another object as boundary reference
    public float customXLimit = 3f; // Custom X movement limit

    [HideInInspector]
    public bool isTransitioning = false;

    private Vector3 originalLocalPosition;
    private Transform parentTransform;
    private Vector3 basePosition; // World space base position
    
    // Track balls in magnet range
    public List<GameObject> ballsInMagnetRange = new List<GameObject>();

    void Start() {
        // Store original local position and parent
        originalLocalPosition = transform.localPosition;
        parentTransform = transform.parent;

        // Initialize mini-paddle specific values
        xBoundary = customXLimit;
        currentFlipHeight = 0f;

        // Calculate base position in world space
        basePosition = transform.position;

        GameManager.IsGameStart = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    void Awake() {
        currentFlipHeight = 0f;
        basePosition = transform.position;
        GameManager.IsGameStart = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }

    void Update() {
        if (!isTransitioning) {
            if (disableMovement > 0) {
                disableMovement -= Time.deltaTime;
                flipping = false;
            }

            HandleInput();
            if (customXLimit > 1) {
                PaddleMovement();
            }
            //if (disableMovement > 0) {
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
        if (disableMovement > 0) {
            flipping = false; // Ensure flipping is disabled
            return;
        }

        DetectControlType();

        // Flip input detection
        bool flipDown = Input.GetMouseButtonDown(0);
        bool flipUp = Input.GetMouseButtonUp(0);
        bool flipHeld = Input.GetMouseButton(0);

        flipDown |= Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        flipUp |= Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
        flipHeld |= Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

        flipDown |= Input.GetKeyDown(KeyCode.JoystickButton2) || Input.GetKeyDown(KeyCode.JoystickButton1) || Input.GetKeyDown(KeyCode.JoystickButton6);
        flipUp |= Input.GetKeyUp(KeyCode.JoystickButton2) || Input.GetKeyUp(KeyCode.JoystickButton1) || Input.GetKeyUp(KeyCode.JoystickButton6);
        flipHeld |= Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.JoystickButton1) || Input.GetKey(KeyCode.JoystickButton6);

        // Magnet input detection
        bool magnetDown = Input.GetMouseButtonDown(1);
        magnetDown |= Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        magnetDown |= Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton3) || Input.GetKeyDown(KeyCode.JoystickButton7);

        movementSpeedMod();

        if (!GameManager.IsGameStart) {
            if (!lazerPaddle) {
                anyBallStuck = false;

                foreach (var ball in GameManager.ActiveBalls) {
                    if (ball != null && ball.GetComponent<BallMovement>().isStuckToPaddle) {
                        anyBallStuck = true;
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

                    // Calculate current flip progress in local up direction
                    float currentProgress = Mathf.InverseLerp(0f, maxFlipHeightLocal, currentFlipHeight);
                    recoilSpeed = recoilCurve.Evaluate(currentProgress);
                } else {
                    if (flipDown) {
                        anyBallStuck = false;
                    }
                }
            } else {
                if (flipDown) {
                    firePaddleLaser();
                }
            }
            //BallMovement ballMovement = col.gameObject.GetComponent<BallMovement>();
            if (magnetDown && ballsInMagnetRange.Count > 0) {
                Debug.Log("[Magnet Check] Got the ball");
                MagnetPull();
            }
        }
    }

    public void movementSpeedMod() {
        bool increasePressed = Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.LeftShift);
        bool decreasePressed = Input.GetKey(KeyCode.JoystickButton5) || Input.GetKey(KeyCode.LeftControl);

        if (increasePressed) {
            if (modDefault > 1f) {
                mod = (modDefault >= 1.8f) ? 1f : mod + 1.8f;
            } else {
                mod = Mathf.Min(mod + 1.8f, 1.8f);
            }
        } else if (decreasePressed) {
            if (modDefault == 0.3f) {
                mod = (modDefault <= 0.3f) ? 1f : mod - 1.7f;
            } else {
                mod = Mathf.Max(mod - 1.7f, 0.3f);
            }
        } else {
            mod = modDefault;
        }
    }
    #endregion

    #region Movement
    void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.CompareTag("Lazer")) {
            disableMovement = col.gameObject.GetComponent<Lazer>().disableTime;
            flipping = false;
        }
        
        if (col.CompareTag("Ball")) {
            if (!ballsInMagnetRange.Contains(col.gameObject)) {
                ballsInMagnetRange.Add(col.gameObject);
            }
            Debug.Log($"[Magnet Dir] Ball entered trigger: {col.gameObject.name}");
        }
    }
    void OnTriggerExit2D(Collider2D col) {
        if (col.CompareTag("Ball")) {
            if (ballsInMagnetRange.Contains(col.gameObject)) {
                ballsInMagnetRange.Remove(col.gameObject);
            }
            Debug.Log($"Ball exited trigger: {col.gameObject.name}.");
        }
    }

    /*void PaddleMovement() {
        
        
        
                    Vector3 localMouse = transform.InverseTransformPoint(mouseWorld);

                    // Simple local X movement with boundaries
                    localPos.x = Mathf.Clamp(localMouse.x, -customXLimit - modf, customXLimit + modf);
                }
                break;

            case ControlType.Keyboard:
            case ControlType.Gamepad: {
                    moveInput = Input.GetAxisRaw("Horizontal");
                    localPos.x += moveInput * moveSpeed * mod * Time.deltaTime;
                    localPos.x = Mathf.Clamp(localPos.x, -customXLimit - modf, customXLimit + modf);
                }
                break;
        }

        transform.localPosition = localPos;
        basePosition = transform.position;
    }*/

    void PaddleMovement() {
        if (disableMovement > 0) {
            //disableMovement -= Time.deltaTime;
            return;
        }

        Vector3 localPos = transform.localPosition;
        CalculateModf();
        float moveSpeed = 10f;

        switch (controlType) {
            case ControlType.Mouse: {
                    // Convert mouse world position to local space of paddle's parent
                    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector3 localMouse = parentTransform != null
                        ? parentTransform.InverseTransformPoint(mouseWorld)
                        : mouseWorld;

                    // Project mouse position onto paddle's local right axis
                    Vector3 localRight = transform.right; // This is the paddle's local right direction
                    Vector3 toMouseLocal = localMouse - originalLocalPosition;

                    // Calculate how far along the paddle's right axis the mouse is
                    float mouseDistanceAlongRight = Vector3.Dot(toMouseLocal, localRight);

                    // Clamp the distance from original position
                    float clampedDistance = Mathf.Clamp(mouseDistanceAlongRight, -customXLimit, customXLimit);

                    // Move to clamped position relative to original position
                    localPos = originalLocalPosition + localRight * clampedDistance;
                }
                break;

            case ControlType.Keyboard:
            case ControlType.Gamepad: {
                    moveInput = Input.GetAxisRaw("Horizontal");

                    // Move along the paddle's local right axis
                    Vector3 localRight = transform.right;
                    localPos += localRight * moveInput * moveSpeed * mod * Time.deltaTime;

                    // Calculate current distance from original position along the right axis
                    Vector3 displacementFromOriginal = localPos - originalLocalPosition;
                    float currentDistance = Vector3.Dot(displacementFromOriginal, localRight);

                    // Clamp the distance from original position
                    float clampedDistance = Mathf.Clamp(currentDistance, -customXLimit - modf, customXLimit + modf);

                    // Apply clamped position relative to original position
                    localPos = originalLocalPosition + localRight * clampedDistance;
                }
                break;
        }

        transform.localPosition = localPos; // Apply local position back (respects rotation)
        basePosition = transform.position; // Update base position for flip calcs
    }

    private void CalculateModf() {
        float paddleScale = transform.localScale.x;
        if (paddleScale < 0.5f) modf = 0.01f;
        else if (paddleScale < 0.8f) modf = 0.05f;
        else if (paddleScale < 1.2f) modf = 0.1f;
        else if (paddleScale < 1.7f) modf = 0.2f;
        else if (paddleScale < 2.51f) modf = 0.3f;
        else if (paddleScale < 3.751f) modf = 0.45f;
        else if (paddleScale < 5.6251f) modf = 0.65f;
        else if (paddleScale < 8.4385f) modf = 1.0f;
        else if (paddleScale < 12.6563f) modf = 1.59f;
    }
    #endregion

    #region Paddle Abilities
    void MagnetPull() {
        if (GameManager.IsGameStart || GameManager.ActiveBalls == null) return;

        foreach (GameObject ball in GameManager.ActiveBalls) {
            if (ball == null) continue;
            BallMovement ballMovement = ball.GetComponent<BallMovement>();
            if (ballMovement == null) continue;

            float horizontalDistance = Mathf.Abs(ball.transform.position.x - transform.position.x);
            float range = Mathf.Clamp01(1f - (horizontalDistance / magnetOffset));
            bool isWithinOffset = range > 0f;

            if (isWithinOffset) {

                if (ballMovement.curPaddleMagnetVolume == null || ballMovement.curPaddleMagnetVolume.gameObject != gameObject) {
                    continue;
                }
                Debug.Log($"[Magnet Check] Mini Paddle {gameObject.name} Worked");

                if (ballMovement.isStuckToPaddle || (ballMovement.stickTarget != null && ballMovement.stickTarget != gameObject)) {
                    ballMovement.ReleaseFromStick();
                }

                // Calculate direction from ball to paddle
                Vector3 directionToPaddle = new Vector3((transform.position - ball.transform.position).x, -1, 0);
                Debug.Log("[Magnet Check] direction to paddle: " + directionToPaddle);

                // Apply magnet effect based on ball's current state
                if (ballMovement.moveDir.y > 0) {
                    // Ball moving upward - gradually pull toward paddle
                    /*Vector3 newDirection = Vector3.Lerp(ballMovement.moveDir.normalized, new Vector3(directionToPaddle.x, -1, 0), 0.3f * Time.deltaTime);
                    ballMovement.moveDir = newDirection.normalized * ballMovement.moveDir.magnitude;*/

                    if (ballMovement.currentSpeed > 13.5f) {
                        ballMovement.currentSpeed -= (decelerate / 3);
                    } else {
                        ballMovement.currentSpeed -= decelerate;
                        Debug.Log($"[Magnet Check] Mini Paddle, {gameObject.name}, pulled ball, {ball.name} from: {ball.GetComponent<BallMovement>().currentSpeed + decelerate} to: {ball.GetComponent<BallMovement>().currentSpeed}");
                    }
                } else if (ballMovement.moveDir.y < 0) {
                    // Ball moving downward - strongly pull toward paddle
                    //ballMovement.moveDir = directionToPaddle * ballMovement.currentSpeed;
                    ballMovement.currentSpeed += (decelerate / 3);
                } else {
                    // Ball has no vertical movement - set direction toward paddle
                    //ballMovement.moveDir = directionToPaddle * ballMovement.currentSpeed;
                }

                if (transform.rotation.z < 0) {
                    ballMovement.moveDir.x -= 0.2f;
                    Debug.Log("[Magnet Check] Rotation < 0, pulling to positive");
                } else if (transform.rotation.z > 0) {
                    ballMovement.moveDir.x += 0.2f;
                    Debug.Log("[Magnet Check] Rotation > 0, pulling to negative");
                }

                // Ensure ball doesn't get stuck by maintaining minimum speed
                //ballMovement.currentSpeed = Mathf.Max(ballMovement.currentSpeed, 2f);
            }
        }
    }

    void flip() {
        float targetFlipHeight = flipping ? maxFlipHeightLocal : 0f;
        currentFlipHeight = Mathf.MoveTowards(currentFlipHeight, targetFlipHeight, flipSpeed * Time.deltaTime);

        // Calculate the offset in local up direction
        Vector3 localOffset = transform.up * currentFlipHeight;

        // Apply the offset to the base position
        transform.position = basePosition + localOffset;
    }
    #endregion

    #region Power Ups
    void firePaddleLaser() {
        if (bulletCount > 0) {
            // Calculate fire positions relative to paddle's current rotation
            Vector3 localLeft = new Vector3(-transform.localScale.x / 3, 0.13f, 0);
            Vector3 localRight = new Vector3(transform.localScale.x / 3, 0.13f, 0);

            Vector3 worldLeft = transform.TransformPoint(localLeft);
            Vector3 worldRight = transform.TransformPoint(localRight);

            Instantiate(lazerPaddleProjectile, worldLeft, transform.rotation);
            Instantiate(lazerPaddleProjectile, worldRight, transform.rotation);
            bulletCount -= 2;
        }
    }
    #endregion

    #region Public Methods
    public void SetCustomBoundaries(float xLimit, float yBasePos) {
        xBoundary = xLimit;
        baseYPos = yBasePos;
        customXLimit = xLimit;
        currentFlipHeight = 0f;
        basePosition = transform.position;
    }

    public void ResetToOriginalPosition() {
        if (parentTransform != null) {
            transform.localPosition = originalLocalPosition;
        }
        basePosition = transform.position;
        currentFlipHeight = 0f;
    }
    #endregion

    #region Gizmos
    void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        DrawMagnetZone();
        DrawEffectLines();
        DrawBoundaries();
        DrawFlipDirection();
    }

    void DrawMagnetZone() {
        Vector3 paddlePos = transform.position;
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);

        Vector3 topLeft = paddlePos + transform.TransformDirection(new Vector3(-magnetOffset, 10f, 0));
        Vector3 topRight = paddlePos + transform.TransformDirection(new Vector3(magnetOffset, 10f, 0));
        Vector3 bottomLeft = paddlePos + transform.TransformDirection(new Vector3(-magnetOffset, 0, 0));
        Vector3 bottomRight = paddlePos + transform.TransformDirection(new Vector3(magnetOffset, 0, 0));

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

    void DrawBoundaries() {
        Vector3 center = GetWorldCenterPosition();
        Gizmos.color = Color.green;

        Vector3 leftBound = center + Vector3.left * xBoundary;
        Vector3 rightBound = center + Vector3.right * xBoundary;

        Gizmos.DrawLine(leftBound + Vector3.up * 2, leftBound + Vector3.down * 2);
        Gizmos.DrawLine(rightBound + Vector3.up * 2, rightBound + Vector3.down * 2);
    }

    void DrawFlipDirection() {
        // Draw the flip direction and max height
        Gizmos.color = Color.cyan;
        Vector3 basePos = basePosition;
        Vector3 maxFlipPos = basePos + transform.up * maxFlipHeightLocal;

        Gizmos.DrawLine(basePos, maxFlipPos);
        Gizmos.DrawWireSphere(maxFlipPos, 0.1f);

        // Draw current flip position
        Gizmos.color = Color.yellow;
        Vector3 currentFlipPos = basePos + transform.up * currentFlipHeight;
        Gizmos.DrawWireSphere(currentFlipPos, 0.05f);
    }

    // Get world center position for boundary calculation
    private Vector3 GetWorldCenterPosition() {
        if (customBoundaryReference != null) {
            return customBoundaryReference.position;
        } else if (parentTransform != null) {
            return parentTransform.TransformPoint(originalLocalPosition);
        } else {
            return transform.position;
        }
    }
    #endregion
}