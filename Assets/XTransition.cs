using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XTransition : MonoBehaviour {

    // Defines the type of transition:
    //      0 = Main (central) board
    //      1 = Right Side board
    //     -1 = Left Side board
    public int transition;
    
    // Duration of the transition animation in seconds
    public GameObject partnerTransition;
    
    // Flag to prevent multiple transitions at once
    public float moveDuration = 0.4f;

    // Flag to prevent multiple transitions at once
    private bool isTransitioning = false;

    // Triggered when something enters this object's collider
    private void OnTriggerEnter2D(Collider2D collision) {
        if (!collision.CompareTag("Player") || partnerTransition == null || isTransitioning) return;
        // Add this wherever you want to check the history
        StartCoroutine(DoTransition());
    }

    // Handles the smooth movement of the player, ball, and camera between boards
    public IEnumerator DoTransition() {
        isTransitioning = true;

        // Find main objects in the scene
        var paddle = GameObject.FindWithTag("Player");
        var ball = GameObject.FindWithTag("Ball");
        var cam = Camera.main.transform;

        Debug.Log($"GOT THIS FAR 11");

        // Disable player and ball controls during transition to prevent immediate retrigger
        var partnerCollider = partnerTransition.GetComponent<Collider2D>();
        if (partnerCollider) partnerCollider.enabled = false;

        var paddleControl = paddle?.GetComponent<PaddleMove>(); // Null check
        if (paddleControl) paddleControl.enabled = false;

        var ballControl = ball?.GetComponent<BallMovement>(); // Null check
        if (ballControl) ballControl.enabled = false;

        Debug.Log($"GOT THIS FAR 1");

        // Store starting positions
        Vector3 startPaddle = paddle.transform.position;
        Vector3 startBall = ball.transform.position;
        Vector3 startCam = cam.position;

        Debug.Log($"GOT THIS FAR 2");

        // Target positions for paddle, ball, and camera
        Vector3 endPaddle = partnerTransition.transform.position;
        Vector3 endBall = new Vector3(endPaddle.x + (ball.transform.position.x - startPaddle.x), partnerTransition.transform.position.y, ball.transform.position.z);
        Vector3 endCam = new Vector3(endPaddle.x, endPaddle.y + 0.5f, startCam.z);

        Debug.Log($"GOT THIS FAR 3");

        // Animate objects over moveDuration seconds
        float elapsed = 0f;
        while (elapsed < moveDuration) {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            // Smoothly interpolate positions
            paddle.transform.position = Vector3.Lerp(startPaddle, endPaddle, t);
            ball.transform.position = Vector3.Lerp(startBall, endBall, t);
            cam.position = Vector3.Lerp(startCam, endCam, t);

            yield return null; // To wait until next frame
        }

        Debug.Log($"GOT THIS FAR 4");

        // Snap to end positions
        paddle.transform.position = endPaddle;
        ball.transform.position = endBall;
        cam.position = endCam;

        
        // Update GameManager with new column (transition) and current board
        GameManager.currentColumn = transition;

        Debug.Log($"GOT THIS FAR");

        // Find which room the player landed on based on position
        for (int i = 0; i < GameManager.currentLevelData.LevelRooms.Count; i++) {
            var room = GameManager.currentLevelData.LevelRooms[i];
            Debug.Log("[XTRANS] Checking board: " + i);
            if (GameManager.currentLevelData.LevelRooms[i].z == transition) {
                Debug.Log("[XTRANS] The current board was: " + GameManager.currentBoard);
                GameManager.currentBoard = i;
                break;
            }
        }



        // THEN push the updated currentBoard reference
        Debug.Log($"[DEBUG] Entering side room logic");
        if (transition != 0)
        { // When going to side rooms
            Debug.Log($"[DEBUG] Entering side room logic");
            if (GameManager.RoomHistory == null)
            {
                Debug.Log("[DEBUG] Initializing RoomHistory stack");
                GameManager.RoomHistory = new Stack<int>();
            }
            GameManager.RoomHistory.Push(GameManager.currentBoard);
            Debug.Log($"[Room History] Pushed room {GameManager.currentBoard} to history stack. Stack count: {GameManager.RoomHistory.Count}");
        }
        else if (transition == 0)
        { // When returning to main room
            Debug.Log($"[DEBUG] Entering main room logic");
            if (GameManager.RoomHistory != null && GameManager.RoomHistory.Count > 0)
            {
                while (GameManager.RoomHistory.Count > 0)
                {
                    int poppedRoom = GameManager.RoomHistory.Pop();
                    Debug.Log($"[Room History] Popped room {poppedRoom} from history stack.");
                }
            }
            else
            {
                Debug.Log("[DEBUG] RoomHistory is empty or null, nothing to pop");
            }
        }
        else
        {
            Debug.Log($"[DEBUG] Unexpected transition value: {transition}");
        }









        // Re-enable player and ball controls
        if (paddleControl) paddleControl.enabled = true;
        if (ballControl) ballControl.enabled = true;

        // Reactivate partner's collider after a short delay to prevent immediate retrigger
        yield return new WaitForSeconds(0.1f);
        if (partnerCollider) partnerCollider.enabled = true;

        // Mark transition as finished
        isTransitioning = false;
    }
}
