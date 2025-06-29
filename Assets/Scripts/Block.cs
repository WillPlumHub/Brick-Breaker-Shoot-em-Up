using UnityEngine;

public class Block : MonoBehaviour {

    public int health = 1;
    private CameraShake cameraShake;

    void Start() {
        cameraShake = FindObjectOfType<CameraShake>();
        if (cameraShake != null) {
            GameObject foundObject = cameraShake.gameObject;
        } else {
            Debug.LogWarning("No GameObject with CameraShake script found.");
        }
    }


    public void TakeDamage(int damage) {
        health -= damage;
        cameraShake.start = true;
        if (health <= 0) {
            DestroyBlock();
        }
    }

    private void DestroyBlock() {
        Destroy(gameObject);
    }
}