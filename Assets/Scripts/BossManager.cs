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
        
        // Initialize first boss if none is active
        if (currentBoss == null)
        {
            InitializeBoss(BossType.TheDrunkard);
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
        
        // Apply passive mechanics
        foreach (var mechanic in activeMechanics.Where(m => m.isPassive))
        {
            ApplyMechanic(mechanic);
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
        else if (currentHand >= currentBoss.handsPerRound)
        {
            BossHeals();
        }
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
        
        if (currentHand >= currentBoss.handsPerRound)
        {
            BossHeals();
        }
        
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
        
        // Trigger mechanics that activate on card dealt
        foreach (var mechanic in activeMechanics.Where(m => m.triggersOnCardDealt))
        {
            if (Random.Range(0f, 1f) <= mechanic.activationChance)
            {
                ApplyMechanicToCard(mechanic, card, isPlayer);
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
    /// Boss heals after handsPerRound hands
    /// </summary>
    private void BossHeals()
    {
        int healAmount = Mathf.Max(1, currentBoss.maxHealth / 4);
        currentBossHealth = Mathf.Min(currentBossHealth + healAmount, currentBoss.maxHealth);
        
        currentHand = 0;
        
        OnBossHealed?.Invoke(currentBoss);
        
        Debug.Log($"{currentBoss.bossName} heals for {healAmount}! Health: {currentBossHealth}/{currentBoss.maxHealth}");
        
        // Show heal effect and update health bar
        if (newBossPanel != null)
        {
            newBossPanel.UpdateHealthBar();
            newBossPanel.ShowHealEffect();
        }
        StartCoroutine(ShowBossHealEffect());
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
        
        // Find next boss
        var nextBoss = allBosses.Find(b => b.unlockOrder == totalBossesDefeated);
        
        if (nextBoss != null)
        {
            // Show next boss introduction
            yield return StartCoroutine(ShowNextBossIntroduction(nextBoss));
            
            // Initialize the next boss
            InitializeBoss(nextBoss.bossType);
        }
        else
        {
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
        Debug.Log("=== END BOSS DEBUG ===");
    }
}
