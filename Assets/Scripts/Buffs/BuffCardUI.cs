using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single buff choice card inside the BuffSelectionUI panel.
/// Handles its own hover effect and click callback.
/// </summary>
public class BuffCardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image          iconImage;
    [SerializeField] private Image          accentBorder;
    [SerializeField] private TextMeshProUGUI buffNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button         selectButton;

    // ── State ──────────────────────────────────────────────────
    private BuffDefinition  definition;
    private BuffSelectionUI owner;
    private int             cardIndex;

    // ── Bind ───────────────────────────────────────────────────

    public void Bind(BuffDefinition def, BuffSelectionUI selectionUI, int index)
    {
        definition  = def;
        owner       = selectionUI;
        cardIndex   = index;

        // Icon / colors
        if (iconImage != null && def.Icon != null)
            iconImage.sprite = def.Icon;

        if (accentBorder != null)
            accentBorder.color = def.AccentColor;

        // Texts
        if (buffNameText != null)
            buffNameText.text = def.BuffName;

        // Level context
        int currentLevel = BuffManager.Instance != null ? BuffManager.Instance.GetBuffLevel(def.BuffType) : 0;
        int nextLevel    = currentLevel + 1;

        if (levelText != null)
        {
            if (def.MaxLevel <= 1)
                levelText.text = "";  // one-time buff, no level label
            else
                levelText.text = $"Lv {nextLevel}/{def.MaxLevel}";
        }

        if (descriptionText != null)
            descriptionText.text = def.GetLevelDescription(nextLevel);

        // Button
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnClick);
    }

    // ── Interaction ────────────────────────────────────────────

    private void OnClick()
    {
        owner?.OnCardChosen(definition);
    }

    // ── Enter animation (scale punch, unscaled) ────────────────

    public void PlayEnterAnimation(float delay)
    {
        StartCoroutine(EnterRoutine(delay));
    }

    private IEnumerator EnterRoutine(float delay)
    {
        transform.localScale = Vector3.zero;

        if (delay > 0f)
        {
            float elapsed = 0f;
            while (elapsed < delay) { elapsed += Time.unscaledDeltaTime; yield return null; }
        }

        // Spring to full scale
        float t = 0f;
        float dur = 0.22f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float scale = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(Mathf.Clamp01(t)));
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
