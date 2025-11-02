using UnityEngine;

public class LevelSpawner : MonoBehaviour {
    public LevelData levelData;
    public Transform gridParent;

    void Start() {
        if (levelData == null) {
            Debug.LogError("[LevelSpawner] No LevelData assigned!");
            return;
        }

        if (gridParent == null)
            gridParent = this.transform;

        // Instantiate all prefabs from the LevelData grid
        levelData.InstantiateGridObjects(gridParent);

        Debug.Log("[LevelSpawner] Grid instantiation complete.");
    }
}
