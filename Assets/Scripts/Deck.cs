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
        else if (value == 10 && suitIndex == 10) valueName = "Jack";
        else if (value == 10 && suitIndex == 11) valueName = "Queen"; 
        else if (value == 10 && suitIndex == 12) valueName = "King";
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
    
    // Blind progression constants
    public const int SmallBlindRounds = 5;
    public const uint SmallBlindGoal = 300;
    public const int BigBlindRounds = 8;
    public const uint BigBlindGoal = 600;
    public const int MegaBlindRounds = 12;
    public const uint MegaBlindGoal = 1200;
    public const int SuperBlindRounds = 15;
    public const uint SuperBlindGoal = 2000;
    
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
}

// Enum to track current blind level
internal enum BlindLevel
{
    SmallBlind,
    BigBlind,
    MegaBlind,
    SuperBlind,
    Completed
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
    public Text playerScoreText;
    public Text dealerScoreText;
    public Button peekButton; // Eye button for peeking
    public Button transformButton; // Transformation button for card transformation
    public Button raiseBetButton;
    public Button lowerBetButton;
    public Button placeBetButton; // New button to confirm bet placement
    public Text balance;
    public Text bet;
    public Text roundText; // Text to display current round
    public Text blindText; // Text to display current blind
    public Text goalText; // Text to display progress towards goal
    
    // UI elements for streak
    public Text streakText;
    public GameObject streakPanel;
    public StreakFlameEffect streakFlameEffect;
    
    // Game History
    public GameHistoryManager gameHistoryManager;

    private uint _balance = Constants.InitialBalance;
    private uint _bet;
    private bool _isPeeking = false;
    private bool _isBetPlaced = false; // Track if bet has been placed for current round
    public bool _hasUsedPeekThisRound = false; // Track if peek has been used in current round
    public bool _hasUsedTransformThisRound = false; // Track if transform has been used in current round
    
    // Blind progression variables
    private BlindLevel _currentBlind = BlindLevel.SmallBlind;
    private int _currentRound = 1;
    private uint _startingBalanceForBlind;
    private uint _goalForCurrentBlind = Constants.SmallBlindGoal;
    private int _totalRoundsForBlind = Constants.SmallBlindRounds;

    // Add a new field to track blackjack earnings
    private long _earningsForCurrentBlind = 0;

    // Streak variables
    private int _currentStreak = 0;
    private int _streakMultiplier = 1;
    
    // Button hold variables
    private bool _isHoldingRaiseBet = false;
    private bool _isHoldingLowerBet = false;
    private Coroutine _raiseBetCoroutine = null;
    private Coroutine _lowerBetCoroutine = null;
    

    // Public property to access balance
    /*public uint Balance
    {
        get { return _balance; }
        set 
        { 
            // We don't update earnings here anymore - only track in EndHand and OnCardPurchased
            _balance = value;
            UpdateBalanceDisplay();
            UpdateGoalProgress();
            
        }
    }*/
    public uint Balance
    {
        get { return _balance; }
        set
        {
            _balance = value;

            PlayerPrefs.SetInt("UserCash", (int)_balance); // Save to PlayerPrefs
            PlayerPrefs.Save();

            UpdateBalanceDisplay();
            UpdateGoalProgress();
        }
    }


    public int[] values = new int[Constants.DeckCards];
    int cardIndex = 0;  
       
    private void Awake() => 
        InitCardValues();

    private void Start()
    {
        _balance = (uint)PlayerPrefs.GetInt("UserCash", 1000); // Default to 1000 if not saved
        bet.text = _bet.ToString() + " $";
        UpdateBalanceDisplay();

        ShuffleCards();
        
        _startingBalanceForBlind = _balance;
        _earningsForCurrentBlind = 0; // Reset earnings at game start
        bet.text = _bet.ToString() + " $";
        UpdateBalanceDisplay();
        UpdateRoundDisplay();
        UpdateBlindDisplay();
        UpdateGoalProgress();
        UpdateStreakUI(); // Initialize streak display with 1x flame
        
        // Set the button text to "Next Round" at the start
        SetButtonTextToNextRound();
        
        // Debug logging of initial state
        DebugPrintBlindState("Initial setup");
        
        // Disable the next round button until the round is over
        playAgainButton.interactable = false;
        
        // IMPORTANT: Ensure we're referencing the correct PlayerPanel
        EnsureCorrectPlayerReference();
        
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
        
        if (placeBetButton != null)
        {
            placeBetButton.onClick.RemoveAllListeners();
            placeBetButton.onClick.AddListener(PlaceBet);
        }
        
        // Configure bet button hold functionality
        SetupBetButtonHoldListeners();
        
        // Initialize game in betting state (no cards dealt yet)
        InitializeBettingState();
    }
 
