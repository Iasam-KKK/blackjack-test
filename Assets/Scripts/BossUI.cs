using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections;

public class BossUI : MonoBehaviour
{
    [Header("Boss Display")]
    public Image bossPortrait;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossDescriptionText;
    public Slider bossHealthBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI handsRemainingText;
    public TextMeshProUGUI currentHandText;
    
    [Header("Boss Effects")]
    public Image bossBackground;
    public ParticleSystem bossParticles;
    public AudioSource bossAudioSource;
    
    [Header("Transition UI")]
    public Transform centerPosition; // Position for boss to appear in center
    public Transform panelPosition; // Position for boss in the panel
    
    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float healthBarAnimationDuration = 0.3f;
    public float shakeIntensity = 10f;
    public float shakeDuration = 0.5f;
    public float transitionDuration = 1.0f;
    
    private BossManager bossManager;
    private bool isVisible = false;
    private Coroutine autoHideCoroutine;
    
    private void Start()
    {
        // Find BossManager
        bossManager = FindObjectOfType<BossManager>();
        
        // Debug component assignments
        DebugComponentAssignments();
        
        // Set up initial state
        if (bossBackground != null)
        {
            Color color = bossBackground.color;
            color.a = 0f;
            bossBackground.color = color;
        }
        
        // Don't hide initially - let the boss management system control visibility
        // StartCoroutine(DelayedHide()); // Commented out to prevent auto-hiding
    }
    
    /// <summary>
    /// Debug method to check component assignments
    /// </summary>
    private void DebugComponentAssignments()
    {
        Debug.Log("=== BossUI Component Assignment Debug ===");
        Debug.Log($"bossPortrait: {bossPortrait != null} {(bossPortrait != null ? $"(GameObject: {bossPortrait.gameObject.name})" : "")}");
        Debug.Log($"bossNameText: {bossNameText != null}");
        Debug.Log($"bossDescriptionText: {bossDescriptionText != null}");
        Debug.Log($"bossBackground: {bossBackground != null}");
        Debug.Log($"centerPosition: {centerPosition != null}");
        Debug.Log($"panelPosition: {panelPosition != null}");
        Debug.Log("=========================================");
    }
    
    /// <summary>
    /// Test method to manually trigger boss introduction (for debugging)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestBossIntroduction()
    {
        Debug.Log("Manual test: Starting boss introduction");
        if (bossManager != null && bossManager.GetCurrentBoss() != null)
        {
            ShowBossInCenter(bossManager.GetCurrentBoss());
        }
        else
        {
            Debug.LogWarning("Cannot test boss introduction - no current boss found");
        }
    }
    private IEnumerator DelayedHide()
    {
        yield return new WaitForSeconds(2.5f);
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Auto-hide the boss introduction panel after a delay
    /// </summary>
    private IEnumerator AutoHideAfterDelay(float delay)
    {
        Debug.Log($"AutoHideAfterDelay started, will hide in {delay} seconds");
        yield return new WaitForSeconds(delay);
        
        Debug.Log("AutoHideAfterDelay timer completed, starting fade out");
        
        // Fade out and hide
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log("Fading out CanvasGroup");
            canvasGroup.DOFade(0f, fadeInDuration).OnComplete(() => {
                Debug.Log("Fade complete, hiding GameObject");
                gameObject.SetActive(false);
                isVisible = false;
            });
        }
        else
        {
            Debug.Log("No CanvasGroup found, hiding GameObject immediately");
            gameObject.SetActive(false);
            isVisible = false;
        }
        
        autoHideCoroutine = null; // Clear the reference
        Debug.Log("Boss introduction panel auto-hidden after delay");
    }

    
    private void Update()
    {
        if (bossManager != null && bossManager.IsBossActive())
        {
            UpdateBossDisplay();
        }
    }
    
    /// <summary>
    /// Show the boss UI with animation
    /// </summary>
    public void ShowBossUI(BossData bossData)
    {
        if (isVisible) return;
        
        gameObject.SetActive(true);
        isVisible = true;
        
        // Set boss data
        if (bossPortrait != null && bossData.bossPortrait != null)
        {
            bossPortrait.sprite = bossData.bossPortrait;
        }
        
        if (bossNameText != null)
        {
            bossNameText.text = bossData.bossName;
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.text = bossData.bossDescription;
        }
        
        // Animate background fade in
        if (bossBackground != null)
        {
            bossBackground.DOFade(0.3f, fadeInDuration);
        }
        
        // Animate UI elements
        AnimateUIElementsIn();
        
        // Play boss music if available
        if (bossAudioSource != null && bossData.bossMusic != null)
        {
            bossAudioSource.clip = bossData.bossMusic;
            bossAudioSource.Play();
        }
        
        // Start particle effects
        if (bossParticles != null)
        {
            bossParticles.Play();
        }
    }
    
