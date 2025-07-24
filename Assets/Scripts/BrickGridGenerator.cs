using UnityEngine;

public class BrickGridGenerator : MonoBehaviour {

    [Header("Grid Settings")]
    public int rows = 5;
    public int columns = 10;
    public float spacingX = 0.1f;
    public float spacingY = 0.1f;

    [Header("Brick Settings")]
    public GameObject brickPrefab;
    public Vector2 brickSize = new Vector2(1f, 0.5f);
    public Vector2 startPosition = new Vector2(-4.5f, 4f);

    public GameObject[,] brickGrid;
    private bool[,] brickEnabled;

    void Start() {
        brickGrid = new GameObject[rows, columns];
        brickEnabled = new bool[rows, columns];

        // Enable all bricks by default
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                brickEnabled[row, col] = true;
                CreateBrick(row, col);
            }
        }
    }

    void CreateBrick(int row, int col)
    {
        if (brickPrefab == null || !brickEnabled[row, col])
            return;

        Vector2 position = new Vector2(
            startPosition.x + col * (brickSize.x + spacingX),
            startPosition.y - row * (brickSize.y + spacingY)
        );

        GameObject brick = Instantiate(brickPrefab, position, Quaternion.identity, transform);
        brick.name = $"Brick_{row}_{col}";
        brickGrid[row, col] = brick;
    }

    void DestroyBrick(int row, int col)
    {
        if (brickGrid[row, col] != null)
        {
            Destroy(brickGrid[row, col]);
            brickGrid[row, col] = null;
        }
    }

    /// <summary>
    /// Toggles a brick on or off at the given row and column without regenerating the whole grid.
    /// </summary>
    public void ToggleBrick(int row, int col)
    {
        if (!IsValidIndex(row, col))
        {
            Debug.LogWarning($"Invalid grid index: ({row}, {col})");
            return;
        }

        if (brickEnabled[row, col])
        {
            // Turn brick OFF
            DestroyBrick(row, col);
            brickEnabled[row, col] = false;
        }
        else
        {
            // Turn brick ON
            brickEnabled[row, col] = true;
            CreateBrick(row, col);
        }
    }

    bool IsValidIndex(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }
}
