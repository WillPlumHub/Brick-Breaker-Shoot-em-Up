using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapShip : MonoBehaviour {

    public enum ControlType { Mouse, Keyboard, Gamepad }
    [Header("Control Settings")]
    public ControlType controlType = ControlType.Mouse;

    [Header("Movement Settings")]
    public float currentSpeed = 0f;
    public float moveSpeed = 5f;
    public bool moving = false;
    public float flipLength = 0.5f;
    public float burstSpeed = 20f;
    public bool flipping = false;
    private float speedSmoothTime = 0.5f;
    private float movementSpeedTimer = 0f;
    public bool disabled = false;

    [Header("Camera Follow")]
    public float cameraFollowSpeed = 5f;
    private Vector3 cameraOffset = new Vector3(0, 0, -10f);
    private Transform cameraTransform;

    [Header("Idle Camera Recentering")]
    public float idleThreshold = 0.3f; // Time before recentring
    public float recenterSpeed = 4f; // How quickly the camera recenters when idle
    private float idleTimer = 0f;

    private float followLerpFactor = 0f;
    private float followRampUpTime = 1f; // Time it takes to reach full follow speed
    private float currentFollowTime = 0f;

    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private Vector3 inputPosition;

    private bool inputDown = false;
    private bool inputUp = false;
    private bool inputHeld = false;
    private float inputCheckCooldown = 0.1f;
    private float lastInputCheckTime = 0f;

    [Header("Magnet")]
    public float strength = 10;
    public float minRange = 1;

    void Start() {
        originalPosition = transform.position;
        targetPosition = originalPosition;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void Update() {
        HandleInput();
        aboutFace();
        movement();
        cameraFollow();
        AdjustTriggerToCameraEdges();
    }

    void DetectControlType() {
        if (Time.time - lastInputCheckTime < inputCheckCooldown) return;

        // Mouse input
        if (Input.GetMouseButtonDown(0) || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0) {
            controlType = ControlType.Mouse;
        } else if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0 || Input.GetButton("Jump")) {
            if (Input.GetJoystickNames().Length > 0) {
                controlType = ControlType.Gamepad;
            } else {
                controlType = ControlType.Keyboard;
            }
        }
        lastInputCheckTime = Time.time;
    }

    void HandleInput() {
        DetectControlType();

        inputDown = false;
        inputUp = false;
        inputHeld = false;

        // --- Flip Input ---
        bool flipDown = Input.GetMouseButtonDown(0)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.JoystickButton2)
            || Input.GetKeyDown(KeyCode.JoystickButton1)
            || Input.GetKeyDown(KeyCode.JoystickButton6);

        bool flipUp = Input.GetMouseButtonUp(0)
            || Input.GetKeyUp(KeyCode.Space)
            || Input.GetKeyUp(KeyCode.JoystickButton2)
            || Input.GetKeyUp(KeyCode.JoystickButton1)
            || Input.GetKeyUp(KeyCode.JoystickButton6);

        bool flipHeld = Input.GetMouseButton(0)
            || Input.GetKey(KeyCode.Space)
            || Input.GetKey(KeyCode.JoystickButton2)
            || Input.GetKey(KeyCode.JoystickButton1)
            || Input.GetKey(KeyCode.JoystickButton6);

        inputDown = flipDown;
        inputUp = flipUp;
        inputHeld = flipHeld;

        IsRightClickPressed();
        
        // --- Input Direction ---
        Vector2 direction = Vector2.zero;

        // Mouse position
        if (Input.mousePresent) {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = transform.position.z;
            direction = (mouseWorld - transform.position).normalized;
            inputPosition = mouseWorld;
        }

        // Keyboard/Controller input
        Vector2 axisInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (axisInput.sqrMagnitude > 0.01f) {
            direction = axisInput.normalized;
            inputPosition = transform.position + (Vector3)(direction * flipLength);
        }

        // Call general movement logic
        generalMove();
    }

    public void generalMove() {
        if (inputDown && !disabled) {
            idleTimer = 0f;
            originalPosition = transform.position;

            // Start flip
            Vector3 dir = (inputPosition - originalPosition).normalized;
            targetPosition = originalPosition + dir * flipLength;

            currentSpeed = burstSpeed;
            movementSpeedTimer = 0f;

            flipping = true;
            moving = false;
        }

        // Start moving directly if in trigger and input is held
        if (disabled && inputHeld && !flipping && !moving) {
            idleTimer = 0f;
            movementSpeedTimer = 0f;
            moving = true;
        }

        if (flipping && inputUp) { // Handle early release during flip
            idleTimer = 0f;
            movementSpeedTimer = 0f;

            // Cancel flip, return to original
            targetPosition = originalPosition;
            currentSpeed = burstSpeed;
        }
        
        if (flipping && Vector3.Distance(transform.position, targetPosition) < 0.01f) { // Detect flip completion
            flipping = false;

            if (inputHeld) { // Begin movement if still holding
                moving = true;
                movementSpeedTimer = 0f;
            } else { // Flip ended and not holding — stay idle
                moving = false;
            }
        }

        if (moving && inputHeld) { // Movement logic
            idleTimer = 0f;

            movementSpeedTimer += Time.deltaTime;
            float t = Mathf.Clamp01(movementSpeedTimer / speedSmoothTime);
            currentSpeed = Mathf.Lerp(0f, moveSpeed, t);

            targetPosition = inputPosition;
        }
        
        if (moving && inputUp) { // Stop moving on release
            idleTimer = 0f;
            movementSpeedTimer = 0f;
            moving = false;
        }
    }

    public void movement() {
        if (flipping || moving) {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
        }
    }

    public void aboutFace() {
        if (flipping) return; // Don't rotate while flipping

        Vector3 directionToMouse = inputPosition - transform.position;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
        
    public void cameraFollow() {
        if (moving) { // Accelerate camera follow
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

    public void AdjustTriggerToCameraEdges() {
        // Find trigger collider
        BoxCollider2D triggerCollider = null;
        foreach (var col in GetComponents<BoxCollider2D>()) {
            if (col.isTrigger) { triggerCollider = col; break; }
        }
        if (triggerCollider == null) return;

        // Camera bounds
        float camHeight = 47.5f;
        float camWidth = 120f;
        Vector3 camPos = Camera.main.transform.position;

        // Compute distances to each camera edge along forward
        float distTop = Vector3.Dot((new Vector3(transform.position.x, camPos.y + camHeight, 0) - transform.position), transform.up.normalized);
        float distBottom = Vector3.Dot((new Vector3(transform.position.x, camPos.y - camHeight, 0) - transform.position), transform.up.normalized);
        float distRight = Vector3.Dot((new Vector3(camPos.x + camWidth, transform.position.y, 0) - transform.position), transform.up.normalized);
        float distLeft = Vector3.Dot((new Vector3(camPos.x - camWidth, transform.position.y, 0) - transform.position), transform.up.normalized);

        // Pick the maximum positive distance (forward)
        float maxForwardDist = Mathf.Max(distTop, distBottom, distRight, distLeft, 0f);

        // Apply to collider along local Y (forward)
        triggerCollider.size = new Vector2(3f, maxForwardDist);
        triggerCollider.offset = new Vector2(0f, maxForwardDist / 2f);
    }
    public bool IsRightClickPressed() {

        bool MagnetPull = Input.GetMouseButton(1)
            || Input.GetKey(KeyCode.JoystickButton3)
            || Input.GetKey(KeyCode.JoystickButton0)
            || Input.GetKey(KeyCode.JoystickButton7);

        // Assuming you're checking for right mouse button
        return MagnetPull;
    }
}