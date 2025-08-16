using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShinkBall : MonoBehaviour {

    public int score = 75;
    public GameManager gameManager;

    void Awake() {
        if (gameManager == null) {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null) {
                Debug.LogWarning("GameManager not found in scene!");
            }
        }
    }

    public void OnTriggerEnter2D(UnityEngine.Collider2D collision) {
        if (!collision.CompareTag("Player")) return;

        if (gameManager != null && GameManager.ActiveBalls != null) {
            foreach (GameObject ball in GameManager.ActiveBalls) {
                if (ball != null) {
                    ball.GetComponent<BallMovement>().ShrinkBall();
                }
            }
        }
        ScoreSpawn(score);
        Destroy(gameObject);
    }

    private void ScoreSpawn(int score)
    {
        if (GameManager.Instance != null)
        {
            GameManager.CurrentScore += score;
            ScoreNumberController.instance.SpawnScore(score, transform.position);

            GameManager.CanSpawnBall = false;
        }
    }
}   