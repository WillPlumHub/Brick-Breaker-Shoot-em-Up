using UnityEngine;
using System.Collections.Generic;

public class BrickMonitor : MonoBehaviour {

    [Header("Monitoring Settings")]
    public float checkInterval = 0.5f;

    [Header("Brick Counting")]
    public int totalBricksRemaining = 0;
    public int activeGeneratorsCount = 0;
    private bool _hasHadBricks = false;  // New flag to track if we've ever had bricks

    private float _checkTimer;
    private List<BrickGen> _brickGenerators = new List<BrickGen>();

    void Start()
    {
        RefreshGeneratorList();
        _checkTimer = checkInterval;
        CountAllBricks(); // Initial count
    }

    void Update()
    {
        _checkTimer -= Time.deltaTime;
        if (_checkTimer <= 0f)
        {
            CountAllBricks();
            _checkTimer = checkInterval;
        }
    }

    public void RefreshGeneratorList()
    {
        _brickGenerators.Clear();
        var generators = FindObjectsOfType<BrickGen>(true);

        foreach (var generator in generators)
        {
            if (generator != null && generator.isActiveAndEnabled)
            {
                _brickGenerators.Add(generator);
            }
        }

        activeGeneratorsCount = _brickGenerators.Count;
    }

    public void CountAllBricks()
    {
        int brickCount = 0;

        // Count bricks from all active generators
        foreach (var generator in _brickGenerators)
        {
            if (generator != null)
            {
                brickCount += generator.GetActiveBrickCount();
            }
        }

        // Count any orphan bricks not managed by generators
        Brick[] orphanBricks = FindObjectsOfType<Brick>(true);
        brickCount += orphanBricks.Length;

        // Update our flag if we find any bricks
        if (brickCount > 0)
        {
            _hasHadBricks = true;
        }

        totalBricksRemaining = brickCount;

        // Only check for level completion if we've had bricks at some point
        if (_hasHadBricks && totalBricksRemaining <= 0)
        {
            totalBricksRemaining = 0;

            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                //gameManager.LoadNextLevel();
            }
            else
            {
                Debug.LogWarning("GameManager not found!");
            }
        }
    }

    public void ForceRefresh()
    {
        RefreshGeneratorList();
        CountAllBricks();
    }
}