using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public Button tutorialButton;      // New tutorial button
    public Button inventoryButton;     // New inventory button
    public Button settingsButton;
    public Button quitButton;
    // public TextMeshProUGUI titleText;
    public GameObject settingsPanel;

    [Header("Tutorial Settings Panel")]
    public GameObject tutorialSettingsPanel; // Panel for tutorial options
    public Button replayTutorialButton;      // Button to replay tutorial
    public Toggle tutorialEnabledToggle;     // Toggle to enable/disable tutorial for new players
    public Button tutorialSettingsCloseButton;

    [Header("Inventory")]
    public InventoryPanelUIV3 inventoryPanelUIV3; // Reference to V3 inventory panel

    [Header("Audio")]
    public AudioSource buttonClickSound;

    private void Start()
    {
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
        
        if (inventoryButton != null)
            inventoryButton.onClick.AddListener(OnInventoryButtonClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        // Setup tutorial settings
        if (replayTutorialButton != null)
            replayTutorialButton.onClick.AddListener(OnReplayTutorialClicked);
        
        if (tutorialSettingsCloseButton != null)
            tutorialSettingsCloseButton.onClick.AddListener(CloseTutorialSettings);

        // Make sure panels are closed initially
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (tutorialSettingsPanel != null)
            tutorialSettingsPanel.SetActive(false);

        // Initialize tutorial settings
        InitializeTutorialSettings();

        // Set title text
        // if (titleText != null)
        //     titleText.text = "BLACKJACK";
    }

    private void InitializeTutorialSettings()
    {
        // Set up tutorial enabled toggle based on PlayerPrefs
        if (tutorialEnabledToggle != null)
        {
            bool tutorialEnabled = PlayerPrefs.GetInt("TutorialEnabledForNewPlayers", 1) == 1;
            tutorialEnabledToggle.isOn = tutorialEnabled;
            tutorialEnabledToggle.onValueChanged.AddListener(OnTutorialEnabledChanged);
        }
    }

    /// <summary>
    /// Called when Play button is clicked
    /// </summary>
    public void OnPlayButtonClicked()
    {
        PlayButtonSound();
        
        // Load the boss map scene (new flow: MainMenu -> BossMap -> Blackjack)
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadBossMapScene();
        }
        else
        {
            // Fallback if GameSceneManager instance is not available
            UnityEngine.SceneManagement.SceneManager.LoadScene("BossMap");
        }
    }

    /// <summary>
    /// Called when Tutorial button is clicked
    /// </summary>
    public void OnTutorialButtonClicked()
    {
        PlayButtonSound();
        
        // Reset tutorial progress and start game with tutorial
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();
        
        // Load the game scene - tutorial will start automatically
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }
    }

    /// <summary>
    /// Called when Inventory button is clicked
    /// </summary>
    public void OnInventoryButtonClicked()
    {
        PlayButtonSound();
        
        if (inventoryPanelUIV3 != null)
        {
            inventoryPanelUIV3.ToggleInventory();
        }
        else
        {
            Debug.LogWarning("Inventory Panel UIV3 not assigned in Main Menu Manager!");
        }
    }

    /// <summary>
    /// Called when Settings button is clicked
    /// </summary>
    public void OnSettingsButtonClicked()
    {
        PlayButtonSound();
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    /// <summary>
    /// Called when Quit button is clicked
    /// </summary>
    public void OnQuitButtonClicked()
    {
        PlayButtonSound();
        
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.QuitGame();
        }
        else
        {
            // Fallback if GameSceneManager instance is not available
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    /// <summary>
    /// Called when Replay Tutorial button is clicked
    /// </summary>
    public void OnReplayTutorialClicked()
    {
        PlayButtonSound();
        
        // Reset tutorial progress
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();
        
        // Close settings panel
        if (tutorialSettingsPanel != null)
            tutorialSettingsPanel.SetActive(false);
        
        // Load game scene with tutorial
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }
    }

    /// <summary>
    /// Called when tutorial enabled toggle changes
    /// </summary>
    public void OnTutorialEnabledChanged(bool enabled)
    {
        PlayerPrefs.SetInt("TutorialEnabledForNewPlayers", enabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Tutorial for new players " + (enabled ? "enabled" : "disabled"));
    }

    /// <summary>
    /// Show tutorial settings panel
    /// </summary>
    public void ShowTutorialSettings()
    {
        PlayButtonSound();
        
        if (tutorialSettingsPanel != null)
        {
            tutorialSettingsPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Close tutorial settings panel
    /// </summary>
    public void CloseTutorialSettings()
    {
        PlayButtonSound();
        
        if (tutorialSettingsPanel != null)
            tutorialSettingsPanel.SetActive(false);
    }

    /// <summary>
    /// Close settings panel
    /// </summary>
    public void CloseSettings()
    {
        PlayButtonSound();
        
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Play button click sound effect
    /// </summary>
    private void PlayButtonSound()
    {
        if (buttonClickSound != null)
        {
            buttonClickSound.Play();
        }
    }

    /// <summary>
    /// Check if this is a first-time player
    /// </summary>
    public bool IsFirstTimePlayer()
    {
        return PlayerPrefs.GetInt("TutorialCompleted", 0) == 0;
    }

    /// <summary>
    /// Get tutorial enabled setting
    /// </summary>
    public bool IsTutorialEnabledForNewPlayers()
    {
        return PlayerPrefs.GetInt("TutorialEnabledForNewPlayers", 1) == 1;
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
        
        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(OnTutorialButtonClicked);
        
        if (inventoryButton != null)
            inventoryButton.onClick.RemoveListener(OnInventoryButtonClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);

        if (replayTutorialButton != null)
            replayTutorialButton.onClick.RemoveListener(OnReplayTutorialClicked);

        if (tutorialEnabledToggle != null)
            tutorialEnabledToggle.onValueChanged.RemoveListener(OnTutorialEnabledChanged);
    }
} 