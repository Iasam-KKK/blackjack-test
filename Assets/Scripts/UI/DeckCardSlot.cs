using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;

/// <summary>
/// Represents a single card slot in the deck inspector grid
/// Displays the card image and handles interaction
/// Also supports action card equip functionality with embedded equip button
/// </summary>
public class DeckCardSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    public Image cardImage;                 // Image component to display the card
    public Text emptyText;                  // Text to show when slot is empty
    public Image borderImage;               // Optional border highlight
    public GameObject dealtOverlay;         // Overlay to show when card has been dealt
    
    [Header("Action Card Components")]
    public GameObject equippedIndicator;    // Visual indicator when action card is equipped (e.g. checkmark or badge)
    public Image selectionBorder;           // Border shown when slot is selected
    public Color selectedBorderColor = new Color(1f, 0.8f, 0f, 1f); // Gold color for selection
    public Color equippedIndicatorColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green for equipped
    
    [Header("Embedded Equip Button (Inside Prefab)")]
    [Tooltip("Button to equip/unequip action card - disabled by default, shown on click")]
    public Button equipButton;              // Equip button embedded in the prefab
    public TextMeshProUGUI equipButtonText; // Text on the equip button
    public string equipText = "Equip";      // Text when card is not equipped
    public string unequipText = "Unequip";  // Text when card is equipped
    
    [Header("Empty State")]
    public Sprite emptySlotSprite;          // Sprite to show when no card
    public Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Header("Dealt/Used State")]
    public Color dealtColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);  // Grayed out look for dealt cards
    public Color normalColor = Color.white;                        // Normal card color
    
    [Header("Hover Effects")]
    public float hoverScale = 1.1f;         // Scale on hover
    public float hoverDuration = 0.2f;      // Animation duration
    
    // Current card data
    private PlayerDeckCard currentCard;
    private ActionCardData currentActionCardData;  // Reference to actual ActionCardData for action cards
    private bool isEmpty = true;
    private bool isSelected = false;
    private bool isEquipped = false;
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
        
        // Setup equip button if present
        SetupEquipButton();
    }
    
    /// <summary>
    /// Setup the embedded equip button
    /// </summary>
    private void SetupEquipButton()
    {
        if (equipButton != null)
        {
            // Hide button by default
            equipButton.gameObject.SetActive(false);
            
            // Add click listener
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
    }
    
    /// <summary>
    /// Set this slot to display a specific card
    /// </summary>
    public void SetCard(PlayerDeckCard card, bool showDealtAsGrayed = true)
    {
        currentCard = card;
        isEmpty = false;
        currentActionCardData = null; // Clear action card data for regular cards
        
        if (cardImage != null)
        {
            if (card.cardSprite != null)
            {
                cardImage.sprite = card.cardSprite;
                // Apply dealt/disabled color only if showDealtAsGrayed is true and card is dealt
                if (showDealtAsGrayed && card.isDealt)
                {
                    cardImage.color = dealtColor;
                }
                else
                {
                    cardImage.color = normalColor;
                }
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
        
        // Optional: Also use overlay if assigned (can be used in addition to color change)
        if (dealtOverlay != null)
        {
            dealtOverlay.SetActive(showDealtAsGrayed && card.isDealt);
        }
        
        // Hide equipped indicator for regular cards
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(false);
        }
        
        // Hide equip button for regular cards
        HideEquipButton();
        
        // Reset selection state
        SetSelected(false);
    }
    
    /// <summary>
    /// Set this slot to display an action card with its data
    /// </summary>
    public void SetActionCard(PlayerDeckCard card, ActionCardData actionData)
    {
        currentCard = card;
        currentActionCardData = actionData;
        isEmpty = false;
        
        if (cardImage != null)
        {
            bool hasSprite = false;
            
            // Priority 1: Use actionIcon from ActionCardData
            if (actionData != null && actionData.actionIcon != null)
            {
                cardImage.sprite = actionData.actionIcon;
                cardImage.color = normalColor;
                hasSprite = true;
            }
            // Priority 2: Use cardBackground from ActionCardData
            else if (actionData != null && actionData.cardBackground != null)
            {
                cardImage.sprite = actionData.cardBackground;
                cardImage.color = normalColor;
                hasSprite = true;
            }
            // Priority 3: Use cardSprite from PlayerDeckCard
            else if (card != null && card.cardSprite != null)
            {
                cardImage.sprite = card.cardSprite;
                cardImage.color = normalColor;
                hasSprite = true;
            }
            
            // Fallback: No sprite available - show colored card with action name
            if (!hasSprite)
            {
                // Use cardColor from ActionCardData if available, otherwise use a default action card color
                Color actionColor = actionData?.cardColor ?? new Color(0.3f, 0.4f, 0.6f, 1f);
                cardImage.sprite = null; // Clear sprite to show solid color
                cardImage.color = actionColor;
            }
        }
        
        // Hide empty text - action cards are never "empty"
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }
        
        // Hide dealt overlay for action cards
        if (dealtOverlay != null)
        {
            dealtOverlay.SetActive(false);
        }
        
        // Check if this action card is equipped
        if (ActionCardManager.Instance != null && actionData != null)
        {
            SetEquippedState(ActionCardManager.Instance.IsCardEquipped(actionData));
        }
        
        // Hide equip button initially (will show on click/selection)
        HideEquipButton();
        
        // Reset selection
        isSelected = false;
    }
    
    /// <summary>
    /// Set this slot to empty state
    /// </summary>
    public void SetEmpty()
    {
        currentCard = null;
        currentActionCardData = null;
        isEmpty = true;
        isSelected = false;
        isEquipped = false;
        
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
        
        // Hide equipped indicator
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(false);
        }
        
        // Hide selection border
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
        }
        
        // Hide equip button
        HideEquipButton();
    }
    
    /// <summary>
    /// Get the current card in this slot
    /// </summary>
    public PlayerDeckCard GetCard()
    {
        return currentCard;
    }
    
    /// <summary>
    /// Get the current action card data (for action cards only)
    /// </summary>
    public ActionCardData GetCurrentActionCardData()
    {
        return currentActionCardData;
    }
    
    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return isEmpty;
    }
    
    /// <summary>
    /// Check if this slot contains an action card
    /// </summary>
    public bool IsActionCard()
    {
        return currentCard != null && currentCard.isActionCard;
    }
    
    /// <summary>
    /// Set the selected state of this slot
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(selected);
            if (selected)
            {
                selectionBorder.color = selectedBorderColor;
                selectionBorder.DOFade(1f, 0.2f);
            }
        }
        else if (borderImage != null)
        {
            // Use existing border image as selection indicator
            borderImage.color = selected ? selectedBorderColor : Color.white;
            borderImage.DOFade(selected ? 1f : 0f, 0.2f);
        }
        
        // Show/hide embedded equip button based on selection (only for action cards)
        UpdateEquipButtonVisibility();
    }
    
    /// <summary>
    /// Set the equipped state visual indicator
    /// </summary>
    public void SetEquippedState(bool equipped)
    {
        isEquipped = equipped;
        
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(equipped);
        }
        
        // Optional: Apply tint to show equipped state
        if (cardImage != null && currentActionCardData != null)
        {
            cardImage.color = equipped ? new Color(0.8f, 1f, 0.8f, 1f) : normalColor;
        }
        
        // Update equip button text
        UpdateEquipButtonText();
    }
    
    // ============ EMBEDDED EQUIP BUTTON METHODS ============
    
    /// <summary>
    /// Update visibility of embedded equip button
    /// </summary>
    private void UpdateEquipButtonVisibility()
    {
        if (equipButton == null) return;
        
        // Only show for action cards when selected
        bool shouldShow = isSelected && currentActionCardData != null;
        
        // Additional check: if not equipped, check if we can equip more
        if (shouldShow && !isEquipped && ActionCardManager.Instance != null)
        {
            shouldShow = ActionCardManager.Instance.CanEquipMore;
        }
        
        equipButton.gameObject.SetActive(shouldShow);
        
        if (shouldShow)
        {
            UpdateEquipButtonText();
            
            // Animate button appearance
            equipButton.transform.localScale = Vector3.zero;
            equipButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }
    }
    
    /// <summary>
    /// Update the equip button text based on equipped state
    /// </summary>
    private void UpdateEquipButtonText()
    {
        if (equipButtonText != null)
        {
            equipButtonText.text = isEquipped ? unequipText : equipText;
        }
        else if (equipButton != null)
        {
            // Fallback: try to find Text component
            Text buttonText = equipButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = isEquipped ? unequipText : equipText;
            }
        }
    }
    
    /// <summary>
    /// Handle equip button click
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (currentActionCardData == null)
        {
            Debug.LogWarning("[DeckCardSlot] No action card data to equip/unequip");
            return;
        }
        
        if (ActionCardManager.Instance == null)
        {
            Debug.LogError("[DeckCardSlot] ActionCardManager not found!");
            return;
        }
        
        bool success;
        
        if (isEquipped)
        {
            // Unequip the card
            success = ActionCardManager.Instance.UnequipCard(currentActionCardData);
            if (success)
            {
                Debug.Log($"[DeckCardSlot] Unequipped: {currentActionCardData.actionName}");
                SetEquippedState(false);
            }
        }
        else
        {
            // Equip the card
            success = ActionCardManager.Instance.EquipCard(currentActionCardData);
            if (success)
            {
                Debug.Log($"[DeckCardSlot] Equipped: {currentActionCardData.actionName}");
                SetEquippedState(true);
            }
        }
        
        if (success)
        {
            // Visual feedback - pulse animation on button
            equipButton.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1f);
            
            // Update button visibility (may need to hide if max equipped reached)
            UpdateEquipButtonVisibility();
            
            // Notify inspector panel to update UI
            if (inspectorPanel != null)
            {
                inspectorPanel.OnActionCardSlotSelected(this, currentActionCardData);
            }
        }
    }
    
    /// <summary>
    /// Hide the equip button (called when deselected or slot cleared)
    /// </summary>
    public void HideEquipButton()
    {
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(false);
        }
    }
    
    // ============ INTERACTION HANDLERS ============
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Scale up on hover
        transform.DOKill();
        transform.DOScale(Vector3.one * hoverScale, hoverDuration).SetEase(Ease.OutQuad);
        
        // Show border highlight (if not selected)
        if (borderImage != null && !isSelected)
        {
            borderImage.DOFade(1f, hoverDuration);
        }
        
        // If this is an action card, show its description
        if (currentCard != null && currentCard.isActionCard && inspectorPanel != null)
        {
            if (currentActionCardData != null)
            {
                string equippedStatus = isEquipped ? " [EQUIPPED]" : "";
                inspectorPanel.ShowActionCardDescription($"<b>{currentActionCardData.actionName}</b>{equippedStatus}\n{currentActionCardData.actionDescription}");
            }
            else
            {
                inspectorPanel.ShowActionCardDescription($"Action Card\n{currentCard.displayName}");
            }
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset scale
        transform.DOKill();
        transform.DOScale(Vector3.one, hoverDuration).SetEase(Ease.OutQuad);
        
        // Hide border highlight (if not selected)
        if (borderImage != null && !isSelected)
        {
            borderImage.DOFade(0f, hoverDuration);
        }
        
        // Only clear description if this slot is not selected
        if (inspectorPanel != null && !isSelected)
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
        
        // If action card, notify inspector panel for equip functionality
        if (currentCard.isActionCard && inspectorPanel != null)
        {
            // Notify panel that this action card was selected
            inspectorPanel.OnActionCardSlotSelected(this, currentActionCardData);
        }
    }
    
    private void OnDestroy()
    {
        transform.DOKill();
        if (borderImage != null) borderImage.DOKill();
    }
}
