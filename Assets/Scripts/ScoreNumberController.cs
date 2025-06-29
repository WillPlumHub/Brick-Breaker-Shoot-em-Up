using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreNumberController : MonoBehaviour {

    public static ScoreNumberController instance;

    public ScoreNumber numberToSpawn;
    public RectTransform numberCanvas;

    private void Awake() {
        instance = this;
    }

    public void SpawnScore(int scoreAmount, Vector3 worldLocation) {

        // Convert world to screen point
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(worldLocation);

        // Convert screen to anchored UI position
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(numberCanvas, screenPoint, null,  out anchoredPos);

        // Instantiate under canvas
        ScoreNumber newScore = Instantiate(numberToSpawn, numberCanvas);
        newScore.GetComponent<RectTransform>().anchoredPosition = anchoredPos;

        newScore.Setup(scoreAmount);
        newScore.gameObject.SetActive(true);
    }

}