using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All polyomino shapes, categorised by difficulty tier.
/// Tier 1 = trivially easy (1–2 cells), Tier 4 = very hard (complex 5-cell shapes).
/// </summary>
public static class Polyominos
{
    // ─────────────────────────────────────────────────────────
    //  Tier 1  ─  1–2 cells  (tiny, always easy to place)
    // ─────────────────────────────────────────────────────────
    private static readonly int[][,] tier1 = new int[][,]
    {
        // single cell
        new int[,] { {1} },

        // domino H
        new int[,] { {1,1} },

        // domino V
        new int[,] { {1},{1} },
    };

    // ─────────────────────────────────────────────────────────
    //  Tier 2  ─  3 cells  (trominos, easy)
    // ─────────────────────────────────────────────────────────
    private static readonly int[][,] tier2 = new int[][,]
    {
        // straight 3 H
        new int[,] { {1,1,1} },

        // straight 3 V
        new int[,] { {1},{1},{1} },

        // L-tromino (corner)
        new int[,]
        {
            {1,0},
            {1,1},
        },
        new int[,]
        {
            {0,1},
            {1,1},
        },
        new int[,]
        {
            {1,1},
            {1,0},
        },
        new int[,]
        {
            {1,1},
            {0,1},
        },
        new int[,]
        {
            {1,0},
            {0,1},
        },
        new int[,]
        {
            {0,1},
            {1,0},
        },
    };

    // ─────────────────────────────────────────────────────────
    //  Tier 3  ─  3,4 cells  (tetrominoes, medium)
    // ─────────────────────────────────────────────────────────
    private static readonly int[][,] tier3 = new int[][,]
    {
        // 2×2 square
        new int[,]
        {
            {1,1},
            {1,1},
        },

        // straight 4 H
        new int[,] { {1,1,1,1} },

        // straight 4 V
        new int[,] { {1},{1},{1},{1} },

        // L-tetromino
        new int[,]
        {
            {1,0},
            {1,0},
            {1,1},
        },
        new int[,]
        {
            {0,1},
            {0,1},
            {1,1},
        },
        new int[,]
        {
            {1,1},
            {1,0},
            {1,0},
        },
        new int[,]
        {
            {1,1},
            {0,1},
            {0,1},
        },

        // S/Z-tetromino
        new int[,]
        {
            {1,0},
            {1,1},
            {0,1},
        },
        new int[,]
        {
            {0,1},
            {1,1},
            {1,0},
        },

        // T-tetromino
        new int[,]
        {
            {1,0,0},
            {1,1,1},
        },
        new int[,]
        {
            {0,0,1},
            {1,1,1},
        },
        new int[,]
        {
            {0,1,0},
            {1,1,1},
        },
        new int[,]
        {
            {1,1,1},
            {0,1,0},
        },
        new int[,]
        {
            {1,0,0},
            {0,1,0},
            {0,0,1},
        },
        new int[,]
        {
            {0,0,1},
            {0,1,0},
            {1,0,0},
        },
    };

