using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemy : MonoBehaviour {

    [Header("Timer")]
    public float timer = 0;

    [Header("Firing Stats")]
    public int ClipSize = 3;
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
    private GameObject Paddle;

    void Awake() {
        Paddle = GameObject.Find("Paddle");
    }

    void Update() {
        timer += Time.deltaTime;
        if (timer >= ReloadSpeed && !isFiring) {
            StartCoroutine(Fire());
        }
    }

    private IEnumerator Fire() {
        isFiring = true;

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
                Vector3 directionToPaddle = (Paddle.transform.position - transform.position).normalized;
                float angleToPaddle = Mathf.Atan2(directionToPaddle.y, directionToPaddle.x) * Mathf.Rad2Deg;
                rotation = Quaternion.Euler(0f, 0f, angleToPaddle + 90f + UnityEngine.Random.Range(SpreadOffsetMin, SpreadOffsetMax));

                Vector3 offset = new Vector3(SpawnPosOffsetX, SpawnPosOffsetY, 0f);
                Vector3 rotatedOffset = rotation * offset;

                spawnPos = transform.position + rotatedOffset;
            }

            Instantiate(BaseProjectile, spawnPos, rotation);
            yield return new WaitForSeconds(1f / RateOfFire);
        }

        timer = 0;
        isFiring = false;
    }


}