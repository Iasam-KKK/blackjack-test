using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Controls the Boss Map scene UI and boss selection
/// Temporary implementation for testing - will be replaced with full UI later
/// </summary>
public class BossMapController : MonoBehaviour
{
    [Header("UI References")]
    public Button returnToMenuButton;
    public TextMeshProUGUI bossListText;
    public Transform bossButtonContainer;
    public GameObject bossButtonPrefab;
    public BossDetailsPanel bossDetailsPanel;
    
    [Header("Simple UI (for testing)")]
    public bool useSimpleUI = true;
    
    private List<GameObject> spawnedButtons = new List<GameObject>();
    
    private void Start()
    {
        Debug.Log("[BossMapController] Initializing Boss Map");
        
        // Wire up return button programmatically
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
            Debug.Log("[BossMapController] Return button wired");
        }
        
        // Small delay to ensure BossProgressionManager is fully initialized
        Invoke("RefreshBossList", 0.1f);
    }
    
    /// <summary>
    /// Refresh the boss selection list
    /// </summary>
    public void RefreshBossList()
    {
        if (BossProgressionManager.Instance == null)
        {
            Debug.LogError("[BossMapController] BossProgressionManager not found!");
            if (bossListText != null)
            {
                bossListText.text = "ERROR: BossProgressionManager not found!\nMake sure to start from Main Menu.";
            }
            return;
        }
        
        // Get ALL bosses sorted by unlock order (creates the tower from bottom to top)
        var allBosses = BossProgressionManager.Instance.allBosses;
        
        Debug.Log($"[BossMapController] Displaying {allBosses.Count} total bosses in tower");
        
        if (useSimpleUI)
        {
            // Simple text-based UI for testing
            DisplaySimpleBossList(allBosses);
        }
        else
        {
            // Button-based UI (tower layout)
            DisplayBossTower(allBosses);
        }
        
        // Update the boss panel to show the current/next boss
        NewBossPanel bossPanel = FindObjectOfType<NewBossPanel>();
        if (bossPanel != null)
        {
            bossPanel.UpdateHealthBar(); // This will trigger UpdateBossDisplay
            Debug.Log("[BossMapController] Boss panel refreshed");
        }
    }
    
    /// <summary>
    /// Display bosses as simple text with instructions (for testing)
    /// </summary>
    private void DisplaySimpleBossList(List<BossData> allBosses)
    {
        if (bossListText == null) return;
        
        string text = "=== BOSS TOWER ===\n(Climb from bottom to top)\n\n";
        
        int availableIndex = 0;
        
        // Display all bosses in unlock order (bottom to top)
        for (int i = 0; i < allBosses.Count; i++)
        {
            var boss = allBosses[i];
            bool isUnlocked = BossProgressionManager.Instance.IsBossUnlocked(boss.bossType);
            bool isDefeated = BossProgressionManager.Instance.IsBossDefeated(boss.bossType);
            
            if (isDefeated)
            {
                text += $"✓ [{boss.bossName}] - DEFEATED\n";
            }
            else if (isUnlocked)
            {
                availableIndex++;
                text += $"\n>>> [{availableIndex}] {boss.bossName} <<<\n";
                text += $"    Health: {boss.maxHealth} | Hands: {boss.handsPerRound}\n";
                text += $"    AVAILABLE - Press {availableIndex} to select\n\n";
            }
            else
            {
                text += $"[{boss.bossName}] - LOCKED\n";
            }
        }
        
        text += "\n=== CONTROLS ===\n";
        text += "Press number keys to select available boss\n";
        text += "Press ENTER to start battle\n";
        
        bossListText.text = text;
    }
    
    /// <summary>
    /// Display all bosses as buttons in tower layout (bottom to top)
    /// </summary>
    private void DisplayBossTower(List<BossData> allBosses)
    {
        // Clear existing buttons
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();
        
        if (bossButtonContainer == null)
        {
            Debug.LogWarning("[BossMapController] Boss button container not assigned, falling back to simple UI");
            DisplaySimpleBossList(allBosses);
            return;
        }
        
        Debug.Log($"[BossMapController] Creating tower with {allBosses.Count} bosses");
        
        // Create buttons for ALL bosses in unlock order
        foreach (var boss in allBosses)
        {
            bool isUnlocked = BossProgressionManager.Instance.IsBossUnlocked(boss.bossType);
            bool isDefeated = BossProgressionManager.Instance.IsBossDefeated(boss.bossType);
            
            GameObject btnObj;
            
            if (bossButtonPrefab != null)
            {
                // Use prefab
                btnObj = Instantiate(bossButtonPrefab, bossButtonContainer);
            }
            else
            {
                // Create simple button
                btnObj = CreateBossTowerButton(boss, isUnlocked, isDefeated);
                btnObj.transform.SetParent(bossButtonContainer, false);
            }
            
            // Configure button based on state
            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                // Get the text component
                var texts = btnObj.GetComponentsInChildren<TextMeshProUGUI>();
                var buttonText = texts.Length > 0 ? texts[0] : null;
                
                if (isDefeated)
                {
                    // Defeated: Dark green, clickable to view info
                    button.interactable = true;
                    
                    var img = button.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        img.color = new Color(0.2f, 0.4f, 0.2f, 0.7f);
                    }
                    
                    if (buttonText != null)
                    {
                        buttonText.text = $"✓ {boss.bossName}\n(DEFEATED)";
                        buttonText.color = new Color(0.5f, 1f, 0.5f, 1f);
                    }
                    
                    // Wire click event
                    var bossData = boss;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBossButtonClicked(bossData));
                }
                else if (isUnlocked)
                {
                    // Available: Blue, highlighted, clickable
                    button.interactable = true;
                    
                    var img = button.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        img.color = new Color(0.3f, 0.5f, 0.8f, 1f);
                    }
                    
                    if (buttonText != null)
                    {
                        buttonText.text = $"{boss.bossName}\nHealth: {boss.maxHealth} | Hands: {boss.handsPerRound}";
                        buttonText.color = Color.white;
                        Debug.Log($"[BossMapController] Set available boss text: {boss.bossName}");
                    }
                    
                    // Wire click event
                    var bossData = boss;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBossButtonClicked(bossData));
                }
                else
                {
                    // Locked: Dark grey, but still clickable to view info
                    button.interactable = true;
                    
                    var img = button.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        img.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                    }
                    
                    if (buttonText != null)
                    {
                        buttonText.text = $"{boss.bossName}\n(LOCKED)";
                        buttonText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    }
                    
                    // Wire click event to show boss info
                    var bossData = boss;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBossButtonClicked(bossData));
                }
                
                Debug.Log($"[BossMapController] Boss button created: {boss.bossName} (Unlocked: {isUnlocked}, Defeated: {isDefeated}, Clickable: {button.interactable})");
            }
            
            spawnedButtons.Add(btnObj);
        }
        
        Debug.Log($"[BossMapController] Tower created with {spawnedButtons.Count} boss buttons");
        
        // Hide the text list when using buttons
        if (bossListText != null)
        {
            bossListText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Create a boss tower button (shows status: locked/available/defeated)
    /// </summary>
    private GameObject CreateBossTowerButton(BossData boss, bool isUnlocked, bool isDefeated)
    {
        // Create button container
        GameObject btnObj = new GameObject($"BossButton_{boss.bossName}");
        
        // Add RectTransform
        var rectTransform = btnObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(700, 140);
        
        // Add Image background
        var image = btnObj.AddComponent<UnityEngine.UI.Image>();
        
        // Set initial color based on state
        if (isDefeated)
        {
            image.color = new Color(0.2f, 0.4f, 0.2f, 0.7f); // Dark green
        }
        else if (isUnlocked)
        {
            image.color = new Color(0.3f, 0.5f, 0.8f, 1f); // Blue
        }
        else
        {
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Very dark (locked)
        }
        
        // Add Button component
        var button = btnObj.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(
            Mathf.Min(image.color.r + 0.2f, 1f),
            Mathf.Min(image.color.g + 0.2f, 1f),
            Mathf.Min(image.color.b + 0.2f, 1f),
            1f
        );
        colors.pressedColor = new Color(
            Mathf.Min(image.color.r + 0.3f, 1f),
            Mathf.Min(image.color.g + 0.3f, 1f),
            Mathf.Min(image.color.b + 0.3f, 1f),
            1f
        );
        button.colors = colors;
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        
        // Set text based on state - ENSURE boss name is always set
        if (isDefeated)
        {
            text.text = $"✓ {boss.bossName}\n(DEFEATED)";
            text.color = new Color(0.5f, 1f, 0.5f, 1f);
        }
        else if (isUnlocked)
        {
            // Make sure this ALWAYS gets set for unlocked bosses
            text.text = $"{boss.bossName}\nHealth: {boss.maxHealth} | Hands: {boss.handsPerRound}";
            text.color = Color.white;
            Debug.Log($"[BossMapController] Created button text for {boss.bossName}: {text.text}");
        }
        else
        {
            text.text = $"{boss.bossName}\n(LOCKED)";
            text.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
        
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        
        Debug.Log($"[BossMapController] Created tower button for {boss.bossName} (Unlocked: {isUnlocked}, Defeated: {isDefeated})");
        return btnObj;
    }
    
    /// <summary>
    /// Handle boss button click - toggle or update panel
    /// </summary>
    private void OnBossButtonClicked(BossData boss)
    {
        if (useSimpleUI)
        {
            // Simple UI mode: directly start battle (only for unlocked)
            bool isUnlocked = BossProgressionManager.Instance.IsBossUnlocked(boss.bossType);
            bool isDefeated = BossProgressionManager.Instance.IsBossDefeated(boss.bossType);
            
            if (isUnlocked && !isDefeated)
            {
                SelectBoss(boss);
                StartBattle();
            }
        }
        else
        {
            // Full UI mode: show/toggle details panel
            if (bossDetailsPanel != null)
            {
                // Check if clicking same boss (toggle) or different boss (update)
                if (bossDetailsPanel.IsVisible() && bossDetailsPanel.GetCurrentBoss() == boss)
                {
                    // Same boss clicked - close panel
                    bossDetailsPanel.HidePanel();
                    Debug.Log($"[BossMapController] Closing details panel for {boss.bossName}");
                }
                else
                {
                    // Different boss or panel closed - show/update panel
                    bossDetailsPanel.ShowBossDetails(boss);
                    Debug.Log($"[BossMapController] Showing details panel for {boss.bossName}");
                }
            }
            else
            {
                Debug.LogWarning("[BossMapController] Boss details panel not assigned!");
            }
        }
    }
    
    /// <summary>
    /// Select boss and immediately start battle (for keyboard shortcuts)
    /// </summary>
    private void SelectAndStartBoss(BossData boss)
    {
        SelectBoss(boss);
        StartBattle();
    }
    
    /// <summary>
    /// Handle keyboard input for boss selection (simple UI only)
    /// </summary>
    private void Update()
    {
        if (!useSimpleUI) return;
        if (BossProgressionManager.Instance == null) return;
        
        var availableBosses = BossProgressionManager.Instance.GetAvailableBosses();
        if (availableBosses.Count == 0) return;
        
        // Number keys 1-9 to select available boss
        int availableCount = 0;
        foreach (var boss in BossProgressionManager.Instance.allBosses)
        {
            bool isUnlocked = BossProgressionManager.Instance.IsBossUnlocked(boss.bossType);
            bool isDefeated = BossProgressionManager.Instance.IsBossDefeated(boss.bossType);
            
            if (isUnlocked && !isDefeated)
            {
                availableCount++;
                if (availableCount <= 9 && Input.GetKeyDown(KeyCode.Alpha0 + availableCount))
                {
                    SelectBoss(boss);
                    StartBattle();
                    return;
                }
            }
        }
        
        // Enter key to start battle with selected boss
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var selectedBoss = BossProgressionManager.Instance.GetSelectedBoss();
            if (selectedBoss.HasValue)
            {
                StartBattle();
            }
            else if (availableBosses.Count > 0)
            {
                // Auto-select first available boss if none selected
                SelectBoss(availableBosses[0]);
                StartBattle();
            }
        }
    }
    
    /// <summary>
    /// Select a boss for the next battle
    /// </summary>
    public void SelectBoss(BossData boss)
    {
        if (BossProgressionManager.Instance == null)
        {
            Debug.LogError("[BossMapController] BossProgressionManager not found!");
            return;
        }
        
        Debug.Log($"[BossMapController] Boss selected: {boss.bossName}");
        BossProgressionManager.Instance.SelectBoss(boss.bossType);
        
        // Update the boss panel to show the selected boss
        NewBossPanel bossPanel = FindObjectOfType<NewBossPanel>();
        if (bossPanel != null)
        {
            bossPanel.UpdateHealthBar(); // This will trigger UpdateBossDisplay
            Debug.Log($"[BossMapController] Updated boss panel for {boss.bossName}");
        }
        
        if (bossListText != null)
        {
            bossListText.text += $"\n\n>>> SELECTED: {boss.bossName} <<<\nPress ENTER to start battle";
        }
    }
    
    /// <summary>
    /// Start battle with selected boss
    /// </summary>
    public void StartBattle()
    {
        if (BossProgressionManager.Instance == null)
        {
            Debug.LogError("[BossMapController] BossProgressionManager not found!");
            return;
        }
        
        var selectedBoss = BossProgressionManager.Instance.GetSelectedBoss();
        
        if (!selectedBoss.HasValue)
        {
            Debug.LogWarning("[BossMapController] No boss selected!");
            return;
        }
        
        Debug.Log($"[BossMapController] Starting battle with {selectedBoss.Value}");
        
        // Load game scene
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadGameScene();
        }
        else
        {
            Debug.LogError("[BossMapController] GameSceneManager not found!");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }
    }
    
    /// <summary>
    /// Return to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("[BossMapController] Returning to Main Menu");
        
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogWarning("[BossMapController] GameSceneManager not found, using direct scene load");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
    
    /// <summary>
    /// Reset progression (for testing)
    /// </summary>
    [ContextMenu("Reset Boss Progression")]
    public void ResetProgression()
    {
        if (BossProgressionManager.Instance != null)
        {
            BossProgressionManager.Instance.ResetProgression();
            RefreshBossList();
            Debug.Log("[BossMapController] Progression reset");
        }
    }
    
    /// <summary>
    /// Debug: Show progression stats
    /// </summary>
    [ContextMenu("Show Progression Stats")]
    public void ShowProgressionStats()
    {
        if (BossProgressionManager.Instance == null)
        {
            Debug.LogError("[BossMapController] BossProgressionManager not found!");
            return;
        }
        
        var stats = BossProgressionManager.Instance.GetProgressionStats();
        Debug.Log($"=== BOSS PROGRESSION STATS ===");
        Debug.Log($"Total Bosses: {stats.totalBosses}");
        Debug.Log($"Unlocked: {stats.unlockedBosses}");
        Debug.Log($"Defeated: {stats.defeatedBosses}");
        Debug.Log($"Total Rewards: {stats.totalRewards}");
        Debug.Log($"Claimed Rewards: {stats.claimedRewards}");
        Debug.Log($"Unclaimed Rewards: {stats.unclaimedRewards}");
        
        var available = BossProgressionManager.Instance.GetAvailableBosses();
        Debug.Log($"Available to fight: {string.Join(", ", available.ConvertAll(b => b.bossName))}");
    }
}

