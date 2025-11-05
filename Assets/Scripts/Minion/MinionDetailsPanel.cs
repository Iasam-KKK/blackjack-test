using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Minion details panel that slides in below boss panel when minion is selected
/// Shows minion name, portrait, description, and stats
/// </summary>
public class MinionDetailsPanel : MonoBehaviour
{
    [Header("Panel References")]
    public RectTransform panelRect;
    public CanvasGroup canvasGroup;
    
    [Header("Minion Info UI")]
    public TextMeshProUGUI minionNameText;
    public Image minionPortraitImage;
    public TextMeshProUGUI minionDescriptionText;
    public TextMeshProUGUI minionStatsText;
    public TextMeshProUGUI minionMechanicsText;
    
    [Header("Buttons")]
    public Button closeButton;
    
    [Header("Animation Settings")]
    public float slideInDuration = 0.4f;
    public float slideOutDuration = 0.25f;
    public Ease slideEase = Ease.OutCubic;
    
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private MinionData currentMinion;
    private BossType currentBossType;
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
        
        // Calculate hidden position (below visible position)
        hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y - panelRect.rect.height - 50);
        
        // Start hidden
        panelRect.anchoredPosition = hiddenPosition;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        
        // Wire up close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePanel);
        }
        
        Debug.Log("[MinionDetailsPanel] Initialized");
    }
    
    /// <summary>
    /// Show the panel with minion details
    /// </summary>
    public void ShowMinionDetails(MinionData minion, BossType bossType)
    {
        if (minion == null)
        {
            Debug.LogError("[MinionDetailsPanel] Cannot show details for null minion");
            return;
        }
        
        currentMinion = minion;
        currentBossType = bossType;
        
        // Update UI with minion data
        UpdateMinionInfo(minion, bossType);
        
        // Animate panel in
        ShowPanel();
        
        Debug.Log($"[MinionDetailsPanel] Showing details for {minion.minionName}");
    }
    
    /// <summary>
    /// Update all UI elements with minion data
    /// </summary>
    private void UpdateMinionInfo(MinionData minion, BossType bossType)
    {
        // Minion name
        if (minionNameText != null)
        {
            minionNameText.text = minion.minionName;
        }
        
        // Minion portrait
        if (minionPortraitImage != null && minion.minionPortrait != null)
        {
            minionPortraitImage.sprite = minion.minionPortrait;
            minionPortraitImage.enabled = true;
        }
        else if (minionPortraitImage != null)
        {
            minionPortraitImage.enabled = false;
        }
        
        // Minion description
        if (minionDescriptionText != null)
        {
            minionDescriptionText.text = minion.minionDescription;
        }
        
        // Minion stats
        if (minionStatsText != null)
        {
            string stats = $"<b>Health:</b> {minion.maxHealth}\n";
            stats += $"<b>Hands:</b> {minion.handsPerRound}\n";
            stats += $"<b>Difficulty:</b> {minion.difficultyMultiplier:F1}x";
            
            // Show defeat status
            bool isDefeated = BossProgressionManager.Instance != null &&
                            BossProgressionManager.Instance.IsMinionDefeated(bossType, minion.minionName);
            
            if (isDefeated)
            {
                stats += "\n\n<color=green><b>✓ DEFEATED</b></color>";
            }
            
            minionStatsText.text = stats;
        }
        
        // Minion mechanics
        if (minionMechanicsText != null)
        {
            if (minion.mechanics != null && minion.mechanics.Count > 0)
            {
                string mechanics = "<b>Special Ability:</b>\n";
                foreach (var mechanic in minion.mechanics)
                {
                    if (mechanic.mechanicType != BossMechanicType.None)
                    {
                        mechanics += $"• <b>{mechanic.mechanicName}:</b> {mechanic.mechanicDescription}\n";
                        mechanics += $"  <color=yellow>({mechanic.activationChance * 100:F0}% chance)</color>\n";
                    }
                }
                minionMechanicsText.text = mechanics;
            }
            else
            {
                minionMechanicsText.text = "<b>Special Ability:</b>\nNone";
            }
        }
    }
    
    /// <summary>
    /// Animate panel sliding up
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
        
        Debug.Log("[MinionDetailsPanel] Panel sliding up");
    }
    
    /// <summary>
    /// Animate panel sliding down
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
        
        Debug.Log("[MinionDetailsPanel] Panel sliding down");
    }
    
    /// <summary>
    /// Check if panel is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Get current minion being displayed
    /// </summary>
    public MinionData GetCurrentMinion()
    {
        return currentMinion;
    }
    
    /// <summary>
    /// Deselect minion (called when panel is hidden)
    /// </summary>
    public void OnPanelHidden()
    {
        currentMinion = null;
    }
}

