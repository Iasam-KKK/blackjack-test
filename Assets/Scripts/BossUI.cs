using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BossUI : MonoBehaviour
{
    [Header("Boss Display")]
    public Image bossPortrait;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossDescriptionText;
    public Image  bossHealthBar;
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
        
        // Hide initially
        gameObject.SetActive(false);
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
        
        // Update health bar
        if (bossHealthBar != null)
        {
            float targetValue = 1f - ((float)currentHealth / maxHealth);
            bossHealthBar.DOFillAmount(targetValue, healthBarAnimationDuration);
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
            bossHealthBar.fillAmount = 0f;
            bossHealthBar.DOFillAmount(0f, healthBarAnimationDuration).SetDelay(fadeInDuration * 0.5f);
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
            var fillImage = bossHealthBar.GetComponent<Image>();
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
        if (bossPortrait != null && bossData.bossPortrait != null)
        {
            bossPortrait.sprite = bossData.bossPortrait;
        }
        
        if (bossNameText != null)
        {
            bossNameText.text = bossData.bossName;
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
        gameObject.SetActive(true);
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, fadeInDuration);
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
