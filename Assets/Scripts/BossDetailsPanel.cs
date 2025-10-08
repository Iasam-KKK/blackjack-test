using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Boss details panel that slides in from the right when a boss is selected
/// Shows boss name, portrait, description, mechanics, and play button
/// </summary>
public class BossDetailsPanel : MonoBehaviour
{
    [Header("Panel References")]
    public RectTransform panelRect;
    public CanvasGroup canvasGroup;
    
    [Header("Boss Info UI")]
    public TextMeshProUGUI bossNameText;
    public Image bossPortraitImage;
    public TextMeshProUGUI bossDescriptionText;
    public TextMeshProUGUI bossStatsText;
    public TextMeshProUGUI bossMechanicsText;
    
    [Header("Buttons")]
    public Button playButton;
    public Button closeButton;
    
    [Header("Animation Settings")]
    public float slideInDuration = 0.5f;
    public float slideOutDuration = 0.3f;
    public Ease slideEase = Ease.OutCubic;
    
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private BossData currentBoss;
    private bool isVisible = false;
    
    private void Awake()
    {
        // Calculate positions
        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // Store visible position (current position)
        visiblePosition = panelRect.anchoredPosition;
        
        // Calculate hidden position (off-screen to the right)
        hiddenPosition = new Vector2(visiblePosition.x + panelRect.rect.width + 100, visiblePosition.y);
        
        // Start hidden
        panelRect.anchoredPosition = hiddenPosition;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        // Wire up buttons
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        }
        
        Debug.Log("[BossDetailsPanel] Initialized - Hidden position: " + hiddenPosition + ", Visible: " + visiblePosition);
    }
    
    /// <summary>
    /// Show the panel with boss details
    /// </summary>
    public void ShowBossDetails(BossData boss)
    {
        if (boss == null)
        {
            Debug.LogError("[BossDetailsPanel] Cannot show details for null boss");
            return;
        }
        
        currentBoss = boss;
        
        // Update UI with boss data
        UpdateBossInfo(boss);
        
        // Animate panel in
        ShowPanel();
        
        Debug.Log($"[BossDetailsPanel] Showing details for {boss.bossName}");
    }
    
    /// <summary>
    /// Update all UI elements with boss data
    /// </summary>
    private void UpdateBossInfo(BossData boss)
    {
        // Boss name
        if (bossNameText != null)
        {
            bossNameText.text = boss.bossName;
        }
        
        // Boss portrait
        if (bossPortraitImage != null && boss.bossPortrait != null)
        {
            bossPortraitImage.sprite = boss.bossPortrait;
            bossPortraitImage.enabled = true;
        }
        else if (bossPortraitImage != null)
        {
            bossPortraitImage.enabled = false;
        }
        
        // Boss description
        if (bossDescriptionText != null)
        {
            bossDescriptionText.text = boss.bossDescription;
        }
        
        // Boss stats
        if (bossStatsText != null)
        {
            string stats = $"<b>Health:</b> {boss.maxHealth}\n";
            stats += $"<b>Hands per Round:</b> {boss.handsPerRound}\n";
            stats += $"<b>Difficulty:</b> {boss.difficultyMultiplier:F1}x";
            bossStatsText.text = stats;
        }
        
        // Boss mechanics
        if (bossMechanicsText != null)
        {
            if (boss.mechanics != null && boss.mechanics.Count > 0)
            {
                string mechanics = "<b>Special Abilities:</b>\n";
                foreach (var mechanic in boss.mechanics)
                {
                    if (mechanic.mechanicType != BossMechanicType.None)
                    {
                        mechanics += $"• {mechanic.mechanicName}\n";
                    }
                }
                bossMechanicsText.text = mechanics;
            }
            else
            {
                bossMechanicsText.text = "<b>Special Abilities:</b>\nNone";
            }
        }
        
        // Check if boss is available to play
        if (playButton != null)
        {
            bool isUnlocked = BossProgressionManager.Instance != null && 
                             BossProgressionManager.Instance.IsBossUnlocked(boss.bossType);
            bool isDefeated = BossProgressionManager.Instance != null && 
                             BossProgressionManager.Instance.IsBossDefeated(boss.bossType);
            
            playButton.interactable = isUnlocked && !isDefeated;
            
            var playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
            if (playButtonText != null)
            {
                if (isDefeated)
                {
                    playButtonText.text = "DEFEATED";
                }
                else if (!isUnlocked)
                {
                    playButtonText.text = "LOCKED";
                }
                else
                {
                    playButtonText.text = "PLAY";
                }
            }
        }
    }
    
    /// <summary>
    /// Animate panel sliding in from right
    /// </summary>
    private void ShowPanel()
    {
        if (isVisible) return;
        
        isVisible = true;
        
        // Kill any existing animations
        panelRect.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        
        // Animate position
        panelRect.DOAnchorPos(visiblePosition, slideInDuration)
            .SetEase(slideEase)
            .SetUpdate(true);
        
        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, slideInDuration * 0.5f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                });
        }
        
        Debug.Log("[BossDetailsPanel] Panel sliding in");
    }
    
    /// <summary>
    /// Animate panel sliding out to right
    /// </summary>
    public void HidePanel()
    {
        if (!isVisible) return;
        
        isVisible = false;
        
        // Kill any existing animations
        panelRect.DOKill();
        if (canvasGroup != null) canvasGroup.DOKill();
        
        // Disable interaction immediately
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        // Fade out
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, slideOutDuration * 0.5f)
                .SetUpdate(true);
        }
        
        // Animate position
        panelRect.DOAnchorPos(hiddenPosition, slideOutDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
        
        Debug.Log("[BossDetailsPanel] Panel sliding out");
    }
    
    /// <summary>
    /// Handle play button click
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (currentBoss == null)
        {
            Debug.LogError("[BossDetailsPanel] No boss selected!");
            return;
        }
        
        Debug.Log($"[BossDetailsPanel] Starting battle with {currentBoss.bossName}");
        
        // Select boss in progression manager
        if (BossProgressionManager.Instance != null)
        {
            BossProgressionManager.Instance.SelectBoss(currentBoss.bossType);
        }
        
        // Load game scene
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameScene();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }
    }
    
    /// <summary>
    /// Check if panel is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Get current boss being displayed
    /// </summary>
    public BossData GetCurrentBoss()
    {
        return currentBoss;
    }
}