    private void InitCardValues()
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
    }
 
    private void FisherYatesShuffle()
    {
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

    private int GetPlayerPoints() => 
        player.GetComponent<CardHand>().points;

    private int GetDealerPoints() => 
        dealer.GetComponent<CardHand>().points;

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
        int playerPoints = GetDealerPoints();
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

    private void PushDealer()
    {
        // Check if we're reaching the end of the deck and need to reshuffle
        if (cardIndex >= values.Length - 1)
        {
            Debug.Log("Dealer drawing - Deck is almost empty, reshuffling...");
            ShuffleCards();
            cardIndex = 0;
        }

        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex], originalIndices[cardIndex]);
        cardIndex++;
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

        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex], originalIndices[cardIndex]);
        cardIndex++;
        UpdateScoreDisplays();  

        CalculateProbabilities();
    }

    public void Hit()
    { 
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot hit: No bet placed");
            return;
        }
        
        CardHand playerHand = player.GetComponent<CardHand>();
        if (!playerHand.CanAddMoreCards())
        {
            finalMessage.text = "Maximum cards reached!";
            return;
        }

        // Start animated hit
        StartCoroutine(HitAnimated());
    }

    private IEnumerator HitAnimated()
    {
        // Disable buttons during animation
        hitButton.interactable = false;
        stickButton.interactable = false;
        
        // Deal card with animation
        yield return StartCoroutine(PushPlayerAnimated());
        
        // Re-enable buttons
        hitButton.interactable = true;
        stickButton.interactable = true;
        
        UpdateDiscardButtonState();  
 
        if (Blackjack(player, true)) { EndHand(WinCode.PlayerWins); }
        else if (GetPlayerPoints() > Constants.Blackjack) { EndHand(WinCode.DealerWins); }
    }

    public void Stand()
    {
        if (!_isBetPlaced)
        {
            Debug.LogWarning("Cannot stand: No bet placed");
            return;
        }
        
        // Start animated stand sequence
        StartCoroutine(StandAnimated());
    }

    private IEnumerator StandAnimated()
    {
        // Disable buttons during animation
        hitButton.interactable = false;
        stickButton.interactable = false;
        
        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;  

        AnimateFlipDealerCard();
        
        // Wait a moment for the flip animation
        yield return new WaitForSeconds(0.25f);
        
        // Dealer draws cards with animation until reaching 17 or more
        while (dealerPoints < Constants.DealerStand)
        {
            yield return StartCoroutine(PushDealerAnimated());
            dealerPoints = dealer.GetComponent<CardHand>().points;
            UpdateScoreDisplays();
            
            // Small delay between dealer cards for dramatic effect
            yield return new WaitForSeconds(Constants.CardDealDelay);
        }
        
        UpdateScoreDisplays();  

        if (dealerPoints > Constants.Blackjack || dealerPoints < playerPoints) 
        { 
            EndHand(WinCode.PlayerWins); 
        }
        else if (playerPoints < dealerPoints) { EndHand(WinCode.DealerWins); }
        else { EndHand(WinCode.Draw); }
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
        FlipDealerCard();
        uint oldBalance = _balance;
        long oldEarnings = _earningsForCurrentBlind;
        
        // Get current scores for history
        int playerScore = GetPlayerPoints();
        int dealerScore = GetDealerPoints();
        string outcomeText = "";
        
        switch (code)
        {
            case WinCode.DealerWins:
                finalMessage.text = "You lose!";
                outcomeText = "Lose";
                if (_bet <= _balance) {
                    // FIXED: Track net loss (bet amount) toward earnings
                    _earningsForCurrentBlind -= _bet; // Lose the bet amount
                    Balance -= _bet; // Lose the bet from balance
                    if (PlayerStats.instance.PlayerHasCard(TarotCardType.WitchDoctor)) {
                        int refund = Mathf.RoundToInt(_bet * 0.1f);
                        Balance += (uint)refund;
                        Debug.Log("Witch Doctor refunded 10% of your bet: " + refund);
                    }
                } else {
                    // Safety check
                    Debug.LogWarning("Bet amount greater than balance! Setting balance to 0.");
                    _earningsForCurrentBlind -= _balance; // Lost whatever was left
                    Balance = 0;
                }
                
                // Reset streak on loss
                _currentStreak = 0;
                _streakMultiplier = 0;
                
                Debug.Log("Loss calculation: Lost Bet=$" + _bet + ", Earnings Impact=-$" + _bet);
                break;
                
            case WinCode.PlayerWins:
                // Increment streak
                _currentStreak++;
                
                // Calculate streak level and multiplier
                _streakMultiplier = Mathf.Min(_currentStreak / Constants.StreakMultiplierIncrement, Constants.MaxStreakLevel);
                float multiplier = CalculateWinMultiplier();
                
                // Calculate suit bonuses from tarot cards (explicitly use player hand)
                uint suitBonuses = CalculateSuitBonuses(player);
                
                // FIXED: Calculate profit correctly for blackjack
                // In blackjack: you get your bet back + profit
                // Base profit = bet amount (1:1 payout)
                // Streak bonus multiplies the profit only
                uint baseProfit = _bet; // Standard blackjack 1:1 payout
                uint streakBonus = (uint)(baseProfit * (multiplier - 1.0f)); // Extra bonus from streak
                uint totalWinnings = _bet + baseProfit + streakBonus + suitBonuses; // Bet returned + base profit + streak bonus + suit bonuses
                uint netEarnings = baseProfit + streakBonus + suitBonuses; // Only profit counts toward earnings (not returned bet)
                
                finalMessage.text = "You win!";
                outcomeText = "Win";
                
                // Add streak info if there's a streak
                if (_currentStreak > 1)
                {
                    finalMessage.text += "\nStreak: " + _currentStreak + " (" + multiplier.ToString("0.0") + "x bonus)";
                }
                
                // Add suit bonus info if there are any
                if (suitBonuses > 0)
                {
                    finalMessage.text += "\nSuit Bonus: +" + suitBonuses;
                }
                
                // Track only NET EARNINGS (profit) toward blind goals
                _earningsForCurrentBlind += netEarnings;
                // Add total winnings (including returned bet) to balance
                Balance += totalWinnings;
                
                Debug.Log("Win calculation: Bet=$" + _bet + ", Base Profit=$" + baseProfit + 
                         ", Streak Bonus=$" + streakBonus + ", Suit Bonuses=$" + suitBonuses + 
                         ", Total Winnings=$" + totalWinnings + ", Net Earnings=$" + netEarnings + 
                         ", Multiplier=" + multiplier.ToString("F2") + "x");
                break;
                
            case WinCode.Draw:
                finalMessage.text = "Draw!";
                outcomeText = "Draw";
                
                Balance += _bet;
                Debug.Log("Draw: Refunded bet amount $" + _bet + " back to player");
                
                // Reset streak on draw
                _currentStreak = 0;
                _streakMultiplier = 0;
                break;
                
            default:
                Debug.Assert(false);    
                break;
        }
        
        // Record game history entry
        if (gameHistoryManager != null)
        {
            string currentBlindName = GetCurrentBlindName();
            GameHistoryEntry historyEntry = new GameHistoryEntry(
                _currentRound,
                currentBlindName,
                playerScore,
                dealerScore,
                _bet,
                oldBalance,
                _balance,
                outcomeText
            );
            Debug.Log("Recording history entry: Round " + _currentRound + ", Outcome: " + outcomeText + ", Bet: " + _bet);
            gameHistoryManager.AddHistoryEntry(historyEntry);
        }
        else
        {
            Debug.LogWarning("GameHistoryManager is null! Cannot record history entry.");
        }
        
        // Update streak UI
        UpdateStreakUI();
        
        Debug.Log("Hand ended: " + code + 
                  " - Old balance: " + oldBalance + 
                  ", New balance: " + _balance + 
                  ", Bet: " + _bet + 
                  ", Balance change: " + (_balance - oldBalance) + 
                  ", Old earnings: " + oldEarnings + 
                  ", New earnings: " + _earningsForCurrentBlind + 
                  ", Current streak: " + _currentStreak +
                  ", Multiplier: " + CalculateWinMultiplier().ToString("F2") + "x");
 
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        raiseBetButton.interactable = false;
        lowerBetButton.interactable = false;
        placeBetButton.interactable = false;
        
        // Enable the next round button when the round is over
        playAgainButton.interactable = true;
 
        _bet = 0;
        bet.text = _bet.ToString() + " $";
        UpdateBalanceDisplay();
        UpdateScoreDisplays();
        UpdateGoalProgress(); // Update goal progress when hand ends
        
        // Debug print the blind state after updating the balance
        DebugPrintBlindState("After EndHand");

        if (Balance == 0)
        {
            finalMessage.text += "\n - GAME OVER -";
            // Change button text to "Play Again" when it's game over
            Text buttonText = playAgainButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Play Again";
            }
            // Don't automatically start NewGame coroutine - let user click Play Again
        }
    }

    public void PlayAgain()
    {   
        // Check if this is a game over scenario (balance is 0 or button text is "Play Again")
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        bool isGameOver = (Balance == 0 || (buttonText != null && buttonText.text == "Play Again"));
        
        if (isGameOver)
        {
            // Instant restart - reload the scene immediately
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }
        
        // Increment round
        _currentRound++;
        
        // Check if we've completed rounds for current blind
        if (_currentRound > _totalRoundsForBlind)
        {
            Debug.Log("Completed all rounds for blind level " + _currentBlind + 
                     " - Total earnings: " + _earningsForCurrentBlind + 
                     ", Goal: " + _goalForCurrentBlind);
                     
            // Make sure earnings aren't negative
            if (_earningsForCurrentBlind < 0)
            {
                Debug.LogWarning("Negative earnings detected at end of blind: " + _earningsForCurrentBlind + ". Resetting to 0.");
                _earningsForCurrentBlind = 0;
            }
                     
            // Check if goal was met using direct earnings tracking
            if (_earningsForCurrentBlind >= _goalForCurrentBlind)
            {
                // Advance to next blind
                AdvanceToNextBlind();
            }
            else
            {
                // Goal not met, game over
                long displayEarnings = _earningsForCurrentBlind < 0 ? 0 : _earningsForCurrentBlind;
                finalMessage.text = "Goal not met! Game Over!\nYou earned $" + displayEarnings + 
                                    " but needed $" + _goalForCurrentBlind;
                // Change button text to "Play Again" since it's game over
                Text playAgainText = playAgainButton.GetComponentInChildren<Text>();
                if (playAgainText != null)
                {
                    playAgainText.text = "Play Again";
                }
                // Don't automatically start NewGame coroutine - let user click Play Again
                return;
            }
        }
        
        // Update displays
        UpdateRoundDisplay();
        UpdateBlindDisplay();
        UpdateGoalProgress();
        
        // Reset bet for new round
        _bet = 0;
        bet.text = _bet.ToString() + " $";
        
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
        
        ShuffleCards();
        UpdateStreakUI(); // Update streak UI for new round
        
        // Go back to betting state instead of immediately starting game
        InitializeBettingState();  
    }

    public void RaiseBet()
    {
        RaiseBetWithMultiplier(1);
    }
    
    public void LowerBet()
    {
        LowerBetWithMultiplier(1);
    }
    
    private void RaiseBetWithMultiplier(int multiplier)
    {
        uint increment = Constants.BetIncrement * (uint)multiplier;
        if (_bet + increment <= Balance)
        {
            _bet += increment;
        }
        else if (_bet < Balance)
        {
            _bet = Balance; // Set to max possible bet
        }
        
        bet.text = _bet.ToString() + " $";
        
        // Update place bet button state
        if (placeBetButton != null)
        {
            placeBetButton.interactable = (_bet > 0);
        }
    }
    
    private void LowerBetWithMultiplier(int multiplier)
    {
        uint decrement = Constants.BetIncrement * (uint)multiplier;
        if (_bet >= decrement)
        {
            _bet -= decrement;
        }
        else
        {
            _bet = 0; // Set to minimum bet
        }
        
        bet.text = _bet.ToString() + " $";
        
        // Update place bet button state
        if (placeBetButton != null)
        {
            placeBetButton.interactable = (_bet > 0);
        }
    }

    /*
    public void PlaceBet()
    {
        if (_bet <= 0)
        {
            Debug.LogWarning("Cannot place bet: Bet amount is 0");
            return;
        }
        
        if (_bet > _balance)
        {
            Debug.LogWarning("Cannot place bet: Bet amount exceeds balance");
            return;
        }
        
        // Mark bet as placed
        _isBetPlaced = true;
        
        // Disable betting buttons
        raiseBetButton.interactable = false;
        lowerBetButton.interactable = false;
        placeBetButton.interactable = false;
        
        // Clear the betting message
        finalMessage.text = "";
        
        Debug.Log("Bet placed: $" + _bet + " - Starting game");
        
        // Now start the actual game
        StartGame();
    }
    */
    public void PlaceBet()
    {
        if (_bet <= 0)
        {
            Debug.LogWarning("Cannot place bet: Bet amount is 0");
            return;
        }

        if (_bet > _balance)
        {
            Debug.LogWarning("Cannot place bet: Bet amount exceeds balance");
            return;
        }

        // Deduct the bet from the balance and save it
        _balance -= _bet;
        PlayerPrefs.SetInt("UserCash", (int)_balance);
        PlayerPrefs.Save();
        
        // Update balance display immediately
        UpdateBalanceDisplay();

        Debug.Log("Bet placed: $" + _bet + " | New Balance: $" + _balance);

        // Mark bet as placed
        _isBetPlaced = true;

        // Disable betting buttons
        raiseBetButton.interactable = false;
        lowerBetButton.interactable = false;
        placeBetButton.interactable = false;

        // Clear the betting message
        finalMessage.text = "";

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
            SpriteRenderer cardSpriteRenderer = cardGO.GetComponent<SpriteRenderer>();

            if (cardSpriteRenderer.sprite == cardModel.cardFront)
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
    
    // Make UpdateScoreDisplays public so it can be called from CardHand
    public void UpdateScoreDisplays()
    {
        playerScoreText.text = "Score: " + GetVisibleScore(player, true);
        dealerScoreText.text = "Score: " + GetVisibleScore(dealer, false);
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
    private void UpdatePeekButtonState()
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
            SpriteRenderer spriteRenderer = firstCard.GetComponent<SpriteRenderer>();
            
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

    // Method to debug print the current state of the blind system
    private void DebugPrintBlindState(string context)
    {
        Debug.Log(context + " - BLIND SYSTEM STATE: " +
                 "Blind Level: " + _currentBlind + 
                 ", Round: " + _currentRound + "/" + _totalRoundsForBlind +
                 ", Current Balance: " + _balance + 
                 ", Starting Balance: " + _startingBalanceForBlind +
                 ", Earnings: " + _earningsForCurrentBlind +
                 ", Goal: " + _goalForCurrentBlind);
    }
    
    // Method to advance to the next blind level
    private void AdvanceToNextBlind()
    {
        _currentRound = 1;
        _startingBalanceForBlind = _balance; // Reset starting balance to current balance for the new blind
        _earningsForCurrentBlind = 0; // Reset earnings for the new blind
        
        // Make sure button says "Next Round" when advancing to a new blind level
        SetButtonTextToNextRound();
        
        DebugPrintBlindState("Before advancing blind");
        
        // Give player feedback about advancing to next blind
        string successMessage = "Congratulations! You've completed the ";
        
        switch (_currentBlind)
        {
            case BlindLevel.SmallBlind:
                successMessage += "Small Blind!";
                _currentBlind = BlindLevel.BigBlind;
                _goalForCurrentBlind = Constants.BigBlindGoal;
                _totalRoundsForBlind = Constants.BigBlindRounds;
                break;
            case BlindLevel.BigBlind:
                successMessage += "Big Blind!";
                _currentBlind = BlindLevel.MegaBlind;
                _goalForCurrentBlind = Constants.MegaBlindGoal;
                _totalRoundsForBlind = Constants.MegaBlindRounds;
                break;
            case BlindLevel.MegaBlind:
                successMessage += "Mega Blind!";
                _currentBlind = BlindLevel.SuperBlind;
                _goalForCurrentBlind = Constants.SuperBlindGoal;
                _totalRoundsForBlind = Constants.SuperBlindRounds;
                break;
            case BlindLevel.SuperBlind:
                successMessage += "Super Blind!";
                _currentBlind = BlindLevel.Completed;
                finalMessage.text = "Congratulations! You have completed all Blinds!";
                break;
            default:
                break;
        }
        
        // Display success message
        finalMessage.text = successMessage;
        
        DebugPrintBlindState("After advancing blind");
    }
    
    // Update round display
    public void UpdateRoundDisplay()
    {
        if (roundText != null)
        {
            roundText.text = "Round " + _currentRound + "/" + _totalRoundsForBlind;
            Debug.Log("Updated round display: " + roundText.text);
        }
        else
        {
            Debug.LogWarning("roundText is not assigned in the inspector!");
        }
    }
    
    // Update blind display
    public void UpdateBlindDisplay()
    {
        if (blindText != null)
        {
            string blindName = "";
            switch (_currentBlind)
            {
                case BlindLevel.SmallBlind:
                    blindName = "Small Blind";
                    break;
                case BlindLevel.BigBlind:
                    blindName = "Big Blind";
                    break;
                case BlindLevel.MegaBlind:
                    blindName = "Mega Blind";
                    break;
                case BlindLevel.SuperBlind:
                    blindName = "Super Blind";
                    break;
                case BlindLevel.Completed:
                    blindName = "All Completed!";
                    break;
            }
            blindText.text = blindName;
            Debug.Log("Updated blind display: " + blindText.text);
        }
        else
        {
            Debug.LogWarning("blindText is not assigned in the inspector!");
        }
    }
    
    // Update goal progress
    public void UpdateGoalProgress()
    {
        if (goalText != null)
        {
            // Ensure we never show negative earnings
            long displayEarnings = _earningsForCurrentBlind;
            if (displayEarnings < 0)
            {
                Debug.LogWarning("Earnings calculation resulted in negative value: " + _earningsForCurrentBlind);
                displayEarnings = 0;
            }
            
            goalText.text = "Goal $" + displayEarnings + "/" + _goalForCurrentBlind;
            Debug.Log("Updated goal progress: " + goalText.text);
        }
        else
        {
            Debug.LogWarning("goalText is not assigned in the inspector!");
        }
    }

    // Update balance display
    private void UpdateBalanceDisplay()
    {
        if (balance != null)
        {
            balance.text = " " + _balance.ToString() + " $";
            
        }
    }

    // Method for the ShopManager to call when a card is purchased
    public void OnCardPurchased(uint cost)
    {
        // Card purchases do NOT affect earnings at all
        // We just log the purchase
        Debug.Log("Card purchased for $" + cost + " - NOT affecting goal progress");
        DebugPrintBlindState("After card purchase");
    }

    // Method to set the button text to "Next Round"
    private void SetButtonTextToNextRound()
    {
        Text buttonText = playAgainButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = "Next Round";
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

        // Apply Artificer bonus only if streak is active AND player has Artificer card
        bool hasArtificer = PlayerActuallyHasCard(TarotCardType.Artificer);
        Debug.Log("Artificer check: streak=" + _streakMultiplier + ", hasArtificer=" + hasArtificer);
        
        if (_streakMultiplier > 0 && hasArtificer)
        {
            baseMultiplier *= 1.1f; // FIXED: Add 10% to the multiplier (was 0.1f which was reducing it)
            Debug.Log("Artificer card active: Multiplier increased by 10%");
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

    // HELPER FUNCTION - Check if player actually has a tarot card in the panel (more reliable than PlayerStats)
    private bool PlayerActuallyHasCard(TarotCardType cardType)
    {
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager == null || shopManager.tarotPanel == null)
        {
            Debug.LogWarning("Cannot find ShopManager or tarot panel");
            return false;
        }
        
        TarotCard[] actualCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
        foreach (var card in actualCards)
        {
            if (card.cardData != null && card.cardData.cardType == cardType && !card.isInShop)
            {
                Debug.Log("Found " + cardType + " in tarot panel: " + card.cardData.cardName);
                return true;
            }
        }
        
        Debug.Log("Did NOT find " + cardType + " in tarot panel");
        return false;
    }

    /// <summary>
    /// Verify and potentially fix the player panel reference
    /// </summary>
    private void EnsureCorrectPlayerReference()
    {
        // Try to find PlayerPanel specifically by name
        GameObject playerPanel = GameObject.Find("PlayerPanel");
        
        if (playerPanel != null)
        {
            CardHand playerHand = playerPanel.GetComponent<CardHand>();
            if (playerHand != null)
            {
                if (player != playerPanel)
                {
                    Debug.LogWarning("Player reference was pointing to '" + (player != null ? player.name : "null") + 
                                   "' but should point to 'PlayerPanel'. Fixing reference.");
                    player = playerPanel;
                }
                Debug.Log("✓ Player reference correctly points to PlayerPanel with CardHand component");
            }
            else
            {
                Debug.LogError("PlayerPanel found but has no CardHand component!");
            }
        }
        else
        {
            Debug.LogError("Could not find PlayerPanel GameObject in scene!");
            
            // Try alternative search
            CardHand[] allHands = FindObjectsOfType<CardHand>();
            foreach (CardHand hand in allHands)
            {
                if (!hand.isDealer && hand.gameObject.name.ToLower().Contains("player"))
                {
                    Debug.Log("Found potential player hand: " + hand.gameObject.name);
                    player = hand.gameObject;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Debug method to show exactly what cards are detected in player panel
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugPlayerPanelCards()
    {
        Debug.Log("=== PLAYER PANEL CARD DETECTION DEBUG ===");
        
        // Ensure we have the correct reference
        EnsureCorrectPlayerReference();
        
        if (player == null)
        {
            Debug.LogError("Player reference is NULL!");
            return;
        }
        
        Debug.Log("Player GameObject name: " + player.name);
        Debug.Log("Player GameObject full path: " + GetGameObjectPath(player));
        
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand == null)
        {
            Debug.LogError("Player GameObject has no CardHand component!");
            return;
        }
        
        Debug.Log("Player hand isDealer flag: " + playerHand.isDealer);
        Debug.Log("Player hand has " + playerHand.cards.Count + " cards:");
        
        for (int i = 0; i < playerHand.cards.Count; i++)
        {
            GameObject cardObj = playerHand.cards[i];
            CardModel cardModel = cardObj.GetComponent<CardModel>();
            
            if (cardModel != null && cardModel.cardFront != null)
            {
                // Get card info using our NEW system with original deck index
                CardInfo cardInfo = GetCardInfoFromModel(cardModel);
                
                // Show the original deck index vs shuffled position
                int shuffledIndex = -1;
                for (int deckIndex = 0; deckIndex < faces.Length; deckIndex++)
                {
                    if (faces[deckIndex] == cardModel.cardFront)
                    {
                        shuffledIndex = deckIndex;
                        break;
                    }
                }
                
                Debug.Log("  Card " + (i+1) + ": " + cardInfo.cardName + 
                         " | Original Index: " + cardModel.originalDeckIndex + 
                         " | Shuffled Index: " + shuffledIndex +
                         " | Correct Suit: " + cardInfo.suit + 
                         " | Card Object: " + cardObj.name);
            }
            else
            {
                Debug.LogWarning("  Card " + (i+1) + ": CardModel or cardFront is null! Object: " + cardObj.name);
            }
        }
        
        // Show suit counts
        Dictionary<CardSuit, int> suitCounts = GetAllHandSuitCounts(player);
        Debug.Log("Suit Counts - Hearts: " + suitCounts[CardSuit.Hearts] + 
                 ", Diamonds: " + suitCounts[CardSuit.Diamonds] + 
                 ", Clubs: " + suitCounts[CardSuit.Clubs] + 
                 ", Spades: " + suitCounts[CardSuit.Spades]);
        
        Debug.Log("=== END PLAYER PANEL DEBUG ===");
    }

    /// <summary>
    /// Helper method to get full GameObject path
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "null";
        
        string path = obj.name;
        Transform current = obj.transform.parent;
        
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        
        return path;
    }

    // TAROT CARD BONUS FUNCTIONS - Individual calculations for each suit-based tarot card
    
    /// <summary>
    /// Calculate The Botanist bonus (+50 per club in winning hand)
    /// </summary>
    public uint CalculateBotanistBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.Botanist))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
        {
            Debug.Log("Botanist bonus: Skipping - not checking player hand");
            return 0;
        }
        
        // Get actual cards in player's hand and count clubs
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> clubCards = handCards.Where(card => card.suit == CardSuit.Clubs).ToList();
        
        if (clubCards.Count > 0)
        {
            Debug.Log("Botanist bonus calculation:");
            Debug.Log("  Player hand contains " + handCards.Count + " total cards");
            Debug.Log("  Club cards found in player hand:");
            foreach (CardInfo card in clubCards)
            {
                Debug.Log("    - " + card.cardName);
            }
        }
        
        uint bonus = (uint)(clubCards.Count * Constants.SuitBonusAmount);
        
        if (bonus > 0)
        {
            Debug.Log("Botanist bonus: " + clubCards.Count + " clubs = +" + bonus + " (only counting player hand)");
        }
        
        return bonus;
    }
    
    /// <summary>
    /// Calculate The Assassin bonus (+50 per spade in winning hand)
    /// </summary>
    public uint CalculateAssassinBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.Assassin))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
        {
            Debug.Log("Assassin bonus: Skipping - not checking player hand");
            return 0;
        }
        
        // Get actual cards in player's hand and count spades
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> spadeCards = handCards.Where(card => card.suit == CardSuit.Spades).ToList();
        
        if (spadeCards.Count > 0)
        {
            Debug.Log("Assassin bonus calculation:");
            Debug.Log("  Player hand contains " + handCards.Count + " total cards");
            Debug.Log("  Spade cards found in player hand:");
            foreach (CardInfo card in spadeCards)
            {
                Debug.Log("    - " + card.cardName);
            }
        }
        
        uint bonus = (uint)(spadeCards.Count * Constants.SuitBonusAmount);
        
        if (bonus > 0)
        {
            Debug.Log("Assassin bonus: " + spadeCards.Count + " spades = +" + bonus + " (only counting player hand)");
        }
        
        return bonus;
    }
    
    /// <summary>
    /// Calculate The Secret Lover bonus (+50 per heart in winning hand)
    /// </summary>
    public uint CalculateSecretLoverBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.SecretLover))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
        {
            Debug.Log("Secret Lover bonus: Skipping - not checking player hand");
            return 0;
        }
        
        // Get actual cards in player's hand and count hearts
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> heartCards = handCards.Where(card => card.suit == CardSuit.Hearts).ToList();
        
        Debug.Log("Secret Lover bonus calculation:");
        Debug.Log("  Player hand contains " + handCards.Count + " total cards");
        if (heartCards.Count > 0)
        {
            Debug.Log("  Heart cards found in player hand:");
            foreach (CardInfo card in heartCards)
            {
                Debug.Log("    - " + card.cardName);
            }
        }
        else
        {
            Debug.Log("  No heart cards found in player hand");
        }
        
        uint bonus = (uint)(heartCards.Count * Constants.SuitBonusAmount);
        
        Debug.Log("Secret Lover bonus: " + heartCards.Count + " hearts = +" + bonus + " (only counting player hand)");
        
        return bonus;
    }
    
    /// <summary>
    /// Calculate The Jeweler bonus (+50 per diamond in winning hand)
    /// </summary>
    public uint CalculateJewelerBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.Jeweler))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
        {
            Debug.Log("Jeweler bonus: Skipping - not checking player hand");
            return 0;
        }
        
        // Get actual cards in player's hand and count diamonds
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> diamondCards = handCards.Where(card => card.suit == CardSuit.Diamonds).ToList();
        
        if (diamondCards.Count > 0)
        {
            Debug.Log("Jeweler bonus calculation:");
            Debug.Log("  Player hand contains " + handCards.Count + " total cards");
            Debug.Log("  Diamond cards found in player hand:");
            foreach (CardInfo card in diamondCards)
            {
                Debug.Log("    - " + card.cardName);
            }
        }
        
        uint bonus = (uint)(diamondCards.Count * Constants.SuitBonusAmount);
        
        if (bonus > 0)
        {
            Debug.Log("Jeweler bonus: " + diamondCards.Count + " diamonds = +" + bonus + " (only counting player hand)");
        }
        
        return bonus;
    }
    
    /// <summary>
    /// Calculate The House Keeper bonus (+10 per Jack/Queen/King in winning hand)
    /// </summary>
    public uint CalculateHouseKeeperBonus(GameObject handOwner = null)
    {
        if (!PlayerActuallyHasCard(TarotCardType.HouseKeeper))
            return 0;
            
        GameObject targetHand = handOwner ?? player;
        
        // Ensure we're only checking the player's hand for bonuses
        if (targetHand != player)
        {
            Debug.Log("House Keeper bonus: Skipping - not checking player hand");
            return 0;
        }
        
        // Get actual cards in player's hand and count Jack/Queen/King cards
        List<CardInfo> handCards = GetHandCardInfo(targetHand);
        List<CardInfo> faceCards = handCards.Where(card => 
            card.suitIndex == 10 || // Jack
            card.suitIndex == 11 || // Queen  
            card.suitIndex == 12    // King
        ).ToList();
        
        if (faceCards.Count > 0)
        {
            Debug.Log("House Keeper bonus calculation:");
            Debug.Log("  Player hand contains " + handCards.Count + " total cards");
            Debug.Log("  Face cards (J/Q/K) found in player hand:");
            foreach (CardInfo card in faceCards)
            {
                Debug.Log("    - " + card.cardName);
            }
        }
        
        uint bonus = (uint)(faceCards.Count * Constants.HouseKeeperBonusAmount);
        
        if (bonus > 0)
        {
            Debug.Log("House Keeper bonus: " + faceCards.Count + " face cards = +" + bonus + " (only counting player hand)");
        }
        
        return bonus;
    }
    
    /// <summary>
    /// Calculate all suit bonuses for winning hands
    /// </summary>
    public uint CalculateSuitBonuses(GameObject handOwner = null)
    {
        if (PlayerStats.instance == null)
            return 0;
            
        // Ensure we have the correct PlayerPanel reference
        EnsureCorrectPlayerReference();
        
        // Tarot card bonuses should ONLY apply to the player's hand, never dealer's hand
        GameObject targetHand = player;
        
        Debug.Log("=== CALCULATING SUIT BONUSES FOR PLAYER HAND ONLY ===");
        
        // Verify we have the player object
        if (targetHand == null)
        {
            Debug.LogError("Player object is null, cannot calculate suit bonuses");
            return 0;
        }
        
        Debug.Log("Using player reference: " + targetHand.name + " (Path: " + GetGameObjectPath(targetHand) + ")");
        
        // Debug player panel cards if in editor
        #if UNITY_EDITOR
        DebugPlayerPanelCards();
        #endif
        
        // Debug: Print owned tarot cards vs actual tarot panel
        if (PlayerStats.instance != null && PlayerStats.instance.ownedCards != null)
        {
            Debug.Log("PlayerStats thinks player owns " + PlayerStats.instance.ownedCards.Count + " tarot cards:");
            foreach (var card in PlayerStats.instance.ownedCards)
            {
                Debug.Log("  - " + card.cardName + " (Type: " + card.cardType + ")");
            }
        }
        
        // Check what's actually in the tarot panel
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null && shopManager.tarotPanel != null)
        {
            TarotCard[] actualCards = shopManager.tarotPanel.GetComponentsInChildren<TarotCard>();
            Debug.Log("Tarot panel actually contains " + actualCards.Length + " cards:");
            foreach (var card in actualCards)
            {
                if (card.cardData != null)
                {
                    Debug.Log("  - " + card.cardData.cardName + " (Type: " + card.cardData.cardType + ")");
                }
            }
        }
        
        // Calculate bonuses - these methods now ensure they only count player hand cards
        uint botanistBonus = CalculateBotanistBonus(targetHand);
        uint assassinBonus = CalculateAssassinBonus(targetHand);
        uint secretLoverBonus = CalculateSecretLoverBonus(targetHand);
        uint jewelerBonus = CalculateJewelerBonus(targetHand);
        uint houseKeeperBonus = CalculateHouseKeeperBonus(targetHand);
        
        uint totalBonus = botanistBonus + assassinBonus + secretLoverBonus + jewelerBonus + houseKeeperBonus;
        
        Debug.Log("Bonus breakdown: Botanist=" + botanistBonus + ", Assassin=" + assassinBonus + 
                 ", SecretLover=" + secretLoverBonus + ", Jeweler=" + jewelerBonus + 
                 ", HouseKeeper=" + houseKeeperBonus + ", Total=" + totalBonus);

        return totalBonus;
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
        {
            Debug.LogError("Player object is null, cannot calculate suit bonus breakdown");
            return breakdown;
        }
        
        breakdown[TarotCardType.Botanist] = CalculateBotanistBonus(targetHand);
        breakdown[TarotCardType.Assassin] = CalculateAssassinBonus(targetHand);
        breakdown[TarotCardType.SecretLover] = CalculateSecretLoverBonus(targetHand);
        breakdown[TarotCardType.Jeweler] = CalculateJewelerBonus(targetHand);
        breakdown[TarotCardType.HouseKeeper] = CalculateHouseKeeperBonus(targetHand);
        
        return breakdown;
    }

    // Debug method to test suit detection (can be called from Unity Inspector or console)
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugTestSuitDetection()
    {
        Debug.Log("=== SUIT DETECTION TEST ===");
        
        // Test a few example cards from each suit
        int[] testIndices = { 0, 5, 12, 13, 18, 25, 26, 31, 38, 39, 44, 51 };
        string[] expectedSuits = { "Hearts", "Hearts", "Hearts", "Diamonds", "Diamonds", "Diamonds", 
                                 "Clubs", "Clubs", "Clubs", "Spades", "Spades", "Spades" };
        
        for (int i = 0; i < testIndices.Length; i++)
        {
            int cardIndex = testIndices[i];
            CardSuit detectedSuit = GetCardSuit(cardIndex);
            string suitName = detectedSuit.ToString();
            bool isCorrect = suitName == expectedSuits[i];
            
            Debug.Log("Card Index " + cardIndex + ": Expected " + expectedSuits[i] + 
                     ", Got " + suitName + " - " + (isCorrect ? "✓" : "✗"));
        }
        
        Debug.Log("=== END SUIT DETECTION TEST ===");
    }

    // Debug method to test current hand suit counts
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugCurrentHandSuits()
    {
        Debug.Log("=== CURRENT HAND ANALYSIS ===");
        
        // Get detailed hand information
        List<CardInfo> playerCards = GetHandCardInfo(player);
        Dictionary<CardSuit, int> suitCounts = GetAllHandSuitCounts(player);
        
        Debug.Log("Player Hand Cards (" + playerCards.Count + " total):");
        foreach (CardInfo card in playerCards)
        {
            Debug.Log("  - " + card.cardName + " (Index: " + card.index + ", Value: " + card.value + ")");
        }
        
        Debug.Log("Suit Counts - Hearts: " + suitCounts[CardSuit.Hearts] + 
                 ", Diamonds: " + suitCounts[CardSuit.Diamonds] + 
                 ", Clubs: " + suitCounts[CardSuit.Clubs] + 
                 ", Spades: " + suitCounts[CardSuit.Spades]);
        
        // Test individual tarot bonuses
        Debug.Log("--- TAROT BONUS BREAKDOWN ---");
        uint botanistBonus = CalculateBotanistBonus();
        uint assassinBonus = CalculateAssassinBonus();
        uint secretLoverBonus = CalculateSecretLoverBonus();
        uint jewelerBonus = CalculateJewelerBonus();
        uint houseKeeperBonus = CalculateHouseKeeperBonus();
        uint totalBonuses = CalculateSuitBonuses();
        
        Debug.Log("Botanist (Clubs): +" + botanistBonus);
        Debug.Log("Assassin (Spades): +" + assassinBonus);
        Debug.Log("Secret Lover (Hearts): +" + secretLoverBonus);
        Debug.Log("Jeweler (Diamonds): +" + jewelerBonus);
        Debug.Log("House Keeper (J/Q/K): +" + houseKeeperBonus);
        Debug.Log("Total Suit Bonuses: +" + totalBonuses);
        
        Debug.Log("=== END HAND ANALYSIS ===");
    }
    
    // Debug method to show complete deck organization
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugDeckStructure()
    {
        Debug.Log("=== DECK STRUCTURE ANALYSIS ===");
        
        foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
        {
            Debug.Log("--- " + suit.ToString().ToUpper() + " ---");
            List<CardInfo> suitCards = GetCardsOfSuit(suit);
            
            for (int i = 0; i < suitCards.Count; i++)
            {
                CardInfo card = suitCards[i];
                Debug.Log("Index " + card.index + ": " + card.cardName + " (Value: " + card.value + ")");
            }
        }
        
        Debug.Log("=== END DECK STRUCTURE ===");
    }
    
    // Debug method to test specific card lookups
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugCardLookupTest()
    {
        Debug.Log("=== CARD LOOKUP TEST ===");
        
        // Test specific card lookups
        int aceOfHeartsIndex = GetCardIndex(0, CardSuit.Hearts);
        int kingOfSpadesIndex = GetCardIndex(12, CardSuit.Spades);
        
        CardInfo aceOfHearts = GetCardInfo(aceOfHeartsIndex);
        CardInfo kingOfSpades = GetCardInfo(kingOfSpadesIndex);
        
        Debug.Log("Ace of Hearts - Index: " + aceOfHearts.index + ", Name: " + aceOfHearts.cardName);
        Debug.Log("King of Spades - Index: " + kingOfSpades.index + ", Name: " + kingOfSpades.cardName);
        
        // Test hand contains specific cards
        bool hasAceOfHearts = HandContainsCard(player, 1, CardSuit.Hearts);
        bool hasKingOfSpades = HandContainsCard(player, 10, CardSuit.Spades);
        
        Debug.Log("Player has Ace of Hearts: " + hasAceOfHearts);
        Debug.Log("Player has King of Spades: " + hasKingOfSpades);
        
        Debug.Log("=== END CARD LOOKUP TEST ===");
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
    
    // Get current blind name for history tracking
    private string GetCurrentBlindName()
    {
        switch (_currentBlind)
        {
            case BlindLevel.SmallBlind:
                return "Small Blind";
            case BlindLevel.BigBlind:
                return "Big Blind";
            case BlindLevel.MegaBlind:
                return "Mega Blind";
            case BlindLevel.SuperBlind:
                return "Super Blind";
            case BlindLevel.Completed:
                return "Completed";
            default:
                return "Unknown";
        }
    }

    private void InitializeBettingState()
    {
        // Reset bet placement state
        _isBetPlaced = false;
        
        // Clear any existing cards
        if (player != null && player.GetComponent<CardHand>() != null)
        {
            player.GetComponent<CardHand>().Clear();
        }
        if (dealer != null && dealer.GetComponent<CardHand>() != null)
        {
            dealer.GetComponent<CardHand>().Clear();
        }
        
        // Disable game action buttons until bet is placed
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        peekButton.interactable = false;
        transformButton.interactable = false;
        playAgainButton.interactable = false;
        
        // Enable betting buttons
        raiseBetButton.interactable = true;
        lowerBetButton.interactable = true;
        placeBetButton.interactable = (_bet > 0); // Only enable if there's a bet amount
        
        // Clear score displays
        playerScoreText.text = "Score: 0";
        dealerScoreText.text = "Score: 0";
        probMessage.text = "";
        finalMessage.text = "Place your bet to start the round!";
        
        // Reset tarot ability usage for new round
        _hasUsedPeekThisRound = false;
        _hasUsedTransformThisRound = false;
        
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
        
        // Enable game action buttons after dealing is complete
        hitButton.interactable = true;
        stickButton.interactable = true;
        
        UpdateScoreDisplays(); 
        UpdateDiscardButtonState();  
        UpdatePeekButtonState();
        UpdateTransformButtonState();

        // Check for blackjack after all cards are dealt
        if (Blackjack(player, true))
        {
            if (Blackjack(dealer, false)) { EndHand(WinCode.Draw); }        
            else { EndHand(WinCode.PlayerWins); }                          
        }
        else if (Blackjack(dealer, false)) { EndHand(WinCode.DealerWins); }  
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
    }

    private IEnumerator PushAnimated(GameObject handOwner, bool isPlayer)
    {
        // Check if we're reaching the end of the deck and need to reshuffle
        if (cardIndex >= values.Length - 1)
        {
            Debug.Log((isPlayer ? "Player" : "Dealer") + " drawing - Deck is almost empty, reshuffling...");
            ShuffleCards();
            cardIndex = 0;
        }

        CardHand hand = handOwner.GetComponent<CardHand>();
        
        // Check if hand can accept more cards
        if (hand.cards.Count >= Constants.MaxCardsInHand)
        {
            Debug.LogWarning("Maximum card limit reached (" + Constants.MaxCardsInHand + ")");
            yield break;
        }
        
        // Create the card without automatic positioning for animation
        GameObject newCard = hand.CreateCard(faces[cardIndex], values[cardIndex], true, originalIndices[cardIndex]);
        cardIndex++;
        
        if (newCard != null)
        {
            // Calculate final position before animation
            Vector3 finalPosition = CalculateFinalCardPosition(hand, hand.cards.Count - 1);
            
            // Animate the card from deck position to its final position
            yield return StartCoroutine(AnimateCardDealing(newCard, finalPosition));
            
            // Arrange all cards after animation completes
            hand.ArrangeCardsInWindow();
        }
    }

    private Vector3 CalculateFinalCardPosition(CardHand hand, int cardIndex)
    {
        // Calculate where this card should end up in the hand
        float panelWidth = 20f;
        float cardSpacing = 3.2f; // CARD_SPACING from CardHand
        float cardScale = 7.5f; // CARD_SCALE from CardHand
        
        SpriteRenderer cardSprite = hand.card.GetComponent<SpriteRenderer>();
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
    
    // Setup hold functionality for bet buttons
    private void SetupBetButtonHoldListeners()
    {
        if (raiseBetButton != null)
        {
            // Add event trigger for pointer down/up events
            UnityEngine.EventSystems.EventTrigger raiseTrigger = raiseBetButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (raiseTrigger == null)
            {
                raiseTrigger = raiseBetButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // Clear existing entries
            raiseTrigger.triggers.Clear();
            
            // Pointer down event
            UnityEngine.EventSystems.EventTrigger.Entry pointerDownEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDownEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { StartRaiseBetHold(); });
            raiseTrigger.triggers.Add(pointerDownEntry);
            
            // Pointer up event
            UnityEngine.EventSystems.EventTrigger.Entry pointerUpEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUpEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { StopRaiseBetHold(); });
            raiseTrigger.triggers.Add(pointerUpEntry);
            
            // Pointer exit event (in case pointer leaves button while held)
            UnityEngine.EventSystems.EventTrigger.Entry pointerExitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExitEntry.callback.AddListener((data) => { StopRaiseBetHold(); });
            raiseTrigger.triggers.Add(pointerExitEntry);
        }
        
        if (lowerBetButton != null)
        {
            // Add event trigger for pointer down/up events
            UnityEngine.EventSystems.EventTrigger lowerTrigger = lowerBetButton.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (lowerTrigger == null)
            {
                lowerTrigger = lowerBetButton.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }
            
            // Clear existing entries
            lowerTrigger.triggers.Clear();
            
            // Pointer down event
            UnityEngine.EventSystems.EventTrigger.Entry pointerDownEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerDownEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { StartLowerBetHold(); });
            lowerTrigger.triggers.Add(pointerDownEntry);
            
            // Pointer up event
            UnityEngine.EventSystems.EventTrigger.Entry pointerUpEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerUpEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { StopLowerBetHold(); });
            lowerTrigger.triggers.Add(pointerUpEntry);
            
            // Pointer exit event (in case pointer leaves button while held)
            UnityEngine.EventSystems.EventTrigger.Entry pointerExitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            pointerExitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            pointerExitEntry.callback.AddListener((data) => { StopLowerBetHold(); });
            lowerTrigger.triggers.Add(pointerExitEntry);
        }
    }
    
    // Start holding raise bet button
    private void StartRaiseBetHold()
    {
        if (!_isHoldingRaiseBet && raiseBetButton.interactable)
        {
            _isHoldingRaiseBet = true;
            _raiseBetCoroutine = StartCoroutine(RaiseBetHoldCoroutine());
        }
    }
    
    // Stop holding raise bet button
    private void StopRaiseBetHold()
    {
        _isHoldingRaiseBet = false;
        if (_raiseBetCoroutine != null)
        {
            StopCoroutine(_raiseBetCoroutine);
            _raiseBetCoroutine = null;
        }
    }
    
    // Start holding lower bet button
    private void StartLowerBetHold()
    {
        if (!_isHoldingLowerBet && lowerBetButton.interactable)
        {
            _isHoldingLowerBet = true;
            _lowerBetCoroutine = StartCoroutine(LowerBetHoldCoroutine());
        }
    }
    
    // Stop holding lower bet button
    private void StopLowerBetHold()
    {
        _isHoldingLowerBet = false;
        if (_lowerBetCoroutine != null)
        {
            StopCoroutine(_lowerBetCoroutine);
            _lowerBetCoroutine = null;
        }
    }
    
    // Coroutine for raise bet hold functionality
    private IEnumerator RaiseBetHoldCoroutine()
    {
        // Initial delay before starting rapid increments
        yield return new WaitForSeconds(0.5f);
        
        float holdTime = 0f;
        int multiplier = 1;
        
        while (_isHoldingRaiseBet && raiseBetButton.interactable)
        {
            RaiseBetWithMultiplier(multiplier);
            
            holdTime += 0.1f;
            
            // Increase multiplier based on hold time
            if (holdTime > 3f) multiplier = 10;      // After 3 seconds, 10x speed
            else if (holdTime > 2f) multiplier = 5;  // After 2 seconds, 5x speed
            else if (holdTime > 1f) multiplier = 2;  // After 1 second, 2x speed
            else multiplier = 1;                     // First second, normal speed
            
            // Faster interval as time progresses
            float interval = holdTime > 2f ? 0.05f : 0.1f;
            yield return new WaitForSeconds(interval);
        }
    }
    
    // Coroutine for lower bet hold functionality
    private IEnumerator LowerBetHoldCoroutine()
    {
        // Initial delay before starting rapid decrements
        yield return new WaitForSeconds(0.5f);
        
        float holdTime = 0f;
        int multiplier = 1;
        
        while (_isHoldingLowerBet && lowerBetButton.interactable)
        {
            LowerBetWithMultiplier(multiplier);
            
            holdTime += 0.1f;
            
            // Increase multiplier based on hold time
            if (holdTime > 3f) multiplier = 10;      // After 3 seconds, 10x speed
            else if (holdTime > 2f) multiplier = 5;  // After 2 seconds, 5x speed
            else if (holdTime > 1f) multiplier = 2;  // After 1 second, 2x speed
            else multiplier = 1;                     // First second, normal speed
            
            // Faster interval as time progresses
            float interval = holdTime > 2f ? 0.05f : 0.1f;
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// Debug method to verify suit detection matches the visual deck layout
    /// Based on user's description: Row 1=Hearts, Row 2=Diamonds, Row 3=Clubs, Row 4=Spades
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void DebugSuitDetectionVsLayout()
    {
        Debug.Log("=== SUIT DETECTION VS DECK LAYOUT TEST ===");
        Debug.Log("Expected layout: Row 1=Hearts(0-12), Row 2=Diamonds(13-25), Row 3=Clubs(26-38), Row 4=Spades(39-51)");
        
        // Test key cards from each suit to verify detection
        int[] testIndices = { 
            0, 6, 12,      // Hearts: Ace, 7, King
            13, 19, 25,    // Diamonds: Ace, 7, King  
            26, 32, 38,    // Clubs: Ace, 7, King
            39, 45, 51     // Spades: Ace, 7, King
        };
        
        string[] expectedSuits = { 
            "Hearts", "Hearts", "Hearts",
            "Diamonds", "Diamonds", "Diamonds", 
            "Clubs", "Clubs", "Clubs",
            "Spades", "Spades", "Spades"
        };
        
        bool allCorrect = true;
        
        for (int i = 0; i < testIndices.Length; i++)
        {
            int cardIndex = testIndices[i];
            CardSuit detectedSuit = GetCardSuit(cardIndex);
            string suitName = detectedSuit.ToString();
            bool isCorrect = suitName == expectedSuits[i];
            
            if (!isCorrect) allCorrect = false;
            
            string status = isCorrect ? "✓" : "✗ ERROR";
            Debug.Log("Index " + cardIndex + ": Expected " + expectedSuits[i] + 
                     ", Got " + suitName + " - " + status);
        }
        
        if (allCorrect)
        {
            Debug.Log("✓ ALL SUIT DETECTIONS CORRECT!");
        }
        else
        {
            Debug.LogError("✗ SUIT DETECTION ERRORS FOUND! Check card sprite order in faces array.");
        }
        
        Debug.Log("=== END SUIT DETECTION TEST ===");
    }
}