    /// <summary>
    /// Hide the boss UI with animation
    /// </summary>
    public void HideBossUI()
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
        BossData currentBoss = bossManager.GetCurrentBoss();
        if (currentBoss == null) return;
        
        int currentHealth = bossManager.GetCurrentBossHealth();
        int maxHealth = currentBoss.maxHealth;
        int currentHand = bossManager.currentHand;
        int handsPerRound = currentBoss.handsPerRound;
        
        // Update health bar with smooth animation
        if (bossHealthBar != null)
        {
            float targetValue = 1f - ((float)currentHealth / maxHealth);
            bossHealthBar.DOValue(targetValue, healthBarAnimationDuration).SetEase(Ease.OutQuad);
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
            bossHealthBar.value = 0f;
            bossHealthBar.DOValue(0f, healthBarAnimationDuration).SetDelay(fadeInDuration * 0.5f);
        }
    }
    
    /// <summary>
    /// Shake the boss UI when boss takes damage
    /// </summary>
    public void ShakeBossUI()
    {
        transform.DOShakePosition(shakeDuration, shakeIntensity, 10, 90, false, true);
        
        // Flash the health bar red
        if (bossHealthBar != null)
        {
            var fillImage = bossHealthBar.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                Color originalColor = fillImage.color;
                fillImage.DOColor(Color.red, 0.1f).OnComplete(() => {
                    fillImage.DOColor(originalColor, 0.1f);
                });
            }
        }
    }
    
    /// <summary>
    /// Show boss defeat effect
    /// </summary>
    public void ShowBossDefeatEffect()
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
    public void ShowBossHealEffect()
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
    /// Show boss in center of screen for transition
    /// </summary>
    public void ShowBossInCenter(BossData bossData)
    {
        Debug.Log($"ShowBossInCenter called for boss: {bossData?.bossName}");
        
        // Ensure the GameObject is active and visible state is set
        gameObject.SetActive(true);
        isVisible = true;
        
        // Set boss data with force refresh
        Debug.Log($"Attempting to set boss portrait. bossPortrait component: {bossPortrait != null}");
        if (bossData != null)
        {
            Debug.Log($"Boss data exists: {bossData.bossName}, Portrait sprite: {bossData.bossPortrait != null}");
            if (bossData.bossPortrait != null)
            {
                Debug.Log($"Portrait sprite name: {bossData.bossPortrait.name}");
            }
        }
        
        if (bossPortrait != null && bossData?.bossPortrait != null)
        {
            Debug.Log($"Setting boss portrait to: {bossData.bossPortrait.name}");
            bossPortrait.sprite = bossData.bossPortrait;
            
            // Force the image to refresh and ensure it's enabled
            bossPortrait.enabled = false;
            bossPortrait.enabled = true;
            bossPortrait.gameObject.SetActive(false);
            bossPortrait.gameObject.SetActive(true);
            
            Debug.Log($"Boss portrait updated successfully. Current sprite: {bossPortrait.sprite?.name}");
        }
        else
        {
            Debug.LogError($"Failed to set boss portrait! Portrait component: {bossPortrait != null}, Boss data: {bossData != null}, Portrait sprite: {bossData?.bossPortrait != null}");
            if (bossPortrait == null)
            {
                Debug.LogError("BossPortrait Image component is not assigned in the Inspector!");
            }
        }
        
        if (bossNameText != null)
        {
            bossNameText.text = bossData.bossName;
            Debug.Log($"Set boss name to: {bossData.bossName}");
        }
        
        if (bossDescriptionText != null)
        {
            bossDescriptionText.text = bossData.bossDescription;
            Debug.Log($"Set boss description to: {bossData.bossDescription}");
        }
        
        // Move to center position
        if (centerPosition != null)
        {
            transform.position = centerPosition.position;
        }
        
        // Scale up for dramatic effect
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one * 1.5f, fadeInDuration).SetEase(Ease.OutBack);
        
        // Show with fade in
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeInDuration);
        
        // Animate background fade in
        if (bossBackground != null)
        {
            bossBackground.DOFade(0.3f, fadeInDuration);
        }
        
        // Stop any existing auto-hide coroutine and start a new one
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(5f));
    }
    
    /// <summary>
    /// Animate boss from center to panel position
    /// </summary>
    public void AnimateToPanel()
    {
        if (panelPosition != null)
        {
            // Animate to panel position
            transform.DOMove(panelPosition.position, transitionDuration).SetEase(Ease.InOutQuad);
            
            // Scale down to normal size
            transform.DOScale(Vector3.one, transitionDuration).SetEase(Ease.InOutQuad);
            
            // Show full UI elements
            AnimateUIElementsIn();
        }
    }
}
