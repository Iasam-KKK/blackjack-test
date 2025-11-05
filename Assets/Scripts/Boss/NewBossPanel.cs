using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class NewBossPanel : MonoBehaviour
{
    [Header("Boss Display")]
    public Image bossPortrait;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossDescriptionText;
    public Image bossHealthBar; // Changed from Slider to Image
    public TextMeshProUGUI healthText;
    // Hand tracking removed - using player health as primary game state
    
    [Header("Player Health Display")]
    public TextMeshProUGUI playerHealthText;
    public Image playerHealthBar;
    
    [Header("Testing")]
    public UnityEngine.UI.Button testPlayerWinButton;
    
    [Header("Boss Messages")]
    public TextMeshProUGUI bossMessageText; // For showing boss mechanic messages
    public GameObject bossMessagePanel; // Panel to contain the message
    
    [Header("Boss Effects")]
    public Image bossBackground;
    public ParticleSystem bossParticles;
    public AudioSource bossAudioSource;
    
    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float healthBarAnimationDuration = 0.3f;
    public float shakeIntensity = 10f;
    public float shakeDuration = 0.5f;
    
    private BossManager bossManager;
    private GameProgressionManager gameProgressionManager;
    private bool isVisible = false;
    private BossData cachedBoss;
    
    private void Start()
    {
        // Find BossManager
        bossManager = FindObjectOfType<BossManager>();
        
        // Find GameProgressionManager
        gameProgressionManager = GameProgressionManager.Instance;
        if (gameProgressionManager == null)
        {
            gameProgressionManager = FindObjectOfType<GameProgressionManager>();
        }
        
        // Setup test player win button
        if (testPlayerWinButton != null)
        {
            testPlayerWinButton.onClick.AddListener(TestPlayerWin);
            Debug.Log("[NewBossPanel] Test Player Win button configured");
        }
        
        // Set up initial state
        if (bossBackground != null)
        {
            Color color = bossBackground.color;
            color.a = 0f;
            bossBackground.color = color;
        }
        
        // Don't hide initially - let the ShowBossPanel method control visibility
         gameObject.SetActive(true); // Removed this line
         
        // Initial update
        UpdateBossDisplay();
    }
    
    private void OnEnable()
    {
        // Update when panel is enabled (e.g., when returning to BossMap)
        UpdateBossDisplay();
    }
    
    private void Update()
    {
        // Update display when boss changes or when in any scene
        BossData currentBoss = GetCurrentDisplayBoss();
        if (currentBoss != null && currentBoss != cachedBoss)
        {
            cachedBoss = currentBoss;
            UpdateBossDisplay();
        }
        
        // Also update during battle to reflect health changes
        if (bossManager != null && bossManager.IsBossActive())
        {
            UpdateBossDisplay();
        }
        
        // Update player health display
        UpdatePlayerHealth();
    }
    
    /// <summary>
    /// Update player health display
    /// </summary>
    private void UpdatePlayerHealth()
    {
        if (gameProgressionManager == null) return;
        
        float playerHealth = gameProgressionManager.GetPlayerHealth();
        
        // Update player health text
        if (playerHealthText != null)
        {
            playerHealthText.text = $"{playerHealth:F0}%";
        }
        
        // Update player health bar
        if (playerHealthBar != null)
        {
            playerHealthBar.fillAmount = playerHealth / 100f;
        }
    }
    
    /// <summary>
    /// Test function to force player win and reduce encounter health
    /// </summary>
    public void TestPlayerWin()
    {
        if (gameProgressionManager == null)
        {
            Debug.LogWarning("[NewBossPanel] GameProgressionManager not found for test player win");
            return;
        }
        
        if (!gameProgressionManager.isEncounterActive)
        {
            Debug.LogWarning("[NewBossPanel] No active encounter to test win against");
            return;
        }
        
        Debug.Log("[NewBossPanel] Test Player Win triggered - forcing encounter health reduction");
        
        // Force player win by reducing encounter health
        gameProgressionManager.OnPlayerWinRound();
        
        // Update UI to reflect the change
        UpdateBossDisplay();
        UpdatePlayerHealth();
    }
    
    /// <summary>
    /// Manually trigger boss panel display (for testing)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestShowBossPanel()
    {
        Debug.Log("TestShowBossPanel called - manually triggering boss panel display");
        ShowBossPanel();
    }
    
    /// <summary>
    /// Manually update the health bar display
    /// </summary>
    public void UpdateHealthBar()
    {
        Debug.Log("UpdateHealthBar called");
        UpdateBossDisplay();
    }
    
    /// <summary>
    /// Show the boss panel with animation
    /// </summary>
    public void ShowBossPanel()
    {
        Debug.Log("ShowBossPanel called");
        
        bool wasVisible = isVisible;
        
        if (wasVisible) 
        {
            Debug.Log("Boss panel already visible, updating boss info");
        }
        
        gameObject.SetActive(true);
        isVisible = true;
        
        BossData currentBoss = bossManager?.GetCurrentBoss();
        Debug.Log($"Current boss: {(currentBoss != null ? currentBoss.bossName : "null")}");
        
        if (currentBoss != null)
        {
            // Set boss data
            if (bossPortrait != null && currentBoss.bossPortrait != null)
            {
                bossPortrait.sprite = currentBoss.bossPortrait;
                Debug.Log("Set boss portrait");
            }
            else
            {
                Debug.LogWarning("Boss portrait or portrait sprite is null");
            }
            
            if (bossNameText != null)
            {
                bossNameText.text = currentBoss.bossName;
                Debug.Log($"Set boss name: {currentBoss.bossName}");
            }
            else
            {
                Debug.LogWarning("Boss name text component is null");
            }
            
            if (bossDescriptionText != null)
            {
                bossDescriptionText.text = currentBoss.bossDescription;
                Debug.Log($"Set boss description: {currentBoss.bossDescription}");
            }
            else
            {
                Debug.LogWarning("Boss description text component is null");
            }
        }
        else
        {
            Debug.LogError("Current boss is null! BossManager might not be initialized properly.");
        }
        
        // Only animate if the panel wasn't already visible
        if (!wasVisible)
        {
            // Animate background fade in
            if (bossBackground != null)
            {
                bossBackground.DOFade(0.3f, fadeInDuration);
            }
            
            // Animate UI elements
            AnimateUIElementsIn();
        }
        
        // Update the display immediately to show current health
        UpdateBossDisplay();
        
        // Play boss music if available
        if (bossAudioSource != null && currentBoss?.bossMusic != null)
        {
            bossAudioSource.clip = currentBoss.bossMusic;
            bossAudioSource.Play();
        }
        
        // Start particle effects
        if (bossParticles != null)
        {
            bossParticles.Play();
        }
    }
    
    /// <summary>
    /// Hide the boss panel with animation
    /// </summary>
    public void HideBossPanel()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        // Animate background fade out
        if (bossBackground != null)
        {
            bossBackground.DOFade(0f, fadeInDuration).OnComplete(() => {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
        
        // Stop audio
        if (bossAudioSource != null)
        {
            bossAudioSource.Stop();
        }
        
        // Stop particles
        if (bossParticles != null)
        {
            bossParticles.Stop();
        }
    }
    
    /// <summary>
    /// Get the boss that should be displayed based on current game state
    /// </summary>
    private BossData GetCurrentDisplayBoss()
    {
        // Priority 1: If there's an active boss battle, show that
        if (bossManager != null && bossManager.IsBossActive())
        {
            return bossManager.GetCurrentBoss();
        }
        
        // Priority 2: If we're in BossMap, show the selected or next available boss
        if (GameProgressionManager.Instance != null)
        {
            BossType? selectedBoss = GameProgressionManager.Instance.GetSelectedBoss();
            if (selectedBoss.HasValue)
            {
                return GameProgressionManager.Instance.GetBossData(selectedBoss.Value);
            }
            
            // Fallback: show first available (unlocked but not defeated) boss
            var availableBosses = GameProgressionManager.Instance.GetAvailableBosses();
            if (availableBosses.Count > 0)
            {
                return availableBosses[0];
            }
        }
        
        // Fallback: show current boss from BossManager if available
        if (bossManager != null)
        {
            return bossManager.GetCurrentBoss();
        }
        
        return null;
    }
    
    /// <summary>
    /// Update the boss display with current information
    /// </summary>
    private void UpdateBossDisplay()
    {
        // Use GameProgressionManager as single source of truth
        if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.isEncounterActive)
        {
            if (GameProgressionManager.Instance.isMinion)
            {
                UpdateMinionDisplay();
                return;
            }
        }
        
        BossData currentBoss = GetCurrentDisplayBoss();
        if (currentBoss == null)
        {
            Debug.LogWarning("[NewBossPanel] No boss to display");
            return;
        }
        
        // Update portrait
        if (bossPortrait != null && currentBoss.bossPortrait != null)
        {
            bossPortrait.sprite = currentBoss.bossPortrait;
        }
        
        // Update name and description
        if (bossNameText != null)
        {
            bossNameText.text = currentBoss.bossName;
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.text = currentBoss.bossDescription;
        }
        
        // If boss is active in battle, show current battle stats
        bool isInBattle = (bossManager != null && bossManager.IsBossActive() && bossManager.GetCurrentBoss() == currentBoss);
        
        int currentHealth;
        int maxHealth = currentBoss.maxHealth;
        
        if (isInBattle)
        {
            currentHealth = bossManager.GetCurrentBossHealth();
        }
        else
        {
            // Not in battle - show default stats
            currentHealth = maxHealth;
        }
        
        // Update health bar with smooth animation
        if (bossHealthBar != null)
        {
            // Calculate health percentage (full bar = full health, empty bar = no health)
            float targetValue = (float)currentHealth / maxHealth;
            bossHealthBar.DOFillAmount(targetValue, healthBarAnimationDuration).SetEase(Ease.OutQuad);
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
        
        // Hand tracking removed - using player health as primary game state
    }
    
    /// <summary>
    /// Update display for minion encounters
    /// </summary>
    private void UpdateMinionDisplay()
    {
        if (GameProgressionManager.Instance == null || !GameProgressionManager.Instance.isEncounterActive || !GameProgressionManager.Instance.isMinion)
        {
            Debug.LogWarning("[NewBossPanel] No active minion encounter");
            return;
        }
        
        var minion = GameProgressionManager.Instance.currentMinion;
        if (minion == null)
        {
            Debug.LogWarning("[NewBossPanel] Minion data is null");
            return;
        }
        
        Debug.Log($"[NewBossPanel] Updating display for minion: {minion.minionName}");
        
        // Update portrait with minion portrait
        if (bossPortrait != null)
        {
            if (minion.minionPortrait != null)
            {
                bossPortrait.sprite = minion.minionPortrait;
                bossPortrait.enabled = true;
                Debug.Log($"[NewBossPanel] Set minion portrait: {minion.minionPortrait.name}");
            }
            else
            {
                Debug.LogError($"[NewBossPanel] CRITICAL: Minion '{minion.minionName}' has NO portrait assigned in ScriptableObject!");
                Debug.LogError($"[NewBossPanel] Please assign a sprite to the 'minionPortrait' field in the MinionData asset");
                // Keep current sprite as fallback, but make it obvious
                bossPortrait.color = Color.red;
            }
        }
        else
        {
            Debug.LogError("[NewBossPanel] CRITICAL: bossPortrait UI Image component is not assigned in Inspector!");
        }
        
        // Update name and description
        if (bossNameText != null)
        {
            bossNameText.text = minion.minionName;
            Debug.Log($"[NewBossPanel] Set minion name: {minion.minionName}");
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.text = minion.minionDescription;
        }
        
        // Get current stats from GameProgressionManager (SINGLE SOURCE OF TRUTH)
        int currentHealth = GameProgressionManager.Instance.currentEncounterHealth;
        int maxHealth = minion.maxHealth;
        
        Debug.Log($"[NewBossPanel] Minion stats - Health: {currentHealth}/{maxHealth}");
        
        // Update health bar
        if (bossHealthBar != null)
        {
            float targetValue = (float)currentHealth / maxHealth;
            Debug.Log($"[NewBossPanel] Setting health bar fill amount to: {targetValue} ({currentHealth}/{maxHealth})");
            bossHealthBar.DOFillAmount(targetValue, healthBarAnimationDuration).SetEase(Ease.OutQuad);
        }
        else
        {
            Debug.LogWarning("[NewBossPanel] bossHealthBar is null!");
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
            Debug.Log($"[NewBossPanel] Set health text: {currentHealth}/{maxHealth}");
        }
        
        // Hand tracking removed - using player health as primary game state
    }
    
    /// <summary>
    /// Animate UI elements coming in
    /// </summary>
    private void AnimateUIElementsIn()
    {
        // Animate boss portrait
        if (bossPortrait != null)
        {
            bossPortrait.transform.localScale = Vector3.zero;
            bossPortrait.transform.DOScale(Vector3.one, fadeInDuration).SetEase(Ease.OutBack);
        }
        
        // Animate boss name
        if (bossNameText != null)
        {
            bossNameText.transform.localPosition += Vector3.up * 50f;
            bossNameText.DOFade(0f, 0f);
            bossNameText.transform.DOLocalMove(bossNameText.transform.localPosition - Vector3.up * 50f, fadeInDuration).SetEase(Ease.OutQuad);
            bossNameText.DOFade(1f, fadeInDuration);
        }
        
        // Animate health bar
        if (bossHealthBar != null)
        {
            // Get current boss health and set initial value
            BossData currentBoss = bossManager?.GetCurrentBoss();
            if (currentBoss != null)
            {
                int currentHealth = bossManager.GetCurrentBossHealth();
                int maxHealth = currentBoss.maxHealth;
                float initialValue = (float)currentHealth / maxHealth;
                
                // Start from 0 and animate to current health
                bossHealthBar.fillAmount = 0f;
                bossHealthBar.DOFillAmount(initialValue, healthBarAnimationDuration).SetDelay(fadeInDuration * 0.5f);
                Debug.Log($"Initializing health bar: {currentHealth}/{maxHealth} = {initialValue}");
            }
        }
    }
    
    /// <summary>
    /// Shake the boss panel when boss takes damage
    /// </summary>
    public void ShakePanel()
    {
        transform.DOShakePosition(shakeDuration, shakeIntensity, 10, 90, false, true);
        
        // Flash the health bar red
        if (bossHealthBar != null)
        {
            Color originalColor = bossHealthBar.color;
            bossHealthBar.DOColor(Color.red, 0.1f).OnComplete(() => {
                bossHealthBar.DOColor(originalColor, 0.1f);
            });
        }
    }
    
    /// <summary>
    /// Show boss defeat effect
    /// </summary>
    public void ShowDefeatEffect()
    {
        // Flash the entire UI
        if (bossBackground != null)
        {
            bossBackground.DOColor(Color.white, 0.1f).OnComplete(() => {
                bossBackground.DOColor(new Color(0.3f, 0.3f, 0.3f, 0.3f), 0.3f);
            });
        }
        
        // Shake more violently
        transform.DOShakePosition(1f, shakeIntensity * 2, 20, 90, false, true);
        
        // Play defeat sound
        if (bossAudioSource != null)
        {
            // You can add a defeat sound here
        }
    }
    
    /// <summary>
    /// Show boss heal effect
    /// </summary>
    public void ShowHealEffect()
    {
        // Flash green
        if (bossBackground != null)
        {
            bossBackground.DOColor(Color.green, 0.1f).OnComplete(() => {
                bossBackground.DOColor(new Color(0.3f, 0.3f, 0.3f, 0.3f), 0.3f);
            });
        }
        
        // Gentle shake
        transform.DOShakePosition(0.5f, shakeIntensity * 0.5f, 5, 90, false, true);
    }
    
    /// <summary>
    /// Show next boss introduction with special effects
    /// </summary>
    public void ShowNextBossIntroduction(BossData nextBoss)
    {
        Debug.Log($"ShowNextBossIntroduction called for: {nextBoss.bossName}");
        
        // Reset visibility state
        isVisible = false;
        
        // Show the panel
        gameObject.SetActive(true);
        isVisible = true;
        
        // Set next boss data
        if (bossPortrait != null && nextBoss.bossPortrait != null)
        {
            bossPortrait.sprite = nextBoss.bossPortrait;
        }
        
        if (bossNameText != null)
        {
            bossNameText.text = $"NEXT: {nextBoss.bossName}";
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.text = nextBoss.bossDescription;
        }
        
        // Reset health bar for next boss
        if (bossHealthBar != null)
        {
            bossHealthBar.fillAmount = 1f; // Full health for next boss
        }
        
        if (healthText != null)
        {
            healthText.text = $"{nextBoss.maxHealth}/{nextBoss.maxHealth}";
        }
        
        // Hand tracking removed - using player health as primary game state
        
        // Animate with special effects for next boss introduction
        AnimateNextBossIntroduction();
    }
    
    /// <summary>
    /// Animate the next boss introduction with dramatic effects
    /// </summary>
    private void AnimateNextBossIntroduction()
    {
        // Start with everything invisible
        if (bossPortrait != null)
        {
            bossPortrait.transform.localScale = Vector3.zero;
        }
        
        if (bossNameText != null)
        {
            bossNameText.alpha = 0f;
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.alpha = 0f;
        }
        
        // Animate background with dramatic effect
        if (bossBackground != null)
        {
            bossBackground.color = Color.black;
            bossBackground.DOColor(new Color(0.3f, 0.3f, 0.3f, 0.3f), fadeInDuration * 2f);
        }
        
        // Animate boss portrait with dramatic entrance
        if (bossPortrait != null)
        {
            bossPortrait.transform.DOScale(Vector3.one * 1.2f, fadeInDuration * 1.5f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    bossPortrait.transform.DOScale(Vector3.one, fadeInDuration * 0.5f);
                });
        }
        
        // Animate text elements with delay
        if (bossNameText != null)
        {
            bossNameText.DOFade(1f, fadeInDuration).SetDelay(fadeInDuration * 0.5f);
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.DOFade(1f, fadeInDuration).SetDelay(fadeInDuration * 1f);
        }
        
        // Animate health bar
        if (bossHealthBar != null)
        {
            bossHealthBar.fillAmount = 0f;
            bossHealthBar.DOFillAmount(1f, healthBarAnimationDuration).SetDelay(fadeInDuration * 1.5f);
        }
        
        // Play dramatic particle effects
        if (bossParticles != null)
        {
            bossParticles.Play();
        }
    }
    
    /// <summary>
    /// Show a boss message (for mechanic feedback)
    /// </summary>
    public void ShowBossMessage(string message)
    {
        if (bossMessageText != null)
        {
            bossMessageText.text = message;
        }
        
        if (bossMessagePanel != null)
        {
            bossMessagePanel.SetActive(true);
            
            // Animate the message in
            bossMessagePanel.transform.localScale = Vector3.zero;
            bossMessagePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }
    
    /// <summary>
    /// Hide the boss message
    /// </summary>
    public void HideBossMessage()
    {
        if (bossMessagePanel != null)
        {
            // Animate the message out
            bossMessagePanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => {
                    bossMessagePanel.SetActive(false);
                });
        }
    }
    
    // End of NewBossPanel class
}
