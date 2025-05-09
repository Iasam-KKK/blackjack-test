﻿#undef ARRAY_SHUFFLE

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
    public const int InitialDiscardTokens = 3; // Starting number of discard tokens
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
    public Button discardButton; // New discard button
    public Text finalMessage;
    public Text probMessage;
    public Text playerScoreText;
    public Text dealerScoreText;
    public Text discardTokensText; // Display for discard tokens

    // Bet
    public Button raiseBetButton;
    public Button lowerBetButton;
    public Text balance;
    public Text bet;
    private uint _balance = Constants.InitialBalance;
    private uint _bet;

    // Discard tokens
    private int _discardTokens = Constants.InitialDiscardTokens;

    public int[] values = new int[Constants.DeckCards];
    int cardIndex = 0;  
       
    private void Awake() => 
        InitCardValues();

    private void Start()
    {
        ShuffleCards();
        UpdateDiscardTokensUI();
        StartGame();   
    }

    // O(n) -> Linear time complexity
    private void InitCardValues()
    {
        int count = 0;
        for (int i = 0; i < values.Length; ++i) 
        {
            // This only affects the J, Q, K cards
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

    // Swap algorithms by un/definining `ARRAY_SHUFFLE` at the top
    private void ShuffleCards()
    {
        #if (ARRAY_SHUFFLE)
            ArrayShuffle();
        #else
            FisherYatesShuffle();
        #endif
    }

    // O(n) -> Linear time complexity
    private void FisherYatesShuffle()
    {
        for (int i = 0; i < values.Length; ++i)
        {
            int rndIndex = Random.Range(i, values.Length);

            // Swap sprites
            Sprite currCard = faces[i];
            faces[i] = faces[rndIndex];
            faces[rndIndex] = currCard;

            // Swap card values
            int currValue = values[i];
            values[i] = values[rndIndex];
            values[rndIndex] = currValue;
        }
    }

    // O(n * log n) -> Linearithmic time complexity
    private void ArrayShuffle()
    {
        // Randomized indices array -> [0, values.Length - 1]
        System.Random rnd = new System.Random();
        int[] index = Enumerable.Range(0, values.Length).ToArray();
        index.OrderBy(_ => rnd.Next()).ToArray();
        
        // Temporary arrays for shuffling
        int[] tmpValues = new int[Constants.DeckCards];
        Sprite[] tmpFaces = new Sprite[Constants.DeckCards];

        // Copy the elements by means of the randomized indices
        for (int i = 0; i < Constants.DeckCards; ++i)
        {
            tmpValues[index[i]] = values[i];
            tmpFaces[index[i]] = faces[i];
        }

        // Update the resulting arrays
        for (int i = 0; i < Constants.DeckCards; ++i)
        {
            values[i] = tmpValues[i];
            faces[i] = tmpFaces[i];
        }
    }

    private void StartGame()
    {
        StopCoroutine(NewGame());

        for (int i = 0; i < Constants.InitialCardsDealt; ++i)
        {
            PushPlayer();
            PushDealer();
        }
        UpdateScoreDisplays(); // Initial score display
        UpdateDiscardButtonState(); // Update discard button state

        if (Blackjack(player, true))
        {
            if (Blackjack(dealer, false)) { EndHand(WinCode.Draw); }        // Draw
            else { EndHand(WinCode.PlayerWins); }                           // Player wins
        }
        else if (Blackjack(dealer, false)) { EndHand(WinCode.DealerWins); } // Dealer wins
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
                // Contemplate soft aces that make make a blackjack
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

        // Every remaining ace has two possible values (different sums)
        for (int i = cardIndex; i < values.Length; ++i)
        {
            if (values[i] == 1) { possibleCases++; }
        }
        
        probMessage.text = ProbabilityDealerHigher(possibleCases) + " % | " + 
            ProbabilityPlayerInBetween(possibleCases) + " % | " + 
            ProbabibilityPlayerOver() + " %";
    }

    // Having the card hidden, probability that the dealer has a higher point count than the player
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
                // Default case
                sum = dealerPointsVisible + values[i];
                if (sum < Constants.Blackjack && sum > playerPoints)
                {
                    favorableCases++;
                }

                // Hidden ace as 11 points
                if (values[i] == 1)
                {
                    sum = dealerPointsVisible + Constants.SoftAce;
                    if (sum < Constants.Blackjack && sum > playerPoints)
                    {
                        favorableCases++;
                    }
                }

                // Visible ace as 11 points
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

    // Probability that the player gets 17 - 21 points if he/she asks for a card
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

            // Contemplate an ace as 11 points
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

    // Probability that the player goes over 21 points if he/she asks for a card
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
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        // Potentially update dealer score here if their first card was visible,
        // but typical Blackjack rules hide it. So, update when it's flipped.
    }

    private void PushPlayer()
    {
        player.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        UpdateScoreDisplays(); // Update after player gets a card

        CalculateProbabilities();
    }

    public void Hit()
    {
        PushPlayer();
        // FlipDealerCard(); // Dealer card is not flipped on player hit generally
        
        UpdateScoreDisplays(); // Update scores after player hits
        UpdateDiscardButtonState(); // Update discard button state

        // Check for Blackjack and the win
        if (Blackjack(player, true)) { EndHand(WinCode.PlayerWins); }
        else if (GetPlayerPoints() > Constants.Blackjack) { EndHand(WinCode.DealerWins); }
    }

    public void Stand()
    {
        int playerPoints = player.GetComponent<CardHand>().points;
        int dealerPoints = dealer.GetComponent<CardHand>().points; // This is full points, not visible yet

        FlipDealerCard(); // Dealer reveals card now

        while (dealerPoints < Constants.DealerStand)
        {
            PushDealer();
            dealerPoints = dealer.GetComponent<CardHand>().points;
            UpdateScoreDisplays(); // Update as dealer draws
        }
        
        UpdateScoreDisplays(); // Final update after dealer's turn

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
        UpdateScoreDisplays(); // Update scores when dealer card is flipped
    }

    private void EndHand(WinCode code)
    {   
        FlipDealerCard();
        switch (code)
        {
            case WinCode.DealerWins:
                finalMessage.text = "You lose!";
                _balance -= _bet;
                break;
            case WinCode.PlayerWins:
                finalMessage.text = "You win!";
                _balance += Constants.BetWinMultiplier * _bet;
                // Award a discard token for winning
                _discardTokens++;
                UpdateDiscardTokensUI();
                break;
            case WinCode.Draw:
                finalMessage.text = "Draw!";
                break;
            default:
                Debug.Assert(false);    // Report invalid input
                break;
        }

        // Disable buttons
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        raiseBetButton.interactable = false;
        lowerBetButton.interactable = false;

        // Update bet and balance
        _bet = 0;
        bet.text = "Bet: 0 $";
        balance.text = "Balance: " + _balance + " $";
        UpdateScoreDisplays(); // Ensure final scores are displayed

        if (_balance == 0)
        {
            finalMessage.text += "\n - GAME OVER -";
            StartCoroutine(NewGame());
        }
    }

    public void PlayAgain()
    {   
        // Reset GUI
        hitButton.interactable = true;
        stickButton.interactable = true;
        raiseBetButton.interactable = true;
        lowerBetButton.interactable = true;
        UpdateDiscardButtonState(); // Initialize discard button state
        finalMessage.text = "";

        // Clear hand
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();          
        cardIndex = 0;
        
        ShuffleCards();
        StartGame();
        UpdateScoreDisplays(); // Reset scores for new game (will be updated again in StartGame)
    }

    public void RaiseBet()
    {
        if (_bet < _balance)
        {
            _bet += Constants.BetIncrement;
            bet.text = "Bet: " + _bet.ToString() + " $";
            playAgainButton.interactable = true;
        }
    }
    
    public void LowerBet()
    {
        if (_bet > 0)
        {
            _bet -= Constants.BetIncrement;
            bet.text = "Bet: " + _bet.ToString() + " $";
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
            // We need to access the SpriteRenderer of the CardModel to check the current sprite
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

    // Listen for card selection changes 
    void Update()
    {
        // Check for card selection changes to update the discard button state
        CardHand playerHand = player.GetComponent<CardHand>();
        if (playerHand != null)
        {
            bool hasSelectedCard = playerHand.HasSelectedCard();
            
            // Only update the button if its current state is different from what it should be
            if (discardButton != null && discardButton.interactable != (_discardTokens > 0 && hasSelectedCard))
            {
                UpdateDiscardButtonState();
                
                // Debug message to help diagnose selection issues
                if (hasSelectedCard)
                {
                    Debug.Log("Card selected - Discard button should be enabled: " + (_discardTokens > 0));
                }
            }
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
            discardButton.interactable = (_discardTokens > 0 && hasSelectedCard);
            
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
        if (_discardTokens <= 0)
        {
            Debug.LogWarning("Cannot discard: No tokens left");
            return;
        }
        
        if (!playerHand.HasSelectedCard())
        {
            Debug.LogWarning("Cannot discard: No card selected");
            return;
        }
        
        // All checks passed, proceed with discard
        Debug.Log("Discarding card... Tokens before: " + _discardTokens);
        _discardTokens--;
        
        // Call the discard method on the hand
        playerHand.DiscardSelectedCard();
        
        // Update UI elements
        UpdateDiscardTokensUI();
        UpdateDiscardButtonState();
        
        // Check if player busted but now is back under 21 after discarding
        int playerPoints = GetPlayerPoints();
        Debug.Log("Player points after discard: " + playerPoints);
        
        if (playerPoints <= Constants.Blackjack)
        {
            hitButton.interactable = true;
            stickButton.interactable = true;
        }
    }
    
    private void UpdateDiscardTokensUI()
    {
        if (discardTokensText != null)
        {
            discardTokensText.text = "Discard Tokens: " + _discardTokens;
        }
    }
}