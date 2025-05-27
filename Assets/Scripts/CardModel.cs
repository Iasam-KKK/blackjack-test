using UnityEngine;
using DG.Tweening;

public class CardModel : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    BoxCollider2D boxCollider;
    public Sprite cardBack;
    public Sprite cardFront;
    public int value;
    public bool isSelected = false;
    private Vector3 originalPosition;
    private bool isInitialized = false;
    
    // Reference to the CardHand that owns this card
    private CardHand ownerHand;
    
    // Card selection constants
    private const float SELECTION_RAISE_AMOUNT = 0.1f; // Small raise amount for camera setup
    public static float SELECTION_SCALE_INCREASE = 1.15f; // Scale up selected cards by 15%
    private const float COLLIDER_SIZE_MULTIPLIER = 1.5f; // Make collider larger than sprite

    private void Awake() 
    { 
        spriteRenderer = GetComponent<SpriteRenderer>();
         
        // Create a fresh collider
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Destroy(boxCollider);
        }
        
        boxCollider = gameObject.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = false;
    }
    
    private void Start()
    { 
        originalPosition = transform.localPosition;
        isInitialized = true;
        
        // Force update collider size
        UpdateColliderSize();
        
        // Find the owning hand - we need this for card selection
        FindOwnerHand();
    }
    
    // Find the CardHand that owns this card based on the tag or scene hierarchy
    public void FindOwnerHand()
    {
        // First try to get the owner from the Deck system (optimal)
        Deck deck = FindObjectOfType<Deck>();
        if (deck != null)
        {
            // Check if this card is in the dealer's or player's hand list
            if (deck.dealer != null)
            {
                CardHand dealerHand = deck.dealer.GetComponent<CardHand>();
                if (dealerHand != null && dealerHand.cards.Contains(gameObject))
                {
                    ownerHand = dealerHand;
                    Debug.Log("Card " + name + " found in dealer hand");
                    return;
                }
            }
            
            if (deck.player != null)
            {
                CardHand playerHand = deck.player.GetComponent<CardHand>();
                if (playerHand != null && playerHand.cards.Contains(gameObject))
                {
                    ownerHand = playerHand;
                    Debug.Log("Card " + name + " found in player hand");
                    return;
                }
            }
        }
        
        // If we reach here, we couldn't find the owner hand
        Debug.LogWarning("Could not determine owner hand for card " + name);
    }

    private void UpdateColliderSize()
    {
        // Set the collider size to match the sprite but make it larger
        if (spriteRenderer.sprite != null && boxCollider != null)
        {
            // Get original sprite bounds
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            
            // Make collider larger to ensure clicks are detected
            boxCollider.size = spriteSize * COLLIDER_SIZE_MULTIPLIER;
        }
    }

    public void ToggleFace(bool showFace)
    {
        // Store a reference to help with face comparison
        Sprite previousSprite = spriteRenderer.sprite;
        
        // Toggle the sprite
        spriteRenderer.sprite = showFace ? cardFront : cardBack;
        
        // Update collider when sprite changes
        UpdateColliderSize();
    }
     
    private void OnMouseEnter()
    { 
        if (CanInteract())
        {
            spriteRenderer.color = new Color(1f, 1f, 0.8f);
        }
    }
     
    private void OnMouseExit()
    { 
        spriteRenderer.color = Color.white;
    }
     
    private bool CanInteract()
    {
        // If we haven't found the owner hand yet, try to find it
        if (ownerHand == null)
        {
            FindOwnerHand();
        }
        
        // Simple interaction rule: if it's not a dealer card and showing its front face, it can be interacted with
        bool isFrontFacing = spriteRenderer.sprite == cardFront;
        bool isPlayerCard = ownerHand != null && !ownerHand.isDealer;
        
        // IMPORTANT: For debugging - if owner is null but we can visually verify it's a player card
        if (ownerHand == null)
        {
            // Just assume the card is interactive if it's showing front face (user asked us to)
            return isFrontFacing;
        }
        
        return isPlayerCard && isFrontFacing;
    }
        
    private void OnMouseDown()
    {
        if (!CanInteract()) return;
        
        if (Input.GetMouseButton(0))
        {
            if (isSelected)
            {
                DeselectCard();
            }
            else
            {
                // Find the hand this card belongs to
                Deck deck = FindObjectOfType<Deck>();
                CardHand hand = null;
                
                if (deck != null && deck.player != null)
                {
                    hand = deck.player.GetComponent<CardHand>();
                }
                
                // If we found the hand, check if we're below max selections
                if (hand != null && hand.GetSelectedCardCount() < Constants.MaxSelectedCards)
                {
                    SelectCard();
                }
                else if (hand == null)
                {
                    // Fallback if we can't find the hand - just allow selection
                    SelectCard();
                }
                else
                {
                    Debug.Log("Cannot select more than " + Constants.MaxSelectedCards + " cards at once");
                }
            }
        } 
        else if (Input.GetMouseButton(1) && isSelected)
        {
            DeselectCard();
        }
    }
    
    // Method to update the original position after animations complete
    public void UpdateOriginalPosition()
    {
        originalPosition = transform.localPosition;
        Debug.Log("Updated original position for card " + name + " to: " + originalPosition);
    }
    
    public void SelectCard()
    {
        if (!isSelected && isInitialized)
        {
            isSelected = true;
             
            // Always update original position when selecting to ensure it's current
            originalPosition = transform.localPosition;
             
            transform.DOKill();
            
            // Move card up slightly and scale it up for better visibility
            transform.DOLocalMove(new Vector3(transform.localPosition.x, originalPosition.y + SELECTION_RAISE_AMOUNT, transform.localPosition.z), 0.3f)
                .SetEase(Ease.OutBack);
                
            // Scale up the card to make selection more visible
            Vector3 originalScale = transform.localScale;
            transform.DOScale(originalScale * SELECTION_SCALE_INCREASE, 0.3f)
                .SetEase(Ease.OutBack);
                
            spriteRenderer.DOColor(new Color(1f, 1f, 0.5f), 0.3f);
             
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateDiscardButtonState();
                deck.UpdateTransformButtonState();
            }
        }
    }
    
    public void DeselectCard()
    {
        if (isSelected)
        {
            isSelected = false;
             
            transform.DOKill();
             
            // Reset position
            transform.DOLocalMove(new Vector3(transform.localPosition.x, originalPosition.y, transform.localPosition.z), 0.3f)
                .SetEase(Ease.OutQuad);
            
            // Reset scale
            Vector3 originalScale = transform.localScale / SELECTION_SCALE_INCREASE;
            transform.DOScale(originalScale, 0.3f)
                .SetEase(Ease.OutQuad);
                 
            spriteRenderer.DOColor(Color.white, 0.3f);
             
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateDiscardButtonState();
                deck.UpdateTransformButtonState();
            }
        }
    }
}