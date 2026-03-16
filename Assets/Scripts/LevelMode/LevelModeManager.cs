using UnityEngine;
using System.Collections.Generic;

public class LevelModeManager : MonoBehaviour
{
    public static LevelModeManager Instance { get; private set; }

    public bool IsLevelModeActive { get; private set; }
    public LevelData CurrentLevel { get; private set; }

    public event System.Action OnGoalUpdated;
    public event System.Action OnLevelCompleted;

    public int CurrentLevelIndex { get; private set; } = -1;

    // ── Move Limit ────────────────────────────────────────────────
    /// <summary>Remaining block placements. -1 = unlimited.</summary>
    public int MovesLeft { get; private set; } = -1;
    /// <summary>True when this level has a move limit.</summary>
    public bool HasMoveLimit => CurrentLevel != null && CurrentLevel.MaxBlockPlacements > 0;

    public event System.Action OnMoveUsed;
    public event System.Action OnMovesExhausted;

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
        return blockPool[poolDrawIndex++];
    }

    /// <summary>True when there is a defined pool and it is fully exhausted.</summary>
    public bool IsPoolExhausted => CurrentLevel != null && CurrentLevel.HasSpawnPool
        && poolDrawIndex >= blockPool.Count;

    // ── Goals ──────────────────────────────────────────────────────
    private List<LevelGoal> runtimeGoals = new List<LevelGoal>();
    public IReadOnlyList<LevelGoal> RuntimeGoals => runtimeGoals;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StopLevelMode()
    {
        IsLevelModeActive = false;
        CurrentLevel = null;
        MovesLeft = -1;
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
