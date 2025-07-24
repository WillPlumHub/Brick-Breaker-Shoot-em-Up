using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class PaddleMove : MonoBehaviour {

    [Header("Movement")]
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
        MagnetPull();
        MouseMovement();
        flip();
        variableUpdate();
    }

    void HandleInput() { // Handle mouse button state
        bool mouseDownThisFrame = Input.GetMouseButtonDown(0);

        if (mouseDownThisFrame && GameManager.IsGameStart) { // Start game on first click
            GameManager.IsGameStart = false;
            return;
        } else if (!GameManager.IsGameStart) {
            if (!lazerPaddle) {
                foreach (var ball in GameManager.ActiveBalls) { // Check if any balls are stuck
                    if (ball != null && ball.GetComponent<BallMovement>().isStuckToPaddle) {
                        anyBallStuck = true;
                        return;
                    }
                }

                if (!anyBallStuck) {
                    if (mouseDownThisFrame) { // Activate flip
                        gameManager.PlaySFX(gameManager.flipSound);
                        flipping = true;
                    } else if (Input.GetMouseButtonUp(0)) { // Deactivate flip
                        flipping = false;
                    }

                    if (Input.GetMouseButton(0) && transform.position.y < maxFlipHeight) { // Recoil speed modification
                        float currentY = transform.position.y;
                        normalizedHeight = Mathf.InverseLerp(-4f, -3.8f, currentY);
                        recoilSpeed = recoilCurve.Evaluate(normalizedHeight);
                    } else {
                        recoilSpeed = 5;
                    }
                } else {
                    if (mouseDownThisFrame) { // Unstick ball(s)
                        anyBallStuck = false;
                    }
                }

            } else { // W/ lazer paddle, fire lazer projectile
                if (mouseDownThisFrame) {
                    firePaddleLaser();
                }
            }

        }
    }

    void OnTriggerEnter2D(Collider2D col) {
        if (col.gameObject.CompareTag("Lazer")) { // Disable on hit
            disableMovement = col.gameObject.GetComponent<Lazer>().disableTime;
        }
    }

    void MagnetPull() { // Magnet pull
        if (!Input.GetMouseButtonDown(1) || GameManager.IsGameStart || GameManager.ActiveBalls == null) return; // Don't bother conditions

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

        //float clampedX = Mathf.Clamp(mousePosition.x, -XBoundry + (transform.localScale.x / 2), XBoundry - (transform.localScale.x / 2));
    void MouseMovement() {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = Camera.main.transform.position.z + Camera.main.nearClipPlane;

        float paddleWidth = transform.localScale.x;
        //XBoundry = 0.03071f * paddleWidth * paddleWidth - 0.12456f * paddleWidth + 7.8862f;

        if (transform.localScale.x < 0.77f) {
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

        float clampedX = Mathf.Clamp(mousePosition.x, -XBoundry + (paddleWidth / 2), XBoundry - (paddleWidth / 2));

        if (disableMovement > 0) {
            disableMovement -= 1 * Time.deltaTime;
        } else {
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
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x - transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            Instantiate(lazerPaddleProjectile, new Vector2(transform.position.x + transform.localScale.x / 3, transform.position.y + 0.13f), Quaternion.identity);
            bulletCount-=2;
        }
    }
}