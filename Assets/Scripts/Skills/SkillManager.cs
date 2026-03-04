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
        foreach (var skill in skills)
        {
            skill.OnBlockPlaced();
        }
    }

    public void ActivateSkill(int skillIndex)
    {
        if (skillIndex >= 0 && skillIndex < skills.Count)
        {
            skills[skillIndex].Activate();
        }
    }

    public bool AreAnySkillsReady()
    {
        foreach (var skill in skills)
        {
            if (skill.IsReady())
            {
                return true;
            }
        }
        return false;
    }

    public List<Skill> GetSkills()
    {
        return skills;
    }
}
