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
    public const int InitialDiscardTokens = 3; // Starting number of discard tokens
    public const int MaxCardsInHand = 5; // Maximum number of cards in a hand
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
    public Text discardTokensText; 
     public Button raiseBetButton;
    public Button lowerBetButton;
    public Text balance;
    public Text bet;
    private uint _balance = Constants.InitialBalance;
    private uint _bet;
 
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

        for (int i = 0; i < Constants.InitialCardsDealt; ++i)
        {
            PushPlayer();
            PushDealer();
        }
        UpdateScoreDisplays(); 
        UpdateDiscardButtonState();  

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
        dealer.GetComponent<CardHand>().Push(faces[cardIndex], values[cardIndex]);
        cardIndex++;
        // Potentially update dealer score here if their first card was visible,
        // but typical Blackjack rules hide it. So, update when it's flipped.
    }

    private void PushPlayer()
    {
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
        switch (code)
        {
            case WinCode.DealerWins:
                finalMessage.text = "You lose!";
                _balance -= _bet;
                break;
            case WinCode.PlayerWins:
                finalMessage.text = "You win!";
                _balance += Constants.BetWinMultiplier * _bet; 
                _discardTokens++;
                UpdateDiscardTokensUI();
                break;
            case WinCode.Draw:
                finalMessage.text = "Draw!";
                break;
            default:
                Debug.Assert(false);    
                break;
        }
 
        hitButton.interactable = false;
        stickButton.interactable = false;
        discardButton.interactable = false;
        raiseBetButton.interactable = false;
        lowerBetButton.interactable = false;
 
        _bet = 0;
        bet.text = "Bet: 0 $";
        balance.text = "Balance: " + _balance + " $";
        UpdateScoreDisplays();  

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
        UpdateDiscardButtonState();  
        finalMessage.text = "";

        // Clear hand
        player.GetComponent<CardHand>().Clear();
        dealer.GetComponent<CardHand>().Clear();          
        cardIndex = 0;
        
        ShuffleCards();
        StartGame();
        UpdateScoreDisplays();  
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
             
            if (discardButton != null && discardButton.interactable != (_discardTokens > 0 && hasSelectedCard))
            {
                UpdateDiscardButtonState();
                 
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
         
        Debug.Log("Discarding card... Tokens before: " + _discardTokens);
        _discardTokens--;
         
        playerHand.DiscardSelectedCard();
         
        UpdateDiscardTokensUI();
        UpdateDiscardButtonState();
         
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