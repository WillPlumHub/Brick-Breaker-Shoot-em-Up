using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour {

    public string sceneToLoad;
    
    void Awake() {
        GameManager.nextScene = sceneToLoad;
    }
}