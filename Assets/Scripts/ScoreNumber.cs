using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreNumber : MonoBehaviour {

    public TMP_Text scoreText;

    public float lifeTime = 1f;
    private float lifeCounter;

    public float floatSpeed = 1f;
    private RectTransform rectTransform;

    void Start() {
        lifeCounter = lifeTime;

        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>();

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = GetComponentInParent<RectTransform>();

        SetAlpha(1f);
    }

    void Update() {
        if (lifeCounter > 0) {
            lifeCounter -= Time.deltaTime;

            float alpha = Mathf.Clamp01(lifeCounter / lifeTime);
            SetAlpha(alpha);

            if (rectTransform != null) {
                rectTransform.anchoredPosition += Vector2.up * floatSpeed * Time.deltaTime;
            }

            if (lifeCounter <= 0) {
                Destroy(gameObject);
            }
        }
    }

    public void Setup(int scoreDisplay) {
        lifeCounter = lifeTime;
        scoreText.text = scoreDisplay.ToString();
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha) {
        Color color = scoreText.color;
        color.a = alpha;
        scoreText.color = color;
    }
}