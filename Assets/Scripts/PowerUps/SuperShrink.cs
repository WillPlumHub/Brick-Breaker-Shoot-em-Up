using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperShrink : MonoBehaviour {

    public GameManager gameManager;

    void Awake() {
        if (gameManager == null) {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    public void OnTriggerEnter2D(UnityEngine.Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            if (gameManager != null) {
                gameManager.superShrinkPaddle();
            } else {
                Debug.LogWarning("GameManager not found!");
            }
            Destroy(gameObject);
        }
    }
}