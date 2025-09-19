using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class LevelEntrance : MonoBehaviour {
    public LevelData level;
    public TMP_Text scoreText;
    public GameObject levelPromptUI;
    public Vector3 DifficultyStartPosition;
    public List<Image> Stars;

    private bool playerInTrigger = false;
    private bool inputStarted = false;
    public GameObject paddle;

    void Start() {
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text text in allTexts) {
            if (text.gameObject.name.Contains("level", System.StringComparison.OrdinalIgnoreCase)) {
                scoreText = text;
                levelPromptUI = scoreText.transform.parent.Find("Background").gameObject;
                break;
            }
        }
        HideLevelUI();
        if (paddle == null) {
            paddle = GameObject.Find("Map Paddle");
        }
    }

    void Update() {
        if (playerInTrigger && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton2))) {
            inputStarted = true;
            paddle.GetComponent<MapShip>().disabled = true;
        }

        if (playerInTrigger && inputStarted && (Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.JoystickButton2))) {
            if (levelPromptUI == null || !levelPromptUI.activeSelf) {
                ShowLevelUI();
                DifficultyStars();
            } else {
                LoadLevel();
            }
        }

        if (playerInTrigger && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton1))) {
            if (levelPromptUI == null || levelPromptUI.activeSelf) {
                HideLevelUI();
                paddle.GetComponent<MapShip>().disabled = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            PolygonCollider2D collider = collision as PolygonCollider2D;
            if (collider != null) {
                collision.GetComponent<MapShip>().disabled = true;
                playerInTrigger = true;
                //ShowLevelUI();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Player") && collision.gameObject == paddle) {
            PolygonCollider2D collider = collision as PolygonCollider2D;
            if (collider != null) {
                collision.GetComponent<MapShip>().disabled = false;
                playerInTrigger = false;
                inputStarted = false;
                HideLevelUI();
            }
        }
    }

    public void ShowLevelUI() {
        // Add UI animation
        if (levelPromptUI != null) levelPromptUI.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (scoreText != null && level != null) scoreText.text = level.StageName;
    }

    public void HideLevelUI() {
        // Add UI animation
        if (levelPromptUI != null) levelPromptUI.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        inputStarted = false;
    }

    private void LoadLevel() {
        if (!string.IsNullOrEmpty(level.StageName)) {
            SceneManager.LoadScene(level.StageName);
        }
    }

    public void DifficultyStars() {
        if (level == null) return;
        if (Stars == null || Stars.Count == 0) return;

        int activeStars = Mathf.Clamp(level.difficulty, 0, Stars.Count);
        for (int i = 0; i < Stars.Count; i++) {
            if (Stars[i] != null) {
                if (i < activeStars) {
                    Stars[i].gameObject.SetActive(true);
                } else {
                    Stars[i].gameObject.SetActive(false);
                }
            }
        }
    }
}