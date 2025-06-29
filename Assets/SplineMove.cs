using System.Collections;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine;

public class SplineMove : MonoBehaviour {

    public GameObject ball;
    public SplineContainer splinePath;
    public SplineAnimate splineAnim;

    public Vector2 endingDir;

    void Start() {
        
    }

    void Update() {
        
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.tag == "Ball") {
            ball = collision.gameObject;
            ball.GetComponent<Rigidbody2D>().simulated = false;
            splineAnim = ball.GetComponent<SplineAnimate>();
            splineAnim.Container = splinePath;
            Debug.Log("Worked: " + ball.name + ", " + splinePath.name);
        }
    }
}
