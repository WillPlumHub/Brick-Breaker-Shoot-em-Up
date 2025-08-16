using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEntrance : MonoBehaviour {

    public LevelData level;
    public static bool playerInTrigger = false;
    private bool inputStarted = false;
    
    void Update() {
        // Check input down while in trigger
        if (playerInTrigger && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton1))) {
            inputStarted = true;
        }

        // Check input up while in trigger and after starting input
        if (playerInTrigger && inputStarted && (Input.GetKeyUp(KeyCode.Space) || Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.JoystickButton1))) {
            if (!string.IsNullOrEmpty(level.StageName)) {
                SceneManager.LoadScene(level.StageName);
            } else {
                Debug.Log("No level to load");
            }
            inputStarted = false; // Reset
        }

        if (!playerInTrigger) {
            inputStarted = false;
        }
    }

    void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            playerInTrigger = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            playerInTrigger = false;
        }
    }
}