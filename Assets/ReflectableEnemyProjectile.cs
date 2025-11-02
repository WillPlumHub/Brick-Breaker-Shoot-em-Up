using UnityEngine;

public class ReflectableEnemyProjectile : MonoBehaviour {

    public float speed = 5f;
    public float disableTime = 10f;
    public Transform sourceEnemy;
    public Vector2 moveDir;

    void Update() {
        transform.position += new Vector3(moveDir.x, moveDir.y, 0f).normalized * speed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            PaddleMove paddle = other.GetComponent<PaddleMove>();

            if (paddle != null) {
                if (paddle.flipping) {
                    if (sourceEnemy == null) return;
                    Vector3 direction = (sourceEnemy.position - transform.position).normalized;
                    moveDir = new Vector2(direction.x, direction.y);
                } else {
                    paddle.disableMovement = disableTime;
                    Destroy(gameObject);
                }
            }
        } else if (other.CompareTag("Enemy")) {
            BasicEnemy enemy = other.GetComponent<BasicEnemy>();
            if (enemy != null) {
                enemy.health--;
            }
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            MinPaddleMove miniPaddle = collision.gameObject.GetComponent<MinPaddleMove>();

            if (miniPaddle != null) {
                if (miniPaddle.flipping) {
                    if (sourceEnemy == null) return;
                    Vector3 direction = (sourceEnemy.position - transform.position).normalized;
                    moveDir = new Vector2(direction.x, direction.y);
                } else {
                    miniPaddle.disableMovement = disableTime;
                    Destroy(gameObject);
                }
            }
        } else if (collision.gameObject.CompareTag("Enemy")) {
            BasicEnemy enemy = collision.gameObject.GetComponent<BasicEnemy>();
            if (enemy != null) {
                enemy.health--;
            }
            Destroy(gameObject);
        }
    }
}