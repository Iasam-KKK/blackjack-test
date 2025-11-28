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
    public Button optionsButton;       // Options button to open options panel
    public Button quitButton;
    // public TextMeshProUGUI titleText;
    public GameObject settingsPanel;

    [Header("Options Panel")]
    public GameObject optionsPanel;    // Options panel GameObject
    public Button optionsCloseButton;  // Close button inside options panel
    public Button resetProgressButton; // Reset progress button inside options panel

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
        
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        // Setup tutorial settings
        if (replayTutorialButton != null)
            replayTutorialButton.onClick.AddListener(OnReplayTutorialClicked);
        
        if (tutorialSettingsCloseButton != null)
            tutorialSettingsCloseButton.onClick.AddListener(CloseTutorialSettings);

        // Setup options panel
        if (optionsCloseButton != null)
            optionsCloseButton.onClick.AddListener(CloseOptionsPanel);
        
        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(OnResetProgressClicked);

        // Make sure panels are closed initially
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (tutorialSettingsPanel != null)
            tutorialSettingsPanel.SetActive(false);
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

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
    /// Called when Options button is clicked
    /// </summary>
    public void OnOptionsButtonClicked()
    {
        PlayButtonSound();
        
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
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
    /// Close options panel
    /// </summary>
    public void CloseOptionsPanel()
    {
        PlayButtonSound();
        
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    /// <summary>
    /// Called when Reset Progress button is clicked
    /// </summary>
    public void OnResetProgressClicked()
    {
        PlayButtonSound();
        
        // Clear all PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        // Clear runtime inventory data
        ClearInventoryData();
        
        Debug.Log("All progress and PlayerPrefs have been reset.");
        
        // Close options panel after reset
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    /// <summary>
    /// Clear all inventory data from runtime ScriptableObject
    /// </summary>
    private void ClearInventoryData()
    {
        // Use InventoryManagerV3 if available
        if (InventoryManagerV3.Instance != null && InventoryManagerV3.Instance.inventoryData != null)
        {
            var inventoryData = InventoryManagerV3.Instance.inventoryData;
            
            // Clear all storage slots
            foreach (var slot in inventoryData.storageSlots)
            {
                if (slot != null && slot.isOccupied)
                {
                    slot.RemoveCard();
                }
            }
            
            // Clear all equipment slots
            foreach (var slot in inventoryData.equipmentSlots)
            {
                if (slot != null && slot.isOccupied)
                {
                    slot.RemoveCard();
                }
            }
            
            // Force refresh inventory UI if it's open
            if (InventoryManagerV3.Instance.inventoryPanelV3 != null)
            {
                InventoryManagerV3.Instance.inventoryPanelV3.RefreshAllSlots();
            }
            
            Debug.Log("Inventory data cleared from runtime.");
        }
        // Fallback to InventoryManager if V3 is not available
        else if (InventoryManager.Instance != null && InventoryManager.Instance.inventoryData != null)
        {
            var inventoryData = InventoryManager.Instance.inventoryData;
            
            // Clear all storage slots
            foreach (var slot in inventoryData.storageSlots)
            {
                if (slot != null && slot.isOccupied)
                {
                    slot.RemoveCard();
                }
            }
            
            // Clear all equipment slots
            foreach (var slot in inventoryData.equipmentSlots)
            {
                if (slot != null && slot.isOccupied)
                {
                    slot.RemoveCard();
                }
            }
            
            Debug.Log("Inventory data cleared from runtime (using InventoryManager).");
        }
        
        // Clear PlayerStats ownedCards
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.ownedCards.Clear();
            Debug.Log("PlayerStats ownedCards cleared.");
        }
        
        // Reset game progression if available
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.ResetProgression();
            Debug.Log("Game progression reset.");
        }
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
        
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OnOptionsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);

        if (replayTutorialButton != null)
            replayTutorialButton.onClick.RemoveListener(OnReplayTutorialClicked);

        if (tutorialEnabledToggle != null)
            tutorialEnabledToggle.onValueChanged.RemoveListener(OnTutorialEnabledChanged);

        if (optionsCloseButton != null)
            optionsCloseButton.onClick.RemoveListener(CloseOptionsPanel);

        if (resetProgressButton != null)
            resetProgressButton.onClick.RemoveListener(OnResetProgressClicked);
    }
} 