using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Blackjack";
    public string bossMapSceneName = "BossMap";
    public string mapSceneName = "MapScene";

    private void Awake()
    {
        // Singleton pattern to ensure only one GameSceneManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load the main menu scene
    /// </summary>
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Load the game scene (Blackjack)
    /// </summary>
    public void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Load the boss map scene
    /// </summary>
    public void LoadBossMapScene()
    {
        SceneManager.LoadScene(bossMapSceneName);
    }

    /// <summary>
    /// Load the map scene (procedural node-based map)
    /// </summary>
    public void LoadMapScene()
    {
        SceneManager.LoadScene(mapSceneName);
    }

    /// <summary>
    /// Quit the application
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>
    /// Restart the current scene
    /// </summary>
    public void RestartCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
} 