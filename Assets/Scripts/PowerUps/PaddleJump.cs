using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaddleJump : MonoBehaviour {

    public int score = 75;
    public PaddleMove paddleMove;

    void Awake() {
        if (paddleMove == null) {
            paddleMove = FindObjectOfType<PaddleMove>();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            if (paddleMove != null && paddleMove.maxFlipHeight <= -2.5f) {
                paddleMove.maxFlipHeight += 0.5f;
            } else {
                Debug.LogWarning("GameManager not found!");
            }
            ScoreSpawn(score);
            Destroy(gameObject);
        }
    }

    private void ScoreSpawn(int score) {
        if (GameManager.Instance != null) {
            GameManager.CurrentScore += score;
            ScoreNumberController.instance.SpawnScore(score, transform.position);

            GameManager.CanSpawnBall = false;
        }
    }
}