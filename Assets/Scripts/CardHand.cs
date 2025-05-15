using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardHand : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();
    public GameObject card;
    public bool isDealer = false;
    public int points;
    private int coordY;
    
    // Card positioning constants
    private const float CARD_SPACING = 1.4f;
    private const float CARD_OVERLAP_THRESHOLD = 0.8f;
     
    private void Awake() => 
        DefaultState();

    public void Clear()
    {
        DefaultState();
    
        foreach (GameObject c in cards) { Destroy(c); }
        cards.Clear();
    }

    private void DefaultState()
    {
        points = 0;

        coordY = isDealer ? -1 : 3;
    }

    public void FlipFirstCard() => 
        cards[0].GetComponent<CardModel>().ToggleFace(true);
    
    public void Push(Sprite front, int value)
    {
        if (cards.Count >= Constants.MaxCardsInHand)
        {
            Debug.LogWarning("Maximum card limit reached (" + Constants.MaxCardsInHand + ")");
            
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateScoreDisplays();
                deck.UpdateDiscardButtonState();
            }
            return;
        }
        
        GameObject cardCopy = (GameObject) Instantiate(card);
        cards.Add(cardCopy);

        cardCopy.transform.SetParent(transform);
        cardCopy.name = "Card_" + cards.Count;

        ArrangeCardsInWindow();

        CardModel cardModel = cardCopy.GetComponent<CardModel>();
        if (cardModel == null)
        {
            Debug.LogError("CardModel component missing on card prefab!");
            return;
        }
        
        cardModel.cardFront = front;
        cardModel.value = value;
        
        bool isCovered = (isDealer && cards.Count <= 1) ? false : true;
        cardModel.ToggleFace(isCovered);
        
        UpdatePoints();
    }
    
    private void ArrangeCardsInWindow()
    {
        if (cards.Count == 0) return;
        
        float totalWidth = CARD_SPACING * (cards.Count - 1);
        
        float startX = -totalWidth / 2;
        
        for (int i = 0; i < cards.Count; i++)
        {
            float posX = startX + (CARD_SPACING * i);
            Vector3 targetPos = new Vector3(posX, coordY, 0);
             
            if (i == cards.Count - 1)
            {
                cards[i].transform.position = targetPos;
            } 
            else
            {
                cards[i].transform.DOMove(targetPos, 0.3f).SetEase(Ease.OutQuad);
            }
        }
    }
    
    public void UpdatePoints()
    {
        int val = 0;
        int aces = 0;
        foreach (GameObject c in cards)
        {            
            if (c.GetComponent<CardModel>().value == 1) 
            {
                aces++;
            }
            else
            {
                val += c.GetComponent<CardModel>().value;
            }
        }
 
        for (int i = 0; i < aces; ++i)
        {
            val += (val + Constants.SoftAce <= Constants.Blackjack) ? Constants.SoftAce : 1;
        }

        points = val;
    }
    
    public bool HasSelectedCard()
    {
        foreach (GameObject cardObj in cards)
        {
            if (cardObj.GetComponent<CardModel>().isSelected)
            {
                return true;
            }
        }
        return false;
    }
    
    public GameObject GetSelectedCard()
    {
        foreach (GameObject cardObj in cards)
        {
            if (cardObj.GetComponent<CardModel>().isSelected)
            {
                return cardObj;
            }
        }
        return null;
    }
    
    public void DiscardSelectedCard()
    {
        GameObject selectedCard = GetSelectedCard();
        if (selectedCard != null)
        {
            CardModel cardModel = selectedCard.GetComponent<CardModel>();
            Debug.Log("Discarding card with value: " + cardModel.value);
             
            cardModel.DeselectCard();
             
            selectedCard.transform.DOMove(new Vector3(selectedCard.transform.position.x + 3, selectedCard.transform.position.y, selectedCard.transform.position.z), 0.5f)
                .SetEase(Ease.OutQuint)
                .OnComplete(() => { 
                    Deck deck = FindObjectOfType<Deck>();
                     
                    cards.Remove(selectedCard);
                    Destroy(selectedCard);
                     
                    ArrangeCardsInWindow();
                     
                    UpdatePoints();
                     
                    if (deck != null)
                    {
                        Debug.Log("Updating score display after discard, new points: " + points);
                        deck.UpdateScoreDisplays();
                    }
                    else
                    {
                        Debug.LogError("Could not find Deck to update score display!");
                    }
                });
        }
        else
        {
            Debug.LogError("No selected card found to discard!");
        }
    }
     
    private void RearrangeCards()
    {
        ArrangeCardsInWindow();
    }
     
    public int GetCardCount()
    {
        return cards.Count;
    }
     
    public bool CanAddMoreCards()
    {
        return cards.Count < Constants.MaxCardsInHand;
    }

    public int GetSelectedCardCount()
    {
        int count = 0;
        foreach (GameObject cardObj in cards)
        {
            if (cardObj.GetComponent<CardModel>().isSelected)
            {
                count++;
            }
        }
        return count;
    }
    
    public List<GameObject> GetSelectedCards()
    {
        List<GameObject> selectedCards = new List<GameObject>();
        foreach (GameObject cardObj in cards)
        {
            if (cardObj.GetComponent<CardModel>().isSelected)
            {
                selectedCards.Add(cardObj);
            }
        }
        return selectedCards;
    }
    
    public void TransformSelectedCards()
    {
        List<GameObject> selectedCards = GetSelectedCards();
        
        if (selectedCards.Count != Constants.MaxSelectedCards)
        {
            Debug.LogError("TransformSelectedCards requires exactly " + Constants.MaxSelectedCards + " selected cards!");
            return;
        }
        
        // Get the two selected cards
        GameObject firstCardObj = selectedCards[0];  // The card to be replaced
        GameObject secondCardObj = selectedCards[1]; // The card to be duplicated
        
        CardModel firstCard = firstCardObj.GetComponent<CardModel>();
        CardModel secondCard = secondCardObj.GetComponent<CardModel>();
        
        // Store the original scale of cards in the hand
        Vector3 originalScale = secondCardObj.transform.localScale;
        
        Debug.Log("Transforming card with value " + firstCard.value + " into duplicate of card with value " + secondCard.value);
        Debug.Log("Original card scale: " + originalScale);
        
        // Store the position of the first card
        Vector3 firstCardPosition = firstCardObj.transform.position;
        
        // Visual effect for transformation
        firstCardObj.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
            // Deselect the second card
            secondCard.DeselectCard();
            
            // Create a visual copy of the second card
            GameObject newCardObj = Instantiate(card, transform);
            CardModel newCard = newCardObj.GetComponent<CardModel>();
            
            // Set properties to match the second card
            newCard.value = secondCard.value;
            newCard.cardFront = secondCard.cardFront;
            
            // Position at the first card's location
            newCardObj.transform.position = firstCardPosition;
            newCardObj.transform.localScale = Vector3.zero; // Start small for grow effect
            
            // Remove the first card from the list before destroying it
            cards.Remove(firstCardObj);
            
            // Add the new card to our hand
            cards.Add(newCardObj);
            newCardObj.name = "Card_Transformed";
            
            // Destroy the original first card
            Destroy(firstCardObj);
            
            // Grow the new card with animation to the exact same scale as other cards
            newCardObj.transform.DOScale(originalScale, 0.3f).SetEase(Ease.OutBack).OnComplete(() => {
                // Ensure the card has the exact same scale as other cards
                newCardObj.transform.localScale = originalScale;
                Debug.Log("New card scale after animation: " + newCardObj.transform.localScale);
            });
            
            // Show the front face
            newCard.ToggleFace(true);
            
            // Rearrange cards in hand
            ArrangeCardsInWindow();
            
            // Update hand points
            UpdatePoints();
            
            // Update UI displays
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateScoreDisplays();
            }
        });
    }
}