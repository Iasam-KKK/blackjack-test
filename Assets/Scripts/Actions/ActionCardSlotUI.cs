using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI component for an equipped action card slot in the ActionCardWindowUI.
/// Handles card display, hover effects, selection, and the Use button.
/// </summary>
public class ActionCardSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image cardImage;
    public Image cardBackground;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public Button useButton;
    public TextMeshProUGUI useButtonText;
    public GameObject selectionHighlight;
    public GameObject usedOverlay; // Shows when card has been used this hand
    
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 1f, 0.8f, 1f);
    public Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);
    public Color usedColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    public float hoverScale = 1.05f;
    public float selectedScale = 1.1f;
    
    [Header("Card Data")]
    public ActionCardData cardData;
    public int slotIndex;
    
    // State
    private bool isSelected = false;
    private bool isHovered = false;
    private bool hasBeenUsedThisHand = false;
    private ActionCardWindowUI parentWindow;
    private Deck deck;
    private int originalSiblingIndex; // Track original position for bring to front
    private Coroutine captureIndexCoroutine;
    
    private void Awake()
    {
        // Setup use button
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonClicked);
        }
        
        // Find Deck
        deck = FindObjectOfType<Deck>();
    }
    
    private void Start()
    {
        // Capture original sibling index after layout has settled
        captureIndexCoroutine = StartCoroutine(CaptureOriginalSiblingIndex());
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
    /// Check if this slot is currently selected
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// Initialize the slot with action card data
    /// </summary>
    public void Initialize(ActionCardData data, int index, ActionCardWindowUI window)
    {
        cardData = data;
        slotIndex = index;
        parentWindow = window;
        hasBeenUsedThisHand = false;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update the visual display
    /// </summary>
    public void UpdateDisplay()
    {
        if (cardData == null)
        {
            ShowEmpty();
            return;
        }
        
        ShowCard();
    }
    
    /// <summary>
    /// Show the action card
    /// </summary>
    private void ShowCard()
    {
        // Card image (icon or background)
        if (cardImage != null)
        {
            if (cardData.actionIcon != null)
            {
                cardImage.sprite = cardData.actionIcon;
                cardImage.color = normalColor;
                cardImage.gameObject.SetActive(true);
            }
            else if (cardData.cardBackground != null)
            {
                cardImage.sprite = cardData.cardBackground;
                cardImage.color = normalColor;
                cardImage.gameObject.SetActive(true);
            }
            else
            {
                cardImage.color = cardData.cardColor;
                cardImage.gameObject.SetActive(true);
            }
        }
        
        // Card background
        if (cardBackground != null)
        {
            if (cardData.cardBackground != null)
            {
                cardBackground.sprite = cardData.cardBackground;
                cardBackground.color = normalColor;
            }
            else
            {
                cardBackground.sprite = null;
                cardBackground.color = cardData.cardColor;
            }
            cardBackground.gameObject.SetActive(true);
        }
        
        // Card name
        if (cardNameText != null)
        {
            cardNameText.text = cardData.actionName;
            cardNameText.gameObject.SetActive(true);
        }
        
        // Description (if available)
        if (descriptionText != null)
        {
            descriptionText.text = cardData.actionDescription;
            descriptionText.gameObject.SetActive(true);
        }
        
        // Use button
        if (useButton != null)
        {
            useButton.gameObject.SetActive(true);
            UpdateUseButtonState();
        }
        
        // Used overlay - only show for per-hand limited cards, not game-wide limited cards
        if (usedOverlay != null)
        {
            // Don't show "used" overlay for game-wide limited cards (they track usage differently)
            if (cardData.hasLimitedGameUses)
            {
                usedOverlay.SetActive(false);
            }
            else
            {
                usedOverlay.SetActive(hasBeenUsedThisHand);
            }
        }
    }
    
    /// <summary>
    /// Show empty state
    /// </summary>
    private void ShowEmpty()
    {
        if (cardImage != null) cardImage.gameObject.SetActive(false);
        if (cardBackground != null) cardBackground.gameObject.SetActive(false);
        if (cardNameText != null) cardNameText.gameObject.SetActive(false);
        if (descriptionText != null) descriptionText.gameObject.SetActive(false);
        if (useButton != null) useButton.gameObject.SetActive(false);
        if (selectionHighlight != null) selectionHighlight.SetActive(false);
        if (usedOverlay != null) usedOverlay.SetActive(false);
    }
    
    /// <summary>
    /// Update the Use button state
    /// </summary>
    private void UpdateUseButtonState()
    {
        if (useButton == null) return;
        
        bool canUse = CanUseCard();
        useButton.interactable = canUse;
        
        // Update button text
        if (useButtonText != null)
        {
            // For game-wide limited cards, check the actual limit instead of hasBeenUsedThisHand
            if (cardData.hasLimitedGameUses)
            {
                if (cardData.actionType == ActionCardType.MinorHeal)
                {
                    if (ActionCardManager.Instance == null || !ActionCardManager.Instance.CanUseMinorHeal())
                    {
                        useButtonText.text = "No Uses Left";
                    }
                    else if (!canUse)
                    {
                        useButtonText.text = "Can't Use";
                    }
                    else
                    {
                        useButtonText.text = "Use";
                    }
                }
                else if (!canUse)
                {
                    useButtonText.text = "Can't Use";
                }
                else
                {
                    useButtonText.text = "Use";
                }
            }
            else if (hasBeenUsedThisHand && !cardData.canBeUsedMultipleTimes)
            {
                useButtonText.text = "Used";
            }
            else if (!canUse)
            {
                useButtonText.text = "Can't Use";
            }
            else
            {
                useButtonText.text = "Use";
            }
        }
        
        // Update button color
        Image buttonImage = useButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = canUse ? new Color(0.3f, 0.6f, 0.3f, 1f) : new Color(0.4f, 0.4f, 0.4f, 1f);
        }
    }
    
    /// <summary>
    /// Check if the card can be used
    /// </summary>
    private bool CanUseCard()
    {
        if (cardData == null) return false;
        
        // For cards with limited game uses (like Minor Heal), only check game-wide limit
        // Don't check per-hand usage for these cards
        if (cardData.hasLimitedGameUses)
        {
            // Check Minor Heal special case
            if (cardData.actionType == ActionCardType.MinorHeal)
            {
                if (ActionCardManager.Instance == null || !ActionCardManager.Instance.CanUseMinorHeal())
                {
                    return false;
                }
            }
            // Future: Add other limited-use cards here if needed
        }
        else
        {
            // For regular cards, check if already used this hand
            if (hasBeenUsedThisHand && !cardData.canBeUsedMultipleTimes)
            {
                return false;
            }
        }
        
        // Check if we have action points (if deck is available)
        if (deck != null)
        {
            if (deck.GetRemainingActions() < cardData.actionsRequired)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Handle Use button click
    /// </summary>
    private void OnUseButtonClicked()
    {
        if (!CanUseCard()) return;
        
        // Find Deck if not cached
        if (deck == null)
        {
            deck = FindObjectOfType<Deck>();
        }
        
        if (deck == null)
        {
            Debug.LogError("[ActionCardSlotUI] Deck not found!");
            return;
        }
        
        // Execute the action based on type
        bool success = ExecuteAction();
        
        if (success)
        {
            // Consume action points
            for (int i = 0; i < cardData.actionsRequired; i++)
            {
                deck.ConsumeAction();
            }
            
            // Mark as used this hand (if not reusable AND not a game-wide limited use card)
            // Cards with hasLimitedGameUses track usage globally, not per-hand
            if (!cardData.canBeUsedMultipleTimes && !cardData.hasLimitedGameUses)
            {
                hasBeenUsedThisHand = true;
            }
            
            // Visual feedback
            PlayUseAnimation();
            
            // Update display
            UpdateDisplay();
            
            // Notify parent window
            if (parentWindow != null)
            {
                parentWindow.OnCardUsed(this);
            }
            
            Debug.Log($"[ActionCardSlotUI] Used: {cardData.actionName}");
        }
    }
    
    /// <summary>
    /// Execute the action card's effect
    /// </summary>
    private bool ExecuteAction()
    {
        switch (cardData.actionType)
        {
            case ActionCardType.ValuePlusOne:
                return deck.ActionValuePlusOne();
            
            case ActionCardType.MinorSwapWithDealer:
                return deck.ActionMinorSwapWithDealer();
            
            case ActionCardType.ShieldCard:
                return deck.ActionShieldCard();
            
            case ActionCardType.MinorHeal:
                return deck.ActionMinorHeal();
            
            // Legacy action types
            case ActionCardType.SwapTwoCards:
                return deck.ActionSwapTwoCards();
            
            case ActionCardType.AddOneToCard:
                deck.ActionAddOneToCard();
                return true;
            
            case ActionCardType.SubtractOneFromCard:
                return deck.ActionSubtractOneFromCard();
            
            case ActionCardType.PeekDealerCard:
                return deck.ActionPeekDealerCard();
            
            case ActionCardType.ForceRedraw:
                return deck.ActionForceRedraw();
            
            case ActionCardType.DoubleCardValue:
                return deck.ActionDoubleCardValue();
            
            case ActionCardType.SetCardToTen:
                return deck.ActionSetCardToTen();
            
            case ActionCardType.FlipAce:
                return deck.ActionFlipAce();
            
            case ActionCardType.CopyCard:
                return deck.ActionCopyCard();
            
            default:
                Debug.LogWarning($"[ActionCardSlotUI] Unknown action type: {cardData.actionType}");
                return false;
        }
    }
    
    /// <summary>
    /// Play use animation
    /// </summary>
    private void PlayUseAnimation()
    {
        transform.DOKill();
        transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 10, 1f);
        
        if (cardImage != null)
        {
            cardImage.DOColor(Color.green, 0.2f).OnComplete(() => {
                cardImage.DOColor(normalColor, 0.2f);
            });
        }
    }
    
    /// <summary>
    /// Handle pointer click - toggle selection
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardData == null) return;
        
        if (parentWindow != null)
        {
            parentWindow.OnCardClicked(this);
        }
    }
    
    /// <summary>
    /// Handle pointer enter - hover effect
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardData == null) return;
        
        isHovered = true;
        
        // Scale up
        transform.DOKill();
        transform.DOScale(Vector3.one * hoverScale, 0.2f).SetEase(Ease.OutQuad);
        
        // Tint
        if (cardImage != null && !isSelected)
        {
            cardImage.DOColor(hoverColor, 0.2f);
        }
    }
    
    /// <summary>
    /// Handle pointer exit - remove hover effect
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        
        if (!isSelected)
        {
            // Reset scale
            transform.DOKill();
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutQuad);
            
            // Reset tint
            if (cardImage != null)
            {
                cardImage.DOColor(normalColor, 0.2f);
            }
        }
    }
    
    /// <summary>
    /// Set selection state - brings card to front or restores position
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Selection highlight
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(selected);
        }
        
        // Scale
        transform.DOKill();
        transform.DOScale(Vector3.one * (selected ? selectedScale : 1f), 0.2f).SetEase(Ease.OutQuad);
        
        // Tint
        if (cardImage != null)
        {
            cardImage.DOColor(selected ? selectedColor : normalColor, 0.2f);
        }
        
        // Bring to front / restore position
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
    /// Reset for new hand
    /// </summary>
    public void ResetForNewHand()
    {
        hasBeenUsedThisHand = false;
        UpdateDisplay();
    }
    
    private void OnDestroy()
    {
        // Kill DOTween animations
        transform.DOKill();
        if (cardImage != null) cardImage.DOKill();
        
        // Stop coroutines
        if (captureIndexCoroutine != null)
        {
            StopCoroutine(captureIndexCoroutine);
        }
        
        // Clean up button listener
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
        }
    }
}

