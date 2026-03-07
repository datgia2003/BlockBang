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

    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (isGameOver && Input.GetMouseButtonDown(0))
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        int[] chosen = GenerateSolvableBatch(currentScore);

        for (int i = 0; i < blocks.Length; i++)
        {
            polyominoIndexes[i] = chosen[i];
            blocks[i].gameObject.SetActive(true);
            blocks[i].Show(polyominoIndexes[i]);
            blockCount++;
        }
    }

    public void Remove()
    {
        blockCount--;
        if (blockCount <= 0)
        {
            blockCount = 0;
            GenerateNewBlocks();
            return; // GenerateNewBlocks already handles game-over check via the new batch
        }
        CheckGameOver();
    }

    public void ResetSortingOrder()
    {
        for (int i = 0; i < blocks.Length; i++)
            blocks[i].SetSortingOrder(0);
    }

    // ─────────────────────────────────────────────────────────
    //  Batch generation with solvability guarantee
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a set of polyomino indexes such that:
    ///   1. Each piece is chosen from a difficulty tier appropriate to the current score.
    ///   2. At least ONE piece in the batch can be placed somewhere on the current board.
    ///      (We do NOT simulate placing all 3 simultaneously — that would be too restrictive.)
    ///
    /// If every random batch is unsolvable (board nearly full), we fall back to Tier-1
    /// pieces on the last retry to prevent impossible situations.
    /// </summary>
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

            if (BatchIsSolvable(batch))
                return batch;
        }

        // Absolute fallback: single-cell pieces always fit somewhere
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
