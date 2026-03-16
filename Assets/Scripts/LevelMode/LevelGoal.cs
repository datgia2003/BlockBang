using UnityEngine;

[System.Serializable]
public class LevelGoal
{
    public LevelGoalType GoalType;
    public int TargetAmount;
    
    [HideInInspector]
    public int CurrentAmount;
    
    public bool IsCompleted => CurrentAmount >= TargetAmount;
}
