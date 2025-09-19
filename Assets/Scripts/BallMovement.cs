using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using static Unity.Collections.AllocatorManager;
using static UnityEngine.Rendering.DebugUI;

public class BallMovement : MonoBehaviour {

    [Header("Movement")]
    public Vector2 moveDir = Vector2.up; // Direction the ball is currently moving
    public float targetSpeed = 5f; // Target speed ball moves at
    public float currentSpeed; // Reference to ball's speed
    private float savedSpeed = 5f; // The ball's speed before and after getting grabbed
    public float accelerationDrag = 1.8f; // Speed ball accelerates/decellerates at when moving freely
    public float gravityBallReduction = 8f;
    public float terminalFallSpeed = 12f;
    private float paddleOffsetX = 0f; // The X position of the ball when it hits the paddle

    [Header("Sticking")]
    public bool isStuckToPaddle = false;
    public Transform stickTarget; // The object the ball is currently sticking to
    private float stickOffsetX = 0f; // X offset from the target object
    private float stickOffsetY = 1.8f; // Default Y offset from the target object

    [Header("Collision")]
    public GameObject paddle; // Reference to the paddle
    public float paddleOffset; // The x point where the ball hits the paddle
    public float offset = 1.8f; // The distance between ball & paddle at the start of a game
    public float deathPlane = -6f; // The height the ball dies at
    private float floorAngleThreshold = 30f; // Threshold to check if collision is against a wall or floor
    private bool collisionProcessedThisFrame = false;

    [Header("Other")]
    public int scoreMult;
    public bool ceilingBreak = false;

    [Header("Transition Freeze")]
    public bool isHorizontalTransitioning = false;

    #region Initialization
    private void Awake() {
        paddle = GameObject.Find("Paddle");
        GameManager.RegisterBall(gameObject); // Register this ball with the GameManager
        currentSpeed = GameManager.BallSpeed;
    }

    private void OnDestroy() { // Unregister this ball when destroyed
        GameManager.UnregisterBall(gameObject);
    }

    public void InitializeBall(Vector3 direction) {
        moveDir = direction;
    }
    #endregion

    private void Update() {
        bool grabPaddle = paddle.GetComponent<PaddleMove>().grabPaddle;
        boardTransition();
        StickManager();

        if (GameManager.IsGameStart) { // If we haven't already set the stick target, set it to the paddle
            if (stickTarget == null && paddle != null) {
                stickTarget = paddle.transform;
                stickOffsetX = 0f;
                stickOffsetY = offset;
            }
            StickToPaddle();
        } else {
            MoveBall();
            CheckPaddleCollision();
            AdjustSpeed();
        }

        CheckDeathPlane();
        ClampMoveDirection();
        bounceCheck();
        GravityBallManager();

        if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
            moveDir = Vector2.zero;
        }

