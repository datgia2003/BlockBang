using UnityEngine;

public abstract class Skill : ScriptableObject
{
    [SerializeField] private string skillName;
    [SerializeField] private int cooldown;
    private int currentCooldown;

    public string SkillName => skillName;
    public int Cooldown => cooldown;
    public int CurrentCooldown => currentCooldown;

    public bool IsReady()
    {
        return currentCooldown <= 0;
    }

    public void Reset()
    {
        currentCooldown = 0;
    }

    public void OnBlockPlaced()
    {
        if (currentCooldown > 0)
        {
            currentCooldown--;
        }
    }

    public void Activate()
    {
        if (IsReady())
        {
            Execute();
            currentCooldown = cooldown;
        }
    }

    public abstract void Execute();
}
