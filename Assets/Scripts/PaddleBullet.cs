using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleBullet : MonoBehaviour
{

    public float speed = 1f;
    private GameObject paddle;
    public GameObject brickContainer;
    public GameObject[] bricks;

    private void Awake() {
        paddle = GameObject.Find("Paddle");
        if (brickContainer == null) {
            brickContainer = GameObject.Find("BlockList");
        }
        UpdateBricks();
    }

    void Update() {
        transform.Translate(Vector2.up * speed * Time.deltaTime, Space.World);

        if (transform.position.y >= 7f) {
            ReturnBulletToPaddle();
            Destroy(gameObject);
        }

        foreach (var brick in bricks) {
            if (brick != null) {
                bool inVerticalRange = transform.position.y < brick.transform.position.y + 0.495f && transform.position.y > brick.transform.position.y - 0.495f;
                                
                bool inHorizontalRange = transform.position.x > brick.transform.position.x - 0.38f && transform.position.x < brick.transform.position.x + 0.38f;

                if (inVerticalRange && inHorizontalRange) {
                    //Debug.Log($"Bullet overlaps brick: {brick.name}");
                    
                    ReturnBulletToPaddle();
                    UpdateBricks();
                    if (!GameManager.BrickThu) {
                        Destroy(gameObject);
                    }
                    if (GameManager.FireBall) {
                        // Fireball logic
                    }

                    brick.GetComponent<ObjHealth>()?.TakeDamage(1, 1);
                    if (GameManager.Instance != null) {
                        GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
                    }
                    return;
                }
            }
        }
    }

    private void UpdateBricks() {
        bricks = GameObject.FindGameObjectsWithTag("Brick");
    }

    private void ReturnBulletToPaddle() {
        if (paddle != null) {
            if (paddle.GetComponent<PaddleMove>() != null) {
                paddle.GetComponent<PaddleMove>().bulletCount++;
            }
        }
    }
}