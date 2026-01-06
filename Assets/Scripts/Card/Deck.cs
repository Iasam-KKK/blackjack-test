#undef ARRAY_SHUFFLE

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

// Struct to hold complete card information
[System.Serializable]
public struct CardInfo
{
    public int index;           // Index in the deck (0-51)
    public int value;           // Card value (1-10, J/Q/K = 10)
    public CardSuit suit;       // Heart, Diamond, Club, Spade
    public int suitIndex;       // Index within the suit (0-12: A, 2-10, J, Q, K)
    public string cardName;     // Human readable name (e.g., "Ace of Hearts", "King of Spades")
    public Sprite cardSprite;   // The actual card sprite

    public CardInfo(int deckIndex, int cardValue, Sprite sprite, Sprite[] allSprites)
    {
        index = deckIndex;
        value = cardValue;
        suit = GetSuitFromIndex(deckIndex);
        suitIndex = deckIndex % Constants.CardsPerSuit;
        cardSprite = sprite;
        cardName = GenerateCardName(cardValue, suit, suitIndex);
    }
    
    private static CardSuit GetSuitFromIndex(int deckIndex)
    {
        int suitNumber = deckIndex / Constants.CardsPerSuit;
        return (CardSuit)suitNumber;
    }
    
    private static string GenerateCardName(int value, CardSuit suit, int suitIndex)
    {
        string valueName;
        if (suitIndex == 0) valueName = "Ace";
        else if (suitIndex == 10) valueName = "Jack";
        else if (suitIndex == 11) valueName = "Queen"; 
        else if (suitIndex == 12) valueName = "King";
        else valueName = value.ToString();
        
        return valueName + " of " + suit.ToString();
    }
}

internal static class Constants
{
    public const int DeckCards = 52;
    public const int Blackjack = 21;
    public const int DealerStand = 17;
    public const int SoftAce = 11;
    public const int InitialCardsDealt = 2;
    public const int ProbPrecision = 2;
    public const uint InitialBalance = 1000;
    public const uint BetIncrement = 10;
    public const uint BetWinMultiplier = 2;
    public const uint NewGameCountdown = 5;
    public const int MaxCardsInHand = 5; // Maximum number of cards in a hand
    public const float PeekDuration = 2.0f; // Duration in seconds to peek at dealer's card
    public const int MaxSelectedCards = 2; // Maximum number of cards that can be selected at once for transformation
    
    // Boss system constants
    public const int DefaultBossHealth = 3;
    public const int DefaultHandsPerRound = 3;
    
    // Streak system constants
    public const int StreakMultiplierIncrement = 1;
    public const float BaseWinMultiplier = 1.0f; // Base bonus multiplier (1.0 = no bonus, profit = bet amount)
    public const float StreakMultiplierStep = 0.5f; // How much bonus increases per streak level (was 0.25f)
    public const int MaxStreakLevel = 5;
    
    // Card dealing animation constants
    public const float CardDealDuration = 0.35f; // Duration for each card to be dealt (faster)
    public const float CardDealDelay = 0.15f; // Delay between dealing each card (faster)
    public const float CardDealDistance = 10f; // Distance cards travel from deck position
    
    // Suit bonus constants
    public const int SuitBonusAmount = 50; // Bonus amount per card of matching suit
    public const int CardsPerSuit = 13; // Number of cards per suit (A, 2-10, J, Q, K)
    public const int HouseKeeperBonusAmount = 10; // Bonus amount per Jack/Queen/King card
    
    // Round Flow constants
    public const int MaxHitsPerHand = 3; // Maximum extra hits per hand (default: 3, upgradeable)
    public const int DefaultActionBudget = 2; // Default action budget per hand for special cards
    public const int DefaultTarotLimit = 1; // Default number of tarots usable per hand
    public const uint DefaultMinBet = 10; // Default minimum bet
    public const uint DefaultMaxBet = 1000; // Default maximum bet (can be modified by balance or bosses)
}

// Boss system enums
internal enum BossState
{
    Waiting,
    Fighting,
    Defeated
}

internal enum WinCode 
{ 
    DealerWins, 
    PlayerWins, 
    Draw 
}

public class Deck : MonoBehaviour
{
    public Sprite[] faces;
    public int[] originalIndices = new int[Constants.DeckCards]; // Track original positions before shuffle
    public GameObject dealer;
    public GameObject player;
    public Transform deckPosition; // Position where cards are dealt from (optional)

    public Button hitButton;
    public Button stickButton;
    public Button playAgainButton;
    public Button discardButton; 
    public Text finalMessage;
    public Text probMessage;
    // SCORE SYSTEM 2.0: Removed playerScoreText and dealerScoreText - now handled by ScoreManager
    public Button peekButton; // Eye button for peeking
    public Button transformButton; // Transformation button for card transformation
    public Button doubleDownButton; // New button for Double Down action
    
    // BETTING SYSTEM 2.0: Removed old betting UI references (raiseBetButton, lowerBetButton, placeBetButton, balance, bet)
    // Now handled by BettingManager
    
    // UI elements for Round Flow tracking
    public Text hitsRemainingText; // Display remaining hits
    public Text actionsRemainingText; // Display remaining actions
    public Text discardPileCountText; // Display discard pile count
    public Button reshuffleButton; // Button to manually reshuffle discard pile
    public Text deckCardsRemainingText; // Display cards remaining in deck
    
    // Action Cards System
    public Transform actionCardsPanel; // Panel to hold action card slots
    public GameObject actionCardPrefab; // Prefab for action cards
    public ActionCardData[] availableActionCards; // Array of available action cards
    
    // Deck Inspector System
    public PlayerDeck playerDeck;                    // Player's 30-card deck
    public DeckInspectorPanel deckInspectorPanel;   // Deck inspector UI panel
    public Button deckInspectorButton;              // Button to open deck inspector
    
    // New Boss Panel (replaces old blind panel system)
    public NewBossPanel newBossPanel;
    
    // UI elements for streak
    public Text streakText;
    public GameObject streakPanel;
    public StreakFlameEffect streakFlameEffect;
    
    // Game History
    public GameHistoryManager gameHistoryManager;
    public GameHistoryManagerV2 gameHistoryManagerV2;

    // Card Preview System for new tarot cards
    public CardPreviewManager cardPreviewManager;
    
    // Boss System Integration
    public BossManager bossManager;
    
    // BETTING SYSTEM 2.0: Reference to BettingManager
    public BettingManager bettingManager;
    
    // SCORE SYSTEM 2.0: Reference to ScoreManager
    public ScoreManager scoreManager;
    
    // BETTING SYSTEM 2.0: Current bet amount (set by BettingManager or tarot cards)
    public float CurrentBetAmount { get; set; } = 0f;

    // BETTING SYSTEM 2.0: Removed _balance and _bet (now handled by BettingManager and GameProgressionManager)
    private bool _isPeeking = false;
    public bool _isBetPlaced = false; // Track if bet has been placed for current round
    public bool _hasUsedPeekThisRound = false; // Track if peek has been used in current round
    public bool _hasUsedTransformThisRound = false; // Track if transform has been used in current round
    
    // NEW: Track usage of preview cards
    public bool _hasUsedSpyThisRound = false;
    public bool _hasUsedBlindSeerThisRound = false;
    public bool _hasUsedCorruptJudgeThisRound = false;
    public bool _hasUsedHitmanThisRound = false;
    public bool _hasUsedFortuneTellerThisRound = false;
    public bool _hasUsedMadWriterThisRound = false;
    
    // NEW: Track activation of passive cards (now made active)
    public bool _hasActivatedBotanistThisRound = false;
    public bool _hasActivatedAssassinThisRound = false;
    public bool _hasActivatedSecretLoverThisRound = false;
    public bool _hasActivatedJewelerThisRound = false;
    public bool _hasActivatedHouseKeeperThisRound = false;
    public bool _hasActivatedWitchDoctorThisRound = false;
    public bool _hasActivatedArtificerThisRound = false;
    
    // Boss system variables
    private BossState _currentBossState = BossState.Waiting;

    // Streak variables
    private int _currentStreak = 0;
    private int _streakMultiplier = 1;
    
    // Button hold variables
    private bool _isHoldingRaiseBet = false;
    private bool _isHoldingLowerBet = false;
    private Coroutine _raiseBetCoroutine = null;
    private Coroutine _lowerBetCoroutine = null;
    public CardModel selectedCardForMakeupArtist;
    
    // Track the last card hit by player for The Escapist
    public GameObject _lastHitCard = null;
    
    // Turn-based system variables
    public enum GameTurn { Player, Dealer, GameOver }
    public GameTurn _currentTurn = GameTurn.Player;
    public bool _playerStood = false;
    public bool _dealerStood = false;
    public bool _gameInProgress = false;
    
    // Round Flow tracking variables
    private int _hitsThisHand = 0; // Track number of hits this hand
    private int _maxHitsPerHand = Constants.MaxHitsPerHand; // Upgradeable
    private int _actionsRemainingThisHand = Constants.DefaultActionBudget; // Action budget for special cards
    private int _maxActionsPerHand = Constants.DefaultActionBudget; // Upgradeable
    private int _tarotsUsedThisHand = 0; // Track tarot usage per hand
    private int _maxTarotsPerHand = Constants.DefaultTarotLimit; // Upgradeable
    private uint _minBet = Constants.DefaultMinBet; // Minimum bet (can be modified by bosses/curses)
    private uint _maxBet = Constants.DefaultMaxBet; // Maximum bet (can be modified by bosses/curses)
    private bool _hasDoubledDown = false; // Track if player has doubled down this hand
    
    // Discard pile system
    private List<CardInfo> _discardPile = new List<CardInfo>();



    // Public property to access balance
    // BETTING SYSTEM 2.0: Removed Balance property - now using GameProgressionManager.playerHealthPercentage


    public int[] values = new int[Constants.DeckCards];
    int cardIndex = 0;
    
    // Public property to access cardIndex for boss mechanics
    public int CardIndex => cardIndex;  
       
    private void Awake() => 
        InitCardValues();

    private void Start()
    {
        // BETTING SYSTEM 2.0: Removed balance initialization - now using GameProgressionManager

        ShuffleCards();
        
        // Subscribe to ActionCardManager events
        if (ActionCardManager.Instance != null)
        {
            ActionCardManager.Instance.OnEquippedCardsChanged += RefreshActionCardsDisplay;
        }
        
        // Find BossManager if not assigned
        if (bossManager == null)
            bossManager = FindObjectOfType<BossManager>();
        
        // Initialize TarotEffectManager and register this deck
        if (TarotEffectManager.Instance != null)
        {
            TarotEffectManager.Instance.SetDeck(this);
            Debug.Log("[Deck] Registered with TarotEffectManager");
        }
        
        // Initialize boss system if available
        if (bossManager != null)
        {
            Debug.Log("Boss system initialized");
            // Initialize new boss panel if available
            if (newBossPanel != null)
            {
                newBossPanel.ShowBossPanel();
            }
        }
        else
        {
            Debug.LogWarning("BossManager not found - boss system disabled");
        }
        
        // BETTING SYSTEM 2.0: Removed bet display initialization
        UpdateStreakUI(); // Initialize streak display with 1x flame
        
        // Initialize boss system - BUT NOT if minion encounter is active
        // BossManager.Start() already handles minion initialization
        if (bossManager != null)
        {
            // Check if this is a minion encounter - if so, BossManager already initialized it
            if (MinionEncounterManager.Instance != null && MinionEncounterManager.Instance.isMinionActive)
            {
                Debug.Log("[Deck] Minion encounter detected - skipping boss initialization");
                _currentBossState = BossState.Fighting;
            }
            else
            {
                // Not a minion encounter - this shouldn't happen as BossManager.Start() handles boss selection
                // Only initialize if BossManager didn't already set up a boss
                if (!bossManager.IsBossActive())
                {
                    Debug.LogWarning("[Deck] No boss or minion active - BossManager should have initialized this");
                }
                _currentBossState = BossState.Fighting;
            }
        }
        
        // Set the button text to "Next Hand" at the start
        SetButtonTextToNextHand();
        
        // Disable the next round button until the round is over
        playAgainButton.interactable = false;
        
        // Set up dealer and player hands
        if (dealer != null)
        {
            CardHand dealerHand = dealer.GetComponent<CardHand>();
            if (dealerHand != null)
            {
                dealerHand.isDealer = true; // Explicitly set dealer flag
                Debug.Log("Set up dealer hand, isDealer=" + dealerHand.isDealer);
            }
            else
            {
                Debug.LogError("Dealer GameObject does not have a CardHand component!");
            }
        }
        else
        {
            Debug.LogWarning("Dealer reference missing!");
        }
        
        if (player != null)
        {
            CardHand playerHand = player.GetComponent<CardHand>();
            if (playerHand != null)
            {
                playerHand.isDealer = false; // Explicitly set player flag
                Debug.Log("Set up player hand, isDealer=" + playerHand.isDealer);
            }
            else
            {
                Debug.LogError("Player GameObject does not have a CardHand component!");
            }
        }
        else
        {
            Debug.LogWarning("Player reference missing!");
        }
        
        // Configure button listeners
        if (peekButton != null)
        {
            peekButton.onClick.RemoveAllListeners();
            peekButton.onClick.AddListener(PeekAtDealerCard);
        }
        
        if (transformButton != null)
        {
            transformButton.onClick.RemoveAllListeners();
            transformButton.onClick.AddListener(TransformSelectedCards);
        }
        
        // Deck Inspector button setup
        if (deckInspectorButton != null)
        {
            deckInspectorButton.onClick.RemoveAllListeners();
            deckInspectorButton.onClick.AddListener(ToggleDeckInspector);
        }
        
        // Initialize PlayerDeck if not assigned
        if (playerDeck == null)
        {
            playerDeck = FindObjectOfType<PlayerDeck>();
        }
        
        // BETTING SYSTEM 2.0: Removed placeBetButton setup - now handled by BettingManager
        // BETTING SYSTEM 2.0: Removed SetupBetButtonHoldListeners() - no longer needed
        
        // Subscribe to GameProgressionManager events
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.OnPlayerGameOver += HandlePlayerGameOver;
        }
        
        // Initialize game in betting state (no cards dealt yet)
        InitializeBettingState();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from GameProgressionManager events
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.OnPlayerGameOver -= HandlePlayerGameOver;
        }
        
        // Unsubscribe from ActionCardManager events
        if (ActionCardManager.Instance != null)
        {
            ActionCardManager.Instance.OnEquippedCardsChanged -= RefreshActionCardsDisplay;
        }
    }
    
    /// <summary>
    /// Called when player health reaches 0 - show game over UI and pause game
    /// </summary>
    private void HandlePlayerGameOver()
    {
        Debug.Log("[Deck] HandlePlayerGameOver - Player health depleted!");
        
        // Pause the game
        Time.timeScale = 0f;
        
        // End current game if in progress
        _gameInProgress = false;
        _currentTurn = GameTurn.GameOver;
        
        // Disable all game buttons
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        // BETTING SYSTEM 2.0: Removed betting button disabling - handled by BettingManager
        
        // Disable play again button (game over means return to main menu, not next round)
        playAgainButton.interactable = false;
        
        // Show game over message
        if (finalMessage != null)
        {
            finalMessage.text = "GAME OVER\nPlayer Health Depleted\n\nReturning to Main Menu...";
            finalMessage.gameObject.SetActive(true);
        }
        
        Debug.Log("[Deck] Game over UI displayed, returning to main menu in 3 seconds");
    }
 // Helper: apply equipped frame to a spawned card GameObject (sprite-based prefab)
 private void ApplyEquippedFrame(GameObject cardGO)
 {
     if (cardGO == null) return;

     // Try CardUI first
     CardUI cardUI = cardGO.GetComponent<CardUI>();
     if (cardUI != null)
     {
         cardUI.ApplyEquippedFrame();
         return;
     }

     // Fallback: find "Frame" child and set its Image sprite
     Transform frameT = cardGO.transform.Find("Frame");
     if (frameT != null)
     {
         Image img = frameT.GetComponent<Image>();
         if (img != null && DeckMaterialManager.Instance != null)
         {
             Sprite frame = DeckMaterialManager.Instance.GetCurrentFrame();
             img.sprite = frame;
             img.enabled = frame != null;
         }
     }
 }

