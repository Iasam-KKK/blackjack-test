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
    public TextMeshProUGUI handsRemainingText;
    public TextMeshProUGUI currentHandText;
    
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
    private bool isVisible = false;
    
    private void Start()
    {
        // Find BossManager
        bossManager = FindObjectOfType<BossManager>();
        
        // Set up initial state
        if (bossBackground != null)
        {
            Color color = bossBackground.color;
            color.a = 0f;
            bossBackground.color = color;
        }
        
        // Don't hide initially - let the ShowBossPanel method control visibility
        // gameObject.SetActive(false); // Removed this line
    }
    
    private void Update()
    {
        if (bossManager != null && bossManager.IsBossActive())
        {
            UpdateBossDisplay();
        }
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
        
        if (isVisible) 
        {
            Debug.Log("Boss panel already visible, returning");
            return;
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
        
        // Animate background fade in
        if (bossBackground != null)
        {
            bossBackground.DOFade(0.3f, fadeInDuration);
        }
        
        // Animate UI elements
        AnimateUIElementsIn();
        
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
    /// Update the boss display with current information
    /// </summary>
    private void UpdateBossDisplay()
    {
        BossData currentBoss = bossManager?.GetCurrentBoss();
        if (currentBoss == null) return;
        
        int currentHealth = bossManager.GetCurrentBossHealth();
        int maxHealth = currentBoss.maxHealth;
        int currentHand = bossManager.currentHand;
        int handsPerRound = currentBoss.handsPerRound;
        
        // Update health bar with smooth animation
        if (bossHealthBar != null)
        {
            // Calculate health percentage (full bar = full health, empty bar = no health)
            float targetValue = (float)currentHealth / maxHealth;
            Debug.Log($"Updating health bar: {currentHealth}/{maxHealth} = {targetValue}");
            bossHealthBar.DOFillAmount(targetValue, healthBarAnimationDuration).SetEase(Ease.OutQuad);
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth}/{maxHealth}";
        }
        
        // Update hands remaining
        if (handsRemainingText != null)
        {
            int handsRemaining = handsPerRound - currentHand;
            handsRemainingText.text = $"Hands: {handsRemaining}/{handsPerRound}";
        }
        
        // Update current hand
        if (currentHandText != null)
        {
            currentHandText.text = $"Hand {currentHand + 1}";
        }
        
        // Update boss name and description if they exist
        if (bossNameText != null && currentBoss.bossName != null)
        {
            bossNameText.text = currentBoss.bossName;
        }
        
        if (bossDescriptionText != null && currentBoss.bossDescription != null)
        {
            bossDescriptionText.text = currentBoss.bossDescription;
        }
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
    
    // End of NewBossPanel class
}
