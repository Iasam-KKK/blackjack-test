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
    // public Image[] equipmentSlotImages; // OLD: 3 equipped card images - now using InventorySlotUIV3
    
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
    
    [Header("Slot Prefabs")]
    public GameObject slotPrefabV3; // For cards
    public GameObject materialSlotPrefab; // For materials (optional if using static slots)
    
    [Header("Materials Panel")]
    public Transform materialsContentContainer; // Where material slots will be created (optional)
    public MaterialSlotUIV3[] staticMaterialSlots; // Use this if you have pre-created material slots in hierarchy
    
    [Header("Settings")]
    public bool hideOnStart = true;
    
    private List<InventorySlotUIV3> storageSlots = new List<InventorySlotUIV3>();
    private List<InventorySlotUIV3> equipmentSlots = new List<InventorySlotUIV3>();
    private List<MaterialSlotUIV3> materialSlots = new List<MaterialSlotUIV3>();
    private MaterialSlotUIV3 selectedMaterialSlot;
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
        
        // Setup Materials tab buttons (different actions for materials)
        if (materialsEquipButton != null)
        {
            materialsEquipButton.onClick.RemoveAllListeners();
            materialsEquipButton.onClick.AddListener(OnMaterialEquipClicked);
        }
        
        if (materialsUnequipButton != null)
        {
            materialsUnequipButton.onClick.RemoveAllListeners();
            materialsUnequipButton.onClick.AddListener(OnMaterialUnequipClicked);
        }
        
        if (materialsDiscardButton != null)
        {
            materialsDiscardButton.onClick.RemoveAllListeners();
            // Discard not used for materials
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
        
        // Populate equipment slots
        PopulateEquipmentSlots();
        
        // Populate material slots
        PopulateMaterialSlots();
        
        // Initialize displays
        ClearCardDescription();
        UpdateOverviewStats();
        UpdateActionButtons();
        
        Debug.Log("InventoryPanelUIV3 initialized with " + storageSlots.Count + " storage slots, " + equipmentSlots.Count + " equipment slots, and " + materialSlots.Count + " material slots");
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
        if (InventoryManagerV3.Instance == null)
        {
            Debug.LogError("[InventoryPanelUIV3] PopulateStorageSlots - InventoryManagerV3.Instance is NULL!");
            return;
        }
        
        if (InventoryManagerV3.Instance.inventoryData == null)
        {
            Debug.LogError("[InventoryPanelUIV3] PopulateStorageSlots - InventoryManagerV3.Instance.inventoryData is NULL! Please assign InventoryData in Inspector!");
            return;
        }
        
        Debug.Log($"[InventoryPanelUIV3] PopulateStorageSlots - Successfully found inventoryData: {InventoryManagerV3.Instance.inventoryData.name}");
        
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
    
    public void PopulateEquipmentSlots()
    {
        if (equipmentSlotsContainer == null || slotPrefabV3 == null)
        {
            Debug.LogError("InventoryPanelUIV3: Missing equipment container or slot prefab!");
            return;
        }
        
        // Clear existing slots
        foreach (var slot in equipmentSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        equipmentSlots.Clear();
        
        // Get inventory data
        if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
        {
            Debug.LogError("InventoryPanelUIV3: InventoryManagerV3 or inventoryData not found!");
            return;
        }
        
        var inventoryData = InventoryManagerV3.Instance.inventoryData;
        
        // Create equipment slots (3 slots)
        for (int i = 0; i < inventoryData.equipmentSlotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefabV3, equipmentSlotsContainer);
            InventorySlotUIV3 slotUI = slotObj.GetComponent<InventorySlotUIV3>();
            
            if (slotUI != null)
            {
                var slotData = InventoryManagerV3.Instance.GetEquipmentSlot(i);
                slotUI.Initialize(i, slotData, this);
                equipmentSlots.Add(slotUI);
            }
            else
            {
                Debug.LogError($"InventoryPanelUIV3: Slot prefab missing InventorySlotUIV3 component!");
            }
        }
        
        Debug.Log($"InventoryPanelUIV3: Created {equipmentSlots.Count} equipment slots");
    }
    
    public void PopulateMaterialSlots()
    {
        // Method 1: Use static slots (if pre-created in hierarchy)
        if (staticMaterialSlots != null && staticMaterialSlots.Length > 0)
        {
            Debug.Log($"[InventoryPanelUIV3] Using {staticMaterialSlots.Length} static material slots");
            
            materialSlots.Clear();
            foreach (var slot in staticMaterialSlots)
            {
                if (slot != null && slot.materialData != null)
                {
                    slot.Initialize(slot.materialData, this);
                    materialSlots.Add(slot);
                    Debug.Log($"[InventoryPanelUIV3] Initialized static slot: {slot.materialData.materialName}");
                }
                else
                {
                    Debug.LogWarning($"[InventoryPanelUIV3] Static material slot is missing MaterialData! Assign it in Inspector.");
                }
            }
            
            Debug.Log($"[InventoryPanelUIV3] Initialized {materialSlots.Count} static material slots");
            return;
        }
        
        // Method 2: Dynamically create slots (fallback)
        if (materialsContentContainer == null || materialSlotPrefab == null)
        {
            Debug.LogWarning("InventoryPanelUIV3: No static material slots and missing dynamic slot setup - materials tab won't work");
            return;
        }
        
        // Clear existing material slots
        foreach (var slot in materialSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        materialSlots.Clear();
        
        // Get all available materials
        MaterialData[] allMaterials = MaterialManager.GetAllMaterials();
        
        if (allMaterials == null || allMaterials.Length == 0)
        {
            Debug.LogWarning("InventoryPanelUIV3: No materials found! Make sure MaterialData assets exist in Resources/Materials folder");
            return;
        }
        
        // Create a slot for each material
        foreach (MaterialData material in allMaterials)
        {
            GameObject slotObj = Instantiate(materialSlotPrefab, materialsContentContainer);
            MaterialSlotUIV3 slotUI = slotObj.GetComponent<MaterialSlotUIV3>();
            
            if (slotUI != null)
            {
                slotUI.Initialize(material, this);
                materialSlots.Add(slotUI);
            }
            else
            {
                Debug.LogError($"InventoryPanelUIV3: Material slot prefab missing MaterialSlotUIV3 component!");
            }
        }
        
        Debug.Log($"InventoryPanelUIV3: Created {materialSlots.Count} dynamic material slots");
    }
    
    public void OnMaterialSlotSelected(MaterialSlotUIV3 slot)
    {
        Debug.Log($"[InventoryPanelUIV3] Material selected: {slot.materialData.materialName}");
        
        // Clear previous selection
        if (selectedMaterialSlot != null && selectedMaterialSlot != slot)
        {
            selectedMaterialSlot.SetSelected(false);
        }
        
        selectedMaterialSlot = slot;
        selectedMaterialSlot.SetSelected(true);
        
        // Update material description panel
        UpdateSelectedMaterial(slot.materialData);
        
        // Update action buttons for materials tab
        UpdateMaterialActionButtons();
    }
    
    private void UpdateSelectedMaterial(MaterialData material)
    {
        // Update the card description panel to show material info
        if (selectedCardImage != null && material.backgroundSprite != null)
        {
            selectedCardImage.sprite = material.backgroundSprite;
            selectedCardImage.color = Color.white;
        }
        
        if (selectedCardDescriptionTxt != null)
        {
            selectedCardDescriptionTxt.text = $"Material: {material.materialName}\n\nUses: {(material.maxUses == -1 ? "Unlimited" : material.maxUses.ToString())}";
        }
        
        if (materialTxt != null)
        {
            materialTxt.text = material.materialName;
        }
        
        if (usesTxt != null)
        {
            usesTxt.text = material.maxUses == -1 ? "∞" : material.maxUses.ToString();
        }
        
        if (statusTxt != null)
        {
            statusTxt.text = "Available";
            statusTxt.color = Color.green;
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
        
        // Also refresh equipment slots
        RefreshEquipmentSlots();
    }
    
    public void RefreshEquipmentSlots()
    {
        if (InventoryManagerV3.Instance == null || InventoryManagerV3.Instance.inventoryData == null)
        {
            return;
        }
        
        // Refresh equipment slots using the same system as storage slots
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            var slotData = InventoryManagerV3.Instance.GetEquipmentSlot(i);
            equipmentSlots[i].slotData = slotData;
            equipmentSlots[i].UpdateDisplay();
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
            materialTxt.text = card.GetMaterialDisplayName();
        }
        
        // Update uses
        if (usesTxt != null)
        {
            int remainingUses = card.GetRemainingUses();
            if (remainingUses == -1)
            {
                usesTxt.text = "∞";
            }
            else
            {
                usesTxt.text = $"{remainingUses}/{card.maxUses}";
            }
        }
        
        // Update status
        if (statusTxt != null)
        {
            if (slotData.isEquipmentSlot)
            {
                statusTxt.text = "Equipped";
                statusTxt.color = Color.yellow;
            }
            else
            {
                statusTxt.text = "In Storage";
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
        
        // Reset info to placeholders
        if (materialTxt != null)
        {
            materialTxt.text = "???";
        }
        
        if (usesTxt != null)
        {
            usesTxt.text = "?/?";
        }
        
        if (statusTxt != null)
        {
            statusTxt.text = "???";
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
            storageTxt.text = $"{stats.storageUsed}/{stats.storageTotal}";
        }
        
        // Update equipment count
        if (equipmentTxt != null)
        {
            equipmentTxt.text = $"{stats.equipmentUsed}/{stats.equipmentTotal}";
        }
        
        // Update usable cards count
        if (usableCardsTxt != null)
        {
            usableCardsTxt.text = $"{stats.usableCards}";
        }
        
        // Update depleted cards count
        if (depletedCardsTxt != null)
        {
            depletedCardsTxt.text = $"{stats.unusableCards}";
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
    
    public void UpdateActionButtons()
    {
        if (InventoryManagerV3.Instance == null)
        {
            Debug.LogWarning("[InventoryPanelUIV3] UpdateActionButtons - InventoryManagerV3.Instance is NULL");
            return;
        }
        
        var currentTab = InventoryManagerV3.Instance.GetCurrentTab();
        
        if (currentTab == TabType.AllCards)
        {
            // Update buttons for All Cards tab (card equipping logic)
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
            
            Debug.Log($"[InventoryPanelUIV3] UpdateActionButtons (AllCards) - HasSelection: {hasSelection}, CanEquip: {canEquip}, CanUnequip: {canUnequip}");
            
            if (equipButton != null) equipButton.interactable = canEquip;
            if (unequipButton != null) unequipButton.interactable = canUnequip;
            if (discardButton != null) discardButton.interactable = canDiscard;
        }
        else
        {
            // Update buttons for Materials tab
            UpdateMaterialActionButtons();
        }
    }
    
    private void UpdateMaterialActionButtons()
    {
        // For materials tab: Equip applies material as background frame
        bool hasMaterialSelected = selectedMaterialSlot != null;
        bool hasCardSelected = InventoryManagerV3.Instance.GetSelectedSlot() != null &&
                              InventoryManagerV3.Instance.GetSelectedSlot().slotData != null &&
                              InventoryManagerV3.Instance.GetSelectedSlot().slotData.isOccupied;
        
        // Can equip if either:
        // 1. Material selected (applies to deck cards as frame)
        // 2. Material selected + card selected (applies to specific card)
        bool canEquip = hasMaterialSelected;
        
        Debug.Log($"[InventoryPanelUIV3] UpdateMaterialActionButtons - MaterialSelected: {hasMaterialSelected}, CardSelected: {hasCardSelected}, CanEquip: {canEquip}");
        
        if (materialsEquipButton != null)
        {
            materialsEquipButton.interactable = canEquip;
        }
        
        // Unequip and Discard not relevant for materials
        if (materialsUnequipButton != null)
        {
            materialsUnequipButton.interactable = false;
        }
        
        if (materialsDiscardButton != null)
        {
            materialsDiscardButton.interactable = false;
        }
    }
    
    private void OnEquipClicked()
    {
        Debug.Log("[InventoryPanelUIV3] OnEquipClicked called");
        
        if (InventoryManagerV3.Instance != null)
        {
            bool success = InventoryManagerV3.Instance.EquipSelectedCard();
            Debug.Log($"[InventoryPanelUIV3] Equip result: {success}");
            
            if (!success)
            {
                var selected = InventoryManagerV3.Instance.GetSelectedSlot();
                Debug.LogWarning($"[InventoryPanelUIV3] Equip failed - Selected slot: {(selected != null ? "Yes" : "No")}, " +
                               $"IsEquipmentSlot: {(selected?.slotData?.isEquipmentSlot)}, " +
                               $"HasEquipmentSpace: {InventoryManagerV3.Instance.HasEquipmentSpace()}");
            }
        }
        else
        {
            Debug.LogError("[InventoryPanelUIV3] OnEquipClicked - InventoryManagerV3.Instance is NULL!");
        }
    }
    
    private void OnUnequipClicked()
    {
        Debug.Log("[InventoryPanelUIV3] OnUnequipClicked called");
        
        if (InventoryManagerV3.Instance != null)
        {
            bool success = InventoryManagerV3.Instance.UnequipSelectedCard();
            Debug.Log($"[InventoryPanelUIV3] Unequip result: {success}");
            
            if (!success)
            {
                var selected = InventoryManagerV3.Instance.GetSelectedSlot();
                Debug.LogWarning($"[InventoryPanelUIV3] Unequip failed - Selected slot: {(selected != null ? "Yes" : "No")}, " +
                               $"IsEquipmentSlot: {(selected?.slotData?.isEquipmentSlot)}, " +
                               $"HasStorageSpace: {InventoryManagerV3.Instance.HasStorageSpace()}");
            }
        }
        else
        {
            Debug.LogError("[InventoryPanelUIV3] OnUnequipClicked - InventoryManagerV3.Instance is NULL!");
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
    
    private void OnMaterialEquipClicked()
    {
        Debug.Log("[InventoryPanelUIV3] OnMaterialEquipClicked called");
        
        if (selectedMaterialSlot == null || selectedMaterialSlot.materialData == null)
        {
            Debug.LogWarning("[InventoryPanelUIV3] No material selected!");
            return;
        }
        
        // Apply material as deck background frame
        if (DeckMaterialManager.Instance != null)
        {
            // Find the index of this material type
            int materialIndex = (int)selectedMaterialSlot.materialData.materialType;
            DeckMaterialManager.Instance.EquipMaterial(materialIndex);
            
            Debug.Log($"[InventoryPanelUIV3] Applied material '{selectedMaterialSlot.materialData.materialName}' as deck frame");
        }
        else
        {
            Debug.LogWarning("[InventoryPanelUIV3] DeckMaterialManager.Instance not found!");
        }
    }
    
    private void OnMaterialUnequipClicked()
    {
        Debug.Log("[InventoryPanelUIV3] OnMaterialUnequipClicked called");
        
        // Remove current material frame
        if (DeckMaterialManager.Instance != null)
        {
            DeckMaterialManager.Instance.EquipMaterial(-1); // -1 = no frame
            Debug.Log("[InventoryPanelUIV3] Removed material frame from deck");
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

