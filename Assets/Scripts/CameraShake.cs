using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour {

    public float duration = 1f;
    public AnimationCurve curve;
    public bool start;
    
    // Update is called once per frame
    void Update() {
        if (start) {
            start = false;
            StartCoroutine(Shaking());
        }
    }

    IEnumerator Shaking() {
        Vector3 startPos = transform.position;
        float elapsedTime = 0;

        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            float strength = curve.Evaluate(elapsedTime / duration);
            transform.position = startPos + Random.insideUnitSphere * strength;
            yield return null;
        }

        transform.position = startPos;
    }
}