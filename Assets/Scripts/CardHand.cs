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

        // Card placement for both the player and the dealer
        coordY = isDealer ? -1 : 3;
    }

    public void FlipFirstCard() => 
        cards[0].GetComponent<CardModel>().ToggleFace(true);
    
    public void Push(Sprite front, int value)
    {
        // Create a new card and add it to the current hand
        GameObject cardCopy = (GameObject) Instantiate(card);
        cards.Add(cardCopy);

        // Set parent to this hand
        cardCopy.transform.SetParent(transform);
        cardCopy.name = "Card_" + cards.Count;

        // Position it on the table
        float coordX = 1.4f * (float) (cards.Count - 4);
        cardCopy.transform.position = new Vector3(coordX, coordY);

        // Assign it the right cover and value
        CardModel cardModel = cardCopy.GetComponent<CardModel>();
        if (cardModel == null)
        {
            Debug.LogError("CardModel component missing on card prefab!");
            return;
        }
        
        cardModel.cardFront = front;
        cardModel.value = value;
        
        // Cover up the dealer's first card
        bool isCovered = (isDealer && cards.Count <= 1) ? false : true;
        cardModel.ToggleFace(isCovered);
        
        UpdatePoints();
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

        // Consider soft aces situations
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
            
            // First deselect the card visually (removes highlight)
            cardModel.DeselectCard();
            
            // Animate card moving away before destroying
            selectedCard.transform.DOMove(new Vector3(selectedCard.transform.position.x + 3, selectedCard.transform.position.y, selectedCard.transform.position.z), 0.5f)
                .SetEase(Ease.OutQuint)
                .OnComplete(() => {
                    // Store a reference to the Deck before removing the card
                    Deck deck = FindObjectOfType<Deck>();
                    
                    // Remove and destroy the card
                    cards.Remove(selectedCard);
                    Destroy(selectedCard);
                    
                    // Rearrange the remaining cards
                    RearrangeCards();
                    
                    // Recalculate points
                    UpdatePoints();
                    
                    // Force score display update on the Deck
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
        for (int i = 0; i < cards.Count; i++)
        {
            float coordX = 1.4f * (float)(i - (cards.Count / 2));
            cards[i].transform.DOMove(new Vector3(coordX, coordY, 0), 0.3f);
        }
    }
}