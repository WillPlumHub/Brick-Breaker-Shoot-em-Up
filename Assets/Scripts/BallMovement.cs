using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using static Unity.Collections.AllocatorManager;

public class BallMovement : MonoBehaviour {

    public float deathPlane = -6f; // The height the ball dies at
    public float offset = 1.8f; // The distance between ball & paddle at the start of a game
    private float floorAngleThreshold = 30f; // Threshold to check if collision is against a wall or floor
    public float targetSpeed = 5f; // Target speed ball moves at
    public float accelerationDrag = 1.8f; // Speed ball accelerates/decellerates at when moving freely

    public float paddleOffset; // The x point where the ball hits the paddle
    public Vector3 moveDir = new Vector3(0f, 1f, 0f); // Direction the ball is currently moving
    public float currentSpeed; // Reference to ball's speed
    public int scoreMult;
    
    public GameObject paddle; // Reference to the paddle
    //public bool touchingPaddle;
    private bool collisionProcessedThisFrame = false;
    public bool ceilingBreak = false;
    public bool isStuckToPaddle = false;

    public float gravityBallReduction = 8f;
    public float terminalFallSpeed = 12f;

    private void Awake() {
        paddle = GameObject.Find("Paddle");

        // Register this ball with the GameManager
        GameManager.RegisterBall(gameObject);

        currentSpeed = GameManager.BallSpeed;
    }

    private void OnDestroy() {
        // Unregister this ball when destroyed
        GameManager.UnregisterBall(gameObject);
    }

    public void InitializeBall(Vector3 direction) {
        moveDir = direction;
    }

    private void Update() {
        bool grabPaddle = paddle.GetComponent<PaddleMove>().grabPaddle;

        if (GameManager.IsGameStarted) {
            StickToPaddle();
        } else {
            Movement();
            CheckPaddleCollision();
            HandleSpeedAdjustment();
        }

        CheckDeathPlane();
        bounceCheck();


        if (isStuckToPaddle && Input.GetMouseButtonDown(0)) {
            isStuckToPaddle = false;
            Debug.Log("YEPPERS");
        }
        if (isStuckToPaddle) {
            GameManager.GravityBall = false;
        }

        if (currentSpeed <= 0.1f && moveDir.y > 0) {
            moveDir.y = -1f;
            
            if (!GameManager.GravityBall) {
                currentSpeed = 3f;
            }
        }

        if (currentSpeed >= 5 && currentSpeed <= 8) {
            accelerationDrag = 1.8f;
        }

        if (GameManager.GravityBall && !GameManager.IsGameStarted) {
            GravityBall();
        }

        /*if (Input.GetButtonDown("Jump")) {
            ShrinkBall();
        }*/
    }

    public void FastBall() {
        GameManager.GravityBall = false;
        currentSpeed = 14f;
        accelerationDrag = 0.2f;
    }

    public void SlowBall() {
        currentSpeed = 1f;
        accelerationDrag = 0.2f;
        GameManager.BrickThu = false;
        GameManager.FireBall = false;
        GameManager.GravityBall = false;
    }

    public void MegaBall() {
        transform.localScale = new Vector3(transform.localScale.x * 2, transform.localScale.y * 2, 1f);
        GameManager.GravityBall = false;
    }

    public void ShrinkBall() {
        transform.localScale = new Vector3(transform.localScale.x / 2, transform.localScale.y / 2, 1f);
        //x2 score mult
        GameManager.scoreMult = 2;
        GameManager.GravityBall = false;
    }

