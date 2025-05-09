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

    private void Awake() 
    { 
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Ensure there's a collider for mouse interaction
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
    }
    
    private void Start()
    {
        // Store original position after the card is placed in its final position
        originalPosition = transform.position;
        isInitialized = true;
        
        // Ensure collider size matches sprite
        if (spriteRenderer.sprite != null)
        {
            boxCollider.size = spriteRenderer.sprite.bounds.size;
        }
        
        // Make sure collider is enabled
        boxCollider.enabled = true;
    }

    public void ToggleFace(bool showFace)
    {
        spriteRenderer.sprite = showFace ? cardFront : cardBack;
    }
    
    // Called when mouse pointer enters object
    private void OnMouseEnter()
    {
        // Debug visual feedback - subtle highlight
        if (CanInteract())
        {
            spriteRenderer.color = new Color(1f, 1f, 0.8f);
        }
    }
    
    // Called when mouse pointer exits object
    private void OnMouseExit()
    {
        // Reset color
        spriteRenderer.color = Color.white;
    }
    
    // Check if this card can be interacted with (player's card and face up)
    private bool CanInteract()
    {
        return spriteRenderer.sprite == cardFront && 
               transform.parent != null &&
               transform.parent.GetComponent<CardHand>() != null &&
               !transform.parent.GetComponent<CardHand>().isDealer;
    }
        
    private void OnMouseDown()
    {
        if (!CanInteract()) return;
        
        // Right-click to select
        if (Input.GetMouseButton(1))
        {
            if (!isSelected)
            {
                SelectCard();
                Debug.Log("RIGHT-CLICK: Card selected: " + gameObject.name);
            }
        }
        // Left-click to deselect if already selected
        else if (Input.GetMouseButton(0))
        {
            if (isSelected)
            {
                DeselectCard();
                Debug.Log("LEFT-CLICK: Card deselected: " + gameObject.name);
            }
        }
    }
    
    public void SelectCard()
    {
        if (!isSelected && isInitialized)
        {
            isSelected = true;
            
            // Store current position as original if not set yet
            if (originalPosition == Vector3.zero)
            {
                originalPosition = transform.position;
            }
            
            // Animate card moving up with more visual feedback
            transform.DOKill(); // Cancel any ongoing animations
            transform.DOMove(new Vector3(transform.position.x, originalPosition.y + 0.5f, transform.position.z), 0.3f)
                .SetEase(Ease.OutBack); // Add a slight bounce effect
                
            // Add a glow effect or color change
            spriteRenderer.DOColor(new Color(1f, 1f, 0.5f), 0.3f);
            
            // Ensure the deck knows a card is selected
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateDiscardButtonState();
            }
        }
    }
    
    public void DeselectCard()
    {
        if (isSelected)
        {
            isSelected = false;
            
            // Cancel any ongoing animations
            transform.DOKill();
            
            // Animate card moving back down
            transform.DOMove(new Vector3(transform.position.x, originalPosition.y, transform.position.z), 0.3f)
                .SetEase(Ease.OutQuad);
                
            // Reset color
            spriteRenderer.DOColor(Color.white, 0.3f);
            
            // Update discard button state
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateDiscardButtonState();
            }
        }
    }
}