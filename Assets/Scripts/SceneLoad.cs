using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour {

    public LevelData sceneToLoad;
    
    void Awake() {
        if (sceneToLoad != null) {
            GameManager.nextScene = sceneToLoad.StageName;
        } else {
            GameManager.nextScene = "";
        }
    }
}