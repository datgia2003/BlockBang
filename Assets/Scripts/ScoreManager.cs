using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    private int score;
    private int currentCombo = 0;
    private bool clearedLineInTurn = false;

    /// <summary>Current score — read by Blocks for difficulty scaling.</summary>
    public int Score => score;
    public int Combo => currentCombo;


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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        UpdateScoreText();
        UpdateComboText();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();

        LevelModeManager.Instance?.OnGoalProgress(LevelGoalType.Score, amount);

        // Change background music per 500 points
        SoundManager.Instance?.ChangeMusicLevel(score / 500);

        // Notify buff milestone system
        BuffManager.Instance?.OnScoreChanged(score);
    }

    /// <summary>
    /// Processes cleared cells and lines using the formula: Score = (CellClear x 5) x ComboMultiplier.
    /// Returns the calculated score amount for visual popups.
    /// </summary>
    public int AddClears(int cellCount, int lineCount, Vector3? popupPos = null)
    {
        if (lineCount > 0)
        {
            currentCombo += lineCount;
            clearedLineInTurn = true;

            if (popupPos.HasValue)
            {
                ComboPopup.Create(popupPos.Value, currentCombo);
            }
        }

        float comboMult = 1f + (currentCombo * 0.1f);
        int finalAmount = Mathf.RoundToInt(cellCount * 5 * comboMult);

        // Apply score multiplier buff (if any) - Disabled in Level Mode
        if (BuffManager.Instance != null && (LevelModeManager.Instance == null || !LevelModeManager.Instance.IsLevelModeActive))
        {
            float buffMult = BuffManager.Instance.ScoreMultiplier;
            if (buffMult > 1f)
                finalAmount = Mathf.RoundToInt(finalAmount * buffMult);
        }

        AddScore(finalAmount);
        UpdateComboText();

        return finalAmount;
    }

    /// <summary>
    /// Resets combo if no lines were cleared during the turn activity.
    /// Call this when a turn (including all fallout effects) is truly finished.
    /// </summary>
    public void EndTurn()
    {
        if (!clearedLineInTurn)
        {
            currentCombo = 0;
            UpdateComboText();
        }
        clearedLineInTurn = false;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void UpdateComboText()
    {
        if (comboText != null)
        {
            comboText.text = currentCombo > 0 ? $"Combo x{1.0f + currentCombo * 0.1f:0.0}" : "";
            
            // Visual juice for combo increase
            if (currentCombo > 0 && JuiceManager.Instance != null)
                JuiceManager.Instance.PunchScale(comboText.transform, 0.4f, 0.2f);
        }
    }
}

public class ScorePopup : MonoBehaviour
{
    private static Color[] colors = {
        new Color(1f, 0.8f, 0f),   // Yellow/Gold
        new Color(0.2f, 1f, 0.2f), // Bright Green
        new Color(0.2f, 0.8f, 1f), // Cyan
        new Color(1f, 0.3f, 0.8f), // Pink
        new Color(1f, 0.4f, 0.2f)  // Orange
    };

    public static void Create(Vector3 worldPos, int amount, int style = 0)
    {
        GameObject go = new GameObject("ScorePopup_" + amount);
        go.transform.position = worldPos;
        
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "+" + amount.ToString();
        tmp.fontSize = 6f; 
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 500;
        tmp.fontStyle = FontStyles.Bold;

        int colorIdx = (style + amount) % colors.Length;
        tmp.color = colors[colorIdx];

        var popup = go.AddComponent<ScorePopup>();
        popup.StartCoroutine(popup.AnimateRoutine(tmp, style));
    }

    private System.Collections.IEnumerator AnimateRoutine(TextMeshPro tmp, int style)
    {
        float duration = 1.0f;
        float elapsed = 0f;
        
        Vector3 startPos = transform.position;
        Color startColor = tmp.color;

        int animType = style % 3;
        
        startPos += new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0f);
        
        float driftY = Random.Range(1.0f, 2.0f);
        float driftX = Random.Range(-0.5f, 0.5f);

        Vector3 originalScale = Vector3.one;
        tmp.transform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            if (animType == 0)
            {
                transform.position = startPos + new Vector3(0, t * driftY, 0);
            }
            else if (animType == 1)
            {
                float xOffset = Mathf.Sin(t * Mathf.PI * 4f) * 0.4f;
                transform.position = startPos + new Vector3(xOffset, t * driftY, 0);
            }
            else 
            {
                float currentX = Mathf.Lerp(0f, driftX * 2f, t);
                float currentY = Mathf.Sin(t * Mathf.PI) * driftY;
                transform.position = startPos + new Vector3(currentX, currentY + t * 0.5f, 0);
            }

            if (t < 0.2f)
            {
                float st = t / 0.2f;
                tmp.transform.localScale = originalScale * (st * 1.5f);
            }
            else if (t < 0.4f)
            {
                float st = (t - 0.2f) / 0.2f;
                tmp.transform.localScale = originalScale * Mathf.Lerp(1.5f, 1f, st);
            }

            if (t > 0.6f)
            {
                float ft = (t - 0.6f) / 0.4f;
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - ft);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}

public class ComboPopup : MonoBehaviour
{
    private static Color[] comboColors = {
        new Color(1f, 0.4f, 0f),      // Orange
        new Color(1f, 0.9f, 0.1f),    // Yellow
        new Color(0.1f, 1f, 0.8f),    // Cyan
        new Color(1f, 0.2f, 0.8f),    // Magenta/Pink
        new Color(1f, 0.25f, 0.25f),  // Bright Red
        new Color(0.4f, 1f, 0.4f)     // Light Green
    };

    public static void Create(Vector3 worldPos, int comboCount)
    {
        if (comboCount <= 0) return;

        GameObject go = new GameObject("ComboPopup_" + comboCount);
        // Randomize position slightly around the clear point
        go.transform.position = worldPos + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.4f, 1.0f), 0);
        
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = comboCount + " Combo";
        tmp.fontSize = 8.5f + Mathf.Min(comboCount, 10) * 0.6f; // Scale font with combo
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 600;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;

        int colorIdx = (comboCount - 1) % comboColors.Length;
        tmp.color = comboColors[colorIdx];

        var popup = go.AddComponent<ComboPopup>();
        popup.StartCoroutine(popup.AnimateRoutine(tmp));
    }

    private System.Collections.IEnumerator AnimateRoutine(TextMeshPro tmp)
    {
        float duration = 1.35f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Color startColor = tmp.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            // Wavy floating path
            float xOffset = Mathf.Sin(t * Mathf.PI * 2.5f) * 0.3f;
            transform.position = startPos + new Vector3(xOffset, t * 1.8f, 0);

            // Pop in + Pulse scale
            if (t < 0.15f)
            {
                float st = t / 0.15f;
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.4f, st);
            }
            else if (t < 0.3f)
            {
                float st = (t - 0.15f) / 0.15f;
                transform.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, st);
            }
            else
            {
                // Subtle breathing
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.05f);
            }

            // Reveal and Fade
            if (t > 0.65f)
            {
                float ft = (t - 0.65f) / 0.35f;
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - ft);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
