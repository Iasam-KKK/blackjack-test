using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    // public TextMeshProUGUI titleText;
    public GameObject settingsPanel;

    [Header("Audio")]
    public AudioSource buttonClickSound;

    private void Start()
    {
        // Setup button listeners
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitButtonClicked);

        // Make sure settings panel is closed initially
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Set title text
        // if (titleText != null)
        //     titleText.text = "BLACKJACK";
    }

    /// <summary>
    /// Called when Play button is clicked
    /// </summary>
    public void OnPlayButtonClicked()
    {
        PlayButtonSound();
        
        // Load the game scene
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameScene();
        }
        else
        {
            // Fallback if GameSceneManager instance is not available
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
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

    private void OnDestroy()
    {
        // Clean up button listeners
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitButtonClicked);
    }
} 