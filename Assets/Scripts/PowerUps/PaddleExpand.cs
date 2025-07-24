using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class PaddleExpand : MonoBehaviour {

    public GameManager gameManager;

    void Awake() {
        if (gameManager == null) {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    public void OnTriggerEnter2D(UnityEngine.Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            if (gameManager != null) {
                gameManager.expandPaddle();
            } else {
                Debug.LogWarning("GameManager not found!");
            }
            Destroy(gameObject);
        }
    }
}