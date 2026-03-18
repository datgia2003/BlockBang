using UnityEngine;
using System.Collections.Generic;

public class LevelModeManager : MonoBehaviour
{
    private static LevelModeManager _instance;
    private static bool _isQuitting = false;

    public static LevelModeManager Instance
    {
        get
        {
            if (_isQuitting) return null;

            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelModeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("LevelModeManager (Created)");
                    _instance = go.AddComponent<LevelModeManager>();
                }
            }
            return _instance;
        }
    }

    public bool IsLevelModeActive { get; private set; }
    public LevelData CurrentLevel { get; private set; }

    public event System.Action OnGoalUpdated;
    public event System.Action OnLevelCompleted;
    public event System.Action OnPoolChanged;

    public int CurrentLevelIndex { get; private set; } = -1;

    // ── Move Limit ────────────────────────────────────────────────
    /// <summary>Remaining block placements. -1 = unlimited.</summary>
    public int MovesLeft { get; private set; } = -1;
    public int DiscardsLeft { get; private set; } = 0;
    /// <summary>True when this level has a move limit.</summary>
    public bool HasMoveLimit => CurrentLevel != null && CurrentLevel.MaxBlockPlacements > 0;

    public event System.Action OnMoveUsed;
    public event System.Action OnMovesExhausted;
    public event System.Action OnDiscardUsed;

    // ── Block Pool ────────────────────────────────────────────────
    private List<(int index, Element[] elements)> blockPool = new List<(int, Element[])>();   // shuffled, drawn from front
    private int poolDrawIndex = 0;

    /// <summary>How many blocks remain in the pool. -1 = pool not in use.</summary>
    public int PoolRemaining => (CurrentLevel != null && CurrentLevel.HasSpawnPool)
        ? Mathf.Max(0, blockPool.Count - poolDrawIndex)
        : -1;

    /// <summary>
    /// Draw the next polyomino from the pool.
    /// Returns (-1, null) when the pool is exhausted.
    /// </summary>
    public (int index, Element[] elements) DrawFromPool()
    {
        if (CurrentLevel == null || !CurrentLevel.HasSpawnPool) return (-1, null);
        if (poolDrawIndex >= blockPool.Count) return (-1, null);
        
        var result = blockPool[poolDrawIndex++];
        OnPoolChanged?.Invoke();
        return result;
    }

    /// <summary>True when there is a defined pool and it is fully exhausted.</summary>
    public bool IsPoolExhausted => CurrentLevel != null && CurrentLevel.HasSpawnPool
        && poolDrawIndex >= blockPool.Count;

    /// <summary>
    /// Returns the next block in the pool without drawing it.
    /// Returns (-1, null) if none left.
    /// </summary>
    public (int index, Element[] elements) PeekNext()
    {
        if (CurrentLevel == null || !CurrentLevel.HasSpawnPool) return (-1, null);
        if (poolDrawIndex >= blockPool.Count) return (-1, null);
        return blockPool[poolDrawIndex];
    }

    /// <summary>
    /// Returns a dictionary showing how many of each shape index remain in the pool.
    /// Used for the "Deck View" UI.
    /// </summary>
    public Dictionary<int, int> GetPoolSummary()
    {
        var summary = new Dictionary<int, int>();
        if (CurrentLevel == null || !CurrentLevel.HasSpawnPool) return summary;

        for (int i = poolDrawIndex; i < blockPool.Count; i++)
        {
            int shape = blockPool[i].index;
            if (summary.ContainsKey(shape)) summary[shape]++;
            else summary[shape] = 1;
        }
        return summary;
    }

    // ── Goals ──────────────────────────────────────────────────────
    private List<LevelGoal> runtimeGoals = new List<LevelGoal>();
    public IReadOnlyList<LevelGoal> RuntimeGoals => runtimeGoals;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public void StopLevelMode()
    {
        IsLevelModeActive = false;
        CurrentLevel = null;
        CurrentLevelIndex = -1;
        MovesLeft = -1;
        DiscardsLeft = 0;
        blockPool.Clear();
        poolDrawIndex = 0;
    }

    public void StartLevel(LevelData levelData, int index = -1)
    {
        IsLevelModeActive = true;
        CurrentLevel = levelData;
        CurrentLevelIndex = index;
        Debug.Log($"[LevelMode] ACTIVATED flag: {IsLevelModeActive} for {levelData.LevelName} (Index: {index})");

        // ── Build block pool ──────────────────────────────────
        blockPool.Clear();
        poolDrawIndex = 0;
        if (levelData.HasSpawnPool)
        {
            foreach (var entry in levelData.BlockSpawnPool)
                for (int i = 0; i < Mathf.Max(0, entry.Count); i++)
                    blockPool.Add((entry.PolyominoIndex, entry.CustomElements));

            // Shuffle pool using Fisher-Yates
            for (int i = blockPool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (blockPool[i], blockPool[j]) = (blockPool[j], blockPool[i]);
            }

            // Auto-compute move limit from pool size if not manually overridden
            MovesLeft = levelData.MaxBlockPlacements > 0
                ? levelData.MaxBlockPlacements
                : levelData.TotalPoolSize;
        }
        else
        {
            MovesLeft = levelData.MaxBlockPlacements > 0 ? levelData.MaxBlockPlacements : -1;
        }

        // ── Goals ─────────────────────────────────────────────
        runtimeGoals.Clear();
        DiscardsLeft = levelData.DiscardLimit;
        foreach (var goal in levelData.Goals)
        {
            runtimeGoals.Add(new LevelGoal
            {
                GoalType = goal.GoalType,
                TargetAmount = goal.TargetAmount,
                CurrentAmount = 0
            });
        }

        Debug.Log($"[LevelMode] Started '{levelData.LevelName}'. Pool: {blockPool.Count} blocks, MoveLimit: {MovesLeft}");
    }

    /// <summary>
    /// Called by Blocks.cs every time a block is successfully placed.
    /// Returns true if the player has run out of moves (game over for this level).
    /// </summary>
    public bool OnBlockPlaced()
    {
        if (!IsLevelModeActive || !HasMoveLimit) return false;

        MovesLeft--;
        OnMoveUsed?.Invoke();
        OnGoalUpdated?.Invoke(); // refresh UI

        Debug.Log($"[LevelMode] Block placed. Moves left: {MovesLeft}");

        if (MovesLeft <= 0)
        {
            OnMovesExhausted?.Invoke();
            // Check if all goals are already done (placement itself might complete the level)
            bool allDone = true;
            foreach (var g in runtimeGoals)
                if (!g.IsCompleted) { allDone = false; break; }

            if (!allDone)
            {
                // Out of moves without completing goals → fail
                Invoke(nameof(LevelFailed), 0.8f);
            }
            return true;
        }
        return false;
    }

    private void LevelFailed()
    {
        if (!IsLevelModeActive) return;
        IsLevelModeActive = false;
        Debug.Log("[LevelMode] Failed — out of moves!");
        UIManager.Instance?.ShowGameOverScreen();
    }

    public void TryDiscard()
    {
        if (!IsLevelModeActive) return;
        if (DiscardsLeft == 0) return; // Note: -1 still works if we wanted unlimited but usually it's positive
        
        // We'll let Blocks.cs handle the actual subtraction if it's successful
    }

    public void UseDiscard()
    {
        if (DiscardsLeft > 0) DiscardsLeft--;
        
        // Strategic cost: -2 moves (if limit exists)
        if (HasMoveLimit)
        {
            MovesLeft = Mathf.Max(0, MovesLeft - 2);
            OnMoveUsed?.Invoke();
            
            if (MovesLeft <= 0)
            {
                OnMovesExhausted?.Invoke();
                // Check if already won (edge case, but good to be safe)
                bool allDone = true;
                foreach (var g in runtimeGoals) if (!g.IsCompleted) { allDone = false; break; }
                
                if (!allDone)
                {
                    Invoke(nameof(LevelFailed), 0.5f);
                }
            }
        }

        OnDiscardUsed?.Invoke();
        OnGoalUpdated?.Invoke(); // Refresh UI including move count
    }

    public void OnGoalProgress(LevelGoalType type, int amount = 1)
    {
        if (!IsLevelModeActive) return;

        bool allCompleted = true;
        foreach (var goal in runtimeGoals)
        {
            if (goal.GoalType == type && !goal.IsCompleted)
            {
                goal.CurrentAmount = Mathf.Min(goal.CurrentAmount + amount, goal.TargetAmount);
                Debug.Log($"[LevelMode] Goal {type} updated: {goal.CurrentAmount}/{goal.TargetAmount}");
                OnGoalUpdated?.Invoke();
            }

            if (!goal.IsCompleted)
                allCompleted = false;
        }

        if (allCompleted && runtimeGoals.Count > 0)
        {
            Invoke(nameof(LevelCompleted), 1f);
        }
    }

    private void LevelCompleted()
    {
        if (!IsLevelModeActive) return;

        IsLevelModeActive = false;
        Debug.Log($"[LevelMode] Level {CurrentLevel.LevelName} Completed!");
        OnLevelCompleted?.Invoke();
        UIManager.Instance?.ShowVictoryScreen();
    }

    public void LoadNextLevel()
    {
        if (UIManager.Instance == null) return;
        
        int next = CurrentLevelIndex + 1;
        if (next < UIManager.Instance.LevelCount)
        {
            UIManager.Instance.LoadLevel(next);
        }
        else
        {
            // Back to menu if no more levels
            UIManager.Instance.ReturnToMenu();
        }
    }
}
