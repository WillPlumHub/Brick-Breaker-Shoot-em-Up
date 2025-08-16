using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public float mod = -1.56f;

    void LateUpdate()
    {
        var rooms = GameManager.currentLevelData.LevelRooms;
        int boardIndex = GameManager.currentBoard;

        Vector3 roomPos;

        // Main room: use its Y directly
        if (rooms[boardIndex].z == 0f)
        {
            roomPos = rooms[boardIndex];
        }
        else
        {
            // Side room: use main room's Y
            int mainRoomIndex = FindAssociatedMainRoom(boardIndex);
            Debug.Log("MainRoomIndex: " + mainRoomIndex);
            roomPos = rooms[boardIndex];
            roomPos.y = rooms[mainRoomIndex].y;
        }

        // Horizontal offset based on column
        if (GameManager.currentColumn == 0) mod = 0f;
        else if (GameManager.currentColumn == 1) mod = -1.56f;
        else if (GameManager.currentColumn == -1) mod = 1.56f;

        float targetX = mod + (GameManager.currentColumn * 25.10178f);

        // Snap directly to calculated target
        transform.position = new Vector3(targetX, roomPos.y, transform.position.z);
    }

    private int FindAssociatedMainRoom(int boardIndex)
    {
        Debug.Log("Main Index is running");
        for (int i = boardIndex; i >= 0; i--)
        {
            Debug.Log("Main Index: " + i);
            if (GameManager.currentLevelData.LevelRooms[i].z == 0f)
                Debug.Log("Main Index: Found it: " + i);
                return i;
        }
        return 15;
    }
}