// Re-apply frames to all currently dealt cards (player & dealer hands)
// Re-apply the equipped frame to all currently active dealt cards
    public void RefreshAllActiveCardFrames()
    {
        // First try to update via CardUI components (fast)
        CardUI[] cardUIs = FindObjectsOfType<CardUI>();
        foreach (var c in cardUIs)
        {
            if (c != null)
                c.ApplyEquippedFrame();
        }

        // If your CardHand keeps references, you can also iterate them (optional)
        if (dealer != null)
        {
            var dealerHand = dealer.GetComponent<CardHand>();
            if (dealerHand != null && dealerHand.cards != null)
            {
                foreach (var go in dealerHand.cards)
                    ApplyEquippedFrame(go);
            }
        }

        if (player != null)
        {
            var playerHand = player.GetComponent<CardHand>();
            if (playerHand != null && playerHand.cards != null)
            {
                foreach (var go in playerHand.cards)
                    ApplyEquippedFrame(go);
            }
        }
    }    private void InitCardValues()
    {
        int count = 0;
        for (int i = 0; i < values.Length; ++i) 
        { 
            if (count > 9)
            {
                values[i] = 10; 
                values[++i] = 10;
                values[++i] = 10;
                count = 0;
            }
            else
            {
                values[i] = count + 1; 
                count++;
            }
        }
        
        // Initialize original indices to track pre-shuffle positions
        for (int i = 0; i < originalIndices.Length; i++)
        {
            originalIndices[i] = i;
        }
    }
 
    private void ShuffleCards()
    {
        #if (ARRAY_SHUFFLE)
            ArrayShuffle();
        #else
            FisherYatesShuffle();
        #endif
        
        // Note: PlayerDeck is now separate and handles its own shuffling
        Debug.Log("[Deck] Main deck (dealer) shuffled");
    }
 
    private void FisherYatesShuffle()
    {
        // First, check if any cards have been permanently destroyed by The Traitor
        if (bossManager != null)
        {
            List<Sprite> tempFaces = new List<Sprite>();
            List<int> tempValues = new List<int>();
            List<int> tempOriginalIndices = new List<int>();
            
            // Only include cards that haven't been destroyed
            for (int i = 0; i < faces.Length; i++)
            {
                if (!bossManager.IsCardDestroyed(originalIndices[i]))
                {
                    tempFaces.Add(faces[i]);
                    tempValues.Add(values[i]);
                    tempOriginalIndices.Add(originalIndices[i]);
                }
                else
                {
                    Debug.Log($"Skipping destroyed card at original index {originalIndices[i]}");
                }
            }
            
            // If cards were destroyed, update the arrays
            if (tempFaces.Count < faces.Length)
            {
                faces = tempFaces.ToArray();
                values = tempValues.ToArray();
                originalIndices = tempOriginalIndices.ToArray();
                Debug.Log($"Deck reduced from 52 to {faces.Length} cards due to The Traitor's destruction");
            }
        }
        
        // Now perform the regular shuffle
        for (int i = 0; i < values.Length; ++i)
        {
            int rndIndex = Random.Range(i, values.Length);
 
            Sprite currCard = faces[i];
            faces[i] = faces[rndIndex];
            faces[rndIndex] = currCard;
 
            int currValue = values[i];
            values[i] = values[rndIndex];
            values[rndIndex] = currValue;
            
            // Also shuffle the original indices to maintain mapping
            int currOriginalIndex = originalIndices[i];
            originalIndices[i] = originalIndices[rndIndex];
            originalIndices[rndIndex] = currOriginalIndex;
        }
    }

    private void ArrayShuffle()
    { 
        System.Random rnd = new System.Random();
        int[] index = Enumerable.Range(0, values.Length).ToArray();
        index.OrderBy(_ => rnd.Next()).ToArray();
         
        int[] tmpValues = new int[Constants.DeckCards];
        Sprite[] tmpFaces = new Sprite[Constants.DeckCards];
        int[] tmpOriginalIndices = new int[Constants.DeckCards];
 
        for (int i = 0; i < Constants.DeckCards; ++i)
        {
            tmpValues[index[i]] = values[i];
            tmpFaces[index[i]] = faces[i];
            tmpOriginalIndices[index[i]] = originalIndices[i];
        }
 
        for (int i = 0; i < Constants.DeckCards; ++i)
        {
            values[i] = tmpValues[i];
            faces[i] = tmpFaces[i];
            originalIndices[i] = tmpOriginalIndices[i];
        }
    }

    private void StartGame()
    {
        // Only start if bet has been placed
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot start game: No bet placed");
            return;
        }
        
        StopCoroutine(NewGame());
        
        // Start animated card dealing
        StartCoroutine(DealInitialCardsAnimated());
    }

    private bool Blackjack(GameObject whoever, bool isPlayer)
    {
        int handPoints = isPlayer ? GetPlayerPoints() : GetDealerPoints();
        if (handPoints == Constants.Blackjack) { return true; }
        else
        {
            CardHand hand = whoever.GetComponent<CardHand>();
            foreach (GameObject card in hand.cards)
            { 
                if (card.GetComponent<CardModel>().value == 1)
                {
                    if ((handPoints - 1 + Constants.SoftAce) == Constants.Blackjack) 
                    { 
                        return true; 
                    }
                }
            }
        }

        return false;
    }
    
    /// <summary>
    /// Check for blackjack using actual card values (not just visible cards)
    /// Used at game start when dealer's first card is face-down
    /// </summary>
    private bool BlackjackActual(GameObject whoever)
    {
        int handPoints = GetActualScore(whoever);
        return handPoints == Constants.Blackjack;
    }

    // SCORE SYSTEM 2.0: Replaced with ScoreManager
    public int GetPlayerPoints()
    {
        if (scoreManager != null)
            return scoreManager.CalculatePlayerScore();
        return GetVisibleScore(player, true); // Fallback
    }

    public int GetDealerPoints()
    {
        if (scoreManager != null)
            return scoreManager.CalculateDealerScore();
        return GetVisibleScore(dealer, false); // Fallback
    }

    private void CalculateProbabilities()
    {
        float possibleCases = values.Length - cardIndex + 1.0f;
 
        for (int i = cardIndex; i < values.Length; ++i)
        {
            if (values[i] == 1) { possibleCases++; }
        }
        
        probMessage.text = ProbabilityDealerHigher(possibleCases) + " % | " + 
            ProbabilityPlayerInBetween(possibleCases) + " % | " + 
            ProbabibilityPlayerOver() + " %";
    }
 
    private double ProbabilityDealerHigher(float possibleCases)
    {
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        List<CardModel> dealerCards = dealerHand.cards
            .Select(card => card.GetComponent<CardModel>()).ToList();

        int favorableCases = 0;
        if (dealerCards.Count > 1) 
        {
            int dealerPointsVisible = dealerCards[1].value;

            int playerPoints = GetPlayerPoints();
            int sum = 0;

            for (int i = cardIndex; i < values.Length; ++i)
            { 
                sum = dealerPointsVisible + values[i];
                if (sum < Constants.Blackjack && sum > playerPoints)
                {
                    favorableCases++;
                }
 
                if (values[i] == 1)
                {
                    sum = dealerPointsVisible + Constants.SoftAce;
                    if (sum < Constants.Blackjack && sum > playerPoints)
                    {
                        favorableCases++;
                    }
                }
 
                if (dealerPointsVisible == 1)
                {
                    sum = Constants.SoftAce + values[i];
                    if (sum < Constants.Blackjack && sum > playerPoints)
                    {
                        favorableCases++;
                    }
                }
            }
        }

        return System.Math.Round((favorableCases / possibleCases) * 100, Constants.ProbPrecision);
    }
 
    private double ProbabilityPlayerInBetween(float possibleCases)
    {
        int playerPoints = GetPlayerPoints();
        int favorableCases = 0;
        int sum = 0;

        for (int i = cardIndex; i < values.Length; ++i)
        {
            sum = playerPoints + values[i];
            if (sum >= Constants.DealerStand && sum <= Constants.Blackjack)
            {
                favorableCases++;
            }
 
            if (values[i] == 1)
            {
                sum = playerPoints + Constants.SoftAce;
                if (sum >= Constants.DealerStand && sum <= Constants.Blackjack)
                {
                    favorableCases++;
                }
            }
        }
    
        return System.Math.Round((favorableCases / possibleCases) * 100, Constants.ProbPrecision);
    }
 
    private double ProbabibilityPlayerOver()
    {
        float possibleCases = values.Length - cardIndex + 1.0f;
        int playerPoints = GetPlayerPoints();
        int favorableCases = 0;
        int sum = 0;

        for (int i = cardIndex; i < values.Length; ++i)
        {
            sum = playerPoints + values[i];
            if (sum > Constants.Blackjack) { favorableCases++; }
        }

        return System.Math.Round((favorableCases / possibleCases) * 100, Constants.ProbPrecision);
    }

    /*private void PushDealer()
    {
        // Check if we're reaching the end of the deck and need to reshuffle
        if (cardIndex >= values.Length - 1)
        {
            Debug.Log("Dealer drawing - Deck is almost empty, reshuffling...");
            ShuffleCards();
            cardIndex = 0;
        }

        GameObject newCard = dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex], originalIndices[cardIndex]);
        
        // Debug: Log what card was dealt to dealer
        CardInfo cardInfo = new CardInfo(originalIndices[cardIndex], values[cardIndex], faces[cardIndex], faces);
        Debug.Log($"Dealt to dealer: {cardInfo.cardName} (Index: {cardIndex})");
        
        cardIndex++;
        
        // Notify boss system about card dealt
        if (bossManager != null && newCard != null)
        {
            bossManager.OnCardDealt(newCard, false);
        }
    }*/

    /*private void PushPlayer()
    {
        // Check if we're reaching the end of the deck and need to reshuffle
        if (cardIndex >= values.Length - 1)
        {
            Debug.Log("Player drawing - Deck is almost empty, reshuffling...");
            ShuffleCards();
            cardIndex = 0;
        }

        // Check if The Seductress should intercept this card before it goes to the player
        if (bossManager != null && bossManager.currentBoss != null && 
            bossManager.currentBoss.bossType == BossType.TheSeductress)
        {
            CardInfo cardInfo = new CardInfo(originalIndices[cardIndex], values[cardIndex], faces[cardIndex], faces);
            bool isKingOrJack = (cardInfo.suitIndex == 10 || cardInfo.suitIndex == 12); // Jack=10, King=12
            
            Debug.Log($"Seductress check: {cardInfo.cardName} (suitIndex: {cardInfo.suitIndex}) - isKingOrJack: {isKingOrJack}");
            
            if (isKingOrJack)
            {
                Debug.Log($"The Seductress intercepts {cardInfo.cardName} before it reaches the player!");
                
                // Deal the card to the dealer instead
                GameObject interceptedCard = dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex], originalIndices[cardIndex]);
                
                cardIndex++;
                
                // Notify boss system about intercepted card
                if (bossManager != null && interceptedCard != null)
                {
                    bossManager.OnCardDealt(interceptedCard, false); // Treat as dealer card
                    // Also apply The Seductress mechanic
                    var mechanic = bossManager.currentBoss.GetMechanic(BossMechanicType.SeductressIntercept);
                    if (mechanic != null)
                    {
                        bossManager.StartCoroutine(bossManager.HandleSeductressInterception(interceptedCard, cardInfo, true));
                    }
                }
                
                UpdateScoreDisplays();
                CalculateProbabilities();
                return; // Card was intercepted, don't deal to player
            }
        }
        else
        {
            // Debug why interception didn't trigger
            if (bossManager == null)
                Debug.Log("BossManager is null!");
            else if (bossManager.currentBoss == null)
                Debug.Log("Current boss is null!");
            else
                Debug.Log($"Current boss is {bossManager.currentBoss.bossName} (type: {bossManager.currentBoss.bossType})");
        }

        GameObject playerCard = player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex], originalIndices[cardIndex]);
        
        // Debug: Log what card was dealt to player
        CardInfo playerCardInfo = new CardInfo(originalIndices[cardIndex], values[cardIndex], faces[cardIndex], faces);
        Debug.Log($"Dealt to player: {playerCardInfo.cardName} (Index: {cardIndex})");
        
        cardIndex++;
        UpdateScoreDisplays();  

        CalculateProbabilities();
        
        // Notify boss system about card dealt
        if (bossManager != null && playerCard != null)
        {
            bossManager.OnCardDealt(playerCard, true);
        }
        
        // Apply Captain's Jack nullification if this is The Captain boss
        if (bossManager != null && bossManager.currentBoss != null && 
            bossManager.currentBoss.bossType == BossType.TheCaptain)
        {
            ApplyCaptainJackNullification();
        }
    }*/
    private void PushDealer()
    {
        // Check if we're reaching the end of the deck and need to reshuffle
        if (cardIndex >= values.Length - 1)
        {
            Debug.Log("Dealer drawing - Deck is almost empty, reshuffling...");
            ShuffleCards();
            cardIndex = 0;
        }

        GameObject newCard = dealer.GetComponent<CardHand>().Push(
            faces[cardIndex], 
            values[cardIndex], 
            originalIndices[cardIndex]
        );

        // ✅ Apply frame
        CardUI cardUI = newCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.ApplyEquippedFrame();
        }

        // Debug: Log what card was dealt to dealer
        CardInfo cardInfo = new CardInfo(originalIndices[cardIndex], values[cardIndex], faces[cardIndex], faces);
        Debug.Log($"Dealt to dealer: {cardInfo.cardName} (Index: {cardIndex})");

        cardIndex++;
        
        // SCORE SYSTEM 2.0: Update scores after dealing card
        UpdateScoreDisplays();

        // Notify boss system about card dealt
        if (bossManager != null && newCard != null)
        {
            bossManager.OnCardDealt(newCard, false);
        }
    }
    private void PushPlayer()
{
    // Check if we're reaching the end of the deck and need to reshuffle
    if (cardIndex >= values.Length - 1)
    {
        Debug.Log("Player drawing - Deck is almost empty, reshuffling...");
        ShuffleCards();
        cardIndex = 0;
    }

    // Seductress interception logic
    if (bossManager != null && bossManager.currentBoss != null && 
        bossManager.currentBoss.bossType == BossType.TheSeductress)
    {
        CardInfo cardInfo = new CardInfo(originalIndices[cardIndex], values[cardIndex], faces[cardIndex], faces);
        bool isKingOrJack = (cardInfo.suitIndex == 10 || cardInfo.suitIndex == 12);

        if (isKingOrJack)
        {
            GameObject interceptedCard = dealer.GetComponent<CardHand>().Push(
                faces[cardIndex], 
                values[cardIndex], 
                originalIndices[cardIndex]
            );

            // ✅ Apply frame to intercepted card
            CardUI interceptedUI = interceptedCard.GetComponent<CardUI>();
            if (interceptedUI != null)
            {
                interceptedUI.ApplyEquippedFrame();
            }

            cardIndex++;
            
            // SCORE SYSTEM 2.0: Update scores after interception
            UpdateScoreDisplays();

            if (bossManager != null && interceptedCard != null)
            {
                bossManager.OnCardDealt(interceptedCard, false);

                var mechanic = bossManager.currentBoss.GetMechanic(BossMechanicType.SeductressIntercept);
                if (mechanic != null)
                    bossManager.StartCoroutine(bossManager.HandleSeductressInterception(interceptedCard, cardInfo, true));
            }

            UpdateScoreDisplays();
            CalculateProbabilities();
            return;
        }
    }

    // Normal case → card goes to player
    GameObject playerCard = player.GetComponent<CardHand>().Push(
        faces[cardIndex], 
        values[cardIndex], 
        originalIndices[cardIndex]
    );

    // ✅ Apply frame to player's card
    CardUI playerUI = playerCard.GetComponent<CardUI>();
    if (playerUI != null)
    {
        playerUI.ApplyEquippedFrame();
    }

    // Debug log
    CardInfo playerCardInfo = new CardInfo(originalIndices[cardIndex], values[cardIndex], faces[cardIndex], faces);
    Debug.Log($"Dealt to player: {playerCardInfo.cardName} (Index: {cardIndex})");

    cardIndex++;
    UpdateScoreDisplays();
    CalculateProbabilities();

    if (bossManager != null && playerCard != null)
    {
        bossManager.OnCardDealt(playerCard, true);
    }

    // Captain mechanic
    if (bossManager != null && bossManager.currentBoss != null && 
        bossManager.currentBoss.bossType == BossType.TheCaptain)
    {
        ApplyCaptainJackNullification();
    }
}


    public void Hit()
    { 
        Debug.Log($"=== HIT CALLED === Current turn: {_currentTurn}, Game in progress: {_gameInProgress}, Bet placed: {_isBetPlaced}");
        
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot hit: No bet placed");
            return;
        }
        
        if (_currentTurn != GameTurn.Player || !_gameInProgress)
        {
            Debug.LogWarning($"Cannot hit: Not player's turn (Current: {_currentTurn}, InProgress: {_gameInProgress})");
            return;
        }
        
        // Check hit limit
        if (_hitsThisHand >= _maxHitsPerHand)
        {
            finalMessage.text = "Maximum hits reached!";
            Debug.LogWarning($"Maximum hits reached: {_hitsThisHand}/{_maxHitsPerHand}");
            return;
        }
        
        CardHand playerHand = player.GetComponent<CardHand>();
        if (!playerHand.CanAddMoreCards())
        {
            finalMessage.text = "Maximum cards reached!";
            return;
        }

        Debug.Log("Hit conditions passed - starting turn-based hit");
        
        // Increment hit counter
        _hitsThisHand++;
        Debug.Log($"Hit count: {_hitsThisHand}/{_maxHitsPerHand}");
        
        // Update Round Flow UI
        UpdateRoundFlowUI();
        
        // After first hit, disable Double Down
        if (doubleDownButton != null)
        {
            doubleDownButton.interactable = false;
        }

        // Notify boss system about player action
        if (bossManager != null)
        {
            bossManager.OnPlayerAction();
        }

        // Start turn-based animated hit
        StartCoroutine(TurnBasedHitAnimated());
    }


    public void Stand()
    {
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot stand: No bet placed");
            return;
        }
        
        if (_currentTurn != GameTurn.Player || !_gameInProgress)
        {
            Debug.LogWarning("Cannot stand: Not player's turn");
            return;
        }
        
        // Notify boss system about player action
        if (bossManager != null)
        {
            bossManager.OnPlayerAction();
        }
        
        // Player chooses to stand - mark it and switch turns
        _playerStood = true;
        Debug.Log("Player stands");
        
        // Disable Double Down when standing
        if (doubleDownButton != null)
        {
            doubleDownButton.interactable = false;
        }
        
        // Check if both players have stood or game should end
        CheckTurnBasedGameEnd();
    }
    
    /// <summary>
    /// Double Down: Double the bet, receive exactly one more card, then stand
    /// Only available on the initial 2-card hand
    /// </summary>
    public void DoubleDown()
    {
        Debug.Log("=== DOUBLE DOWN CALLED ===");
        
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot double down: No bet placed");
            return;
        }
        
        if (_currentTurn != GameTurn.Player || !_gameInProgress)
        {
            Debug.LogWarning("Cannot double down: Not player's turn");
            return;
        }
        
        if (_hasDoubledDown)
        {
            Debug.LogWarning("Cannot double down: Already doubled down this hand");
            return;
        }
        
        // Can only double down on initial 2-card hand (before any hits)
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand.cards.Count != Constants.InitialCardsDealt || _hitsThisHand > 0)
        {
            finalMessage.text = "Can only double down on initial hand!";
            Debug.LogWarning("Cannot double down: Not on initial 2-card hand");
            return;
        }
        
        // BETTING SYSTEM 2.0: Check if player has enough health to double the bet
        if (GameProgressionManager.Instance != null)
        {
            float currentHealth = GameProgressionManager.Instance.playerHealthPercentage;
            if (CurrentBetAmount > currentHealth)
            {
                finalMessage.text = "Insufficient health to double down!";
                Debug.LogWarning($"Cannot double down: Need {CurrentBetAmount:F0}, have {currentHealth:F0}");
                return;
            }
        }
        
        Debug.Log($"Double Down: Doubling bet from {CurrentBetAmount:F0} to {(CurrentBetAmount * 2):F0}");
        
        // BETTING SYSTEM 2.0: Deduct additional bet from health (without triggering game over - that happens on loss)
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.DeductBet(CurrentBetAmount);
        }
        
        // Double the bet amount
        CurrentBetAmount *= 2;
        
        _hasDoubledDown = true;
        
        // Disable all player controls except for the final card deal
        DisablePlayerControls();
        if (doubleDownButton != null)
        {
            doubleDownButton.interactable = false;
        }
        
        Debug.Log("Double Down: Dealing one card then automatically standing");
        
        // Notify boss system
        if (bossManager != null)
        {
            bossManager.OnPlayerAction();
        }
        
        // Deal exactly one card then automatically stand
        StartCoroutine(DoubleDownAnimated());
    }
    
    /// <summary>
    /// Double Down animation: Deal one card, then automatically stand
    /// </summary>
    private IEnumerator DoubleDownAnimated()
    {
        // Deal one card with animation
        yield return StartCoroutine(PushPlayerAnimated());
        
        // Track the card
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand != null && playerHand.cards.Count > 0)
        {
            _lastHitCard = playerHand.cards[playerHand.cards.Count - 1];
            _hitsThisHand++; // Count this as a hit
        }
        
        // Check for bust or blackjack
        if (Blackjack(player, true))
        {
            EndHand(WinCode.PlayerWins);
            yield break;
        }
        else if (GetPlayerPoints() > Constants.Blackjack)
        {
            EndHand(WinCode.DealerWins);
            yield break;
        }
        
        // Automatically stand after receiving the card
        _playerStood = true;
        Debug.Log("Double Down: Automatically standing after receiving card");
        
        // Switch to dealer's turn
        SwitchToDealerTurn();
    }
    
    /// <summary>
    /// Turn-based hit animation and logic
    /// </summary>
    private IEnumerator TurnBasedHitAnimated()
    {
        // Disable controls during animation
        DisablePlayerControls();
        
        // Deal card with animation
        yield return StartCoroutine(PushPlayerAnimated());
        
        // Track the last hit card for The Escapist
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand != null && playerHand.cards.Count > 0)
        {
            _lastHitCard = playerHand.cards[playerHand.cards.Count - 1];
            Debug.Log("Tracking last hit card for The Escapist: " + (_lastHitCard?.name ?? "null"));
        }
        
        // Check for bust or blackjack
        if (Blackjack(player, true)) 
        { 
            EndHand(WinCode.PlayerWins); 
            yield break;
        }
        else if (GetPlayerPoints() > Constants.Blackjack) 
        { 
            EndHand(WinCode.DealerWins); 
            yield break;
        }
        
        // Switch to dealer's turn
        SwitchToDealerTurn();
    }
    
    /// <summary>
    /// Switch to dealer's turn
    /// </summary>
    private void SwitchToDealerTurn()
    {
        if (!_gameInProgress) return;
        
        _currentTurn = GameTurn.Dealer;
        UpdateTurnUI();
        DisablePlayerControls();
        
        // Start dealer's turn with a small delay
        StartCoroutine(DealerTurnCoroutine());
    }
    
    /// <summary>
    /// Handle dealer's turn - continues until dealer stands or busts
    /// </summary>
    private IEnumerator DealerTurnCoroutine()
    {
        yield return new WaitForSeconds(1f); // Small delay for dramatic effect
        
        if (!_gameInProgress) yield break;
        
        // Flip dealer's first card if not already flipped
        AnimateFlipDealerCard();
        yield return new WaitForSeconds(0.5f);
        
        // Dealer continues hitting until they reach 17 or bust
        while (_gameInProgress && !_dealerStood)
        {
            int dealerPoints = GetDealerPoints();
            bool dealerShouldHit = DealerShouldHit(dealerPoints);
            
            if (dealerShouldHit)
            {
                Debug.Log($"Dealer has {dealerPoints} points, hitting...");
                yield return StartCoroutine(PushDealerAnimated());
                
                // Check for dealer bust or blackjack
                dealerPoints = GetDealerPoints();
                if (dealerPoints > Constants.Blackjack)
                {
                    Debug.Log("Dealer busts!");
                    EndHand(WinCode.PlayerWins);
                    yield break;
                }
                else if (Blackjack(dealer, false))
                {
                    Debug.Log("Dealer gets blackjack!");
                    EndHand(WinCode.DealerWins);
                    yield break;
                }
                
                // Small delay between dealer hits for better UX
                yield return new WaitForSeconds(0.8f);
            }
            else
            {
                Debug.Log($"Dealer has {dealerPoints} points, standing");
                _dealerStood = true;
                break;
            }
        }
        
        // Check if game should end
        CheckTurnBasedGameEnd();
    }
    
    /// <summary>
    /// Dealer AI logic - decides whether dealer should hit
    /// </summary>
    private bool DealerShouldHit(int dealerPoints)
    {
        // Standard dealer rules: hit on 16 or less, stand on 17 or more
        // This ensures fair play - dealer must follow the same rules as in real blackjack
        return dealerPoints < Constants.DealerStand;
    }
    
    /// <summary>
    /// Check if the turn-based game should end
    /// </summary>
    private void CheckTurnBasedGameEnd()
    {
        if (!_gameInProgress) return;
        
        // If both players have stood, determine winner
        if (_playerStood && _dealerStood)
        {
            DetermineWinner();
            return;
        }
        
        // If only player stood, continue with dealer turns until dealer stands or busts
        if (_playerStood && !_dealerStood)
        {
            SwitchToDealerTurn();
            return;
        }
        
        // If only dealer stood, switch back to player
        if (_dealerStood && !_playerStood)
        {
            SwitchToPlayerTurn();
            return;
        }
        
        // If neither has stood, switch back to player
        if (!_playerStood && !_dealerStood)
        {
            SwitchToPlayerTurn();
        }
    }
    
    /// <summary>
    /// Switch back to player's turn
    /// </summary>
    private void SwitchToPlayerTurn()
    {
        if (!_gameInProgress) return;
        
        _currentTurn = GameTurn.Player;
        UpdateTurnUI();
        EnablePlayerControls();
    }
    
    /// <summary>
    /// Determine winner when both players have finished
    /// </summary>
    private void DetermineWinner()
    {
        int playerPoints = GetPlayerPoints();
        int dealerPoints = GetDealerPoints();
        
        Debug.Log($"Determining winner: Player={playerPoints}, Dealer={dealerPoints}");
        
        if (playerPoints > Constants.Blackjack)
        {
            EndHand(WinCode.DealerWins); // Player bust
        }
        else if (dealerPoints > Constants.Blackjack)
        {
            EndHand(WinCode.PlayerWins); // Dealer bust
        }
        else if (playerPoints > dealerPoints)
        {
            EndHand(WinCode.PlayerWins); // Player higher
        }
        else if (dealerPoints > playerPoints)
        {
            EndHand(WinCode.DealerWins); // Dealer higher
        }
        else
        {
            EndHand(WinCode.Draw); // Tie
        }
    }


    public void FlipDealerCard()
    {
        dealer.GetComponent<CardHand>().FlipFirstCard();
        UpdateScoreDisplays(); 
    }

    private void AnimateFlipDealerCard()
    {
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        if (dealerHand.cards.Count > 0)
        {
            GameObject firstCard = dealerHand.cards[0];
            
            // Create a flip animation
            Sequence flipSequence = DOTween.Sequence();
            
            // Scale down on Y axis (flip effect)
            flipSequence.Append(firstCard.transform.DOScaleY(0, 0.1f).SetEase(Ease.InQuart));
            
            // Change the card face at the middle of the flip
            flipSequence.AppendCallback(() => {
                dealerHand.FlipFirstCard();
                UpdateScoreDisplays();
            });
            
            // Scale back up
            flipSequence.Append(firstCard.transform.DOScaleY(firstCard.transform.localScale.y, 0.1f).SetEase(Ease.OutQuart));
        }
        else
        {
            // Fallback to regular flip if no cards
            FlipDealerCard();
        }
    }

