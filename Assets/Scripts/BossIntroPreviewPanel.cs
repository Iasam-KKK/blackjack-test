using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class BossIntroPreviewPanel : MonoBehaviour
{
    [Header("Boss Intro Elements")]
    public Image bossPreviewImage;
    public Image bossIntroPanelBg;  // New field for background image
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI bossStatusText;
    
    [Header("Animation Settings")]
    public float updateDuration = 0.3f;
    public float shakeIntensity = 5f;
    public float shakeDuration = 0.3f;
    public float displayDuration = 5f;  // Duration to display before auto-disable
    
    [Header("Intro Animation Settings")]
    public float introFadeDuration = 1f;
    public float introScaleAmount = 1.2f;
    public Ease introEase = Ease.OutBack;
    
    private BossManager bossManager;
    private Coroutine autoDisableCoroutine;
    
    private void Start()
    {
        Debug.Log("BossIntroPreviewPanel: Start() called");
        bossManager = BossManager.Instance;
        
        Debug.Log($"BossIntroPreviewPanel: BossManager instance found: {bossManager != null}");
        
        // Subscribe to boss events
        if (bossManager != null)
        {
            bossManager.OnBossDefeated += OnBossDefeated;
            bossManager.OnBossHealed += OnBossHealed;
            Debug.Log("BossIntroPreviewPanel: Subscribed to boss events");
        }
        else
        {
            Debug.LogError("BossIntroPreviewPanel: BossManager.Instance is null!");
        }
         
        gameObject.SetActive(false);
        Debug.Log("BossIntroPreviewPanel: Panel set to inactive");
    }
    
    /// <summary>
    /// Show the boss intro panel with current boss information
    /// </summary>
    public void ShowBossIntro()
    {
        Debug.Log("BossIntroPreviewPanel: ShowBossIntro() called");
        
        if (bossManager == null) 
        {
            Debug.LogError("BossIntroPreviewPanel: BossManager is null in ShowBossIntro");
            return;
        }
        
        BossData currentBoss = bossManager.GetCurrentBoss();
        if (currentBoss == null) 
        {
            Debug.LogError("BossIntroPreviewPanel: Current boss is null");
            return;
        }
        
        Debug.Log($"BossIntroPreviewPanel: Showing intro for boss: {currentBoss.bossName}");
        
        // Enable the panel
        gameObject.SetActive(true);
        Debug.Log("BossIntroPreviewPanel: Panel activated");
        
        // Update all boss information
        UpdateBossIntroDisplay(currentBoss);
        
        // Start intro animation
        PlayIntroAnimation();
        
        // Start auto-disable timer
        StartAutoDisableTimer();
    }
    
    /// <summary>
    /// Show boss intro for a specific boss
    /// </summary>
    public void ShowBossIntro(BossData boss)
    {
        Debug.Log($"BossIntroPreviewPanel: ShowBossIntro(BossData) called with boss: {boss?.bossName ?? "null"}");
        
        if (boss == null) 
        {
            Debug.LogError("BossIntroPreviewPanel: Boss data is null in ShowBossIntro(BossData)");
            return;
        }
        
        Debug.Log($"BossIntroPreviewPanel: Showing intro for specific boss: {boss.bossName}");
        
        // Enable the panel
        gameObject.SetActive(true);
        Debug.Log("BossIntroPreviewPanel: Panel activated for specific boss");
        
        // Update all boss information
        UpdateBossIntroDisplay(boss);
        
        // Start intro animation
        PlayIntroAnimation();
        
        // Start auto-disable timer
        StartAutoDisableTimer();
    }
    
    /// <summary>
    /// Update the boss intro display with boss information
    /// </summary>
    private void UpdateBossIntroDisplay(BossData boss)
    {
        Debug.Log($"Updating boss intro display for: {boss.bossName}");
        
        // Update boss image
        if (bossPreviewImage != null && boss.bossPortrait != null)
        {
            bossPreviewImage.sprite = boss.bossPortrait;
            Debug.Log($"Updated boss portrait for: {boss.bossName}");
        }
        else
        {
            Debug.LogWarning($"Boss preview image or portrait is null - Image: {bossPreviewImage != null}, Portrait: {boss.bossPortrait != null}");
        }
        
        // Update boss background from scriptable object
        if (bossIntroPanelBg != null && boss.bossIntroPanelBg != null)
        {
            bossIntroPanelBg.sprite = boss.bossIntroPanelBg;
            Debug.Log($"Updated boss intro panel background for: {boss.bossName}");
        }
        else
        {
            Debug.LogWarning($"Boss intro panel bg or sprite is null - Panel: {bossIntroPanelBg != null}, Sprite: {boss.bossIntroPanelBg != null}");
        }
        
        // Update boss name
        if (bossNameText != null)
        {
            bossNameText.text = boss.bossName;
            Debug.Log($"Updated boss name text: {boss.bossName}");
        }
        else
        {
            Debug.LogWarning("Boss name text component is null");
        }
        
        // Update boss status
        if (bossStatusText != null)
        {
            int maxHealth = boss.maxHealth;
            int handsPerRound = boss.handsPerRound;
            
            bossStatusText.text = $"Health: {maxHealth} | Hands: {handsPerRound}";
            Debug.Log($"Updated boss status: Health: {maxHealth} | Hands: {handsPerRound}");
        }
        else
        {
            Debug.LogWarning("Boss status text component is null");
        }
    }
    
    /// <summary>
    /// Play the intro animation sequence
    /// </summary>
    private void PlayIntroAnimation()
    {
        // Kill any existing animations to prevent conflicts
        DOTween.Kill(transform);
        if (bossPreviewImage != null) DOTween.Kill(bossPreviewImage.transform);
        if (bossIntroPanelBg != null) DOTween.Kill(bossIntroPanelBg.transform);
        
        // Reset transforms and alpha values
        transform.localScale = Vector3.zero;
        
        if (bossPreviewImage != null)
        {
            var imageColor = bossPreviewImage.color;
            imageColor.a = 0f;
            bossPreviewImage.color = imageColor;
        }
        
        if (bossIntroPanelBg != null)
        {
            var bgColor = bossIntroPanelBg.color;
            bgColor.a = 0f;
            bossIntroPanelBg.color = bgColor;
        }
        
        // Create animation sequence
        Sequence introSequence = DOTween.Sequence();
        
        // Start with background fade in
        if (bossIntroPanelBg != null)
        {
            introSequence.Append(bossIntroPanelBg.DOFade(1f, introFadeDuration * 0.3f));
        }
        
        // Scale up the panel with bounce effect
        introSequence.Append(transform.DOScale(Vector3.one * introScaleAmount, introFadeDuration * 0.5f).SetEase(introEase));
        introSequence.Append(transform.DOScale(Vector3.one, introFadeDuration * 0.3f).SetEase(Ease.OutBounce));
        
        // Fade in boss image
        if (bossPreviewImage != null)
        {
            introSequence.Join(bossPreviewImage.DOFade(1f, introFadeDuration * 0.6f));
        }
        
        // Add shake effect at the end
        introSequence.AppendCallback(() => {
            if (bossPreviewImage != null)
            {
                bossPreviewImage.transform.DOShakePosition(shakeDuration, shakeIntensity, 10, 90, false, true);
            }
        });
        
        Debug.Log($"Playing intro animation for boss: {(bossManager?.GetCurrentBoss()?.bossName ?? "Unknown")}");
    }
    
    /// <summary>
    /// Start the auto-disable timer
    /// </summary>
    private void StartAutoDisableTimer()
    {
        // Stop any existing timer
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
        }
        
        // Start new timer
        autoDisableCoroutine = StartCoroutine(AutoDisableAfterDelay());
    }
    
    /// <summary>
    /// Coroutine to auto-disable the panel after delay
    /// </summary>
    private IEnumerator AutoDisableAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        HideBossIntro();
    }
    
    /// <summary>
    /// Hide the boss intro panel with animation
    /// </summary>
    public void HideBossIntro()
    {
        // Stop auto-disable timer
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
            autoDisableCoroutine = null;
        }
        
        // Create hide animation
        Sequence hideSequence = DOTween.Sequence();
        
        // Fade out elements
        if (bossPreviewImage != null)
        {
            hideSequence.Append(bossPreviewImage.DOFade(0f, introFadeDuration * 0.5f));
        }
        
        if (bossIntroPanelBg != null)
        {
            hideSequence.Join(bossIntroPanelBg.DOFade(0f, introFadeDuration * 0.5f));
        }
        
        // Scale down
        hideSequence.Join(transform.DOScale(Vector3.zero, introFadeDuration * 0.5f).SetEase(Ease.InBack));
        
        // Disable the panel when animation completes
        hideSequence.OnComplete(() => {
            gameObject.SetActive(false);
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
                // Show next boss intro after a delay
                DOVirtual.DelayedCall(2f, () => {
                    ShowBossIntro(nextBoss);
                });
            }
        }
    }
    
    /// <summary>
    /// Called when a boss heals
    /// </summary>
    private void OnBossHealed(BossData healedBoss)
    {
        // Optional: Could show a brief flash or effect
        if (gameObject.activeInHierarchy && bossPreviewImage != null)
        {
            bossPreviewImage.transform.DOShakePosition(shakeDuration * 0.5f, shakeIntensity * 0.5f, 5, 90, false, true);
        }
    }
    
    /// <summary>
    /// Manually trigger boss intro for current boss
    /// </summary>
    public void TriggerCurrentBossIntro()
    {
        Debug.Log("BossIntroPreviewPanel: TriggerCurrentBossIntro() called manually");
        ShowBossIntro();
    }
    
    /// <summary>
    /// Test method to force show intro with dummy data
    /// </summary>
    [ContextMenu("Test Show Intro Panel")]
    public void TestShowIntroPanel()
    {
        Debug.Log("BossIntroPreviewPanel: TestShowIntroPanel() called");
        
        // Force show the panel for testing
        gameObject.SetActive(true);
        
        // Try to get boss data
        if (bossManager != null)
        {
            var currentBoss = bossManager.GetCurrentBoss();
            if (currentBoss != null)
            {
                Debug.Log($"Testing with current boss: {currentBoss.bossName}");
                UpdateBossIntroDisplay(currentBoss);
                PlayIntroAnimation();
                StartAutoDisableTimer();
            }
            else
            {
                Debug.LogError("No current boss found for testing");
            }
        }
        else
        {
            Debug.LogError("BossManager not found for testing");
        }
    }
    
    /// <summary>
    /// Force hide the intro panel immediately
    /// </summary>
    public void ForceHide()
    {
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
            autoDisableCoroutine = null;
        }
        
        // Kill any running animations
        DOTween.Kill(transform);
        if (bossPreviewImage != null) DOTween.Kill(bossPreviewImage.transform);
        if (bossIntroPanelBg != null) DOTween.Kill(bossIntroPanelBg.transform);
        
        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // Stop any running coroutines
        if (autoDisableCoroutine != null)
        {
            StopCoroutine(autoDisableCoroutine);
        }
        
        // Kill any running animations
        DOTween.Kill(transform);
        if (bossPreviewImage != null) DOTween.Kill(bossPreviewImage.transform);
        if (bossIntroPanelBg != null) DOTween.Kill(bossIntroPanelBg.transform);
        
        // Unsubscribe from events
        if (bossManager != null)
        {
            bossManager.OnBossDefeated -= OnBossDefeated;
            bossManager.OnBossHealed -= OnBossHealed;
        }
    }
}
