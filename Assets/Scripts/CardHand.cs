using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardHand : MonoBehaviour
{
    public List<GameObject> cards = new List<GameObject>();
    public GameObject card;
    public bool isDealer = false;
    public int points;
    
    // Card positioning constants
    private const float CARD_SPACING = 3.2f; // Increased spacing for camera-based setup
    private const float CARD_SCALE = 7.5f; // Much larger scale for camera-based view
    private const float CARD_OVERLAP_THRESHOLD = 1.8f;
    
    // Canvas reference for scaling context
    private Canvas parentCanvas;
     
    private void Awake()
    {
        DefaultState();
        // Find parent canvas if it exists
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void Clear()
    {
        DefaultState();
    
        foreach (GameObject c in cards) { Destroy(c); }
        cards.Clear();
    }

    private void DefaultState()
    {
        points = 0;
    }

    public void FlipFirstCard() => 
        cards[0].GetComponent<CardModel>().ToggleFace(true);
    
    // New method for creating cards without automatic positioning (for animations)
    public GameObject CreateCard(Sprite front, int value, bool skipArrangement = false)
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
            return null;
        }
        
        GameObject cardCopy = (GameObject) Instantiate(card);
        cards.Add(cardCopy);
        
        // Set parent directly to this transform
        cardCopy.transform.SetParent(transform, false);
        cardCopy.name = "Card_" + cards.Count;
        
        // Apply appropriate scale for camera-based setup
        ApplyCardScale(cardCopy);

        // Position the cards within the hand (unless skipped for animation)
        if (!skipArrangement)
        {
            ArrangeCardsInWindow();
        }

        CardModel cardModel = cardCopy.GetComponent<CardModel>();
        if (cardModel == null)
        {
            Debug.LogError("CardModel component missing on card prefab!");
            return null;
        }
        
        cardModel.cardFront = front;
        cardModel.value = value;
        
        // For dealers, the first card should show back, others show front
        // For players, all cards should show front
        bool shouldShowFront = true;
        if (isDealer && cards.Count == 1)  // First dealer card
        {
            shouldShowFront = false;
        }
        
        // Toggle the face appropriately
        cardModel.ToggleFace(shouldShowFront);
        
        // Ensure BoxCollider2D is properly sized for interaction
        BoxCollider2D boxCollider = cardCopy.GetComponent<BoxCollider2D>();
        if (boxCollider != null && cardModel.cardFront != null)
        {
            // Size the collider based on the sprite bounds and scale
            Bounds bounds = cardModel.cardFront.bounds;
            Vector2 size = bounds.size;
            
            // Make collider slightly larger to ensure it covers the entire card
            boxCollider.size = size * 0.7f;
        }
        
        UpdatePoints();
        return cardCopy;
    }
    
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
        
        // Set parent directly to this transform
        cardCopy.transform.SetParent(transform, false);
        cardCopy.name = "Card_" + cards.Count;
        
        // Apply appropriate scale for camera-based setup
        ApplyCardScale(cardCopy);

        // Position the cards within the hand
        ArrangeCardsInWindow();

        CardModel cardModel = cardCopy.GetComponent<CardModel>();
        if (cardModel == null)
        {
            Debug.LogError("CardModel component missing on card prefab!");
            return;
        }
        
        cardModel.cardFront = front;
        cardModel.value = value;
        
        // For dealers, the first card should show back, others show front
        // For players, all cards should show front
        bool shouldShowFront = true;
        if (isDealer && cards.Count == 1)  // First dealer card
        {
            shouldShowFront = false;
        }
        
        // Toggle the face appropriately
        cardModel.ToggleFace(shouldShowFront);
        
        // Ensure BoxCollider2D is properly sized for interaction
        BoxCollider2D boxCollider = cardCopy.GetComponent<BoxCollider2D>();
        if (boxCollider != null && cardModel.cardFront != null)
        {
            // Size the collider based on the sprite bounds and scale
            Bounds bounds = cardModel.cardFront.bounds;
            Vector2 size = bounds.size;
            
            // Make collider slightly larger to ensure it covers the entire card
            boxCollider.size = size * 0.7f;
        }
        
        UpdatePoints();
    }
    
    private void ApplyCardScale(GameObject cardObj)
    {
        // For camera-based setup, use a consistent scale
        Vector3 scale = Vector3.one * CARD_SCALE;
        cardObj.transform.localScale = scale;
    }
    
    public void ArrangeCardsInWindow()
    {
        if (cards.Count == 0) return;
        
        // For camera-based setup, use fixed width spacing
        float panelWidth = 20f; // Default width for camera-based setup
        float cardSpacing = CARD_SPACING;
        
        // Calculate card width based on actual card sprite and scale
        SpriteRenderer cardSprite = card.GetComponent<SpriteRenderer>();
        float cardWidth = 0;
        
        if (cardSprite != null && cardSprite.sprite != null)
        {
            cardWidth = cardSprite.sprite.bounds.size.x * CARD_SCALE * 0.8f;
        }
        else
        {
            cardWidth = 1f * CARD_SCALE; // Fallback size if sprite not available
        }
        
        // Limit max card width
        cardWidth = Mathf.Min(cardWidth, 3f);
        
        // Ensure minimum card width
        cardWidth = Mathf.Max(cardWidth, 0.2f);
        
        // Calculate spacing based on number of cards
        float availableWidth = panelWidth;
        float actualSpacing = Mathf.Min(cardSpacing, (availableWidth - (cardWidth * cards.Count)) / Mathf.Max(1, cards.Count - 1));
        actualSpacing = Mathf.Max(actualSpacing, -cardWidth * 0.5f); // Prevent excessive overlap
        
        float totalWidth = (cards.Count > 1) ? 
            cardWidth + (actualSpacing * (cards.Count - 1)) : 
            cardWidth;
            
        float startX = -totalWidth / 2 + (cardWidth / 2);
        
        for (int i = 0; i < cards.Count; i++)
        {
            float posX = startX + (i * (cardWidth + actualSpacing));
            // Use local position within the hand
            Vector3 targetPos = new Vector3(posX, 0, 0);
             
            if (i == cards.Count - 1)
            {
                cards[i].transform.localPosition = targetPos;
                // Update original position for the last card immediately
                CardModel cardModel = cards[i].GetComponent<CardModel>();
                if (cardModel != null)
                {
                    cardModel.UpdateOriginalPosition();
                }
            } 
            else
            {
                GameObject currentCard = cards[i]; // Capture the card reference for the closure
                cards[i].transform.DOLocalMove(targetPos, 0.3f).SetEase(Ease.OutQuad)
                    .OnComplete(() => {
                        // Update original position after movement animation completes
                        CardModel cardModel = currentCard.GetComponent<CardModel>();
                        if (cardModel != null)
                        {
                            cardModel.UpdateOriginalPosition();
                        }
                    });
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
             
            // Determine discard direction based on canvas mode
            float discardOffset = 0.5f;
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
            {
                // For screen space canvas, use a larger offset
                RectTransform panelRect = GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    discardOffset = panelRect.rect.width * 0.2f; // 20% of panel width
                }
            }
            
            // Reset card scale first (selection might have scaled it up)
            Vector3 originalScale = selectedCard.transform.localScale / CardModel.SELECTION_SCALE_INCREASE;
            selectedCard.transform.DOScale(originalScale, 0.2f)
                .SetEase(Ease.OutQuad);
            
            cardModel.DeselectCard();
            
            // Use local movement for discard animation
            Vector3 discardTarget = new Vector3(selectedCard.transform.localPosition.x + discardOffset, 
                                                selectedCard.transform.localPosition.y, 
                                                selectedCard.transform.localPosition.z);
            
            selectedCard.transform.DOLocalMove(discardTarget, 0.5f)
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
                        deck.UpdateDiscardButtonState();
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
        Vector3 originalScale = secondCardObj.transform.localScale / CardModel.SELECTION_SCALE_INCREASE;
        
        Debug.Log("Transforming card with value " + firstCard.value + " into duplicate of card with value " + secondCard.value);
        Debug.Log("Original card scale: " + originalScale);
        
        // Store the position of the first card
        Vector3 firstCardPosition = firstCardObj.transform.localPosition;
        
        // Visual effect for transformation - scale up by 20% from current scale
        Vector3 growScale = firstCardObj.transform.localScale * 1.2f;
        firstCardObj.transform.DOScale(growScale, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => {
            // Deselect the second card
            secondCard.DeselectCard();
            
            // Create a visual copy of the second card
            GameObject newCardObj = Instantiate(card, transform, false); // worldPositionStays = false
            CardModel newCard = newCardObj.GetComponent<CardModel>();
            
            // Set properties to match the second card
            newCard.value = secondCard.value;
            newCard.cardFront = secondCard.cardFront;
            
            // Position at the first card's location
            newCardObj.transform.localPosition = firstCardPosition;
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
                deck.UpdateDiscardButtonState();
            }
        });
    }
}