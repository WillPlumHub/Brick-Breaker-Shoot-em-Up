using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalRoomData : MonoBehaviour {

    private GameObject brickContainer;
    public Vector4 localRoomData;
    public Vector2 initialRoomPosition;
    public int numberOfBricks = 0;
    public int initialNumberOfBricks = 0;
    public GameObject Brick;

    void Start() {
        if (GameManager.currentLevelData == null) {
            Debug.LogError("currentLevelData is null!");
            return;
        }

        if (GameManager.currentLevelData.LevelRoomsInspector == null) {
            Debug.LogError("LevelRoomsInspector list is null!");
            return;
        }

        int row = (int)initialRoomPosition.y;
        int column = (int)initialRoomPosition.x;

        // Add proper bounds checking
        if (row < 0 || row >= GameManager.currentLevelData.LevelRoomsInspector.Count) {
            Debug.LogError($"initialRoomPosition.y {row} out of range! Level has {GameManager.currentLevelData.LevelRoomsInspector.Count} rows.");
            return;
        }
        if (column < 0 || column > 2) {
            Debug.LogError("initialRoomPosition.x must be 0, 1, or 2!");
            return;
        }
                
        // Find this room’s BlockList once
        Transform blockListTransform = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("BlockList"));
        if (blockListTransform != null) {
            brickContainer = blockListTransform.gameObject;
        }
        
        CountBricks();

        StartCoroutine(DefaultBrick());

    }

    public void Update() {
        CountBricks();
    }

    public void CountBricks() {
        numberOfBricks = 0;
        if (brickContainer != null) {
            foreach (Transform child in brickContainer.transform) {
                ObjHealth health = child.GetComponent<ObjHealth>();
                if (child.gameObject.activeInHierarchy && health != null && !health.Invincibility) {
                    numberOfBricks++;
                }
            }
        } else {
            // Fallback: search by tag, but still scoped to this room
            foreach (ObjHealth health in GetComponentsInChildren<ObjHealth>()) {
                if (health.gameObject.activeInHierarchy && !health.Invincibility) {
                    numberOfBricks++;
                }
            }
        }

        if (initialNumberOfBricks == 0 || initialNumberOfBricks < numberOfBricks) {
            initialNumberOfBricks = numberOfBricks;
        }
    }

    public IEnumerator DefaultBrick() {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if ((int)localRoomData.z != 3 && initialNumberOfBricks == 0) {
            Debug.Log("[Default Brick] Laid default brick");
            Instantiate(Brick, transform.position, Quaternion.identity);
        } else {
            Debug.Log("[Default Brick] Bricks accounted for");
        }
    }
}