using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPanelUIV3 : MonoBehaviour
{
    [Header("Main Panel")]
    public GameObject inventoryPanelV3;
    public TextMeshProUGUI inventoryTitleTxt; // Static
    public Button backCloseButton;
    
    [Header("Card Description Panel")]
    public GameObject cardDescriptionPanel;
    public TextMeshProUGUI cardDescriptionTitleTxt; // Static "Card Description"
    public Image selectedCardImage;
    public TextMeshProUGUI selectedCardDescriptionTxt; // Dynamic
    public GameObject cardInfoContainer;
    public TextMeshProUGUI materialTxt; // Dynamic - card material
    public TextMeshProUGUI usesTxt; // Dynamic - "Uses: X/Y"
    public TextMeshProUGUI statusTxt; // Dynamic - "Equipped" or "In Storage"
    
    [Header("Overview Panel")]
    public GameObject overviewPanel;
    public TextMeshProUGUI overviewTitleTxt; // Static "Overview"
    public TextMeshProUGUI storageTxt; // Dynamic - "Storage: X/16"
    public TextMeshProUGUI equipmentTxt; // Dynamic - "Equipment: X/3"
    public TextMeshProUGUI usableCardsTxt; // Dynamic count
    public TextMeshProUGUI depletedCardsTxt; // Dynamic count
    
    [Header("Tab Switcher")]
    public Button allCardsTabButton;
    public Button materialsTabButton;
    public CanvasGroup allCardsTabCanvasGroup;
    public CanvasGroup materialsTabCanvasGroup;
    public GameObject allCardsPanel; // The scroll view container
    public GameObject materialsPanel; // Reference for future implementation
    
    [Header("Storage Scroll View")]
    public ScrollRect storageScrollView;
    public Transform storageContentContainer;
    public GridLayoutGroup storageGridLayout;
    
    [Header("Equipment Slots")]
    public Transform equipmentSlotsContainer;
    public Image[] equipmentSlotImages; // 3 equipped card images
    
    [Header("Action Buttons - All Cards Tab")]
    public GameObject allCardsButtonsContainer;
    public Button equipButton;
    public Button unequipButton;
    public Button discardButton;
    
    [Header("Action Buttons - Materials Tab")]
    public GameObject materialsButtonsContainer;
    public Button materialsEquipButton;
    public Button materialsUnequipButton;
    public Button materialsDiscardButton;
    
    [Header("Slot Prefab")]
    public GameObject slotPrefabV3;
    
    [Header("Settings")]
    public bool hideOnStart = true;
    
    private List<InventorySlotUIV3> storageSlots = new List<InventorySlotUIV3>();
    private bool isVisible = false;
    private Sprite defaultCardSprite; // Store the default/placeholder sprite
    
    public void Initialize()
    {
        Debug.Log("InventoryPanelUIV3.Initialize() called");
        
        // Setup button listeners
        if (backCloseButton != null)
        {
            Debug.Log("Setting up back/close button listener");
            backCloseButton.onClick.RemoveAllListeners(); // Clear any existing listeners
            backCloseButton.onClick.AddListener(HideInventory);
        }
        else
        {
            Debug.LogError("InventoryPanelUIV3: backCloseButton is NULL! Please assign it in the Inspector.");
        }
        
        // Setup All Cards tab buttons
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(OnEquipClicked);
        }
        
        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
            unequipButton.onClick.AddListener(OnUnequipClicked);
        }
        
        if (discardButton != null)
        {
            discardButton.onClick.RemoveAllListeners();
            discardButton.onClick.AddListener(OnDiscardClicked);
        }
        
        // Setup Materials tab buttons (same actions)
        if (materialsEquipButton != null)
        {
            materialsEquipButton.onClick.RemoveAllListeners();
            materialsEquipButton.onClick.AddListener(OnEquipClicked);
        }
        
        if (materialsUnequipButton != null)
        {
            materialsUnequipButton.onClick.RemoveAllListeners();
            materialsUnequipButton.onClick.AddListener(OnUnequipClicked);
        }
        
        if (materialsDiscardButton != null)
        {
            materialsDiscardButton.onClick.RemoveAllListeners();
            materialsDiscardButton.onClick.AddListener(OnDiscardClicked);
        }
        
        if (allCardsTabButton != null)
        {
            allCardsTabButton.onClick.RemoveAllListeners();
            allCardsTabButton.onClick.AddListener(() => SwitchTab(TabType.AllCards));
        }
        
        if (materialsTabButton != null)
        {
            materialsTabButton.onClick.RemoveAllListeners();
            materialsTabButton.onClick.AddListener(() => SwitchTab(TabType.Materials));
        }
        
        // Initialize slots
        PopulateStorageSlots();
        
        // Initialize tab state
        SwitchTab(TabType.AllCards);
        
        // Store the default card sprite before clearing
        if (selectedCardImage != null)
        {
            defaultCardSprite = selectedCardImage.sprite;
            Debug.Log($"Stored default card sprite: {(defaultCardSprite != null ? defaultCardSprite.name : "none")}");
        }
        
        // Hide panel initially if set
        if (hideOnStart && inventoryPanelV3 != null)
        {
            inventoryPanelV3.SetActive(false);
            isVisible = false;
        }
        
        // Initialize displays
        ClearCardDescription();
        UpdateOverviewStats();
        UpdateActionButtons();
        
        Debug.Log("InventoryPanelUIV3 initialized with " + storageSlots.Count + " storage slots");
    }
    
    public void PopulateStorageSlots()
    {
        if (storageContentContainer == null || slotPrefabV3 == null)
        {
            Debug.LogError("InventoryPanelUIV3: Missing storage container or slot prefab!");
            return;
        }
        
        // Clear existing slots
        foreach (var slot in storageSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        storageSlots.Clear();
        
        // Get inventory data
        if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
        {
            Debug.LogError("InventoryPanelUIV3: InventoryManagerV3 or inventoryData not found!");
            return;
        }
        
        var inventoryData = InventoryManagerV3.Instance.inventoryData;
        
        // Create storage slots (16 slots)
        for (int i = 0; i < inventoryData.storageSlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefabV3, storageContentContainer);
            InventorySlotUIV3 slotUI = slotObj.GetComponent<InventorySlotUIV3>();
            
            if (slotUI != null)
            {
                var slotData = InventoryManagerV3.Instance.GetStorageSlot(i);
                slotUI.Initialize(i, slotData, this);
                storageSlots.Add(slotUI);
            }
            else
            {
                Debug.LogError($"InventoryPanelUIV3: Slot prefab missing InventorySlotUIV3 component!");
            }
        }
    }
    
    public void ShowInventory()
    {
        Debug.Log("ShowInventory() called - isVisible: " + isVisible);
        
        if (isVisible)
        {
            Debug.LogWarning("ShowInventory() called but inventory is already visible");
            return;
        }
        
        isVisible = true;
        
        if (inventoryPanelV3 != null)
        {
            Debug.Log("Setting inventoryPanelV3 to active");
            inventoryPanelV3.SetActive(true);
        }
        else
        {
            Debug.LogError("ShowInventory(): inventoryPanelV3 is NULL! Please assign it in the Inspector.");
        }
        
        // Refresh displays
        RefreshAllSlots();
        RefreshEquipmentSlots();
        UpdateOverviewStats();
        UpdateActionButtons();
    }
    
    public void HideInventory()
    {
        Debug.Log("HideInventory() called - isVisible: " + isVisible);
        
        if (!isVisible)
        {
            Debug.LogWarning("HideInventory() called but inventory is not visible");
            // Force hide anyway in case state is wrong
            if (inventoryPanelV3 != null)
            {
                inventoryPanelV3.SetActive(false);
            }
            return;
        }
        
        isVisible = false;
        
        if (inventoryPanelV3 != null)
        {
            Debug.Log("Setting inventoryPanelV3 to inactive");
            inventoryPanelV3.SetActive(false);
        }
        else
        {
            Debug.LogError("HideInventory(): inventoryPanelV3 is NULL!");
        }
        
        // Clear selection
        if (InventoryManagerV3.Instance != null)
        {
            InventoryManagerV3.Instance.ClearSelection();
        }
    }
    
    // Alternative method that can be called directly from Unity Button in Inspector
    public void CloseInventory()
    {
        Debug.Log("CloseInventory() called directly from button");
        isVisible = false;
        
        if (inventoryPanelV3 != null)
        {
            inventoryPanelV3.SetActive(false);
        }
        
        if (InventoryManagerV3.Instance != null)
        {
            InventoryManagerV3.Instance.ClearSelection();
        }
    }
    
    public void ToggleInventory()
    {
        if (isVisible)
        {
            HideInventory();
        }
        else
        {
            ShowInventory();
        }
    }
    
    public void RefreshAllSlots()
    {
        if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
        {
            return;
        }
        
        // Refresh storage slots
        for (int i = 0; i < storageSlots.Count; i++)
        {
            var slotData = InventoryManagerV3.Instance.GetStorageSlot(i);
            storageSlots[i].slotData = slotData;
            storageSlots[i].UpdateDisplay();
        }
    }
    
    public void RefreshEquipmentSlots()
    {
        if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
        {
            return;
        }
        
        if (equipmentSlotImages == null || equipmentSlotImages.Length < 3)
        {
            return;
        }
        
        var inventoryData = InventoryManagerV3.Instance.inventoryData;
        
        // Update each equipment slot image
        for (int i = 0; i < 3 && i < inventoryData.equipmentSlots.Count; i++)
        {
            var slot = inventoryData.equipmentSlots[i];
            var slotImage = equipmentSlotImages[i];
            
            if (slotImage != null)
            {
                if (slot.isOccupied && slot.storedCard != null && slot.storedCard.cardImage != null)
                {
                    slotImage.sprite = slot.storedCard.cardImage;
                    slotImage.color = Color.white;
                    slotImage.gameObject.SetActive(true);
                }
                else
                {
                    slotImage.sprite = null;
                    slotImage.color = new Color(1, 1, 1, 0.3f);
                    slotImage.gameObject.SetActive(true);
                }
            }
        }
    }
    
    public void UpdateSelectedCard(InventorySlotData slotData)
    {
        if (slotData == null || !slotData.isOccupied || slotData.storedCard == null)
        {
            ClearCardDescription();
            return;
        }
        
        var card = slotData.storedCard;
        
        // Update card image - keep enabled, just change sprite
        if (selectedCardImage != null)
        {
            if (card.cardImage != null)
            {
                selectedCardImage.sprite = card.cardImage;
                selectedCardImage.color = Color.white;
            }
            else
            {
                selectedCardImage.sprite = null;
                selectedCardImage.color = new Color(1, 1, 1, 0.3f); // Semi-transparent if no sprite
            }
        }
        
        // Update description text
        if (selectedCardDescriptionTxt != null)
        {
            selectedCardDescriptionTxt.text = card.description;
        }
        
        // Update material
        if (materialTxt != null)
        {
            materialTxt.text = "Material: " + card.GetMaterialDisplayName();
        }
        
        // Update uses
        if (usesTxt != null)
        {
            int remainingUses = card.GetRemainingUses();
            if (remainingUses == -1)
            {
                usesTxt.text = "Uses: Unlimited";
            }
            else
            {
                usesTxt.text = $"Uses: {remainingUses}/{card.maxUses}";
            }
        }
        
        // Update status
        if (statusTxt != null)
        {
            if (slotData.isEquipmentSlot)
            {
                statusTxt.text = "Status: Equipped";
                statusTxt.color = Color.yellow;
            }
            else
            {
                statusTxt.text = "Status: In Storage";
                statusTxt.color = Color.white;
            }
        }
        
        UpdateActionButtons();
    }
    
    public void ClearCardDescription()
    {
        // Restore default/placeholder sprite instead of clearing it
        if (selectedCardImage != null)
        {
            if (defaultCardSprite != null)
            {
                selectedCardImage.sprite = defaultCardSprite; // Restore placeholder sprite
                selectedCardImage.color = new Color(1, 1, 1, 0.3f); // Semi-transparent for placeholder
            }
            else
            {
                // Fallback if no default sprite was stored
                selectedCardImage.sprite = null;
                selectedCardImage.color = new Color(1, 1, 1, 0.1f);
            }
        }
        
        // Reset to placeholder text
        if (selectedCardDescriptionTxt != null)
        {
            selectedCardDescriptionTxt.text = "Select a card to view details";
        }
        
        // Reset info to placeholders (preserve ??? if that's what you set)
        if (materialTxt != null && !materialTxt.text.Contains("???"))
        {
            materialTxt.text = "Material: ???";
        }
        
        if (usesTxt != null && !usesTxt.text.Contains("???"))
        {
            usesTxt.text = "Uses: ???";
        }
        
        if (statusTxt != null && !statusTxt.text.Contains("???"))
        {
            statusTxt.text = "Status: ???";
        }
        
        UpdateActionButtons();
    }
    
    public void UpdateOverviewStats()
    {
        if (InventoryManagerV3.Instance == null)
        {
            return;
        }
        
        var stats = InventoryManagerV3.Instance.GetInventoryStats();
        
        // Update storage count
        if (storageTxt != null)
        {
            storageTxt.text = $"Storage: {stats.storageUsed}/{stats.storageTotal}";
        }
        
        // Update equipment count
        if (equipmentTxt != null)
        {
            equipmentTxt.text = $"Equipment: {stats.equipmentUsed}/{stats.equipmentTotal}";
        }
        
        // Update usable cards count
        if (usableCardsTxt != null)
        {
            usableCardsTxt.text = $"Usable Cards: {stats.usableCards}";
        }
        
        // Update depleted cards count
        if (depletedCardsTxt != null)
        {
            depletedCardsTxt.text = $"Depleted Cards: {stats.unusableCards}";
        }
    }
    
    public void SwitchTab(TabType tab)
    {
        if (InventoryManagerV3.Instance != null)
        {
            InventoryManagerV3.Instance.SetCurrentTab(tab);
        }
        
        if (tab == TabType.AllCards)
        {
            // All Cards tab is active
            if (allCardsTabCanvasGroup != null)
            {
                allCardsTabCanvasGroup.alpha = 1f;
            }
            
            if (materialsTabCanvasGroup != null)
            {
                materialsTabCanvasGroup.alpha = 0.5f;
            }
            
            if (allCardsPanel != null)
            {
                allCardsPanel.SetActive(true);
            }
            
            if (materialsPanel != null)
            {
                materialsPanel.SetActive(false);
            }
            
            // Show All Cards buttons, hide Materials buttons
            if (allCardsButtonsContainer != null)
            {
                allCardsButtonsContainer.SetActive(true);
            }
            
            if (materialsButtonsContainer != null)
            {
                materialsButtonsContainer.SetActive(false);
            }
        }
        else // Materials
        {
            // Materials tab is active
            if (allCardsTabCanvasGroup != null)
            {
                allCardsTabCanvasGroup.alpha = 0.5f;
            }
            
            if (materialsTabCanvasGroup != null)
            {
                materialsTabCanvasGroup.alpha = 1f;
            }
            
            if (allCardsPanel != null)
            {
                allCardsPanel.SetActive(false);
            }
            
            if (materialsPanel != null)
            {
                materialsPanel.SetActive(true);
            }
            
            // Hide All Cards buttons, show Materials buttons
            if (allCardsButtonsContainer != null)
            {
                allCardsButtonsContainer.SetActive(false);
            }
            
            if (materialsButtonsContainer != null)
            {
                materialsButtonsContainer.SetActive(true);
            }
        }
        
        // Update button states for the active tab
        UpdateActionButtons();
    }
    
    public void OnSlotSelected(InventorySlotUIV3 slot)
    {
        if (InventoryManagerV3.Instance != null)
        {
            InventoryManagerV3.Instance.SetSelectedSlot(slot);
        }
        
        UpdateActionButtons();
    }
    
    private void UpdateActionButtons()
    {
        if (InventoryManagerV3.Instance == null)
        {
            return;
        }
        
        var selectedSlot = InventoryManagerV3.Instance.GetSelectedSlot();
        bool hasSelection = selectedSlot != null && selectedSlot.slotData != null && selectedSlot.slotData.isOccupied;
        
        bool canEquip = hasSelection && 
                       !selectedSlot.slotData.isEquipmentSlot && 
                       selectedSlot.slotData.storedCard.CanBeUsed() &&
                       InventoryManagerV3.Instance.HasEquipmentSpace();
        
        bool canUnequip = hasSelection && 
                         selectedSlot.slotData.isEquipmentSlot &&
                         InventoryManagerV3.Instance.HasStorageSpace();
        
        bool canDiscard = hasSelection;
        
        // Update All Cards tab buttons
        if (equipButton != null)
        {
            equipButton.interactable = canEquip;
        }
        
        if (unequipButton != null)
        {
            unequipButton.interactable = canUnequip;
        }
        
        if (discardButton != null)
        {
            discardButton.interactable = canDiscard;
        }
        
        // Update Materials tab buttons (same logic for now)
        if (materialsEquipButton != null)
        {
            materialsEquipButton.interactable = canEquip;
        }
        
        if (materialsUnequipButton != null)
        {
            materialsUnequipButton.interactable = canUnequip;
        }
        
        if (materialsDiscardButton != null)
        {
            materialsDiscardButton.interactable = canDiscard;
        }
    }
    
    private void OnEquipClicked()
    {
        if (InventoryManagerV3.Instance != null)
        {
            bool success = InventoryManagerV3.Instance.EquipSelectedCard();
            if (success)
            {
                Debug.Log("Card equipped successfully");
            }
        }
    }
    
    private void OnUnequipClicked()
    {
        if (InventoryManagerV3.Instance != null)
        {
            bool success = InventoryManagerV3.Instance.UnequipSelectedCard();
            if (success)
            {
                Debug.Log("Card unequipped successfully");
            }
        }
    }
    
    private void OnDiscardClicked()
    {
        if (InventoryManagerV3.Instance != null)
        {
            // You might want to add a confirmation dialog here
            bool success = InventoryManagerV3.Instance.DiscardSelectedCard();
            if (success)
            {
                Debug.Log("Card discarded successfully");
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up button listeners
        if (backCloseButton != null)
        {
            backCloseButton.onClick.RemoveAllListeners();
        }
        
        // All Cards tab buttons
        if (equipButton != null)
        {
            equipButton.onClick.RemoveAllListeners();
        }
        
        if (unequipButton != null)
        {
            unequipButton.onClick.RemoveAllListeners();
        }
        
        if (discardButton != null)
        {
            discardButton.onClick.RemoveAllListeners();
        }
        
        // Materials tab buttons
        if (materialsEquipButton != null)
        {
            materialsEquipButton.onClick.RemoveAllListeners();
        }
        
        if (materialsUnequipButton != null)
        {
            materialsUnequipButton.onClick.RemoveAllListeners();
        }
        
        if (materialsDiscardButton != null)
        {
            materialsDiscardButton.onClick.RemoveAllListeners();
        }
        
        // Tab buttons
        if (allCardsTabButton != null)
        {
            allCardsTabButton.onClick.RemoveAllListeners();
        }
        
        if (materialsTabButton != null)
        {
            materialsTabButton.onClick.RemoveAllListeners();
        }
    }
}

