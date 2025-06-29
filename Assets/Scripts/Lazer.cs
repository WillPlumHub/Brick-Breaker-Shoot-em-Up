using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lazer : MonoBehaviour {

    public float speed;
    public float disableTime;

    void Update() {
        transform.Translate(Vector2.down * (speed * Time.deltaTime));
    }

}
