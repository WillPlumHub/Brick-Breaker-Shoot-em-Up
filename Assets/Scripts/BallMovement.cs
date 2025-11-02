using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using UnityEngine.UIElements;
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
    public Collider2D curPaddleMagnetVolume;
    public float paddleOffset; // The x point where the ball hits the paddle
    public float offset = 1.8f; // The distance between ball & paddle at the start of a game
    public float deathPlane = -6f; // The height the ball dies at
    private float floorAngleThreshold = 30f; // Threshold to check if collision is against a wall or floor
    private bool collisionProcessedThisFrame = false;

    [Header("Other")]
    public float maxMegaScale = 0.3f;
    public int scoreMult;
    public bool ceilingBreak = false;

    [Header("Transition Freeze")]
    public float VerticalTransTimeUp = 0.5f;
    public float VerticalTransTimeDown = 0.2f;
    public float HorizontalTransTime = 0.5f;
    public bool isHorizontalTransitioning = false;
    private Vector2 velocity = Vector2.zero;

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
        if (GameManager.ActiveBalls[0] != null && gameObject == GameManager.ActiveBalls[0].gameObject) {
            moveDir = direction;
        } else {
            velocity = direction;
        }
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
            //if (IsObjectInViewport() || GameManager.isTransitioning) {
            if (GameManager.ActiveBalls[0] != null && gameObject == GameManager.ActiveBalls[0].gameObject) {
                GetComponent<SpriteRenderer>().color = Color.red;
                MoveBall();
            } else {
                Debug.Log($"{gameObject.name} STATE - Stuck:{isStuckToPaddle}, HorizTrans:{isHorizontalTransitioning}, GMTrans:{GameManager.isTransitioning}, MoveDir:{moveDir}");
                NormalPhysicsUpdate();
            }
            //}
            CheckPaddleCollision();
            roofCheck();
            AdjustSpeed();
        }

        CheckDeathPlane();
        ClampMoveDirection();
        bounceCheck();
        GravityBallManager();

        if (Input.GetKeyDown(KeyCode.KeypadEnter)) {
            moveDir = Vector2.zero;
        }
                
        

    }

    #region Ball Movement
    private void MoveBall() {
        //if (GameManager.isTransitioning || isHorizontalTransitioning) return;

        if (currentSpeed > 30) {
            currentSpeed = 30;
        }

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

    private void NormalPhysicsUpdate() {
        // Initialize velocity from current direction and speed
        if (velocity == Vector2.zero) {
            velocity = moveDir.normalized * currentSpeed;
        }

        // Apply manual gravity
        velocity.y -= 9.81f * Time.deltaTime;

        // Clamp downward speed
        velocity.y = Mathf.Max(velocity.y, -terminalFallSpeed);

        // Move the ball
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // Sync moveDir so CheckPaddleCollision() works
        if (velocity.y > 0.001f) {
            moveDir.y = 1;
        } else {
            moveDir.y = -1;
        }

            // Simple wall bounds (keep your existing code)
            GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer != null) {
            LocalRoomData roomData = currentLayer.GetComponent<LocalRoomData>();
            if (roomData != null) {
                float halfWidth = 7.3f + roomData.localRoomData.x;
                float left = currentLayer.transform.position.x - halfWidth;
                float right = currentLayer.transform.position.x + halfWidth;
                float floorY = paddle.GetComponent<PaddleMove>().baseYPos - 0.5f;

                // Horizontal bounce
                if (transform.position.x < left || transform.position.x > right) {
                    velocity.x *= -1f;
                    moveDir.x *= -1f;
                    transform.position = new Vector3(Mathf.Clamp(transform.position.x, left, right), transform.position.y, 0f);
                }

                if (transform.position.y >= GameManager.currentRoofHeight - 0.25f) {
                    velocity.y *= -1f;
                    moveDir.y *= -1f;
                    transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, GameManager.Paddle.GetComponent<PaddleMove>().baseYPos - 1, GameManager.currentRoofHeight), 0f);
                }

                // Kill ball below floor
                if (transform.position.y < floorY) {
                    Destroy(gameObject);
                }
            }
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
        /*if (transform.position.y > (paddleMove.baseYPos + 9.38f + GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.y) && GameManager.currentBoardRow + 1 < totalRows && (GameManager.isShiftingDown || GameManager.levelLayers[GameManager.currentBoardRow + 1, GameManager.currentBoardColumn] == null)) {
            Debug.Log("[Rebounding] Rebounding now during roof scroll: " + (paddleMove.baseYPos + 9.38f + GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.y));
            moveDir.y = -1;
        //} else if (transform.position.y > paddleMove.baseYPos + 10 && GameManager.currentBoardRow + 1 < totalRows && !GameManager.isShiftingDown) { // Try to move UP a board (only if roof is NOT scrolling)
        }
        
        // TRANSITION UP
        else*/ if (transform.position.y > GameManager.currentRoofHeight && GameManager.currentBoardRow + 1 < totalRows && !GameManager.isShiftingDown) { // Try to move UP a board (only if roof is NOT scrolling)
            if (gameObject == GameManager.ActiveBalls[0].gameObject) {
                Debug.Log("CHECK totalRows: " + totalRows + ", " + (GameManager.currentBoardRow + 1) + ". Current Roof Height: " + GameManager.currentRoofHeight);
                //Debug.Log("[Glitch] Current row: " + GameManager.currentBoardRow + ", next row: " + (GameManager.currentBoardRow + 1) + ", totalRows: " + totalRows);
                CameraFollow.RoomTransitioning = true;
                StartCoroutine(CameraFollow.Transition(VerticalTransTimeUp));                                                  // Fix timer
                float t = (GameManager.currentRoofHeight - paddle.transform.position.y) + 0.5f;
                Debug.Log("GOING UP: " + t);
                paddleMove.baseYPos += t;
                paddleMove.currentYPos += t;
                paddleMove.maxFlipHeight += t;
                if ((int)GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.z != 3) {
                    GameManager.curHeight += (10 + GameManager.GetCurrentLayer().GetComponent<LocalRoomData>().localRoomData.y);
                } else {
                    GameManager.curHeight += 10;
                }
                GameManager.currentBoardRow += 1;
                Debug.Log("CHECK totalRows: " + GameManager.currentRoofHeight);

                transform.position = new Vector3(transform.position.x, transform.position.y + 0.3f, 0f);                                                                    // Fix
                if (currentSpeed > 10) {
                    currentSpeed = 10;
                }

                Vector2 roomAbove = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);
                UpdateRoomHistory(roomAbove);
                //GameManager.isTransitioning = false;

                foreach (GameObject ball in GameManager.ActiveBalls) {
                    Vector2 oldDir = ball.GetComponent<BallMovement>().moveDir;
                    ball.transform.position = new Vector3(GameManager.ActiveBalls[0].transform.position.x, GameManager.ActiveBalls[0].transform.position.y + 0.3f, 0f);
                    ball.GetComponent<BallMovement>().moveDir = oldDir; // restore
                }
            } else {
                moveDir.y *= -1;
            }
        }
        
        // DELETE BALL IF NO LOWER ROOM
        else if (transform.position.y < paddleMove.baseYPos - 1.1f && GameManager.currentBoardRow > 0 && (GameManager.isShiftingDown || GameManager.levelLayers[GameManager.currentBoardRow - 1, GameManager.currentBoardColumn] == null)) {
            int activeBallCount = ballCountCheck();
            if (activeBallCount > 1) {
                gameObject.SetActive(false);
            } else {
                Destroy(gameObject);
            }
        }

        // TRANSITION DOWN
        else if (transform.position.y < paddleMove.baseYPos - 1.1f && GameManager.currentBoardRow > 0) {
            if (!GameManager.isShiftingDown && gameObject == GameManager.ActiveBalls[0].gameObject) {
                GameObject roomBelowObj = GameManager.levelLayers[GameManager.currentBoardRow - 1, GameManager.currentBoardColumn];
                if (roomBelowObj != null) {
                    LocalRoomData roomBelowData = roomBelowObj.GetComponent<LocalRoomData>();

                    if (roomBelowData != null && (int)roomBelowData.localRoomData.z == 3 && (int)roomBelowData.localRoomData.y > 0) {
                        Debug.Log("[Down test] Deleting Ball bc next room is an auto-scroller. roomBelowData Z: " + roomBelowData.localRoomData.z);
                        Destroy(gameObject);
                        return;
                    }

                    CameraFollow.RoomTransitioning = true;
                    StartCoroutine(CameraFollow.Transition(VerticalTransTimeDown));                                                  // Fix timer
                    // Use the same logic as "going up" but inverted
                    if ((int)roomBelowData.localRoomData.z == 4) {
                        GameManager.curHeight -= 10 + roomBelowData.localRoomData.y;
                    } else {
                        GameManager.curHeight -= 10;
                    }

                    GameManager.currentBoardRow -= 1;

                    // Always: 10 + target room’s Y offset
                    float paddleStep = 10f + roomBelowData.localRoomData.y;
                    Debug.Log($"GOING DOWN -> NextRow = {GameManager.currentBoardRow - 1}, Offset = {roomBelowData.localRoomData.y}, PaddleStep = {paddleStep}");
                    paddleMove.baseYPos -= paddleStep;
                    paddleMove.currentYPos -= paddleStep;
                    paddleMove.maxFlipHeight -= paddleStep;

                    if (currentSpeed > 10) {
                        currentSpeed = 10;
                    }

                    Vector2 roomBelow = new Vector2(GameManager.currentBoardColumn, GameManager.currentBoardRow);
                    UpdateRoomHistory(roomBelow);

                    GameManager.isTransitioning = false;

                    foreach (GameObject ball in GameManager.ActiveBalls) {
                        Vector2 oldDir = ball.GetComponent<BallMovement>().moveDir;
                        ball.transform.position = new Vector3(
                            GameManager.ActiveBalls[0].transform.position.x,
                            GameManager.ActiveBalls[0].transform.position.y - 1f,
                            0f
                        );
                        ball.GetComponent<BallMovement>().moveDir = oldDir;
                    }
                }
            } else {
                Destroy(gameObject);
            }
        }


        // Reset position if Vertically out of bounds - Only allow if roof is NOT scrolling
        /*if (transform.position.y > paddleMove.baseYPos + GameManager.currentRoofHeight && GameManager.currentBoardRow == totalRows - 1 && !GameManager.isShiftingDown && !GameManager.isTransitioning) {
            transform.position = new Vector3(0, currentLayer.transform.position.y);
            moveDir.y = -1;
            moveDir.x = 0;
        }*/

        // EARLY RETURN FOR ROOF SCROLLING - After handling upward rebound
        if (GameManager.isShiftingDown) {
            return; // Don't process horizontal transitions during roof scroll
        }

        float roomCenterX = currentLayer.transform.position.x;
        float roomHalfWidth = 7.3f + roomData.localRoomData.x;
        // If outside of Horizontally out of bounds Paul Mcartny
        // RIGHT SIDE TRANSITION
        if (transform.position.x >= roomCenterX + roomHalfWidth + 1f && !GameManager.ceilinigDestroyed) {
            if (gameObject == GameManager.ActiveBalls[0].gameObject) {

                // Only the 0th ball triggers the horizontal transition
                isHorizontalTransitioning = true;

                if (GameManager.isShiftingDown) {
                    Debug.Log("[boardTransition] Canceling roof scroll for right transition");
                    GameManager.needsFastRoofScroll = true;
                    GameManager.lastHorizontalTransitionTime = Time.time;
                }

                if (!GameManager.isTransitioning) {
                    transform.position = new Vector3(roomCenterX, transform.position.y);
                    moveDir = new Vector2(0, -1); // reset
                } else if (GameManager.currentBoardColumn < 2) {
                    GameManager.currentBoardColumn++;
                    StartCoroutine(CameraFollow.Transition(HorizontalTransTime));

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

                    foreach (GameObject ball in GameManager.ActiveBalls) {
                        ball.transform.position = GameManager.ActiveBalls[0].transform.position;
                        ball.GetComponent<BallMovement>().currentSpeed = 4f;
                    }
                }

                moveDir = new Vector2(-0.5f, 1f); // Right side transition
                currentSpeed = targetSpeed;
            } else {
                // Non-0th balls bounce instead of transitioning
                moveDir.x *= -1;
            }
        }

        // LEFT SIDE TRANSITION
        if (transform.position.x <= roomCenterX - roomHalfWidth - 1f && !GameManager.ceilinigDestroyed) {
            if (gameObject == GameManager.ActiveBalls[0].gameObject) {
                isHorizontalTransitioning = true;

                if (GameManager.isShiftingDown) {
                    Debug.Log("[boardTransition] Canceling roof scroll for left transition");
                }

                if (!GameManager.isTransitioning) {
                    transform.position = new Vector3(roomCenterX, transform.position.y);
                    moveDir = new Vector2(0, -1); // reset
                } else if (GameManager.currentBoardColumn > 0) {
                    GameManager.currentBoardColumn--;
                    StartCoroutine(CameraFollow.Transition(HorizontalTransTime));
                    if (currentSpeed > 10) {
                        currentSpeed = 10;
                    }
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

                moveDir = new Vector2(0.5f, 1f); // Left side transition
                currentSpeed = targetSpeed;
            } else {
                // Non-0th balls bounce instead of transitioning
                moveDir.x *= -1;
            }
        }

        // Update brick container while ignoring Z3 bricks
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
            //Debug.Log($"[Room History] Added room [{newRoom.x}, {newRoom.y}]. Count: {GameManager.RoomHistory.Count}");
        }
    }
    #endregion


    #region Collisions
    private void CheckPaddleCollision() {
        bool inVerticalRange = transform.position.y < paddle.transform.position.y + 0.13f && transform.position.y > paddle.transform.position.y - 0.13f;
        //bool inVerticalRange = transform.position.y < paddle.transform.position.y + 0.3f && transform.position.y > paddle.transform.position.y - 0.3f;

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

            if (GameManager.ActiveBalls[0] != null && gameObject == GameManager.ActiveBalls[0].gameObject) {
                Debug.Log($"{gameObject.name} - COLLISION SUCCESS - Processing paddle hit");
                paddleOffsetX = transform.position.x - paddle.transform.position.x;
                PaddleBounce();
            } else {
                SecondaryBallPaddleBounce();
            }
        }

        

        if (!inVerticalRange || !inHorizontalRange || moveDir.y != -1) {
            Debug.Log($"{gameObject.name} - Collision FAILED: " +
                     $"V={inVerticalRange} (ballY:{transform.position.y:F3}, paddleY:{paddle.transform.position.y:F3}), " +
                     $"H={inHorizontalRange} (ballX:{transform.position.x:F3}, paddleX:{paddle.transform.position.x:F3}), " +
                     $"Dir={moveDir.y} (moveDir.y:{moveDir.y}), " +
                     $"Stuck={isStuckToPaddle}, Trans={isHorizontalTransitioning}");
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
            //Debug.Log($"Bounce! posX={transform.position.x}, bounds=({leftBoundary}, {rightBoundary}), roomPosX={roomPosition.x}");
            bounce(true);
        }
    }

    public int ballCountCheck() {
        int activeBallCount = 0;
        foreach (var ball in GameManager.ActiveBalls) {
            if (ball != null && ball.gameObject != null && ball.gameObject.activeInHierarchy) {
                activeBallCount++;
            }
        }

        return activeBallCount;
    }

    public void HandleRoofCollision() {
        int activeBallCount = ballCountCheck();

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
        if (gameObject == GameManager.ActiveBalls[0].gameObject && collision.gameObject.CompareTag("Ball")) {
            return;
        }

        GameObject currentLayer = GameManager.GetCurrentLayer();
        if (currentLayer == null) return;
        if (collision.contactCount == 0 || collisionProcessedThisFrame) return;
        collisionProcessedThisFrame = true;

        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal;
        float angle = Vector2.Angle(normal, Vector2.up);

        // RoofPiece collision check
        if (collision.gameObject.name.StartsWith("RoofPiece") || collision.gameObject.name.EndsWith("Wall")) {

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
        bool isInViewport = viewportPosition.x >= 0 && viewportPosition.x <= 1 && viewportPosition.y >= -0.5 && viewportPosition.y <= 1.5 && viewportPosition.z > 0; // z > 0 means in front of camera
        return isInViewport;
    }

    public void roofCheck() {
        if (moveDir.y <= 0 || GameManager.isTransitioning || isStuckToPaddle) return;

        float radius = GetComponent<CircleCollider2D>().radius;
        float rayDistance = radius + 0.1f;

        // Cast multiple rays to cover the ball's width
        Vector2[] rayOrigins = new Vector2[] {
        transform.position,                                    // Center
        transform.position + new Vector3(radius * 0.7f, 0, 0), // Right side
        transform.position - new Vector3(radius * 0.7f, 0, 0)  // Left side
    };

        foreach (Vector2 origin in rayOrigins) {
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.up, rayDistance);
            if (hit.collider != null && hit.collider.gameObject.name.StartsWith("RoofPiece")) {
                //Debug.Log("Roof hit detected via raycast");
                moveDir.y = -1;
                GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
                return; // Only need to detect one hit
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            curPaddleMagnetVolume = null;
            Debug.Log("WORKED");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            curPaddleMagnetVolume = collision;
            Debug.Log("WORKED");
        }



        if (collision.gameObject.CompareTag("XTransition") && !GameManager.IsGameStart && !isStuckToPaddle) {
            //Debug.Log("Hitting XTrans");
            if (gameObject != GameManager.ActiveBalls[0].gameObject) {
                return;
            }

            // FREEZE THE BALL IMMEDIATELY FOR HORIZONTAL TRANSITION
            isHorizontalTransitioning = true;

            // CANCEL ROOF SCROLL HERE - This is the key fix!
            if (GameManager.isShiftingDown) {
                Debug.Log("[XTransition] Canceling roof scroll before horizontal transition");
                //GameManager.CancelRoofTransition();
            }
    
            CameraFollow.RoomTransitioning = true;
            
            GameManager.lastHorizontalTransitionTime = Time.time;

            //Debug.Log("[HRT]: " + GameManager.isTransitioning);

            LocalRoomData roomData = GameManager.GetCurrentLayer().GetComponent<LocalRoomData>();
            //Debug.Log("[CASE 3] XTrans triggered. Pre-Brick Count == " + roomData.numberOfBricks);

            XTransition currentTrans = collision.gameObject.GetComponent<XTransition>();
            GameObject partner = currentTrans.partnerTransition;
            if (currentTrans == null || partner == null) return;
            
            int destinationColumn = (int)currentTrans.transition;
            //Debug.Log("destinationColumn: " + destinationColumn + ", currentTrans: " + currentTrans + ", partner: " + partner);

            // Properly find which room contains the partner transition
            bool foundPartner = false;
            for (int row = 0; row < GameManager.levelLayers.GetLength(0); row++) {
                for (int col = 0; col < GameManager.levelLayers.GetLength(1); col++) {
                    GameObject layer = GameManager.levelLayers[row, col];
                    if (layer != null && partner.transform.IsChildOf(layer.transform)) {
                        GameManager.currentBoardRow = row;
                        GameManager.currentBoardColumn = col;
                        foundPartner = true;
                        //Debug.Log($"Found partner at [{row}, {col}]");
                        break;
                    }
                }
                if (foundPartner) break;
            }

            if (!foundPartner) {
                isHorizontalTransitioning = false;
                return;
            }

            //Debug.Log("Now in Position: [" + GameManager.currentBoardRow + ", " + GameManager.currentBoardColumn + "]");
            roomData = GameManager.GetCurrentLayer().GetComponent<LocalRoomData>();
            //Debug.Log("[CASE 3] XTrans triggered. Post-Brick Count == " + roomData.numberOfBricks);

            // --- TELEPORT THE BALL ---
            float dir = 0;
            if (transform.position.x < partner.transform.position.x) { // Coming from left
                transform.position = new Vector3(partner.transform.position.x + 1f, partner.transform.position.y, 0f);
                moveDir = new Vector2(0.5f, 1f); // Set the preset direction
                dir = 1;
            } else { // Coming from right
                transform.position = new Vector3(partner.transform.position.x - 1f, partner.transform.position.y, 0f);
                moveDir = new Vector2(-0.5f, 1f); // Set the preset direction
                dir = -1;
            }
            foreach (GameObject ball in GameManager.ActiveBalls) {
                ball.transform.position = GameManager.ActiveBalls[0].transform.position;
                if (ball.GetComponent<BallMovement>().currentSpeed > 5f) {
                    ball.GetComponent<BallMovement>().currentSpeed = 3f;
                }
                ball.GetComponent<BallMovement>().moveDir.y = 1;
                ball.GetComponent<BallMovement>().velocity.y = 6.5f;
                ball.GetComponent<BallMovement>().velocity.x = dir * Random.Range(1, 6);
                //ball.GetComponent<BallMovement>().moveDir.x = dir;
            }
            //Debug.Log("Warped ball to: " + transform.position);

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
                //Debug.Log($"[Room History] Added room [{currentRoom.x}, {currentRoom.y}]. Count: {GameManager.RoomHistory.Count}");
            } else {
                //Debug.Log($"[Room History] Room {currentRoom} already most recent, skipping add.");
            }
            
            GameManager.isTransitioning = false;


            // START COROUTINE TO UNFREEZE WHEN CAMERA CATCHES UP
            isHorizontalTransitioning = false;
            //Debug.Log("[HRT]: " + GameManager.isTransitioning);
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
        if (transform.localScale.x < maxMegaScale) {
            transform.localScale = new Vector3(transform.localScale.x * 2, transform.localScale.y * 2, 1f);
            GameManager.GravityBall = false;
        }
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

    private void HandleBlockCollision(GameObject collidedObject) {
        if (currentSpeed > targetSpeed) {
            scoreMult = (int)(currentSpeed - 3);
        } else {
            scoreMult = 2;
        }

        if (GameManager.FireBall) {
            // Fireball logic
        }

        collidedObject.GetComponent<ObjHealth>().TakeDamage(1, scoreMult, currentSpeed);
        if (collidedObject.CompareTag("Brick")) {
            //Debug.Log("Collided with: " + collidedObject.name);
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
        }
    }

    private void HandleBouncePhysics(float angle, GameObject collidedObject) {
        if (!collidedObject.CompareTag("Player")) {
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);

            if (angle <= floorAngleThreshold || angle >= 180f - floorAngleThreshold) {
                // Vertical bounce (floor/ceiling)
                if (GameManager.ActiveBalls[0] != null && gameObject != GameManager.ActiveBalls[0].gameObject) {
                    // For secondary balls using physics, reverse Y velocity
                    velocity.y *= -1;
                }
                moveDir.y *= -1;
            } else {
                // Horizontal bounce (walls)
                if (GameManager.ActiveBalls[0] != null && gameObject != GameManager.ActiveBalls[0].gameObject) {
                    // For secondary balls using physics, reverse X velocity
                    velocity.x *= -1;
                }
                moveDir.x *= -1;
            }
        }
    }

    /*private void HandleBouncePhysics(float angle, GameObject collidedObject) {
        if (!collidedObject.CompareTag("Player")) {
            //Debug.Log("Collided with: " + collidedObject.name);
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);

            if (angle <= floorAngleThreshold || angle >= 180f - floorAngleThreshold) {
                moveDir.y *= -1;
            } else {
                moveDir.x *= -1;
            }
        }
    }*/

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

    private void SecondaryBallPaddleBounce() {
        GameManager.Instance.PlaySFX(GameManager.Instance.paddleBounceSound); 
        
        // Calculate bounce angle based on where ball hit paddle (same as primary balls)
        paddleOffset = paddle.transform.position.x - transform.position.x;
        Vector2 newDirection = new Vector2(-(paddleOffset / 1.1f), 1f).normalized;
        velocity = newDirection * Mathf.Max(currentSpeed, 5f);
        moveDir = newDirection;
        PaddleMove paddleMove = paddle.GetComponent<PaddleMove>();

        // Position adjustment for flipping paddle
        if (paddle.transform.position.y != -3.8f && paddleMove.flipping) {
            transform.position = new Vector3(transform.position.x, paddle.transform.position.y, 0);
        }

        if (paddleMove.recoilSpeed > 5) {
            velocity = velocity.normalized * paddleMove.recoilSpeed;
            currentSpeed = paddleMove.recoilSpeed;
            accelerationDrag = 1.8f;
        }

        // SFX Check
        if (currentSpeed >= 14f && paddleMove.flipping && paddleMove.recoilSpeed >= 14f) {
            GameManager.Instance.PlaySFX(GameManager.Instance.perfectFlipSound);
        }        
    }
    #endregion
}