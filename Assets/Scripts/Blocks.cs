using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Blocks : MonoBehaviour
{
    [SerializeField] private Block[] blocks;
    [SerializeField] private Board board;

    [Header("Generation Settings")]
    [Tooltip("Maximum number of re-rolls when a generated batch has no valid placement.")]
    [SerializeField] private int maxSolvabilityRetries = 30;

    [Tooltip("Maximum score used to cap the difficulty curve.")]
    [SerializeField] private int maxScoreForDifficulty = 2000;

    private int[] polyominoIndexes;
    private int blockCount = 0;
    private bool isGameOver = false;
    private bool pendingGameOverCheck = false;

    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (pendingGameOverCheck && !board.IsEffectChainActive)
        {
            pendingGameOverCheck = false;
            CheckGameOver();
        }
    }

    void Start()
    {
        float blockWidth = (float)Board.Size / blocks.Length;
        float cellSize   = (float)Board.Size / (Block.Size * blocks.Length + blocks.Length + 1);

        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i].transform.localPosition = new(blockWidth * (i + 0.5f), -0.25f - cellSize * 4.0f, 0f);
            blocks[i].transform.localScale    = new(cellSize, cellSize, cellSize);
            blocks[i].Initialize();
        }

        polyominoIndexes = new int[blocks.Length];
        GenerateNewBlocks();
    }

    // ─────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────

    public void GenerateNewBlocks()
    {
        blockCount = 0;
        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;

        var lvl = LevelModeManager.Instance;
        bool usePool = lvl != null && lvl.IsLevelModeActive
                       && lvl.CurrentLevel != null && lvl.CurrentLevel.HasSpawnPool;

        if (usePool)
        {
            // Draw from level pool; hide slots that have no block left
            for (int i = 0; i < blocks.Length; i++)
            {
                var drawn = lvl.DrawFromPool();
                if (drawn.index < 0)
                {
                    // Pool exhausted — hide this slot
                    blocks[i].gameObject.SetActive(false);
                    polyominoIndexes[i] = -1;
                }
                else
                {
                    polyominoIndexes[i] = drawn.index;
                    blocks[i].gameObject.SetActive(true);
                    blocks[i].Show(polyominoIndexes[i], drawn.elements);
                    blockCount++;
                }
            }

            // If ALL slots are empty, trigger fail (no moves left)
            if (blockCount == 0)
            {
                pendingGameOverCheck = true; // will trigger CheckGameOver → game over
            }
        }
        else
        {
            // Endless / no-pool: normal difficulty-weighted random
            int[] chosen = GenerateSolvableBatch(currentScore);
            for (int i = 0; i < blocks.Length; i++)
            {
                polyominoIndexes[i] = chosen[i];
                blocks[i].gameObject.SetActive(true);
                blocks[i].Show(polyominoIndexes[i]);
                blockCount++;
            }
        }

        pendingGameOverCheck = true;
    }

    public void Remove()
    {
        // Notify LevelModeManager of placement (before possible game over)
        bool outOfMoves = LevelModeManager.Instance?.OnBlockPlaced() ?? false;

        blockCount--;
        if (blockCount <= 0)
        {
            blockCount = 0;

            // If we're in level mode and out of moves, don't generate new blocks
            // LevelModeManager.LevelFailed() will handle the screen
            if (outOfMoves) return;

            GenerateNewBlocks();
            return;
        }

        // If out of moves but there are still blocks in the current batch, hide them
        if (outOfMoves)
        {
            for (int i = 0; i < blocks.Length; i++)
                blocks[i].gameObject.SetActive(false);
            return;
        }

        pendingGameOverCheck = true;
    }

    public void ResetSortingOrder()
    {
        for (int i = 0; i < blocks.Length; i++)
            blocks[i].SetSortingOrder(0);
    }

    // ─────────────────────────────────────────────────────────
    //  Batch generation with solvability guarantee
    // ─────────────────────────────────────────────────────────

    private int[] GenerateSolvableBatch(int score)
    {
        int[] batch = new int[blocks.Length];

        for (int attempt = 0; attempt < maxSolvabilityRetries; attempt++)
        {
            bool forceTier1 = (attempt >= maxSolvabilityRetries - 3);
            for (int i = 0; i < blocks.Length; i++)
            {
                batch[i] = forceTier1
                    ? Polyominos.RandomIndexFromTier(1)
                    : Polyominos.RandomIndexForScore(score);
            }
            if (BatchIsSolvable(batch)) return batch;
        }

        // Absolute fallback
        for (int i = 0; i < blocks.Length; i++)
            batch[i] = Polyominos.RandomIndexFromTier(1);
        return batch;
    }

    /// <summary>
    /// Returns true if at least one piece in the batch can be placed on the board.
    /// </summary>
    private bool BatchIsSolvable(int[] batch)
    {
        foreach (int idx in batch)
        {
            if (board.CheckPlace(idx))
                return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────
    //  Game-over check
    // ─────────────────────────────────────────────────────────

    private void CheckGameOver()
    {
        bool canPlace = false;
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].gameObject.activeSelf && board.CheckPlace(polyominoIndexes[i]))
            {
                canPlace = true;
                break;
            }
        }

        if (!canPlace)
        {
            bool skillSavesUs = SkillManager.Instance != null
                                && SkillManager.Instance.AreAnySkillsReady();
            if (!skillSavesUs)
            {
                isGameOver = true;
                UIManager.Instance?.ShowGameOverScreen();
            }
        }
    }
}