    public void GravityBall() {
        if (moveDir.y > 0) {
            currentSpeed -= gravityBallReduction * Time.deltaTime;
            if (currentSpeed <= 0.1f) {
                currentSpeed = 0.1f;
                moveDir.y = -1;
            }
        }

        else if (moveDir.y < 0) {
            currentSpeed += gravityBallReduction * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, terminalFallSpeed);
        }
    }

    private void bounceCheck() {
        if (transform.position.x >= 7.3f || transform.position.x <= -7.3f) {
            bounce(true);
        }
        if (transform.position.y >= 5.2f && !GameManager.ceilinigDestroyed) {
            bounce(false);
        }
        if (transform.position.y >= 6f && !GameManager.ceilinigDestroyed) {
            transform.position = new Vector3(0, 0);
            moveDir.y = -1;
            moveDir.x = 0;
        }
    }

    private void bounce(bool horizontal) {
        GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
        if (horizontal) {
            moveDir.x *= -1;
        } else {
            moveDir.y *= -1;
        }
    }

    private void StickToPaddle() {
        //currentSpeed = 0;
        
        transform.position = new Vector3(paddle.transform.position.x, paddle.transform.position.y + offset, 0);
    }

    private void Movement() {
        if (moveDir != Vector3.zero) {
            transform.position += moveDir.normalized * currentSpeed * Time.deltaTime;
        }
    }

    private void HandleSpeedAdjustment() {
        if (!GameManager.GravityBall)
        {
            if (currentSpeed > targetSpeed)
            {
                currentSpeed -= accelerationDrag * Time.deltaTime;
                if (currentSpeed < targetSpeed)
                {
                    currentSpeed = targetSpeed;
                }
            }
            else if (currentSpeed < targetSpeed)
            {
                currentSpeed += accelerationDrag * Time.deltaTime;
                if (currentSpeed > targetSpeed)
                {
                    currentSpeed = targetSpeed;
                }
            }
        }


        if (moveDir.y > 0 && moveDir.y != 1) {
            moveDir.y = 1;
        }
        if (moveDir.y < 0 && moveDir.y != -1) {
            moveDir.y = -1;
        }
        /*if (moveDir.y == -1 && currentSpeed < 0) {
            currentSpeed = 0.1f;
        }*/
    }

    private void CheckPaddleCollision() {
        bool inVerticalRange = transform.position.y < paddle.transform.position.y + 0.13f && transform.position.y > paddle.transform.position.y - 0.13f;
        bool inHorizontalRange = transform.position.x > paddle.transform.position.x - paddle.transform.localScale.x / 2.2f && transform.position.x < paddle.transform.position.x + paddle.transform.localScale.x / 2.2f;

        if (inVerticalRange && inHorizontalRange && moveDir.y == -1) {
            if (paddle.GetComponent<PaddleMove>().grabPaddle) {
                isStuckToPaddle = true;
            }
            
            PaddleBounce();
        }
    }


    private void CheckDeathPlane() {
        if (transform.position.y <= deathPlane) {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.contactCount == 0 || collisionProcessedThisFrame)
            return;

        collisionProcessedThisFrame = true;

        Vector2 normal = collision.GetContact(0).normal;
        float angle = Vector2.Angle(normal, Vector2.up);

        if (collision.gameObject.CompareTag("Block") || collision.gameObject.CompareTag("Brick")) {
            HandleBlockCollision(collision.gameObject);
        }
        
        if (!GameManager.BrickThu || collision.gameObject.CompareTag("Block")) {
            HandleBouncePhysics(angle, collision.gameObject);
        }

        StartCoroutine(ResetCollisionFlag());
    }

    private void HandleBlockCollision(GameObject collidedObject) {
        if (currentSpeed > targetSpeed) {
            scoreMult = (int)(currentSpeed - 3);
        } else {
            scoreMult = 2;
        }

        if (GameManager.FireBall) {
            // Fireball logic
        }

        collidedObject.GetComponent<ObjHealth>().TakeDamage(1, scoreMult);
        if (collidedObject.CompareTag("Brick")) {
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
        }
    }

    private void HandleBouncePhysics(float angle, GameObject collidedObject) {
        if (!collidedObject.CompareTag("Player")) {
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);

            if (angle <= floorAngleThreshold || angle >= 180f - floorAngleThreshold) {
                moveDir.y *= -1;
            } else {
                moveDir.x *= -1;
            }
        }
    }

    public void PaddleBounce() {
        GameManager.Instance.PlaySFX(GameManager.Instance.paddleBounceSound);

        paddleOffset = paddle.transform.position.x - transform.position.x;
        moveDir.y *= -1;
        moveDir.x = -(paddleOffset / 1.1f);

        if (paddle.transform.position.y != -3.8f && paddle.GetComponent<PaddleMove>().flipping) {
            transform.position = new Vector3(transform.position.x, paddle.transform.position.y, 0);
        }

        if (paddle.GetComponent<PaddleMove>().recoilSpeed > 5) {
            currentSpeed = paddle.GetComponent<PaddleMove>().recoilSpeed;
            accelerationDrag = 1.8f;
        }

        if (currentSpeed > 14f) {
            GameManager.Instance.PlaySFX(GameManager.Instance.perfectFlipSound);
            Debug.Log($"Speed: {currentSpeed} PERFECT!!!");
        } else {
            Debug.Log($"Speed: {currentSpeed}");
        }
    }

    private IEnumerator ResetCollisionFlag() {
        yield return new WaitForEndOfFrame();
        collisionProcessedThisFrame = false;
    }
}