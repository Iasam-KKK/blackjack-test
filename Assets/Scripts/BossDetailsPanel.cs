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
    
    [Header("Minion Slots")]
    public Button[] minionSlots = new Button[3]; // 3 minion portrait buttons
    public Image[] minionPortraits = new Image[3]; // Portrait images
    public GameObject[] minionDefeatedMarkers = new GameObject[3]; // Checkmarks
    
    [Header("Minion Panel")]
    public MinionDetailsPanel minionDetailsPanel; // Panel that shows below
    
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
    private MinionData selectedMinion;
    
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
        
        // Update minion slots
        UpdateMinionSlots(boss);
        
        // Update play button based on minion progress
        UpdatePlayButton(boss);
    }
    
    /// <summary>
    /// Update the 3 minion portrait slots
    /// </summary>
    private void UpdateMinionSlots(BossData boss)
    {
        // Hide minion details panel when switching bosses
        if (minionDetailsPanel != null)
        {
            minionDetailsPanel.HidePanel();
        }
        selectedMinion = null;
        
        // Check if boss has minions
        if (boss.minions == null || boss.minions.Count == 0)
        {
            // No minions - hide all slots
            for (int i = 0; i < 3; i++)
            {
                if (minionSlots[i] != null)
                    minionSlots[i].gameObject.SetActive(false);
            }
            return;
        }
        
        // Update each minion slot
        for (int i = 0; i < 3; i++)
        {
            if (i < boss.minions.Count && minionSlots[i] != null)
            {
                var minion = boss.minions[i];
                bool isDefeated = BossProgressionManager.Instance != null && 
                                BossProgressionManager.Instance.IsMinionDefeated(boss.bossType, minion.minionName);
                
                // Show slot
                minionSlots[i].gameObject.SetActive(true);
                
                // Set portrait
                if (minionPortraits[i] != null && minion.minionPortrait != null)
                {
                    minionPortraits[i].sprite = minion.minionPortrait;
                    minionPortraits[i].enabled = true;
                }
                else if (minionPortraits[i] != null)
                {
                    minionPortraits[i].enabled = false;
                }
                
                // Show/hide defeated marker
                if (minionDefeatedMarkers[i] != null)
                {
                    minionDefeatedMarkers[i].SetActive(isDefeated);
                }
                
                // Make slot non-interactable if defeated
                minionSlots[i].interactable = !isDefeated;
                
                // Wire up click event
                var minionData = minion;
                var slotIndex = i;
                minionSlots[i].onClick.RemoveAllListeners();
                minionSlots[i].onClick.AddListener(() => OnMinionSlotClicked(minionData, slotIndex));
            }
            else if (minionSlots[i] != null)
            {
                // Hide unused slots
                minionSlots[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Update play button text based on minion progression
    /// </summary>
    private void UpdatePlayButton(BossData boss)
    {
        if (playButton == null) return;
        
        bool isUnlocked = BossProgressionManager.Instance != null && 
                         BossProgressionManager.Instance.IsBossUnlocked(boss.bossType);
        bool isDefeated = BossProgressionManager.Instance != null && 
                         BossProgressionManager.Instance.IsBossDefeated(boss.bossType);
        
        var playButtonText = playButton.GetComponentInChildren<TextMeshProUGUI>();
        
        // Check if boss has minions
        if (boss.HasMinions() && boss.minions.Count >= 3)
        {
            int defeatedCount = BossProgressionManager.Instance != null ?
                              BossProgressionManager.Instance.GetMinionDefeatedCount(boss.bossType) : 0;
            
            bool bossUnlockedInAct = BossProgressionManager.Instance != null &&
                                    BossProgressionManager.Instance.IsBossUnlockedInAct(boss.bossType);
            
            if (isDefeated)
            {
                playButton.interactable = false;
                if (playButtonText != null)
                    playButtonText.text = "DEFEATED";
            }
            else if (bossUnlockedInAct && selectedMinion == null)
            {
                // Boss unlocked (2+ minions defeated) and no minion selected
                playButton.interactable = true;
                if (playButtonText != null)
                    playButtonText.text = "FIGHT BOSS";
            }
            else if (selectedMinion != null)
            {
                // Minion selected - play button starts minion battle
                playButton.interactable = true;
                if (playButtonText != null)
                    playButtonText.text = "PLAY";
            }
            else
            {
                // Need to select a minion first
                playButton.interactable = false;
                if (playButtonText != null)
                    playButtonText.text = $"DEFEAT MINIONS ({defeatedCount}/3)";
            }
        }
        else
        {
            // No minions - standard boss play button
            playButton.interactable = isUnlocked && !isDefeated;
            
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
        
        // Also hide minion panel when boss panel closes
        if (minionDetailsPanel != null && minionDetailsPanel.IsVisible())
        {
            minionDetailsPanel.HidePanel();
        }
        
        // Clear selected minion
        selectedMinion = null;
        
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
    /// Handle minion slot click
    /// </summary>
    private void OnMinionSlotClicked(MinionData minion, int slotIndex)
    {
        if (minion == null || currentBoss == null) return;
        
        // Toggle minion selection
        if (selectedMinion == minion && minionDetailsPanel != null && minionDetailsPanel.IsVisible())
        {
            // Clicking same minion - deselect and hide panel
            selectedMinion = null;
            minionDetailsPanel.HidePanel();
        }
        else
        {
            // Select new minion and show panel
            selectedMinion = minion;
            
            Debug.Log($"[BossDetailsPanel] Minion selected: {minion.minionName}");
            
            // Show minion details panel
            if (minionDetailsPanel != null)
            {
                minionDetailsPanel.ShowMinionDetails(minion, currentBoss.bossType);
            }
        }
        
        // Update play button
        UpdatePlayButton(currentBoss);
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
        
        // Check if we're starting a minion battle or boss battle
        if (selectedMinion != null)
        {
            // Start minion battle
            Debug.Log($"[BossDetailsPanel] Starting minion battle with {selectedMinion.minionName}");
            
            // Start act if not already started
            if (BossProgressionManager.Instance != null)
            {
                BossProgressionManager.Instance.StartBossAct(currentBoss.bossType);
            }
            
            // Initialize minion encounter
            if (MinionEncounterManager.Instance != null)
            {
                MinionEncounterManager.Instance.InitializeMinion(selectedMinion, currentBoss.bossType);
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
        else
        {
            // Start boss battle (no minion selected or boss unlocked)
            Debug.Log($"[BossDetailsPanel] Starting boss battle with {currentBoss.bossName}");
            
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
    }
    
    /// <summary>
    /// Refresh the panel (call after minion defeated to update UI)
    /// </summary>
    public void RefreshPanel()
    {
        if (currentBoss != null && isVisible)
        {
            UpdateMinionSlots(currentBoss);
            UpdatePlayButton(currentBoss);
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

