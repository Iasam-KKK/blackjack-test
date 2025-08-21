using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BossPreviewPanel : MonoBehaviour
{
    [Header("Boss Preview Elements")]
    public Image bossPreviewImage;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossStatusText;
    
    [Header("Animation Settings")]
    public float updateDuration = 0.3f;
    public float shakeIntensity = 5f;
    public float shakeDuration = 0.3f;
    
    private BossManager bossManager;
    
    private void Start()
    {
        bossManager = BossManager.Instance;
        
        // Subscribe to boss events
        if (bossManager != null)
        {
            bossManager.OnBossDefeated += OnBossDefeated;
            bossManager.OnBossHealed += OnBossHealed;
        }
        
        UpdateBossPreview();
    }
    
    private void Update()
    {
        // Update preview when boss changes
        if (bossManager != null && bossManager.IsBossActive())
        {
            UpdateBossPreview();
        }
    }
    
    /// <summary>
    /// Update the boss preview with current boss information
    /// </summary>
    public void UpdateBossPreview()
    {
        if (bossManager == null) return;
        
        BossData currentBoss = bossManager.GetCurrentBoss();
        if (currentBoss == null) return;
        
        // Update boss image
        if (bossPreviewImage != null && currentBoss.bossPortrait != null)
        {
            bossPreviewImage.sprite = currentBoss.bossPortrait;
        }
        
        // Update boss name
        if (bossNameText != null)
        {
            bossNameText.text = currentBoss.bossName;
        }
        
        // Update boss status
        if (bossStatusText != null)
        {
            int currentHealth = bossManager.GetCurrentBossHealth();
            int maxHealth = currentBoss.maxHealth;
            int currentHand = bossManager.currentHand;
            int handsPerRound = currentBoss.handsPerRound;
            
            bossStatusText.text = $"{currentHealth} {currentHand + 1}";
        }
    }
    
    /// <summary>
    /// Update boss preview with animation
    /// </summary>
    public void UpdateBossPreviewWithAnimation()
    {
        if (bossPreviewImage != null)
        {
            // Shake the preview image
            bossPreviewImage.transform.DOShakePosition(shakeDuration, shakeIntensity, 10, 90, false, true);
        }
        
        // Update the preview
        UpdateBossPreview();
    }
    
    /// <summary>
    /// Show next boss preview with transition effect
    /// </summary>
    public void ShowNextBossPreview(BossData nextBoss)
    {
        if (bossPreviewImage != null && nextBoss.bossPortrait != null)
        {
            // Fade out current image
            bossPreviewImage.DOFade(0f, updateDuration * 0.5f).OnComplete(() => {
                // Change to next boss image
                bossPreviewImage.sprite = nextBoss.bossPortrait;
                
                // Fade in new image
                bossPreviewImage.DOFade(1f, updateDuration * 0.5f);
            });
        }
        
        if (bossNameText != null)
        {
            bossNameText.text = nextBoss.bossName;
        }
        
        if (bossStatusText != null)
        {
            bossStatusText.text = $"{nextBoss.maxHealth} 1";
        }
        
        // Animate the preview panel
        transform.DOScale(Vector3.one * 1.1f, updateDuration).SetEase(Ease.OutBack)
            .OnComplete(() => {
                transform.DOScale(Vector3.one, updateDuration * 0.5f);
            });
    }
    
    /// <summary>
    /// Called when a boss is defeated
    /// </summary>
    private void OnBossDefeated(BossData defeatedBoss)
    {
        // Find next boss
        if (bossManager != null)
        {
            var nextBoss = bossManager.allBosses.Find(b => b.unlockOrder == bossManager.GetTotalBossesDefeated());
            if (nextBoss != null)
            {
                // Show next boss preview after a delay
                DOVirtual.DelayedCall(2f, () => {
                    ShowNextBossPreview(nextBoss);
                });
            }
        }
    }
    
    /// <summary>
    /// Called when a boss heals
    /// </summary>
    private void OnBossHealed(BossData healedBoss)
    {
        UpdateBossPreviewWithAnimation();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (bossManager != null)
        {
            bossManager.OnBossDefeated -= OnBossDefeated;
            bossManager.OnBossHealed -= OnBossHealed;
        }
    }
}
