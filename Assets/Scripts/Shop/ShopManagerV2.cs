using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shop System V2 Manager
/// Handles tab switching, item spawning, details panel, and purchases
/// Coexists with the original shop system
/// </summary>
public class ShopManagerV2 : MonoBehaviour
{
    [Header("Tab System")]
    public GameObject tarotCardsTab;
    public GameObject actionCardsTab;
    public GameObject materialsTab;
    public CanvasGroup tarotTabButton;
    public CanvasGroup actionTabButton;
    public CanvasGroup materialTabButton;
    
    [Header("Content Areas")]
    public Transform tarotCardsContent; // Scroll view content
    public Transform actionCardsContent;
    public Transform materialsContent;
    
    [Header("Details Panel")]
    public GameObject detailsPanel;
    public Image detailsImage;
    public TextMeshProUGUI detailsName;
    public TextMeshProUGUI detailsPrice;
    public TextMeshProUGUI detailsDescription;
    public TextMeshProUGUI detailsMaterial;
    public TextMeshProUGUI detailsUses;
    
    [Header("Shop Settings")]
    public GameObject shopItemSlotPrefab;
    public List<TarotCardData> availableTarotCards = new List<TarotCardData>();
    public List<ActionCardData> availableActionCards = new List<ActionCardData>();
    
    [Header("References")]
    public Deck deck;
    public Button closeButton;
    public GameObject shopPanelV2; // The entire shop panel
    
    [Header("Tab Button References")]
    public Button tarotTabButtonClick;
    public Button actionTabButtonClick;
    public Button materialTabButtonClick;
    
    // Internal state
    private ShopTabType currentTab = ShopTabType.TarotCards;
    private ShopItemSlotV2 currentlySelectedSlot = null;
    private List<ShopItemSlotV2> spawnedSlots = new List<ShopItemSlotV2>();
    
    private void Start()
    {
        // Setup button listeners
        if (tarotTabButtonClick != null)
        {
            tarotTabButtonClick.onClick.AddListener(() => SwitchTab(ShopTabType.TarotCards));
        }
        
        if (actionTabButtonClick != null)
        {
            actionTabButtonClick.onClick.AddListener(() => SwitchTab(ShopTabType.ActionCards));
        }
        
        if (materialTabButtonClick != null)
        {
            materialTabButtonClick.onClick.AddListener(() => SwitchTab(ShopTabType.Materials));
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseShop);
        }
        
        // Find deck if not assigned
        if (deck == null)
        {
            deck = FindObjectOfType<Deck>();
        }
        
