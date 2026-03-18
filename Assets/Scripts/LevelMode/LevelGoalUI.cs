using UnityEngine;
using TMPro;
using System.Text;

public class LevelGoalUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goalsText;
    [SerializeField] private TextMeshProUGUI movesText;   // optional separate text for move counter
    [SerializeField] private TextMeshProUGUI discardText; // NEW: text for discard counter
    [SerializeField] private GameObject container;

    private void Start()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.OnGoalUpdated += UpdateUI;
            LevelModeManager.Instance.OnMoveUsed    += UpdateUI;
            LevelModeManager.Instance.OnDiscardUsed += UpdateUI;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        var mgr = LevelModeManager.Instance;
        if (mgr == null || !mgr.IsLevelModeActive)
        {
            if (container != null) container.SetActive(false);
            return;
        }

        if (container != null) container.SetActive(true);

        // ── Goals text ────────────────────────────────────────────
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>{mgr.CurrentLevel.LevelName}</b>");

        foreach (var goal in mgr.RuntimeGoals)
        {
            string tick = goal.IsCompleted ? " <color=green>✓</color>" : "";
            sb.AppendLine($"{GetGoalName(goal.GoalType)}: {goal.CurrentAmount}/{goal.TargetAmount}{tick}");
        }

        if (goalsText != null)
            goalsText.text = sb.ToString();

        // ── Moves text ────────────────────────────────────────────
        if (movesText != null)
        {
            if (mgr.HasMoveLimit)
            {
                movesText.text = $"Moves: {mgr.MovesLeft}";
                movesText.color = mgr.MovesLeft <= 5
                    ? new Color(1f, 0.3f, 0.3f)   // red warning
                    : Color.white;
            }
        }

        // ── Discard text ──────────────────────────────────────────
        if (discardText != null)
        {
            if (mgr.DiscardsLeft >= 0)
            {
                discardText.text = $"Discards: {mgr.DiscardsLeft}";
                discardText.color = mgr.DiscardsLeft == 0 ? Color.grey : Color.white;
            }
            else
            {
                discardText.text = "";
            }
        }
    }

    private string GetGoalName(LevelGoalType type)
    {
        return type switch
        {
            LevelGoalType.Score          => "Score",
            LevelGoalType.ClearLines     => "Lines",
            LevelGoalType.ClearColumns   => "Columns",
            LevelGoalType.ClearRows      => "Rows",
            LevelGoalType.TriggerLightning => "Lightning",
            LevelGoalType.TriggerFire    => "Fire",
            LevelGoalType.TriggerIce     => "Ice",
            _                            => type.ToString()
        };
    }

    private void OnDestroy()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.OnGoalUpdated -= UpdateUI;
            LevelModeManager.Instance.OnMoveUsed    -= UpdateUI;
            LevelModeManager.Instance.OnDiscardUsed -= UpdateUI;
        }
    }
}
