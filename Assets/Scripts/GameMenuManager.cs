using UnityEngine;
using UnityEngine.UI;

public class GameMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button mainMenuButton;
    public Button restartButton;
    public GameObject pauseMenu;
    public KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused = false;

    private void Start()
    {
        // Setup button listeners
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonClicked);

        // Make sure pause menu is closed initially
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    private void Update()
    {
        // Handle pause key input
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePauseMenu();
        }
    }

    /// <summary>
    /// Called when Main Menu button is clicked
    /// </summary>
    public void OnMainMenuButtonClicked()
    {
        // Resume time before switching scenes
        Time.timeScale = 1f;
        
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadMainMenu();
        }
        else
        {
            // Fallback if GameSceneManager instance is not available
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Called when Restart button is clicked
    /// </summary>
    public void OnRestartButtonClicked()
    {
        // Resume time before restarting
        Time.timeScale = 1f;
        
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.RestartCurrentScene();
        }
        else
        {
            // Fallback if GameSceneManager instance is not available
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Toggle the pause menu
    /// </summary>
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }

        // Pause/unpause the game
        Time.timeScale = isPaused ? 0f : 1f;
    }

    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClicked);
        
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartButtonClicked);
        
        // Make sure time scale is reset
        Time.timeScale = 1f;
    }
} 