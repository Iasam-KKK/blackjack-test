using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Manages player health across all scenes in the game
/// Singleton that persists throughout the game session
/// </summary>
public class PlayerHealthManager : MonoBehaviour
{
    public static PlayerHealthManager Instance { get; private set; }
    
    [Header("Health Settings")]
    [Range(0f, 100f)]
    public float currentHealthPercentage = 100f;
    public float maxHealthPercentage = 100f;
    
    [Header("Damage Settings")]
    public float damagePerLoss = 10f; // How much health player loses per lost hand
    
    [Header("UI References (Optional)")]
    public Image healthBarImage; // Fill image for health bar
    public Text healthText; // Text to show health percentage
    
    [Header("Events")]
    public Action<float> OnHealthChanged; // Passes new health percentage
    public Action OnGameOver;
    
    [Header("Persistence")]
    private const string HEALTH_SAVE_KEY = "PlayerHealth";
    private const string MAX_HEALTH_SAVE_KEY = "PlayerMaxHealth";
    
    private void Awake()
    {
        // Singleton pattern - keep one instance across all scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlayerHealthManager] Instance created and set to DontDestroyOnLoad");
            LoadHealth();
        }
        else
        {
            Debug.LogWarning($"[PlayerHealthManager] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        // Subscribe to scene changes to update UI references
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    /// <summary>
    /// Called when a new scene is loaded - find UI references
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerHealthManager] Scene loaded: {scene.name}, finding UI references");
        FindUIReferences();
        UpdateHealthUI();
    }
    
    /// <summary>
    /// Find health bar UI in current scene
    /// </summary>
    private void FindUIReferences()
    {
        // Try to find health bar by tag or name
        GameObject healthBarObj = GameObject.FindGameObjectWithTag("PlayerHealthBar");
        if (healthBarObj == null)
        {
            healthBarObj = GameObject.Find("PlayerHealthBar");
        }
        
        if (healthBarObj != null)
        {
            healthBarImage = healthBarObj.GetComponent<Image>();
            if (healthBarImage != null)
            {
                Debug.Log("[PlayerHealthManager] Found player health bar UI");
            }
        }
        
        // Try to find health text - check multiple ways
        GameObject healthTextObj = GameObject.FindGameObjectWithTag("PlayerHealthText");
        if (healthTextObj == null)
        {
            healthTextObj = GameObject.Find("PlayerHealthText");
        }
        
        // FALLBACK: Look for Text component inside PlayerHealthPanel
        if (healthTextObj == null)
        {
            GameObject panelObj = GameObject.Find("PlayerHealthPanel");
            if (panelObj != null)
            {
                healthText = panelObj.GetComponentInChildren<Text>();
                if (healthText != null)
                {
                    Debug.Log("[PlayerHealthManager] Found player health text inside PlayerHealthPanel");
                }
            }
        }
        else
        {
            healthText = healthTextObj.GetComponent<Text>();
            if (healthText != null)
            {
                Debug.Log("[PlayerHealthManager] Found player health text UI");
            }
        }
        
        // Additional fallback: search for any Text component near health bar
        if (healthText == null && healthBarObj != null)
        {
            healthText = healthBarObj.GetComponentInChildren<Text>();
            if (healthText != null)
            {
                Debug.Log("[PlayerHealthManager] Found player health text as child of health bar");
            }
        }
    }
    
    /// <summary>
    /// Set UI references manually (call from UI setup scripts)
    /// </summary>
    public void SetHealthUI(Image healthBar, Text healthTextComponent = null)
    {
        healthBarImage = healthBar;
        healthText = healthTextComponent;
        UpdateHealthUI();
        Debug.Log("[PlayerHealthManager] Health UI manually assigned");
    }
    
    /// <summary>
    /// Take damage (lose health)
    /// </summary>
    public void TakeDamage(float damage)
    {
        float previousHealth = currentHealthPercentage;
        currentHealthPercentage = Mathf.Max(0f, currentHealthPercentage - damage);
        
        Debug.Log($"[PlayerHealthManager] Player takes {damage} damage. Health: {previousHealth}% -> {currentHealthPercentage}%");
        
        SaveHealth();
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealthPercentage);
        
        if (currentHealthPercentage <= 0f && previousHealth > 0f)
        {
            TriggerGameOver();
        }
    }
    
    /// <summary>
    /// Heal player
    /// </summary>
    public void Heal(float amount)
    {
        float previousHealth = currentHealthPercentage;
        currentHealthPercentage = Mathf.Min(maxHealthPercentage, currentHealthPercentage + amount);
        
        Debug.Log($"[PlayerHealthManager] Player healed {amount}. Health: {previousHealth}% -> {currentHealthPercentage}%");
        
        SaveHealth();
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealthPercentage);
    }
    
    /// <summary>
    /// Restore to full health
    /// </summary>
    public void RestoreToFull()
    {
        currentHealthPercentage = maxHealthPercentage;
        Debug.Log("[PlayerHealthManager] Health restored to full");
        SaveHealth();
        UpdateHealthUI();
        OnHealthChanged?.Invoke(currentHealthPercentage);
    }
    
    /// <summary>
    /// Get current health percentage
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealthPercentage;
    }
    
    /// <summary>
    /// Check if player is alive
    /// </summary>
    public bool IsAlive()
    {
        return currentHealthPercentage > 0f;
    }
    
    /// <summary>
    /// Update health UI display
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthBarImage != null)
        {
            healthBarImage.fillAmount = currentHealthPercentage / maxHealthPercentage;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.RoundToInt(currentHealthPercentage)}%";
        }
    }
    
    /// <summary>
    /// Save health to PlayerPrefs
    /// </summary>
    public void SaveHealth()
    {
        PlayerPrefs.SetFloat(HEALTH_SAVE_KEY, currentHealthPercentage);
        PlayerPrefs.SetFloat(MAX_HEALTH_SAVE_KEY, maxHealthPercentage);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Load health from PlayerPrefs
    /// </summary>
    public void LoadHealth()
    {
        currentHealthPercentage = PlayerPrefs.GetFloat(HEALTH_SAVE_KEY, maxHealthPercentage);
        maxHealthPercentage = PlayerPrefs.GetFloat(MAX_HEALTH_SAVE_KEY, 100f);
        
        Debug.Log($"[PlayerHealthManager] Health loaded: {currentHealthPercentage}%");
        UpdateHealthUI();
    }
    
    /// <summary>
    /// Reset health to full and clear save data
    /// </summary>
    public void ResetHealth()
    {
        currentHealthPercentage = maxHealthPercentage;
        SaveHealth();
        UpdateHealthUI();
        Debug.Log("[PlayerHealthManager] Health reset to full");
    }
    
    /// <summary>
    /// Trigger game over
    /// </summary>
    private void TriggerGameOver()
    {
        Debug.Log("[PlayerHealthManager] GAME OVER - Player health depleted!");
        OnGameOver?.Invoke();
        
        // Reset progression
        if (BossProgressionManager.Instance != null)
        {
            BossProgressionManager.Instance.ResetProgression();
        }
        
        // Reset health
        ResetHealth();
        
        // Return to main menu after a delay
        Invoke(nameof(ReturnToMainMenu), 3f);
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    private void ReturnToMainMenu()
    {
        Debug.Log("[PlayerHealthManager] Returning to main menu after game over");
        SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// Manual UI update (call when UI changes externally)
    /// </summary>
    public void RefreshUI()
    {
        UpdateHealthUI();
    }
}

