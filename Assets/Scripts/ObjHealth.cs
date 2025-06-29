using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ObjHealth : MonoBehaviour {

    public float health = 1;
    public int scoreValue = 2;
    public GameObject breakEffectPrefab;
    public bool CameraShake = false;
    public float powerUpOdds = 10f;
    private CameraShake cameraShake;

    void Start() {
        gridSet();

        cameraShake = FindObjectOfType<CameraShake>();
        if (cameraShake != null) {
            GameObject foundObject = cameraShake.gameObject;
        } else {
            Debug.LogWarning("No GameObject with CameraShake script found.");
        }
    }

    public void gridSet(){
        float xSize = 0.7f;
        float ySize = 0.36f;
        Vector3 snappedPosition = new Vector3(Mathf.Round(transform.position.x / xSize) * xSize, Mathf.Round(transform.position.y / ySize) * ySize, 0f);
        transform.position = snappedPosition;
    }

    public void TakeDamage(int damage, int scoreMult) {
        health -= damage;

        if (CameraShake) {
            cameraShake.start = true;
        }

        if (health <= 0 || GameManager.BrickThu) {
            if (!CameraShake) {
                GameManager.brickCount -= 1;
            }
            DestroyBlock(scoreMult);
        } else {
            GameManager.CurrentScore += (scoreValue * GameManager.scoreMult) / 2;
            ScoreSpawn((scoreValue * GameManager.scoreMult) / 2);
        }
    }

    private void DestroyBlock(int scoreMult) {

        if (Random.Range(0f, 100f) < powerUpOdds && !CameraShake) {
            Debug.Log("Got a power up");
            //Instantiate power up
        }

        int score = (scoreValue * scoreMult) * (GameManager.scoreMult);

        ScoreSpawn(score);

        Debug.Log("Score Given: " + scoreValue + " * " + scoreMult + " = " + score);

        

        if (breakEffectPrefab != null)
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    private void ScoreSpawn(int score) {
        if (GameManager.Instance != null) {
            GameManager.CurrentScore += score;
            ScoreNumberController.instance.SpawnScore(score, transform.position);

            GameManager.CanSpawnBall = false;
        }
    }
}