    // ─────────────────────────────────────────────────────────
    //  Tier 4  ─  5 cells (pentominoes, hard)
    // ─────────────────────────────────────────────────────────
    private static readonly int[][,] tier4 = new int[][,]
    {
        // straight 5 H
        new int[,] { {1,1,1,1,1} },

        // straight 5 V
        new int[,] { {1},{1},{1},{1},{1} },

        // L-pentomino variants
        new int[,]
        {
            {1,0,0},
            {1,0,0},
            {1,0,0},
            {1,1,1},
        },
        new int[,]
        {
            {0,0,1},
            {1,1,1},
            {0,0,1},
            {0,0,1},
        },
        new int[,]
        {
            {1,1,1},
            {1,0,0},
            {1,0,0},
        },
        new int[,]
        {
            {1,0,0},
            {1,1,1},
            {0,0,1},
        },

        // T-pentomino (cross arms)
        new int[,]
        {
            {0,0,1},
            {1,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {1,0,0},
            {1,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {0,1,0},
            {1,1,1},
            {0,1,0}
        },
        new int[,]
        {
            {1,1,0},
            {0,1,1},
            {0,0,0}
        },
        new int[,]
        {
            {0,1,1},
            {1,1,0},
            {0,0,0}
        },

        // Big L (original game shapes)
        new int[,]
        {
            {0,0,1},
            {0,0,1},
            {1,1,1},
        },
        new int[,]
        {
            {1,1,1},
            {0,1,0},
            {0,0,0}
        },
    };

    // ─────────────────────────────────────────────────────────
    //  Flat index lookup (built once on first access)
    // ─────────────────────────────────────────────────────────

    private static int[][,] _all;
    private static int[] _tierStartIndex;   // first global index of each tier (0-based)
    private static int[] _tierEndIndex;     // last global index of each tier (exclusive)

    /// <summary>Total number of tiers (1-based public API).</summary>
    public const int TierCount = 4;

    private static void EnsureBuilt()
    {
        if (_all != null) return;

        var tiers = new[] { tier1, tier2, tier3, tier4 };
        var list  = new List<int[,]>();
        _tierStartIndex = new int[TierCount];
        _tierEndIndex   = new int[TierCount];

        for (int t = 0; t < TierCount; t++)
        {
            _tierStartIndex[t] = list.Count;
            foreach (var poly in tiers[t])
            {
                var copy  = (int[,])poly.Clone();
                ReverseRows(copy);
                list.Add(copy);
            }
            _tierEndIndex[t] = list.Count;
        }

        _all = list.ToArray();
    }

    // ─────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────

    public static int[,] Get(int index)
    {
        EnsureBuilt();
        return _all[index];
    }

    public static int Length
    {
        get { EnsureBuilt(); return _all.Length; }
    }

    /// <summary>Pick a random polyomino index from a specific tier (1–4).</summary>
    public static int RandomIndexFromTier(int tier)
    {
        EnsureBuilt();
        int t = Mathf.Clamp(tier, 1, TierCount) - 1;
        return Random.Range(_tierStartIndex[t], _tierEndIndex[t]);
    }

    /// <summary>
    /// Return a random polyomino index weighted towards lower tiers at low scores
    /// and higher tiers at high scores.
    ///
    /// Score thresholds (configurable):
    ///   0–199   : mostly Tier 1–2
    ///   200–499 : Tier 1–3
    ///   500+    : all tiers, Tier 3–4 dominant
    /// </summary>
    public static int RandomIndexForScore(int score)
    {
        EnsureBuilt();

        // Build a cumulative weight table for 4 tiers
        float t1, t2, t3, t4;

        if (score < 200)
        {
            t1 = 0.35f; t2 = 0.45f; t3 = 0.18f; t4 = 0.02f;
        }
        else if (score < 500)
        {
            t1 = 0.20f; t2 = 0.35f; t3 = 0.35f; t4 = 0.10f;
        }
        else if (score < 1000)
        {
            t1 = 0.10f; t2 = 0.25f; t3 = 0.40f; t4 = 0.25f;
        }
        else
        {
            t1 = 0.05f; t2 = 0.15f; t3 = 0.40f; t4 = 0.40f;
        }

        // Apply HardPieceRateDown buff: each level reduces t3+t4 by 5% total
        // Reduction is split evenly between t3 and t4, and redistributed to t1+t2
        float hardReduction = BuffManager.Instance?.HardPieceRateReduction ?? 0f;
        if (hardReduction > 0f)
        {
            float reduce3 = Mathf.Min(t3, hardReduction * 0.5f);
            float reduce4 = Mathf.Min(t4, hardReduction * 0.5f);
            t3 -= reduce3;
            t4 -= reduce4;
            // Redistribute equally to t1 and t2
            float gained = reduce3 + reduce4;
            t1 += gained * 0.5f;
            t2 += gained * 0.5f;
        }

        float roll = Random.value;
        int chosenTier;
        if      (roll < t1)           chosenTier = 1;
        else if (roll < t1 + t2)      chosenTier = 2;
        else if (roll < t1 + t2 + t3) chosenTier = 3;
        else                           chosenTier = 4;

        return RandomIndexFromTier(chosenTier);
    }

    // ─────────────────────────────────────────────────────────
    //  Internal helpers
    // ─────────────────────────────────────────────────────────

    static void ReverseRows(int[,] polyomino)
    {
        int rows = polyomino.GetLength(0);
        int cols = polyomino.GetLength(1);
        for (int r = 0; r < rows / 2; r++)
        {
            int bot = rows - 1 - r;
            for (int c = 0; c < cols; c++)
            {
                int tmp = polyomino[r, c];
                polyomino[r, c] = polyomino[bot, c];
                polyomino[bot, c] = tmp;
            }
        }
    }
}