private void EndHand(WinCode code)
{
    // End turn-based game
    _gameInProgress = false;
    _currentTurn = GameTurn.GameOver;
    
    FlipDealerCard();
    // BETTING SYSTEM 2.0: Track health for history instead of balance
    float oldHealth = GameProgressionManager.Instance != null ? GameProgressionManager.Instance.playerHealthPercentage : 0f;

    int playerScore = GetPlayerPoints();
    int dealerScore = GetDealerPoints();
    string outcomeText = "";
    float winLossAmount = 0f; // Track win/loss amount for history V2
    float totalWinnings = 0f; // Track total winnings for Win case

    switch (code)
    {
        case WinCode.DealerWins:
            // BETTING SYSTEM 2.0: Player loses - bet was already deducted when placed
            // Additional damage may be applied by special boss mechanics
            
            finalMessage.text = "You lose!";
            finalMessage.gameObject.SetActive(true);  
            outcomeText = "Lose";
            
            // Check if The Chiromancer is active and apply special damage mechanics
            float additionalDamage = 0f;
            if (bossManager != null && bossManager.currentBoss != null &&
                bossManager.currentBoss.bossType == BossType.TheChiromancer)
            {
                Debug.Log("The Chiromancer is active - applying special damage mechanics!");
                if (dealerScore == Constants.Blackjack)
                {
                    // Chiromancer wins with 21 - takes additional 1x bet damage (total 2x)
                    additionalDamage = CurrentBetAmount;
                    finalMessage.text += $"\nChiromancer wins with 21! Additional {additionalDamage:F0} damage!";
                    Debug.Log($"Chiromancer wins with 21 - additional damage: {additionalDamage}");
                }
                else
                {
                    // Chiromancer wins normally - takes additional 0.5x bet damage (total 1.5x)
                    additionalDamage = CurrentBetAmount * 0.5f;
                    finalMessage.text += $"\nChiromancer takes extra {additionalDamage:F0} damage!";
                    Debug.Log($"Chiromancer wins normally - additional damage: {additionalDamage}");
                }
                
                // Apply additional damage
                if (GameProgressionManager.Instance != null && additionalDamage > 0)
                {
                    GameProgressionManager.Instance.DamagePlayer(additionalDamage);
                }
            }
            
            // Witch Doctor refund (heal back 10% of bet)
            if (PlayerActuallyHasCard(TarotCardType.WitchDoctor) && PlayerHasActivatedCard(TarotCardType.WitchDoctor))
            {
                float refund = CurrentBetAmount * 0.1f;
                if (GameProgressionManager.Instance != null)
                {
                    GameProgressionManager.Instance.HealPlayer(refund);
                }
                Debug.Log($"Witch Doctor refunded 10% of your bet: {refund:F1}");
            }

            _currentStreak = 0;
            _streakMultiplier = 0;

            Debug.Log($"Loss: Bet was {CurrentBetAmount}, Additional Damage={additionalDamage}");
            winLossAmount = -CurrentBetAmount; // Negative for losses
            
            // Notify GameProgressionManager about player loss (SINGLE SOURCE OF TRUTH)
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.OnPlayerLoseRound();
            }
            
            // Notify boss system about player loss (for mechanics only, state is in GameProgressionManager)
            if (bossManager != null)
            {
                bossManager.OnPlayerLose();
                // New boss panel updates automatically via BossManager events
            }
            break;

        case WinCode.PlayerWins:
            // BETTING SYSTEM 2.0: Player wins - heal back bet + winnings
            _currentStreak++;
            _streakMultiplier = Mathf.Min(_currentStreak / Constants.StreakMultiplierIncrement, Constants.MaxStreakLevel);
            float multiplier = CalculateWinMultiplier();
            float suitBonuses = CalculateSuitBonusesAsPercentage(player);

            // Check if player has natural blackjack (21 with exactly 2 cards) for 3:2 payout
            CardHand playerHand = player.GetComponent<CardHand>();
            bool isNaturalBlackjack = (playerScore == Constants.Blackjack && 
                                     playerHand != null && 
                                     playerHand.cards.Count == Constants.InitialCardsDealt &&
                                     _hitsThisHand == 0);
            
            // Calculate base profit: 1x bet for regular win, 1.5x bet for blackjack (3:2 payout)
            float profitMultiplier = isNaturalBlackjack ? 1.5f : 1.0f;
            float baseProfit = CurrentBetAmount * profitMultiplier;
            // Streak bonus must be calculated on baseProfit to ensure blackjack gets 1.5x streak bonus
            // This makes streak bonuses proportionally higher for blackjack (1.5x) vs regular wins (1.0x)
            float streakBonus = baseProfit * (multiplier - 1.0f);
            
            // Total winnings = bet refund + profit + bonuses
            // Since bet was already deducted, we need to refund it + give profit + bonuses
            totalWinnings = CurrentBetAmount + baseProfit + streakBonus + suitBonuses;
            float netHeal = baseProfit + streakBonus + suitBonuses; // Net gain (excluding bet refund)
            winLossAmount = totalWinnings; // Positive for wins

            finalMessage.text = isNaturalBlackjack ? "Blackjack! You win!" : "You win!";
            if (isNaturalBlackjack)
            {
                finalMessage.text += "\n3:2 Payout!";
            }
            if (_currentStreak > 1)
            {
                finalMessage.text += "\nStreak: " + _currentStreak + " (" + multiplier.ToString("0.0") + "x bonus)";
            }
            if (suitBonuses > 0)
            {
                finalMessage.text += $"\nSuit Bonus: +{suitBonuses:F0}";
            }
            finalMessage.gameObject.SetActive(true); // ✅ Show result
            outcomeText = isNaturalBlackjack ? "Blackjack" : "Win";

            // Heal player with winnings
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.HealPlayer(totalWinnings);
            }

            Debug.Log($"Win calculation: Bet={CurrentBetAmount}, IsBlackjack={isNaturalBlackjack}, " +
                     $"Profit Multiplier={profitMultiplier:F2}x, Base Profit={baseProfit}, " +
                     $"Streak Bonus={streakBonus}, Suit Bonuses={suitBonuses}, " +
                     $"Total Heal={totalWinnings}, Net Heal={netHeal}, " +
                     $"Streak Multiplier={multiplier:F2}x");
            
            // Notify GameProgressionManager about player win (SINGLE SOURCE OF TRUTH)
            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.OnPlayerWinRound();
            }
            
            // Notify boss system about player win (for mechanics only, state is in GameProgressionManager)
            if (bossManager != null)
            {
                bossManager.OnPlayerWin();
                // New boss panel updates automatically via BossManager events
            }
            break;

        case WinCode.Draw:
            // BETTING SYSTEM 2.0: Draw - refund bet to player (heal back what was bet)
            finalMessage.text = "Draw!";
            finalMessage.gameObject.SetActive(true); // ✅ Show result
            outcomeText = "Draw";

            if (GameProgressionManager.Instance != null)
            {
                GameProgressionManager.Instance.HealPlayer(CurrentBetAmount);
            }

            Debug.Log($"Draw: Refunded bet amount {CurrentBetAmount} back to player");
            winLossAmount = 0f; // Draw = 0 (bet was refunded)

            _currentStreak = 0;
            _streakMultiplier = 0;
            break;

        default:
            Debug.Assert(false);
            break;
    }

    // GAME HISTORY 2.0: Update game history with boss data and win/loss amounts
    // Try to get singleton instance if reference is not set
    GameHistoryManagerV2 historyManager = gameHistoryManagerV2 != null ? gameHistoryManagerV2 : GameHistoryManagerV2.Instance;
    
    if (historyManager != null)
    {
        BossData currentBoss = bossManager != null && bossManager.currentBoss != null ? bossManager.currentBoss : null;
        
        GameHistoryEntryV2 historyEntryV2 = new GameHistoryEntryV2(
            currentBoss,
            winLossAmount,
            outcomeText,
            playerScore,
            dealerScore
        );
        Debug.Log($"[Deck] Recording history entry V2: Boss={historyEntryV2.GetBossName()}, Outcome: {outcomeText}, Win/Loss: {winLossAmount:F0} SOL, PlayerScore: {playerScore}, DealerScore: {dealerScore}");
        historyManager.AddHistoryEntry(historyEntryV2);
    }
    else if (gameHistoryManager != null)
    {
        // Fallback to old system if V2 is not available
        string currentBossName = bossManager != null && bossManager.currentBoss != null ? bossManager.currentBoss.bossName : "Unknown Boss";
        float newHealth = GameProgressionManager.Instance != null ? GameProgressionManager.Instance.playerHealthPercentage : 0f;
        
        // Convert float health to uint for history (multiply by 10 to preserve decimal)
        GameHistoryEntry historyEntry = new GameHistoryEntry(
            1, // Hand number (boss system doesn't use rounds)
            currentBossName,
            playerScore,
            dealerScore,
            (uint)(CurrentBetAmount * 10), // Convert percentage to uint (5.5% = 55)
            (uint)(oldHealth * 10),
            (uint)(newHealth * 10),
            outcomeText
        );
        Debug.Log($"Recording history entry (old system): Boss={currentBossName}, Outcome: {outcomeText}, Bet: {CurrentBetAmount:F0}");
        gameHistoryManager.AddHistoryEntry(historyEntry);
    }
    else
    {
        Debug.LogWarning("GameHistoryManagerV2 and GameHistoryManager are both null! Cannot record history entry.");
    }

    UpdateStreakUI();

    float newHealth2 = GameProgressionManager.Instance != null ? GameProgressionManager.Instance.playerHealthPercentage : 0f;
    Debug.Log($"Hand ended: {code} - Old health: {oldHealth:F1}, New health: {newHealth2:F1}, " +
              $"Bet: {CurrentBetAmount:F0}, Health change: {(newHealth2 - oldHealth):F1}, " +
              $"Current streak: {_currentStreak}, Multiplier: {CalculateWinMultiplier():F2}x");

    // Move cards from hands to discard pile (post-hand update)
    MoveHandsToDiscardPile();
    UpdateRoundFlowUI(); // Update discard pile count display

    hitButton.interactable = false;
    stickButton.interactable = false;
    discardButton.interactable = false;
    peekButton.interactable = false;
    transformButton.interactable = false;
    // BETTING SYSTEM 2.0: Removed betting button disabling - handled by BettingManager
    if (doubleDownButton != null)
    {
        doubleDownButton.interactable = false;
    }

    playAgainButton.interactable = true;

    // BETTING SYSTEM 2.0: Reset bet amount
    CurrentBetAmount = 0f;
    // BETTING SYSTEM 2.0: Removed bet display and balance display updates
    UpdateScoreDisplays();

    // BETTING SYSTEM 2.0: Check for game over conditions - health depleted or boss defeated
    bool isGameOver = false;
    if (GameProgressionManager.Instance != null)
    {
        isGameOver = (GameProgressionManager.Instance.playerHealthPercentage <= 0) || (bossManager != null && bossManager.IsGameOver());
    }
    
    if (isGameOver)
    {
        if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.playerHealthPercentage <= 0)
        {
            finalMessage.text += "\n - GAME OVER (No Money) -";
        }
        else if (bossManager != null && bossManager.IsGameOver())
        {
            if (bossManager.GetRemainingHands() <= 0 && bossManager.GetCurrentBossHealth() > 0)
            {
                finalMessage.text += "\n - GAME OVER (No Hands Left) -";
            }
            else if (bossManager.GetCurrentBossHealth() <= 0)
            {
                finalMessage.text += "\n - BOSS DEFEATED! -";
            }
        }
        
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            // NEW FLOW: Return to Boss Map instead of restarting
            buttonText.text = "Return to Map";
        }
    }
    
    // Return stolen cards for The Naughty Child after each hand
    if (bossManager != null)
    {
        bossManager.ReturnNaughtyChildStolenCards();
    }
}

    public void PlayAgain()
    {   
        // BETTING SYSTEM 2.0: Check if this is a game over scenario
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        bool playerHealthDepleted = (GameProgressionManager.Instance != null && GameProgressionManager.Instance.playerHealthPercentage <= 0);
        bool isGameOver = playerHealthDepleted || 
                         (bossManager != null && bossManager.IsGameOver()) ||
                         (buttonText != null && (buttonText.text == "Play Again" || buttonText.text == "Restart Game"));
        
        bool isReturnToMap = (buttonText != null && buttonText.text == "Return to Map");
        bool isNextBoss = (buttonText != null && buttonText.text == "Next Boss");
        
        if (isReturnToMap)
        {
            // NEW FLOW: Return to BossMap scene
            Debug.Log("[Deck] Returning to Boss Map...");
            
            if (GameSceneManager.Instance != null)
            {
                GameSceneManager.Instance.LoadBossMapScene();
            }
            else
            {
                // Fallback
                UnityEngine.SceneManagement.SceneManager.LoadScene("BossMap");
            }
            return;
        }
        else if (isGameOver)
        {
            // Full game restart - reset everything except inventory
            RestartGame();
            return;
        }
        else if (isNextBoss)
        {
            // Transitioning to next boss - don't restart, just prepare for next boss
            Debug.Log("Transitioning to next boss...");
            
            // Clear final message
            finalMessage.text = "";
            finalMessage.gameObject.SetActive(false);
            
            // Reset button text
            if (buttonText != null)
            {
                buttonText.text = "Next Hand";
            }
            
            // The boss initialization will be handled by BossManager's DelayedBossInitialization
            // Just wait for it to complete and then initialize betting state
            StartCoroutine(WaitForBossInitialization());
            return;
        }
        
        // Show boss transition animation
        ShowBossTransition();
        
        // BETTING SYSTEM 2.0: Reset bet for new round
        CurrentBetAmount = 0f;
        
        // Clear hand
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();          
        cardIndex = 0;
        
        // Reset tarot cards
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.ResetTarotCardsForNewRound();
        }
        
        // Reset new tarot card usage tracking
        _hasUsedSpyThisRound = false;
        _hasUsedBlindSeerThisRound = false;
        _hasUsedCorruptJudgeThisRound = false;
        _hasUsedHitmanThisRound = false;
        _hasUsedFortuneTellerThisRound = false;
        _hasUsedMadWriterThisRound = false;
        
        // Reset passive card activation tracking
        _hasActivatedBotanistThisRound = false;
        _hasActivatedAssassinThisRound = false;
        _hasActivatedSecretLoverThisRound = false;
        _hasActivatedJewelerThisRound = false;
        _hasActivatedHouseKeeperThisRound = false;
        _hasActivatedWitchDoctorThisRound = false;
        _hasActivatedArtificerThisRound = false;
        
        // Reset The Escapist tracking
        _lastHitCard = null;
        
        // Re-apply Naughty Child consumable hiding for new round
        if (bossManager != null && bossManager.currentBoss != null && 
            bossManager.currentBoss.bossType == BossType.TheNaughtyChild)
        {
            // Re-hide consumables at the start of each new round
            StartCoroutine(ReHideConsumablesAfterDelay());
        }
        
        ShuffleCards();
        UpdateStreakUI(); // Update streak UI for new round
        
        // Go back to betting state instead of immediately starting game
        InitializeBettingState();  
    }
    
    /// <summary>
    /// Called by BossManager when game over conditions are met
    /// </summary>
    public void OnGameOver(bool playerWon, bool bossDefeated)
    {
        Debug.Log($"OnGameOver called - Player won: {playerWon}, Boss defeated: {bossDefeated}");
        
        // Disable all game buttons
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        // BETTING SYSTEM 2.0: Removed betting button disabling - handled by BettingManager
        
        // Enable restart button
        playAgainButton.interactable = true;
        
        // Update button text
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = "Restart Game";
        }
        
        // Show appropriate game over message
        if (bossDefeated)
        {
            finalMessage.text = "Boss Defeated! Well done!";
        }
        else if (bossManager != null && bossManager.GetRemainingHands() <= 0)
        {
            finalMessage.text = "No hands remaining! Game Over!";
        }
        else
        {
            finalMessage.text = "Game Over!";
        }
        
        finalMessage.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Called by BossManager when transitioning to next boss (not game over)
    /// </summary>
    public void OnBossTransition()
    {
        Debug.Log("OnBossTransition called - preparing for next boss");
        
        // Enable next hand button (not restart)
        playAgainButton.interactable = true;
        
        // Update button text to show next round
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = "Next Boss";
        }
        
        // Show transition message
        finalMessage.text = "Boss Defeated! Preparing for next boss...";
        finalMessage.gameObject.SetActive(true);
        
        // Disable game action buttons until next boss starts
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        // BETTING SYSTEM 2.0: Removed betting button disabling - handled by BettingManager
    }
    
    /// <summary>
    /// Restart the entire game while preserving inventory
    /// </summary>
    private void RestartGame()
    {
        Debug.Log("Restarting game - preserving inventory");
        
        // BETTING SYSTEM 2.0: Reset health to full
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.RestorePlayerHealth();
        }
        
        // Reset boss system
        if (bossManager != null)
        {
            bossManager.ResetGameState();
        }
        
        // Reset streak
        _currentStreak = 0;
        _streakMultiplier = 0;
        
        // BETTING SYSTEM 2.0: Reset bet
        CurrentBetAmount = 0f;
        
        // Clear hands
        if (player != null && player.GetComponent<CardHand>() != null)
        {
            player.GetComponent<CardHand>().Clear();
        }
        if (dealer != null && dealer.GetComponent<CardHand>() != null)
        {
            dealer.GetComponent<CardHand>().Clear();
        }
        
        // Reset deck
        cardIndex = 0;
        ShuffleCards();
        
        // Clear discard pile for new game
        ClearDiscardPile();
        
        // Reset Round Flow tracking
        _hitsThisHand = 0;
        _actionsRemainingThisHand = _maxActionsPerHand;
        _tarotsUsedThisHand = 0;
        _hasDoubledDown = false;
        ResetBetRange(); // Reset bet range to defaults
        
        // Reset all round-based tracking
        _hasUsedPeekThisRound = false;
        _hasUsedTransformThisRound = false;
        _hasUsedSpyThisRound = false;
        _hasUsedBlindSeerThisRound = false;
        _hasUsedCorruptJudgeThisRound = false;
        _hasUsedHitmanThisRound = false;
        _hasUsedFortuneTellerThisRound = false;
        _hasUsedMadWriterThisRound = false;
        _hasActivatedBotanistThisRound = false;
        _hasActivatedAssassinThisRound = false;
        _hasActivatedSecretLoverThisRound = false;
        _hasActivatedJewelerThisRound = false;
        _hasActivatedHouseKeeperThisRound = false;
        _hasActivatedWitchDoctorThisRound = false;
        _hasActivatedArtificerThisRound = false;
        _lastHitCard = null;
        
        // Reset tarot cards for new game
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.ResetTarotCardsForNewRound();
        }
        
        // Update UI
        UpdateStreakUI();
        // BETTING SYSTEM 2.0: Removed UpdateBalanceDisplay() - handled by BettingManager
        
        // Clear final message and reset button text
        finalMessage.text = "";
        finalMessage.gameObject.SetActive(false);
        
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = "Next Hand";
        }
        
        // Initialize betting state
        InitializeBettingState();
        
        // Initialize action cards
        SpawnActionCards();
        
        // Setup reshuffle button
        if (reshuffleButton != null)
        {
            reshuffleButton.onClick.AddListener(ReshuffleDiscardPileIntoDeck);
            reshuffleButton.gameObject.SetActive(false); // Hidden by default
        }
        
        Debug.Log("Game restarted successfully");
    }
    
    /// <summary>
    /// Public method to restart game from menu (preserves inventory)
    /// </summary>
    public void RestartGameFromMenu()
    {
        Debug.Log("RestartGameFromMenu called from pause menu");
        
        // Call the private restart method
        RestartGame();
    }
    
    /// <summary>
    /// Wait for boss initialization to complete before starting betting
    /// </summary>
    private IEnumerator WaitForBossInitialization()
    {
        Debug.Log("Waiting for boss initialization...");
        
        // Wait a moment for boss initialization to complete
        yield return new WaitForSeconds(1f);
        
        // BETTING SYSTEM 2.0: Reset bet for new boss
        CurrentBetAmount = 0f;
        
        // Clear hands
        if (player != null && player.GetComponent<CardHand>() != null)
        {
            player.GetComponent<CardHand>().Clear();
        }
        if (dealer != null && dealer.GetComponent<CardHand>() != null)
        {
            dealer.GetComponent<CardHand>().Clear();
        }
        
        // Reset deck
        cardIndex = 0;
        ShuffleCards();
        
        // Reset round-based tracking for new boss
        _hasUsedPeekThisRound = false;
        _hasUsedTransformThisRound = false;
        _hasUsedSpyThisRound = false;
        _hasUsedBlindSeerThisRound = false;
        _hasUsedCorruptJudgeThisRound = false;
        _hasUsedHitmanThisRound = false;
        _hasUsedFortuneTellerThisRound = false;
        _hasUsedMadWriterThisRound = false;
        _hasActivatedBotanistThisRound = false;
        _hasActivatedAssassinThisRound = false;
        _hasActivatedSecretLoverThisRound = false;
        _hasActivatedJewelerThisRound = false;
        _hasActivatedHouseKeeperThisRound = false;
        _hasActivatedWitchDoctorThisRound = false;
        _hasActivatedArtificerThisRound = false;
        _lastHitCard = null;
        
        // Reset tarot cards for new boss
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.ResetTarotCardsForNewRound();
        }
        
        // Update UI
        UpdateStreakUI();
        
        // Initialize betting state for new boss
        InitializeBettingState();
        
        Debug.Log("Boss transition complete - ready for new boss");
    }
    
    /// <summary>
    /// Coroutine to re-hide consumables after a short delay for Naughty Child boss
    /// </summary>
    private IEnumerator ReHideConsumablesAfterDelay()
    {
        // Wait a moment for the round to properly start
        yield return new WaitForSeconds(1f);
        
        // Re-apply the hide consumables mechanic
        if (bossManager != null && bossManager.currentBoss != null && 
            bossManager.currentBoss.bossType == BossType.TheNaughtyChild)
        {
            var hideConsumablesMechanic = bossManager.currentBoss.mechanics.Find(m => m.mechanicType == BossMechanicType.HideConsumables);
            if (hideConsumablesMechanic != null)
            {
                bossManager.ApplyMechanic(hideConsumablesMechanic);
            }
        }
    }

    // BETTING SYSTEM 2.0: Removed old betting methods (RaiseBet, LowerBet, PlaceBet)
    // Now handled by BettingManager
    
    /// <summary>
    /// BETTING SYSTEM 2.0: Start betting round with bet amount from BettingManager
    /// Called by BettingManager after bet is placed
    /// </summary>
    public void StartBettingRound(float betAmount)
    {
        // Set the current bet amount
        CurrentBetAmount = betAmount;
        
        // Mark bet as placed
        _isBetPlaced = true;
        
        // Clear the betting message
        finalMessage.text = "";
        
        Debug.Log($"[Deck] Betting round started with bet amount: {betAmount}");
        
        // Start the game
        StartGame();
    }


    IEnumerator NewGame(bool withDelay = true)
    {   
        if (withDelay)
        {
            yield return new WaitForSeconds(Constants.NewGameCountdown);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private int GetVisibleScore(GameObject handOwner, bool isPlayer)
    {
        CardHand hand = handOwner.GetComponent<CardHand>();
        int visibleScore = 0;
        int aces = 0;

        foreach (GameObject cardGO in hand.cards)
        {
            CardModel cardModel = cardGO.GetComponent<CardModel>(); 
            Image cardImage = cardGO.GetComponent<Image>();

            if (cardImage.sprite == cardModel.cardFront)
            {
                if (cardModel.value == 1) // Ace
                {
                    aces++;
                    visibleScore += Constants.SoftAce; // Add as 11 for now
                }
                else
                {
                    visibleScore += cardModel.value;
                }
            }
            // If it's the dealer and the card is the first one and not yet flipped (showing cardBack),
            // and we are calculating for display purposes (not final hand), it's 0.
            // However, the logic above (sprite == cardFront) correctly handles this.
        }

        // Adjust for Aces: if score > 21 and there are Aces, convert Aces from 11 to 1
        while (visibleScore > Constants.Blackjack && aces > 0)
        {
            visibleScore -= (Constants.SoftAce - 1); // Subtract 10 (11 - 1)
            aces--;
        }
        return visibleScore;
    }
    
    /// <summary>
    /// Get actual score of all cards regardless of visibility (for blackjack checking)
    /// </summary>
    private int GetActualScore(GameObject handOwner)
    {
        CardHand hand = handOwner.GetComponent<CardHand>();
        int actualScore = 0;
        int aces = 0;

        foreach (GameObject cardGO in hand.cards)
        {
            CardModel cardModel = cardGO.GetComponent<CardModel>();
            
            if (cardModel.value == 1) // Ace
            {
                aces++;
                actualScore += Constants.SoftAce; // Add as 11 for now
            }
            else
            {
                actualScore += cardModel.value;
            }
        }

        // Adjust for Aces: if score > 21 and there are Aces, convert Aces from 11 to 1
        while (actualScore > Constants.Blackjack && aces > 0)
        {
            actualScore -= (Constants.SoftAce - 1); // Subtract 10 (11 - 1)
            aces--;
        }
        return actualScore;
    }
 
    void Update()
    { 
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand != null)
        {
            bool hasSelectedCard = playerHand.HasSelectedCard();
             
            if (discardButton != null && discardButton.interactable != hasSelectedCard)
            {
                UpdateDiscardButtonState();
                 
                if (hasSelectedCard)
                {
                    Debug.Log("Card selected - Discard button should be enabled: " + hasSelectedCard);
                }
            }
        }
        
        if (peekButton != null && !_isPeeking)
        {
            UpdatePeekButtonState();
        }
        
        if (transformButton != null)
        {
            UpdateTransformButtonState();
        }
    }
    
    // SCORE SYSTEM 2.0: Replaced with ScoreManager
    public void UpdateScoreDisplays()
    {
        if (scoreManager != null)
        {
            scoreManager.UpdateAllScores();
        }
        else
        {
            // Fallback for old system (commented out text references)
            // playerScoreText.text = "Score: " + GetVisibleScore(player, true);
            // dealerScoreText.text = "Score: " + GetVisibleScore(dealer, false);
            Debug.LogWarning("[Deck] ScoreManager not assigned! Assign it in Inspector.");
        }
    }
    
    // Make UpdateDiscardButtonState public so it can be called from CardModel
    public void UpdateDiscardButtonState()
    {
        if (discardButton != null)
        {
            bool hasSelectedCard = player.GetComponent<CardHand>().HasSelectedCard();
            discardButton.interactable = (_isBetPlaced && hasSelectedCard);
            
            // Visual feedback on button
            var buttonImage = discardButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (discardButton.interactable)
                {
                    buttonImage.color = Color.white;
                }
                else
                {
                    buttonImage.color = new Color(0.7f, 0.7f, 0.7f);
                }
            }
            
            if (hasSelectedCard)
            {
                Debug.Log("Card is selected - Discard button enabled: " + discardButton.interactable);
            }
        }
    }
    
    // This method should be explicitly assigned to the Discard button's onClick event in the Inspector
    public void DiscardSelectedCard()
    {
        Debug.Log("DiscardSelectedCard method called");
        
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot discard: No bet placed");
            return;
        }
        
        CardHand playerHand = player.GetComponent<CardHand>();
        
        if (!playerHand.HasSelectedCard())
        {
            Debug.LogWarning("Cannot discard: No card selected");
            return;
        }
         
        Debug.Log("Discarding card...");
         
        playerHand.DiscardSelectedCard();
         
        UpdateDiscardButtonState();
         
        int playerPoints = GetPlayerPoints();
        Debug.Log("Player points after discard: " + playerPoints);
        
        if (playerPoints <= Constants.Blackjack)
        {
            hitButton.interactable = true;
            stickButton.interactable = true;
        }
    }
    
    // Update peek button state
    public void UpdatePeekButtonState()
    {
        if (peekButton != null)
        {
            // Only enable if: bet placed, game is active, not currently peeking, and hasn't used peek this round
            peekButton.interactable = (_isBetPlaced && hitButton.interactable && !_isPeeking && !_hasUsedPeekThisRound);
            
            // Visual feedback on button
            var buttonImage = peekButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (peekButton.interactable)
                {
                    buttonImage.color = Color.white;
                }
                else
                {
                    buttonImage.color = new Color(0.7f, 0.7f, 0.7f);
                }
            }
        }
    }
    
    // Peek at dealer's card functionality
    /// <summary>
    /// Toggle the deck inspector panel visibility
    /// </summary>
    public void ToggleDeckInspector()
    {
        if (deckInspectorPanel != null)
        {
            deckInspectorPanel.TogglePanel();
        }
        else
        {
            Debug.LogWarning("[Deck] DeckInspectorPanel not assigned!");
        }
    }
    
    /// <summary>
    /// Open the deck inspector panel
    /// </summary>
    public void OpenDeckInspector()
    {
        if (deckInspectorPanel != null)
        {
            deckInspectorPanel.OpenPanel();
        }
    }
    
    /// <summary>
    /// Close the deck inspector panel
    /// </summary>
    public void CloseDeckInspector()
    {
        if (deckInspectorPanel != null)
        {
            deckInspectorPanel.ClosePanel();
        }
    }
    
    public void PeekAtDealerCard()
    {
        Debug.Log("PeekAtDealerCard method called");
        
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot peek: No bet placed");
            return;
        }
        
        if (_isPeeking || _hasUsedPeekThisRound)
        {
            Debug.LogWarning("Cannot peek: Already peeking or already used peek this round");
            return;
        }
        
        _hasUsedPeekThisRound = true; // Mark peek as used for this round
        Debug.Log("Peeking at dealer card...");
        UpdatePeekButtonState();
        UpdateDiscardButtonState();
        
        // Temporarily flip the dealer's card
        StartCoroutine(PeekCoroutine());
    }
    
    private IEnumerator PeekCoroutine()
    {
        _isPeeking = true;
        peekButton.interactable = false;
        
        Debug.Log("Starting peek coroutine");
        
        // Get dealer's hand
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        
        // Save current state
        List<GameObject> cards = dealerHand.cards;
        if (cards.Count > 0)
        {
            GameObject firstCard = cards[0];
            CardModel cardModel = firstCard.GetComponent<CardModel>();
            Image spriteRenderer = firstCard.GetComponent<Image>();
            
            // Store original sprite
            Sprite originalSprite = spriteRenderer.sprite;
            bool wasShowingBack = (originalSprite == cardModel.cardBack);
            
            Debug.Log("Card was showing " + (wasShowingBack ? "back" : "front"));
            
            // If card is showing back, flip it to show front
            if (wasShowingBack)
            {
                spriteRenderer.sprite = cardModel.cardFront;
                Debug.Log("Flipped card to show front");
                UpdateScoreDisplays();
            }
            
            // Wait for the peek duration
            yield return new WaitForSeconds(Constants.PeekDuration);
            
            Debug.Log("Peek duration finished, checking if game is still in progress");
            
            // If game is still in progress and card was originally showing back, flip it back
            if (hitButton.interactable && wasShowingBack)
            {
                spriteRenderer.sprite = cardModel.cardBack;
                Debug.Log("Flipped card back to show back");
                UpdateScoreDisplays();
            }
            else
            {
                Debug.Log("Not flipping card back: " + 
                    (!hitButton.interactable ? "game has ended" : "card was already showing front"));
            }
        }
        else
        {
            Debug.LogWarning("No dealer cards to peek at");
            yield return new WaitForSeconds(Constants.PeekDuration);
        }
        
        _isPeeking = false;
        UpdatePeekButtonState();
        Debug.Log("Peek coroutine completed");
    }

    // NEW TAROT CARD METHODS FOR PREVIEW FUNCTIONALITY
    
    /// <summary>
    /// The Spy - Allows to peek at the next enemy card (dealer's next card)
    /// </summary>
    public void UseSpyCard()
    {
        if (_hasUsedSpyThisRound || !_isBetPlaced)
        {
            Debug.Log("Spy card already used this round or no bet placed");
            return;
        }
        
        _hasUsedSpyThisRound = true;
        
        // Get the next card that would be dealt to the dealer
        if (cardIndex < values.Length)
        {
            CardInfo nextCard = GetCardInfo(cardIndex);
            List<CardInfo> previewCards = new List<CardInfo> { nextCard };
            
                    if (cardPreviewManager != null)
        {
            Debug.Log("CardPreviewManager found! Showing preview for: " + nextCard.cardName);
            cardPreviewManager.ShowPreview(
                previewCards,
                "The Spy - Next Dealer Card",
                false, // No rearranging
                false, // No removing
                0,
                null, // No confirm callback needed
                null  // No cancel callback needed
            );
        }
        else
        {
            Debug.LogWarning("CardPreviewManager not found! Check if it's assigned in Deck component.");
        }
        }
        else
        {
            Debug.Log("No more cards in deck");
        }
    }
    
    /// <summary>
    /// The Blind Seer - Allows to see the next 2 cards to be dealt from the deck
    /// </summary>
    public void UseBlindSeerCard()
    {
        if (_hasUsedBlindSeerThisRound || !_isBetPlaced)
        {
            Debug.Log("Blind Seer card already used this round or no bet placed");
            return;
        }
        
        _hasUsedBlindSeerThisRound = true;
        
        // Get next 2 cards from deck
        List<CardInfo> nextCards = new List<CardInfo>();
        int cardsToShow = Mathf.Min(2, values.Length - cardIndex);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            if (cardIndex + i < values.Length)
            {
                nextCards.Add(GetCardInfo(cardIndex + i));
            }
        }
        
        if (nextCards.Count > 0 && cardPreviewManager != null)
        {
            cardPreviewManager.ShowPreview(
                nextCards,
                "The Blind Seer - Next Two Cards",
                false, // No rearranging
                false, // No removing
                0,
                null, // No confirm callback needed
                null  // No cancel callback needed
            );
        }
        else
        {
            Debug.Log("No more cards in deck to reveal");
        }
    }
    
    /// <summary>
    /// The Corrupt Judge - Peek into the next three cards in your hand and rearrange the first two if desired
    /// </summary>
    public void UseCorruptJudgeCard()
    {
        if (_hasUsedCorruptJudgeThisRound || !_isBetPlaced)
        {
            Debug.Log("Corrupt Judge card already used this round or no bet placed");
            return;
        }
        
        _hasUsedCorruptJudgeThisRound = true;
        
        // Get next 3 cards from deck that would go to player
        List<CardInfo> nextCards = new List<CardInfo>();
        int cardsToShow = Mathf.Min(3, values.Length - cardIndex);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            if (cardIndex + i < values.Length)
            {
                nextCards.Add(GetCardInfo(cardIndex + i));
            }
        }
        
        if (nextCards.Count > 0 && cardPreviewManager != null)
        {
            cardPreviewManager.ShowCorruptJudgePreview(
                nextCards,
                (rearrangedCards) => OnCorruptJudgeConfirm(rearrangedCards),
                null
            );
        }
    }
    
    private void OnCorruptJudgeConfirm(List<CardInfo> rearrangedCards)
    {
        // Apply the rearrangement to the actual deck
        // Only the first two cards can be rearranged
        if (rearrangedCards.Count >= 2)
        {
            // Update the deck arrays with the new order
            for (int i = 0; i < Mathf.Min(2, rearrangedCards.Count); i++)
            {
                int deckPosition = cardIndex + i;
                if (deckPosition < values.Length)
                {
                    // Find the original index of this card and update deck
                    for (int j = 0; j < faces.Length; j++)
                    {
                        if (faces[j] == rearrangedCards[i].cardSprite)
                        {
                            faces[deckPosition] = rearrangedCards[i].cardSprite;
                            values[deckPosition] = rearrangedCards[i].value;
                            originalIndices[deckPosition] = rearrangedCards[i].index;
                            break;
                        }
                    }
                }
            }
            Debug.Log("Corrupt Judge rearranged the first two upcoming cards");
        }
    }
    
    /// <summary>
    /// The Hitman - Peek into the first three cards on your deck and remove one at discretion from play
    /// </summary>
    public void UseHitmanCard()
    {
        if (_hasUsedHitmanThisRound || !_isBetPlaced)
        {
            Debug.Log("Hitman card already used this round or no bet placed");
            return;
        }
        
        _hasUsedHitmanThisRound = true;
        
        // Get next 3 cards from deck
        List<CardInfo> nextCards = new List<CardInfo>();
        int cardsToShow = Mathf.Min(3, values.Length - cardIndex);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            if (cardIndex + i < values.Length)
            {
                nextCards.Add(GetCardInfo(cardIndex + i));
            }
        }
        
        if (nextCards.Count > 0 && cardPreviewManager != null)
        {
            cardPreviewManager.ShowPreview(
                nextCards,
                "The Hitman - Remove One Card",
                false, // No rearranging
                true,  // Allow removing
                1,     // Max remove 1
                (remainingCards) => OnHitmanConfirm(nextCards, remainingCards),
                null
            );
        }
    }
    
    private void OnHitmanConfirm(List<CardInfo> originalCards, List<CardInfo> remainingCards)
    {
        // Find which card was removed
        if (remainingCards.Count < originalCards.Count)
        {
            // Compact the deck by removing the selected card
            for (int i = 0; i < originalCards.Count; i++)
            {
                bool cardStillExists = false;
                foreach (var remaining in remainingCards)
                {
                    if (remaining.index == originalCards[i].index)
                    {
                        cardStillExists = true;
                        break;
                    }
                }
                
                if (!cardStillExists)
                {
                    // Remove this card from the deck
                    RemoveCardFromDeck(cardIndex + i);
                    Debug.Log("Hitman removed: " + originalCards[i].cardName);
                    break;
                }
            }
        }
    }
    
    private void RemoveCardFromDeck(int indexToRemove)
    {
        if (indexToRemove < 0 || indexToRemove >= values.Length) return;
        
        // Shift all cards after the removed index forward
        for (int i = indexToRemove; i < values.Length - 1; i++)
        {
            values[i] = values[i + 1];
            faces[i] = faces[i + 1];
            originalIndices[i] = originalIndices[i + 1];
        }
        
        // Create new smaller arrays
        int[] newValues = new int[values.Length - 1];
        Sprite[] newFaces = new Sprite[faces.Length - 1];
        int[] newOriginalIndices = new int[originalIndices.Length - 1];
        
        System.Array.Copy(values, newValues, newValues.Length);
        System.Array.Copy(faces, newFaces, newFaces.Length);
        System.Array.Copy(originalIndices, newOriginalIndices, newOriginalIndices.Length);
        
        values = newValues;
        faces = newFaces;
        originalIndices = newOriginalIndices;
    }
    
    /// <summary>
    /// The Fortune Teller - Take a peek into the next two cards on your deck
    /// </summary>
    public void UseFortuneTellerCard()
    {
        if (_hasUsedFortuneTellerThisRound || !_isBetPlaced)
        {
            Debug.Log("Fortune Teller card already used this round or no bet placed");
            return;
        }
        
        _hasUsedFortuneTellerThisRound = true;
        
        // Get next 2 cards that will be dealt from the deck
        List<CardInfo> nextCards = new List<CardInfo>();
        int cardsToShow = Mathf.Min(2, values.Length - cardIndex);
        
        for (int i = 0; i < cardsToShow; i++)
        {
            if (cardIndex + i < values.Length)
            {
                nextCards.Add(GetCardInfo(cardIndex + i));
            }
        }
        
        if (nextCards.Count > 0 && cardPreviewManager != null)
        {
            cardPreviewManager.ShowPreview(
                nextCards,
                "The Fortune Teller - Next Player Cards",
                false, // No rearranging
                false, // No removing
                0,
                null, // No confirm callback needed
                null  // No cancel callback needed
            );
        }
        else
        {
            Debug.Log("No more cards in deck to show");
        }
    }
    
    /// <summary>
    /// The Mad Writer - Take a look at the next card and shuffle the whole deck if desired
    /// </summary>
    public void UseMadWriterCard()
    {
        if (_hasUsedMadWriterThisRound || !_isBetPlaced)
        {
            Debug.Log("Mad Writer card already used this round or no bet placed");
            return;
        }
        
        _hasUsedMadWriterThisRound = true;
        
        // Get next card from deck
        if (cardIndex < values.Length)
        {
            CardInfo nextCard = GetCardInfo(cardIndex);
            
            if (cardPreviewManager != null)
            {
                cardPreviewManager.ShowMadWriterPreview(
                    nextCard,
                    () => Debug.Log("Mad Writer chose to keep deck order"),
                    () => {
                        ShuffleCards();
                        Debug.Log("Mad Writer shuffled the deck!");
                    },
                    null
                );
            }
        }
        else
        {
            Debug.Log("No more cards in deck");
        }
    }

    // Make UpdateTransformButtonState public
    public void UpdateTransformButtonState()
    {
        if (transformButton != null)
        {
            CardHand playerHand = player.GetComponent<CardHand>();
            int selectedCount = playerHand ? playerHand.GetSelectedCardCount() : 0;
            
            bool gameActive = hitButton.interactable;
            bool has2CardsSelected = selectedCount == Constants.MaxSelectedCards;
            bool notUsedThisRound = !_hasUsedTransformThisRound;
            
            // Enable only if: bet placed, game is active, exactly 2 cards selected, and hasn't used transform this round
            transformButton.interactable = (_isBetPlaced && gameActive && has2CardsSelected && notUsedThisRound);
            
            // Debug log to track transformation button state
            if (has2CardsSelected)
            {
                string reason = "";
                if (!gameActive) reason += "Game not active. ";
                if (!notUsedThisRound) reason += "Already used transformation this round. ";
                
                if (reason == "")
                    Debug.Log("Transform button enabled: Selected=" + selectedCount);
                else
                    Debug.Log("Transform button disabled: " + reason + "Selected=" + selectedCount);
            }
            
            // Visual feedback on button
            var buttonImage = transformButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (transformButton.interactable)
                {
                    buttonImage.color = Color.white;
                }
                else
                {
                    buttonImage.color = new Color(0.7f, 0.7f, 0.7f);
                }
            }
        }
    }
    
    /// <summary>
    /// Set minimum bet (for boss/curse modifications)
    /// </summary>
    public void SetMinBet(uint minBet)
    {
        _minBet = minBet;
        Debug.Log($"Minimum bet set to: ${_minBet}");
    }
    
    /// <summary>
    /// Set maximum bet (for boss/curse modifications)
    /// </summary>
    public void SetMaxBet(uint maxBet)
    {
        _maxBet = maxBet;
        Debug.Log($"Maximum bet set to: ${_maxBet}");
    }
    
    /// <summary>
    /// Get current minimum bet
    /// </summary>
    public uint GetMinBet() => _minBet;
    
    /// <summary>
    /// Get current maximum bet
    /// </summary>
    public uint GetMaxBet() => _maxBet;
    
    /// <summary>
    /// Reset bet range to defaults
    /// </summary>
    public void ResetBetRange()
    {
        _minBet = Constants.DefaultMinBet;
        _maxBet = Constants.DefaultMaxBet;
        Debug.Log($"Bet range reset to: ${_minBet} - ${_maxBet}");
    }
    
    /// <summary>
    /// Upgrade max hits per hand (for progression system)
    /// </summary>
    public void UpgradeMaxHits(int additionalHits)
    {
        _maxHitsPerHand += additionalHits;
        Debug.Log($"Max hits upgraded to: {_maxHitsPerHand}");
    }
    
    /// <summary>
    /// Upgrade action budget (for progression system)
    /// </summary>
    public void UpgradeActionBudget(int additionalActions)
    {
        _maxActionsPerHand += additionalActions;
        Debug.Log($"Max actions per hand upgraded to: {_maxActionsPerHand}");
    }
    
    /// <summary>
    /// Upgrade tarot limit (for progression system)
    /// </summary>
    public void UpgradeTarotLimit(int additionalTarots)
    {
        _maxTarotsPerHand += additionalTarots;
        Debug.Log($"Max tarots per hand upgraded to: {_maxTarotsPerHand}");
    }
    
    /// <summary>
    /// Adjust the hit counter when cards are removed by tarot effects.
    /// This allows the player to hit again after cards are removed.
    /// </summary>
    /// <param name="cardsRemoved">Number of cards that were removed from the player's hand</param>
    public void AdjustHitsAfterCardRemoval(int cardsRemoved)
    {
        if (cardsRemoved <= 0) return;
        
        int previousHits = _hitsThisHand;
        _hitsThisHand = Mathf.Max(0, _hitsThisHand - cardsRemoved);
        Debug.Log($"[Deck] Adjusted hits after card removal: {previousHits} -> {_hitsThisHand} ({cardsRemoved} cards removed)");
        UpdateRoundFlowUI();
    }
    
    /// <summary>
    /// Reset hit counter to zero (used when hand is completely cleared)
    /// </summary>
    public void ResetHitsThisHand()
    {
        int previousHits = _hitsThisHand;
        _hitsThisHand = 0;
        Debug.Log($"[Deck] Reset hits: {previousHits} -> 0");
        UpdateRoundFlowUI();
    }
    
    /// <summary>
    /// Get remaining actions this hand
    /// </summary>
    public int GetRemainingActions() => _actionsRemainingThisHand;
    
    /// <summary>
    /// Consume one action from budget (returns true if successful)
    /// </summary>
    public bool ConsumeAction()
    {
        if (_actionsRemainingThisHand > 0)
        {
            _actionsRemainingThisHand--;
            Debug.Log($"Action consumed. Remaining: {_actionsRemainingThisHand}/{_maxActionsPerHand}");
            UpdateRoundFlowUI();
            return true;
        }
        Debug.LogWarning("No actions remaining this hand!");
        return false;
    }
    
    /// <summary>
    /// Check if tarot can be used (and consume usage if yes)
    /// </summary>
    public bool CanUseTarot()
    {
        if (_tarotsUsedThisHand < _maxTarotsPerHand)
        {
            _tarotsUsedThisHand++;
            Debug.Log($"Tarot used. Count: {_tarotsUsedThisHand}/{_maxTarotsPerHand}");
            UpdateRoundFlowUI();
            return true;
        }
        Debug.LogWarning($"Tarot limit reached this hand! ({_tarotsUsedThisHand}/{_maxTarotsPerHand})");
        return false;
    }
    
    /// <summary>
    /// Update all Round Flow UI elements
    /// </summary>
    public void UpdateRoundFlowUI()
    {
        // Update hits remaining
        if (hitsRemainingText != null)
        {
            int hitsRemaining = _maxHitsPerHand - _hitsThisHand;
            hitsRemainingText.text = $"Hits: {hitsRemaining}/{_maxHitsPerHand}";
        }
        
        // Update actions remaining
        if (actionsRemainingText != null)
        {
            actionsRemainingText.text = $"Actions: {_actionsRemainingThisHand}/{_maxActionsPerHand}";
        }
        
        // Update discard pile count
        if (discardPileCountText != null)
        {
            discardPileCountText.text = $"Discard Pile: {_discardPile.Count}";
        }
        
        // Update deck cards remaining
        UpdateDeckCardsDisplay();
        
        // Update reshuffle button visibility
        UpdateReshuffleButtonVisibility();
    }
    
    /// <summary>
    /// Update deck cards remaining display
    /// </summary>
    private void UpdateDeckCardsDisplay()
    {
        int cardsRemaining = GetDeckCardsRemaining();
        
        if (deckCardsRemainingText != null)
        {
            deckCardsRemainingText.text = $"Deck: {cardsRemaining}";
            
            // Change color based on remaining cards
            if (cardsRemaining < 5)
            {
                deckCardsRemainingText.color = Color.red;
            }
            else if (cardsRemaining < 10)
            {
                deckCardsRemainingText.color = Color.yellow;
            }
            else
            {
                deckCardsRemainingText.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// Show/hide reshuffle button based on deck cards remaining
    /// </summary>
    private void UpdateReshuffleButtonVisibility()
    {
        if (reshuffleButton == null) return;
        
        int cardsRemaining = GetDeckCardsRemaining();
        
        // Show button when < 10 cards
        if (cardsRemaining < 10 && cardsRemaining >= 5 && _discardPile.Count > 0)
        {
            reshuffleButton.gameObject.SetActive(true);
        }
        else
        {
            reshuffleButton.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Check if auto-reshuffle is needed (< 5 cards)
    /// </summary>
    private void CheckAutoReshuffle()
    {
        int cardsRemaining = GetDeckCardsRemaining();
        
        if (cardsRemaining < 5 && _discardPile.Count > 0)
        {
            Debug.Log($"AUTO-RESHUFFLE triggered! Only {cardsRemaining} cards remaining.");
            StartCoroutine(AutoReshuffleWithNotification());
        }
    }
    
    /// <summary>
    /// Test method to consume an action (for testing action budget)
    /// </summary>
    public void TestConsumeAction()
    {
        if (ConsumeAction())
        {
            Debug.Log("Test action consumed successfully!");
            UpdateRoundFlowUI();
        }
        else
        {
            Debug.LogWarning("No actions remaining to consume!");
        }
    }
    
    /// <summary>
    /// Add a card to the discard pile
    /// </summary>
    public void AddToDiscardPile(int cardIndex, int value, Sprite sprite)
    {
        CardInfo discardedCard = new CardInfo(cardIndex, value, sprite, faces);
        _discardPile.Add(discardedCard);
        Debug.Log($"Card added to discard pile: {discardedCard.cardName} (Total in discard: {_discardPile.Count})");
    }
    
    /// <summary>
    /// Move all cards from both hands to discard pile (called at end of hand)
    /// </summary>
    public void MoveHandsToDiscardPile()
    {
        // Move player cards to discard pile
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand != null)
        {
            foreach (GameObject card in playerHand.cards)
            {
                CardModel cardModel = card.GetComponent<CardModel>();
                if (cardModel != null)
                {
                    CardInfo cardInfo = new CardInfo(cardModel.originalDeckIndex, cardModel.value, cardModel.cardFront, faces);
                    _discardPile.Add(cardInfo);
                }
            }
            Debug.Log($"Moved {playerHand.cards.Count} cards from player hand to discard pile");
        }
        
        // Move dealer cards to discard pile
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        if (dealerHand != null)
        {
            foreach (GameObject card in dealerHand.cards)
            {
                CardModel cardModel = card.GetComponent<CardModel>();
                if (cardModel != null)
                {
                    CardInfo cardInfo = new CardInfo(cardModel.originalDeckIndex, cardModel.value, cardModel.cardFront, faces);
                    _discardPile.Add(cardInfo);
                }
            }
            Debug.Log($"Moved {dealerHand.cards.Count} cards from dealer hand to discard pile");
        }
        
        Debug.Log($"Total cards in discard pile: {_discardPile.Count}");
    }
    
    /// <summary>
    /// Get discard pile count
    /// </summary>
    public int GetDiscardPileCount() => _discardPile.Count;
    
    /// <summary>
    /// Get cards remaining in deck
    /// </summary>
    public int GetDeckCardsRemaining() => Constants.DeckCards - cardIndex;
    
    /// <summary>
    /// Clear discard pile (for new game)
    /// </summary>
    public void ClearDiscardPile()
    {
        _discardPile.Clear();
        Debug.Log("Discard pile cleared");
    }
    
    /// <summary>
    /// Reshuffle discard pile back into deck (when deck runs low)
    /// </summary>
    public void ReshuffleDiscardPileIntoDeck()
    {
        if (_discardPile.Count == 0)
        {
            Debug.Log("No cards in discard pile to reshuffle");
            return;
        }
        
        Debug.Log($"Reshuffling {_discardPile.Count} cards from discard pile back into deck");
        
        int cardsReshuffled = _discardPile.Count;
        _discardPile.Clear();
        cardIndex = 0; // Reset to beginning of deck
        
        // Reshuffle the entire deck for variety
        ShuffleCards();
        
        Debug.Log($"Reshuffled {cardsReshuffled} cards. Deck reset to position 0");
        
        // Update UI
        UpdateRoundFlowUI();
        
        // Show notification
        if (finalMessage != null)
        {
            finalMessage.text = $"Deck Reshuffled! (+{cardsReshuffled} cards)";
            StartCoroutine(ClearMessageAfterDelay(2f));
        }
    }
    
    /// <summary>
    /// Auto-reshuffle with visual notification
    /// </summary>
    private IEnumerator AutoReshuffleWithNotification()
    {
        if (finalMessage != null)
        {
            finalMessage.text = "AUTO-RESHUFFLING DECK...";
            finalMessage.gameObject.SetActive(true);
        }
        
        yield return new WaitForSeconds(1f);
        
        ReshuffleDiscardPileIntoDeck();
        
        yield return new WaitForSeconds(1.5f);
        
        if (finalMessage != null && _gameInProgress)
        {
            finalMessage.text = "";
        }
    }
    
    /// <summary>
    /// Clear message after delay
    /// </summary>
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (finalMessage != null && _gameInProgress)
        {
            finalMessage.text = "";
        }
    }
    
    // ===== ACTION CARDS SYSTEM =====
    
    /// <summary>
    /// Spawn action cards in the panel
    /// Uses ActionCardManager's equipped cards if available, otherwise falls back to availableActionCards
    /// </summary>
    private void SpawnActionCards()
    {
        if (actionCardsPanel == null || actionCardPrefab == null)
        {
            Debug.LogWarning("Action cards panel or prefab not configured");
            return;
        }
        
        // Clear existing cards
        foreach (Transform child in actionCardsPanel)
        {
            Destroy(child.gameObject);
        }
        
        // Determine which action cards to spawn
        System.Collections.Generic.List<ActionCardData> cardsToSpawn = null;
        
        // First priority: Use ActionCardManager's equipped cards
        if (ActionCardManager.Instance != null && ActionCardManager.Instance.EquippedCount > 0)
        {
            cardsToSpawn = ActionCardManager.Instance.EquippedCards;
            Debug.Log($"[SpawnActionCards] Using {cardsToSpawn.Count} equipped cards from ActionCardManager");
        }
        // Fallback: Use availableActionCards array (legacy support)
        else if (availableActionCards != null && availableActionCards.Length > 0)
        {
            cardsToSpawn = new System.Collections.Generic.List<ActionCardData>(availableActionCards);
            Debug.Log($"[SpawnActionCards] Using {cardsToSpawn.Count} cards from availableActionCards array (fallback)");
        }
        
        if (cardsToSpawn == null || cardsToSpawn.Count == 0)
        {
            Debug.Log("[SpawnActionCards] No action cards to spawn");
            return;
        }
        
        // Spawn each action card
        foreach (ActionCardData actionData in cardsToSpawn)
        {
            if (actionData == null) continue;
            
            GameObject cardObj = Instantiate(actionCardPrefab, actionCardsPanel);
            ActionCard actionCard = cardObj.GetComponent<ActionCard>();
            
            if (actionCard != null)
            {
                actionCard.actionData = actionData;
            }
        }
        
        Debug.Log($"[SpawnActionCards] Spawned {cardsToSpawn.Count} action cards");
    }
    
    /// <summary>
    /// Refresh action cards display (called when equipped cards change)
    /// </summary>
    public void RefreshActionCardsDisplay()
    {
        SpawnActionCards();
        ResetActionCards();
    }
    
    /// <summary>
    /// Reset all action cards for new hand
    /// </summary>
    private void ResetActionCards()
    {
        if (actionCardsPanel == null) return;
        
        foreach (Transform child in actionCardsPanel)
        {
            ActionCard actionCard = child.GetComponent<ActionCard>();
            if (actionCard != null)
            {
                actionCard.ResetForNewHand();
            }
        }
    }
    
    // ===== ACTION IMPLEMENTATIONS =====
    
    /// <summary>
    /// ACTION: Swap player's selected card with dealer's selected card
    /// Called from UI button - uses action budget
    /// </summary>
    public void ActionSwapWithDealerCard()
    {
        // Check if player has actions remaining
        if (_actionsRemainingThisHand <= 0)
        {
            Debug.LogWarning("No actions remaining this hand!");
            if (finalMessage != null)
            {
                finalMessage.text = "No actions remaining!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return;
        }
        
        CardHand playerHand = player.GetComponent<CardHand>();
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        
        if (playerHand == null || dealerHand == null) return;
        
        // Get player's selected card
        GameObject playerCard = null;
        foreach (GameObject card in playerHand.cards)
        {
            if (card.GetComponent<CardModel>().isSelected)
            {
                playerCard = card;
                break;
            }
        }
        
        if (playerCard == null)
        {
            Debug.LogWarning("Select one of your cards first!");
            if (finalMessage != null)
            {
                finalMessage.text = "Select your card first!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return;
        }
        
        // Get dealer's selected card (or first visible card if none selected)
        GameObject dealerCard = null;
        foreach (GameObject card in dealerHand.cards)
        {
            CardModel cardModel = card.GetComponent<CardModel>();
            Image sr = card.GetComponent<Image>();
            // Only swap with face-up dealer cards
            if (sr != null && sr.sprite == cardModel.cardFront)
            {
                if (cardModel.isSelected)
                {
                    dealerCard = card;
                    break;
                }
            }
        }
        
        // If no dealer card selected, use first visible card
        if (dealerCard == null)
        {
            foreach (GameObject card in dealerHand.cards)
            {
                CardModel cardModel = card.GetComponent<CardModel>();
                Image sr = card.GetComponent<Image>();
                if (sr != null && sr.sprite == cardModel.cardFront)
                {
                    dealerCard = card;
                    break;
                }
            }
        }
        
        if (dealerCard == null)
        {
            Debug.LogWarning("No visible dealer cards to swap with!");
            if (finalMessage != null)
            {
                finalMessage.text = "No dealer cards available!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return;
        }
        
        // Consume action
        if (!ConsumeAction())
        {
            return;
        }
        
        // Swap the cards
        CardModel playerCardModel = playerCard.GetComponent<CardModel>();
        CardModel dealerCardModel = dealerCard.GetComponent<CardModel>();
        
        int tempValue = playerCardModel.value;
        Sprite tempFront = playerCardModel.cardFront;
        int tempIndex = playerCardModel.originalDeckIndex;
        
        playerCardModel.value = dealerCardModel.value;
        playerCardModel.cardFront = dealerCardModel.cardFront;
        playerCardModel.originalDeckIndex = dealerCardModel.originalDeckIndex;
        playerCard.GetComponent<Image>().sprite = dealerCardModel.cardFront;
        
        dealerCardModel.value = tempValue;
        dealerCardModel.cardFront = tempFront;
        dealerCardModel.originalDeckIndex = tempIndex;
        dealerCard.GetComponent<Image>().sprite = tempFront;
        
        // Deselect cards
        playerCardModel.DeselectCard();
        if (dealerCardModel.isSelected)
        {
            dealerCardModel.DeselectCard();
        }
        
        UpdateScoreDisplays();
        
        Debug.Log("Swapped player card with dealer card!");
        
        // Show success message
        if (finalMessage != null)
        {
            finalMessage.text = "Cards swapped!";
            StartCoroutine(ClearMessageAfterDelay(1.5f));
        }
    }
    
    /// <summary>
    /// ACTION: Swap two selected cards in player's hand (keep for backward compatibility)
    /// </summary>
    public bool ActionSwapTwoCards()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        // Get selected cards
        List<GameObject> selectedCards = new List<GameObject>();
        foreach (GameObject card in playerHand.cards)
        {
            if (card.GetComponent<CardModel>().isSelected)
            {
                selectedCards.Add(card);
            }
        }
        
        if (selectedCards.Count != 2)
        {
            Debug.LogWarning("Select exactly 2 cards to swap!");
            return false;
        }
        
        // Swap card values and sprites
        CardModel card1 = selectedCards[0].GetComponent<CardModel>();
        CardModel card2 = selectedCards[1].GetComponent<CardModel>();
        
        int tempValue = card1.value;
        Sprite tempFront = card1.cardFront;
        int tempIndex = card1.originalDeckIndex;
        
        card1.value = card2.value;
        card1.cardFront = card2.cardFront;
        card1.originalDeckIndex = card2.originalDeckIndex;
        card1.GetComponent<Image>().sprite = card2.cardFront;
        
        card2.value = tempValue;
        card2.cardFront = tempFront;
        card2.originalDeckIndex = tempIndex;
        card2.GetComponent<Image>().sprite = tempFront;
        
        // Deselect cards
        card1.DeselectCard();
        card2.DeselectCard();
        
        UpdateScoreDisplays();
        Debug.Log("Cards swapped successfully!");
        return true;
    }
    
    /// <summary>
    /// ACTION: Add +1 to a selected card (max 10)
    /// Called from UI button - uses action budget
    /// </summary>
    public void ActionAddOneToCard()
    {
        // Check if player has actions remaining
        if (_actionsRemainingThisHand <= 0)
        {
            Debug.LogWarning("No actions remaining this hand!");
            if (finalMessage != null)
            {
                finalMessage.text = "No actions remaining!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return;
        }
        
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card to add +1!");
            if (finalMessage != null)
            {
                finalMessage.text = "Select a card first!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return;
        }
        
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        if (cardModel.value >= 10)
        {
            Debug.LogWarning("Card is already at maximum value (10)!");
            if (finalMessage != null)
            {
                finalMessage.text = "Card already at max (10)!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return;
        }
        
        // Consume action
        if (!ConsumeAction())
        {
            return;
        }
        
        // Execute action
        cardModel.value++;
        cardModel.DeselectCard();
        UpdateScoreDisplays();
        
        Debug.Log($"Added +1 to card (new value: {cardModel.value})");
        
        // Show success message
        if (finalMessage != null)
        {
            finalMessage.text = $"Card +1! (Now: {cardModel.value})";
            StartCoroutine(ClearMessageAfterDelay(1.5f));
        }
    }
    
    /// <summary>
    /// ACTION: Subtract -1 from a selected card (min 1)
    /// </summary>
    public bool ActionSubtractOneFromCard()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card to subtract -1!");
            return false;
        }
        
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        if (cardModel.value <= 1)
        {
            Debug.LogWarning("Card is already at minimum value (1)!");
            return false;
        }
        
        cardModel.value--;
        cardModel.DeselectCard();
        UpdateScoreDisplays();
        Debug.Log($"Subtracted -1 from card (new value: {cardModel.value})");
        return true;
    }
    
    /// <summary>
    /// ACTION: Peek at dealer's hidden card
    /// </summary>
    public bool ActionPeekDealerCard()
    {
        PeekAtDealerCard();
        return true;
    }
    
    /// <summary>
    /// ACTION: Force redraw - discard selected card and draw new one
    /// </summary>
    public bool ActionForceRedraw()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card to redraw!");
            return false;
        }
        
        // Remove selected card
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        AddToDiscardPile(cardModel.originalDeckIndex, cardModel.value, cardModel.cardFront);
        playerHand.cards.Remove(selectedCard);
        Destroy(selectedCard);
        
        // Draw new card
        StartCoroutine(PushPlayerAnimated());
        UpdateScoreDisplays();
        Debug.Log("Card redrawn!");
        return true;
    }
    
    /// <summary>
    /// ACTION: Double a card's value (capped at 10)
    /// </summary>
    public bool ActionDoubleCardValue()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card to double!");
            return false;
        }
        
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        int newValue = Mathf.Min(cardModel.value * 2, 10);
        cardModel.value = newValue;
        cardModel.DeselectCard();
        UpdateScoreDisplays();
        Debug.Log($"Card value doubled (new value: {newValue})");
        return true;
    }
    
    /// <summary>
    /// ACTION: Set any card to value 10
    /// </summary>
    public bool ActionSetCardToTen()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card to set to 10!");
            return false;
        }
        
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        cardModel.value = 10;
        cardModel.DeselectCard();
        UpdateScoreDisplays();
        Debug.Log("Card set to 10!");
        return true;
    }
    
    /// <summary>
    /// ACTION: Flip Ace between 1 and 11
    /// </summary>
    public bool ActionFlipAce()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card!");
            return false;
        }
        
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        if (cardModel.originalDeckIndex % 13 != 0) // Not an ace
        {
            Debug.LogWarning("Selected card is not an Ace!");
            return false;
        }
        
        // Toggle between 1 and 11
        cardModel.value = (cardModel.value == 1) ? 11 : 1;
        cardModel.DeselectCard();
        UpdateScoreDisplays();
        Debug.Log($"Ace flipped to {cardModel.value}!");
        return true;
    }
    
    /// <summary>
    /// ACTION: Shield card (placeholder - could protect from boss effects)
    /// </summary>
    public bool ActionShieldCard()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("Select a card to shield!");
            return false;
        }
        
        // For now, just visual feedback
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        cardModel.DeselectCard();
        Debug.Log("Card shielded! (Protected from boss effects)");
        // TODO: Implement actual shield mechanics
        return true;
    }
    
    /// <summary>
    /// ACTION: Copy one card's value to another
    /// </summary>
    public bool ActionCopyCard()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return false;
        
        List<GameObject> selectedCards = new List<GameObject>();
        foreach (GameObject card in playerHand.cards)
        {
            if (card.GetComponent<CardModel>().isSelected)
            {
                selectedCards.Add(card);
            }
        }
        
        if (selectedCards.Count != 2)
        {
            Debug.LogWarning("Select exactly 2 cards: source and target!");
            return false;
        }
        
        // Copy first card's value to second card
        CardModel sourceCard = selectedCards[0].GetComponent<CardModel>();
        CardModel targetCard = selectedCards[1].GetComponent<CardModel>();
        
        targetCard.value = sourceCard.value;
        
        sourceCard.DeselectCard();
        targetCard.DeselectCard();
        UpdateScoreDisplays();
        Debug.Log($"Copied value {sourceCard.value} to second card!");
        return true;
    }
    
    // ============ LOW-IMPACT ACTION CARD MODIFIERS ============
    
    /// <summary>
    /// ACTION: Value Plus One - Add +1 to any card including empty cards
    /// Similar to AddOneToCard but specifically for the low-impact action card system
    /// </summary>
    public bool ActionValuePlusOne()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null)
        {
            Debug.LogWarning("[ActionValuePlusOne] Player hand not found!");
            return false;
        }
        
        GameObject selectedCard = playerHand.GetSelectedCard();
        if (selectedCard == null)
        {
            Debug.LogWarning("[ActionValuePlusOne] No card selected!");
            if (finalMessage != null)
            {
                finalMessage.text = "Select a card first!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return false;
        }
        
        CardModel cardModel = selectedCard.GetComponent<CardModel>();
        if (cardModel == null)
        {
            Debug.LogError("[ActionValuePlusOne] Selected card has no CardModel component!");
            return false;
        }
        
        // Handle empty cards (value 0) - they should become value 1
        // For regular cards, check if already at max
        if (cardModel.value > 0 && cardModel.value >= 10)
        {
            Debug.LogWarning("[ActionValuePlusOne] Card is already at maximum value (10)!");
            if (finalMessage != null)
            {
                finalMessage.text = "Card already at max (10)!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return false;
        }
        
        // Execute action - increase value by 1
        int oldValue = cardModel.value;
        cardModel.value++;
        
        // Update card visual if needed (for empty cards becoming value 1)
        if (oldValue == 0 && cardModel.value == 1)
        {
            // Empty card became an Ace - update sprite if possible
            Image cardImage = selectedCard.GetComponent<Image>();
            if (cardImage != null && faces != null && faces.Length > 0)
            {
                // Try to set a default Ace sprite (first card in deck is usually Ace of Spades)
                // This is a fallback - ideally the card should already have a sprite
                if (cardImage.sprite == null)
                {
                    // Find an Ace sprite (value 1, any suit)
                    for (int i = 0; i < faces.Length && i < values.Length; i++)
                    {
                        if (values[i] == 1)
                        {
                            cardImage.sprite = faces[i];
                            cardModel.cardFront = faces[i];
                            break;
                        }
                    }
                }
            }
        }
        
        // Deselect the card
        cardModel.DeselectCard();
        
        // Update score displays
        UpdateScoreDisplays();
        
        Debug.Log($"[ActionValuePlusOne] Card value increased from {oldValue} to {cardModel.value}");
        
        if (finalMessage != null)
        {
            finalMessage.text = $"+1! Card now {cardModel.value}";
            StartCoroutine(ClearMessageAfterDelay(1.5f));
        }
        
        // Check for bust/blackjack after modifying - but don't call EndHand immediately
        // Let the game flow handle it naturally
        int playerPoints = GetPlayerPoints();
        if (playerPoints > Constants.Blackjack)
        {
            // Don't end hand immediately - let the player see the result
            // The game will handle bust on next action
            Debug.Log($"[ActionValuePlusOne] Player busted with {playerPoints} points");
        }
        else if (playerPoints == Constants.Blackjack)
        {
            // Don't end hand immediately - let the player see the result
            Debug.Log($"[ActionValuePlusOne] Player got blackjack with {playerPoints} points");
        }
        
        // Re-enable player controls if game is still in progress and it's player's turn
        // This ensures hit/stand buttons remain functional after using the action card
        if (_gameInProgress && _currentTurn == GameTurn.Player && _isBetPlaced)
        {
            EnablePlayerControls();
        }
        
        return true;
    }
    
    /// <summary>
    /// ACTION: Minor Swap With Dealer - Swap player card with dealer's face-up card
    /// Requires selecting player card first, then dealer's face-up card
    /// </summary>
    public bool ActionMinorSwapWithDealer()
    {
        CardHand playerHand = player.GetComponent<CardHand>();
        CardHand dealerHand = dealer.GetComponent<CardHand>();
        
        if (playerHand == null || dealerHand == null) return false;
        
        // Get selected player card
        GameObject selectedPlayerCard = playerHand.GetSelectedCard();
        if (selectedPlayerCard == null)
        {
            Debug.LogWarning("Select a player card first to swap!");
            if (finalMessage != null)
            {
                finalMessage.text = "Select your card first!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return false;
        }
        
        // Find selected dealer card (must be face-up)
        GameObject selectedDealerCard = null;
        foreach (GameObject card in dealerHand.cards)
        {
            CardModel cardModel = card.GetComponent<CardModel>();
            Image cardImage = card.GetComponent<Image>();
            
            // Check if card is face-up (showing front sprite) and selected
            if (cardImage != null && cardImage.sprite == cardModel.cardFront && cardModel.isSelected)
            {
                selectedDealerCard = card;
                break;
            }
        }
        
        if (selectedDealerCard == null)
        {
            Debug.LogWarning("Select a face-up dealer card to swap with!");
            if (finalMessage != null)
            {
                finalMessage.text = "Select dealer's face-up card!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return false;
        }
        
        // Perform the swap
        CardModel playerCardModel = selectedPlayerCard.GetComponent<CardModel>();
        CardModel dealerCardModel = selectedDealerCard.GetComponent<CardModel>();
        
        // Swap values
        int tempValue = playerCardModel.value;
        playerCardModel.value = dealerCardModel.value;
        dealerCardModel.value = tempValue;
        
        // Swap sprites
        Image playerImage = selectedPlayerCard.GetComponent<Image>();
        Image dealerImage = selectedDealerCard.GetComponent<Image>();
        
        Sprite tempSprite = playerCardModel.cardFront;
        playerCardModel.cardFront = dealerCardModel.cardFront;
        dealerCardModel.cardFront = tempSprite;
        
        // Update visual display
        if (playerImage != null) playerImage.sprite = playerCardModel.cardFront;
        if (dealerImage != null) dealerImage.sprite = dealerCardModel.cardFront;
        
        // Swap original deck indices for proper tracking
        int tempIndex = playerCardModel.originalDeckIndex;
        playerCardModel.originalDeckIndex = dealerCardModel.originalDeckIndex;
        dealerCardModel.originalDeckIndex = tempIndex;
        
        // Deselect both cards
        playerCardModel.DeselectCard();
        dealerCardModel.DeselectCard();
        
        UpdateScoreDisplays();
        
        Debug.Log($"[ActionMinorSwapWithDealer] Swapped player card (now {playerCardModel.value}) with dealer card (now {dealerCardModel.value})");
        
        if (finalMessage != null)
        {
            finalMessage.text = "Cards swapped!";
            StartCoroutine(ClearMessageAfterDelay(1.5f));
        }
        
        // Check for bust after swap
        int playerPoints = GetPlayerPoints();
        if (playerPoints > Constants.Blackjack)
        {
            EndHand(WinCode.DealerWins);
        }
        else if (playerPoints == Constants.Blackjack)
        {
            EndHand(WinCode.PlayerWins);
        }
        
        return true;
    }
    
    /// <summary>
    /// ACTION: Minor Heal - Restore 10 health points
    /// Limited to 3 uses per entire game session
    /// </summary>
    public bool ActionMinorHeal()
    {
        // Check if ActionCardManager exists and has uses remaining
        if (ActionCardManager.Instance == null)
        {
            Debug.LogError("[ActionMinorHeal] ActionCardManager not found!");
            return false;
        }
        
        if (!ActionCardManager.Instance.CanUseMinorHeal())
        {
            Debug.LogWarning("[ActionMinorHeal] No Minor Heal uses remaining!");
            if (finalMessage != null)
            {
                finalMessage.text = "No heals remaining!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return false;
        }
        
        // Use the heal charge
        if (!ActionCardManager.Instance.UseMinorHeal())
        {
            return false;
        }
        
        // Apply healing via GameProgressionManager (soul/health system)
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.HealPlayer(10f);
            Debug.Log($"[ActionMinorHeal] Healed 10 soul (HP). Remaining uses: {ActionCardManager.Instance.MinorHealRemainingUses}");
            
            if (finalMessage != null)
            {
                finalMessage.text = $"+10 Soul! ({ActionCardManager.Instance.MinorHealRemainingUses} heals left)";
                StartCoroutine(ClearMessageAfterDelay(2f));
            }
        }
        else
        {
            Debug.LogWarning("[ActionMinorHeal] GameProgressionManager not found!");
            if (finalMessage != null)
            {
                finalMessage.text = "Heal failed!";
                StartCoroutine(ClearMessageAfterDelay(1.5f));
            }
            return false;
        }
        
        return true;
    }
    
    // Transform selected cards functionality
    public void TransformSelectedCards()
    {
        Debug.Log("TransformSelectedCards method called, Used this round: " + _hasUsedTransformThisRound);
        
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot transform: No bet placed");
            return;
        }
        
        CardHand playerHand = player.GetComponent<CardHand>();
        int selectedCount = playerHand ? playerHand.GetSelectedCardCount() : 0;
        
        // Detailed check with better error messages
        if (selectedCount != Constants.MaxSelectedCards)
        {
            Debug.LogWarning("Cannot transform: Need exactly " + Constants.MaxSelectedCards + " cards selected (currently " + selectedCount + ")");
            return;
        }
        
        if (_hasUsedTransformThisRound)
        {
            Debug.LogWarning("Cannot transform: Already used transformation this round");
            return;
        }
        
        // Use a token for transformation
        _hasUsedTransformThisRound = true;
        Debug.Log("Transforming cards... Used this round: " + _hasUsedTransformThisRound);
        
        // Perform the transformation
        playerHand.TransformSelectedCards();
         
        UpdateTransformButtonState();
        UpdateDiscardButtonState();
        UpdateScoreDisplays();
         
        int playerPoints = GetPlayerPoints();
        Debug.Log("Player points after transformation: " + playerPoints);
        
        if (playerPoints > Constants.Blackjack)
        {
            EndHand(WinCode.DealerWins);
        }
        else if (playerPoints == Constants.Blackjack)
        {
            EndHand(WinCode.PlayerWins);
        }
    }


    
    // Boss system methods
    public void ShowBossTransition()
    {
        Debug.Log("ShowBossTransition called");
        Debug.Log($"newBossPanel: {(newBossPanel != null ? "not null" : "null")}");
        Debug.Log($"bossManager: {(bossManager != null ? "not null" : "null")}");
        Debug.Log($"bossManager.currentBoss: {(bossManager?.currentBoss != null ? bossManager.currentBoss.bossName : "null")}");
        
        if (newBossPanel != null && bossManager != null && bossManager.currentBoss != null)
        {
            // Show the new boss panel directly
            newBossPanel.ShowBossPanel();
        }
        else
        {
            Debug.LogWarning("Cannot show boss transition - missing required components");
        }
    }

    // Update balance display
    // BETTING SYSTEM 2.0: Removed UpdateBalanceDisplay() - now handled by BettingManager

    // Method for the ShopManager to call when a card is purchased
    public void OnCardPurchased(uint cost)
    {
        // Card purchases do NOT affect boss progression
        Debug.Log("Card purchased for $" + cost + " - Boss system unaffected");
    }

    // Method to set the button text to "Next Hand"
    private void SetButtonTextToNextHand()
    {
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = "Next Hand";
        }
    }

    // Method to calculate win multiplier based on streak level
    /*private float CalculateWinMultiplier()
    {
        return Constants.BaseWinMultiplier + (_streakMultiplier * Constants.StreakMultiplierStep);
    }*/
    private float CalculateWinMultiplier()
    {
        float baseMultiplier = Constants.BaseWinMultiplier + (_streakMultiplier * Constants.StreakMultiplierStep);

        // Apply Artificer bonus only if streak is active AND player has activated Artificer card
        bool hasArtificer = PlayerActuallyHasCard(TarotCardType.Artificer) && PlayerHasActivatedCard(TarotCardType.Artificer);
        
        if (_streakMultiplier > 0 && hasArtificer)
        {
            baseMultiplier *= 1.1f; // Add 10% to the multiplier
        }

        return baseMultiplier;
    }

    // CARD UTILITY FUNCTIONS - Easy access to card data
    
    /// <summary>
    /// Get complete information about a card by its deck index
    /// </summary>
    public CardInfo GetCardInfo(int deckIndex)
    {
        if (deckIndex < 0 || deckIndex >= Constants.DeckCards)
        {
            Debug.LogWarning("Invalid card index: " + deckIndex);
            return new CardInfo();
        }
        
        return new CardInfo(deckIndex, values[deckIndex], faces[deckIndex], faces);
    }
    
    /// <summary>
    /// Get complete information about a card by its sprite (DEPRECATED - use GetCardInfoFromModel instead)
    /// </summary>
    public CardInfo GetCardInfoBySprite(Sprite cardSprite)
    {
        for (int i = 0; i < faces.Length; i++)
        {
            if (faces[i] == cardSprite)
            {
                return GetCardInfo(i);
            }
        }
        
        Debug.LogWarning("Card sprite not found in deck");
        return new CardInfo();
    }
    
    /// <summary>
    /// Get complete information about a card using its stored original deck index
    /// </summary>
    public CardInfo GetCardInfoFromModel(CardModel cardModel)
    {
        if (cardModel == null)
        {
            Debug.LogWarning("CardModel is null");
            return new CardInfo();
        }
        
        if (cardModel.originalDeckIndex < 0 || cardModel.originalDeckIndex >= Constants.DeckCards)
        {
            Debug.LogWarning("Invalid original deck index: " + cardModel.originalDeckIndex + " for card with value " + cardModel.value);
            return new CardInfo();
        }
        
        // Use the original deck index to get correct suit/value information
        return new CardInfo(cardModel.originalDeckIndex, cardModel.value, cardModel.cardFront, faces);
    }
    
    /// <summary>
    /// Get all cards of a specific suit from the deck
    /// </summary>
    public List<CardInfo> GetCardsOfSuit(CardSuit suit)
    {
        List<CardInfo> suitCards = new List<CardInfo>();
        int startIndex = (int)suit * Constants.CardsPerSuit;
        int endIndex = startIndex + Constants.CardsPerSuit;
        
        for (int i = startIndex; i < endIndex; i++)
        {
            suitCards.Add(GetCardInfo(i));
        }
        
        return suitCards;
    }
    
    /// <summary>
    /// Get all card information for cards currently in a hand
    /// </summary>
    public List<CardInfo> GetHandCardInfo(GameObject handOwner)
    {
        List<CardInfo> handCards = new List<CardInfo>();
        CardHand hand = handOwner.GetComponent<CardHand>();
        
        if (hand == null || hand.cards == null)
        {
            return handCards;
        }
        
        foreach (GameObject cardObject in hand.cards)
        {
            CardModel cardModel = cardObject.GetComponent<CardModel>();
            if (cardModel != null && cardModel.cardFront != null)
            {
                CardInfo cardInfo = GetCardInfoFromModel(cardModel);
                handCards.Add(cardInfo);
            }
        }
        
        return handCards;
    }
    
    /// <summary>
    /// Filter hand cards by suit
    /// </summary>
    public List<CardInfo> GetHandCardsBySuit(GameObject handOwner, CardSuit targetSuit)
    {
        List<CardInfo> allHandCards = GetHandCardInfo(handOwner);
        return allHandCards.Where(card => card.suit == targetSuit).ToList();
    }
    
    /// <summary>
    /// Get count of cards by suit in a hand (optimized version)
    /// </summary>
    public int GetHandSuitCount(GameObject handOwner, CardSuit targetSuit)
    {
        return GetHandCardsBySuit(handOwner, targetSuit).Count;
    }
    
    /// <summary>
    /// Get all suit counts for a hand at once
    /// </summary>
    public Dictionary<CardSuit, int> GetAllHandSuitCounts(GameObject handOwner)
    {
        Dictionary<CardSuit, int> suitCounts = new Dictionary<CardSuit, int>();
        
        // Initialize all suits to 0
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            suitCounts[suit] = 0;
        }
        
        // Count cards in hand
        List<CardInfo> handCards = GetHandCardInfo(handOwner);
        foreach (CardInfo card in handCards)
        {
            suitCounts[card.suit]++;
        }
        
        return suitCounts;
    }
    
    /// <summary>
    /// Check if a specific card (by value and suit) exists in hand
    /// </summary>
    public bool HandContainsCard(GameObject handOwner, int value, CardSuit suit)
    {
        List<CardInfo> handCards = GetHandCardInfo(handOwner);
        return handCards.Any(card => card.value == value && card.suit == suit);
    }
    
    /// <summary>
    /// Get the deck index for a specific card value and suit
    /// </summary>
    public int GetCardIndex(int suitIndex, CardSuit suit)
    {
        if (suitIndex < 0 || suitIndex >= Constants.CardsPerSuit)
        {
            Debug.LogWarning("Invalid suit index: " + suitIndex);
            return -1;
        }
        
        return ((int)suit * Constants.CardsPerSuit) + suitIndex;
    }
    
    // Method to determine the suit of a card based on its index in the deck
    private CardSuit GetCardSuit(int cardIndex)
    {
        // Each suit has 13 cards (A, 2-10, J, Q, K)
        // Hearts: 0-12, Diamonds: 13-25, Clubs: 26-38, Spades: 39-51
        int suitIndex = cardIndex / Constants.CardsPerSuit;
        
        switch (suitIndex)
        {
            case 0: return CardSuit.Hearts;
            case 1: return CardSuit.Diamonds;
            case 2: return CardSuit.Clubs;
            case 3: return CardSuit.Spades;
            default: 
                Debug.LogWarning("Invalid card index: " + cardIndex);
                return CardSuit.Hearts; // Default fallback
        }
    }

    // Method to count cards of a specific suit in a hand (legacy - use GetHandSuitCount instead)
    private int CountCardsOfSuit(GameObject handOwner, CardSuit targetSuit)
    {
        return GetHandSuitCount(handOwner, targetSuit);
    }

    // HELPER FUNCTION - Check if player actually has a tarot card in the panel
    public bool PlayerActuallyHasCard(TarotCardType cardType)
    {
        // First check the new inventory system for equipped cards
        if (InventoryManager.Instance != null)
        {
            var equippedCards = InventoryManager.Instance.GetEquippedUsableCards();
            foreach (var card in equippedCards)
            {
                if (card.cardType == cardType && card.CanBeUsed())
                {
                    return true;
                }
            }
            return false; // If inventory system is available, only use equipped cards
        }
        
        // Fallback to old system if inventory manager not available
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager == null || shopManager.tarotPanel == null)
        {
            return false;
        }
        
        TarotCard[] actualCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        
        foreach (var card in actualCards)
        {
            if (card.cardData != null && card.cardData.cardType == cardType && !card.isInShop)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // NEW HELPER FUNCTION - Check if player has activated a passive card this round
    public bool PlayerHasActivatedCard(TarotCardType cardType)
    {
        switch (cardType)
        {
            case TarotCardType.Botanist:
                return _hasActivatedBotanistThisRound;
            case TarotCardType.Assassin:
                return _hasActivatedAssassinThisRound;
            case TarotCardType.SecretLover:
                return _hasActivatedSecretLoverThisRound;
            case TarotCardType.Jeweler:
                return _hasActivatedJewelerThisRound;
            case TarotCardType.HouseKeeper:
                return _hasActivatedHouseKeeperThisRound;
            case TarotCardType.WitchDoctor:
                return _hasActivatedWitchDoctorThisRound;
            case TarotCardType.Artificer:
                return _hasActivatedArtificerThisRound;
            default:
                return false;
        }
    }





    // TAROT CARD BONUS FUNCTIONS - Individual calculations for each suit-based tarot card
    
    /// <summary>
    /// Calculate The Botanist bonus (+50 per club in winning hand)
    /// </summary>
    public uint CalculateBotanistBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.Botanist) || !PlayerHasActivatedCard(TarotCardType.Botanist))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
            return 0;
        
        // Get actual cards in player's hand and count clubs
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> clubCards = handCards.Where(card => card.suit == CardSuit.Clubs).ToList();
        
        return (uint)(clubCards.Count * Constants.SuitBonusAmount);
    }
    
    /// <summary>
    /// Calculate The Assassin bonus (+50 per spade in winning hand)
    /// </summary>
    public uint CalculateAssassinBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.Assassin) || !PlayerHasActivatedCard(TarotCardType.Assassin))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
            return 0;
        
        // Get actual cards in player's hand and count spades
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> spadeCards = handCards.Where(card => card.suit == CardSuit.Spades).ToList();
        
        return (uint)(spadeCards.Count * Constants.SuitBonusAmount);
    }
    
    /// <summary>
    /// Calculate The Secret Lover bonus (+50 per heart in winning hand)
    /// </summary>
    public uint CalculateSecretLoverBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.SecretLover) || !PlayerHasActivatedCard(TarotCardType.SecretLover))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
            return 0;
        
        // Get actual cards in player's hand and count hearts
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> heartCards = handCards.Where(card => card.suit == CardSuit.Hearts).ToList();
        
        return (uint)(heartCards.Count * Constants.SuitBonusAmount);
    }
    
    /// <summary>
    /// Calculate The Jeweler bonus (+50 per diamond in winning hand)
    /// </summary>
    public uint CalculateJewelerBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.Jeweler) || !PlayerHasActivatedCard(TarotCardType.Jeweler))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
            return 0;
        
        // Get actual cards in player's hand and count diamonds
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> diamondCards = handCards.Where(card => card.suit == CardSuit.Diamonds).ToList();
        
        return (uint)(diamondCards.Count * Constants.SuitBonusAmount);
    }
    
    /// <summary>
    /// Calculate The House Keeper bonus (+10 per Jack/Queen/King in winning hand)
    /// </summary>
    public uint CalculateHouseKeeperBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.HouseKeeper) || !PlayerHasActivatedCard(TarotCardType.HouseKeeper))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
            return 0;
        
        // Get actual cards in player's hand and count Jack/Queen/King cards
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> faceCards = handCards.Where(card => 
            card.suitIndex == 10 || // Jack
            card.suitIndex == 11 || // Queen  
            card.suitIndex == 12    // King
        ).ToList();
        
        return (uint)(faceCards.Count * Constants.HouseKeeperBonusAmount);
    }
    
    /// <summary>
    /// Calculate all suit bonuses for winning hands
    /// </summary>
    public uint CalculateSuitBonuses(GameObject handOwner = null)
    {
        if (PlayerStats.instance == null)
            return 0;
            
        // Tarot card bonuses should ONLY apply to the player's hand, never dealer's hand
        GameObject targetHand = player;
        
        // Verify we have the player object
        if (targetHand == null)
            return 0;
        
        // Calculate bonuses - these methods now ensure they only count player hand cards
        uint botanistBonus = CalculateBotanistBonus(targetHand);
        uint assassinBonus = CalculateAssassinBonus(targetHand);
        uint secretLoverBonus = CalculateSecretLoverBonus(targetHand);
        uint jewelerBonus = CalculateJewelerBonus(targetHand);
        uint houseKeeperBonus = CalculateHouseKeeperBonus(targetHand);
        
        return botanistBonus + assassinBonus + secretLoverBonus + jewelerBonus + houseKeeperBonus;
    }
    
    /// <summary>
    /// BETTING SYSTEM 2.0: Calculate suit bonuses as health percentage
    /// Converts suit bonuses to percentage equivalent
    /// </summary>
    public float CalculateSuitBonusesAsPercentage(GameObject handOwner = null)
    {
        if (PlayerStats.instance == null)
            return 0f;
            
        // Tarot card bonuses should ONLY apply to the player's hand, never dealer's hand
        GameObject targetHand = player;
        
        // Verify we have the player object
        if (targetHand == null)
            return 0f;
        
        // Calculate bonuses - convert from currency to percentage (divide by 10 for percentage equivalent)
        // e.g., $50 bonus = 5% health bonus
        uint botanistBonus = CalculateBotanistBonus(targetHand);
        uint assassinBonus = CalculateAssassinBonus(targetHand);
        uint secretLoverBonus = CalculateSecretLoverBonus(targetHand);
        uint jewelerBonus = CalculateJewelerBonus(targetHand);
        uint houseKeeperBonus = CalculateHouseKeeperBonus(targetHand);
        
        uint totalBonus = botanistBonus + assassinBonus + secretLoverBonus + jewelerBonus + houseKeeperBonus;
        
        // Convert to percentage: every $10 = 1% health
        return totalBonus / 10f;
    }
    
    /// <summary>
    /// Get detailed breakdown of all suit bonuses (useful for UI display)
    /// </summary>
    public Dictionary<TarotCardType, uint> GetSuitBonusBreakdown(GameObject handOwner = null)
    {
        Dictionary<TarotCardType, uint> breakdown = new Dictionary<TarotCardType, uint>();
        
        if (PlayerStats.instance == null)
            return breakdown;
            
        // Tarot card bonuses should ONLY apply to the player's hand, never dealer's hand
        GameObject targetHand = player;
        
        // Verify we have the player object
        if (targetHand == null)
            return breakdown;
        
        breakdown[TarotCardType.Botanist] = CalculateBotanistBonus(targetHand);
        breakdown[TarotCardType.Assassin] = CalculateAssassinBonus(targetHand);
        breakdown[TarotCardType.SecretLover] = CalculateSecretLoverBonus(targetHand);
        breakdown[TarotCardType.Jeweler] = CalculateJewelerBonus(targetHand);
        breakdown[TarotCardType.HouseKeeper] = CalculateHouseKeeperBonus(targetHand);
        
        return breakdown;
    }


    

    
    /// <summary>
    /// Use The Escapist card - called when player clicks on it
    /// </summary>
    public IEnumerator UseEscapistCard()
    {
        Debug.Log("=== ESCAPIST CARD ACTIVATED BY PLAYER ===");
        
        // Check if we have a last hit card to remove
        if (_lastHitCard == null)
        {
            Debug.Log("The Escapist: No last hit card to remove");
            yield break;
        }
        
        Debug.Log("The Escapist activating! Removing last hit card: " + _lastHitCard.name);
        
        // Remove the last hit card from player's hand
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand != null && playerHand.cards.Contains(_lastHitCard))
        { 
            Debug.Log("The Escapist: Found last hit card in player's hand - activating");
            playerHand.cards.Remove(_lastHitCard);
             
            // Start the animation and wait for it to complete
            yield return StartCoroutine(AnimateEscapistCardRemoval(_lastHitCard, playerHand));
             
            _lastHitCard = null;
             
            // Note: The Escapist card now destroys itself in TarotCard.cs
            
                                 Debug.Log("The Escapist: Card removal completed - continuing game flow");
                     
                     
                     hitButton.interactable = true;
                     stickButton.interactable = true;
                      
                     finalMessage.text = "The Escapist saved you! Continue playing...";
                     
                     Debug.Log("The Escapist: Game continues - player can hit or stand");
        }
        else
        {
            Debug.LogWarning("The Escapist: Last hit card not found in player's hand");
        }
    }
    
    /// <summary>
    /// Animate the removal of a card by The Escapist
    /// </summary>
    private IEnumerator AnimateEscapistCardRemoval(GameObject cardToRemove, CardHand playerHand)
    {
        if (cardToRemove == null) yield break; // Safety check
        
        // Create a dramatic escape animation
        Sequence escapeSequence = DOTween.Sequence();
        
        // Flash the card white to indicate The Escapist's intervention
        Image spriteRenderer = cardToRemove.GetComponent<Image>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            escapeSequence.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            escapeSequence.Append(spriteRenderer.DOColor(originalColor, 0.1f));
            escapeSequence.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            escapeSequence.Append(spriteRenderer.DOColor(originalColor, 0.1f));
        }
        
        // Scale up and rotate for dramatic effect
        escapeSequence.Append(cardToRemove.transform.DOScale(cardToRemove.transform.localScale * 1.3f, 0.2f)
            .SetEase(Ease.OutQuad));
        escapeSequence.Join(cardToRemove.transform.DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad));
        
        // Fade out and shrink
        if (spriteRenderer != null)
        {
            escapeSequence.Join(spriteRenderer.DOFade(0f, 0.3f));
        }
        escapeSequence.Join(cardToRemove.transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InQuart));
        
        yield return escapeSequence.WaitForCompletion();
        
        // After animation completes, destroy the card and update the hand
        if (cardToRemove != null)
        {
            Destroy(cardToRemove);
        }
        
        // Rearrange remaining cards and update points
        if (playerHand != null)
        {
            playerHand.ArrangeCardsInWindow();
            playerHand.UpdatePoints();
        }
        UpdateScoreDisplays();
        
        Debug.Log("The Escapist: Card removal animation completed");
    }
    
    /// <summary>
    /// Remove The Escapist card from the tarot panel after use
    /// </summary>
    private void RemoveEscapistFromTarotPanel()
    {
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager == null || shopManager.tarotPanel == null)
        {
            Debug.LogWarning("Cannot find ShopManager or tarot panel to remove The Escapist");
            return;
        }
        
        // Find The Escapist card in the tarot panel
        TarotCard[] tarotCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        foreach (TarotCard card in tarotCards)
        {
            if (card.cardData != null && card.cardData.cardType == TarotCardType.TheEscapist && !card.isInShop)
            {
                Debug.Log("Removing The Escapist from tarot panel: " + card.cardData.cardName);
                
                // Also remove from PlayerStats if it exists there
                if (PlayerStats.instance != null && PlayerStats.instance.ownedCards != null)
                {
                    PlayerStats.instance.ownedCards.Remove(card.cardData);
                }
                
                // Animate the card destruction
                StartCoroutine(AnimateEscapistDestruction(card.gameObject));
                break;
            }
        }
    }
    
    /// <summary>
    /// Animate The Escapist card destroying itself
    /// </summary>
    private IEnumerator AnimateEscapistDestruction(GameObject escapistCard)
    {
        if (escapistCard == null) yield break;
        
        // Create a self-destruction animation
        Sequence destructionSequence = DOTween.Sequence();
        
        // Flash red to indicate self-destruction
        Image cardImage = escapistCard.GetComponent<Image>();
        if (cardImage != null)
        {
            Color originalColor = cardImage.color;
            destructionSequence.Append(cardImage.DOColor(Color.red, 0.15f));
            destructionSequence.Append(cardImage.DOColor(originalColor, 0.15f));
            destructionSequence.Append(cardImage.DOColor(Color.red, 0.15f));
        }
        
        // Shake and scale up before destruction
        destructionSequence.Append(escapistCard.transform.DOShakePosition(0.3f, 20f, 20, 90, false, true));
        destructionSequence.Join(escapistCard.transform.DOScale(escapistCard.transform.localScale * 1.2f, 0.3f));
        
                         // Final destruction - fade out and shrink
                 if (cardImage != null)
                 {
                     destructionSequence.Append(cardImage.DOFade(0f, 0.4f));
                 }
                 destructionSequence.Join(escapistCard.transform.DOScale(Vector3.zero, 0.4f)
                     .SetEase(Ease.InQuart));
                 
                 yield return destructionSequence.WaitForCompletion();
                 
                 // Destroy the card object after animation completes
                 if (escapistCard != null)
                 {
                     Destroy(escapistCard);
                 }
        
        Debug.Log("The Escapist has been destroyed after saving the player");
    }

    
    // Method to handle streak rewards and multiplier calculation
    private void HandleStreakRewards()
    {
        // Calculate streak level (capped at MaxStreakLevel)
        _streakMultiplier = Mathf.Min(_currentStreak / Constants.StreakMultiplierIncrement, Constants.MaxStreakLevel);
        
        // Update UI to reflect new streak level
        UpdateStreakUI();
        
        // Log streak information
        Debug.Log("Streak updated: Level=" + _streakMultiplier + ", Streak=" + _currentStreak +
                 ", Multiplier=" + CalculateWinMultiplier().ToString("F2") + "x");
    }

    // Update streak UI
    private void UpdateStreakUI()
    {
        // Always show the streak panel
        if (streakPanel != null)
        {
            streakPanel.SetActive(true);
        }
        
        if (streakText != null)
        {
            if (_currentStreak > 0)
            {
                // Display streak count and multiplier
                float multiplier = CalculateWinMultiplier();
                streakText.text = multiplier.ToString("0.0") + "x";
            }
            else
            {
                // Show base multiplier when no streak
                streakText.text = "1x";
            }
        }
        
        // Update flame effect if available - always pass at least level 1
        if (streakFlameEffect != null)
        {
            int flameLevel = _streakMultiplier > 0 ? _streakMultiplier : 1;
            streakFlameEffect.SetStreakLevel(flameLevel);
        }
    }
    


    private void InitializeBettingState()
    {
        // Reset bet placement state
        _isBetPlaced = false;
        
        // Reset turn-based variables
        _currentTurn = GameTurn.Player;
        _playerStood = false;
        _dealerStood = false;
        _gameInProgress = false;
        
        // Reset Round Flow tracking variables
        _hitsThisHand = 0;
        _actionsRemainingThisHand = _maxActionsPerHand;
        _tarotsUsedThisHand = 0;
        _hasDoubledDown = false;
        
        // Reset action cards for new hand
        ResetActionCards();
        
        // Reset ActionCardManager for new hand (resets per-hand usage for ActionCardSlotUI)
        if (ActionCardManager.Instance != null)
        {
            ActionCardManager.Instance.ResetForNewHand();
        }
        
        // Clear any existing cards
        if (player != null && player.GetComponent<CardHand>() != null)
        {
            player.GetComponent<CardHand>().Clear();
        }
        if (dealer != null && dealer.GetComponent<CardHand>() != null)
        {
            dealer.GetComponent<CardHand>().Clear();
        }
        
        // Check if auto-reshuffle is needed
        CheckAutoReshuffle();
        
        // Disable game action buttons until bet is placed
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        playAgainButton.interactable = false;
        if (doubleDownButton != null)
        {
            doubleDownButton.interactable = false;
        }
        
        // BETTING SYSTEM 2.0: Enable BettingManager for new round
        if (bettingManager != null)
        {
            bettingManager.EnableBetting();
        }
        else
        {
            Debug.LogWarning("[Deck] BettingManager not found! Assign it in Inspector.");
        }
        
        // SCORE SYSTEM 2.0: Clear score displays via ScoreManager
        if (scoreManager != null)
        {
            scoreManager.UpdateAllScores();
        }
        probMessage.text = "";
        finalMessage.text = "Place your bet to start the round!";
        
        // Update Round Flow UI
        UpdateRoundFlowUI();
        
        // Reapply boss bet range if boss is active (fixes bet range reset issue)
        if (bossManager != null && bossManager.currentBoss != null && bossManager.currentBoss.modifiesBetRange)
        {
            SetMinBet(bossManager.currentBoss.customMinBet);
            SetMaxBet(bossManager.currentBoss.customMaxBet);
            Debug.Log($"Reapplied boss bet range: ${bossManager.currentBoss.customMinBet} - ${bossManager.currentBoss.customMaxBet}");
        }
        
        // Reset tarot ability usage for new round
        _hasUsedPeekThisRound = false;
        _hasUsedTransformThisRound = false;
        
        // Reset new tarot card usage for new round
        _hasUsedSpyThisRound = false;
        _hasUsedBlindSeerThisRound = false;
        _hasUsedCorruptJudgeThisRound = false;
        _hasUsedHitmanThisRound = false;
        _hasUsedFortuneTellerThisRound = false;
        _hasUsedMadWriterThisRound = false;
        
        // Reset passive card activation for new round
        _hasActivatedBotanistThisRound = false;
        _hasActivatedAssassinThisRound = false;
        _hasActivatedSecretLoverThisRound = false;
        _hasActivatedJewelerThisRound = false;
        _hasActivatedHouseKeeperThisRound = false;
        _hasActivatedWitchDoctorThisRound = false;
        _hasActivatedArtificerThisRound = false;
        
        // Don't reset _lastHitCard here - it should persist until The Escapist is used
        // or until a new game starts (PlayAgain)
        
        Debug.Log("Initialized betting state - waiting for bet placement");
    }
    private IEnumerator DealInitialCardsAnimated()
    {
        // Disable game action buttons during dealing
        hitButton.interactable = false;
        stickButton.interactable = false;

        // Deal cards alternately: player, dealer, player, dealer
        for (int i = 0; i < Constants.InitialCardsDealt; ++i)
        {
            // Deal to player first
            yield return StartCoroutine(PushPlayerAnimated());
            yield return new WaitForSeconds(Constants.CardDealDelay);

            // Then deal to dealer
            yield return StartCoroutine(PushDealerAnimated());
            yield return new WaitForSeconds(Constants.CardDealDelay);
        }

        // Apply Captain's Jack nullification after initial deal
        if (bossManager != null && bossManager.currentBoss != null && 
            bossManager.currentBoss.bossType == BossType.TheCaptain)
        {
            Debug.Log("Applying Captain's Jack nullification after initial deal");
            ApplyCaptainJackNullification();
        }

        // 🔮 Cursed Hourglass effect check — trigger redeal
        /*if (PlayerActuallyHasCard(TarotCardType.CursedHourglass))
        {
            Debug.Log("[CursedHourglass] Active — wiping and redealing hands.");
            yield return StartCoroutine(ActivateCursedHourglassEffect());
            yield break; // Stop here — cards are already redealt in the effect
        }*/

        Debug.Log("DealInitialCardsAnimated: Initializing turn-based system");
        
        // Initialize turn-based system
        _currentTurn = GameTurn.Player;
        _playerStood = false;
        _dealerStood = false;
        _gameInProgress = true;
        
        Debug.Log($"DealInitialCardsAnimated: Turn variables set - Current: {_currentTurn}, InProgress: {_gameInProgress}");

        // Update UI and states
        UpdateScoreDisplays();
        UpdateDiscardButtonState();
        UpdatePeekButtonState();
        UpdateTransformButtonState();

        // Check for blackjack after all cards are dealt
        // Use BlackjackActual to check actual card values, not just visible ones
        if (BlackjackActual(player))
        {
            if (BlackjackActual(dealer))
            {
                EndHand(WinCode.Draw);
            }
            else
            {
                EndHand(WinCode.PlayerWins);
            }
            yield break;
        }
        else if (BlackjackActual(dealer))
        {
            EndHand(WinCode.DealerWins);
            yield break;
        }

        // Start turn-based gameplay
        StartTurnBasedGameplay();
    }
    
    /// <summary>
    /// Initialize turn-based gameplay
    /// </summary>
    private void StartTurnBasedGameplay()
    {
        Debug.Log("=== STARTING TURN-BASED GAMEPLAY ===");
        Debug.Log($"Game in progress: {_gameInProgress}");
        _currentTurn = GameTurn.Player;
        Debug.Log($"Current turn set to: {_currentTurn}");
        UpdateTurnUI();
        EnablePlayerControls();
        Debug.Log($"Hit button interactable: {hitButton.interactable}");
        Debug.Log($"Stand button interactable: {stickButton.interactable}");
        Debug.Log("=== TURN-BASED GAMEPLAY STARTED ===");
    }
    
    /// <summary>
    /// Update UI to show whose turn it is
    /// </summary>
    private void UpdateTurnUI()
    {
        if (!_gameInProgress)
        {
            finalMessage.text = "";
            return;
        }
        
        switch (_currentTurn)
        {
            case GameTurn.Player:
                finalMessage.text = "Your Turn - Hit or Stand";
                finalMessage.gameObject.SetActive(true);
                break;
            case GameTurn.Dealer:
                finalMessage.text = "Dealer's Turn";
                finalMessage.gameObject.SetActive(true);
                break;
            case GameTurn.GameOver:
                // Game over message will be set by EndHand
                break;
        }
    }
    
    /// <summary>
    /// Enable player controls for their turn
    /// </summary>
    private void EnablePlayerControls()
    {
        Debug.Log($"EnablePlayerControls called - Current turn: {_currentTurn}, Game in progress: {_gameInProgress}");
        
        if (_currentTurn != GameTurn.Player || !_gameInProgress)
        {
            Debug.Log("Disabling controls - not player turn or game not in progress");
            hitButton.interactable = false;
            stickButton.interactable = false;
            if (doubleDownButton != null)
            {
                doubleDownButton.interactable = false;
            }
            return;
        }
        
        Debug.Log("Enabling player controls");
        hitButton.interactable = true;
        stickButton.interactable = true;
        UpdateDiscardButtonState();
        UpdatePeekButtonState();
        UpdateTransformButtonState();
        UpdateDoubleDownButtonState();
        
        Debug.Log($"After enabling - Hit: {hitButton.interactable}, Stand: {stickButton.interactable}, DD: {(doubleDownButton != null ? doubleDownButton.interactable.ToString() : "null")}");
    }
    
    /// <summary>
    /// Disable all player controls
    /// </summary>
    private void DisablePlayerControls()
    {
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        if (doubleDownButton != null)
        {
            doubleDownButton.interactable = false;
        }
    }
    
    /// <summary>
    /// Update Double Down button state based on game conditions
    /// </summary>
    private void UpdateDoubleDownButtonState()
    {
        if (doubleDownButton == null) return;
        
        // Double Down only available on initial 2-card hand, before any hits, with sufficient balance
        // BETTING SYSTEM 2.0: Check double down availability
        CardHand playerHand = player.GetComponent<CardHand>();
        bool hasEnoughHealth = false;
        if (GameProgressionManager.Instance != null)
        {
            hasEnoughHealth = CurrentBetAmount <= GameProgressionManager.Instance.playerHealthPercentage;
        }
        
        bool canDoubleDown = _isBetPlaced &&
                            _gameInProgress &&
                            _currentTurn == GameTurn.Player &&
                            !_hasDoubledDown &&
                            _hitsThisHand == 0 &&
                            playerHand.cards.Count == Constants.InitialCardsDealt &&
                            hasEnoughHealth;
        
        doubleDownButton.interactable = canDoubleDown;
        float currentHealth = GameProgressionManager.Instance != null ? GameProgressionManager.Instance.playerHealthPercentage : 0f;
        Debug.Log($"Double Down button state: {canDoubleDown} (Bet: {CurrentBetAmount:F0}, Health: {currentHealth:F0}, Hits: {_hitsThisHand}, Cards: {playerHand.cards.Count})");
    }
    
    private IEnumerator ReplaceCardWithInitialAnimation(CardModel clickedCard)
    {
        CardHand hand = player.GetComponent<CardHand>();
        if (hand == null) yield break;

        // Step 1: Remove the clicked card from hand and destroy it
        hand.cards.Remove(clickedCard.gameObject);
        Destroy(clickedCard.gameObject);

        yield return new WaitForSeconds(0.1f); // tiny delay before adding new card

        // Step 2: Add a new card using your standard PushPlayerAnimated animation
        yield return StartCoroutine(PushPlayerAnimated());

        // Step 3: Update UI
        UpdateScoreDisplays();

        Debug.Log("[Makeup Artist] Replaced card using normal deal animation.");
    }
    public void TriggerCursedHourglassEffect()
    {
        StartCoroutine(ActivateCursedHourglassEffect());
    }
    public IEnumerator ActivateCursedHourglassEffect()
    {
        Debug.Log("[CursedHourglass] Activated: Removing all cards and re-dealing...");

        // Get CardHand references
        CardHand playerHand = player?.GetComponent<CardHand>();
        CardHand dealerHand = dealer?.GetComponent<CardHand>();

        // 1. Destroy only the actual card GameObjects from the cards list
        // DO NOT iterate through transform children - that destroys the SlotsContainer!
        if (playerHand != null)
        {
            foreach (GameObject card in playerHand.cards)
            {
                if (card != null) Destroy(card);
            }
            playerHand.cards.Clear();
        }

        if (dealerHand != null)
        {
            foreach (GameObject card in dealerHand.cards)
            {
                if (card != null) Destroy(card);
            }
            dealerHand.cards.Clear();
        }

        // 2. Reset hit counter since we're starting fresh
        ResetHitsThisHand();

        // 3. Wait a short moment to allow destroy to process
        yield return new WaitForSeconds(0.4f);

        // 4. Re-deal exactly 2 new cards to each hand
        for (int i = 0; i < 2; i++)
        {
            yield return StartCoroutine(PushPlayerAnimated());
            yield return new WaitForSeconds(Constants.CardDealDelay);

            yield return StartCoroutine(PushDealerAnimated());
            yield return new WaitForSeconds(Constants.CardDealDelay);
        }

        // 5. Update UI
        UpdateScoreDisplays();
        Debug.Log("[CursedHourglass] Effect completed - hands re-dealt");
    }
    public IEnumerator ReplaceCardWithMakeupArtist(CardModel cardToReplace)
    {
        Debug.Log("[Makeup Artist] Replacing selected card...");

        CardHand hand = player.GetComponent<CardHand>();
        if (hand == null || cardToReplace == null) yield break;

        // Remove from hand logic
        hand.cards.Remove(cardToReplace.gameObject);
        Destroy(cardToReplace.gameObject);

        yield return new WaitForSeconds(0.2f);

        // Deal one new card to player
        yield return StartCoroutine(PushPlayerAnimated());

        UpdateScoreDisplays();
    }
    public IEnumerator ActivateWhisperOfThePastEffect()
    {
        Debug.Log("[WhisperOfThePast] Start effect");

        // Get player hand reference
        CardHand playerHand = player?.GetComponent<CardHand>();
        
        // Destroy only the actual card GameObjects from the cards list
        // DO NOT iterate through transform children - that destroys the SlotsContainer!
        if (playerHand != null)
        {
            foreach (GameObject card in playerHand.cards)
            {
                if (card != null) Destroy(card);
            }
            playerHand.cards.Clear();
        }

        // Reset hit counter since player's hand is completely cleared
        ResetHitsThisHand();

        yield return new WaitForSeconds(0.4f); // small delay for visual clarity

        // Do NOT deal new cards here — player will hit manually later

        // Update the UI after discarding cards
        UpdateScoreDisplays();
        UpdateDiscardButtonState();
        UpdatePeekButtonState();
        UpdateTransformButtonState();

        // Re-enable hit/stick buttons if needed
        hitButton.interactable = true;
        stickButton.interactable = true;
        
        Debug.Log("[WhisperOfThePast] Effect completed - player can hit for new cards");
    }
    public IEnumerator ActivateSaboteurEffect()
    {
        Debug.Log("[Saboteur] Effect triggered.");

        // 1. Remove all dealer cards - use cards list, NOT transform children
        // DO NOT iterate through transform children - that destroys the SlotsContainer!
        CardHand dealerHand = dealer?.GetComponent<CardHand>();
        if (dealerHand != null)
        {
            foreach (GameObject card in dealerHand.cards)
            {
                if (card != null) Destroy(card);
            }
            dealerHand.cards.Clear();
        }

        yield return new WaitForSeconds(0.3f); // Small delay

        // 2. Re-deal 2 new cards to the dealer only
        for (int i = 0; i < Constants.InitialCardsDealt; ++i)
        {
            yield return StartCoroutine(PushDealerAnimated());
            yield return new WaitForSeconds(Constants.CardDealDelay);
        }

        // 3. Refresh score and UI
        UpdateScoreDisplays();
        UpdateDiscardButtonState();
        UpdatePeekButtonState();
        UpdateTransformButtonState();
    }
    private IEnumerator PushPlayerAnimated()
    {
        yield return StartCoroutine(PushAnimated(player, true));
        UpdateScoreDisplays();
        CalculateProbabilities();
    }
    private IEnumerator PushDealerAnimated()
    {
        yield return StartCoroutine(PushAnimated(dealer, false));
        // SCORE SYSTEM 2.0: Update scores after animated dealer card
        UpdateScoreDisplays();
    }
    public IEnumerator ActivateScammerEffect()
    {
        Debug.Log("[Scammer] Attempting to reverse dealer win...");

        if (player == null || dealer == null) yield break;

        int playerScore = GetVisibleScore(player, true);
        int dealerScore = GetVisibleScore(dealer, false);

        if (dealerScore > playerScore && dealerScore <= 21)
        {
            CardHand dealerHand = dealer.GetComponent<CardHand>();
            if (dealerHand != null && dealerHand.cards.Count > 0)
            {
                GameObject lastCard = dealerHand.cards[dealerHand.cards.Count - 1];
                dealerHand.cards.RemoveAt(dealerHand.cards.Count - 1);
                Destroy(lastCard);
                Debug.Log("[Scammer] Removed dealer's last card to weaken their score.");
            }

            yield return new WaitForSeconds(0.2f);

            UpdateScoreDisplays();

            // 🔁 FIX: Convert existing win/lose/draw to CONTINUE
            if (finalMessage != null)
            {
                finalMessage.text = "Continue";
                finalMessage.gameObject.SetActive(true);
                hitButton.interactable = true;
                stickButton.interactable = true;
            }

            // ✅ Wait 1 second then hide it and resume game
            yield return new WaitForSeconds(1f);

            if (finalMessage != null)
            {
                finalMessage.gameObject.SetActive(false);
            }

            int newDealerScore = GetVisibleScore(dealer, false);

            if (newDealerScore < 17)
            {
                yield return StartCoroutine(PushDealerAnimated()); // Resume dealer turn
            }
            else
            {
                // Even after dropping, dealer might be done — recheck winner
                ResolveEndOfRound();
            }
        }
        else
        {
            Debug.Log("[Scammer] Dealer wasn't winning, no need to reverse.");
        }
    }
    private void ResolveEndOfRound()
    {
        int playerScore = GetVisibleScore(player, true);
        int dealerScore = GetVisibleScore(dealer, false);

        if (dealerScore > 21 || playerScore > dealerScore)
        {
            EndHand(WinCode.PlayerWins);
        }
        else if (dealerScore == playerScore)
        {
            EndHand(WinCode.Draw);
        }
        else
        {
            EndHand(WinCode.DealerWins);
        }
    }
    private void ShowResult(string message)
    {
        if (finalMessage != null)
        {
            // Make sure previous message is hidden first
            finalMessage.gameObject.SetActive(false);

            // Then show the new one
            finalMessage.text = message;
            finalMessage.gameObject.SetActive(true);
        }
    }


    private IEnumerator PushAnimated(GameObject handOwner, bool isPlayer)
    {
        CardHand hand = handOwner.GetComponent<CardHand>();
        
        // Check if hand can accept more cards
        if (hand.cards.Count >= Constants.MaxCardsInHand)
        {
            Debug.LogWarning("Maximum card limit reached (" + Constants.MaxCardsInHand + ")");
            yield break;
        }
        
        // === PLAYER DRAWS FROM PlayerDeck ===
        if (isPlayer && playerDeck != null)
        {
            // Check if PlayerDeck needs reshuffle
            if (playerDeck.RemainingCards == 0)
            {
                Debug.Log("[Deck] PlayerDeck is empty, reshuffling...");
                playerDeck.ReshuffleDiscardPile();
            }
            
            // Draw from PlayerDeck
            PlayerDeckCard playerCard = playerDeck.DrawCard();
            
            if (playerCard == null)
            {
                Debug.LogWarning("[Deck] Failed to draw card from PlayerDeck!");
                yield break;
            }
            
            // Check if The Seductress should intercept this card
            if (bossManager != null && bossManager.currentBoss != null && 
                bossManager.currentBoss.bossType == BossType.TheSeductress)
            {
                bool isKingOrJack = (playerCard.value == 11 || playerCard.value == 13); // Jack=11, King=13
                
                Debug.Log($"Seductress check: {playerCard.displayName} - isKingOrJack: {isKingOrJack}");
                
                if (isKingOrJack)
                {
                    Debug.Log($"The Seductress intercepts {playerCard.displayName} during animated deal!");
                    
                    // Deal the card to the dealer instead
                    CardHand dealerHand = dealer.GetComponent<CardHand>();
                    
                    if (dealerHand.cards.Count >= Constants.MaxCardsInHand)
                    {
                        Debug.LogWarning("Maximum card limit reached for dealer (" + Constants.MaxCardsInHand + ")");
                        yield break;
                    }
                    
                    // Create intercepted card for dealer using PlayerDeck card data
                    GameObject interceptedCard = dealerHand.CreateCard(playerCard.cardSprite, playerCard.GetBlackjackValue(), true, playerCard.id);
                    
                    if (interceptedCard != null)
                    {
                        Vector3 finalPosition = CalculateFinalCardPosition(dealerHand, dealerHand.cards.Count - 1);
                        yield return StartCoroutine(AnimateSeductressInterceptedCard(interceptedCard, finalPosition));
                        dealerHand.ArrangeCardsInWindow();
                        
                        // Create CardInfo for boss mechanic
                        CardInfo cardInfo = new CardInfo(playerCard.id, playerCard.GetBlackjackValue(), playerCard.cardSprite, faces);
                        
                        var mechanic = bossManager.currentBoss.GetMechanic(BossMechanicType.SeductressIntercept);
                        if (mechanic != null)
                        {
                            yield return bossManager.HandleSeductressInterception(interceptedCard, cardInfo, true);
                        }
                        
                        bossManager.OnCardDealt(interceptedCard, false);
                    }
                    
                    yield break; // Card was intercepted
                }
            }
            
            // Create card for player using PlayerDeck card data
            GameObject newCard = hand.CreateCard(playerCard.cardSprite, playerCard.GetBlackjackValue(), true, playerCard.id);
            
            Debug.Log($"[Deck] Player drew: {playerCard.displayName} (Value: {playerCard.GetBlackjackValue()})");
            
            if (newCard != null)
            {
                Vector3 finalPosition = CalculateFinalCardPosition(hand, hand.cards.Count - 1);
                yield return StartCoroutine(AnimateCardDealing(newCard, finalPosition));
                hand.ArrangeCardsInWindow();
                
                // Notify boss system
                if (bossManager != null)
                {
                    bossManager.OnCardDealt(newCard, true);
                }
            }
            
            yield break; // Done with player card
        }
        
        // === DEALER DRAWS FROM MAIN DECK ===
        // Check if auto-reshuffle is needed before dealing
        CheckAutoReshuffle();
        
        // Check if we're reaching the end of the deck and need to reshuffle
        if (cardIndex >= values.Length - 1)
        {
            Debug.Log("Dealer drawing - Deck is almost empty, force reshuffling...");
            ShuffleCards();
            cardIndex = 0;
        }
        
        // Create the card for dealer from main deck
        GameObject dealerCard = hand.CreateCard(faces[cardIndex], values[cardIndex], true, originalIndices[cardIndex]);
        
        Debug.Log($"[Deck] Dealer drew card at index {cardIndex}");
        
        cardIndex++;
        
        if (dealerCard != null)
        {
            // Calculate final position before animation
            Vector3 finalPosition = CalculateFinalCardPosition(hand, hand.cards.Count - 1);
            
            // Animate the card from deck position to its final position
            yield return StartCoroutine(AnimateCardDealing(dealerCard, finalPosition));
            
            // Arrange all cards after animation completes
            hand.ArrangeCardsInWindow();
            
            // Notify boss system about dealer card
            if (bossManager != null)
            {
                bossManager.OnCardDealt(dealerCard, false);
            }
        }
    }
