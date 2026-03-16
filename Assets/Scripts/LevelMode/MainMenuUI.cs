using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private LevelData firstLevel;

    public void OnClickEndlessMode()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.StopLevelMode();
        }
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickLevelMode()
    {
        if (UIManager.Instance != null && UIManager.Instance.LevelCount > 0)
        {
            UIManager.Instance.LoadLevel(0);
        }
        else if (LevelModeManager.Instance != null && firstLevel != null)
        {
            // Fallback for cases where UIManager isn't in menu scene
            LevelModeManager.Instance.StartLevel(firstLevel, 0);
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogWarning("UIManager or Level Data not found!");
            SceneManager.LoadScene(gameSceneName);
        } 
    }
}
