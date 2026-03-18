using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private LevelData firstLevel;

    public void OnClickEndlessMode()
    {
        if (LevelModeManager.Instance != null)
        {
            LevelModeManager.Instance.StopLevelMode();
        }
        // Explicitly load SampleScene
        SceneManager.LoadScene("SampleScene");
    }

    public void OnClickLevelMode()
    {
        Debug.Log("[MainMenu] Level Mode Clicked");
        
        // 1. Try to use UIManager if it exists in the scene
        if (UIManager.Instance != null && UIManager.Instance.LevelCount > 0)
        {
            Debug.Log("[MainMenu] Using UIManager to load level 0");
            UIManager.Instance.LoadLevel(0);
            return;
        }

        // 2. Fallback: Use LevelModeManager directly
        if (LevelModeManager.Instance != null && firstLevel != null)
        {
            Debug.Log("[MainMenu] UIManager not found, using LevelModeManager directly");
            LevelModeManager.Instance.StartLevel(firstLevel, 0);
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            Debug.LogError("[MainMenu] Critical Error: No Managers or LevelData found!");
            SceneManager.LoadScene("SampleScene");
        }
    }
    
}