        // Initialize with tarot cards tab
        SwitchTab(ShopTabType.TarotCards);
    }
    
    /// <summary>
    /// Switch to a different tab
    /// </summary>
    public void SwitchTab(ShopTabType tabType)
    {
        currentTab = tabType;
        
        // Update tab button alphas
        UpdateTabButtonAlphas();
        
        // Show/hide content areas
        if (tarotCardsTab != null) tarotCardsTab.SetActive(tabType == ShopTabType.TarotCards);
        if (actionCardsTab != null) actionCardsTab.SetActive(tabType == ShopTabType.ActionCards);
        if (materialsTab != null) materialsTab.SetActive(tabType == ShopTabType.Materials);
        
        // Clear current selection
        ClearSelection();
        
        // Clear details panel
        ClearDetailsPanel();
        
        // Clear existing items
        ClearSpawnedItems();
        
        // Spawn items for the selected tab
        switch (tabType)
        {
            case ShopTabType.TarotCards:
                SpawnTarotCards();
                break;
                
            case ShopTabType.ActionCards:
                SpawnActionCards();
                break;
                
            case ShopTabType.Materials:
                SpawnMaterials();
                break;
        }
        
        Debug.Log($"[ShopManagerV2] Switched to {tabType} tab");
    }
    
    /// <summary>
    /// Update tab button visual states
    /// </summary>
    private void UpdateTabButtonAlphas()
    {
        if (tarotTabButton != null)
        {
            tarotTabButton.alpha = currentTab == ShopTabType.TarotCards ? 1.0f : 0.5f;
        }
        
        if (actionTabButton != null)
        {
            actionTabButton.alpha = currentTab == ShopTabType.ActionCards ? 1.0f : 0.5f;
        }
        
        if (materialTabButton != null)
        {
            materialTabButton.alpha = currentTab == ShopTabType.Materials ? 1.0f : 0.5f;
        }
    }
    
    /// <summary>
    /// Spawn tarot cards following boss rules (from original ShopManager)
    /// </summary>
    private void SpawnTarotCards()
    {
        if (tarotCardsContent == null || shopItemSlotPrefab == null)
        {
            Debug.LogWarning("[ShopManagerV2] Cannot spawn tarot cards - missing references");
            return;
        }
        
        // Check if tarot cards are disabled by current encounter
        if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.isEncounterActive)
        {
            // Check minion encounter first
            if (GameProgressionManager.Instance.isMinion)
            {
                var minion = GameProgressionManager.Instance.currentMinion;
                if (minion != null && minion.disablesTarotCards)
                {
                    Debug.Log($"[ShopManagerV2] Tarot cards disabled for minion: {minion.minionName}");
                    return; // Don't spawn any tarot cards
                }
            }
            // Check boss encounter
            else
            {
                var boss = GameProgressionManager.Instance.currentBoss;
                if (boss != null && !boss.allowTarotCards)
                {
                    Debug.Log($"[ShopManagerV2] Tarot cards disabled for boss: {boss.bossName}");
                    return; // Don't spawn any tarot cards
                }
            }
        }
        
        // Create a shuffled copy of available cards
        List<TarotCardData> cardsToShow = new List<TarotCardData>(availableTarotCards);
        ShuffleList(cardsToShow);
        
        // Apply boss-specific card limits
        if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.isEncounterActive)
        {
            BossType currentBossType = GameProgressionManager.Instance.currentBossType;
            
            if (currentBossType == BossType.TheFortuneTeller)
            {
                cardsToShow = cardsToShow.GetRange(0, Mathf.Min(2, cardsToShow.Count));
                Debug.Log("[ShopManagerV2] The Fortune Teller: Showing 2 tarot cards");
            }
            else if (currentBossType == BossType.TheThief)
            {
                cardsToShow = cardsToShow.GetRange(0, Mathf.Min(3, cardsToShow.Count));
                Debug.Log("[ShopManagerV2] The Thief: Showing 3 tarot cards");
            }
        }
        
        // Spawn card slots
        foreach (TarotCardData cardData in cardsToShow)
        {
            if (cardData == null) continue;
            
            // Create a copy of the card data to avoid modifying the original
            TarotCardData cardDataCopy = Instantiate(cardData);
            
            // Assign random material
            MaterialData randomMaterial = MaterialManager.GetRandomMaterial();
            cardDataCopy.AssignMaterial(randomMaterial);
            
            // Spawn the slot
            GameObject slotObj = Instantiate(shopItemSlotPrefab, tarotCardsContent);
            ShopItemSlotV2 slot = slotObj.GetComponent<ShopItemSlotV2>();
            
            if (slot != null)
            {
                slot.Initialize(cardDataCopy, ShopItemType.TarotCard, this);
                spawnedSlots.Add(slot);
            }
            
            Debug.Log($"[ShopManagerV2] Spawned tarot card: {cardDataCopy.cardName} with material: {cardDataCopy.GetMaterialDisplayName()}");
        }
        
        Debug.Log($"[ShopManagerV2] Spawned {spawnedSlots.Count} tarot cards");
    }
    
    /// <summary>
    /// Spawn action cards
    /// </summary>
    private void SpawnActionCards()
    {
        if (actionCardsContent == null || shopItemSlotPrefab == null)
        {
            Debug.LogWarning("[ShopManagerV2] Cannot spawn action cards - missing references");
            return;
        }
        
        // For now, show all available action cards
        // TODO: Implement boss-specific rules for action cards if needed
        foreach (ActionCardData actionData in availableActionCards)
        {
            if (actionData == null) continue;
            
            GameObject slotObj = Instantiate(shopItemSlotPrefab, actionCardsContent);
            ShopItemSlotV2 slot = slotObj.GetComponent<ShopItemSlotV2>();
            
            if (slot != null)
            {
                slot.Initialize(actionData, ShopItemType.ActionCard, this);
                spawnedSlots.Add(slot);
            }
        }
        
        Debug.Log($"[ShopManagerV2] Spawned {spawnedSlots.Count} action cards");
    }
    
    /// <summary>
    /// Spawn materials (placeholder for now)
    /// </summary>
    private void SpawnMaterials()
    {
        if (materialsContent == null || shopItemSlotPrefab == null)
        {
            Debug.LogWarning("[ShopManagerV2] Cannot spawn materials - missing references");
            return;
        }
        
        // Get all available materials
        MaterialData[] allMaterials = MaterialManager.GetAllMaterials();
        
        if (allMaterials == null || allMaterials.Length == 0)
        {
            Debug.Log("[ShopManagerV2] No materials available to display");
            return;
        }
        
        // Spawn material slots (read-only for now)
        foreach (MaterialData materialData in allMaterials)
        {
            if (materialData == null) continue;
            
            GameObject slotObj = Instantiate(shopItemSlotPrefab, materialsContent);
            ShopItemSlotV2 slot = slotObj.GetComponent<ShopItemSlotV2>();
            
            if (slot != null)
            {
                slot.Initialize(materialData, ShopItemType.Material, this);
                spawnedSlots.Add(slot);
            }
        }
        
        Debug.Log($"[ShopManagerV2] Spawned {spawnedSlots.Count} materials (read-only)");
    }
    
    /// <summary>
    /// Clear all spawned item slots
    /// </summary>
    private void ClearSpawnedItems()
    {
        foreach (ShopItemSlotV2 slot in spawnedSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        
        spawnedSlots.Clear();
    }
    
    /// <summary>
    /// Handle item slot click
    /// </summary>
    public void OnItemClicked(ShopItemSlotV2 slot)
    {
        if (slot == null) return;
        
        // Deselect previous slot
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetSelected(false);
        }
        
        // Select new slot
        currentlySelectedSlot = slot;
        currentlySelectedSlot.SetSelected(true);
        
        // Update details panel
        UpdateDetailsPanel(slot.GetItemData(), slot.GetItemType());
        
        Debug.Log($"[ShopManagerV2] Item clicked: {slot.GetItemType()}");
    }
    
    /// <summary>
    /// Called when an item is purchased from a slot
    /// </summary>
    public void OnItemPurchased(ShopItemSlotV2 slot)
    {
        // If the purchased item was selected, clear the selection and details
        if (currentlySelectedSlot == slot)
        {
            currentlySelectedSlot = null;
            ClearDetailsPanel();
        }
        
        // Remove from spawned slots list
        spawnedSlots.Remove(slot);
        
        Debug.Log("[ShopManagerV2] Item purchased and slot removed");
    }
    
    /// <summary>
    /// Update the details panel with item information
    /// </summary>
    private void UpdateDetailsPanel(object itemData, ShopItemType itemType)
    {
        // Details panel should already be active by default
        // No need to explicitly activate it
        
        switch (itemType)
        {
            case ShopItemType.TarotCard:
                DisplayTarotDetails(itemData as TarotCardData);
                break;
                
            case ShopItemType.ActionCard:
                DisplayActionDetails(itemData as ActionCardData);
                break;
                
            case ShopItemType.Material:
                DisplayMaterialDetails(itemData as MaterialData);
                break;
        }
    }
    
    /// <summary>
    /// Display tarot card details
    /// </summary>
    private void DisplayTarotDetails(TarotCardData cardData)
    {
        if (cardData == null) return;
        
        // Set image
        if (detailsImage != null && cardData.cardImage != null)
        {
            detailsImage.sprite = cardData.cardImage;
            detailsImage.gameObject.SetActive(true);
        }
        
        // Set name
        if (detailsName != null)
        {
            detailsName.text = cardData.cardName;
        }
        
        // Set price (calculated from material)
        if (detailsPrice != null)
        {
            int calculatedPrice = GetTarotCardPrice(cardData);
            detailsPrice.text = calculatedPrice.ToString();
        }
        
        // Set description
        if (detailsDescription != null)
        {
            detailsDescription.text = cardData.description;
        }
        
        // Set material info (just the material name, no "Material:" prefix)
        if (detailsMaterial != null)
        {
            if (cardData.assignedMaterial != null)
            {
                detailsMaterial.text = cardData.GetMaterialDisplayName();
                detailsMaterial.gameObject.SetActive(true);
            }
            else
            {
                detailsMaterial.gameObject.SetActive(false);
            }
        }
        
        // Set uses info (format as current/max, no "Uses:" prefix)
        if (detailsUses != null)
        {
            if (cardData.maxUses == -1)
            {
                detailsUses.text = "∞";
            }
            else
            {
                int remaining = cardData.GetRemainingUses();
                detailsUses.text = $"{remaining}/{cardData.maxUses}";
            }
            detailsUses.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Display action card details
    /// </summary>
    private void DisplayActionDetails(ActionCardData actionData)
    {
        if (actionData == null) return;
        
        // Set image
        if (detailsImage != null && actionData.actionIcon != null)
        {
            detailsImage.sprite = actionData.actionIcon;
            detailsImage.gameObject.SetActive(true);
        }
        
        // Set name
        if (detailsName != null)
        {
            detailsName.text = actionData.actionName;
        }
        
        // Set price (TODO: Define action card pricing)
        if (detailsPrice != null)
        {
            detailsPrice.text = "100";
        }
        
        // Set description
        if (detailsDescription != null)
        {
            detailsDescription.text = actionData.actionDescription;
        }
        
        // Hide material info for action cards
        if (detailsMaterial != null)
        {
            detailsMaterial.gameObject.SetActive(false);
        }
        
        // Show action cost instead of uses (just the number, no prefix)
        if (detailsUses != null)
        {
            detailsUses.text = actionData.actionsRequired.ToString();
            detailsUses.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Display material details
    /// </summary>
    private void DisplayMaterialDetails(MaterialData materialData)
    {
        if (materialData == null) return;
        
        // Set image
        if (detailsImage != null && materialData.backgroundSprite != null)
        {
            detailsImage.sprite = materialData.backgroundSprite;
            detailsImage.gameObject.SetActive(true);
        }
        
        // Set name
        if (detailsName != null)
        {
            detailsName.text = materialData.materialName;
        }
        
        // Set price (materials are info-only for now)
        if (detailsPrice != null)
        {
            detailsPrice.text = "---";
        }
        
        // Set description (material info)
        if (detailsDescription != null)
        {
            string desc = $"Max Uses: {(materialData.maxUses == -1 ? "Unlimited" : materialData.maxUses.ToString())}\n";
            desc += $"Rarity: {materialData.rarityPercentage}%\n";
            desc += $"Type: {materialData.materialType}";
            detailsDescription.text = desc;
        }
        
        // Hide material and uses fields
        if (detailsMaterial != null)
        {
            detailsMaterial.gameObject.SetActive(false);
        }
        
        if (detailsUses != null)
        {
            detailsUses.gameObject.SetActive(false);
        }
    }
    
    
    /// <summary>
    /// Clear the details panel (but keep it active with default values)
    /// </summary>
    private void ClearDetailsPanel()
    {
        // Don't deactivate the panel - user wants it active by default with dummy values
        // The panel should remain visible with whatever default values are set in the editor
        
        // Optional: Could reset to default/placeholder values here if needed
        // For now, just clear the selection without hiding the panel
    }
    
    /// <summary>
    /// Clear current selection
    /// </summary>
    private void ClearSelection()
    {
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetSelected(false);
            currentlySelectedSlot = null;
        }
    }
    
    /// <summary>
    /// Close the shop
    /// </summary>
    public void CloseShop()
    {
        if (shopPanelV2 != null)
        {
            shopPanelV2.SetActive(false);
        }
        
        Debug.Log("[ShopManagerV2] Shop closed");
    }
    
    /// <summary>
    /// Open the shop
    /// </summary>
    public void OpenShop()
    {
        if (shopPanelV2 != null)
        {
            shopPanelV2.SetActive(true);
        }
        
        // Refresh the current tab
        SwitchTab(currentTab);
        
        Debug.Log("[ShopManagerV2] Shop opened");
    }
    
    /// <summary>
    /// Check if the shop is currently open
    /// </summary>
    public bool IsShopOpen()
    {
        return shopPanelV2 != null && shopPanelV2.activeSelf;
    }
    
    /// <summary>
    /// Toggle the shop open/closed
    /// </summary>
    public void ToggleShop()
    {
        if (IsShopOpen())
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
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
    /// Shuffle a list (Fisher-Yates algorithm)
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}

