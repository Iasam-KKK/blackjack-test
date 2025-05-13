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
         
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider2D>();
        }
    }
    
    private void Start()
    { 
        originalPosition = transform.position;
        isInitialized = true;
         
        if (spriteRenderer.sprite != null)
        {
            boxCollider.size = spriteRenderer.sprite.bounds.size;
        }
         
        boxCollider.enabled = true;
    }

    public void ToggleFace(bool showFace)
    {
        spriteRenderer.sprite = showFace ? cardFront : cardBack;
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
        return spriteRenderer.sprite == cardFront && 
               transform.parent != null &&
               transform.parent.GetComponent<CardHand>() != null &&
               !transform.parent.GetComponent<CardHand>().isDealer;
    }
        
    private void OnMouseDown()
    {
        if (!CanInteract()) return;
         
        if (Input.GetMouseButton(0))
        {
            if (isSelected)
            {
                DeselectCard();
                Debug.Log("LEFT-CLICK: Card deselected: " + gameObject.name);
            }
            else
            {
                SelectCard();
                Debug.Log("LEFT-CLICK: Card selected: " + gameObject.name);
            }
        } 
        else if (Input.GetMouseButton(1))
        {
            if (isSelected)
            {
                DeselectCard();
                Debug.Log("RIGHT-CLICK: Card deselected: " + gameObject.name);
            }
        }
    }
    
    public void SelectCard()
    {
        if (!isSelected && isInitialized)
        {
            isSelected = true;
             
            if (originalPosition == Vector3.zero)
            {
                originalPosition = transform.position;
            }
             
            transform.DOKill();  
            transform.DOMove(new Vector3(transform.position.x, originalPosition.y + 0.5f, transform.position.z), 0.3f)
                .SetEase(Ease.OutBack);  
            spriteRenderer.DOColor(new Color(1f, 1f, 0.5f), 0.3f);
             
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
             
            transform.DOKill();
             
            transform.DOMove(new Vector3(transform.position.x, originalPosition.y, transform.position.z), 0.3f)
                .SetEase(Ease.OutQuad);
                 
            spriteRenderer.DOColor(Color.white, 0.3f);
             
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.UpdateDiscardButtonState();
            }
        }
    }
}