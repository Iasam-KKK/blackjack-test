using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BossManager : MonoBehaviour
{
    [Header("Boss System")]
    public BossData currentBoss;
    public List<BossData> allBosses = new List<BossData>();
    
    [Header("Boss Progression")]
    public int currentBossHealth;
    public int currentHand = 0;
    public int totalBossesDefeated = 0;
    public BossType currentBossType = BossType.TheDrunkard;
    
    [Header("UI References")]
    public NewBossPanel newBossPanel;
    public BossPreviewPanel bossPreviewPanel;
    public BossIntroPreviewPanel bossIntroPreviewPanel;
    public BossUI bossUI;
    
    [Header("Boss Effects")]
    public AudioSource bossAudioSource;
    public ParticleSystem bossEffectParticles;
    
    [Header("Integration")]
    public Deck deck;
    public ShopManager shopManager;
    
    // Events
    public System.Action<BossData> OnBossDefeated;
    public System.Action<BossData> OnBossHealed;
    public System.Action<BossData> OnBossMechanicTriggered;
    
    // State
    private bool isBossActive = false;
    private List<BossMechanic> activeMechanics = new List<BossMechanic>();
    private Dictionary<BossMechanicType, bool> mechanicStates = new Dictionary<BossMechanicType, bool>();
    
    // The Traitor specific tracking
    private List<GameObject> traitorStolenCards = new List<GameObject>();
    private List<int> traitorStolenCardIndices = new List<int>(); // Track original deck indices for permanent destruction
    
    // The Naughty Child specific tracking
    private List<GameObject> naughtyChildStolenCards = new List<GameObject>();
    private List<TarotCard> naughtyChildStolenTarots = new List<TarotCard>();
    private List<Transform> naughtyChildOriginalTarotSlots = new List<Transform>();
    
    // Singleton pattern
    public static BossManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Find references if not assigned
        if (deck == null) deck = FindObjectOfType<Deck>();
        if (shopManager == null) shopManager = FindObjectOfType<ShopManager>();
        
        // Load boss data if not assigned
        if (allBosses.Count == 0)
        {
            LoadAllBossData();
        }
        
        // Load saved progress
        LoadBossProgress();
        
        // Initialize the appropriate boss based on saved progress
        if (currentBoss == null)
        {
            // Find the boss that should be active based on totalBossesDefeated
            var nextBoss = allBosses.Find(b => b.unlockOrder == totalBossesDefeated);
            
            if (nextBoss != null)
            {
                Debug.Log($"Initializing boss based on progress: {nextBoss.bossName} (unlockOrder: {nextBoss.unlockOrder}, totalDefeated: {totalBossesDefeated})");
                InitializeBoss(nextBoss.bossType);
            }
            else
            {
                Debug.LogWarning($"No boss found for unlockOrder {totalBossesDefeated}, falling back to TheDrunkard");
                InitializeBoss(BossType.TheDrunkard);
            }
        }
    }
    
    /// <summary>
    /// Load all boss ScriptableObjects from the ScriptableObjectsBosses and ScriptableObject folders
    /// </summary>
    private void LoadAllBossData()
    {
        Debug.Log("Loading all boss data from ScriptableObjects folders...");
        
        // Load all BossData ScriptableObjects from ScriptableObjectsBosses folder
        BossData[] bossDataArray1 = Resources.LoadAll<BossData>("ScriptableObjectsBosses");
        
        // Load all BossData ScriptableObjects from ScriptableObject folder
        BossData[] bossDataArray2 = Resources.LoadAll<BossData>("ScriptableObject");
        
        // Combine both arrays
        List<BossData> allBossData = new List<BossData>();
        if (bossDataArray1 != null) allBossData.AddRange(bossDataArray1);
        if (bossDataArray2 != null) allBossData.AddRange(bossDataArray2);
        
        if (allBossData.Count > 0)
        {
            allBosses.AddRange(allBossData);
            Debug.Log($"Loaded {allBosses.Count} boss data files: {string.Join(", ", allBosses.Select(b => b.bossName))}");
            
            // Debug boss portrait assignments
            foreach (var boss in allBosses)
            {
                Debug.Log($"Boss {boss.bossName}: Portrait = {boss.bossPortrait != null} {(boss.bossPortrait != null ? $"({boss.bossPortrait.name})" : "")}");
            }
        }
        else
        {
            Debug.LogWarning("No boss data found in ScriptableObjects folders. Please ensure boss ScriptableObjects are properly placed.");
            
            // Try alternative loading method using AssetDatabase (Editor only)
            #if UNITY_EDITOR
            LoadBossDataFromAssetDatabase();
            #endif
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Alternative loading method using AssetDatabase (Editor only)
    /// </summary>
    private void LoadBossDataFromAssetDatabase()
    {
        Debug.Log("Trying to load boss data using AssetDatabase...");
        
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BossData");
        
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            BossData bossData = UnityEditor.AssetDatabase.LoadAssetAtPath<BossData>(path);
            
            if (bossData != null && !allBosses.Contains(bossData))
            {
                allBosses.Add(bossData);
                Debug.Log($"Loaded boss data: {bossData.bossName} from {path}");
            }
        }
        
        Debug.Log($"Total boss data loaded via AssetDatabase: {allBosses.Count}");
    }
    #endif
    
    /// <summary>
    /// Initialize a specific boss
    /// </summary>
    /*
    public void InitializeBoss(BossType bossType)
    {
        Debug.Log($"Initializing boss: {bossType}");
        Debug.Log($"Available bosses count: {allBosses.Count}");
        
        // Find boss data
        currentBoss = allBosses.Find(b => b.bossType == bossType);
        
        if (currentBoss == null)
        {
            Debug.LogError($"Boss data not found for type: {bossType}");
            Debug.LogError($"Available boss types: {string.Join(", ", allBosses.Select(b => b.bossType))}");
            return;
        }
        
        Debug.Log($"Found boss: {currentBoss.bossName} with description: {currentBoss.bossDescription}");
        
        // Set up boss state
        currentBossType = bossType;
        currentBossHealth = currentBoss.maxHealth;
        currentHand = 0;
        isBossActive = true;
        
        // Load boss mechanics
        LoadBossMechanics();
        
        // Apply boss-specific rules
        ApplyBossRules();
        
        // Update UI
        if (newBossPanel != null)
        {
            Debug.Log("Calling newBossPanel.ShowBossPanel() from InitializeBoss");
            newBossPanel.ShowBossPanel();
        }
        else
        {
            Debug.LogWarning("newBossPanel is null in InitializeBoss");
        }
        
        // Update preview panel
        if (bossPreviewPanel != null)
        {
            bossPreviewPanel.UpdateBossPreview();
        }
        
        Debug.Log($"Fighting {currentBoss.bossName} - Health: {currentBossHealth}/{currentBoss.maxHealth}");
    }
    */
    public void InitializeBoss(BossType bossType)
    {
        Debug.Log($"Initializing boss: {bossType}");
        Debug.Log($"Available bosses count: {allBosses.Count}");
    
        // Find boss data
        currentBoss = allBosses.Find(b => b.bossType == bossType);
    
        if (currentBoss == null)
        {
            Debug.LogError($"Boss data not found for type: {bossType}");
            Debug.LogError($"Available boss types: {string.Join(", ", allBosses.Select(b => b.bossType))}");
            return;
        }
    
        Debug.Log($"Found boss: {currentBoss.bossName} with description: {currentBoss.bossDescription}");
    
        // Set up boss state
        currentBossType = bossType;
        currentBossHealth = currentBoss.maxHealth;
        currentHand = 0;
        isBossActive = true;
    
        // Clear any previous boss-specific tracking
        ClearTraitorTracking();
        ClearNaughtyChildTracking();
        
        // Load boss mechanics
        LoadBossMechanics();
    
        // Apply boss-specific rules
        ApplyBossRules();
    
        // Show dramatic center-screen introduction for the first time
        if (bossUI != null)
        {
            Debug.Log("BossUI found, starting introduction sequence");
            StartCoroutine(ShowBossIntroductionSequence());
        }
        else
        {
            Debug.LogError("BossUI is null! Make sure to assign the BossUI component in the BossManager Inspector.");
        }
        
        // Show boss intro preview panel
        if (bossIntroPreviewPanel != null)
        {
            Debug.Log("BossIntroPreviewPanel found, showing boss intro");
            bossIntroPreviewPanel.ShowBossIntro(currentBoss);
        }
        else
        {
            Debug.LogWarning("BossIntroPreviewPanel is null! Make sure to assign it in the BossManager Inspector.");
        }
        
        // Update UI
        if (newBossPanel != null)
        {
            Debug.Log("Calling newBossPanel.ShowBossPanel() from InitializeBoss");
            newBossPanel.ShowBossPanel();
        }
        else
        {
            Debug.LogWarning("newBossPanel is null in InitializeBoss");
        }
    
        // Update preview panel
        if (bossPreviewPanel != null)
        {
            bossPreviewPanel.UpdateBossPreview();
        }
    
        // ✅ Reactivate Tarot cards for this boss if allowed
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            Debug.Log($"Setting up shop for boss: {currentBoss.bossName} (allowTarotCards: {currentBoss.allowTarotCards})");
            shopManager.SetupShop(); // This will check allowTarotCards
        }
    
        Debug.Log($"Fighting {currentBoss.bossName} - Health: {currentBossHealth}/{currentBoss.maxHealth}");
    }

    /// <summary>
    /// Load boss mechanics and set up their states
    /// </summary>
    private void LoadBossMechanics()
    {
        activeMechanics.Clear();
        mechanicStates.Clear();
        
        foreach (var mechanic in currentBoss.mechanics)
        {
            if (mechanic.mechanicType != BossMechanicType.None)
            {
                activeMechanics.Add(mechanic);
                mechanicStates[mechanic.mechanicType] = mechanic.isPassive;
            }
        }
        
        Debug.Log($"Loaded {activeMechanics.Count} mechanics for {currentBoss.bossName}");
    }
    
    /// <summary>
    /// Apply boss-specific rules to the game
    /// </summary>
    private void ApplyBossRules()
    {
        if (currentBoss.disablesTarotCards)
        {
            DisableTarotCards();
        }
        
        if (currentBoss.usesSpecialDeck)
        {
            ModifyDeckForBoss();
        }
        
        // Special handling for The Captain
        if (currentBoss.bossType == BossType.TheCaptain)
        {
            ApplyCaptainRules();
        }
        
        // Apply passive mechanics
        foreach (var mechanic in activeMechanics.Where(m => m.isPassive))
        {
            ApplyMechanic(mechanic);
        }
    }
    
    /// <summary>
    /// Apply The Captain's special rules
    /// </summary>
    private void ApplyCaptainRules()
    {
        if (deck != null)
        {
            Debug.Log("=== APPLYING CAPTAIN RULES ===");
            
            // Jack nullification is handled in the deck when cards are dealt
            Debug.Log("The Captain's Jack nullification will be applied when cards are dealt");
            
            Debug.Log("=== CAPTAIN RULES APPLIED ===");
        }
        else
        {
            Debug.LogError("Deck is null in ApplyCaptainRules!");
        }
    }
    
    /// <summary>
    /// Called when player wins a hand
    /// </summary>
    public void OnPlayerWin()
    {
        if (!isBossActive) return;
        
        currentBossHealth--;
        currentHand++;
        
        Debug.Log($"Boss takes damage! Health: {currentBossHealth}/{currentBoss.maxHealth}");
        
        // Update the health bar display
        if (newBossPanel != null)
        {
            newBossPanel.UpdateHealthBar();
            newBossPanel.ShakePanel();
        }
        
        // Clear The Traitor's stolen cards since player won
        if (currentBoss != null && currentBoss.bossType == BossType.TheTraitor && traitorStolenCards.Count > 0)
        {
            Debug.Log("Player wins against The Traitor - stolen cards are safe!");
            traitorStolenCards.Clear();
            traitorStolenCardIndices.Clear();
        }
        
        // Trigger mechanics that activate on round end
        TriggerMechanicsOnRoundEnd(true);
        
        if (currentBossHealth <= 0)
        {
            DefeatBoss();
        }
        // Removed the BossHeals() call - no more rounds, just a single level
    }
    
    /// <summary>
    /// Called when player loses a hand
    /// </summary>
    public void OnPlayerLose()
    {
        if (!isBossActive) return;
        
        currentHand++;
        
        // Trigger mechanics that activate on round end
        TriggerMechanicsOnRoundEnd(false);
        
        // Handle The Traitor's permanent card destruction
        if (currentBoss != null && currentBoss.bossType == BossType.TheTraitor && traitorStolenCards.Count > 0)
        {
            Debug.Log($"The Traitor wins! Permanently destroying {traitorStolenCards.Count} stolen cards!");
            
            // Mark cards for permanent destruction
            foreach (int cardIndex in traitorStolenCardIndices)
            {
                MarkCardAsDestroyed(cardIndex);
            }
            
            // Show destruction effect
            StartCoroutine(AnimateTraitorCardDestruction());
            
            // Clear the tracking lists for next hand
            traitorStolenCards.Clear();
            traitorStolenCardIndices.Clear();
        }
        
        // Some bosses might heal on player loss
        if (currentBoss.HasMechanic(BossMechanicType.ModifyBet))
        {
            // The Insatiable heals when player loses
            var mechanic = currentBoss.GetMechanic(BossMechanicType.ModifyBet);
            if (mechanic != null && mechanic.mechanicValue > 0)
            {
                currentBossHealth = Mathf.Min(currentBossHealth + 1, currentBoss.maxHealth);
                Debug.Log($"Boss heals on player loss! Health: {currentBossHealth}/{currentBoss.maxHealth}");
                
                // Update the health bar display
                if (newBossPanel != null)
                {
                    newBossPanel.UpdateHealthBar();
                }
            }
        }
        
        // Removed the BossHeals() call - no more rounds, just a single level
        
        if (newBossPanel != null)
        {
            newBossPanel.ShakePanel();
        }
    }
    
    /// <summary>
    /// Called when a card is dealt
    /// </summary>
    public void OnCardDealt(GameObject card, bool isPlayer)
    {
        if (!isBossActive) return;
        
        // Debug: Log current boss and card being dealt
        string playerType = isPlayer ? "player" : "dealer";
        Debug.Log($"OnCardDealt: {playerType} card dealt, Current boss: {currentBoss?.bossName ?? "None"}");
        
        // Debug: Log active mechanics count
        Debug.Log($"Active mechanics count: {activeMechanics.Count}");
        foreach (var mechanic in activeMechanics)
        {
            Debug.Log($"Active mechanic: {mechanic.mechanicName} (type: {mechanic.mechanicType}, triggers on card dealt: {mechanic.triggersOnCardDealt})");
        }
        
        // Special handling for The Captain - nullify Jacks in player's hand
        if (currentBoss != null && currentBoss.bossType == BossType.TheCaptain && isPlayer)
        {
            if (deck != null)
            {
                deck.ApplyCaptainJackNullification();
            }
        }
         
        foreach (var mechanic in activeMechanics.Where(m => m.triggersOnCardDealt))
        {
            Debug.Log($"Checking mechanic: {mechanic.mechanicName} (type: {mechanic.mechanicType}) for {playerType}");
            
            if (Random.Range(0f, 1f) < mechanic.activationChance || mechanic.activationChance >= 1.0f)
            {
                Debug.Log($"Applying mechanic: {mechanic.mechanicName}");
                
                // Special handling for PeekNextCards mechanic
                if (mechanic.mechanicType == BossMechanicType.PeekNextCards)
                {
                    ApplyMechanic(mechanic);
                }
                else
                {
                    ApplyMechanicToCard(mechanic, card, isPlayer);
                }
            }
            else
            {
                Debug.Log($"Mechanic {mechanic.mechanicName} did not trigger (chance: {mechanic.activationChance})");
            }
        }
    }
    
    /// <summary>
    /// Called when player takes an action (hit/stand)
    /// </summary>
    public void OnPlayerAction()
    {
        if (!isBossActive) return;
        
        // Trigger mechanics that activate on player action
        foreach (var mechanic in activeMechanics.Where(m => m.triggersOnPlayerAction))
        {
            if (Random.Range(0f, 1f) <= mechanic.activationChance)
            {
                ApplyMechanic(mechanic);
            }
        }
    }
    
    /// <summary>
    /// Boss heals after handsPerRound hands (DEPRECATED - no more rounds)
    /// </summary>
    private void BossHeals()
    {
        // This method is deprecated since we removed the rounds system
        // Bosses no longer heal - it's a single level until defeat
        Debug.Log("BossHeals called but rounds system is disabled");
    }
    
    /// <summary>
    /// Defeat the current boss
    /// </summary>
    private void DefeatBoss()
    {
        Debug.Log($"Boss {currentBoss.bossName} defeated!");
        
        isBossActive = false;
        totalBossesDefeated++;
        
        // Grant rewards
        GrantBossRewards();
        
        // Save progress
        SaveBossProgress();
        
        // Trigger event
        OnBossDefeated?.Invoke(currentBoss);
        
        // Show defeat effect
        if (newBossPanel != null)
        {
            newBossPanel.ShowDefeatEffect();
        }
        StartCoroutine(ShowBossDefeatEffect());
        
        // Show boss transition to next boss
        StartCoroutine(ShowBossTransition());
    }
    
    /// <summary>
    /// Grant rewards for defeating the boss
    /// </summary>
    private void GrantBossRewards()
    {
        foreach (var reward in currentBoss.rewards)
        {
            if (reward.grantsTarotCard)
            {
                GrantTarotCard(reward.tarotCardType);
            }
            
            if (reward.grantsPermanentUpgrade)
            {
                GrantPermanentUpgrade(reward.upgradeName, reward.upgradeValue);
            }
            
            if (reward.grantsBonusBalance)
            {
                GrantBonusBalance(reward.bonusAmount);
            }
        }
    }
    
    /// <summary>
    /// Grant a tarot card as reward
    /// </summary>
    private void GrantTarotCard(TarotCardType cardType)
    {
        if (shopManager != null)
        {
            // Find the tarot card data
            var cardData = shopManager.availableTarotCards.Find(c => c.cardType == cardType);
            if (cardData != null)
            {
                shopManager.GiveSpecificTarotCard(cardData);
                Debug.Log($"Granted tarot card: {cardData.cardName}");
            }
        }
    }
    
    /// <summary>
    /// Grant a permanent upgrade
    /// </summary>
    private void GrantPermanentUpgrade(string upgradeName, int upgradeValue)
    {
        // Store upgrade in PlayerPrefs
        PlayerPrefs.SetInt($"Upgrade_{upgradeName}", upgradeValue);
        PlayerPrefs.Save();
        
        Debug.Log($"Granted permanent upgrade: {upgradeName} = {upgradeValue}");
    }
    
    /// <summary>
    /// Grant bonus balance
    /// </summary>
    private void GrantBonusBalance(uint amount)
    {
        if (deck != null)
        {
            deck.Balance += amount;
            Debug.Log($"Granted bonus balance: {amount}");
        }
    }
    
    /// <summary>
    /// Unlock the next boss
    /// </summary>
    private void UnlockNextBoss()
    {
        // Find next boss in unlock order
        var nextBoss = allBosses.Find(b => b.unlockOrder == totalBossesDefeated);
        
        if (nextBoss != null)
        {
            // Wait a moment then initialize next boss
            StartCoroutine(DelayedBossInitialization(nextBoss.bossType));
        }
        else
        {
            Debug.Log("All bosses defeated! Game completed!");
            // Handle game completion
        }
    }
    
    /// <summary>
    /// Show boss transition animation and then initialize next boss
    /// </summary>
    private IEnumerator ShowBossTransition()
    {
        // Wait for defeat animation to complete
        yield return new WaitForSeconds(2f);
        
        Debug.Log($"ShowBossTransition: totalBossesDefeated = {totalBossesDefeated}");
        Debug.Log($"Available bosses: {string.Join(", ", allBosses.Select(b => $"{b.bossName}({b.unlockOrder})"))}");
        
        // Find next boss
        var nextBoss = allBosses.Find(b => b.unlockOrder == totalBossesDefeated);
        
        if (nextBoss != null)
        {
            Debug.Log($"Found next boss: {nextBoss.bossName} (unlockOrder: {nextBoss.unlockOrder})");
            // Show next boss introduction
            yield return StartCoroutine(ShowNextBossIntroduction(nextBoss));
            
            // Initialize the next boss
            InitializeBoss(nextBoss.bossType);
        }
        else
        {
            Debug.LogWarning($"No boss found for unlockOrder {totalBossesDefeated}. All bosses defeated: {string.Join(", ", allBosses.Select(b => b.bossName))}");
            Debug.Log("All bosses defeated! Game completed!");
            // Handle game completion
        }
    }
    
    /// <summary>
    /// Show boss introduction sequence for current boss
    /// </summary>
    private IEnumerator ShowBossIntroductionSequence()
    {
        if (currentBoss != null && bossUI != null)
        {
            bossUI.ShowBossInCenter(currentBoss);
            // Panel will auto-hide after 5 seconds, so we don't need to wait or animate to panel
            yield return new WaitForSeconds(0.1f); // Small delay to ensure the introduction starts
        }
    }
    
    /// <summary>
    /// Show next boss introduction with dramatic effect
    /// </summary>
    private IEnumerator ShowNextBossIntroduction(BossData nextBoss)
    {
        Debug.Log($"Showing next boss introduction: {nextBoss.bossName}");
        
        // Show boss intro preview panel first
        if (bossIntroPreviewPanel != null)
        {
            Debug.Log("BossIntroPreviewPanel found for next boss, showing intro");
            bossIntroPreviewPanel.ShowBossIntro(nextBoss);
        }
        else
        {
            Debug.LogWarning("BossIntroPreviewPanel is null in ShowNextBossIntroduction!");
        }
        
        // Show dramatic center-screen introduction using BossUI
        if (bossUI != null)
        {
            Debug.Log("BossUI found for next boss, showing center introduction");
            bossUI.ShowBossInCenter(nextBoss);
            // Panel will auto-hide after 5 seconds, no need to manually transition
            yield return new WaitForSeconds(0.1f); // Small delay to ensure the introduction starts
        }
        else
        {
            Debug.LogError("BossUI is null in ShowNextBossIntroduction! Make sure to assign the BossUI component in the BossManager Inspector.");
        }
        
        // Show the boss panel with next boss info
        if (newBossPanel != null)
        {
            // Show the next boss introduction with special effects
            newBossPanel.ShowNextBossIntroduction(nextBoss);
            
            // Wait for the introduction to be visible
            yield return new WaitForSeconds(2f);
        }
        
        // Update the preview panel with next boss info
        if (bossPreviewPanel != null)
        {
            bossPreviewPanel.ShowNextBossPreview(nextBoss);
        }
        
        yield return new WaitForSeconds(1f);
    }
    
    /// <summary>
    /// Initialize next boss after a delay
    /// </summary>
    private IEnumerator DelayedBossInitialization(BossType nextBossType)
    {
        yield return new WaitForSeconds(2f); // Wait for defeat animation
        InitializeBoss(nextBossType);
    }
    
    /// <summary>
    /// Apply a mechanic to the game
    /// </summary>
    private void ApplyMechanic(BossMechanic mechanic)
    {
        switch (mechanic.mechanicType)
        {
            case BossMechanicType.StealCards:
                StealPlayerCards(mechanic);
                break;
            case BossMechanicType.DestroyCards:
                DestroyTableCards(mechanic);
                break;
            case BossMechanicType.ModifyBet:
                ModifyBetAmount(mechanic);
                break;
            case BossMechanicType.ChiromancerBetting:
                ApplyChiromancerBetting(mechanic);
                break;
            case BossMechanicType.CardValueManipulation:
                ManipulateCardValues(mechanic);
                break;
            case BossMechanicType.FaceDownCards:
                PlayCardsFaceDown(mechanic);
                break;
            case BossMechanicType.DisableTarot:
                DisableTarotCards();
                break;
            case BossMechanicType.TemporaryTheft:
                TemporarilyStealCards(mechanic);
                break;
            case BossMechanicType.MultiplierEffect:
                ApplyMultiplierEffect(mechanic);
                break;
            case BossMechanicType.WinStreakEffect:
                ApplyWinStreakEffect(mechanic);
                break;
            case BossMechanicType.PeekNextCards:
                PeekNextCards(mechanic);
                break;
            case BossMechanicType.JackNullification:
                ApplyJackNullification(mechanic);
                break;
            case BossMechanicType.StealLaidOutCards:
                StealLaidOutCards(mechanic);
                break;
            case BossMechanicType.PermanentDestruction:
                // This is handled in EndHand when boss wins
                break;
            case BossMechanicType.HideConsumables:
                HideConsumables();
                break;
            case BossMechanicType.TemporaryCardTheft:
                TemporaryCardTheft(mechanic);
                break;
            case BossMechanicType.DiplomaticKing:
                // This is handled in ApplyMechanicToCard when a King is dealt
                break;
            case BossMechanicType.SeductressIntercept:
                // This is handled in ApplyMechanicToCard when K/J is dealt
                break;
            case BossMechanicType.PermanentlystealsallQueensplayed:
                StealQueens(mechanic);
                Debug.Log("The Degenerate mechanic applied");
                break;

        }
        
        OnBossMechanicTriggered?.Invoke(currentBoss);
    }
    
    /// <summary>
    /// Apply mechanic to a specific card
    /// </summary>
    private void ApplyMechanicToCard(BossMechanic mechanic, GameObject card, bool isPlayer)
    {
        switch (mechanic.mechanicType)
        {
            case BossMechanicType.CardValueManipulation:
                ManipulateCardValue(card, mechanic);
                break;
            case BossMechanicType.FaceDownCards:
                PlayCardFaceDown(card, mechanic);
                break;
            case BossMechanicType.DiplomaticKing:
                if (!isPlayer) // Only apply to dealer's cards
                {
                    ApplyDiplomaticKing(card, mechanic);
                }
                break;
            case BossMechanicType.SeductressIntercept:
                ApplySeductressIntercept(card, mechanic, isPlayer);
                break;
            case BossMechanicType.CorruptCard:
                ApplyCorruptCard(card, mechanic);
                break;
        }
    }
    
    /// <summary>
    /// Trigger mechanics that activate on round end
    /// </summary>
    private void TriggerMechanicsOnRoundEnd(bool playerWon)
    {
        foreach (var mechanic in activeMechanics.Where(m => m.triggersOnRoundEnd))
        {
            if (Random.Range(0f, 1f) <= mechanic.activationChance)
            {
                ApplyMechanic(mechanic);
            }
        }
    }
    
    // Specific mechanic implementations
    private void StealPlayerCards(BossMechanic mechanic)
    {
        if (deck != null && deck.player != null)
        {
            var playerHand = deck.player.GetComponent<CardHand>();
            var dealerHand = deck.dealer.GetComponent<CardHand>();
            
            if (playerHand != null && dealerHand != null && playerHand.cards.Count > 0)
            {
                int cardsToSteal = Mathf.Min(mechanic.mechanicValue, playerHand.cards.Count);
                int cardsStolen = 0;
                
                for (int i = 0; i < cardsToSteal; i++)
                {
                    // Get safe cards that won't cause dealer to bust
                    List<GameObject> safeCards = GetSafeCardsToSteal(playerHand, dealerHand);
                    
                    GameObject cardToSteal = null;
                    
                    if (safeCards.Count > 0)
                    {
                        // Strategy: If dealer is close to 21, be conservative; otherwise be greedy
                        int dealerPoints = dealerHand.points;
                        
                        if (dealerPoints >= 17)
                        {
                            // Conservative - steal lowest value card
                            cardToSteal = GetLowestValueCardToSteal(safeCards);
                            Debug.Log("The Thief plays conservatively (dealer has 17+)");
                        }
                        else
                        {
                            // Aggressive - steal highest value card that's safe
                            cardToSteal = GetBestCardToSteal(safeCards);
                            Debug.Log("The Thief plays aggressively (dealer under 17)");
                        }
                    }
                    
                    if (cardToSteal != null)
                    {
                        // Remove from player's hand
                        int cardIndex = playerHand.cards.IndexOf(cardToSteal);
                        playerHand.cards.RemoveAt(cardIndex);
                        
                        // Add to dealer's hand
                        dealerHand.cards.Add(cardToSteal);
                        
                        // Move card to dealer's transform
                        cardToSteal.transform.SetParent(dealerHand.transform);
                        
                        // Log the theft
                        CardModel cardModel = cardToSteal.GetComponent<CardModel>();
                        if (cardModel != null)
                        {
                            Debug.Log($"The Thief steals: {deck.GetCardInfoFromModel(cardModel).cardName} (value: {cardModel.value})");
                        }
                        
                        // Animate the theft
                        StartCoroutine(AnimateCardTheft(cardToSteal, false));
                        cardsStolen++;
                    }
                    else
                    {
                        Debug.Log("The Thief found no safe cards to steal - skipping this theft");
                        break; // No more safe cards available
                    }
                }
                
                if (cardsStolen > 0)
                {
                    // Update hand arrangements and points
                    playerHand.ArrangeCardsInWindow();
                    playerHand.UpdatePoints();
                    
                    dealerHand.ArrangeCardsInWindow();
                    dealerHand.UpdatePoints();
                    
                    deck.UpdateScoreDisplays();
                    
                    // Show message
                    if (newBossPanel != null)
                    {
                        newBossPanel.ShowBossMessage($"The Thief steals {cardsStolen} card{(cardsStolen > 1 ? "s" : "")} wisely!");
                        StartCoroutine(HideBossMessageAfterDelay(2f));
                    }
                }
                else
                {
                    Debug.Log("The Thief couldn't steal any cards without busting");
                    if (newBossPanel != null)
                    {
                        newBossPanel.ShowBossMessage("The Thief holds back - no safe cards to steal!");
                        StartCoroutine(HideBossMessageAfterDelay(2f));
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// The Traitor's mechanic - steal laid out cards from player
    /// </summary>
    private void StealLaidOutCards(BossMechanic mechanic)
    {
        if (deck != null && deck.player != null)
        {
            var playerHand = deck.player.GetComponent<CardHand>();
            var dealerHand = deck.dealer.GetComponent<CardHand>();
            
            if (playerHand != null && dealerHand != null && playerHand.cards.Count > 0)
            {
                // Only steal if player has more than 2 cards (to avoid making it impossible to play)
                if (playerHand.cards.Count <= 2)
                {
                    Debug.Log("The Traitor: Not stealing - player has too few cards");
                    return;
                }
                
                int cardsToSteal = Mathf.Min(mechanic.mechanicValue, playerHand.cards.Count - 2);
                cardsToSteal = Mathf.Max(1, cardsToSteal); // At least steal 1 card
                int cardsStolen = 0;
                
                for (int i = 0; i < cardsToSteal; i++)
                {
                    // Get safe cards that won't cause dealer to bust
                    List<GameObject> safeCards = GetSafeCardsToSteal(playerHand, dealerHand);
                    
                    GameObject cardToSteal = null;
                    
                    if (safeCards.Count > 0)
                    {
                        // The Traitor is more aggressive than The Thief - always tries for higher value cards
                        int dealerPoints = dealerHand.points;
                        
                        if (dealerPoints >= 19)
                        {
                            // Very conservative - steal lowest value card
                            cardToSteal = GetLowestValueCardToSteal(safeCards);
                            Debug.Log("The Traitor plays very conservatively (dealer has 19+)");
                        }
                        else if (dealerPoints >= 16)
                        {
                            // Somewhat conservative - random safe card
                            cardToSteal = safeCards[Random.Range(0, safeCards.Count)];
                            Debug.Log("The Traitor plays moderately (dealer has 16-18)");
                        }
                        else
                        {
                            // Aggressive - steal highest value card that's safe
                            cardToSteal = GetBestCardToSteal(safeCards);
                            Debug.Log("The Traitor plays aggressively (dealer under 16)");
                        }
                    }
                    
                    if (cardToSteal != null)
                    {
                        // Track the stolen card for potential permanent destruction
                        traitorStolenCards.Add(cardToSteal);
                        
                        // Get the original deck index before stealing
                        CardModel cardModel = cardToSteal.GetComponent<CardModel>();
                        if (cardModel != null)
                        {
                            traitorStolenCardIndices.Add(cardModel.originalDeckIndex);
                            Debug.Log($"The Traitor steals: {deck.GetCardInfoFromModel(cardModel).cardName} (Original index: {cardModel.originalDeckIndex}, value: {cardModel.value})");
                        }
                        
                        // Remove from player's hand
                        int cardIndex = playerHand.cards.IndexOf(cardToSteal);
                        playerHand.cards.RemoveAt(cardIndex);
                        
                        // Add to dealer's hand
                        dealerHand.cards.Add(cardToSteal);
                        
                        // Move card to dealer's transform
                        cardToSteal.transform.SetParent(dealerHand.transform);
                        
                        // Animate the theft with special Traitor effect
                        StartCoroutine(AnimateCardTheft(cardToSteal, true));
                        cardsStolen++;
                    }
                    else
                    {
                        Debug.Log("The Traitor found no safe cards to steal - skipping this theft");
                        break; // No more safe cards available
                    }
                }
                
                if (cardsStolen > 0)
                {
                    // Update hand arrangements and points
                    playerHand.ArrangeCardsInWindow();
                    playerHand.UpdatePoints();
                    
                    dealerHand.ArrangeCardsInWindow();
                    dealerHand.UpdatePoints();
                    
                    deck.UpdateScoreDisplays();
                    
                    // Show message to player
                    if (newBossPanel != null)
                    {
                        newBossPanel.ShowBossMessage($"The Traitor cunningly steals {cardsStolen} card{(cardsStolen > 1 ? "s" : "")}!");
                        StartCoroutine(HideBossMessageAfterDelay(2f));
                    }
                }
                else
                {
                    Debug.Log("The Traitor couldn't steal any cards without busting");
                    if (newBossPanel != null)
                    {
                        newBossPanel.ShowBossMessage("The Traitor hesitates - no safe cards to steal!");
                        StartCoroutine(HideBossMessageAfterDelay(2f));
                    }
                }
            }
        }
    }
    private void StealQueens(BossMechanic mechanic)
    {
        if (deck == null || deck.player == null) return;

        var playerHand = deck.player.GetComponent<CardHand>();
        var dealerHand = deck.dealer.GetComponent<CardHand>();
        if (playerHand == null || dealerHand == null) return;

        // Collect all Queens from player's hand
        List<GameObject> queensToSteal = new List<GameObject>();
        foreach (var cardObj in playerHand.cards)
        {
            CardModel cardModel = cardObj.GetComponent<CardModel>();
            if (cardModel != null)
            {
                CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
                if (cardInfo.suitIndex == 11) // Queen
                {
                    queensToSteal.Add(cardObj);
                }
            }
        }

        if (queensToSteal.Count == 0)
        {
            Debug.Log("The Degenerate: No Queens to steal.");
            return;
        }

        // Steal all Queens immediately
        foreach (var queen in queensToSteal)
        {
            // Remove from player's hand
            playerHand.cards.Remove(queen);

            // Add to dealer's hand
            dealerHand.cards.Add(queen);
            queen.transform.SetParent(dealerHand.transform);

            // Animate the theft
            StartCoroutine(AnimateCardTheft(queen, true));

            // Debug log
            CardModel cardModel = queen.GetComponent<CardModel>();
            CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
            Debug.Log($"The Degenerate steals: {cardInfo.cardName}");
        }

        // Update hands and points
        playerHand.ArrangeCardsInWindow();
        playerHand.UpdatePoints();
        dealerHand.ArrangeCardsInWindow();
        dealerHand.UpdatePoints();
        deck.UpdateScoreDisplays();

        // Show boss message
        if (newBossPanel != null)
        {
            newBossPanel.ShowBossMessage($"The Degenerate steals {queensToSteal.Count} Queen{(queensToSteal.Count > 1 ? "s" : "")} from your hand!");
            StartCoroutine(HideBossMessageAfterDelay(2f));
        }
    }
    private void DestroyTableCards(BossMechanic mechanic)
    {
        // Implementation for destroying cards on the table
        Debug.Log("Destroying table cards...");
    }
    private void ModifyBetAmount(BossMechanic mechanic)
    {
        if (deck != null)
        {
            uint originalBet = deck._bet;
            uint modifiedBet = (uint)(originalBet * mechanic.mechanicMultiplier);
            deck._bet = modifiedBet;
            
            Debug.Log($"Bet modified from {originalBet} to {modifiedBet}");
        }
    }
    
    /// <summary>
    /// The Chiromancer's special betting mechanic - takes 1.5x bet normally, 2x bet when winning with 21
    /// </summary>
    private void ApplyChiromancerBetting(BossMechanic mechanic)
    {
        // This mechanic is handled in the Deck.cs EndHand method
        // The actual betting logic is applied when the dealer wins
        Debug.Log("Chiromancer betting mechanic is active - special loss calculations will be applied");
    }
    
    private void ManipulateCardValues(BossMechanic mechanic)
    {
        // Implementation for manipulating card values
        Debug.Log("Manipulating card values...");
    }
    
    private void ManipulateCardValue(GameObject card, BossMechanic mechanic)
    {
        var cardModel = card.GetComponent<CardModel>();
        if (cardModel != null)
        {
            int originalValue = cardModel.value;
            int newValue = Mathf.Clamp(originalValue + mechanic.mechanicValue, 1, 10);
            cardModel.value = newValue;
            
            Debug.Log($"Card value changed from {originalValue} to {newValue}");
        }
    }
    
    private void PlayCardsFaceDown(BossMechanic mechanic)
    {
        // Implementation for playing cards face down
        Debug.Log("Playing cards face down...");
    }
    
    private void PlayCardFaceDown(GameObject card, BossMechanic mechanic)
    {
        var cardModel = card.GetComponent<CardModel>();
        if (cardModel != null)
        {
            // Flip card to back
            var spriteRenderer = card.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = cardModel.cardBack;
            }
        }
    }
    
    private void DisableTarotCards()
    {
        if (shopManager != null && shopManager.tarotPanel != null)
        {
            var tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
            foreach (var card in tarotCards)
            {
                card.enabled = false;
                card.GetComponent<Image>().color = Color.gray;
            }
        }
    }
    
    private void TemporarilyStealCards(BossMechanic mechanic)
    {
        // Implementation for temporarily stealing cards
        Debug.Log("Temporarily stealing cards...");
    }
    
    private void ApplyMultiplierEffect(BossMechanic mechanic)
    {
        // Implementation for multiplier effects
        Debug.Log("Applying multiplier effect...");
    }
    
    private void ApplyWinStreakEffect(BossMechanic mechanic)
    {
        // Implementation for win streak effects
        Debug.Log("Applying win streak effect...");
    }
    
    /// <summary>
    /// The Forgetful Seer mechanic - Peek at next 2 cards and randomly act upon them
    /// </summary>
    private void PeekNextCards(BossMechanic mechanic)
    {
        if (deck == null) return;
        
        Debug.Log("The Forgetful Seer is peeking at the next 2 cards...");
        
        // Get the next 2 cards from the deck
        List<CardInfo> nextCards = new List<CardInfo>();
        for (int i = 0; i < 2 && (deck.CardIndex + i) < deck.values.Length; i++)
        {
            int cardIndex = deck.CardIndex + i;
            CardInfo cardInfo = deck.GetCardInfo(cardIndex);
            nextCards.Add(cardInfo);
        }
        
        if (nextCards.Count == 0)
        {
            Debug.Log("No cards left to peek at!");
            return;
        }
        
        // Log what the Forgetful Seer sees
        string cardsSeen = string.Join(", ", nextCards.Select(c => c.cardName));
        Debug.Log($"The Forgetful Seer sees: {cardsSeen}");
        
        // Show visual feedback to the player
        StartCoroutine(ShowForgetfulSeerEffect(cardsSeen));
        
        // Randomly decide what action to take based on the cards seen
        float randomAction = Random.Range(0f, 1f);
        
        if (randomAction < 0.4f) // 40% chance to steal a card
        {
            StealNextCard(mechanic, nextCards);
        }
        else if (randomAction < 0.7f) // 30% chance to modify card values
        {
            ModifyNextCardValues(mechanic, nextCards);
        }
        else if (randomAction < 0.9f) // 20% chance to swap card positions
        {
            SwapNextCardPositions(mechanic, nextCards);
        }
        else // 10% chance to do nothing (forgetful behavior)
        {
            Debug.Log("The Forgetful Seer forgot what they saw and does nothing!");
        }
    }
    
    /// <summary>
    /// Steal one of the next cards (remove it from deck)
    /// </summary>
    private void StealNextCard(BossMechanic mechanic, List<CardInfo> nextCards)
    {
        if (nextCards.Count == 0) return;
        
        // Choose a random card to steal
        int cardToSteal = Random.Range(0, nextCards.Count);
        CardInfo stolenCard = nextCards[cardToSteal];
        
        Debug.Log($"The Forgetful Seer steals: {stolenCard.cardName}");
        
        // Remove the card from the deck
        int deckPosition = deck.CardIndex + cardToSteal;
        if (deckPosition < deck.values.Length)
        {
            // Shift all cards after the stolen position forward
            for (int i = deckPosition; i < deck.values.Length - 1; i++)
            {
                deck.values[i] = deck.values[i + 1];
                deck.faces[i] = deck.faces[i + 1];
                deck.originalIndices[i] = deck.originalIndices[i + 1];
            }
            
            // Resize arrays
            System.Array.Resize(ref deck.values, deck.values.Length - 1);
            System.Array.Resize(ref deck.faces, deck.faces.Length - 1);
            System.Array.Resize(ref deck.originalIndices, deck.originalIndices.Length - 1);
            
            Debug.Log($"Card {stolenCard.cardName} removed from deck");
        }
    }
    
    /// <summary>
    /// Modify the values of the next cards
    /// </summary>
    private void ModifyNextCardValues(BossMechanic mechanic, List<CardInfo> nextCards)
    {
        Debug.Log("The Forgetful Seer modifies the next card values...");
        
        for (int i = 0; i < nextCards.Count; i++)
        {
            int deckPosition = deck.CardIndex + i;
            if (deckPosition < deck.values.Length)
            {
                int originalValue = deck.values[deckPosition];
                int newValue = Mathf.Clamp(originalValue + mechanic.mechanicValue, 1, 10);
                deck.values[deckPosition] = newValue;
                
                Debug.Log($"Modified card {nextCards[i].cardName} value from {originalValue} to {newValue}");
            }
        }
    }
    
    /// <summary>
    /// Swap the positions of the next two cards
    /// </summary>
    private void SwapNextCardPositions(BossMechanic mechanic, List<CardInfo> nextCards)
    {
        if (nextCards.Count < 2) return;
        
        Debug.Log("The Forgetful Seer swaps the next two card positions...");
        
        int firstPos = deck.CardIndex;
        int secondPos = deck.CardIndex + 1;
        
        if (secondPos < deck.values.Length)
        {
            // Swap values
            int tempValue = deck.values[firstPos];
            deck.values[firstPos] = deck.values[secondPos];
            deck.values[secondPos] = tempValue;
            
            // Swap faces
            Sprite tempFace = deck.faces[firstPos];
            deck.faces[firstPos] = deck.faces[secondPos];
            deck.faces[secondPos] = tempFace;
            
            // Swap original indices
            int tempIndex = deck.originalIndices[firstPos];
            deck.originalIndices[firstPos] = deck.originalIndices[secondPos];
            deck.originalIndices[secondPos] = tempIndex;
            
            Debug.Log($"Swapped {nextCards[0].cardName} and {nextCards[1].cardName} positions");
        }
    }
    
    /// <summary>
    /// Apply Jack nullification mechanic (The Captain)
    /// </summary>
    private void ApplyJackNullification(BossMechanic mechanic)
    {
        if (deck != null)
        {
            deck.ApplyCaptainJackNullification();
            Debug.Log("The Captain's Jack nullification mechanic applied");
        }
    }
    
    /// <summary>
    /// Apply The Diplomat's King mechanic
    /// </summary>
    private void ApplyDiplomaticKing(GameObject card, BossMechanic mechanic)
    {
        if (card == null || deck == null) return;
        
        CardModel cardModel = card.GetComponent<CardModel>();
        if (cardModel == null) return;
        
        // Check if this is a King
        CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
        Debug.Log($"The Diplomat card dealt: {cardInfo.cardName} (suitIndex: {cardInfo.suitIndex}, value: {cardModel.value})");
        
        if (cardInfo.suitIndex == 12) // King is at index 12 in each suit
        {
            Debug.Log($"The Diplomat plays a King: {cardInfo.cardName} - ACTIVATING DIPLOMATIC KING MECHANIC!");
            
            // Wait a frame to ensure all cards are properly dealt and arranged
            StartCoroutine(ApplyDiplomaticKingDelayed(cardModel));
        }
        else
        {
            Debug.Log($"Card is not a King (suitIndex: {cardInfo.suitIndex}). Jack=10, Queen=11, King=12");
        }
    }
    
    /// <summary>
    /// Apply The Diplomat's King effect after a short delay to ensure proper calculation
    /// </summary>
    private IEnumerator ApplyDiplomaticKingDelayed(CardModel kingCard)
    {
        // Wait for the card to be properly added to the hand
        yield return new WaitForEndOfFrame();
        
        if (deck == null || deck.dealer == null) yield break;
        
        CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
        if (dealerHand == null) yield break;
        
        // Check if the King has been nullified (e.g., by The Captain or other effects)
        if (kingCard.value == 0)
        {
            Debug.Log("The Diplomat's King has been nullified by another effect!");
            if (newBossPanel != null)
            {
                newBossPanel.ShowBossMessage("The Diplomat's King was nullified!");
                StartCoroutine(HideBossMessageAfterDelay(2f));
            }
            yield break;
        }
        
        // Calculate current dealer points
        int currentPoints = dealerHand.points;
        int playerPoints = deck.GetPlayerPoints();
        
        Debug.Log($"Diplomat's current points: {currentPoints}, Player's points: {playerPoints}");
        
        // Calculate how many points needed to win
        int targetPoints = playerPoints + 1; // Just one more than player to win
        
        // Don't exceed 21 unless player is already over 21
        if (playerPoints <= Constants.Blackjack && targetPoints > Constants.Blackjack)
        {
            targetPoints = Constants.Blackjack; // Cap at 21 if possible
        }
        
        // Calculate the King's value
        int kingValue = targetPoints - (currentPoints - 10); // Subtract 10 because King normally adds 10
        
        // Ensure the value is reasonable (between 1 and 21)
        kingValue = Mathf.Clamp(kingValue, 1, 21);
        
        // Check if applying this value would be blocked by any active effects
        bool canApplyEffect = true;
        
        // For example, if there's a maximum card value restriction active
        if (currentBoss != null && currentBoss.HasMechanic(BossMechanicType.CardValueManipulation))
        {
            var manipMechanic = currentBoss.GetMechanic(BossMechanicType.CardValueManipulation);
            if (manipMechanic != null && manipMechanic.mechanicValue > 0 && kingValue > manipMechanic.mechanicValue)
            {
                kingValue = manipMechanic.mechanicValue;
                Debug.Log($"King value capped by card manipulation effect to {kingValue}");
            }
        }
        
        if (canApplyEffect)
        {
            // Apply the new value to the King
            kingCard.value = kingValue;
            
            Debug.Log($"The Diplomat's King adjusted to value: {kingValue} (Target: {targetPoints})");
            
            // Update the hand points
            dealerHand.UpdatePoints();
            deck.UpdateScoreDisplays();
            
            // Show visual effect
            StartCoroutine(AnimateDiplomaticKing(kingCard.gameObject, kingValue));
            
            // Show message to player
            if (newBossPanel != null)
            {
                newBossPanel.ShowBossMessage($"The Diplomat's King adjusts to {kingValue} points!");
                StartCoroutine(HideBossMessageAfterDelay(3f));
            }
        }
        else
        {
            Debug.Log("The Diplomat's King effect was blocked by another active effect");
            if (newBossPanel != null)
            {
                newBossPanel.ShowBossMessage("The Diplomat's King effect was blocked!");
                StartCoroutine(HideBossMessageAfterDelay(2f));
            }
        }
    }
    
    /// <summary>
    /// Animate The Diplomat's King transformation
    /// </summary>
    private IEnumerator AnimateDiplomaticKing(GameObject kingCard, int newValue)
    {
        if (kingCard == null) yield break;
        
        Sequence kingSequence = DOTween.Sequence();
        
        SpriteRenderer spriteRenderer = kingCard.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Golden glow effect
            kingSequence.Append(spriteRenderer.DOColor(new Color(1f, 0.8f, 0.2f), 0.3f));
            kingSequence.Append(spriteRenderer.DOColor(Color.white, 0.3f));
            kingSequence.Append(spriteRenderer.DOColor(new Color(1f, 0.8f, 0.2f), 0.3f));
            kingSequence.Append(spriteRenderer.DOColor(Color.white, 0.3f));
        }
        
        // Scale pulse effect
        kingSequence.Join(kingCard.transform.DOScale(kingCard.transform.localScale * 1.3f, 0.3f)
            .SetEase(Ease.OutQuad));
        kingSequence.Append(kingCard.transform.DOScale(kingCard.transform.localScale, 0.3f)
            .SetEase(Ease.InQuad));
        
        // Add rotation for dramatic effect
        kingSequence.Join(kingCard.transform.DORotate(new Vector3(0, 0, 360), 0.6f, RotateMode.FastBeyond360));
        
        yield return kingSequence.WaitForCompletion();
    }
    
    /// <summary>
    /// Apply The Seductress's card interception mechanic
    /// </summary>
    private void ApplySeductressIntercept(GameObject card, BossMechanic mechanic, bool isPlayer)
    {
        if (card == null || deck == null) return;
        
        CardModel cardModel = card.GetComponent<CardModel>();
        if (cardModel == null) return;
        
        // Check if this is a King or Jack
        CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
        bool isKingOrJack = (cardInfo.suitIndex == 10 || cardInfo.suitIndex == 12); // Jack=10, King=12
        
        Debug.Log($"The Seductress sees: {cardInfo.cardName} (suitIndex: {cardInfo.suitIndex}) for {(isPlayer ? "player" : "dealer")}");
        
        if (isKingOrJack)
        {
            Debug.Log($"The Seductress intercepts: {cardInfo.cardName}!");
            
            if (isPlayer)
            {
                // This card was intended for the player, but The Seductress intercepts it
                StartCoroutine(InterceptPlayerCard(card, cardInfo));
            }
            else
            {
                // This card was intended for the dealer, apply optimal value
                StartCoroutine(OptimizeSeductressCard(card, cardInfo));
            }
        }
    }
    
    /// <summary>
    /// Intercept a King or Jack that was meant for the player
    /// </summary>
    private IEnumerator InterceptPlayerCard(GameObject card, CardInfo cardInfo)
    {
        // Wait a moment for the card to be properly positioned
        yield return new WaitForEndOfFrame();
        
        if (deck == null || deck.player == null || deck.dealer == null) yield break;
        
        CardHand playerHand = deck.player.GetComponent<CardHand>();
        CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
        
        if (playerHand == null || dealerHand == null) yield break;
        
        // Remove from player's hand if it's there
        if (playerHand.cards.Contains(card))
        {
            playerHand.cards.Remove(card);
            Debug.Log($"Removed {cardInfo.cardName} from player's hand");
        }
        
        // Add to dealer's hand
        dealerHand.cards.Add(card);
        card.transform.SetParent(dealerHand.transform);
        
        // Optimize the card value for The Seductress
        yield return StartCoroutine(OptimizeInterceptedCard(card, cardInfo));
        
        // Update hands
        playerHand.ArrangeCardsInWindow();
        playerHand.UpdatePoints();
        dealerHand.ArrangeCardsInWindow();
        dealerHand.UpdatePoints();
        
        deck.UpdateScoreDisplays();
        
        // Show visual effect and message
        StartCoroutine(AnimateSeductressIntercept(card));
        
        if (newBossPanel != null)
        {
            newBossPanel.ShowBossMessage($"The Seductress seduces your {cardInfo.cardName}!");
            StartCoroutine(HideBossMessageAfterDelay(3f));
        }
    }
    
    /// <summary>
    /// Optimize a King or Jack that was already going to the dealer
    /// </summary>
    private IEnumerator OptimizeSeductressCard(GameObject card, CardInfo cardInfo)
    {
        yield return new WaitForEndOfFrame();
        
        yield return StartCoroutine(OptimizeInterceptedCard(card, cardInfo));
        
        // Show message for optimized card
        if (newBossPanel != null)
        {
            CardModel cardModel = card.GetComponent<CardModel>();
            if (cardModel != null)
            {
                newBossPanel.ShowBossMessage($"The Seductress values her {cardInfo.cardName} as {cardModel.value}!");
                StartCoroutine(HideBossMessageAfterDelay(2f));
            }
        }
    }
    
    /// <summary>
    /// Optimize the value of an intercepted King or Jack
    /// </summary>
    private IEnumerator OptimizeInterceptedCard(GameObject card, CardInfo cardInfo)
    {
        if (deck == null || deck.dealer == null) yield break;
        
        CardModel cardModel = card.GetComponent<CardModel>();
        if (cardModel == null) yield break;
        
        CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
        if (dealerHand == null) yield break;
        
        // Calculate current dealer points (excluding this card)
        int currentPoints = 0;
        foreach (GameObject otherCard in dealerHand.cards)
        {
            if (otherCard != card)
            {
                CardModel otherCardModel = otherCard.GetComponent<CardModel>();
                if (otherCardModel != null)
                {
                    currentPoints += otherCardModel.value;
                }
            }
        }
        
        int playerPoints = deck.GetPlayerPoints();
        
        // Decide optimal value: 10 or 1
        int optimalValue = 10; // Default to 10
        
        // If using 10 would bust, use 1
        if (currentPoints + 10 > Constants.Blackjack)
        {
            optimalValue = 1;
            Debug.Log($"The Seductress chooses value 1 to avoid bust (current: {currentPoints})");
        }
        // If using 10 would give a perfect score (17-21), use it
        else if (currentPoints + 10 >= 17 && currentPoints + 10 <= Constants.Blackjack)
        {
            optimalValue = 10;
            Debug.Log($"The Seductress chooses value 10 for optimal score (current: {currentPoints})");
        }
        // If using 1 would allow drawing more cards safely, and current is low, use 1
        else if (currentPoints < 11 && currentPoints + 1 < 12)
        {
            optimalValue = 1;
            Debug.Log($"The Seductress chooses value 1 to keep options open (current: {currentPoints})");
        }
        // Otherwise, use 10 for maximum advantage
        else
        {
            optimalValue = 10;
            Debug.Log($"The Seductress chooses value 10 for maximum value (current: {currentPoints})");
        }
        
        // Apply the optimal value
        cardModel.value = optimalValue;
        
        Debug.Log($"The Seductress optimized {cardInfo.cardName} to value {optimalValue}");
        
        yield return null;
    }
    
    /// <summary>
    /// Animate The Seductress's card interception
    /// </summary>
    private IEnumerator AnimateSeductressIntercept(GameObject card)
    {
        if (card == null) yield break;
        
        Sequence seductionSequence = DOTween.Sequence();
        
        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Pink/red seductive glow
            seductionSequence.Append(spriteRenderer.DOColor(new Color(1f, 0.4f, 0.7f), 0.3f));
            seductionSequence.Append(spriteRenderer.DOColor(Color.white, 0.3f));
            seductionSequence.Append(spriteRenderer.DOColor(new Color(1f, 0.4f, 0.7f), 0.3f));
            seductionSequence.Append(spriteRenderer.DOColor(Color.white, 0.3f));
        }
        
        // Gentle floating motion
        seductionSequence.Join(card.transform.DOLocalMoveY(card.transform.localPosition.y + 0.5f, 0.6f)
            .SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
        
        // Subtle rotation
        seductionSequence.Join(card.transform.DORotate(new Vector3(0, 0, 10), 0.3f)
            .SetEase(Ease.InOutSine).SetLoops(4, LoopType.Yoyo));
        
        yield return seductionSequence.WaitForCompletion();
    }
    
    /// <summary>
    /// Apply The Corruptor's card corruption mechanic
    /// </summary>
    private void ApplyCorruptCard(GameObject card, BossMechanic mechanic)
    {
        Debug.Log("=== CORRUPTOR: ApplyCorruptCard called ===");
        
        if (card == null) 
        {
            Debug.LogWarning("ApplyCorruptCard: card is null!");
            return;
        }
        
        CardModel cardModel = card.GetComponent<CardModel>();
        if (cardModel == null) 
        {
            Debug.LogWarning("ApplyCorruptCard: CardModel component not found!");
            return;
        }
        
        Debug.Log($"ApplyCorruptCard: Processing card with value {cardModel.value}");
        
        // Get the corruption range from mechanic value (default 3 means +/-1 to +/-3)
        int maxCorruption = mechanic.mechanicValue > 0 ? mechanic.mechanicValue : 3;
        
        // Randomly choose corruption direction and amount
        bool inflate = Random.Range(0f, 1f) < 0.5f; // 50% chance to inflate, 50% to deflate
        int corruptionAmount = Random.Range(1, maxCorruption + 1); // Random amount 1 to maxCorruption
        
        if (!inflate) corruptionAmount = -corruptionAmount; // Make it negative for deflation
        
        int originalValue = cardModel.value;
        int newValue = Mathf.Clamp(originalValue + corruptionAmount, 1, 11); // Keep within valid range
        
        // Only apply corruption if it actually changes the value
        if (newValue != originalValue)
        {
            cardModel.value = newValue;
            
            // Start visual corruption effect
            StartCoroutine(AnimateCardCorruption(card, originalValue, newValue));
            
            // Show boss message
            if (newBossPanel != null)
            {
                string effect = newValue > originalValue ? "inflated" : "deflated";
                newBossPanel.ShowBossMessage($"The Corruptor {effect} a card from {originalValue} to {newValue}!");
                StartCoroutine(HideBossMessageAfterDelay(2.5f));
            }
            
            Debug.Log($"The Corruptor corrupted card: {originalValue} → {newValue} (change: {corruptionAmount})");
        }
    }
    
    /// <summary>
    /// Animate card corruption effect
    /// </summary>
    private IEnumerator AnimateCardCorruption(GameObject card, int originalValue, int newValue)
    {
        if (card == null) yield break;
        
        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        // Store original color
        Color originalColor = spriteRenderer.color;
        
        // Determine corruption color (red for inflation, purple for deflation)
        Color corruptionColor = newValue > originalValue ? 
            new Color(1f, 0.2f, 0.2f, 1f) : // Red for inflation
            new Color(0.4f, 0.2f, 0.8f, 1f); // Purple for deflation
        
        // Create corruption animation sequence
        Sequence corruptionSequence = DOTween.Sequence();
        
        // Flash with corruption color
        corruptionSequence.Append(spriteRenderer.DOColor(corruptionColor, 0.2f));
        corruptionSequence.Append(spriteRenderer.DOColor(originalColor, 0.2f));
        corruptionSequence.Append(spriteRenderer.DOColor(corruptionColor, 0.2f));
        corruptionSequence.Append(spriteRenderer.DOColor(originalColor, 0.3f));
        
        // Add scale effect (grow for inflation, shrink for deflation)
        Vector3 originalScale = card.transform.localScale;
        float scaleMultiplier = newValue > originalValue ? 1.2f : 0.8f;
        
        corruptionSequence.Join(card.transform.DOScale(originalScale * scaleMultiplier, 0.3f)
            .SetEase(Ease.OutBack).SetLoops(2, LoopType.Yoyo));
        
        // Add subtle rotation
        corruptionSequence.Join(card.transform.DORotate(new Vector3(0, 0, 15), 0.2f)
            .SetEase(Ease.InOutSine).SetLoops(4, LoopType.Yoyo));
        
        yield return corruptionSequence.WaitForCompletion();
        
        // Ensure card is back to normal state
        card.transform.localScale = originalScale;
        card.transform.rotation = Quaternion.identity;
        spriteRenderer.color = originalColor;
    }
    
    /// <summary>
    /// Public method to handle The Seductress interception from Deck.cs
    /// </summary>
    public IEnumerator HandleSeductressInterception(GameObject card, CardInfo cardInfo, bool wasIntercepted)
    {
        if (wasIntercepted)
        {
            // This card was meant for the player but was intercepted
            yield return StartCoroutine(OptimizeInterceptedCard(card, cardInfo));
            
            // Show visual effect and message
            StartCoroutine(AnimateSeductressIntercept(card));
            
            if (newBossPanel != null)
            {
                newBossPanel.ShowBossMessage($"The Seductress seduces your {cardInfo.cardName}!");
                StartCoroutine(HideBossMessageAfterDelay(3f));
            }
        }
        else
        {
            // This card was already going to dealer, just optimize
            yield return StartCoroutine(OptimizeInterceptedCard(card, cardInfo));
            
            if (newBossPanel != null)
            {
                CardModel cardModel = card.GetComponent<CardModel>();
                if (cardModel != null)
                {
                    newBossPanel.ShowBossMessage($"The Seductress values her {cardInfo.cardName} as {cardModel.value}!");
                    StartCoroutine(HideBossMessageAfterDelay(2f));
                }
            }
        }
    }
    
    private void ModifyDeckForBoss()
    {
        // Implementation for special deck composition (like The Captain)
        Debug.Log("Modifying deck for boss...");
    }
    
    // Animation coroutines
    private IEnumerator AnimateCardTheft(GameObject card, bool isTraitor = false)
    {
        if (card == null) yield break;
        
        // Create theft animation
        Vector3 targetPos = deck.dealer.transform.position;
        
        Sequence theftSequence = DOTween.Sequence();
        
        // Flash the card - different color for The Traitor
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (isTraitor)
            {
                // The Traitor uses purple/dark color
                theftSequence.Append(spriteRenderer.DOColor(new Color(0.5f, 0f, 0.5f), 0.2f));
                theftSequence.Append(spriteRenderer.DOColor(Color.white, 0.2f));
                theftSequence.Append(spriteRenderer.DOColor(new Color(0.5f, 0f, 0.5f), 0.2f));
                theftSequence.Append(spriteRenderer.DOColor(Color.white, 0.2f));
            }
            else
            {
                // Regular thief uses red
                theftSequence.Append(spriteRenderer.DOColor(Color.red, 0.2f));
                theftSequence.Append(spriteRenderer.DOColor(Color.white, 0.2f));
            }
        }
        
        // Add rotation for more dramatic effect
        if (isTraitor)
        {
            theftSequence.Join(card.transform.DORotate(new Vector3(0, 0, 360), 0.6f, RotateMode.FastBeyond360));
        }
        
        // Move to dealer - wait for rearrangement to happen first
        yield return new WaitForSeconds(0.1f);
        
        // Card should already be in dealer's hand, just ensure proper positioning
        CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
        if (dealerHand != null)
        {
            dealerHand.ArrangeCardsInWindow();
        }
        
        yield return theftSequence.WaitForCompletion();
    }
    
    private IEnumerator HideBossMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (newBossPanel != null)
        {
            newBossPanel.HideBossMessage();
        }
    }
    
    private IEnumerator ShowBossHealEffect()
    {
        if (bossEffectParticles != null)
        {
            bossEffectParticles.Play();
            yield return new WaitForSeconds(1f);
            bossEffectParticles.Stop();
        }
    }
    
    private IEnumerator ShowBossDefeatEffect()
    {
        if (bossEffectParticles != null)
        {
            bossEffectParticles.Play();
            yield return new WaitForSeconds(2f);
            bossEffectParticles.Stop();
        }
    }
    
    /// <summary>
    /// Show visual feedback when the Forgetful Seer peeks at cards
    /// </summary>
    private IEnumerator ShowForgetfulSeerEffect(string cardsSeen)
    {
        // Show a UI message to the player
        if (newBossPanel != null)
        {
            newBossPanel.ShowBossMessage($"The Forgetful Seer peers into the future...\nSees: {cardsSeen}");
        }
        
        // Wait for the message to be visible
        yield return new WaitForSeconds(2f);
        
        // Clear the message
        if (newBossPanel != null)
        {
            newBossPanel.HideBossMessage();
        }
    }
    

    
    // Save/Load Progress
    private void SaveBossProgress()
    {
        PlayerPrefs.SetInt("TotalBossesDefeated", totalBossesDefeated);
        PlayerPrefs.SetInt("CurrentBossType", (int)currentBossType);
        PlayerPrefs.SetInt("CurrentBossHealth", currentBossHealth);
        PlayerPrefs.SetInt("CurrentHand", currentHand);
        PlayerPrefs.Save();
    }
    
    private void LoadBossProgress()
    {
        totalBossesDefeated = PlayerPrefs.GetInt("TotalBossesDefeated", 0);
        currentBossType = (BossType)PlayerPrefs.GetInt("CurrentBossType", 0);
        currentBossHealth = PlayerPrefs.GetInt("CurrentBossHealth", 3);
        currentHand = PlayerPrefs.GetInt("CurrentHand", 0);
    }
    
    // Public methods for external access
    public bool IsBossActive() => isBossActive;
    public BossData GetCurrentBoss() => currentBoss;
    public int GetCurrentBossHealth() => currentBossHealth;
    public int GetTotalBossesDefeated() => totalBossesDefeated;

    public List<BossData> GetAvailableBosses() => allBosses;
    
    // Debug methods
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugBossState()
    {
        Debug.Log($"=== BOSS SYSTEM DEBUG ===");
        Debug.Log($"Current Boss: {currentBoss?.bossName ?? "None"}");
        Debug.Log($"Boss Health: {currentBossHealth}/{currentBoss?.maxHealth ?? 0}");
        Debug.Log($"Current Hand: {currentHand}/{currentBoss?.handsPerRound ?? 0}");
        Debug.Log($"Total Defeated: {totalBossesDefeated}");
        Debug.Log($"Active Mechanics: {activeMechanics.Count}");
        Debug.Log($"Is Boss Active: {isBossActive}");
        
        Debug.Log($"=== BOSS PROGRESSION DEBUG ===");
        Debug.Log($"All Bosses Loaded: {allBosses.Count}");
        foreach (var boss in allBosses.OrderBy(b => b.unlockOrder))
        {
            Debug.Log($"  {boss.bossName}: unlockOrder={boss.unlockOrder}, bossType={boss.bossType}");
        }
        
        var expectedBoss = allBosses.Find(b => b.unlockOrder == totalBossesDefeated);
        Debug.Log($"Expected Boss for {totalBossesDefeated} defeated: {expectedBoss?.bossName ?? "None"}");
        
        Debug.Log($"=== END BOSS DEBUG ===");
    }
    
    /// <summary>
    /// Test method to manually trigger Forgetful Seer mechanic
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestForgetfulSeerMechanic()
    {
        if (currentBoss != null && currentBoss.bossType == BossType.TheForgetfulSeer)
        {
            var mechanic = currentBoss.GetMechanic(BossMechanicType.PeekNextCards);
            if (mechanic != null)
            {
                Debug.Log("Manually triggering Forgetful Seer mechanic...");
                PeekNextCards(mechanic);
            }
            else
            {
                Debug.LogError("Forgetful Seer mechanic not found!");
            }
        }
        else
        {
            Debug.LogError("Current boss is not The Forgetful Seer!");
        }
    }
    
    /// <summary>
    /// Test method to manually trigger Diplomat's King mechanic
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestDiplomatKingMechanic()
    {
        if (currentBoss != null && currentBoss.bossType == BossType.TheDiplomat)
        {
            var mechanic = currentBoss.GetMechanic(BossMechanicType.DiplomaticKing);
            if (mechanic != null)
            {
                Debug.Log("Testing Diplomat King mechanic...");
                
                // Check if dealer has any Kings
                if (deck != null && deck.dealer != null)
                {
                    CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
                    if (dealerHand != null)
                    {
                        foreach (GameObject card in dealerHand.cards)
                        {
                            CardModel cardModel = card.GetComponent<CardModel>();
                            if (cardModel != null)
                            {
                                CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
                                Debug.Log($"Dealer has: {cardInfo.cardName} (suitIndex: {cardInfo.suitIndex})");
                                
                                if (cardInfo.suitIndex == 12) // King
                                {
                                    Debug.Log("Found King in dealer's hand - manually triggering mechanic");
                                    ApplyDiplomaticKing(card, mechanic);
                                    return;
                                }
                            }
                        }
                        Debug.Log("No Kings found in dealer's hand");
                    }
                }
            }
            else
            {
                Debug.LogError("Diplomat King mechanic not found!");
            }
        }
        else
        {
            Debug.LogError("Current boss is not The Diplomat!");
        }
    }
    
    /// <summary>
    /// Test method to manually trigger Seductress mechanic
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestSeductressMechanic()
    {
        if (currentBoss != null && currentBoss.bossType == BossType.TheSeductress)
        {
            var mechanic = currentBoss.GetMechanic(BossMechanicType.SeductressIntercept);
            if (mechanic != null)
            {
                Debug.Log("Testing Seductress mechanic...");
                
                // Check both player and dealer hands for Kings/Jacks
                if (deck != null)
                {
                    if (deck.player != null)
                    {
                        CardHand playerHand = deck.player.GetComponent<CardHand>();
                        if (playerHand != null)
                        {
                            foreach (GameObject card in playerHand.cards)
                            {
                                CardModel cardModel = card.GetComponent<CardModel>();
                                if (cardModel != null)
                                {
                                    CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
                                    Debug.Log($"Player has: {cardInfo.cardName} (suitIndex: {cardInfo.suitIndex})");
                                    
                                    if (cardInfo.suitIndex == 10 || cardInfo.suitIndex == 12) // Jack or King
                                    {
                                        Debug.Log("Found King/Jack in player's hand - manually triggering interception");
                                        ApplySeductressIntercept(card, mechanic, true);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    
                    if (deck.dealer != null)
                    {
                        CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
                        if (dealerHand != null)
                        {
                            foreach (GameObject card in dealerHand.cards)
                            {
                                CardModel cardModel = card.GetComponent<CardModel>();
                                if (cardModel != null)
                                {
                                    CardInfo cardInfo = deck.GetCardInfoFromModel(cardModel);
                                    Debug.Log($"Dealer has: {cardInfo.cardName} (suitIndex: {cardInfo.suitIndex})");
                                    
                                    if (cardInfo.suitIndex == 10 || cardInfo.suitIndex == 12) // Jack or King
                                    {
                                        Debug.Log("Found King/Jack in dealer's hand - manually triggering optimization");
                                        ApplySeductressIntercept(card, mechanic, false);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                    Debug.Log("No Kings or Jacks found in either hand");
                }
            }
            else
            {
                Debug.LogError("Seductress Intercept mechanic not found!");
            }
        }
        else
        {
            Debug.LogError("Current boss is not The Seductress!");
        }
    }
    
    /// <summary>
    /// Reset boss progress for testing
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ResetBossProgress()
    {
        totalBossesDefeated = 0;
        currentBossType = BossType.TheDrunkard;
        currentBossHealth = 3;
        currentHand = 0;
        isBossActive = false;
        currentBoss = null;
        
        SaveBossProgress();
        Debug.Log("Boss progress reset to TheDrunkard");
    }
    
    /// <summary>
    /// Set boss progress to a specific boss for testing
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void SetBossProgress(int bossesDefeated)
    {
        totalBossesDefeated = bossesDefeated;
        SaveBossProgress();
        Debug.Log($"Boss progress set to {bossesDefeated} bosses defeated");
        
        // Reinitialize the appropriate boss
        var nextBoss = allBosses.Find(b => b.unlockOrder == totalBossesDefeated);
        if (nextBoss != null)
        {
            InitializeBoss(nextBoss.bossType);
        }
    }
    
    // The Traitor specific methods
    
    /// <summary>
    /// Mark a card as permanently destroyed
    /// </summary>
    private void MarkCardAsDestroyed(int originalDeckIndex)
    {
        string key = $"DestroyedCard_{originalDeckIndex}";
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        Debug.Log($"Card at original index {originalDeckIndex} marked as permanently destroyed");
    }
    
    /// <summary>
    /// Check if a card has been permanently destroyed
    /// </summary>
    public bool IsCardDestroyed(int originalDeckIndex)
    {
        string key = $"DestroyedCard_{originalDeckIndex}";
        return PlayerPrefs.GetInt(key, 0) == 1;
    }
    
    /// <summary>
    /// Animate The Traitor's card destruction
    /// </summary>
    private IEnumerator AnimateTraitorCardDestruction()
    {
        if (newBossPanel != null)
        {
            newBossPanel.ShowBossMessage("The Traitor destroys your stolen cards forever!");
        }
        
        // Animate each stolen card being destroyed
        foreach (GameObject card in traitorStolenCards)
        {
            if (card != null)
            {
                // Create destruction animation
                Sequence destructionSequence = DOTween.Sequence();
                
                SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // Flash dark purple
                    destructionSequence.Append(spriteRenderer.DOColor(new Color(0.3f, 0f, 0.3f), 0.15f));
                    destructionSequence.Append(spriteRenderer.DOColor(Color.black, 0.15f));
                }
                
                // Shake and shrink
                destructionSequence.Append(card.transform.DOShakePosition(0.3f, 0.5f, 20, 90, false, true));
                destructionSequence.Join(card.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
                
                // Fade out
                if (spriteRenderer != null)
                {
                    destructionSequence.Join(spriteRenderer.DOFade(0f, 0.3f));
                }
            }
        }
        
        yield return new WaitForSeconds(1.5f);
        
        if (newBossPanel != null)
        {
            newBossPanel.HideBossMessage();
        }
    }
    
    /// <summary>
    /// Clear The Traitor's tracking when initializing a new boss
    /// </summary>
    private void ClearTraitorTracking()
    {
        traitorStolenCards.Clear();
        traitorStolenCardIndices.Clear();
    }
    
    /// <summary>
    /// Clear The Naughty Child's tracking when initializing a new boss
    /// </summary>
    private void ClearNaughtyChildTracking()
    {
        naughtyChildStolenCards.Clear();
        naughtyChildStolenTarots.Clear();
        naughtyChildOriginalTarotSlots.Clear();
    }
    
    /// <summary>
    /// Hide all consumables (tarot cards) by greying them out and disabling them
    /// </summary>
    private void HideConsumables()
    {
        if (shopManager != null && shopManager.tarotPanel != null)
        {
            var tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
            foreach (var card in tarotCards)
            {
                if (card != null)
                {
                    // Disable the card's functionality
                    card.enabled = false;
                    
                    // Grey out the card visually
                    var cardImage = card.GetComponent<Image>();
                    if (cardImage != null)
                    {
                        cardImage.color = Color.gray;
                    }
                    
                    Debug.Log($"The Naughty Child hides tarot card: {card.cardData?.cardName}");
                }
            }
            
            // Show boss message
            if (newBossPanel != null)
            {
                newBossPanel.ShowBossMessage("The Naughty Child hides all your tarot cards!");
                StartCoroutine(HideBossMessageAfterDelay(2f));
            }
        }
    }
    
    /// <summary>
    /// Temporarily steal cards from player (both deck cards and tarot cards)
    /// </summary>
    private void TemporaryCardTheft(BossMechanic mechanic)
    {
        int deckCardsToSteal = 0; // Declare at method scope
        
        if (deck != null && deck.player != null)
        {
            var playerHand = deck.player.GetComponent<CardHand>();
            
            if (playerHand != null && playerHand.cards.Count > 0)
            {
                // Steal deck cards
                deckCardsToSteal = Mathf.Min(mechanic.mechanicValue, playerHand.cards.Count - 1); // Leave at least 1 card
                deckCardsToSteal = Mathf.Max(0, deckCardsToSteal);
                
                for (int i = 0; i < deckCardsToSteal; i++)
                {
                    if (playerHand.cards.Count > 1) // Ensure we don't steal all cards
                    {
                        int randomIndex = Random.Range(0, playerHand.cards.Count);
                        GameObject cardToSteal = playerHand.cards[randomIndex];
                        
                        // Remove from player's hand and track it
                        playerHand.cards.RemoveAt(randomIndex);
                        naughtyChildStolenCards.Add(cardToSteal);
                        
                        // Hide the card
                        cardToSteal.SetActive(false);
                        
                        CardModel cardModel = cardToSteal.GetComponent<CardModel>();
                        if (cardModel != null)
                        {
                            Debug.Log($"The Naughty Child steals deck card: {deck.GetCardInfoFromModel(cardModel).cardName}");
                        }
                    }
                }
                
                // Update player hand after stealing deck cards
                if (deckCardsToSteal > 0)
                {
                    playerHand.ArrangeCardsInWindow();
                    playerHand.UpdatePoints();
                    deck.UpdateScoreDisplays();
                }
            }
            
            // Steal tarot cards
            if (shopManager != null && shopManager.tarotPanel != null)
            {
                var tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
                var availableTarots = tarotCards.Where(t => t != null && t.gameObject.activeSelf && !t.isInShop).ToList();
                
                int tarotCardsToSteal = Mathf.Min(2, availableTarots.Count); // Steal up to 2 tarot cards
                
                for (int i = 0; i < tarotCardsToSteal; i++)
                {
                    if (availableTarots.Count > 0)
                    {
                        int randomIndex = Random.Range(0, availableTarots.Count);
                        TarotCard tarotToSteal = availableTarots[randomIndex];
                        
                        // Store original slot for return
                        naughtyChildOriginalTarotSlots.Add(tarotToSteal.transform.parent);
                        naughtyChildStolenTarots.Add(tarotToSteal);
                        
                        // Hide the tarot card
                        tarotToSteal.gameObject.SetActive(false);
                        
                        Debug.Log($"The Naughty Child steals tarot card: {tarotToSteal.cardData?.cardName}");
                        
                        availableTarots.RemoveAt(randomIndex);
                    }
                }
            }
            
            // Show boss message
            if (newBossPanel != null && (deckCardsToSteal > 0 || naughtyChildStolenTarots.Count > 0))
            {
                int totalStolen = deckCardsToSteal + naughtyChildStolenTarots.Count;
                newBossPanel.ShowBossMessage($"The Naughty Child steals {totalStolen} cards from you!");
                StartCoroutine(HideBossMessageAfterDelay(2f));
            }
        }
    }
    
    /// <summary>
    /// Return all stolen cards to the player after the round
    /// </summary>
    public void ReturnNaughtyChildStolenCards()
    {
        if (currentBoss?.bossType != BossType.TheNaughtyChild) return;
        
        int cardsReturned = 0;
        
        // Return stolen deck cards
        if (deck != null && deck.player != null)
        {
            var playerHand = deck.player.GetComponent<CardHand>();
            if (playerHand != null)
            {
                foreach (GameObject stolenCard in naughtyChildStolenCards)
                {
                    if (stolenCard != null)
                    {
                        // Re-activate the card and add back to player's hand
                        stolenCard.SetActive(true);
                        playerHand.cards.Add(stolenCard);
                        cardsReturned++;
                        
                        CardModel cardModel = stolenCard.GetComponent<CardModel>();
                        if (cardModel != null)
                        {
                            Debug.Log($"The Naughty Child returns deck card: {deck.GetCardInfoFromModel(cardModel).cardName}");
                        }
                    }
                }
                
                // Update player hand
                if (naughtyChildStolenCards.Count > 0)
                {
                    playerHand.ArrangeCardsInWindow();
                    playerHand.UpdatePoints();
                    deck.UpdateScoreDisplays();
                }
            }
        }
        
        // Return stolen tarot cards
        for (int i = 0; i < naughtyChildStolenTarots.Count; i++)
        {
            if (i < naughtyChildOriginalTarotSlots.Count && 
                naughtyChildStolenTarots[i] != null && 
                naughtyChildOriginalTarotSlots[i] != null)
            {
                // Return tarot to its original slot
                naughtyChildStolenTarots[i].transform.SetParent(naughtyChildOriginalTarotSlots[i], false);
                naughtyChildStolenTarots[i].transform.localPosition = Vector3.zero;
                naughtyChildStolenTarots[i].gameObject.SetActive(true);
                cardsReturned++;
                
                Debug.Log($"The Naughty Child returns tarot card: {naughtyChildStolenTarots[i].cardData?.cardName}");
            }
        }
        
        // Re-enable tarot cards (unhide consumables)
        if (shopManager != null && shopManager.tarotPanel != null)
        {
            var tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>(true); // Include inactive ones
            foreach (var card in tarotCards)
            {
                if (card != null)
                {
                    // Re-enable the card's functionality
                    card.enabled = true;
                    
                    // Restore normal color
                    var cardImage = card.GetComponent<Image>();
                    if (cardImage != null)
                    {
                        cardImage.color = Color.white;
                    }
                }
            }
        }
        
        // Show return message
        if (cardsReturned > 0 && newBossPanel != null)
        {
            newBossPanel.ShowBossMessage($"The Naughty Child returns {cardsReturned} stolen cards!");
            StartCoroutine(HideBossMessageAfterDelay(2f));
        }
        
        // Clear tracking
        ClearNaughtyChildTracking();
    }
    
    // Smart stealing helper methods
    
    /// <summary>
    /// Get a list of cards from player's hand that are safe for the dealer to steal
    /// (won't cause dealer to bust)
    /// </summary>
    private List<GameObject> GetSafeCardsToSteal(CardHand playerHand, CardHand dealerHand)
    {
        List<GameObject> safeCards = new List<GameObject>();
        
        if (playerHand == null || dealerHand == null || playerHand.cards.Count == 0)
            return safeCards;
        
        int currentDealerPoints = dealerHand.points;
        
        foreach (GameObject card in playerHand.cards)
        {
            CardModel cardModel = card.GetComponent<CardModel>();
            if (cardModel != null)
            {
                // Calculate what dealer's score would be if this card was added
                int potentialPoints = currentDealerPoints + cardModel.value;
                
                // Consider ace flexibility (if it's an ace and would cause bust, try as 1)
                if (cardModel.value == 1) // Ace
                {
                    int potentialWithSoftAce = currentDealerPoints + Constants.SoftAce;
                    // If soft ace doesn't bust, prefer it; otherwise use as 1
                    if (potentialWithSoftAce <= Constants.Blackjack)
                    {
                        potentialPoints = potentialWithSoftAce;
                    }
                }
                
                // Card is safe if it doesn't cause dealer to bust
                if (potentialPoints <= Constants.Blackjack)
                {
                    safeCards.Add(card);
                }
                else
                {
                    Debug.Log($"Skipping card {deck.GetCardInfoFromModel(cardModel).cardName} - would cause dealer bust ({currentDealerPoints} + {cardModel.value} = {potentialPoints})");
                }
            }
        }
        
        return safeCards;
    }
    
    /// <summary>
    /// Get the best card to steal - prioritizes higher value cards that won't cause bust
    /// </summary>
    private GameObject GetBestCardToSteal(List<GameObject> safeCards)
    {
        if (safeCards.Count == 0) return null;
        
        // Sort by value descending (prefer higher value cards)
        safeCards.Sort((a, b) => {
            CardModel cardA = a.GetComponent<CardModel>();
            CardModel cardB = b.GetComponent<CardModel>();
            if (cardA == null || cardB == null) return 0;
            
            int valueA = cardA.value;
            int valueB = cardB.value;
            
            // Special handling for Aces - prefer them as they're flexible
            if (valueA == 1) valueA = 11; // Treat ace as 11 for sorting
            if (valueB == 1) valueB = 11;
            
            return valueB.CompareTo(valueA); // Descending order
        });
        
        return safeCards[0]; // Return the highest value safe card
    }
    
    /// <summary>
    /// Get the lowest value card to steal when dealer needs to be conservative
    /// </summary>
    private GameObject GetLowestValueCardToSteal(List<GameObject> safeCards)
    {
        if (safeCards.Count == 0) return null;
        
        // Sort by value ascending (prefer lower value cards)
        safeCards.Sort((a, b) => {
            CardModel cardA = a.GetComponent<CardModel>();
            CardModel cardB = b.GetComponent<CardModel>();
            if (cardA == null || cardB == null) return 0;
            
            return cardA.value.CompareTo(cardB.value); // Ascending order
        });
        
        return safeCards[0]; // Return the lowest value safe card
    }
    
    [ContextMenu("Test Corruptor Mechanic")]
    public void TestCorruptorMechanic()
    {
        Debug.Log("=== Testing Corruptor Mechanic ===");
        
        if (currentBoss == null)
        {
            Debug.LogWarning("No current boss set!");
            return;
        }
        
        var mechanic = currentBoss.GetMechanic(BossMechanicType.CorruptCard);
        if (mechanic == null)
        {
            Debug.LogWarning("Corruptor mechanic not found!");
            return;
        }
        
        Debug.Log($"Corruptor mechanic found: {mechanic.mechanicName}");
        Debug.Log($"Activation chance: {mechanic.activationChance}");
        Debug.Log($"Max corruption: {mechanic.mechanicValue}");
        Debug.Log($"Triggers on card dealt: {mechanic.triggersOnCardDealt}");
    }
}
