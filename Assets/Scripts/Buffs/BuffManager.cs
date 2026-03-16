using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks:
///  - Which buffs the player has picked and at what level.
///  - Score milestones (100, 200, 400, 800, … xn = xn-1 * 2).
///  - Triggering the buff selection UI when a milestone is reached.
/// Also exposes helpers used by ElementRegistry, Polyominos, Board, ScoreManager, etc.
/// </summary>
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────
    [Header("All Buff Definitions (assign all 8)")]
    [SerializeField] private List<BuffDefinition> allBuffDefinitions;

    [Header("References")]
    [SerializeField] private BuffSelectionUI buffSelectionUI;

    // ── Milestone progression ──────────────────────────────────
    // Sequence: 50, 88, 154, 270, 472, 826, 1445, 2529, 4426, 7745, 13554, 23719, 41508, 72639, 127118, 222456, 389300, 681275, 1192231, 2086404, 3651207, 6390000
    private int currentMilestoneIndex = 0;
    private int nextMilestoneScore    = 50;

    // How many buffs are offered each time
    private const int OfferedCount = 3;

    // ── Active buff state ──────────────────────────────────────
    // Maps BuffType → current level (0 = not picked)
    private readonly Dictionary<BuffType, int> buffLevels = new();

    // ── Public read-only accessors ─────────────────────────────

    private bool IsBuffEnabled => LevelModeManager.Instance == null || LevelModeManager.Instance.CurrentLevel == null;

    public int GetBuffLevel(BuffType type)
    {
        if (!IsBuffEnabled) return 0;
        buffLevels.TryGetValue(type, out int lvl);
        return lvl;
    }

    public bool HasBuff(BuffType type) => IsBuffEnabled && GetBuffLevel(type) > 0;

    // ── Buff-specific value helpers ────────────────────────────

    /// <summary>Additive rate bonus for Lightning spawn (0 → +0.05 → +0.10 → +0.15).</summary>
    public float LightningRateBonus    => IsBuffEnabled ? GetBuffLevel(BuffType.LightningRateUp)    * 0.05f : 0f;

    /// <summary>Additive rate bonus for Fire spawn.</summary>
    public float FireRateBonus         => IsBuffEnabled ? GetBuffLevel(BuffType.FireRateUp)          * 0.05f : 0f;

    /// <summary>Additive rate reduction for Ice spawn (0.05 per level).</summary>
    public float IceRateReduction      => IsBuffEnabled ? GetBuffLevel(BuffType.IceRateDown)         * 0.05f : 0f;

    /// <summary>Cooldown reduction in turns for swap skill.</summary>
    public int SkillCooldownReduction  => IsBuffEnabled ? GetBuffLevel(BuffType.SkillCooldownReduce) : 0;

    /// <summary>Additive reduction to hard piece weight (0.05 per level).</summary>
    public float HardPieceRateReduction => IsBuffEnabled ? GetBuffLevel(BuffType.HardPieceRateDown)  * 0.05f : 0f;

    /// <summary>Score multiplier: level 0=1x, 1=1.5x, 2=1.75x, 3=2x.</summary>
    public float ScoreMultiplier
    {
        get
        {
            if (!IsBuffEnabled) return 1.0f;
            int lvl = GetBuffLevel(BuffType.ScoreMultiplier);
            return lvl switch { 1 => 1.5f, 2 => 1.75f, 3 => 2.0f, _ => 1.0f };
        }
    }

    /// <summary>True if the player can clear row/col with 7 consecutive cells.</summary>
    public bool SevenCellClearEnabled  => IsBuffEnabled && HasBuff(BuffType.SevenCellClear);

    /// <summary>True if the player can clear diagonal lines.</summary>
    public bool DiagonalClearEnabled   => IsBuffEnabled && HasBuff(BuffType.DiagonalClear);

    // ── Unity lifecycle ────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    // ── Called by ScoreManager every time score changes ────────

    /// <summary>
    /// Call this after each score addition.
    /// Returns true when a milestone is crossed so callers know a pause happened.
    /// </summary>
    public void OnScoreChanged(int newScore)
    {
        // Buff system is disabled in Level Mode
        if (LevelModeManager.Instance != null && LevelModeManager.Instance.IsLevelModeActive)
            return;

        if (newScore >= nextMilestoneScore)
        {
            TriggerBuffSelection();
        }
    }

    private void TriggerBuffSelection()
    {
        // Advance milestone: xn = x(n-1) * 2
        currentMilestoneIndex++;
        nextMilestoneScore = ComputeMilestone(currentMilestoneIndex);

        // Pick 3 random buffs to offer (avoid maxed-out ones)
        var offered = PickOfferedBuffs(OfferedCount);
        if (offered.Count == 0) return; // all buffs maxed, nothing to show

        buffSelectionUI.Show(offered);
    }

    /// <summary>
    /// Called by BuffSelectionUI when the player clicks a buff card.
    /// </summary>
    public void ApplyBuff(BuffDefinition def)
    {
        buffLevels.TryGetValue(def.BuffType, out int current);
        buffLevels[def.BuffType] = current + 1;

        Debug.Log($"[Buff] Applied {def.BuffName} → level {buffLevels[def.BuffType]}");
    }

    // ── Milestone math ─────────────────────────────────────────
    // n=0 → 50, n=1 → 200, n=2 → 400, n=3 → 800, …
    private static int ComputeMilestone(int index)
    {
        int v = 50;
        for (int i = 0; i < index; i++)
            v = (int)(v * 1.75f);
        return v;
    }

    // ── Buff selection ─────────────────────────────────────────
    private List<BuffDefinition> PickOfferedBuffs(int count)
    {
        // Gather eligible (not maxed out)
        var eligible = new List<BuffDefinition>();
        foreach (var def in allBuffDefinitions)
        {
            buffLevels.TryGetValue(def.BuffType, out int lvl);
            if (lvl < def.MaxLevel)
                eligible.Add(def);
        }

        // Shuffle
        for (int i = eligible.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
        }

        // Return up to 'count'
        var result = new List<BuffDefinition>();
        for (int i = 0; i < Mathf.Min(count, eligible.Count); i++)
            result.Add(eligible[i]);
        return result;
    }
}
