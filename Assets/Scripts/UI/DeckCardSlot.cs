using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Represents a single card slot in the deck inspector grid
/// Displays the card image and handles interaction
/// </summary>
public class DeckCardSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image cardImage;                 // Image component to display the card
    public Text emptyText;                  // Text to show when slot is empty
    public Image borderImage;               // Optional border highlight
    public GameObject dealtOverlay;         // Overlay to show when card has been dealt
    
    [Header("Empty State")]
    public Sprite emptySlotSprite;          // Sprite to show when no card
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Header("Hover Effects")]
    public float hoverScale = 1.1f;         // Scale on hover
    public float hoverDuration = 0.2f;      // Animation duration
    
    // Current card data
    private PlayerDeckCard currentCard;
    private bool isEmpty = true;
    private DeckInspectorPanel inspectorPanel;
    
    private void Awake()
    {
        // Auto-find components if not assigned
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        
        if (emptyText == null)
        {
            emptyText = GetComponentInChildren<Text>();
        }
        
        // Find parent inspector panel
        inspectorPanel = GetComponentInParent<DeckInspectorPanel>();
    }
    
    /// <summary>
    /// Set this slot to display a specific card
    /// </summary>
    public void SetCard(PlayerDeckCard card)
    {
        currentCard = card;
        isEmpty = false;
        
        if (cardImage != null)
        {
            if (card.cardSprite != null)
            {
                cardImage.sprite = card.cardSprite;
                cardImage.color = Color.white;
            }
            else
            {
                // Fallback if no sprite
                cardImage.sprite = emptySlotSprite;
                cardImage.color = emptyColor;
            }
        }
        
        // Hide empty text
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }
        
        // Show dealt overlay if card has been dealt (for full deck view)
        if (dealtOverlay != null)
        {
            dealtOverlay.SetActive(card.isDealt);
        }
    }
    
    /// <summary>
    /// Set this slot to empty state
    /// </summary>
    public void SetEmpty()
    {
        currentCard = null;
        isEmpty = true;
        
        if (cardImage != null)
        {
            if (emptySlotSprite != null)
            {
                cardImage.sprite = emptySlotSprite;
            }
            cardImage.color = emptyColor;
        }
        
        // Show empty text
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(true);
            emptyText.text = "Empty";
        }
        
        // Hide dealt overlay
        if (dealtOverlay != null)
        {
            dealtOverlay.SetActive(false);
        }
    }
    
    /// <summary>
    /// Get the current card in this slot
    /// </summary>
    public PlayerDeckCard GetCard()
    {
        return currentCard;
    }
    
    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return isEmpty;
    }
    
    // ============ INTERACTION HANDLERS ============
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up on hover
        transform.DOKill();
        transform.DOScale(Vector3.one * hoverScale, hoverDuration).SetEase(Ease.OutQuad);
        
        // Show border highlight
        if (borderImage != null)
        {
            borderImage.DOFade(1f, hoverDuration);
        }
        
        // If this is an action card, show its description
        if (currentCard != null && currentCard.isActionCard && inspectorPanel != null)
        {
            // For now, just show a placeholder since action card system isn't fully integrated
            inspectorPanel.ShowActionCardDescription($"Action Card\n{currentCard.displayName}");
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset scale
        transform.DOKill();
        transform.DOScale(Vector3.one, hoverDuration).SetEase(Ease.OutQuad);
        
        // Hide border highlight
        if (borderImage != null)
        {
            borderImage.DOFade(0f, hoverDuration);
        }
        
        // Clear action card description
        if (inspectorPanel != null)
        {
            inspectorPanel.ClearActionCardDescription();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEmpty || currentCard == null) return;
        
        // Visual feedback
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1f);
        
        // Log card info for debugging
        Debug.Log($"[DeckCardSlot] Clicked: {currentCard.displayName} (Value: {currentCard.value}, Suit: {currentCard.suit})");
        
        // If action card, show description
        if (currentCard.isActionCard && inspectorPanel != null)
        {
            inspectorPanel.ShowActionCardDescription($"Action Card Selected\n{currentCard.displayName}");
        }
    }
}

