using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CashBunch : MonoBehaviour {

    [Header("Movement")]
    public Vector3 moveDir = new Vector3(0f, 1f, 0f); // Direction the ball is currently moving
    public float currentSpeed;
    public float gravityBallSpeedReduction = 8f;
    public float terminalFallSpeed = 12f;
    
    [Header("Collision")]
    public GameObject paddle;
    public float deathPlane = -6f; // The height the ball dies at
    public float floorAngleThreshold = 30f; // Threshold to check if collision is against a wall or floor
    private bool collisionProcessedThisFrame = false;

    [Header("Score")]
    public int scoreValue;
    public int defaultScoreValue;
    public int scoreMult;
    
    private void Awake() {
        paddle = GameObject.Find("Paddle");
        moveDir = new Vector2(0, -1);
    }

    private void Update() {

        Movement();
        CheckPaddleCollision();
        HandleSpeedAdjustment();
        CheckDeathPlane();
        bounceCheck();
        GravityBall();

        if (currentSpeed <= 0.1f && moveDir.y > 0) {
            moveDir.y = -1f;
            currentSpeed = 3f;
        }
    }

    public void GravityBall() {
        if (moveDir.y > 0) {
            currentSpeed -= gravityBallSpeedReduction * Time.deltaTime;
            if (currentSpeed <= 0.1f) {
                currentSpeed = 0.1f;
                moveDir.y = -1;
            }
        } else if (moveDir.y < 0) {
            currentSpeed += gravityBallSpeedReduction * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, terminalFallSpeed);
        }
    }

    private void bounceCheck() {
        if (transform.position.x >= 7.3f || transform.position.x <= -7.3f) {
            bounce(true);
        }
        if (transform.position.y >= (-4.5 + (10 * GameManager.numberOfBoards)) && !GameManager.ceilinigDestroyed) {
            bounce(false);
        }
        if (transform.position.y > (-4.5 + (10 * GameManager.numberOfBoards) + 0.5f) && !GameManager.ceilinigDestroyed) {
            moveDir.y = -1;
            moveDir.x = 0; // Destroy
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

    private void Movement() {
        if (moveDir != Vector3.zero) {
            transform.position += moveDir.normalized * currentSpeed * Time.deltaTime;
        }
    }

    private void HandleSpeedAdjustment() {
        if (moveDir.y > 0 && moveDir.y != 1) {
            moveDir.y = 1;
        }
        if (moveDir.y < 0 && moveDir.y != -1) {
            moveDir.y = -1;
        }
    }

    private void CheckPaddleCollision() {
        bool inVerticalRange = transform.position.y < paddle.transform.position.y + 0.13f && transform.position.y > paddle.transform.position.y - 0.13f;
        bool inCollectionRange = transform.position.y < paddle.transform.position.y && transform.position.y > paddle.transform.position.y;
        bool inHorizontalRange = transform.position.x > paddle.transform.position.x - paddle.transform.localScale.x / 2.2f && transform.position.x < paddle.transform.position.x + paddle.transform.localScale.x / 2.2f;

        if (inVerticalRange && inHorizontalRange && moveDir.y == -1 && paddle.GetComponent<PaddleMove>().flipping && scoreValue > defaultScoreValue) {
            PaddleBounce();
            scoreValue *= scoreMult;
        }
    }

    public void collect() {
        GameManager.CurrentScore += scoreValue;
        Destroy(gameObject);
    }

    private void CheckDeathPlane() {
        if (transform.position.y <= paddle.GetComponent<PaddleMove>().baseYPos - 0.4f) {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.contactCount == 0 || collisionProcessedThisFrame)
            return;

        collisionProcessedThisFrame = true;

        Vector2 normal = collision.GetContact(0).normal;
        float angle = Vector2.Angle(normal, Vector2.up);

        
        if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Brick") && !collision.gameObject.CompareTag("Block")) {
            HandleBouncePhysics(angle, collision.gameObject);
        }

        if (collision.gameObject.CompareTag("Player")) {
            if (scoreValue > defaultScoreValue && !paddle.GetComponent<PaddleMove>().flipping) {
                collect();
            } else if (scoreValue <= defaultScoreValue) {
                collect();
            }
        }

        StartCoroutine(ResetCollisionFlag());
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

        var paddleOffset = paddle.transform.position.x - transform.position.x;
        moveDir.y *= -1;
        moveDir.x = -(paddleOffset / 1.1f);

        if (paddle.transform.position.y != -3.8f && paddle.GetComponent<PaddleMove>().flipping) {
            transform.position = new Vector3(transform.position.x, paddle.transform.position.y, 0);
        }

        if (paddle.GetComponent<PaddleMove>().recoilSpeed > 5) {
            currentSpeed = paddle.GetComponent<PaddleMove>().recoilSpeed;
        }

        if (currentSpeed > 14f && paddle.GetComponent<PaddleMove>().flipping && paddle.GetComponent<PaddleMove>().recoilSpeed >= 14f) {
            GameManager.Instance.PlaySFX(GameManager.Instance.perfectFlipSound);
            //Debug.Log($"Speed: {currentSpeed} PERFECT!!!");
        } else {
            //Debug.Log($"Speed: {currentSpeed}");
        }
    }

    private IEnumerator ResetCollisionFlag() {
        yield return new WaitForEndOfFrame();
        collisionProcessedThisFrame = false;
    }
}