using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// One entry in the block spawn table: how many times a specific shape appears in this level's pool.
/// </summary>
[System.Serializable]
public class PolyominoSpawnEntry
{
    public int PolyominoIndex;
    public Element[] CustomElements = new Element[25]; // 5x5 max piece size
    [Min(0)] public int Count = 1;
}

[CreateAssetMenu(fileName = "New Level", menuName = "BlockBlast/LevelData")]
public class LevelData : ScriptableObject
{
    public string LevelName = "Level 1";
    public int LevelNumber = 1;

    [Tooltip("List of goals to complete the level.")]
    public List<LevelGoal> Goals;

    [Header("Move Limit & Discards")]
    [Tooltip("Maximum block placements allowed. 0 = unlimited.")]
    public int MaxBlockPlacements = 0;
    [Tooltip("How many blocks the player can discard/skip. -1 for unlimited.")]
    public int DiscardLimit = 3;

    [Header("Block Spawn Pool")]
    [Tooltip(
        "Fixed pool of blocks for this level.\n" +
        "Leave empty → random (endless-style).\n" +
        "If set, blocks are drawn randomly from this pool until it is exhausted.\n" +
        "MaxBlockPlacements is automatically set to the total pool size when non-zero entries exist.")]
    public List<PolyominoSpawnEntry> BlockSpawnPool = new List<PolyominoSpawnEntry>();

    [Header("Initial Board Layout")]
    [Tooltip("Board is 8×8. 0 = empty, 2 = occupied.")]
    public int[]     InitialBoardData  = new int[64];
    public Element[] InitialElements   = new Element[64];

    // ── Computed helpers ───────────────────────────────────────

    /// <summary>Total blocks in pool across all entries.</summary>
    public int TotalPoolSize
    {
        get
        {
            int total = 0;
            if (BlockSpawnPool != null)
                foreach (var e in BlockSpawnPool) total += Mathf.Max(0, e.Count);
            return total;
        }
    }

    /// <summary>True when a non-empty spawn pool is defined.</summary>
    public bool HasSpawnPool => BlockSpawnPool != null && BlockSpawnPool.Count > 0 && TotalPoolSize > 0;
}
