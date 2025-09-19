using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button pauseButton;        // Button to toggle pause menu
    public Button resumeButton;       // Button to resume game (hide panel)
    public Button inventoryButton;    // Button to open inventory
    public Button mainMenuButton;     // Optional: return to main menu
    public Button restartButton;      // Optional: restart game
    public GameObject pauseMenuPanel; // The panel that slides down

    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public float slideDistance = 500f;
    
    [Header("Inventory")]
    public InventoryPanelUI inventoryPanelUI; // Reference to inventory panel

    private bool isPaused = false;
    private Vector3 originalPosition;
    private Vector3 hiddenPosition;
    private RectTransform panelRect;

    private void Start()
    {
        // Get the panel's RectTransform
        if (pauseMenuPanel != null)
            panelRect = pauseMenuPanel.GetComponent<RectTransform>();

        // Store positions
        if (panelRect != null)
        {
            originalPosition = panelRect.anchoredPosition;
            hiddenPosition = new Vector3(originalPosition.x, originalPosition.y + slideDistance, originalPosition.z);
        }

        // Setup button listeners
        if (pauseButton != null)
            pauseButton.onClick.AddListener(TogglePauseMenu);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(HidePauseMenu);

        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        // Hide panel initially
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle pause menu - called when pause button is clicked
    /// </summary>
    public void TogglePauseMenu()
    {
        if (isPaused)
        {
            HidePauseMenu();
        }
        else
        {
            ShowPauseMenu();
        }
    }

    /// <summary>
    /// Show pause menu
    /// </summary>
    public void ShowPauseMenu()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f; // Pause the game

        // Show panel
        pauseMenuPanel.SetActive(true);

        // Set to hidden position first
        panelRect.anchoredPosition = hiddenPosition;

        // Animate sliding down
        panelRect.DOAnchorPos(originalPosition, animationDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true); // Ignore time scale
    }

    /// <summary>
    /// Hide pause menu - called by both pause button (toggle) and resume button
    /// </summary>
    public void HidePauseMenu()
    {
        if (!isPaused) return;

        // Animate sliding up
        panelRect.DOAnchorPos(hiddenPosition, animationDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
                pauseMenuPanel.SetActive(false);
                isPaused = false;
                Time.timeScale = 1f; // Resume the game
            });
    }

    /// <summary>
    /// Called when Inventory button is clicked
    /// </summary>
    public void OnInventoryButtonClicked()
    {
        Debug.Log($"🎯 GameMenuManager.OnInventoryButtonClicked() called! isPaused={isPaused}, inventoryPanelUI={inventoryPanelUI != null}");
        
        if (inventoryPanelUI != null)
        {
            // First, hide the pause menu and resume the game
            // Use a callback to open inventory after pause menu animation completes
            HidePauseMenuAndOpenInventory();
        }
        else
        {
            Debug.LogWarning("Inventory Panel UI not assigned in Game Menu Manager!");
        }
    }
    
    /// <summary>
    /// Hide pause menu and then open inventory
    /// </summary>
    private void HidePauseMenuAndOpenInventory()
    {
        Debug.Log($"🔧 HidePauseMenuAndOpenInventory() called! isPaused={isPaused}");
        
        if (!isPaused) 
        {
            // If not paused, just open inventory directly
            Debug.Log("📂 Game not paused, opening inventory directly");
            inventoryPanelUI.ShowInventory();
            return;
        }

        Debug.Log("⏸️ Game is paused, hiding pause menu first then opening inventory");
        
        // Animate sliding up the pause menu
        panelRect.DOAnchorPos(hiddenPosition, animationDuration)
            .SetEase(Ease.InBack)
            .SetUpdate(true)
            .OnComplete(() => {
                Debug.Log("✅ Pause menu animation complete, resuming game and opening inventory");
                pauseMenuPanel.SetActive(false);
                isPaused = false;
                Time.timeScale = 1f; // Resume the game
                
                // Now open the inventory
                inventoryPanelUI.ShowInventory();
            });
    }

    /// <summary>
    /// Go to main menu
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadMainMenu();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    /// <summary>
    /// Restart the game
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.RestartCurrentScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void OnDestroy()
    {
        // Clean up
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(TogglePauseMenu);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(HidePauseMenu);

        if (inventoryButton != null)
            inventoryButton.onClick.RemoveListener(OnInventoryButtonClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartGame);

        DOTween.Kill(this);
        Time.timeScale = 1f;
    }
} 