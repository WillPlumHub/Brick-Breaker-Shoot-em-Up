using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class Brick : MonoBehaviour {

    public int health = 1;
    public int scoreValue = 100;
    public GameObject breakEffectPrefab;

    void Start() {
        
    }

    public void TakeDamage(int damage) {
        health -= damage;

        if (health <= 0) {
            DestroyBlock();
        }
    }

    private void DestroyBlock() {
        // Score points
        if (GameManager.Instance != null) {
            GameManager.CurrentScore += scoreValue;
            GameManager.CanSpawnBall = false;
        }

        // Spawn break effect if available
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}