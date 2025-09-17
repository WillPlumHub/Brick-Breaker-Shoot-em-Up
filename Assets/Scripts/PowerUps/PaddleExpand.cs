using System.Collections;
using UnityEngine;

public class PaddleExpand : MonoBehaviour {
    public int score = 75;
    public GameManager gameManager;
    public float paddleExpandMultiplier = 2f; // Should match what expandPaddle() does

    void Awake() {
        if (gameManager == null) {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (gameManager == null) {
            Debug.LogWarning("GameManager not found!");
            return;
        }

        GameObject paddle = GameObject.Find("Paddle");
        if (paddle == null) {
            Debug.LogWarning("Paddle not found!");
            return;
        }

        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) {
            Debug.LogWarning("Current layer not found!");
            return;
        }

        Transform leftWall = null;
        Transform rightWall = null;

        foreach (Transform child in currentLayer.transform) {
            if (child.name.StartsWith("LWall")) leftWall = child;
            else if (child.name.StartsWith("RWall")) rightWall = child;
        }

        if (leftWall != null && rightWall != null) {
            float roomWidth = Mathf.Abs(rightWall.position.x - leftWall.position.x);
            float paddleScaleX = paddle.transform.localScale.x;
            float newScaleX = paddleScaleX * paddleExpandMultiplier;

            // Use room width minus small margin (to ensure it fits comfortably)
            float safetyMargin = 0.1f;
            if (newScaleX <= roomWidth - safetyMargin) {
                gameManager.expandPaddle();
                Debug.Log($"[PaddleExpand] Paddle expanded to scale.x = {newScaleX:F2}, room width = {roomWidth:F2}");
            } else {
                Debug.Log($"[PaddleExpand] Not enough room to expand: proposed scale.x = {newScaleX:F2}, room width = {roomWidth:F2}");
            }
        }

        ScoreSpawn(score);
        Destroy(gameObject);
    }

    private void ScoreSpawn(int score) {
        if (GameManager.Instance != null) {
            GameManager.CurrentScore += score;
            ScoreNumberController.instance.SpawnScore(score, transform.position);
            GameManager.CanSpawnBall = false;
        }
    }
}