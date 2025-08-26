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
            if (Random.Range(0f, 1f) <= mechanic.activationChance)
            {
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
    /// Show next boss introduction with dramatic effect
    /// </summary>
    private IEnumerator ShowNextBossIntroduction(BossData nextBoss)
    {
        Debug.Log($"Showing next boss introduction: {nextBoss.bossName}");
        
        // Show the boss panel with next boss info
        if (newBossPanel != null)
        {
            // Show the next boss introduction with special effects
            newBossPanel.ShowNextBossIntroduction(nextBoss);
            
            // Wait for the introduction to be visible
            yield return new WaitForSeconds(4f);
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
            if (playerHand != null && playerHand.cards.Count > 0)
            {
                int cardsToSteal = Mathf.Min(mechanic.mechanicValue, playerHand.cards.Count);
                for (int i = 0; i < cardsToSteal; i++)
                {
                    // Steal a random card
                    int randomIndex = Random.Range(0, playerHand.cards.Count);
                    GameObject stolenCard = playerHand.cards[randomIndex];
                    playerHand.cards.RemoveAt(randomIndex);
                    
                    // Add to dealer's hand
                    var dealerHand = deck.dealer.GetComponent<CardHand>();
                    if (dealerHand != null)
                    {
                        dealerHand.cards.Add(stolenCard);
                    }
                    
                    // Animate the theft
                    StartCoroutine(AnimateCardTheft(stolenCard));
                }
            }
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
    
    private void ModifyDeckForBoss()
    {
        // Implementation for special deck composition (like The Captain)
        Debug.Log("Modifying deck for boss...");
    }
    
    // Animation coroutines
    private IEnumerator AnimateCardTheft(GameObject card)
    {
        if (card == null) yield break;
        
        // Create theft animation
        Vector3 originalPos = card.transform.position;
        Vector3 targetPos = deck.dealer.transform.position;
        
        Sequence theftSequence = DOTween.Sequence();
        
        // Flash the card
        var spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            theftSequence.Append(spriteRenderer.DOColor(Color.red, 0.2f));
            theftSequence.Append(spriteRenderer.DOColor(Color.white, 0.2f));
        }
        
        // Move to dealer
        theftSequence.Append(card.transform.DOMove(targetPos, 0.5f).SetEase(Ease.InQuad));
        
        yield return theftSequence.WaitForCompletion();
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
}
