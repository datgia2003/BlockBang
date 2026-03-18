using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscardButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeModeColor = Color.yellow;
    [SerializeField] private Color disabledColor = Color.gray;

    private Blocks blocksManager;

    private void Start()
    {
        blocksManager = FindObjectOfType<Blocks>();
        if (button != null)
        {
            button.onClick.AddListener(OnBtnClick);
        }
        Refresh();
    }

    private void OnEnable()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.OnDiscardUsed += Refresh;
            LevelModeManager.Instance.OnGoalUpdated += Refresh;
        }
    }

    private void OnDisable()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.OnDiscardUsed -= Refresh;
            LevelModeManager.Instance.OnGoalUpdated -= Refresh;
        }
    }

    private void OnBtnClick()
    {
        if (blocksManager != null)
        {
            blocksManager.ToggleDiscardMode();
            Refresh();
        }
    }

    private void Refresh()
    {
        var mgr = LevelModeManager.Instance;
        if (mgr == null || !mgr.IsLevelModeActive)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (countText != null)
        {
            countText.text = $"Discard ({mgr.DiscardsLeft})";
        }

        if (button != null)
        {
            bool canDiscard = mgr.DiscardsLeft > 0;
            // Cost check: must have at least 2 moves left (if moves limited)
            if (mgr.HasMoveLimit && mgr.MovesLeft < 2) canDiscard = false;

            button.interactable = canDiscard;

            // Highlight if mode is toggled on
            var currentBlocks = FindObjectOfType<Blocks>();
            if (currentBlocks != null && currentBlocks.IsDiscardMode)
            {
                countText.color = activeModeColor;
            }
            else
            {
                countText.color = canDiscard ? normalColor : disabledColor;
            }
        }
    }
}
