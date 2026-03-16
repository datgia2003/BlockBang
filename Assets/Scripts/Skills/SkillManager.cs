using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [SerializeField] private List<Skill> skills;
    [SerializeField] private Blocks blocksManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        foreach (var skill in skills)
        {
            skill.Reset();
            if (skill is SwapBlocksSkill swapSkill)
            {
                swapSkill.Initialize(blocksManager);
            }
        }
    }

    public void OnBlockPlaced()
    {
        // Skills are disabled in Level Mode
        if (LevelModeManager.Instance != null && LevelModeManager.Instance.CurrentLevel != null)
            return;

        foreach (var skill in skills)
        {
            skill.OnBlockPlaced();
        }
    }

    public void ActivateSkill(int skillIndex)
    {
        // Skills are disabled in Level Mode
        if (LevelModeManager.Instance != null && LevelModeManager.Instance.CurrentLevel != null)
            return;

        if (skillIndex >= 0 && skillIndex < skills.Count)
        {
            skills[skillIndex].Activate();
        }
    }

    public bool AreAnySkillsReady()
    {
        // Skills cannot rescue player in Level Mode
        if (LevelModeManager.Instance != null && LevelModeManager.Instance.CurrentLevel != null)
            return false;

        foreach (var skill in skills)
        {
            if (skill.IsReady())
                return true;
        }
        return false;
    }

    public List<Skill> GetSkills()
    {
        return skills;
    }
}
