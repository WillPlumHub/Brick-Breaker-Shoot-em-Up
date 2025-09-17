using System.Collections;
using UnityEngine;

public class XTransition : MonoBehaviour
{
    public int transition; // 0 = side → main, 1 = main → right, -1 = main → left
    public GameObject partnerTransition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        /*if (other.CompareTag("Player"))
        {
            PaddleMove paddle = other.GetComponent<PaddleMove>();
            if (paddle != null && partnerTransition != null)
            {
                // Disable normal paddle control during transition
                paddle.isTransitioning = true;

                // Start transition coroutine
                StartCoroutine(DoTransition(paddle));
            }
        }*/
    }

    private IEnumerator DoTransition(PaddleMove paddle)
    {
        Vector3 start = paddle.transform.position;
        Vector3 end = new Vector3(
            partnerTransition.transform.position.x,
            partnerTransition.transform.position.y,
            paddle.transform.position.z
        );

        float duration = 0.5f; // how long transition takes
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth lerp movement
            paddle.transform.position = Vector3.Lerp(start, end, t);

            yield return null;
        }

        // Snap to final
        paddle.transform.position = end;

        // Re-enable player control
        paddle.isTransitioning = false;

        // Update GameManager row/col if needed
        UpdateBoardPosition();
    }

    private void UpdateBoardPosition()
    {
        if (transition == 0)
        {
            GameManager.currentBoardColumn = 1; // back to main
        }
        else if (transition == 1)
        {
            GameManager.currentBoardColumn = 2; // right room
        }
        else if (transition == -1)
        {
            GameManager.currentBoardColumn = 0; // left room
        }
    }
}
