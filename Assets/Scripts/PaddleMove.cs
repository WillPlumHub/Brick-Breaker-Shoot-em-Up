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
    public float mod = 1;
    public float modDefault = 1;
    public float yPos = -4f; // Paddle's Y position
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
        GameManager.IsGameStart = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }
    
    void Update() {
        HandleInput();
        PaddleMovement();
        flip();
        variableUpdate();

        for (int i = 0; i <= 19; i++)
        {
            if (Input.GetKeyDown((KeyCode)(330 + i)))
            {
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
                        return;
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
                        normalizedHeight = Mathf.InverseLerp(-4f, -3.8f, currentY);
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
            if (ballMovement == null || ballMovement.isStuckToPaddle) continue;

            float range = Mathf.Clamp01(1f - (Mathf.Abs(ball.transform.position.x - transform.position.x) / magnetOffset)); // Check if given ball is within magnet range
            bool isWithinOffset = range > 0f;
            
            if (isWithinOffset) { // Actually activate the magnet effect
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

    void PaddleMovement() {
        float targetX = transform.position.x;
        float paddleWidth = transform.localScale.x;
                
        if (transform.localScale.x < 0.77f) { // Update bounds based on size
            XBoundry = 7.38f;
            GameManager.scoreMult = 2;
        } else if (transform.localScale.x < 1.16f) {
            XBoundry = 7.45f;
        } else if (transform.localScale.x < 2.31f) {
            XBoundry = 7.6f;
        } else if (transform.localScale.x < 4.61f) {
            XBoundry = 7.9f;
        } else if (transform.localScale.x < 9.21f) {
            XBoundry = 8.5f;
        } else if (transform.localScale.x < 18.41f) {
            XBoundry = 9.6f;
        }

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
        transform.position = new Vector3(targetX, yPos, 0f);
    }

    void variableUpdate() {
        magnetOffset = GameManager.MagnetOffset;
        decellerate = GameManager.DecelerationRate;
    }

    void flip() {
        float targetHeight = flipping ? maxFlipHeight : -4f;
        yPos = Mathf.MoveTowards(yPos, targetHeight, flipSpeed * Time.deltaTime);
    }

    void firePaddleLaser() {
        if (bulletCount > 0) {
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x - transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x + transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            bulletCount-=2;
        }
    }
}