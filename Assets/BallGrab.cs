using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallGrab : MonoBehaviour {

    public float X = 0f;
    public float Y = 1.5f;
    
    void Start() {
        
    }

    void Update() {
        
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.name == "Ball") {
            BallMovement ball = collision.gameObject.GetComponent<BallMovement>();
            ball.StickToObject(gameObject.transform, X, Y);

            // Start coroutine to watch for unstick
            StartCoroutine(WaitForUnstick(ball));
        }
    }

    private IEnumerator WaitForUnstick(BallMovement ball) {
        // Wait until the ball is no longer stuck to this object
        yield return new WaitUntil(() => ball.stickTarget != transform);
        // Destroy effects
        Destroy(gameObject);
    }

}