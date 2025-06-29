using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class PaddleMove : MonoBehaviour {

    [Header("Movement")]
    public float yPos = -4f; // Paddle's Y position
    public float disableMovement = 0f; // Time left disabled when hit
    public float XBoundry = 7f;
    private Vector3 mousePosition = new Vector3(0, 0, 0);

    [Header("Flip")]
    public bool flipping = false; 
    public float flipSpeed = 20f;
    public float maxFlipHeight = -3.5f; // Max extent paddle will move up to when flipping
    public float recoilSpeed;
    public AnimationCurve recoilCurve;
    public float normalizedHeight;
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

    [Header("Private Vars")]
    private bool _mouseReleased = true;
    
    void Start() {
        GameManager.IsGameStarted = true;

        // Initializing GameManager values in case they aren't already
        if (GameManager.DisableTimer == 0) GameManager.DisableTimer = disableMovement;
        if (GameManager.MagnetOffset == 0) GameManager.MagnetOffset = magnetOffset;
        if (GameManager.DecelerationRate == 0) GameManager.DecelerationRate = decellerate;
    }
    
    void Awake() {
        GameManager.IsGameStarted = true;
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
    }
    
    void Update() {
        HandleInput();
        ClickControls();
        MouseMovement();
        flip();
        variableUpdate();
    }

    void HandleInput() {
        // Handle mouse button state
        bool mouseDown = Input.GetMouseButton(0);
        bool mouseUp = Input.GetMouseButtonUp(0);
        bool mouseDownThisFrame = Input.GetMouseButtonDown(0);

        if (mouseDownThisFrame && GameManager.IsGameStarted && _mouseReleased) { // Start game on first click
            GameManager.IsGameStarted = false;
            _mouseReleased = false;
            return;
        }

        if (!GameManager.IsGameStarted) {

            if (!lazerPaddle) {
            // Now do the ball stuck check safely
            foreach (var ball in GameManager.ActiveBalls) {
                if (ball != null && ball.GetComponent<BallMovement>().isStuckToPaddle) {
                    Debug.Log("BLOCKED FLIP: Ball is stuck!");
                    return;
                }
            }
            Debug.Log("ALLOWED FLIP: No balls stuck.");

                if (!anyBallStuck)
                {
                    if (mouseDown && !flipping && _mouseReleased)
                    {
                        gameManager.PlaySFX(gameManager.flipSound);
                        flipping = true;
                        _mouseReleased = false;
                    }
                    else if (mouseUp && flipping)
                    {
                        flipping = false;
                        _mouseReleased = true;
                    }

                    if (mouseUp) _mouseReleased = true; // Update release state

                    if (mouseDown && transform.position.y < maxFlipHeight)
                    {
                        float currentY = transform.position.y;
                        normalizedHeight = Mathf.InverseLerp(-4f, -3.8f, currentY);
                        recoilSpeed = recoilCurve.Evaluate(normalizedHeight);
                    }
                    else
                    {
                        recoilSpeed = 5;
                    }
                }
            } else {
                if (mouseDownThisFrame) {
                    firePaddleLaser();
                }
            }

        }
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.CompareTag("Lazer")) {
            disableMovement = col.gameObject.GetComponent<Lazer>().disableTime;
        }
    }

    void ClickControls() {
        if (!Input.GetMouseButtonDown(1) || GameManager.IsGameStarted || GameManager.ActiveBalls == null) return;
        float paddleX = transform.position.x;
        foreach (GameObject ball in GameManager.ActiveBalls) {
            if (ball == null) continue;
            BallMovement ballMovement = ball.GetComponent<BallMovement>();
            if (ballMovement == null || ballMovement.isStuckToPaddle) continue;

            float range = Mathf.Clamp01(1f - (Mathf.Abs(ball.transform.position.x - paddleX) / magnetOffset));
            bool isWithinOffset = range > 0f;
            
            if (isWithinOffset) {
                if (ballMovement.moveDir.y == 1) {
                    ballMovement.currentSpeed -= decellerate;
                } else if (ballMovement.moveDir.y == -1) {
                    ballMovement.currentSpeed += decellerate;
                }
            }
        }
    }

    void MouseMovement() {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = Camera.main.transform.position.z + Camera.main.nearClipPlane;

        float clampedX = Mathf.Clamp(mousePosition.x, -XBoundry + (transform.localScale.x / 2), XBoundry - (transform.localScale.x / 2));
        if (disableMovement > 0) {
            disableMovement -= 1 * Time.deltaTime;
        } else if (disableMovement <= 0) {
            transform.position = new Vector3(clampedX, yPos, mousePosition.z);
        }
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
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x - 1f, transform.position.y + 0.13f), Quaternion.identity);
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x + 1f, transform.position.y + 0.13f), Quaternion.identity);
            bulletCount-=2;
        }
    }
}
