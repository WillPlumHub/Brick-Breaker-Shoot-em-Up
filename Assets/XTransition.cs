using System.Collections;
using UnityEngine;

public class XTransition : MonoBehaviour
{
    public int transition; // 0 = main, 1 = right side, -1 = left side
    public GameObject partnerTransition;
    public float moveDuration = 0.4f;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player") || partnerTransition == null || isTransitioning) return;
        StartCoroutine(DoTransition());
    }

    private IEnumerator DoTransition()
    {
        isTransitioning = true;

        var paddle = GameObject.FindWithTag("Paddle");
        var ball = GameObject.FindWithTag("Ball");
        var cam = Camera.main.transform;

        var paddleControl = paddle?.GetComponent<PaddleMove>();
        if (paddleControl) paddleControl.enabled = false;

        var ballControl = ball?.GetComponent<BallMovement>();
        if (ballControl) ballControl.enabled = false;

        var rb = ball?.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = Vector2.zero;

        Vector3 startPaddle = paddle.transform.position;
        Vector3 endPaddle = partnerTransition.transform.position;

        Vector3 startBall = ball.transform.position;
        Vector3 endBall = endPaddle + (ball.transform.position - paddle.transform.position);

        Vector3 startCam = cam.position;
        Vector3 endCam = new Vector3(endPaddle.x, endPaddle.y + 0.5f, startCam.z);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            paddle.transform.position = Vector3.Lerp(startPaddle, endPaddle, t);
            ball.transform.position = Vector3.Lerp(startBall, endBall, t);
            cam.position = Vector3.Lerp(startCam, endCam, t);

            yield return null;
        }

        paddle.transform.position = endPaddle;
        ball.transform.position = endBall;
        cam.position = endCam;

        // Update GameManager's state
        GameManager.currentColumn = transition;

        for (int i = 0; i < GameManager.currentLevelData.LevelRooms.Count; i++)
        {
            var room = GameManager.currentLevelData.LevelRooms[i];
            if (Mathf.Approximately(room.x, endPaddle.x) && Mathf.Approximately(room.y, endPaddle.y))
            {
                GameManager.currentBoard = i;
                break;
            }
        }

        if (paddleControl) paddleControl.enabled = true;
        if (ballControl) ballControl.enabled = true;

        isTransitioning = false;
    }
}
