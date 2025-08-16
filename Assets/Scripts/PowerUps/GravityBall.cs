using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityBall : MonoBehaviour {

    public int score = 75;
    public GameManager gameManager;

    void Awake() {
        if (gameManager == null) {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    public void OnTriggerEnter2D(UnityEngine.Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            if (gameManager != null) {
                gameManager.gravityBall = true;
            } else {
                Debug.LogWarning("GameManager not found!");
            }
            ScoreSpawn(score);
            Destroy(gameObject);
        }
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