         if (GameManager.isTransitioning) {
            Vector2 savedMoveDir = moveDir;
            savedSpeed = currentSpeed;
            moveDir = Vector2.zero;
            currentSpeed = 0f;
            Debug.Log("[NEW TEST] SavedMoveDir: " + savedMoveDir + ", new moveDir: " + moveDir);
         }
    }

    

    #region Ball Movement Helpers
    private void MoveBall() {
        if (GameManager.isTransitioning || isHorizontalTransitioning) return;

        if (moveDir != Vector2.zero) {
            transform.position += new Vector3(moveDir.x, moveDir.y, 0f).normalized * currentSpeed * Time.deltaTime;
        }

        if (currentSpeed >= 5 && currentSpeed <= 8) {
            accelerationDrag = 1.8f;
        }
    }

    private void AdjustSpeed() {
        if (GameManager.GravityBall) return;

        if (currentSpeed > targetSpeed) {
            currentSpeed = Mathf.Max(targetSpeed, currentSpeed - accelerationDrag * Time.deltaTime);
        } else if (currentSpeed < targetSpeed) {
            currentSpeed = Mathf.Min(targetSpeed, currentSpeed + accelerationDrag * Time.deltaTime);
        }
    }

    private void ClampMoveDirection() {
        if (moveDir.y > 0) {
            moveDir.y = 1f;
        }
        if (moveDir.y < 0) {
            moveDir.y = -1f;
        }
    }
    #endregion


    #region Stick Management
    public void StickManager() {
        if (isStuckToPaddle && paddle.GetComponent<PaddleMove>().anyBallStuck) {
            if (currentSpeed > 0f) {
                savedSpeed = currentSpeed;
            }

            currentSpeed = 0f;
            moveDir.y = 1f;
            StickToPaddle();
            GameManager.GravityBall = false;

            if ((Input.GetMouseButtonDown(0) && stickTarget == paddle.transform)) {
                ReleaseFromStick();
            }
            return;
        }
    }

    public void ReleaseFromStick() {
        isStuckToPaddle = false;
        currentSpeed = savedSpeed > 0 ? savedSpeed : targetSpeed;
        stickTarget = null;
    }

    public void StickToObject(Transform target, float offsetX = 0f, float offsetY = 1.8f) {
        isStuckToPaddle = true;
        stickTarget = target;
        stickOffsetX = offsetX;
        stickOffsetY = offsetY;
        currentSpeed = 0f;
    }

    private void StickToPaddle() {
        if (stickTarget == null) return;

        moveDir.y = 1;
        Vector3 targetPos = stickTarget.position;
        transform.position = new Vector3(targetPos.x + stickOffsetX, targetPos.y + stickOffsetY, 0);

        /*moveDir.y = 1;
        transform.position = new Vector3(paddle.transform.position.x + paddleOffsetX, paddle.transform.position.y + offset, 0);*/
    }
    #endregion


    #region Transitions
    public void boardTransition() {
        if (GameManager.hasLoadedNextLevel || GameManager.isRemovingBoard) {
            // Debug.Log("[boardTransition] Skipped due to level load/board removal.");
            return;
        }

        if (paddle == null) return;
        PaddleMove paddleMove = paddle.GetComponent<PaddleMove>();
        if (paddleMove == null) return;

        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) {
            Debug.LogWarning("[boardTransition] No currentLayer found. Aborting.");
            return;
        }

        LocalRoomData roomData = currentLayer.GetComponent<LocalRoomData>();
        if (roomData == null) {
            Debug.LogWarning("[boardTransition] No LocalRoomData on currentLayer. Aborting.");
            return;
        }

        if (GameManager.levelLayers == null) {
            Debug.LogWarning("[boardTransition] levelLayers is null. Aborting.");
            return;
        }

        int totalRows = GameManager.levelLayers.GetLength(0);

        // BOUNCE DOWN if roof is scrolling and ball is too high
        if (transform.position.y > paddleMove.baseYPos + 9.38f && (GameManager.isShiftingDown || GameManager.levelLayers[GameManager.currentBoardRow + 1, GameManager.currentBoardColumn] == null)) {
            Debug.Log("[Rebounding] Rebounding now during roof scroll");
            moveDir.y = -1;
        } else if (transform.position.y > paddleMove.baseYPos + 10 && GameManager.currentBoardRow + 1 < totalRows && !GameManager.isShiftingDown) { // Try to move UP a board (only if roof is NOT scrolling)
            GameManager.isTransitioning = true;
            Debug.Log("GOING UP");

            GameManager.currentBoardRow += 1;
            paddleMove.baseYPos += 10;
            paddleMove.currentYPos += 10;
            paddleMove.maxFlipHeight += 10;

            Vector2 roomAbove = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);
            UpdateRoomHistory(roomAbove);
            GameManager.isTransitioning = false;
        } else if (transform.position.y < paddleMove.baseYPos - 1.1f && GameManager.currentBoardRow > 0) { // Try to move DOWN a board - Only allow if roof is NOT scrolling

            if (!GameManager.isShiftingDown) {

                GameObject roomBelowObj = GameManager.levelLayers[GameManager.currentBoardRow - 1, GameManager.currentBoardColumn];
                if (roomBelowObj != null) {
                    LocalRoomData roomBelowData = roomBelowObj.GetComponent<LocalRoomData>();
                    if (roomBelowData != null && roomBelowData.localRoomData.y > 0) {
                        // Room below has Y > 0, destroy the ball
                        Debug.Log("Ball destroyed - room below has Y > 0");
                        Destroy(gameObject);
                        return;
                    }
                }

                GameManager.isTransitioning = true;
                Debug.Log("GOING DOWN");

                GameManager.currentBoardRow -= 1;
                paddleMove.baseYPos -= 10;
                paddleMove.currentYPos -= 10;
                paddleMove.maxFlipHeight -= 10;

                Vector2 roomBelow = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);
                UpdateRoomHistory(roomBelow);
                GameManager.isTransitioning = false;
            } else {
                Destroy(gameObject);
            }
        }
        // Reset position if Vertically out of bounds - Only allow if roof is NOT scrolling
        if (transform.position.y > paddleMove.baseYPos + 10 && GameManager.currentBoardRow == totalRows - 1 && !GameManager.isShiftingDown && !GameManager.isTransitioning) {
            transform.position = new Vector3(0, currentLayer.transform.position.y);
            moveDir.y = -1;
            moveDir.x = 0;
        }

        // EARLY RETURN FOR ROOF SCROLLING - After handling upward rebound
        if (GameManager.isShiftingDown) {
            return; // Don't process horizontal transitions during roof scroll
        }

        float roomCenterX = currentLayer.transform.position.x;
        float roomHalfWidth = 7.3f + roomData.localRoomData.x;

        // If outside of Horizontally out of bounds - Right Side
        if ((transform.position.x >= roomCenterX + roomHalfWidth + 1f) && !GameManager.ceilinigDestroyed) {
            // FREEZE THE BALL FOR BOUNDARY TRANSITIONS TOO
            isHorizontalTransitioning = true;

            // If roof is moving, cancel it
            if (GameManager.isShiftingDown) {
                Debug.Log("[boardTransition] Canceling roof scroll for right transition");
                //GameManager.CancelRoofTransition();
                GameManager.needsFastRoofScroll = true;
                GameManager.lastHorizontalTransitionTime = Time.time;
            }

            if (!GameManager.isTransitioning) {
                transform.position = new Vector3(roomCenterX, transform.position.y);
                moveDir.y = -1;
                moveDir.x = 0;
            } else if (GameManager.currentBoardColumn < 2) {
                GameManager.currentBoardColumn++;
                // --- TELEPORT THE PADDLE ---
                if (paddleMove != null) {
                    GameObject newRoom = GameManager.levelLayers[GameManager.currentBoardRow, GameManager.currentBoardColumn];
                    Transform leftWall = null;
                    Transform rightWall = null;

                    foreach (Transform child in newRoom.transform) {
                        if (child.name.StartsWith("LWall")) leftWall = child;
                        else if (child.name.StartsWith("RWall")) rightWall = child;
                    }

                    if (leftWall != null && rightWall != null) {
                        roomCenterX = (leftWall.position.x + rightWall.position.x) / 2f;
                        paddleMove.transform.position = new Vector3(roomCenterX, paddleMove.currentYPos, 0f);
                    }

                    Vector2 roomToTheSide = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);
                    UpdateRoomHistory(roomToTheSide);
                }
            }

            // Set preset direction based on which side we're transitioning from
            if (transform.position.x >= roomCenterX + roomHalfWidth + 1f) { // Right side transition
                moveDir = new Vector2(-0.5f, 1f);
            } else { // Left side transition
                moveDir = new Vector2(0.5f, 1f);
            }
            // Reset speed to normal
            currentSpeed = targetSpeed;
        }

        if ((transform.position.x <= roomCenterX - roomHalfWidth - 1f) && !GameManager.ceilinigDestroyed) { // Left Side
            // FREEZE THE BALL FOR BOUNDARY TRANSITIONS TOO
            isHorizontalTransitioning = true;

            // If roof is moving, cancel it
            if (GameManager.isShiftingDown) {
                Debug.Log("[boardTransition] Canceling roof scroll for left transition");
                //GameManager.CancelRoofTransition();
            }

            if (!GameManager.isTransitioning) {
                transform.position = new Vector3(roomCenterX, transform.position.y);
                moveDir.y = -1;
                moveDir.x = 0;
            } else if (GameManager.currentBoardColumn > 0) {
                GameManager.currentBoardColumn--;
                // --- TELEPORT THE PADDLE ---
                if (paddleMove != null) {
                    GameObject newRoom = GameManager.levelLayers[GameManager.currentBoardRow, GameManager.currentBoardColumn];
                    Transform leftWall = null;
                    Transform rightWall = null;

                    foreach (Transform child in newRoom.transform) {
                        if (child.name.StartsWith("LWall")) leftWall = child;
                        else if (child.name.StartsWith("RWall")) rightWall = child;
                    }

                    if (leftWall != null && rightWall != null) {
                        roomCenterX = (leftWall.position.x + rightWall.position.x) / 2f;
                        paddleMove.transform.position = new Vector3(roomCenterX, paddleMove.currentYPos, 0f);
                    }

                    Vector2 roomToTheSide = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);
                    UpdateRoomHistory(roomToTheSide);
                }
            }
            // Set preset direction based on which side we're transitioning from
            if (transform.position.x >= roomCenterX + roomHalfWidth + 1f) { // Right side transition
                moveDir = new Vector2(-0.5f, 1f);
            } else { // Left side transition
                moveDir = new Vector2(0.5f, 1f);
            }
            // Reset speed to normal
            currentSpeed = targetSpeed;
        }

        // --- Update brick container while ignoring Z3 bricks ---
        Transform blockListTransform = currentLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("BlockList"));
        if (blockListTransform != null) {
            GameManager.brickContainer = blockListTransform.gameObject;
        }
    }

    private void UpdateRoomHistory(Vector2 newRoom) {
        if (GameManager.RoomHistory == null) {
            GameManager.RoomHistory = new Queue<Vector2>();
        }
        // Keep only the last 2 rooms
        if (GameManager.RoomHistory.Count >= 2) {
            GameManager.RoomHistory.Dequeue();
        }
        // Add new room if it's different from the most recent
        if (GameManager.RoomHistory.Count == 0 || GameManager.RoomHistory.Last() != newRoom) {
            GameManager.RoomHistory.Enqueue(newRoom);
            Debug.Log($"[Room History] Added room [{newRoom.x}, {newRoom.y}]. Count: {GameManager.RoomHistory.Count}");
        }
    }
    #endregion


    #region Collisions
    private void CheckPaddleCollision() {
        bool inVerticalRange = transform.position.y < paddle.transform.position.y + 0.13f && transform.position.y > paddle.transform.position.y - 0.13f;
        bool inHorizontalRange = transform.position.x > paddle.transform.position.x - paddle.transform.localScale.x / 2.2f && transform.position.x < paddle.transform.position.x + paddle.transform.localScale.x / 2.2f;
        //Debug.Log("Between: " + (paddle.transform.position.x - paddle.transform.localScale.x / 2.2f) + " & " + paddle.transform.position.x + paddle.transform.localScale.x / 2.2f);

        if (inVerticalRange && inHorizontalRange && moveDir.y == -1) {
            //Debug.Log("Collided with: Paddle");
            if (transform.position.x > paddle.transform.position.x - paddle.transform.localScale.x / 2.9f && transform.position.x < paddle.transform.position.x + paddle.transform.localScale.x / 2.9f) {
                if (paddle.GetComponent<PaddleMove>().grabPaddle /*&& paddle.transform.position.y <= -4f*/ && !paddle.GetComponent<PaddleMove>().flipping) {
                    isStuckToPaddle = true;
                    stickTarget = paddle.transform;
                    stickOffsetX = transform.position.x - paddle.transform.position.x;
                    stickOffsetY = offset; // Maintain the same vertical spacing
                }
            }
            paddleOffsetX = transform.position.x - paddle.transform.position.x;
            PaddleBounce();
        }
    }

    private void CheckDeathPlane() {
        if (transform.position.y <= deathPlane && !GameManager.isShiftingDown && !GameManager.isTransitioning) {
            Destroy(gameObject);
        }
    }

    private void bounceCheck() {
        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) return;

        LocalRoomData roomData = currentLayer.GetComponent<LocalRoomData>();
        if (roomData == null) return;

        // Use the room's actual position for boundary calculation
        Vector3 roomPosition = currentLayer.transform.position;
        float halfWidth = roomData.localRoomData.x + 7.3f;

        // Boundaries relative to the room's center position
        float leftBoundary = roomPosition.x - halfWidth;
        float rightBoundary = roomPosition.x + halfWidth;

        if (transform.position.x >= rightBoundary || transform.position.x <= leftBoundary) {
            Debug.Log($"Bounce! posX={transform.position.x}, bounds=({leftBoundary}, {rightBoundary}), roomPosX={roomPosition.x}");
            bounce(true);
        }
    }

    public void HandleRoofCollision() {
        // Count active balls
        int activeBallCount = 0;
        foreach (var ball in GameManager.ActiveBalls) {
            if (ball != null && ball.gameObject != null && ball.gameObject.activeInHierarchy) {
                activeBallCount++;
            }
        }

        // Get the current board GameObject
        GameObject currentBoard = GameManager.GetLayer(GameManager.currentBoardRow, GameManager.currentBoardColumn);
        if (currentBoard == null) {
            Debug.LogWarning("No current board found.");
            return;
        }

        // Read its localRoomData.z value
        LocalRoomData roomData = currentBoard.GetComponent<LocalRoomData>();
        float boardZ;
        if (roomData != null) {
            boardZ = roomData.localRoomData.z;
        } else {
            boardZ = 0f;
        }

        // Get the first digit of the absolute value of Z
        float absZ = Mathf.Abs(boardZ);
        while (absZ >= 10f) absZ /= 10f;
        int firstDigit = (int)Mathf.Floor(absZ);

        bool zStartsWithOne = false;
        if (firstDigit == 1) {
            zStartsWithOne = true;
        }
        if (zStartsWithOne) {
            if (activeBallCount > 1) {
                gameObject.SetActive(false);
            } else {
                GameManager.Instance.StartCoroutine(GameManager.Instance.RemoveCurrentBoard(1));
            }
        } else {
            Debug.Log("HIT ROOF SHOULD DESTROY");
            Destroy(gameObject);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision) {
        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) return;
        if (collision.contactCount == 0 || collisionProcessedThisFrame) return;
        collisionProcessedThisFrame = true;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal;
        float angle = Vector2.Angle(normal, Vector2.up);

        // RoofPiece collision check
        if (collision.gameObject.name.StartsWith("RoofPiece")) {
            Transform boardTransform = collision.transform.parent; // RoofPiece parent = Board

            if (boardTransform != null && boardTransform.gameObject != currentLayer) {
                BoxCollider2D roofCollider = collision.collider as BoxCollider2D;
                float roofTopY;
                if (roofCollider != null) {
                    roofTopY = roofCollider.bounds.max.y;
                } else {
                    roofTopY = collision.transform.position.y;
                }

                float tolerance = 0.15f; // small buffer for floating-point / physics imprecision
                if (transform.position.y >= roofTopY - tolerance) {
                    Debug.Log("HIT ROOF from previous board: " + boardTransform.name);
                    HandleRoofCollision();
                }
            }
        }

        if (collision.gameObject.CompareTag("Block") || collision.gameObject.CompareTag("Brick")) {
            HandleBlockCollision(collision.gameObject);
        }
        if (!collision.gameObject.CompareTag("Player") && (!collision.gameObject.CompareTag("Brick") || !GameManager.BrickThu)) {
            HandleBouncePhysics(angle, collision.gameObject);
        }
        if (gameObject.activeInHierarchy) {
            StartCoroutine(ResetCollisionFlag());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("XTransition") && !GameManager.IsGameStart && !isStuckToPaddle) {
            Debug.Log("Hitting XTrans");

            // FREEZE THE BALL IMMEDIATELY FOR HORIZONTAL TRANSITION
            isHorizontalTransitioning = true;

            // CANCEL ROOF SCROLL HERE - This is the key fix!
            if (GameManager.isShiftingDown) {
                Debug.Log("[XTransition] Canceling roof scroll before horizontal transition");
                //GameManager.CancelRoofTransition();
            }

            // MARK THAT WE NEED FAST SCROLLING AFTER HORIZONTAL TRANSITION
            GameManager.needsFastRoofScroll = true;
            GameManager.lastHorizontalTransitionTime = Time.time;

            GameManager.isTransitioning = true;
            Debug.Log("[HRT]: " + GameManager.isTransitioning);

            LocalRoomData roomData = GameManager.GetCurrentLayer().GetComponent<LocalRoomData>();
            Debug.Log("[CASE 3] XTrans triggered. Pre-Brick Count == " + roomData.numberOfBricks);

            XTransition currentTrans = collision.gameObject.GetComponent<XTransition>();
            GameObject partner = currentTrans.partnerTransition;
            if (currentTrans == null || partner == null) return;
            
            int destinationColumn = (int)currentTrans.transition;
            Debug.Log("destinationColumn: " + destinationColumn + ", currentTrans: " + currentTrans + ", partner: " + partner);

            // Properly find which room contains the partner transition
            bool foundPartner = false;
            for (int row = 0; row < GameManager.levelLayers.GetLength(0); row++) {
                for (int col = 0; col < GameManager.levelLayers.GetLength(1); col++) {
                    GameObject layer = GameManager.levelLayers[row, col];
                    if (layer != null && partner.transform.IsChildOf(layer.transform)) {
                        GameManager.currentBoardRow = row;
                        GameManager.currentBoardColumn = col;
                        foundPartner = true;
                        Debug.Log($"Found partner at [{row}, {col}]");
                        break;
                    }
                }
                if (foundPartner) break;
            }

            if (!foundPartner) {
                isHorizontalTransitioning = false;
                return;
            }

            Debug.Log("Now in Position: [" + GameManager.currentBoardRow + ", " + GameManager.currentBoardColumn + "]");
            roomData = GameManager.GetCurrentLayer().GetComponent<LocalRoomData>();
            Debug.Log("[CASE 3] XTrans triggered. Post-Brick Count == " + roomData.numberOfBricks);

            // --- TELEPORT THE BALL ---
            if (transform.position.x < partner.transform.position.x) { // Coming from left
                transform.position = new Vector3(partner.transform.position.x + 1f, partner.transform.position.y, 0f);
                moveDir = new Vector2(0.5f, 1f); // Set the preset direction
            } else { // Coming from right
                transform.position = new Vector3(partner.transform.position.x - 1f, partner.transform.position.y, 0f);
                moveDir = new Vector2(-0.5f, 1f); // Set the preset direction
            }
            Debug.Log("Warped ball to: " + transform.position);

            // --- TELEPORT THE PADDLE ---
            PaddleMove paddleMove = paddle.GetComponent<PaddleMove>();
            if (paddleMove != null) {
                GameObject newRoom = GameManager.levelLayers[GameManager.currentBoardRow, GameManager.currentBoardColumn];
                Transform leftWall = null;
                Transform rightWall = null;

                foreach (Transform child in newRoom.transform) {
                    if (child.name.StartsWith("LWall")) leftWall = child;
                    else if (child.name.StartsWith("RWall")) rightWall = child;
                }

                if (leftWall != null && rightWall != null) {
                    float roomCenterX = (leftWall.position.x + rightWall.position.x) / 2f;
                    paddleMove.transform.position = new Vector3(roomCenterX, paddleMove.currentYPos, 0f);
                }
            }

            // --- RoomHistory management ---
            if (GameManager.RoomHistory == null) {
                GameManager.RoomHistory = new Queue<Vector2>();
            }

            Vector2 currentRoom = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);

            // Keep only the last 2 rooms (current and previous)
            if (GameManager.RoomHistory.Count >= 2) { // Remove oldest room to maintain size of 2
                GameManager.RoomHistory.Dequeue();
            }

            // Add current room if it's different from the most recent
            if (GameManager.RoomHistory.Count == 0 || GameManager.RoomHistory.Last() != currentRoom) {
                GameManager.RoomHistory.Enqueue(currentRoom);
                Debug.Log($"[Room History] Added room [{currentRoom.x}, {currentRoom.y}]. Count: {GameManager.RoomHistory.Count}");
            } else {
                Debug.Log($"[Room History] Room {currentRoom} already most recent, skipping add.");
            }
            
            GameManager.isTransitioning = false;


            // START COROUTINE TO UNFREEZE WHEN CAMERA CATCHES UP
            isHorizontalTransitioning = false;
            Debug.Log("[HRT]: " + GameManager.isTransitioning);
        }
    }
    
    private IEnumerator ResetCollisionFlag() {
        yield return new WaitForEndOfFrame();
        collisionProcessedThisFrame = false;
    }
    #endregion

    
    #region Power Ups
    public void FastBall() {
        GameManager.GravityBall = false;
        currentSpeed = 14f;
        accelerationDrag = 0.2f;
    }

    public void SlowBall() {
        currentSpeed = 1f;
        accelerationDrag = 0.2f;
        GameManager.BrickThu = false;
        GameManager.FireBall = false;
        GameManager.GravityBall = false;
    }

    public void MegaBall() {
        transform.localScale = new Vector3(transform.localScale.x * 2, transform.localScale.y * 2, 1f);
        GameManager.GravityBall = false;
    }

    public void ShrinkBall() {
        transform.localScale = new Vector3(transform.localScale.x / 2, transform.localScale.y / 2, 1f);
        //x2 score mult
        GameManager.scoreMult = 2;
        GameManager.GravityBall = false;
    }

    public void GravityBallManager() {
        if (currentSpeed <= 0.1f && moveDir.y > 0) {
            moveDir.y = -1f;
            if (!GameManager.GravityBall) {
                currentSpeed = 3f;
            }
        }

        if (GameManager.GravityBall && !GameManager.IsGameStart) {
            GravityBall();
        }
    }

    public void GravityBall() {
        if (moveDir.y > 0) {
            // Reduce speed more gradually when moving upward
            currentSpeed -= gravityBallReduction * Time.deltaTime;

            if (currentSpeed <= 0.1f) {
                moveDir.y = -1;
                currentSpeed = 0.1f;
            }
        } else if (moveDir.y < 0) {
            // Increase speed when falling, but preserve some energy loss
            currentSpeed += gravityBallReduction * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, terminalFallSpeed);
        }
    }
    #endregion


    #region Bounce / Collision Helpers
    private void bounce(bool horizontal) {
        //Debug.Log("Collided with: Bounce Function");
        if (!GameManager.isTransitioning) {
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
        }
        if (horizontal) {
            moveDir.x *= -1;
        } else {
            moveDir.y *= -1;
        }
    }

    private void HandleBlockCollision(GameObject collidedObject)
    {
        if (currentSpeed > targetSpeed)
        {
            scoreMult = (int)(currentSpeed - 3);
        }
        else
        {
            scoreMult = 2;
        }

        if (GameManager.FireBall)
        {
            // Fireball logic
        }

        collidedObject.GetComponent<ObjHealth>().TakeDamage(1, scoreMult, currentSpeed);
        if (collidedObject.CompareTag("Brick"))
        {
            //Debug.Log("Collided with: " + collidedObject.name);
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
        }
    }

    private void HandleBouncePhysics(float angle, GameObject collidedObject)
    {
        if (!collidedObject.CompareTag("Player"))
        {
            //Debug.Log("Collided with: " + collidedObject.name);
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);

            if (angle <= floorAngleThreshold || angle >= 180f - floorAngleThreshold)
            {
                moveDir.y *= -1;
            }
            else
            {
                moveDir.x *= -1;
            }
        }
    }

    public void PaddleBounce() {
        //Debug.Log("Collided with: Paddle");
        GameManager.Instance.PlaySFX(GameManager.Instance.paddleBounceSound);

        paddleOffset = paddle.transform.position.x - transform.position.x;
        moveDir.y *= -1;
        moveDir.x = -(paddleOffset / 1.1f);

        if (paddle.transform.position.y != -3.8f && paddle.GetComponent<PaddleMove>().flipping) {
            transform.position = new Vector3(transform.position.x, paddle.transform.position.y, 0);
        }

        if (paddle.GetComponent<PaddleMove>().recoilSpeed > 5) {
            currentSpeed = paddle.GetComponent<PaddleMove>().recoilSpeed;
            accelerationDrag = 1.8f;
        }

        if (currentSpeed > 14f && paddle.GetComponent<PaddleMove>().flipping && paddle.GetComponent<PaddleMove>().recoilSpeed >= 14f) {
            GameManager.Instance.PlaySFX(GameManager.Instance.perfectFlipSound);
            //Debug.Log($"Speed: {currentSpeed} PERFECT!!!");
        } else {
            //Debug.Log($"Speed: {currentSpeed}");
        }
    }
    #endregion
}