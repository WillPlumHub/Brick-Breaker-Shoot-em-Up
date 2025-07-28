using System.Collections;
using UnityEngine;

public class MapShip : MonoBehaviour {

    [Header("Movement Settings")]
    public float currentSpeed = 0f;
    public float burstSpeed = 20f;
    public float moveSpeed = 5f;
    public float flipLength = 0.5f;
    private float speedSmoothTime = 0.5f;

    [Header("Camera Follow")]
    public Transform cameraTransform;
    public float cameraFollowSpeed = 5f;
    public Vector3 cameraOffset = new Vector3(0, 0, -10f);

    [Header("Idle Camera Recentering")]
    public float idleThreshold = 3f; // Time before recentring
    public float idleTimer = 0f;
    public float recenterSpeed = 2f; // How quickly the camera recenters when idle

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 mousePosition;

    public bool flipping = false;
    public bool moving = false;

    // At top of class
    public float movementSpeedTimer = 0f;
    public bool inPursuit = false;

    float followLerpFactor = 0f;
    float followRampUpTime = 1f; // Time it takes to reach full follow speed
    float currentFollowTime = 0f;


    void Start() {
        originalPosition = transform.position;
        targetPosition = originalPosition;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update() {
        aboutFace();
        mouseSet();
        mouseMove();
        movement();
        cameraFollow();
    }

    public void aboutFace() {
        Vector3 directionToMouse = mousePosition - transform.position;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    public void mouseSet() {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = transform.position.z;
    }

    public void mouseMove() {
        if (Input.GetMouseButton(0)) {
            idleTimer = 0f;
            if (!flipping) { // Do burst
                Vector3 direction = (mousePosition - originalPosition).normalized;
                targetPosition = originalPosition + direction * flipLength;
                currentSpeed = burstSpeed;
                movementSpeedTimer = 0f;
                inPursuit = false;
            } else { // Start pursuit (if not already)
                if (!inPursuit) {
                    currentSpeed = 0f;
                    movementSpeedTimer = 0f;
                    inPursuit = true;
                }

                currentSpeed = 1f;
                movementSpeedTimer += Time.deltaTime;
                float t = Mathf.Clamp01(movementSpeedTimer / speedSmoothTime);
                currentSpeed = Mathf.Lerp(0f, moveSpeed, t);
                targetPosition = mousePosition;
            }
            moving = true;
        }

        if (Input.GetMouseButtonUp(0)) {
            idleTimer = 0f;
            movementSpeedTimer = 0f;
            inPursuit = false;

            if (!flipping) { // Return to origin
                targetPosition = originalPosition;
                currentSpeed = burstSpeed;
            } else { // Stop in place
                moving = false;
                flipping = false;
                originalPosition = transform.position;
            }
        }
    }

    public void movement() {
        if (moving) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
            if (!flipping && Input.GetMouseButton(0) && Vector3.Distance(transform.position, targetPosition) < 0.01f) {
                flipping = true;
            }
            if (!Input.GetMouseButton(0) && !flipping && Vector3.Distance(transform.position, originalPosition) < 0.01f) {
                moving = false;
            }
        }
    }

    public void cameraFollow() {
        if (flipping) { // Accelerate camera follow
            currentFollowTime += Time.deltaTime;
            followLerpFactor = Mathf.Clamp01(currentFollowTime / followRampUpTime);
            Vector3 desiredCameraPos = transform.position + cameraOffset;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredCameraPos, followLerpFactor * cameraFollowSpeed * Time.deltaTime);
        } else { // Reset ramp-up
            currentFollowTime = 0f;
            followLerpFactor = 0f;

            idleTimer += Time.deltaTime; // Idle camera recenter
            if (idleTimer >= idleThreshold) {
                Vector3 recenterPos = transform.position + cameraOffset;
                cameraTransform.position = Vector3.Lerp(cameraTransform.position, recenterPos, recenterSpeed * Time.deltaTime);
            }
        }
    }
}