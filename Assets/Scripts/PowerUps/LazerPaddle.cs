using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazerPaddle : MonoBehaviour {

    public PaddleMove paddleMove;

    void Awake() {
        if (paddleMove == null) {
            paddleMove = FindObjectOfType<PaddleMove>();
        }
    }

    public void OnTriggerEnter2D(UnityEngine.Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            if (paddleMove != null) {
                paddleMove.lazerPaddle = true;
            } else {
                Debug.LogWarning("GameManager not found!");
            }
            Destroy(gameObject);
        }
    }

}