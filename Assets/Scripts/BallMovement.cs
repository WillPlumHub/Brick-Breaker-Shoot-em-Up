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
    public Vector3 moveDir = new Vector3(0f, 1f, 0f); // Direction the ball is currently moving
    public float targetSpeed = 5f; // Target speed ball moves at
    public float currentSpeed; // Reference to ball's speed
    private float savedSpeed = 5f; // The ball's speed before and after getting grabbed
    public float accelerationDrag = 1.8f; // Speed ball accelerates/decellerates at when moving freely
    public bool isStuckToPaddle = false;
    private float paddleOffsetX = 0f; // The X position of the ball when it hits the paddle
    public float gravityBallReduction = 8f;
    public float terminalFallSpeed = 12f;

    [Header("Sticking")]
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
    GameManager gameManager;


    private void Awake() {
        paddle = GameObject.Find("Paddle");
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        // Register this ball with the GameManager
        GameManager.RegisterBall(gameObject);

        currentSpeed = GameManager.BallSpeed;
    }

    private void OnDestroy() {
        // Unregister this ball when destroyed
        GameManager.UnregisterBall(gameObject);
    }

    public void InitializeBall(Vector3 direction) {
        moveDir = direction;
    }

    private void Update() {
        bool grabPaddle = paddle.GetComponent<PaddleMove>().grabPaddle;
        boardTransition();
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

        if (GameManager.IsGameStart) {
            // If we haven't already set the stick target, set it to the paddle
            if (stickTarget == null && paddle != null) {
                stickTarget = paddle.transform;
                stickOffsetX = 0f;
                stickOffsetY = offset;
            }
            
            StickToPaddle();
        } else {
            Movement();
            CheckPaddleCollision();
            HandleSpeedAdjustment();
        }

        CheckDeathPlane();
        //bounceCheck();

        if (currentSpeed <= 0.1f && moveDir.y > 0) {
            moveDir.y = -1f;
            if (!GameManager.GravityBall) {
                currentSpeed = 3f;
            }
        }

        if (currentSpeed >= 5 && currentSpeed <= 8) {
            accelerationDrag = 1.8f;
        }

        if (GameManager.GravityBall && !GameManager.IsGameStart) {
            GravityBall();
        }
    }

    

    public void boardTransition() {
        PaddleMove paddleMove = paddle.GetComponent<PaddleMove>();
        if (paddleMove == null) {
            //Debug.LogError("PaddleMove component not found on paddle.");
            return;
        }

        int boardCount;
        if (GameManager.levelLayers == null) {
            boardCount = 0;
        } else {
            boardCount = GameManager.levelLayers.Count;
        }
        int currentBoard = GameManager.currentBoard;

        // Try to move UP a board
        if (transform.position.y > paddleMove.baseYPos + 10 && currentBoard + 1 < boardCount && !GameManager.isShiftingDown) {
            //GameManager.currentBoard++;
            Debug.Log("GOING UP: BallMovement for loop activated");
            for (int i = GameManager.currentBoard + 1;  i < boardCount; i++) {
                if ((int) GameManager.currentLevelData.LevelRooms[i].z == (int) GameManager.currentLevelData.LevelRooms[GameManager.currentBoard].z) {
                //if (GameManager.levelLayers[i] == 0) {
                    Debug.Log("GOING UP FINAL: CurrentBoard is: " + GameManager.currentBoard + ": " + GameManager.currentLevelData.LevelRooms[GameManager.currentBoard].z + " -> " + GameManager.currentLevelData.LevelRooms[i].z + ", thus " + i);
                    GameManager.currentBoard = i;
                    break;
                }
            }
            paddleMove.currentYPos += 10;
            paddleMove.baseYPos += 10;
            paddleMove.maxFlipHeight += 10;
        }


        // Try to move DOWN a board
        if (transform.position.y < paddleMove.baseYPos - 1.1f && GameManager.currentBoard > 0 && !GameManager.isShiftingDown) {
            Debug.Log("GOING DOWN: BallMovement for loop activated");
            int currentZ = (int)GameManager.levelLayers[GameManager.currentBoard].GetComponent<LocalRoomData>().localLevelData.z;

            for (int i = GameManager.currentBoard - 1; i >= 0; i--) {
                Vector4 target = GameManager.levelLayers[i].GetComponent<LocalRoomData>().localLevelData;
                
                if ((int)target.z == currentZ) {
                    Debug.Log("GOING DOWN: Found next room: " + GameManager.currentBoard + " to " + i + ".   " + currentZ + ", " + (int)target.z);
                    
                    if (currentZ == 0) { // If Main Room
                        Debug.Log($"GOING DOWN: Moving from board {GameManager.currentBoard} (z={currentZ}) to {i} (z={(int)target.z})");
                        GameManager.currentBoard = i;
                        // Adjust paddle positions
                        paddleMove.currentYPos -= 10;
                        paddleMove.baseYPos -= 10;
                        paddleMove.maxFlipHeight -= 10;
                        break;
                    } else { // If Side Room
                        // Check if the main room connected to this side room's Y != 0
                        float mainRoomY = 0;
                        bool mainRoom1 = (i - 1 >= 0) && GameManager.levelLayers[i - 1].GetComponent<LocalRoomData>().localLevelData.z == 0 && GameManager.levelLayers[i - 1].GetComponent<LocalRoomData>().localLevelData.y != 0f;
                        if (mainRoom1) {
                            mainRoomY = GameManager.levelLayers[i - 1].GetComponent<LocalRoomData>().localLevelData.y;
                        }
                        bool mainRoom2Valid = (i - 2 >= 0) && GameManager.levelLayers[i - 2].GetComponent<LocalRoomData>().localLevelData.z == 0 && GameManager.levelLayers[i - 2].GetComponent<LocalRoomData>().localLevelData.y != 0f;
                        if (mainRoom2Valid) {
                            mainRoomY = GameManager.levelLayers[i - 2].GetComponent<LocalRoomData>().localLevelData.y;
                        }

                        // If the main room's Y != 0, skip this side room
                        if (mainRoom1 || mainRoom2Valid) {
                            Debug.Log("GOING DOWN: Skipping side room - connected main room has invalid Y position (!0). targetZ.y: " + target.y + ", mainRoomY: " + mainRoomY);
                            if (target.y != mainRoomY) {
                                continue;
                            }
                        }
                        Debug.Log("GOING DOWN: Side Room: " + GameManager.currentBoard + " to " + i + ".   " + currentZ + ", " + (int)target.z);
                        // To Target Room, counting every main room along the way
                        int zeroCount = 0;
                        for (int j = GameManager.currentBoard; j >= i; j--) {
                            Debug.Log("GOING DOWN: loop " + j + " to " + i);
                            if ((int)GameManager.levelLayers[j].GetComponent<LocalRoomData>().localLevelData.z == 0) {
                            //if ((int)GameManager.currentLevelData.LevelRooms[j].z == 0) {
                                zeroCount++;
                                Debug.Log("GOING DOWN: Found a main board, adding to zeroCount: " + zeroCount);
                            }
                        }

                        if (zeroCount >= 2) { // If new Side Room isn't connected vertically (more than 2 main rooms between)
                            Debug.Log("GOING DOWN: Side Room gap. Current Board was: " + GameManager.currentBoard + ", lastMainBoard: " + GameManager.lastMainBoard);
                            Debug.Log("Last Main board: " + GameManager.lastMainBoard + ", Current Board pretrans: " + GameManager.currentBoard);
                            //int enabledBallCount = GameManager.ActiveBalls.Count(ball => ball != null && ball.gameObject != null && ball.gameObject.activeInHierarchy);
                            // Or using a foreach approach:
                            int count = 0;
                            foreach (var ball in GameManager.ActiveBalls) {
                                if (ball != null && ball.gameObject != null && ball.gameObject.activeInHierarchy) {
                                    count++;
                                }
                            }
                            if (count >= 2) {
                                gameObject.SetActive(false);
                            } else {
                                GameManager.Instance.RemoveAndReturnToMostRecentCentralRoom();
                            }
                        } else {
                            Debug.Log($"GOING DOWN: Moving from board {GameManager.currentBoard} (z={currentZ}) to {i} (z={(int)target.z}), zeros in between: {zeroCount}");
                            GameManager.currentBoard = i; // Valid move, update Current Board
                            paddleMove.currentYPos -= 10;
                            paddleMove.baseYPos -= 10;
                            paddleMove.maxFlipHeight -= 10;
                        }
                        break;
                    }
                }
            }
        }
        
        // Reset position if vertically out of bounds
        if (transform.position.y > paddleMove.baseYPos + 10 && GameManager.currentBoard == boardCount - 1 && !GameManager.isShiftingDown) {
            transform.position = new Vector3(0, GameManager.levelLayers[GameManager.currentBoard].transform.position.y); // Finish: Make sure it respawns on the right board
            moveDir.y = -1;
            moveDir.x = 0;
        }
        
        float offset = GameManager.levelLayers[currentBoard].GetComponent<LocalRoomData>().localLevelData.x;
        //Debug.Log("[CCCCCCCCC] Restting because: " + ((offset + 8.3f) + (24.26f * GameManager.currentColumn)) + ", and " + ((-offset - 8.3f) + (24.26f * GameManager.currentColumn)));
        // If outside of current board's left & right walls, reset back to center
        if ((transform.position.x >= ((offset + 8.3f) + (24.26f * GameManager.currentColumn))
            || transform.position.x <= ((-offset - 8.3f) + (24.26f * GameManager.currentColumn))) && !GameManager.ceilinigDestroyed) {
            //Debug.Log("[CCCCCCCCC] Restting because: " + ((offset + 8.3f) + (24.26f * GameManager.currentBoard)) + ", and " + ((-offset - 8.3f) + (24.26f * GameManager.currentBoard)));
            transform.position = new Vector3(GameManager.levelLayers[GameManager.currentBoard].transform.position.x, 0f);
            moveDir.y = -1;
            moveDir.x = 0;
        }
        
        var levelLayer = GameManager.levelLayers[GameManager.currentBoard];
        Transform blockListTransform = levelLayer.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("BlockList"));

        if (blockListTransform != null) {
            GameManager.brickContainer = blockListTransform.gameObject;
        }
    }


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

    public void GravityBall() {
        if (moveDir.y > 0) {
            currentSpeed -= gravityBallReduction * Time.deltaTime;
            if (currentSpeed <= 0.1f) {
                moveDir.y = -1;
                currentSpeed = 0.1f;
            }
        } else if (moveDir.y < 0 && currentSpeed <= terminalFallSpeed) {
            currentSpeed += gravityBallReduction * Time.deltaTime;
        }
    }

    /*private void bounceCheck() {
        if (transform.position.x >= (GameManager.currentLevelData.LevelRooms[GameManager.currentBoard].x + 7.3f) || transform.position.x <= (-GameManager.currentLevelData.LevelRooms[GameManager.currentBoard].x - 7.3f)) {
            //Debug.Log("Hitting X boundry at Position: " + (gameManager.currentLevelData.LevelRooms[GameManager.currentBoard].x - 7.3f));
            bounce(true);
        }

        // If at absolute top of level, bounce down
        if (transform.position.y >= (-4.5f + (10f * GameManager.numberOfBoards) - 0.5f) && !GameManager.ceilinigDestroyed) { //5.2f
            //Debug.Log("At absolute top of level");
            bounce(false);
            //Debug.Log("Ball hit ceiling at: " + (-4.5f + (10f * GameManager.numberOfBoards)));
        }

    }*/

    private void bounce(bool horizontal) {
        //Debug.Log("Collided with: Bounce Function");
        GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);
        if (horizontal) {
            moveDir.x *= -1;
        } else {
            moveDir.y *= -1;
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

    private void Movement() {
        if (moveDir != Vector3.zero) {
            transform.position += moveDir.normalized * currentSpeed * Time.deltaTime;
        }
    }

    private void HandleSpeedAdjustment() {
        if (!GameManager.GravityBall) {
            if (currentSpeed > targetSpeed) {
                currentSpeed -= accelerationDrag * Time.deltaTime;
                if (currentSpeed < targetSpeed) {
                    currentSpeed = targetSpeed;
                }
            } else if (currentSpeed < targetSpeed) {
                currentSpeed += accelerationDrag * Time.deltaTime;
                if (currentSpeed > targetSpeed) {
                    currentSpeed = targetSpeed;
                }
            }
        }


        if (moveDir.y > 0 && moveDir.y != 1) {
            moveDir.y = 1;
        }
        if (moveDir.y < 0 && moveDir.y != -1) {
            moveDir.y = -1;
        }
        /*if (moveDir.y == -1 && currentSpeed < 0) {
            currentSpeed = 0.1f;
        }*/
    }

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
        if (transform.position.y <= deathPlane) {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {

        //Debug.Log("Collided with: " + collision.gameObject.name);

        if (collision.contactCount == 0 || collisionProcessedThisFrame) return;

        collisionProcessedThisFrame = true;

        Vector2 normal = collision.GetContact(0).normal;
        float angle = Vector2.Angle(normal, Vector2.up);

        if (collision.gameObject.name.EndsWith("Roof") && transform.position.y > collision.transform.position.y) { // Change "Roof" to whatever object destroys balls
            //Destroy(gameObject);
            int count = 0;
            foreach (var ball in GameManager.ActiveBalls)
            {
                if (ball != null && ball.gameObject != null && ball.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }
            if (count >= 2)
            {
                gameObject.SetActive(false);
            }
            else
            {
                GameManager.Instance.RemoveAndReturnToMostRecentCentralRoom();
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

    public void ResetToCentralRoom() // Not used apparently???
    {
        // Set ball to center of the central room
        transform.position = Vector3.zero;

        // Reset movement direction and speed
        moveDir = new Vector2(0.5f, 1f);
        //currentSpeed = initialSpeed;

        // Optionally unstuck from paddle
        isStuckToPaddle = false;

        Debug.Log("Ball reset to central room.");
    }


    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("XTransition") && !GameManager.IsGameStart && !isStuckToPaddle) {
            Debug.Log("Hitting XTrans");

            XTransition currentTrans = collision.gameObject.GetComponent<XTransition>();
            GameObject partner = currentTrans.partnerTransition;

            if (currentTrans == null) {
                Debug.LogWarning("No XTransition component found.");
                return;
            }
            if (partner == null) {
                Debug.LogWarning("No partner transition assigned.");
                return;
            }

            // Update currentColumn to reflect where we're going
            int destinationColumn = (int)currentTrans.transition;
            GameManager.currentColumn = destinationColumn;

            // Update currentBoard to the board that contains the partner transition
            for (int i = 0; i < GameManager.levelLayers.Count; i++) {
                if (partner.transform.IsChildOf(GameManager.levelLayers[i].transform)) {

                    if (currentTrans.transition != 0) {
                        GameManager.lastMainBoard = GameManager.currentBoard;
                        Debug.Log("Last Main board: " + GameManager.lastMainBoard + ", Current Board pretrans: " + GameManager.currentBoard);
                    }
                    GameManager.currentBoard = i;
                    if (currentTrans.transition == 0) {
                        GameManager.lastMainBoard = GameManager.currentBoard;
                    }
                    break;
                }
            }

            Debug.Log("Now in column: " + GameManager.currentColumn);
            Debug.Log("Now in board index: " + GameManager.currentBoard);

            // Positioning the ball based on direction
            if (transform.position.x < partner.transform.position.x) {
                // Coming from left
                transform.position = new Vector3(partner.transform.position.x + 1f, transform.position.y, 0f);
                moveDir.y = 1f;
                moveDir.x = 0.5f;
            } else {
                // Coming from right
                transform.position = new Vector3(partner.transform.position.x - 1f, transform.position.y, 0f);
                moveDir.y = 1f;
                moveDir.x = -0.5f;
            }

            Debug.Log("Warped ball to: " + transform.position);

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
            //Debug.Log("Collided with: " + collidedObject.name);
            GameManager.Instance.PlaySFX(GameManager.Instance.ballBounceSound);

            if (angle <= floorAngleThreshold || angle >= 180f - floorAngleThreshold) {
                moveDir.y *= -1;
            } else {
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

    private IEnumerator ResetCollisionFlag() {
        yield return new WaitForEndOfFrame();
        collisionProcessedThisFrame = false;
    }
}