using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapBall : MonoBehaviour {

    private Transform playerInRange;
    private MapShip playerMapShipInRange;
    private bool playerIsInTrigger = false;

    public Vector2 moveDir = new Vector2(0f, 1f);
    public float currentSpeed = 0f;

    public void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player") && collision.gameObject.GetComponent<MapShip>().flipping) {
            moveDir = (transform.position - collision.transform.position).normalized;
            currentSpeed = 5f;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            playerInRange = collision.transform;
            playerMapShipInRange = collision.GetComponent<MapShip>();
            playerIsInTrigger = true;
            //Debug.Log("Player entered trigger");
        }
    }

    public void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            playerIsInTrigger = false;
            playerInRange = null;
            playerMapShipInRange = null;
            //Debug.Log("Player left trigger");
        }
    }

    void Update() {
        if (playerIsInTrigger && playerInRange != null && playerMapShipInRange != null) {
            if (playerMapShipInRange.IsRightClickPressed() && Vector3.Distance(transform.position, playerInRange.position) > playerMapShipInRange.minRange) {
                var step = playerMapShipInRange.strength * Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, playerInRange.position, step);
                //Debug.Log("Moving towards player");
            }
        }

        if (currentSpeed > 0) {
            transform.position += new Vector3(moveDir.x, moveDir.y, 0f).normalized * currentSpeed * Time.deltaTime;
            currentSpeed -= 2f * Time.deltaTime;
        }
        if (currentSpeed < 1f) {
            currentSpeed = 0f;
        }
    }
}