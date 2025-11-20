using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

/// <summary>
/// Centralized health bar UI manager
/// Manages player and enemy health bar display across all scenes
/// Reads from GameProgressionManager as single source of truth
/// </summary>
public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance { get; private set; }
    
    [Header("Player Health UI References")]
    [Tooltip("Player health bar fill image (Image Type: Filled)")]
    public Image playerHealthBarFill;
    
    [Tooltip("Optional: Text component to display player health percentage")]
    public TextMeshProUGUI playerHealthText;
    
    [Tooltip("Optional: Legacy Text component (if not using TextMeshPro)")]
    public Text playerHealthTextLegacy;
    
    [Header("Enemy Health UI References")]
    [Tooltip("Enemy health bar fill image (Image Type: Filled)")]
    public Image enemyHealthBarFill;
    
    [Tooltip("Text component to display enemy/boss/minion name")]
    public TextMeshProUGUI enemyNameText;
    
    [Tooltip("Optional: Legacy Text component (if not using TextMeshPro)")]
    public Text enemyNameTextLegacy;
    
    [Tooltip("Optional: Text component to display enemy health numbers")]
    public TextMeshProUGUI enemyHealthText;
    
    [Tooltip("Optional: Legacy Text component (if not using TextMeshPro)")]
    public Text enemyHealthTextLegacy;
    
    [Header("Debug Info")]
    [Tooltip("Enable detailed console logging for debugging")]
    public bool showDebugLogs = true;
    
    private void Awake()
    {
        // Singleton pattern - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Log("[HealthBarManager] Instance created and set to DontDestroyOnLoad");
        }
        else
        {
            Log($"[HealthBarManager] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        // Subscribe to scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void Start()
    {
        // Subscribe to GameProgressionManager events
        SubscribeToEvents();
        
        // Initial update
        UpdateAllHealthBars();
        
        // Validate references
        ValidateReferences();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromEvents();
        }
    }
    
    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"[HealthBarManager] Scene loaded: {scene.name}, updating health bars");
        // Note: UI references should be reassigned in Inspector for each scene
        // or use a scene-specific UI manager that registers with this global manager
        UpdateAllHealthBars();
    }
    
    /// <summary>
    /// Subscribe to GameProgressionManager events for real-time updates
    /// </summary>
    private void SubscribeToEvents()
    {
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.OnPlayerHealthChanged += OnPlayerHealthChanged;
            GameProgressionManager.Instance.OnEncounterHealthChanged += OnEnemyHealthChanged;
            Log("[HealthBarManager] Subscribed to GameProgressionManager events");
        }
        else
        {
            LogWarning("[HealthBarManager] GameProgressionManager.Instance is null, cannot subscribe to events");
        }
    }
    
    /// <summary>
    /// Unsubscribe from GameProgressionManager events
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.OnPlayerHealthChanged -= OnPlayerHealthChanged;
            GameProgressionManager.Instance.OnEncounterHealthChanged -= OnEnemyHealthChanged;
        }
    }
    
    /// <summary>
    /// Validate that required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (playerHealthBarFill == null)
        {
            LogWarning("[HealthBarManager] Player health bar fill image is not assigned!");
        }
        
        if (enemyHealthBarFill == null)
        {
            LogWarning("[HealthBarManager] Enemy health bar fill image is not assigned!");
        }
        
        if (enemyNameText == null && enemyNameTextLegacy == null)
        {
            LogWarning("[HealthBarManager] Enemy name text component is not assigned!");
        }
        
        Log("[HealthBarManager] Reference validation complete");
    }
    
    /// <summary>
    /// Manually set player health bar references (for runtime setup or scene switching)
    /// </summary>
    public void SetPlayerHealthReferences(Image healthBarFill, TextMeshProUGUI healthText = null, Text healthTextLegacy = null)
    {
        playerHealthBarFill = healthBarFill;
        playerHealthText = healthText;
        playerHealthTextLegacy = healthTextLegacy;
        
        Log("[HealthBarManager] Player health references updated");
        UpdatePlayerHealthBar();
    }
    
    /// <summary>
    /// Manually set enemy health bar references (for runtime setup or scene switching)
    /// </summary>
    public void SetEnemyHealthReferences(Image healthBarFill, TextMeshProUGUI nameText = null, TextMeshProUGUI healthText = null, Text nameTextLegacy = null, Text healthTextLegacy = null)
    {
        enemyHealthBarFill = healthBarFill;
        enemyNameText = nameText;
        enemyHealthText = healthText;
        enemyNameTextLegacy = nameTextLegacy;
        enemyHealthTextLegacy = healthTextLegacy;
        
        Log("[HealthBarManager] Enemy health references updated");
        UpdateEnemyHealthBar();
        UpdateEnemyName();
    }
    
    /// <summary>
    /// Update all health bars (call this manually if needed)
    /// </summary>
    public void UpdateAllHealthBars()
    {
        UpdatePlayerHealthBar();
        UpdateEnemyHealthBar();
        UpdateEnemyName();
    }
    
    /// <summary>
    /// Update player health bar display
    /// </summary>
    private void UpdatePlayerHealthBar()
    {
        if (GameProgressionManager.Instance == null)
        {
            LogWarning("[HealthBarManager] GameProgressionManager.Instance is null, cannot update player health");
            return;
        }
        
        float playerHealth = GameProgressionManager.Instance.playerHealthPercentage;
        float maxHealth = GameProgressionManager.Instance.maxHealthPercentage;
        float fillAmount = playerHealth / maxHealth;
        
        // Update health bar fill
        if (playerHealthBarFill != null)
        {
            playerHealthBarFill.fillAmount = fillAmount;
            Log($"[HealthBarManager] Updated player health bar: {playerHealth:F0}% (fillAmount: {fillAmount:F2})");
        }
        
        // Update health text
        string healthString = $"{playerHealth:F0}%";
        if (playerHealthText != null)
        {
            playerHealthText.text = healthString;
        }
        else if (playerHealthTextLegacy != null)
        {
            playerHealthTextLegacy.text = healthString;
        }
    }
    
    /// <summary>
    /// Update enemy health bar display
    /// </summary>
    private void UpdateEnemyHealthBar()
    {
        if (GameProgressionManager.Instance == null)
        {
            LogWarning("[HealthBarManager] GameProgressionManager.Instance is null, cannot update enemy health");
            return;
        }
        
        if (!GameProgressionManager.Instance.isEncounterActive)
        {
            // No active encounter, hide or reset enemy health bar
            if (enemyHealthBarFill != null)
            {
                enemyHealthBarFill.fillAmount = 1f; // Show full bar when no encounter
            }
            return;
        }
        
        int currentHealth = GameProgressionManager.Instance.currentEncounterHealth;
        int maxHealth = 0;
        
        // Get max health from current boss or minion
        if (GameProgressionManager.Instance.isMinion && GameProgressionManager.Instance.currentMinion != null)
        {
            maxHealth = GameProgressionManager.Instance.currentMinion.maxHealth;
        }
        else if (!GameProgressionManager.Instance.isMinion && GameProgressionManager.Instance.currentBoss != null)
        {
            maxHealth = GameProgressionManager.Instance.currentBoss.maxHealth;
        }
        
        if (maxHealth == 0)
        {
            LogWarning("[HealthBarManager] Max health is 0, cannot calculate fill amount");
            return;
        }
        
        float fillAmount = (float)currentHealth / maxHealth;
        
        // Update health bar fill
        if (enemyHealthBarFill != null)
        {
            enemyHealthBarFill.fillAmount = fillAmount;
            Log($"[HealthBarManager] Updated enemy health bar: {currentHealth}/{maxHealth} (fillAmount: {fillAmount:F2})");
        }
        
        // Update health text
        string healthString = $"{currentHealth}/{maxHealth}";
        if (enemyHealthText != null)
        {
            enemyHealthText.text = healthString;
        }
        else if (enemyHealthTextLegacy != null)
        {
            enemyHealthTextLegacy.text = healthString;
        }
    }
    
    /// <summary>
    /// Update enemy name display
    /// </summary>
    private void UpdateEnemyName()
    {
        if (GameProgressionManager.Instance == null)
        {
            LogWarning("[HealthBarManager] GameProgressionManager.Instance is null, cannot update enemy name");
            return;
        }
        
        string enemyName = "Unknown";
        
        if (GameProgressionManager.Instance.isEncounterActive)
        {
            if (GameProgressionManager.Instance.isMinion && GameProgressionManager.Instance.currentMinion != null)
            {
                enemyName = GameProgressionManager.Instance.currentMinion.minionName;
            }
            else if (!GameProgressionManager.Instance.isMinion && GameProgressionManager.Instance.currentBoss != null)
            {
                enemyName = GameProgressionManager.Instance.currentBoss.bossName;
            }
        }
        else
        {
            enemyName = "No Encounter";
        }
        
        // Update enemy name text
        if (enemyNameText != null)
        {
            enemyNameText.text = enemyName;
            Log($"[HealthBarManager] Updated enemy name: {enemyName}");
        }
        else if (enemyNameTextLegacy != null)
        {
            enemyNameTextLegacy.text = enemyName;
            Log($"[HealthBarManager] Updated enemy name (Legacy): {enemyName}");
        }
    }
    
    /// <summary>
    /// Event handler for player health changes
    /// </summary>
    private void OnPlayerHealthChanged(float newHealth)
    {
        Log($"[HealthBarManager] Player health changed event: {newHealth:F0}%");
        UpdatePlayerHealthBar();
    }
    
    /// <summary>
    /// Event handler for enemy health changes
    /// </summary>
    private void OnEnemyHealthChanged(int newHealth)
    {
        Log($"[HealthBarManager] Enemy health changed event: {newHealth}");
        UpdateEnemyHealthBar();
    }
    
    /// <summary>
    /// Clear all UI references (useful when transitioning between scenes)
    /// </summary>
    public void ClearReferences()
    {
        playerHealthBarFill = null;
        playerHealthText = null;
        playerHealthTextLegacy = null;
        enemyHealthBarFill = null;
        enemyNameText = null;
        enemyNameTextLegacy = null;
        enemyHealthText = null;
        enemyHealthTextLegacy = null;
        
        Log("[HealthBarManager] All references cleared");
    }
    
    // Debug logging helpers
    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    private void LogWarning(string message)
    {
        if (showDebugLogs)
        {
            Debug.LogWarning(message);
        }
    }
}

