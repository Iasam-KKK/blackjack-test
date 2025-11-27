using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Universal shop item slot for Shop System V2
/// Handles display and interaction for Tarot Cards, Action Cards, and Materials
/// </summary>
public class ShopItemSlotV2 : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public Image materialBackground; // Background image for material (behind card image)
    public Image itemImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemPriceText;
    public Button purchaseButton;
    public CanvasGroup canvasGroup;
    
    [Header("Selection Visual")]
    public Image selectionHighlight; // Optional highlight border/background
    public Color selectedColor = new Color(1f, 1f, 0.5f, 0.3f);
    public Color normalColor = new Color(1f, 1f, 1f, 0f);
    
    [Header("Item Data")]
    private object itemData; // Can be TarotCardData or ActionCardData
    private ShopItemType itemType;
    private ShopManagerV2 shopManager;
    private bool isSelected = false;
    private Tween hoverShakeTween;
    
    private void Awake()
    {
        // Ensure we have a canvas group for selection effects
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Setup purchase button listener
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }
    
    /// <summary>
    /// Initialize the slot with item data
    /// </summary>
    public void Initialize(object data, ShopItemType type, ShopManagerV2 manager)
    {
        itemData = data;
        itemType = type;
        shopManager = manager;
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Update the visual display based on item data
    /// </summary>
    private void UpdateDisplay()
    {
        if (itemData == null) return;
        
        switch (itemType)
        {
            case ShopItemType.TarotCard:
                DisplayTarotCard();
                break;
                
            case ShopItemType.ActionCard:
                DisplayActionCard();
                break;
                
            case ShopItemType.Material:
                DisplayMaterial();
                break;
        }
        
        // Update purchase button state
        UpdatePurchaseButtonState();
    }
    
    private void DisplayTarotCard()
    {
        TarotCardData tarotData = itemData as TarotCardData;
        if (tarotData == null) return;
        
        // Update material background (same logic as old TarotCard.cs)
        if (materialBackground != null)
        {
            if (tarotData.assignedMaterial != null)
            {
                // Prioritize background sprite over color tint
                Sprite materialSprite = tarotData.GetMaterialBackgroundSprite();
                if (materialSprite != null)
                {
                    // Use the material background image
                    materialBackground.sprite = materialSprite;
                    materialBackground.color = Color.white; // Use sprite at full opacity
                    materialBackground.type = Image.Type.Simple;
                    materialBackground.preserveAspect = false; // Stretch to fit card
                }
                else
                {
                    // Fallback to color tint if no sprite is available
                    materialBackground.sprite = null;
                    materialBackground.color = tarotData.GetMaterialColor();
                }
                materialBackground.gameObject.SetActive(true);
            }
            else
            {
                materialBackground.gameObject.SetActive(false);
            }
        }
        
        // Set card image (on top of material background)
        if (itemImage != null && tarotData.cardImage != null)
        {
            itemImage.sprite = tarotData.cardImage;
            itemImage.preserveAspect = true;
        }
        
        // Set name
        if (itemNameText != null)
        {
            itemNameText.text = tarotData.cardName;
        }
        
        // Set price (calculated from material)
        if (itemPriceText != null)
        {
            int calculatedPrice = GetTarotCardPrice(tarotData);
            itemPriceText.text = calculatedPrice.ToString();
        }
    }
    
    private void DisplayActionCard()
    {
        ActionCardData actionData = itemData as ActionCardData;
        if (actionData == null) return;
        
        // Hide material background for action cards
        if (materialBackground != null)
        {
            materialBackground.gameObject.SetActive(false);
        }
        
        // Set image
        if (itemImage != null && actionData.actionIcon != null)
        {
            itemImage.sprite = actionData.actionIcon;
            itemImage.preserveAspect = true;
        }
        
        // Set name
        if (itemNameText != null)
        {
            itemNameText.text = actionData.actionName;
        }
        
        // Set price (action cards might have different pricing)
        if (itemPriceText != null)
        {
            // TODO: Define action card pricing system
            itemPriceText.text = "100"; // Placeholder price
        }
    }
    
    private void DisplayMaterial()
    {
        MaterialData materialData = itemData as MaterialData;
        if (materialData == null) return;
        
        // For materials, show the material background as the main display
        if (materialBackground != null && materialData.backgroundSprite != null)
        {
            materialBackground.sprite = materialData.backgroundSprite;
            materialBackground.color = Color.white;
            materialBackground.gameObject.SetActive(true);
        }
        
        // Hide item image for materials (or use a generic material icon)
        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(false);
        }
        
        // Set name
        if (itemNameText != null)
        {
            itemNameText.text = materialData.materialName;
        }
        
        // Set price
        if (itemPriceText != null)
        {
            // Materials are info-only, no price
            itemPriceText.text = "---";
        }
    }
    
    /// <summary>
    /// Handle click on this slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopManager != null)
        {
            shopManager.OnItemClicked(this);
        }
    }
    
    /// <summary>
    /// Handle mouse enter - add hover shake effect
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Subtle shake effect on hover using DOTween
        if (hoverShakeTween != null && hoverShakeTween.IsActive())
        {
            hoverShakeTween.Kill();
        }
        
        // Only shake if slot has item data
        if (itemData != null)
        {
            // Subtle shake: small rotation shake, 0.3s duration, loops infinitely
            hoverShakeTween = transform.DOShakeRotation(0.3f, strength: 2f, vibrato: 5, randomness: 30f, fadeOut: true)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// Handle mouse exit - stop hover shake effect
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Stop hover shake animation
        if (hoverShakeTween != null && hoverShakeTween.IsActive())
        {
            hoverShakeTween.Kill();
            hoverShakeTween = null;
        }
        
        // Smoothly return to original rotation
        transform.DORotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// Set selection state and visual feedback
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // Update selection highlight if available
        if (selectionHighlight != null)
        {
            selectionHighlight.color = isSelected ? selectedColor : normalColor;
        }
        
        // Optional: Scale effect
        if (isSelected)
        {
            transform.localScale = Vector3.one * 1.05f;
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }
    
    /// <summary>
    /// Get the item data stored in this slot
    /// </summary>
    public object GetItemData()
    {
        return itemData;
    }
    
    /// <summary>
    /// Get the item type
    /// </summary>
    public ShopItemType GetItemType()
    {
        return itemType;
    }
    
    /// <summary>
    /// Check if this slot is currently selected
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// Calculate final price based on material type
    /// </summary>
    private int GetTarotCardPrice(TarotCardData cardData)
    {
        if (cardData == null || cardData.assignedMaterial == null)
        {
            return 50; // Default price if no material assigned
        }
        
        // Price based on material rarity
        switch (cardData.assignedMaterial.materialType)
        {
            case TarotMaterialType.Paper:
                return 5; // Cheapest - 0.5 health
            case TarotMaterialType.Cardboard:
                return 25; // 2.5 health
            case TarotMaterialType.Wood:
                return 50; // 5 health
            case TarotMaterialType.Copper:
                return 75; // 7.5 health
            case TarotMaterialType.Silver:
                return 100; // 10 health
            case TarotMaterialType.Gold:
                return 150; // 15 health
            case TarotMaterialType.Platinum:
                return 175; // 17.5 health
            case TarotMaterialType.Diamond:
                return 200; // 20 health - Most expensive (unlimited uses)
            default:
                return 50;
        }
    }
    
    /// <summary>
    /// Handle purchase button click
    /// </summary>
    private void OnPurchaseClicked()
    {
        if (itemData == null || shopManager == null)
        {
            Debug.LogWarning("[ShopItemSlotV2] Cannot purchase - missing data or manager reference");
            return;
        }
        
        // Attempt to purchase based on item type
        bool purchaseSuccessful = false;
        
        switch (itemType)
        {
            case ShopItemType.TarotCard:
                purchaseSuccessful = PurchaseTarotCard();
                break;
                
            case ShopItemType.ActionCard:
                purchaseSuccessful = PurchaseActionCard();
                break;
                
            case ShopItemType.Material:
                Debug.Log("[ShopItemSlotV2] Materials cannot be purchased (info only)");
                return;
        }
        
        // If purchase was successful, destroy this slot
        if (purchaseSuccessful)
        {
            // Notify shop manager to clear selection if this was selected
            if (isSelected)
            {
                shopManager.OnItemPurchased(this);
            }
            
            // Destroy the slot - grid layout will auto-arrange remaining items
            Destroy(gameObject);
            
            Debug.Log($"[ShopItemSlotV2] Slot destroyed after successful purchase");
        }
    }
    
    /// <summary>
    /// Purchase a tarot card
    /// </summary>
    private bool PurchaseTarotCard()
    {
        TarotCardData cardData = itemData as TarotCardData;
        if (cardData == null) return false;
        
        // Calculate costs based on material (every $10 = 1 health point)
        int price = GetTarotCardPrice(cardData);
        uint cost = (uint)price;
        float healthCost = cost / 10f;
        
        // Check if player has enough health via GameProgressionManager
        if (GameProgressionManager.Instance == null)
        {
            Debug.LogError("[ShopItemSlotV2] GameProgressionManager not found!");
            return false;
        }
        
        float currentHealth = GameProgressionManager.Instance.playerHealthPercentage;
        
        if (currentHealth >= healthCost)
        {
            // Deduct health using GameProgressionManager (this will update the UI automatically)
            GameProgressionManager.Instance.DamagePlayer(healthCost);
            
            // Notify deck of purchase
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.OnCardPurchased(cost);
            }
            
            // Check if inventory manager exists
            if (InventoryManagerV3.Instance == null && InventoryManager.Instance == null)
            {
                Debug.LogError("[ShopItemSlotV2] NO INVENTORY MANAGER FOUND! Card cannot be added!");
                GameProgressionManager.Instance.HealPlayer(healthCost);
                return false;
            }
            
            // Log inventory state before purchase
            if (InventoryManagerV3.Instance != null)
            {
                bool hasSpace = InventoryManagerV3.Instance.HasStorageSpace();
                Debug.Log($"[ShopItemSlotV2] InventoryManagerV3 exists. Has storage space: {hasSpace}");
            }
            
            // Add to inventory
            bool addedToInventory = false;
            if (InventoryManagerV3.Instance != null)
            {
                Debug.Log($"[ShopItemSlotV2] Attempting to add {cardData.cardName} to InventoryManagerV3");
                addedToInventory = InventoryManagerV3.Instance.AddPurchasedCard(cardData);
                Debug.Log($"[ShopItemSlotV2] AddPurchasedCard returned: {addedToInventory}");
            }
            else if (InventoryManager.Instance != null)
            {
                Debug.Log($"[ShopItemSlotV2] Attempting to add {cardData.cardName} to InventoryManager");
                addedToInventory = InventoryManager.Instance.AddPurchasedCard(cardData);
                Debug.Log($"[ShopItemSlotV2] AddPurchasedCard returned: {addedToInventory}");
            }
            
            if (addedToInventory)
            {
                // Add to PlayerStats for compatibility
                if (PlayerStats.instance != null && !PlayerStats.instance.ownedCards.Contains(cardData))
                {
                    PlayerStats.instance.ownedCards.Add(cardData);
                    Debug.Log($"[ShopItemSlotV2] Added {cardData.cardName} to PlayerStats");
                }
                
                // Force an immediate inventory UI refresh
                if (InventoryManagerV3.Instance != null)
                {
                    InventoryManagerV3.Instance.ForceCompleteUIRefresh();
                }
                
                Debug.Log($"[ShopItemSlotV2] ✅ Successfully purchased and added tarot card: {cardData.cardName} for {cost} BAL ({healthCost} health)");
                return true;
            }
            else
            {
                // Refund if inventory full - heal back the health
                GameProgressionManager.Instance.HealPlayer(healthCost);
                
                Debug.LogError($"[ShopItemSlotV2] ❌ FAILED to add {cardData.cardName} to inventory - inventory might be full or not initialized! Purchase refunded.");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"[ShopItemSlotV2] Not enough health. Have: {currentHealth:F0}, Need: {healthCost:F0}");
            return false;
        }
    }
    
    /// <summary>
    /// Purchase an action card
    /// </summary>
    private bool PurchaseActionCard()
    {
        ActionCardData actionData = itemData as ActionCardData;
        if (actionData == null) return false;
        
        // TODO: Implement action card purchase logic
        Debug.Log($"[ShopItemSlotV2] Action card purchase not yet implemented: {actionData.actionName}");
        return false;
    }
    
    /// <summary>
    /// Update purchase button affordability state
    /// </summary>
    private void UpdatePurchaseButtonState()
    {
        if (purchaseButton == null) return;
        
        bool canAfford = false;
        int price = 0;
        
        switch (itemType)
        {
            case ShopItemType.TarotCard:
                TarotCardData tarotData = itemData as TarotCardData;
                if (tarotData != null && GameProgressionManager.Instance != null)
                {
                    float currentHealth = GameProgressionManager.Instance.playerHealthPercentage;
                    price = GetTarotCardPrice(tarotData);
                    float healthCost = price / 10f; // Every $10 = 1 health point
                    canAfford = currentHealth >= healthCost;
                }
                break;
                
            case ShopItemType.ActionCard:
                // TODO: Implement action card affordability check
                price = 100; // Placeholder
                canAfford = true; // Placeholder
                break;
                
            case ShopItemType.Material:
                // Materials are info-only
                canAfford = false;
                price = 0;
                break;
        }
        
        purchaseButton.interactable = canAfford;
        
        // Update button text to show price instead of "Purchase"
        Text buttonText = purchaseButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            if (itemType == ShopItemType.Material)
            {
                buttonText.text = "Info Only";
            }
            else
            {
                buttonText.text = price.ToString();
            }
        }
        
        // Also try TextMeshProUGUI
        TextMeshProUGUI buttonTMP = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonTMP != null)
        {
            if (itemType == ShopItemType.Material)
            {
                buttonTMP.text = "Info Only";
            }
            else
            {
                buttonTMP.text = price.ToString();
            }
        }
    }
    
    private void OnDestroy()
    {
        // Kill any active tweens
        if (hoverShakeTween != null && hoverShakeTween.IsActive())
        {
            hoverShakeTween.Kill();
        }
        transform.DOKill();
    }
}