public GameObject SpawnCardWithValue(CardHand targetHand, int forcedValue)
{
    if (targetHand == null) return null;

    // Just reuse the last card in hand as a placeholder
    if (targetHand.cards.Count == 0) return null;

    // Clone the last card
    GameObject newCard = Instantiate(targetHand.cards[targetHand.cards.Count - 1], targetHand.transform);
    CardModel cm = newCard.GetComponent<CardModel>();
    if (cm != null)
    {
        cm.value = forcedValue;
        cm.ToggleFace(true);
    }

    targetHand.cards.Add(newCard);
    targetHand.ArrangeCardsInWindow();
    targetHand.UpdatePoints();

    return newCard;
}

    private Vector3 CalculateFinalCardPosition(CardHand hand, int cardIndex)
    {
        // Calculate where this card should end up in the hand
        float panelWidth = 20f;
        float cardSpacing = 3.2f; // CARD_SPACING from CardHand
        float cardScale = 7.5f; // CARD_SCALE from CardHand
        
        Image cardSprite = hand.card.GetComponent<Image>();
        float cardWidth = 0;
        
        if (cardSprite != null && cardSprite.sprite != null)
        {
            cardWidth = cardSprite.sprite.bounds.size.x * cardScale * 0.8f;
        }
        else
        {
            cardWidth = 1f * cardScale;
        }
        
        cardWidth = Mathf.Min(cardWidth, 3f);
        cardWidth = Mathf.Max(cardWidth, 0.2f);
        
        int totalCards = hand.cards.Count;
        float availableWidth = panelWidth;
        float actualSpacing = Mathf.Min(cardSpacing, (availableWidth - (cardWidth * totalCards)) / Mathf.Max(1, totalCards - 1));
        actualSpacing = Mathf.Max(actualSpacing, -cardWidth * 0.5f);
        
        float totalWidth = (totalCards > 1) ? 
            cardWidth + (actualSpacing * (totalCards - 1)) : 
            cardWidth;
            
        float startX = -totalWidth / 2 + (cardWidth / 2);
        float posX = startX + (cardIndex * (cardWidth + actualSpacing));
        
        return new Vector3(posX, 0, 0);
    }

    private IEnumerator AnimateCardDealing(GameObject card, Vector3 finalPosition)
    {
        if (card == null) yield break;
        
        // Store the final scale
        Vector3 finalScale = card.transform.localScale;
        
        // Determine start position
        Vector3 startPosition;
        if (deckPosition != null)
        {
            // Convert deck world position to local position relative to the card's parent
            startPosition = card.transform.parent.InverseTransformPoint(deckPosition.position);
        }
        else
        {
            // Default: start from center and slightly above
            startPosition = new Vector3(0, Constants.CardDealDistance, 0);
        }
        
        // Set initial state
        card.transform.localPosition = startPosition;
        card.transform.localScale = Vector3.zero;
        
        // Create animation sequence
        Sequence dealSequence = DOTween.Sequence();
        
        // Scale up the card with a nice bounce effect
        dealSequence.Append(card.transform.DOScale(finalScale, Constants.CardDealDuration * 0.3f)
            .SetEase(Ease.OutBack, 1.1f));
        
        // Move to final position (overlapping with scale)
        dealSequence.Join(card.transform.DOLocalMove(finalPosition, Constants.CardDealDuration)
            .SetEase(Ease.OutQuart));
        
        // Add a slight rotation for more dynamic feel
        card.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
        dealSequence.Join(card.transform.DORotate(Vector3.zero, Constants.CardDealDuration * 0.6f)
            .SetEase(Ease.OutQuart));
        
        // Add a subtle "land" effect at the end
        dealSequence.AppendCallback(() => {
            // Small bounce when card lands
            card.transform.DOPunchScale(Vector3.one * 0.08f, 0.15f, 1, 0.3f);
            
            // Hook for sound effects (can be implemented later)
            OnCardDealt();
        });
        
        // Wait for animation to complete
        yield return dealSequence.WaitForCompletion();
        
        // Ensure final position and scale are exact
        card.transform.localPosition = finalPosition;
        card.transform.localScale = finalScale;
        card.transform.rotation = Quaternion.identity;
        
        // Update the card's original position after animation is complete
        CardModel cardModel = card.GetComponent<CardModel>();
        if (cardModel != null)
        {
            cardModel.UpdateOriginalPosition();
        }
    }

    // Hook for sound effects and other card dealing events
    private void OnCardDealt()
    {
        // This can be used to trigger sound effects, particle effects, etc.
        // For now, just log the event
        Debug.Log("Card dealt with animation");
    }
    
    // Special animation for The Seductress intercepted cards
    private IEnumerator AnimateSeductressInterceptedCard(GameObject card, Vector3 finalPosition)
    {
        if (card == null) yield break;
        
        // Store the final scale
        Vector3 finalScale = card.transform.localScale;
        
        // Determine start position
        Vector3 startPosition;
        if (deckPosition != null)
        {
            startPosition = card.transform.parent.InverseTransformPoint(deckPosition.position);
        }
        else
        {
            startPosition = new Vector3(0, Constants.CardDealDistance, 0);
        }
        
        // Set initial state
        card.transform.localPosition = startPosition;
        card.transform.localScale = Vector3.zero;
        
        // Create special seduction animation sequence
        Sequence seductionDealSequence = DOTween.Sequence();
        
        // Scale up with pink glow
        Image spriteRenderer = card.GetComponent<Image>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.4f, 0.7f); // Start with pink color
            seductionDealSequence.Append(spriteRenderer.DOColor(Color.white, Constants.CardDealDuration * 0.5f));
        }
        
        seductionDealSequence.Join(card.transform.DOScale(finalScale, Constants.CardDealDuration * 0.3f)
            .SetEase(Ease.OutBack, 1.1f));
        
        // Special curved movement for seduction effect
        Vector3 midPoint = Vector3.Lerp(startPosition, finalPosition, 0.5f) + new Vector3(2f, 3f, 0); // Curve upward
        
        seductionDealSequence.Join(card.transform.DOPath(new Vector3[] { startPosition, midPoint, finalPosition }, 
            Constants.CardDealDuration, PathType.CubicBezier)
            .SetEase(Ease.OutQuart));
        
        // Add seductive rotation
        seductionDealSequence.Join(card.transform.DORotate(new Vector3(0, 0, 720), Constants.CardDealDuration)
            .SetEase(Ease.OutQuart));
        
        // Landing effect with pink flash
        seductionDealSequence.AppendCallback(() => {
            if (spriteRenderer != null)
            {
                spriteRenderer.DOColor(new Color(1f, 0.4f, 0.7f), 0.2f)
                    .SetLoops(2, LoopType.Yoyo);
            }
            card.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 1, 0.5f);
        });
        
        yield return seductionDealSequence.WaitForCompletion();
        
        // Ensure final position and scale are exact
        card.transform.localPosition = finalPosition;
        card.transform.localScale = finalScale;
        card.transform.rotation = Quaternion.identity;
        
        // Update card's original position
        CardModel cardModel = card.GetComponent<CardModel>();
        if (cardModel != null)
        {
            cardModel.UpdateOriginalPosition();
        }
        
        Debug.Log("The Seductress intercepted card animation completed");
    }
    
    // BETTING SYSTEM 2.0: Removed all button hold functionality - no longer needed

    /// <summary>
    /// Modify deck for The Captain boss - only Jacks and all Spades
    /// </summary>
    public void ModifyDeckForCaptain()
    {
        Debug.Log("=== MODIFYING DECK FOR CAPTAIN ===");
        Debug.Log($"Original deck size: {faces.Length} cards");
        
        // Create a new deck with only Jacks and Spades
        List<Sprite> newFaces = new List<Sprite>();
        List<int> newValues = new List<int>();
        List<int> newOriginalIndices = new List<int>();
        
        // Add all Jacks (one from each suit)
        for (int suit = 0; suit < 4; suit++)
        {
            int jackIndex = (suit * Constants.CardsPerSuit) + 10; // Jack is at index 10 in each suit
            if (jackIndex < faces.Length)
            {
                newFaces.Add(faces[jackIndex]);
                newValues.Add(values[jackIndex]);
                newOriginalIndices.Add(originalIndices[jackIndex]);
                Debug.Log($"Added Jack: {GetCardInfo(jackIndex).cardName}");
            }
        }
        
        // Add all Spades (13 cards: A, 2-10, J, Q, K)
        int spadesStartIndex = (int)CardSuit.Spades * Constants.CardsPerSuit; // 39
        for (int i = 0; i < Constants.CardsPerSuit; i++)
        {
            int spadeIndex = spadesStartIndex + i;
            if (spadeIndex < faces.Length)
            {
                newFaces.Add(faces[spadeIndex]);
                newValues.Add(values[spadeIndex]);
                newOriginalIndices.Add(originalIndices[spadeIndex]);
                Debug.Log($"Added Spade: {GetCardInfo(spadeIndex).cardName}");
            }
        }
        
        // Replace the deck arrays
        faces = newFaces.ToArray();
        values = newValues.ToArray();
        originalIndices = newOriginalIndices.ToArray();
        
        Debug.Log($"Captain's deck created: {faces.Length} cards ({newFaces.Count - 4} Spades + 4 Jacks)");
        
        // Reset card index and shuffle the new deck
        cardIndex = 0;
        ShuffleCards();
        
        // Debug: Print the first few cards after shuffle
        Debug.Log("First 5 cards in Captain's deck after shuffle:");
        for (int i = 0; i < Mathf.Min(5, faces.Length); i++)
        {
            CardInfo cardInfo = new CardInfo(i, values[i], faces[i], faces);
            Debug.Log($"  Card {i}: {cardInfo.cardName}");
        }
        
        Debug.Log("=== DECK MODIFICATION COMPLETE ===");
        
        // Show message to player
        ShowCaptainDeckMessage();
    }
    
    /// <summary>
    /// Check if a card is a Jack (any suit)
    /// </summary>
    public bool IsJack(CardInfo cardInfo)
    {
        return cardInfo.suitIndex == 10; // Jack is at index 10 in each suit
    }
    
    /// <summary>
    /// Check if a card is a Jack using CardModel
    /// </summary>
    public bool IsJack(CardModel cardModel)
    {
        if (cardModel == null) return false;
        CardInfo cardInfo = GetCardInfoFromModel(cardModel);
        return IsJack(cardInfo);
    }
    
    /// <summary>
    /// Check if a card is a King (any suit)
    /// </summary>
    public bool IsKing(CardInfo cardInfo)
    {
        return cardInfo.suitIndex == 12; // King is at index 12 in each suit
    }
    
    /// <summary>
    /// Check if a card is a King using CardModel
    /// </summary>
    public bool IsKing(CardModel cardModel)
    {
        if (cardModel == null) return false;
        CardInfo cardInfo = GetCardInfoFromModel(cardModel);
        return IsKing(cardInfo);
    }
    
    /// <summary>
    /// Apply The Captain's Jack nullification to player's hand
    /// </summary>
    public void ApplyCaptainJackNullification()
    {
        if (player == null) return;
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null) return;
        
        bool hasJacks = false;
        foreach (GameObject cardObj in playerHand.cards)
        {
            CardModel cardModel = cardObj.GetComponent<CardModel>();
            if (cardModel != null && IsJack(cardModel))
            {
                // Nullify Jack value (set to 0)
                cardModel.value = 0;
                hasJacks = true;
                
                // Visual feedback - make the card appear "nullified"
                Image spriteRenderer = cardObj.GetComponent<Image>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(0.7f, 0.7f, 0.7f, 0.8f); // Gray out the card
                }
                
                Debug.Log($"Captain nullified Jack: {GetCardInfoFromModel(cardModel).cardName}");
            }
        }
        
        if (hasJacks)
        {
            // Update points after nullification
            playerHand.UpdatePoints();
            UpdateScoreDisplays();
            Debug.Log("Captain's Jack nullification applied to player's hand");
        }
    }
    
    /// <summary>
    /// Show Captain's deck modification message
    /// </summary>
    public void ShowCaptainDeckMessage()
    {
        if (finalMessage != null)
        {
            finalMessage.text = "The Captain's deck: Jacks and Spades only!\nYour Jacks will be nullified!";
            finalMessage.gameObject.SetActive(true);
            
            // Hide the message after a few seconds
            StartCoroutine(HideCaptainMessage());
        }
    }
    
    private IEnumerator HideCaptainMessage()
    {
        yield return new WaitForSeconds(3f);
        if (finalMessage != null)
        {
            finalMessage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Ensure deck is properly set up for the current boss
    /// </summary>
    public void EnsureDeckSetup()
    {
        // If no boss system or no special deck is needed, do a regular shuffle
        if (bossManager == null || (bossManager.currentBoss != null && !bossManager.currentBoss.usesSpecialDeck))
        {
            if (cardIndex == 0) // Only shuffle if not already done
            {
                ShuffleCards();
                Debug.Log("Regular deck shuffled");
            }
        }
    }

    /// <summary>
    /// Validate that The Captain's deck is properly set up
    /// </summary>
    public void ValidateCaptainDeck()
    {
        Debug.Log("=== VALIDATING CAPTAIN'S DECK ===");
        Debug.Log($"Deck size: {faces.Length} cards");
        
        if (faces.Length != 17)
        {
            Debug.LogError($"Captain's deck should have 17 cards, but has {faces.Length}!");
            return;
        }
        
        int jackCount = 0;
        int spadeCount = 0;
        
        for (int i = 0; i < faces.Length; i++)
        {
            CardInfo cardInfo = new CardInfo(i, values[i], faces[i], faces);
            
            if (cardInfo.suitIndex == 10) // Jack
            {
                jackCount++;
                Debug.Log($"Jack found: {cardInfo.cardName}");
            }
            else if (cardInfo.suit == CardSuit.Spades)
            {
                spadeCount++;
                Debug.Log($"Spade found: {cardInfo.cardName}");
            }
            else
            {
                Debug.LogError($"Invalid card in Captain's deck: {cardInfo.cardName} (Suit: {cardInfo.suit}, SuitIndex: {cardInfo.suitIndex})");
            }
        }
        
        Debug.Log($"Validation complete: {jackCount} Jacks, {spadeCount} Spades");
        
        if (jackCount != 4 || spadeCount != 13)
        {
            Debug.LogError($"Captain's deck validation failed! Expected 4 Jacks and 13 Spades, got {jackCount} Jacks and {spadeCount} Spades");
        }
        else
        {
            Debug.Log("Captain's deck validation successful!");
        }
        
        Debug.Log("=== END VALIDATION ===");
    }
}