using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bridge script to register scene-specific health bar UI with the global HealthBarManager
/// Add this to each scene that has health bar UI elements
/// </summary>
public class SceneHealthBarBridge : MonoBehaviour
{
    [Header("Player Health UI in This Scene")]
    [Tooltip("Drag the player health bar fill Image here")]
    public Image playerHealthBarFill;
    
    [Tooltip("Optional: Drag the player health text component here")]
    public TextMeshProUGUI playerHealthText;
    
    [Tooltip("Optional: For legacy Text component")]
    public Text playerHealthTextLegacy;
    
    [Header("Enemy Health UI in This Scene")]
    [Tooltip("Drag the enemy health bar fill Image here")]
    public Image enemyHealthBarFill;
    
    [Tooltip("Drag the enemy name text component here")]
    public TextMeshProUGUI enemyNameText;
    
    [Tooltip("Optional: For enemy health numbers display")]
    public TextMeshProUGUI enemyHealthText;
    
    [Tooltip("Optional: For legacy Text component")]
    public Text enemyNameTextLegacy;
    
    [Tooltip("Optional: For legacy Text component")]
    public Text enemyHealthTextLegacy;
    
    [Header("Settings")]
    [Tooltip("Automatically register references on Start")]
    public bool autoRegister = true;
    
    private void Start()
    {
        if (autoRegister)
        {
            RegisterHealthBars();
        }
    }
    
    /// <summary>
    /// Register this scene's health bar UI with the global HealthBarManager
    /// </summary>
    public void RegisterHealthBars()
    {
        if (HealthBarManager.Instance == null)
        {
            Debug.LogWarning("[SceneHealthBarBridge] HealthBarManager.Instance is null! Make sure HealthBarManager exists in the scene.");
            return;
        }
        
        // Register player health UI
        if (playerHealthBarFill != null)
        {
            HealthBarManager.Instance.SetPlayerHealthReferences(
                playerHealthBarFill,
                playerHealthText,
                playerHealthTextLegacy
            );
            Debug.Log("[SceneHealthBarBridge] Player health UI registered");
        }
        else
        {
            Debug.LogWarning("[SceneHealthBarBridge] Player health bar fill is not assigned!");
        }
        
        // Register enemy health UI
        if (enemyHealthBarFill != null)
        {
            HealthBarManager.Instance.SetEnemyHealthReferences(
                enemyHealthBarFill,
                enemyNameText,
                enemyHealthText,
                enemyNameTextLegacy,
                enemyHealthTextLegacy
            );
            Debug.Log("[SceneHealthBarBridge] Enemy health UI registered");
        }
        else
        {
            Debug.LogWarning("[SceneHealthBarBridge] Enemy health bar fill is not assigned!");
        }
    }
    
    private void OnDestroy()
    {
        // Optional: Clear references when this scene unloads
        // Uncomment if you want to explicitly clear references
        /*
        if (HealthBarManager.Instance != null)
        {
            HealthBarManager.Instance.ClearReferences();
        }
        */
    }
}

