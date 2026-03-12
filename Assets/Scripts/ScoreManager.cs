using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    private int score;

    /// <summary>Current score — read by Blocks for difficulty scaling.</summary>
    public int Score => score;


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
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        // Apply score multiplier buff (if any)
        if (BuffManager.Instance != null)
        {
            float mult = BuffManager.Instance.ScoreMultiplier;
            if (mult > 1f)
                amount = Mathf.RoundToInt(amount * mult);
        }

        score += amount;
        UpdateScoreText();

        // Change background music per 500 points
        SoundManager.Instance?.ChangeMusicLevel(score / 500);

        // Notify buff milestone system
        BuffManager.Instance?.OnScoreChanged(score);
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
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
