using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CeilingDestroy : MonoBehaviour {

    public float destroyDelay = 1f;
    public bool hasTriggered = false;

    private void Start() {
        StartCoroutine(CheckForCeilingDestroy());
    }

    private IEnumerator CheckForCeilingDestroy() {
        yield return new WaitForSeconds(0.2f); // optional delay to avoid startup issues

        while (!hasTriggered) {
            if (GameManager.brickCount <= 0 && string.IsNullOrEmpty(GameManager.nextScene)) {
                hasTriggered = true;
                StartCoroutine(DestroyAfterDelay(destroyDelay));
            }
            yield return new WaitForSeconds(0.1f); // check 10x per second instead of every frame
        }
    }

    private IEnumerator DestroyAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);

        if (GameManager.levelLayers.Count > 0) {
            GameObject lastLayer = GameManager.levelLayers[GameManager.levelLayers.Count - 1];
            Transform roofTransform = lastLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));

            if (roofTransform != null) {
                Destroy(roofTransform.gameObject);
                Debug.Log("Ceiling destroyed (no bricks and no next level).");
            } else {
                Debug.LogWarning("Roof not found in last level layer.");
            }
            GameObject Ceiling = GameObject.Find("Ceiling");
            Ceiling.transform.position = new Vector3(roofTransform.position.x, roofTransform.position.y -0.1f, roofTransform.position.z);
        } else {
            Debug.LogWarning("No level layers found when trying to destroy ceiling.");
        }
        yield break;  // explicitly end coroutine
    }


}