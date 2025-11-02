using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemy : MonoBehaviour {

    [Header("Health")]
    public float health = 1;

    [Header("Timer")]
    public float timer = 0;

    [Header("Firing Stats")]
    public int ClipSize = 3;
    public float Range = 10f;
    public float RateOfFire = 1f; // bullets per second
    public float ReloadSpeed = 2f; // time before firing again
    private bool isFiring = false;

    [Header("Position")]
    public float SpawnPosOffsetX = 0f;
    public float SpawnPosOffsetY = 0f;

    [Header("Spread Rotation")]
    [Range(-360, 360)]
    public int SpreadOffsetMax;
    [Range(-360, 360)]
    public int SpreadOffsetMin;
    public bool SpawnTargeting = false;

    [Header("Projectiles")]
    public GameObject BaseProjectile;
    public GameObject SpecialProjectile;

    void Update() {

        if (IsAnyPaddleCloseEnough() && IsObjectInViewport()) {
            timer += Time.deltaTime;
        } else {
            timer = 0;
        }

        if (timer >= ReloadSpeed && !isFiring && IsObjectInViewport() && IsAnyPaddleCloseEnough()) {
            StartCoroutine(Fire());
        }

        if (health <= 0) {
            Destroy(gameObject);
        }
    }

    private bool IsAnyPaddleCloseEnough() {
        return GetClosestPaddle() != null;
    }

    private Transform GetClosestPaddle() {
        Transform closestPaddle = null;
        float closestDistance = float.MaxValue;

        // Check main paddle
        if (GameManager.Paddle != null) {
            float distanceToMainPaddle = Vector3.Distance(transform.position, GameManager.Paddle.transform.position);
            if (distanceToMainPaddle <= Range) {
                closestPaddle = GameManager.Paddle.transform;
                closestDistance = distanceToMainPaddle;
            }
        }

        GameObject[] miniPaddles = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject miniPaddle in miniPaddles) {
            float distanceToMiniPaddle = Vector3.Distance(transform.position, miniPaddle.transform.position);
            if (distanceToMiniPaddle <= Range && distanceToMiniPaddle < closestDistance) {
                closestPaddle = miniPaddle.transform;
                closestDistance = distanceToMiniPaddle;
            }
        }

        return closestPaddle;
    }

    private IEnumerator Fire() {
        isFiring = true;

        Transform targetPaddle = GetClosestPaddle();
        if (targetPaddle == null) {
            isFiring = false;
            yield break;
        }

        for (int i = 0; i < ClipSize; i++) {
            Vector3 spawnPos;
            Quaternion rotation;

            if (!SpawnTargeting) {
                // Calculate offset and rotation for spread shot
                Vector3 offset = new Vector3(SpawnPosOffsetX, SpawnPosOffsetY, 0f);
                float angle = i * UnityEngine.Random.Range(SpreadOffsetMin, SpreadOffsetMax);

                Quaternion spreadRotation = Quaternion.Euler(0f, 0f, angle);
                Vector3 rotatedOffset = spreadRotation * offset;

                spawnPos = transform.position + rotatedOffset;
                rotation = Quaternion.identity;

            } else {
                Vector3 directionToPaddle = (targetPaddle.position - transform.position).normalized;
                float angleToPaddle = Mathf.Atan2(directionToPaddle.y, directionToPaddle.x) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0f, 0f, angleToPaddle + 90f + UnityEngine.Random.Range(SpreadOffsetMin, SpreadOffsetMax));

                Vector3 offset = new Vector3(SpawnPosOffsetX, SpawnPosOffsetY, 0f);
                Vector3 rotatedOffset = rotation * offset;

                spawnPos = transform.position + rotatedOffset;
            }

            // Instantiate projectile
            GameObject proj = Instantiate(BaseProjectile, spawnPos, rotation);

            // Set projectile's movement and source
            ReflectableEnemyProjectile rep = proj.GetComponent<ReflectableEnemyProjectile>();
            rep.sourceEnemy = transform;
            rep.moveDir = (targetPaddle.position - spawnPos).normalized;

            yield return new WaitForSeconds(1f / RateOfFire);
        }
        timer = 0;
        isFiring = false;
    }

    public bool IsObjectInViewport() {
        // Get the main camera
        Camera mainCamera = Camera.main;

        if (mainCamera == null) {
            Debug.LogWarning("Main camera not found!");
            return false;
        }

        // Convert the object's world position to viewport coordinates
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);

        // Check if the object is within the camera's viewport (0-1 range)
        bool isInViewport = viewportPosition.x >= 0 && viewportPosition.x <= 1 && viewportPosition.y >= 0 && viewportPosition.y <= 1 && viewportPosition.z > 0; // z > 0 means in front of camera
        return isInViewport;
    }
}