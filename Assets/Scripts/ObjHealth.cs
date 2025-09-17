using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class ObjHealth : MonoBehaviour {

    public float health = 1;
    public int scoreValue = 2;
    public GameObject breakEffectPrefab;
    public bool CameraShake = false;
    public bool Invincibility = false;
    public float powerUpOdds = 10f;
    private CameraShake cameraShake;

    void Start() {
        GridSet();
        StartCoroutine(AssignParentCoroutine());
        
        cameraShake = FindObjectOfType<CameraShake>();
        if (cameraShake != null) {
            GameObject foundObject = cameraShake.gameObject;
        } else {
            Debug.LogWarning("No GameObject with CameraShake script found.");
        }
    }

    void Update() {
        if (transform.rotation != Quaternion.identity) {
            transform.rotation = Quaternion.identity;
        }
    }

    public void GridSet() {
        float xSize = 0.7f;
        float ySize = 0.36f;
        Vector3 snappedPosition = new Vector3(Mathf.Round(transform.position.x / xSize) * xSize, Mathf.Round(transform.position.y / ySize) * ySize, 0f);
        transform.position = snappedPosition;
    }

    public void TakeDamage(int damage, int scoreMult, float speed) {
        if (!Invincibility) {
            health -= damage;

            if (CameraShake && cameraShake != null) {
                cameraShake.start = true;
                cameraShake.shake(speed);
            }

            if (health <= 0 || GameManager.BrickThu) {
                if (!CameraShake) {
                    GameManager.brickCount -= 1;
                    /*LocalRoomData roomData = GameManager.GetCurrentLayer().GetComponent<LocalRoomData>();
                    Debug.Log("[CASE 3] Brick destroyed. brickCount == " + roomData.numberOfBricks);*/
                }
                DestroyBlock(scoreMult);
            } else {
                GameManager.CurrentScore += (scoreValue * GameManager.scoreMult) / 2;
                ScoreSpawn((scoreValue * GameManager.scoreMult) / 2);
            }
        }
    }

    private void DestroyBlock(int scoreMult) {
        float rand = Random.Range(0f, 100f);
        //Debug.Log("Chance: " + rand);
        if (rand < powerUpOdds && !CameraShake) {
            //Debug.Log("Got a power up");
            GameManager.Instance.PowerUpSpawn(rand, transform.position);
        }

        int score = (scoreValue * scoreMult) * (GameManager.scoreMult);
        ScoreSpawn(score);
        //Debug.Log("Score Given: " + scoreValue + " * " + scoreMult + " = " + score);

        if (breakEffectPrefab != null) {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void ScoreSpawn(int score) {
        if (GameManager.Instance != null) {
            GameManager.CurrentScore += score;
            ScoreNumberController.instance.SpawnScore(score, transform.position);

            GameManager.CanSpawnBall = false;
        }
    }

    private IEnumerator AssignParentCoroutine() {
        yield return new WaitForEndOfFrame();

        if (transform.parent != null && transform.parent.name == "BlockList") {
            yield break; // Already properly parented
        }

        // Find closest BlockList by checking all active ones in the scene
        GameObject[] allBlockLists = GameObject.FindGameObjectsWithTag("BlockList");
        GameObject closestBlockList = null;
        float closestDistance = Mathf.Infinity;
        foreach (GameObject blockList in allBlockLists) {
            if (blockList == null) continue;
            float distance = Vector3.Distance(transform.position, blockList.transform.position);
            if (distance < closestDistance) {
                closestDistance = distance;
                closestBlockList = blockList;
            }
        }

        if (closestBlockList != null) {
            transform.SetParent(closestBlockList.transform);
        } else {
            Debug.LogWarning("No BlockList found for parenting!");
        }

        if (transform.parent != null) {
            bool isTooLow = transform.position.y < (transform.parent.position.y - 2f);
            bool isTooHigh = false;
            if (transform.parent.parent != null) {
                float roofY = float.MaxValue;
                Transform room = transform.parent.parent;
                // Find object whose name starts with "Roof" in room children
                foreach (Transform child in room) {
                    if (child.name.StartsWith("Roof", System.StringComparison.OrdinalIgnoreCase)) {
                        roofY = child.position.y;
                        break;
                    }
                }
                if (transform.position.y > roofY) {
                    isTooHigh = true;
                }
            }
            if (isTooLow || isTooHigh) {
                //GetComponent<SpriteRenderer>().color = Color.red;
                gameObject.SetActive(false);
                //Debug.Log("[Brick Dist.] Brick too far below parent: " + (transform.parent.position.y - transform.position.y) + " units lower");
            }
        } else {
            Debug.LogWarning("[Brick Dist.] No parent assigned to brick: " + gameObject.name);
        }
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Brick")) {
            //GetComponent<SpriteRenderer>().color = Color.red;
            gameObject.SetActive(false);
        } else {
            Debug.Log("[Brick] Destroyed by overlapping with: " + collision.gameObject.name);
            Destroy(gameObject);
        }
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        Destroy(gameObject);
    }
}