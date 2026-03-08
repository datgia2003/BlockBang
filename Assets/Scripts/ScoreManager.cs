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
