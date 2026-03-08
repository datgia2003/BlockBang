using UnityEngine;

/// <summary>
/// Defines a single buff: its type, max level, and per-level descriptions.
/// Create via Assets > BlockBlast > Buff Definition.
/// </summary>
[CreateAssetMenu(fileName = "NewBuff", menuName = "BlockBlast/Buff Definition")]
public class BuffDefinition : ScriptableObject
{
    [Header("Identity")]
    public BuffType BuffType;
    public string   BuffName;
    [TextArea(2, 4)]
    public string   BuffDescription; // uses {value} token for level-specific replacement

    [Header("Visuals")]
    public Sprite   Icon;
    public Color    AccentColor = Color.white;

    [Header("Levels")]
    [Tooltip("How many times this buff can be stacked (1 for one-time buffs, 3 for leveled).")]
    public int MaxLevel = 3;

    [Tooltip("Description shown per level. Index 0 = level 1, etc.")]
    public string[] LevelDescriptions;

    /// <summary>Returns the description for a given 1-based target level.</summary>
    public string GetLevelDescription(int targetLevel)
    {
        if (LevelDescriptions == null || LevelDescriptions.Length == 0)
            return BuffDescription;
        int idx = Mathf.Clamp(targetLevel - 1, 0, LevelDescriptions.Length - 1);
        return LevelDescriptions[idx];
    }
}
