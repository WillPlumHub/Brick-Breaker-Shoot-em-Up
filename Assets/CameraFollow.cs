using UnityEngine;

public class CameraFollow : MonoBehaviour {
    
    public float mod = -1.56f;
    public float movementSpeed = 10;

    void LateUpdate() {
        var levelLayers = GameManager.levelLayers;
        var LevelRooms = GameManager.currentLevelData.LevelRooms;
        int boardIndex = GameManager.currentBoard;

        Vector3 roomPos;

        // Main room: use its Y directly
        if (LevelRooms[boardIndex].z == 0f)
        {
            roomPos = levelLayers[boardIndex].transform.position;
            Debug.Log("1 RoomPos.y: " + roomPos.y);
            Debug.Log("boardIndex: " + boardIndex);
        }
        else
        {
            // Side room: use main room's Y
            int mainRoomIndex = FindAssociatedMainRoom(boardIndex);
            Debug.Log("MainRoomIndex: " + mainRoomIndex);
            Debug.Log("boardIndex: " + boardIndex);
            roomPos = levelLayers[boardIndex].transform.position;
            roomPos.y = LevelRooms[mainRoomIndex].y;
            Debug.Log("2 RoomPos.y: " + roomPos.y);
        }

        // Horizontal offset based on column
        if (GameManager.currentColumn == 0) mod = 0f;
        else if (GameManager.currentColumn == 1) mod = -1.56f;
        else if (GameManager.currentColumn == -1) mod = 1.56f;

        float targetX = mod + (GameManager.currentColumn * 25.10178f);

        // Snap directly to calculated target
        //transform.position = new Vector3(targetX, roomPos.y, transform.position.z);
        Vector3 target = new Vector3(targetX, roomPos.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * Time.deltaTime);
    }

    private int FindAssociatedMainRoom(int boardIndex)
    {
        Debug.Log("Main Index is running");
        for (int i = boardIndex; i >= 0; i--)
        {
            Debug.Log("Main Index: " + i);
            if (GameManager.currentLevelData.LevelRooms[i].z == 0f)
            {
                Debug.Log("Main Index: Found it: " + i);
                return i;
            }
        }
        return boardIndex;
    }
}
