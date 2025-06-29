using UnityEngine;

public class BrickGen : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 5;
    public float cellSizeX = 1.0f; // Separate X cell size
    public float cellSizeY = 0.8f; // Separate Y cell size
    public Vector2 gridOffset = Vector2.zero;

    [Header("Brick Settings")]
    public GameObject brickPrefab;
    public Vector2 brickSize = new Vector2(0.9f, 0.4f); // Slightly smaller than cell for gaps
    public Color[] brickColors = {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta
    };

    void Start() {
        GenerateGrid();
    }

    public void GenerateGrid() {
        // Clear existing bricks
        ClearGrid();

        // Calculate starting position (top-left corner)
        Vector2 startPos = new Vector2(
            -((gridWidth - 1) * cellSizeX) / 2 + gridOffset.x,
            ((gridHeight - 1) * cellSizeY) / 2 + gridOffset.y
        );

        // Create bricks in grid pattern
        for (int y = 0; y < gridHeight; y++) {
            for (int x = 0; x < gridWidth; x++) {
                // Calculate position with perfect grid snapping using separate X/Y spacing
                Vector2 position = new Vector2(startPos.x + x * cellSizeX, startPos.y - y * cellSizeY);

                // Instantiate brick
                GameObject brick = Instantiate(brickPrefab, position, Quaternion.identity, transform);
                brick.name = $"Brick_{x}_{y}";

                // Set brick size
                brick.transform.localScale = new Vector3(brickSize.x, brickSize.y, 1f);

                // Set brick color
                SpriteRenderer renderer = brick.GetComponent<SpriteRenderer>();
                if (renderer != null) {
                    int colorIndex = y % brickColors.Length;
                    renderer.color = brickColors[colorIndex];
                }

                // Add brick component if not present
                Brick brickComponent = brick.GetComponent<Brick>() ?? brick.AddComponent<Brick>();
            }
        }
    }

    public void ClearGrid() {
        // Destroy all child objects (bricks)
        foreach (Transform child in transform) {
            Destroy(child.gameObject);
        }
    }

    public int GetActiveBrickCount() {
        int count = 0;
        foreach (Transform child in transform) {
            if (child.gameObject.activeInHierarchy && child.GetComponent<Brick>() != null)
                count++;
        }
        return count;
    }
}