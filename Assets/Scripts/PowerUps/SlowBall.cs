using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowBall : MonoBehaviour {

    public GameManager gameManager;

    void Awake() {
        if (gameManager == null) {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null) {
                Debug.LogWarning("GameManager not found in scene!");
            }
        }
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.CompareTag("Player")) return;

        if (gameManager != null && GameManager.ActiveBalls != null) {
            foreach (GameObject ball in GameManager.ActiveBalls) {
                if (ball != null) {
                    ball.GetComponent<BallMovement>().SlowBall();
                }
            }
        } else {
            Debug.LogWarning("GameManager not found!");
        }
        Destroy(gameObject);
    }
}