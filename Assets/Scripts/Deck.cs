#undef ARRAY_SHUFFLE

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    public const float BaseWinMultiplier = 1.5f; // Base multiplier (bet 2, get 3 back = 1.5x)
    public const float StreakMultiplierStep = 0.25f; // How much multiplier increases per streak level
    public const int MaxStreakLevel = 5;
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
    public GameObject dealer;
    public GameObject player;

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
    public Text balance;
    public Text bet;
    public Text roundText; // Text to display current round
    public Text blindText; // Text to display current blind
    public Text goalText; // Text to display progress towards goal
    
    // UI elements for streak
    public Text streakText;
    public GameObject streakPanel;
    public StreakFlameEffect streakFlameEffect;

    private uint _balance = Constants.InitialBalance;
    private uint _bet;
    private bool _isPeeking = false;
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

    // Public property to access balance
    public uint Balance
    {
        get { return _balance; }
        set 
        { 
            // We don't update earnings here anymore - only track in EndHand and OnCardPurchased
            _balance = value;
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
        ShuffleCards();
        
        _startingBalanceForBlind = _balance;
        _earningsForCurrentBlind = 0; // Reset earnings at game start
        bet.text = _bet.ToString() + " $";
        UpdateBalanceDisplay();
        UpdateRoundDisplay();
        UpdateBlindDisplay();
        UpdateGoalProgress();
        
        // Set the button text to "Next Round" at the start
        SetButtonTextToNextRound();
        
        // Debug logging of initial state
        DebugPrintBlindState("Initial setup");
        
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
        
        StartGame();
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
        }
    }
 
    private void ArrayShuffle()
    { 
        System.Random rnd = new System.Random();
        int[] index = Enumerable.Range(0, values.Length).ToArray();
        index.OrderBy(_ => rnd.Next()).ToArray();
         
        int[] tmpValues = new int[Constants.DeckCards];
        Sprite[] tmpFaces = new Sprite[Constants.DeckCards];
 
        for (int i = 0; i < Constants.DeckCards; ++i)
        {
            tmpValues[index[i]] = values[i];
            tmpFaces[index[i]] = faces[i];
        }
 
        for (int i = 0; i < Constants.DeckCards; ++i)
        {
            values[i] = tmpValues[i];
            faces[i] = tmpFaces[i];
        }
    }

    private void StartGame()
    {
        StopCoroutine(NewGame());
        
        // Reset tarot ability usage for new round
        _hasUsedPeekThisRound = false;
        _hasUsedTransformThisRound = false;

        for (int i = 0; i < Constants.InitialCardsDealt; ++i)
        {
            PushPlayer();
            PushDealer();
        }
        UpdateScoreDisplays(); 
        UpdateDiscardButtonState();  
        UpdatePeekButtonState();
        UpdateTransformButtonState();

        if (Blackjack(player, true))
        {
            if (Blackjack(dealer, false)) { EndHand(WinCode.Draw); }        
            else { EndHand(WinCode.PlayerWins); }                          
        }
        else if (Blackjack(dealer, false)) { EndHand(WinCode.DealerWins); }  
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

        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
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

        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        UpdateScoreDisplays();  

        CalculateProbabilities();
    }

    public void Hit()
    { 
        CardHand playerHand = player.GetComponent<CardHand>();
        if (!playerHand.CanAddMoreCards())
        {
            finalMessage.text = "Maximum cards reached!";
            return;
        }

        PushPlayer();
        // FlipDealerCard(); // Dealer card is not flipped on player hit generally
        
        UpdateScoreDisplays();  
        UpdateDiscardButtonState();  
 
        if (Blackjack(player, true)) { EndHand(WinCode.PlayerWins); }
        else if (GetPlayerPoints() > Constants.Blackjack) { EndHand(WinCode.DealerWins); }
    }

    public void Stand()
    {
        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points;  

        FlipDealerCard();  
        while (dealerPoints < Constants.DealerStand)
        {
            PushDealer();
            dealerPoints = dealer.GetComponent<CardHand>().points;
            UpdateScoreDisplays();  
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

    private void EndHand(WinCode code)
    {   
        FlipDealerCard();
        uint oldBalance = _balance;
        long oldEarnings = _earningsForCurrentBlind;
        
        switch (code)
        {
            case WinCode.DealerWins:
                finalMessage.text = "You lose!";
                if (_bet <= _balance) {
                    // Track losses BEFORE changing balance
                    _earningsForCurrentBlind -= _bet;
                    Balance -= _bet;
                } else {
                    // Safety check
                    Debug.LogWarning("Bet amount greater than balance! Setting balance to 0.");
                    _earningsForCurrentBlind -= _balance; // Lost whatever was left
                    Balance = 0;
                }
                
                // Reset streak on loss
                _currentStreak = 0;
                _streakMultiplier = 0;
                break;
                
            case WinCode.PlayerWins:
                // Increment streak
                _currentStreak++;
                
                // Calculate streak level and multiplier
                _streakMultiplier = Mathf.Min(_currentStreak / Constants.StreakMultiplierIncrement, Constants.MaxStreakLevel);
                float multiplier = CalculateWinMultiplier();
                
                // Apply multiplier to winnings
                uint winnings = (uint)(_bet * multiplier);
                
                finalMessage.text = "You win!";
                
                // Add streak info if there's a streak
                if (_currentStreak > 1)
                {
                    finalMessage.text += "\nStreak: " + _currentStreak + " (" + multiplier.ToString("0.0") + "x)";
                }
                
                // Track winnings BEFORE changing balance
                _earningsForCurrentBlind += winnings;
                Balance += winnings;
                break;
                
            case WinCode.Draw:
                finalMessage.text = "Draw!";
                
                // Reset streak on draw
                _currentStreak = 0;
                _streakMultiplier = 0;
                break;
                
            default:
                Debug.Assert(false);    
                break;
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
            StartCoroutine(NewGame());
        }
    }

    public void PlayAgain()
    {   
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
                Text buttonText = playAgainButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Play Again";
                }
                StartCoroutine(NewGame());
                return;
            }
        }
        
        // Update displays
        UpdateRoundDisplay();
        UpdateBlindDisplay();
        UpdateGoalProgress();
        
        // Reset GUI
        hitButton.interactable = true;
        stickButton.interactable = true;
        raiseBetButton.interactable = true;
        lowerBetButton.interactable = true;
        // Disable the next round button during gameplay
        playAgainButton.interactable = false;
        _hasUsedPeekThisRound = false; // Reset peek usage for new round
        _hasUsedTransformThisRound = false; // Reset transform usage for new round
        UpdateDiscardButtonState();  
        UpdatePeekButtonState();
        UpdateTransformButtonState();
        UpdateStreakUI(); // Update streak UI for new round
        finalMessage.text = "";

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
        StartGame();
        UpdateScoreDisplays();  
    }

    public void RaiseBet()
    {
        if (_bet < Balance)
        {
            _bet += Constants.BetIncrement;
            bet.text = _bet.ToString() + " $";
            playAgainButton.interactable = true;
        }
    }
    
    public void LowerBet()
    {
        if (_bet > 0)
        {
            _bet -= Constants.BetIncrement;
            bet.text = _bet.ToString() + " $";
        }
    }

    IEnumerator NewGame()
    {   
        yield return new WaitForSeconds(Constants.NewGameCountdown);
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
            discardButton.interactable = hasSelectedCard;
            
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
            // Only enable if: game is active, not currently peeking, and hasn't used peek this round
            peekButton.interactable = (hitButton.interactable && !_isPeeking && !_hasUsedPeekThisRound);
            
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
            
            // Enable only if: game is active, exactly 2 cards selected, and hasn't used transform this round
            transformButton.interactable = (gameActive && has2CardsSelected && notUsedThisRound);
            
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
    private float CalculateWinMultiplier()
    {
        return Constants.BaseWinMultiplier + (_streakMultiplier * Constants.StreakMultiplierStep);
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
        if (streakText != null)
        {
            if (_currentStreak > 0)
            {
                // Display streak count and multiplier
                float multiplier = CalculateWinMultiplier();
                streakText.text = "Streak: " + _currentStreak + " (" + multiplier.ToString("0.0") + "x)";
                
                if (streakPanel != null)
                {
                    streakPanel.SetActive(true);
                }
            }
            else
            {
                if (streakPanel != null)
                {
                    streakPanel.SetActive(false);
                }
            }
        }
        
        // Update flame effect if available
        if (streakFlameEffect != null)
        {
            streakFlameEffect.SetStreakLevel(_streakMultiplier);
        }
    }
}