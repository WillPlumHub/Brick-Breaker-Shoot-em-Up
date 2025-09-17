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

        if (GameManager.levelLayers != null && GameManager.levelLayers.GetLength(0) > 0) {
            // NEW: Find the last row with any rooms
            int lastRow = -1;
            for (int row = GameManager.levelLayers.GetLength(0) - 1; row >= 0; row--) {
                for (int col = 0; col < GameManager.levelLayers.GetLength(1); col++) {
                    if (GameManager.levelLayers[row, col] != null) {
                        lastRow = row;
                        break;
                    }
                }
                if (lastRow != -1) break;
            }

            if (lastRow != -1) {
                // NEW: Check all rooms in the last row for a roof
                Transform roofTransform = null;
                for (int col = 0; col < GameManager.levelLayers.GetLength(1); col++) {
                    GameObject layer = GameManager.levelLayers[lastRow, col];
                    if (layer != null) {
                        roofTransform = layer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Roof"));
                        if (roofTransform != null) break;
                    }
                }

                if (roofTransform != null) {
                    Destroy(roofTransform.gameObject);
                    Debug.Log("Ceiling destroyed (no bricks and no next level).");
                    
                    // Move the ceiling object
                    GameObject ceiling = GameObject.Find("Ceiling");
                    if (ceiling != null) {
                        ceiling.transform.position = new Vector3(roofTransform.position.x, roofTransform.position.y - 0.1f, roofTransform.position.z);
                    }
                } else {
                    Debug.LogWarning("Roof not found in last level layer.");
                }
            } else {
                Debug.LogWarning("No valid layers found when trying to destroy ceiling.");
            }
        } else {
            Debug.LogWarning("No level layers found when trying to destroy ceiling.");
        }
        yield break;  // explicitly end coroutine
    }


}