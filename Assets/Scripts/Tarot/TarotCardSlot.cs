using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Individual tarot card slot component for the tarot window.
/// Handles card display, interactions (single/double click), hover shake, and select move-up.
/// </summary>
public class TarotCardSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image cardImage;
    public Image materialBackground;
    public Button useButton;
    public GameObject selectionHighlight;
    
    [Header("Card Data")]
    public TarotCardData cardData;
    public int slotIndex;
    
    [Header("Tooltip Settings")]
    public float tooltipDelay = 0.5f; // Delay before showing tooltip on hover
    
    [Header("State")]
    private bool isSelected = false;
    private bool isHovered = false;
    private TarotWindowUI parentWindow;
    private RectTransform rectTransform;
    private int originalSiblingIndex; // Track original position in hierarchy
    private Coroutine tooltipCoroutine;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Find parent window
        if (parentWindow == null)
        {
            parentWindow = GetComponentInParent<TarotWindowUI>();
        }
        
        // Setup use button
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
            useButton.interactable = false; // Disabled by default
        }
    }
    
    private void Start()
    {
        // Capture original sibling index after layout has settled
        StartCoroutine(CaptureOriginalSiblingIndex());
    }
    
    /// <summary>
    /// Capture original sibling index after layout group has positioned the card
    /// </summary>
    private System.Collections.IEnumerator CaptureOriginalSiblingIndex()
    {
        // Wait for layout to complete
        yield return new WaitForEndOfFrame();
        yield return null;
        
        // Capture the original sibling index (position in hierarchy)
        originalSiblingIndex = transform.GetSiblingIndex();
    }
    
    /// <summary>
    /// Check if this slot is currently selected
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// Check if this slot is currently hovered
    /// </summary>
    public bool IsHovered()
    {
        return isHovered;
    }
    
    /// <summary>
    /// Set the original sibling index (called by parent window)
    /// </summary>
    public void SetOriginalSiblingIndex(int index)
    {
        originalSiblingIndex = index;
    }
    
    /// <summary>
    /// Get the original sibling index
    /// </summary>
    public int GetOriginalSiblingIndex()
    {
        return originalSiblingIndex;
    }
    
    /// <summary>
    /// Initialize the slot with card data
    /// </summary>
    public void Initialize(TarotCardData card, int index, TarotWindowUI window)
    {
        cardData = card;
        slotIndex = index;
        parentWindow = window;
        
        // Position will be captured in Start() after layout group positions it
        // Don't set position here - let layout group handle it
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update the visual display based on card data
    /// </summary>
    public void UpdateDisplay()
    {
        if (cardData == null)
        {
            ShowEmpty();
            // Hide the entire slot if empty (but keep it in hierarchy for grid positioning)
            if (cardImage != null) cardImage.gameObject.SetActive(false);
            if (materialBackground != null) materialBackground.gameObject.SetActive(false);
            if (useButton != null) useButton.gameObject.SetActive(false);
            if (selectionHighlight != null) selectionHighlight.SetActive(false);
            
            // Make slot non-interactive when empty
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f; // Invisible but maintains position
            canvasGroup.blocksRaycasts = false; // Don't block clicks on cards behind
            return;
        }
        
        // Make slot visible and interactive when it has a card
        CanvasGroup canvasGroup2 = GetComponent<CanvasGroup>();
        if (canvasGroup2 != null)
        {
            canvasGroup2.alpha = 1f;
            canvasGroup2.blocksRaycasts = true;
        }
        
        ShowCard(cardData);
    }
    
    private void ShowCard(TarotCardData card)
    {
        // Register card data for save/load sprite lookup
        if (card.cardImage != null)
        {
            InventoryManager.RegisterCardData(card);
        }
        
        // Update card image
        if (cardImage != null)
        {
            cardImage.gameObject.SetActive(true);
            if (card.cardImage != null)
            {
                cardImage.sprite = card.cardImage;
                cardImage.color = Color.white;
            }
            else
            {
                cardImage.sprite = null;
                cardImage.color = Color.gray;
            }
        }
        
        // Update material background
        if (materialBackground != null)
        {
            materialBackground.gameObject.SetActive(true);
            if (card.assignedMaterial != null)
            {
                Sprite materialSprite = card.GetMaterialBackgroundSprite();
                if (materialSprite != null)
                {
                    materialBackground.sprite = materialSprite;
                    materialBackground.color = Color.white;
                    materialBackground.type = Image.Type.Simple;
                    materialBackground.preserveAspect = false;
                }
                else
                {
                    materialBackground.sprite = null;
                    materialBackground.color = card.GetMaterialColor();
                }
            }
            else
            {
                materialBackground.sprite = null;
                materialBackground.color = Color.white;
            }
        }
        
        // Show use button (but keep it disabled until selected)
        if (useButton != null)
        {
            useButton.gameObject.SetActive(true);
            useButton.interactable = false; // Will be enabled when selected
        }
    }
    
    private void ShowEmpty()
    {
        if (cardImage != null) cardImage.gameObject.SetActive(false);
        if (materialBackground != null) materialBackground.gameObject.SetActive(false);
        if (useButton != null) useButton.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Handle pointer click - single click brings to front or deselects
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Don't handle clicks on empty slots
        if (cardData == null || !cardData.CanBeUsed()) return;
        
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Single click - bring to front or deselect
            if (parentWindow != null)
            {
                parentWindow.OnCardClicked(this);
            }
        }
    }
    
    /// <summary>
    /// Use button clicked - use the card
    /// </summary>
    private void OnUseButtonClicked()
    {
        if (cardData == null || !cardData.CanBeUsed()) return;
        
        // Find Deck to use the card
        Deck deck = FindObjectOfType<Deck>();
        if (deck == null)
        {
            Debug.LogWarning("[TarotCardSlot] Deck not found - cannot use card");
            return;
        }
        
        // Create a temporary TarotCard GameObject with all necessary components
        GameObject tempCardObj = new GameObject("TempTarotCard");
        RectTransform tempRect = tempCardObj.AddComponent<RectTransform>();
        tempCardObj.AddComponent<CanvasRenderer>();
        Image tempImage = tempCardObj.AddComponent<Image>();
        tempImage.sprite = cardData.cardImage;
        
        TarotCard tempCard = tempCardObj.AddComponent<TarotCard>();
        tempCard.cardData = cardData;
        tempCard.isInShop = false;
        tempCard.deck = deck;
        tempCard.cardImage = tempImage;
        
        // Use reflection to call the private TryUseCard method
        var tryUseCardMethod = typeof(TarotCard).GetMethod("TryUseCard", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (tryUseCardMethod != null)
        {
            tryUseCardMethod.Invoke(tempCard, null);
        }
        else
        {
            Debug.LogError("[TarotCardSlot] Could not find TryUseCard method");
        }
        
        // Refresh card data from inventory (durability may have changed)
        if (InventoryManagerV3.Instance != null && InventoryManagerV3.Instance.inventoryData != null)
        {
            var equipmentSlots = InventoryManagerV3.Instance.inventoryData.equipmentSlots;
            if (slotIndex < equipmentSlots.Count && equipmentSlots[slotIndex].isOccupied)
            {
                // Update to reference the authoritative card data from inventory
                cardData = equipmentSlots[slotIndex].storedCard;
            }
        }
        
        // Update display after use
        UpdateDisplay();
        
        // Notify parent window
        if (parentWindow != null)
        {
            parentWindow.OnCardUsed(this);
        }
        
        // Clean up temp object
        Destroy(tempCardObj);
    }
    
    /// <summary>
    /// Handle pointer enter - track hover and show tooltip
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Don't handle hover on empty slots
        if (cardData == null || !cardData.CanBeUsed()) return;
        
        isHovered = true;
        
        // Notify parent window
        if (parentWindow != null)
        {
            parentWindow.OnCardHovered(this);
        }
        
        // Start tooltip timer
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
        tooltipCoroutine = StartCoroutine(ShowTooltipDelayed());
    }
    
    /// <summary>
    /// Handle pointer exit - stop hover and hide tooltip
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        
        // Notify parent window
        if (parentWindow != null)
        {
            parentWindow.OnCardUnhovered(this);
        }
        
        // Stop tooltip timer and hide tooltip
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
    
    /// <summary>
    /// Set selection state - brings card to front (sibling index) or restores position
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Update selection highlight
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
        
        // Update use button state
        if (useButton != null)
        {
            useButton.interactable = selected && cardData != null && cardData.CanBeUsed();
            useButton.gameObject.SetActive(selected && cardData != null);
        }
        
        if (selected)
        {
            // Bring to front by moving to last sibling
            transform.SetAsLastSibling();
        }
        else
        {
            // Restore to original sibling index
            if (parentWindow != null)
            {
                parentWindow.RestoreSlotOrder(this);
            }
        }
    }
    
    /// <summary>
    /// Show tooltip after delay
    /// </summary>
    private System.Collections.IEnumerator ShowTooltipDelayed()
    {
        yield return new WaitForSeconds(tooltipDelay);
        
        if (cardData != null && isHovered && TooltipManager.Instance != null)
        {
            string tooltipText = $"<b>{cardData.cardName}</b>\n\n";
            tooltipText += $"<i>{cardData.description}</i>\n\n";
            tooltipText += $"Material: {cardData.GetMaterialDisplayName()}\n";
            
            int remainingUses = cardData.GetRemainingUses();
            if (remainingUses == -1)
            {
                tooltipText += "Uses: Unlimited";
            }
            else
            {
                tooltipText += $"Uses: {remainingUses}/{cardData.maxUses}";
            }
            
            TooltipManager.Instance.ShowTooltip(cardData.cardName, tooltipText, transform.position, true);
        }
    }
    
    private void OnDestroy()
    {
        // Clean up tooltip coroutine
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
        
        // Clean up button listener
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
        }
    }
}

