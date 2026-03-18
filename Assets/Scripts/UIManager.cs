using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI restartText;
    [SerializeField] private LevelData[] allLevels; // For level selection
    [SerializeField] private GameObject mainMenuButton;

    public int LevelCount => allLevels != null ? allLevels.Length : 0;

    private bool isVictory;
    private bool isGameOver;

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
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        isVictory = false;
        isGameOver = false;
    }

    private void Update()
    {
        if ((isVictory || isGameOver) && Input.GetMouseButtonDown(0))
        {
            if (isVictory)
            {
                LevelModeManager.Instance?.LoadNextLevel();
            }
            else // Game Over
            {
                var lvl = LevelModeManager.Instance;
                if (lvl != null && lvl.CurrentLevelIndex != -1)
                {
                    // Level Mode: Reload using full level setup
                    LoadLevel(lvl.CurrentLevelIndex);
                }
                else
                {
                    // Endless Mode: Just reload the scene
                    UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
                }
            }
        }
    }

    public void LoadEndlessMode()
    {
        LevelModeManager.Instance?.StopLevelMode();
        UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
    }

    public void ReturnToMenu()
    {
        LevelModeManager.Instance?.StopLevelMode();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    public void LoadLevel(int levelIndex)
    {
        if (allLevels != null && levelIndex < allLevels.Length)
        {
            LevelModeManager.Instance?.StartLevel(allLevels[levelIndex], levelIndex);
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
        }
    }

    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            isGameOver = true;
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = "GAME OVER!";
            if (restartText != null)
                restartText.text = "Touch Screen to Restart";
        }
        // === SOUND ===
        SoundManager.Instance?.Play(SoundManager.SFX.GameOver, pitchVariance: 0f);
    }

    public void ShowVictoryScreen()
    {
        if (victoryPanel != null)
        {
            isVictory = true;
            victoryPanel.SetActive(true);
        }
        SoundManager.Instance?.Play(SoundManager.SFX.LevelComplete);
    }
}
