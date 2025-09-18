using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FINAL comprehensive inventory setup script
/// Creates persistent data + full UI + all functionality
/// This replaces all other test scripts - use ONLY this one!
/// </summary>
public class FinalInventorySetup : MonoBehaviour
{
    [Header("Auto Setup")]
    public bool setupOnStart = true;
    
    [Header("Configuration")]
    public int storageSlots = 12;
    public int equipmentSlots = 3;
    
    [Header("Created Components (Auto-filled)")]
    public InventoryManager inventoryManager;
    public InventoryPanelUI inventoryPanelUI;
    public InventoryData inventoryData;
    public Button inventoryButton;
    public Button testAddCardButton;
    
    private Canvas parentCanvas;
    
    private void Start()
    {
        if (setupOnStart)
        {
            Debug.Log("🚀 FINAL Inventory Setup Starting...");
            CreateCompleteInventorySystem();
        }
    }
    
    [ContextMenu("Create Complete Inventory System")]
    public void CreateCompleteInventorySystem()
    {
        Debug.Log("=== CREATING COMPLETE INVENTORY SYSTEM ===");
        
        // Step 1: Create persistent inventory data
        CreatePersistentInventoryData();
        
        // Step 2: Setup inventory manager with persistence
        SetupInventoryManager();
        
        // Step 3: Create full inventory UI
        CreateFullInventoryUI();
        
        // Step 4: Create test buttons
        CreateTestButtons();
        
        // Step 5: Initialize UI after everything is set up
        InitializeUIAfterSetup();
        
        // Step 6: Final test
        TestEverything();
        
        Debug.Log("✅ COMPLETE INVENTORY SYSTEM READY!");
        Debug.Log("🎮 Controls: Press 'I' to toggle inventory, or click green INVENTORY button");
        Debug.Log("🧪 Testing: Click red ADD TEST CARD button to test functionality");
    }
    
    #region Simple PlayerPrefs Setup
    private void CreatePersistentInventoryData()
    {
        Debug.Log("📁 Creating simple PlayerPrefs inventory system...");
        
        // Create runtime inventory data (no ScriptableObject assets needed)
        inventoryData = ScriptableObject.CreateInstance<InventoryData>();
        inventoryData.name = "RuntimeInventoryData";
        inventoryData.storageSlotCount = storageSlots;
        inventoryData.equipmentSlotCount = equipmentSlots;
        inventoryData.InitializeSlots();
        
        Debug.Log("✅ Created runtime inventory data (PlayerPrefs persistence)");
    }
    
    private void SetupInventoryManager()
    {
        Debug.Log("🔧 Setting up inventory manager with PlayerPrefs...");
        
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            GameObject managerObj = new GameObject("InventoryManager");
            inventoryManager = managerObj.AddComponent<InventoryManager>();
            DontDestroyOnLoad(managerObj);
        }
        
        // Assign the runtime data and enable persistence
        inventoryManager.inventoryData = inventoryData;
        inventoryManager.enablePersistence = true;
        
        // Load from PlayerPrefs immediately
        LoadInventoryFromPlayerPrefs();
        
        // Override the InventoryManager's save method to use our PlayerPrefs system
        OverrideInventoryManagerSaving();
        
        // Fix the shopManager reference for tarot panel sync
        FixShopManagerReference();
        
        Debug.Log("✅ InventoryManager configured with PlayerPrefs persistence");
    }
    
    // PlayerPrefs persistence methods
    private void LoadInventoryFromPlayerPrefs()
    {
        const string INVENTORY_SAVE_KEY = "FinalInventoryData_v1";
        
        try
        {
            string jsonData = PlayerPrefs.GetString(INVENTORY_SAVE_KEY, "");
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("📭 No saved inventory data found in PlayerPrefs");
                return;
            }
            
            Debug.Log($"📥 Loading inventory data: {jsonData.Substring(0, System.Math.Min(100, jsonData.Length))}...");
            
            FinalInventorySaveData saveData = JsonUtility.FromJson<FinalInventorySaveData>(jsonData);
            
            Debug.Log($"📊 Parsed save data: {saveData.storageCards.Count} storage cards, {saveData.equippedCards.Count} equipped cards");
            
            // Clear existing slots first
            foreach (var slot in inventoryData.storageSlots)
            {
                slot.RemoveCard();
            }
            foreach (var slot in inventoryData.equipmentSlots)
            {
                slot.RemoveCard();
            }
            
            // Load storage cards
            foreach (var cardSave in saveData.storageCards)
            {
                Debug.Log($"🔄 Loading storage card: {cardSave.cardName} (Type: {cardSave.cardType}, Uses: {cardSave.currentUses}/{cardSave.maxUses}, Slot: {cardSave.slotIndex})");
                
                TarotCardData card = CreateCardFromSaveData(cardSave);
                if (card != null && cardSave.slotIndex < inventoryData.storageSlots.Count)
                {
                    inventoryData.storageSlots[cardSave.slotIndex].StoreCard(card);
                    Debug.Log($"✅ Successfully loaded {card.cardName} into storage slot {cardSave.slotIndex}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to load card {cardSave.cardName} - card is null or invalid slot index {cardSave.slotIndex}");
                }
            }
            
            // Load equipped cards
            foreach (var cardSave in saveData.equippedCards)
            {
                Debug.Log($"🔄 Loading equipped card: {cardSave.cardName} (Type: {cardSave.cardType}, Uses: {cardSave.currentUses}/{cardSave.maxUses}, Slot: {cardSave.slotIndex})");
                
                TarotCardData card = CreateCardFromSaveData(cardSave);
                if (card != null && cardSave.slotIndex < inventoryData.equipmentSlots.Count)
                {
                    inventoryData.equipmentSlots[cardSave.slotIndex].StoreCard(card);
                    Debug.Log($"✅ Successfully loaded {card.cardName} into equipment slot {cardSave.slotIndex}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to load card {cardSave.cardName} - card is null or invalid slot index {cardSave.slotIndex}");
                }
            }
            
            Debug.Log($"✅ Inventory loaded from PlayerPrefs: {saveData.storageCards.Count} storage, {saveData.equippedCards.Count} equipped");
            
            // Force a UI refresh after loading cards to ensure images display
            StartCoroutine(ForceUIRefreshAfterLoad());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load inventory: {e.Message}");
        }
    }
    
    private TarotCardData CreateCardFromSaveData(CardSaveData saveData)
    {
        // Create TarotCardData from save data
        TarotCardData card = saveData.ToTarotCardData();
        
        // Method 1: Try to find original card data from Resources folder first
        TarotCardData originalCard = FindOriginalCardDataFromResources(saveData.cardType);
        if (originalCard != null)
        {
            card.cardImage = originalCard.cardImage;
            card.description = originalCard.description;
            card.price = originalCard.price;
            
            // DON'T copy material background - we need to use the saved material type
            Debug.Log($"✅ Enhanced card with Resources sprite: {saveData.cardName}");
        }
        else
        {
            // Method 2: Try to find existing card sprite from scene cards
            TarotCard[] existingCards = FindObjectsOfType<TarotCard>();
            foreach (var existingCard in existingCards)
            {
                if (existingCard.cardData != null && existingCard.cardData.cardType == saveData.cardType)
                {
                    card.cardImage = existingCard.cardData.cardImage;
                    card.description = existingCard.cardData.description;
                    card.price = existingCard.cardData.price;
                    
                    // DON'T copy material background - we need to use the saved material type
                    Debug.Log($"✅ Enhanced card with scene sprite: {saveData.cardName}");
                    break;
                }
            }
        }
        
        // If no sprite found, use placeholder
        if (card.cardImage == null)
        {
            card.cardImage = CreatePlaceholderSprite();
            card.description = "Inventory card";
            card.price = 100;
            Debug.Log($"⚠️ Using placeholder sprite for: {saveData.cardName}");
        }
        
        // Always use correct material based on saved material type
        if (card.assignedMaterial != null)
        {
            // Try to load the actual MaterialData from Resources first
            MaterialData originalMaterial = LoadMaterialFromResources(saveData.materialType);
            if (originalMaterial != null && originalMaterial.backgroundSprite != null)
            {
                card.assignedMaterial.backgroundSprite = originalMaterial.backgroundSprite;
                Debug.Log($"🎨 Loaded original material background for {saveData.cardName}: {saveData.materialType}");
            }
            else
            {
                // Fallback to creating a colored sprite
                card.assignedMaterial.backgroundSprite = CreateMaterialBackgroundSprite(saveData.materialType);
                Debug.Log($"🎨 Created fallback material background for {saveData.cardName}: {saveData.materialType}");
            }
        }
        
        Debug.Log($"✅ Recreated card: {saveData.cardName} ({saveData.materialType}, {saveData.currentUses}/{saveData.maxUses} uses)");
        return card;
    }
    
    
    private TarotCardData FindOriginalCardDataFromResources(TarotCardType cardType)
    {
        // Try to load from Resources folder first
        TarotCardData[] allCards = Resources.LoadAll<TarotCardData>("");
        foreach (var card in allCards)
        {
            if (card.cardType == cardType)
            {
                Debug.Log($"✅ Found original card data for {cardType} from Resources root");
                return card;
            }
        }
        
        // Try from Materials subfolder in Resources
        TarotCardData[] materialCards = Resources.LoadAll<TarotCardData>("Materials");
        foreach (var card in materialCards)
        {
            if (card.cardType == cardType)
            {
                Debug.Log($"✅ Found original card data for {cardType} from Resources/Materials");
                return card;
            }
        }
        
        // For now, since ScriptableObjects are not in Resources, we'll use UnityEditor to load them
        // This is a workaround - ideally these should be moved to Resources folder
        #if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TarotCardData", new[] {"Assets/ScriptableObject"});
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            TarotCardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<TarotCardData>(path);
            if (card != null && card.cardType == cardType)
            {
                Debug.Log($"✅ Found original card data for {cardType} from ScriptableObject folder: {path}");
                return card;
            }
        }
        #endif
        
        Debug.Log($"⚠️ Could not find original card data for {cardType} anywhere");
        return null;
    }

    private TarotCardData FindOriginalCardData(TarotCardType cardType)
    {
        // Method 1: Try Resources first
        TarotCardData resourceCard = FindOriginalCardDataFromResources(cardType);
        if (resourceCard != null) return resourceCard;
        
        // Method 2: Try to find from existing cards in the scene
        TarotCard[] existingCards = FindObjectsOfType<TarotCard>();
        foreach (var card in existingCards)
        {
            if (card.cardData != null && card.cardData.cardType == cardType)
            {
                Debug.Log($"Found original card data for {cardType} from scene");
                return card.cardData;
            }
        }
        
        Debug.Log($"Could not find original card data for {cardType}");
        return null;
    }
    
    private MaterialData LoadMaterialFromResources(TarotMaterialType materialType)
    {
        // Try to load the specific material from Resources/Materials folder
        string materialName = materialType.ToString();
        MaterialData material = Resources.Load<MaterialData>($"Materials/{materialName}");
        
        if (material != null)
        {
            Debug.Log($"✅ Loaded MaterialData from Resources: {materialName}");
            return material;
        }
        
        // Try alternative naming (CardBoard vs Cardboard)
        if (materialType == TarotMaterialType.Cardboard)
        {
            material = Resources.Load<MaterialData>("Materials/CardBoard");
            if (material != null)
            {
                Debug.Log($"✅ Loaded MaterialData from Resources: CardBoard (alt naming)");
                return material;
            }
        }
        
        Debug.Log($"⚠️ Could not load MaterialData for {materialType} from Resources");
        return null;
    }

    private TarotCardData GetRandomCardFromAssets()
    {
        // Try to get all available cards from Resources
        TarotCardData[] allCards = Resources.LoadAll<TarotCardData>("");
        
        #if UNITY_EDITOR
        // Also try to get cards from ScriptableObject folder
        System.Collections.Generic.List<TarotCardData> cardList = new System.Collections.Generic.List<TarotCardData>(allCards);
        
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TarotCardData", new[] {"Assets/ScriptableObject"});
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            TarotCardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<TarotCardData>(path);
            if (card != null)
            {
                cardList.Add(card);
            }
        }
        
        if (cardList.Count > 0)
        {
            TarotCardData randomCard = cardList[Random.Range(0, cardList.Count)];
            Debug.Log($"✅ Selected random card from assets: {randomCard.name} (Type: {randomCard.cardType})");
            return randomCard;
        }
        #else
        if (allCards.Length > 0)
        {
            TarotCardData randomCard = allCards[Random.Range(0, allCards.Length)];
            Debug.Log($"✅ Selected random card from Resources: {randomCard.name} (Type: {randomCard.cardType})");
            return randomCard;
        }
        #endif
        
        Debug.Log($"⚠️ No cards found in assets");
        return null;
    }
    
    private Sprite CreatePlaceholderSprite()
    {
        // Try to find an existing sprite in the game first
        Sprite existingSprite = TryGetExistingCardSprite();
        if (existingSprite != null)
        {
            Debug.Log("✅ Using existing card sprite for placeholder");
            return existingSprite;
        }
        
        // Create a simple 64x64 texture with a border as placeholder
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[64 * 64];
        
        // Create a simple card-like image with border
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                // Create border
                if (x < 2 || x > 61 || y < 2 || y > 61)
                {
                    pixels[y * 64 + x] = Color.black;
                }
                // Inner area - make it more visible
                else if (x > 10 && x < 54 && y > 10 && y < 54)
                {
                    pixels[y * 64 + x] = new Color(0.8f, 0.8f, 1f, 1f); // Light blue
                }
                else
                {
                    pixels[y * 64 + x] = new Color(0.9f, 0.9f, 0.9f, 1f); // Light gray
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = "PlaceholderCardSprite";
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sprite.name = "PlaceholderCardSprite";
        
        Debug.Log("✅ Created placeholder card sprite");
        return sprite;
    }
    
    private Sprite TryGetExistingCardSprite()
    {
        // Try to find any existing tarot card in the scene and use its sprite
        TarotCard existingCard = FindObjectOfType<TarotCard>();
        if (existingCard != null && existingCard.cardData != null && existingCard.cardData.cardImage != null)
        {
            return existingCard.cardData.cardImage;
        }
        
        // Try to get Unity's built-in sprite
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }
    
    private Sprite CreateMaterialBackgroundSprite(TarotMaterialType materialType)
    {
        // Create a colored background based on material type
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color materialColor = GetMaterialColor(materialType);
        
        Color[] pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = materialColor;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        texture.name = $"Material_{materialType}";
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        sprite.name = $"Material_{materialType}";
        
        Debug.Log($"✅ Created {materialType} material background sprite");
        return sprite;
    }
    
    private Color GetMaterialColor(TarotMaterialType materialType)
    {
        switch (materialType)
        {
            case TarotMaterialType.Paper: return new Color(0.9f, 0.9f, 0.8f, 0.8f); // Light beige
            case TarotMaterialType.Cardboard: return new Color(0.7f, 0.6f, 0.4f, 0.8f); // Brown
            case TarotMaterialType.Wood: return new Color(0.6f, 0.4f, 0.2f, 0.8f); // Dark brown
            case TarotMaterialType.Copper: return new Color(0.8f, 0.5f, 0.2f, 0.8f); // Copper color
            case TarotMaterialType.Silver: return new Color(0.8f, 0.8f, 0.9f, 0.8f); // Silver
            case TarotMaterialType.Gold: return new Color(1f, 0.8f, 0.2f, 0.8f); // Gold
            case TarotMaterialType.Platinum: return new Color(0.9f, 0.9f, 1f, 0.8f); // Platinum
            case TarotMaterialType.Diamond: return new Color(0.9f, 0.9f, 1f, 1f); // Bright white/diamond
            default: return Color.white;
        }
    }
    
    public void SaveInventoryToPlayerPrefs()
    {
        const string INVENTORY_SAVE_KEY = "FinalInventoryData_v1";
        
        if (inventoryData == null) return;
        
        try
        {
            FinalInventorySaveData saveData = new FinalInventorySaveData();
            
            // Save storage slots
            foreach (var slot in inventoryData.storageSlots)
            {
                if (slot.isOccupied && slot.storedCard != null)
                {
                    saveData.storageCards.Add(CardSaveData.FromTarotCardData(slot.storedCard, slot.slotIndex));
                }
            }
            
            // Save equipment slots
            foreach (var slot in inventoryData.equipmentSlots)
            {
                if (slot.isOccupied && slot.storedCard != null)
                {
                    saveData.equippedCards.Add(CardSaveData.FromTarotCardData(slot.storedCard, slot.slotIndex));
                }
            }
            
            string jsonData = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(INVENTORY_SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            
            Debug.Log($"💾 Saved inventory to PlayerPrefs: {saveData.storageCards.Count} storage, {saveData.equippedCards.Count} equipped");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to save inventory: {e.Message}");
        }
    }
    
    // Save data structures
    [System.Serializable]
    public class CardSaveData
    {
        public string cardName;
        public TarotCardType cardType;
        public int currentUses;
        public int maxUses;
        public string materialName;
        public TarotMaterialType materialType;
        public int slotIndex;
        
        // Convert from TarotCardData
        public static CardSaveData FromTarotCardData(TarotCardData card, int slot = -1)
        {
            return new CardSaveData
            {
                cardName = card.cardName,
                cardType = card.cardType,
                currentUses = card.currentUses,
                maxUses = card.maxUses,
                materialName = card.assignedMaterial?.materialName ?? "",
                materialType = card.assignedMaterial?.materialType ?? TarotMaterialType.Paper,
                slotIndex = slot
            };
        }
        
        // Convert back to TarotCardData
        public TarotCardData ToTarotCardData()
        {
            // Create runtime TarotCardData
            TarotCardData card = ScriptableObject.CreateInstance<TarotCardData>();
            card.cardName = cardName;
            card.cardType = cardType;
            card.currentUses = currentUses;
            card.maxUses = maxUses;
            
            // Create runtime MaterialData
            MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
            material.materialName = materialName;
            material.materialType = materialType;
            material.maxUses = maxUses;
            
            card.AssignMaterial(material);
            return card;
        }
    }
    
    [System.Serializable]
    public class FinalInventorySaveData
    {
        public System.Collections.Generic.List<CardSaveData> storageCards = new System.Collections.Generic.List<CardSaveData>();
        public System.Collections.Generic.List<CardSaveData> equippedCards = new System.Collections.Generic.List<CardSaveData>();
    }
    
    private void ForceReloadInventoryFromPlayerPrefs()
    {
        if (inventoryManager == null || !inventoryManager.enablePersistence) return;
        
        string savedData = PlayerPrefs.GetString("InventoryData_v1", "");
        if (string.IsNullOrEmpty(savedData))
        {
            Debug.Log("📭 No saved inventory data found in PlayerPrefs");
            return;
        }
        
        Debug.Log("📥 Found saved inventory data, forcing reload...");
        
        // Use reflection to call the private LoadInventoryFromPlayerPrefs method
        var method = inventoryManager.GetType().GetMethod("LoadInventoryFromPlayerPrefs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
        if (method != null)
        {
            method.Invoke(inventoryManager, null);
            Debug.Log("✅ Successfully reloaded inventory from PlayerPrefs");
        }
        else
        {
            Debug.LogError("❌ Could not find LoadInventoryFromPlayerPrefs method");
        }
    }
    #endregion
    
    #region Full UI Creation
    private void CreateFullInventoryUI()
    {
        Debug.Log("🎨 Creating full inventory UI...");
        
        // Get or create canvas
        SetupCanvas();
        
        // Create main inventory panel
        CreateInventoryPanel();
        
        Debug.Log("✅ Full inventory UI created");
    }
    
    private void SetupCanvas()
    {
        parentCanvas = FindObjectOfType<Canvas>();
        if (parentCanvas == null)
        {
            GameObject canvasObj = new GameObject("InventoryCanvas");
            parentCanvas = canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.Log("✅ Created Canvas");
        }
    }
    
    private void CreateInventoryPanel()
    {
        // Create main inventory panel
        GameObject panelObj = new GameObject("InventoryPanel");
        panelObj.transform.SetParent(parentCanvas.transform, false);
        
        // Add panel background
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Add InventoryPanelUI component
        inventoryPanelUI = panelObj.AddComponent<InventoryPanelUI>();
        
        // Create sections
        CreateTitle(panelObj);
        GameObject storageSection = CreateSection(panelObj, "Storage", new Vector2(0.05f, 0.5f), new Vector2(0.6f, 0.95f));
        GameObject equipmentSection = CreateSection(panelObj, "Equipment", new Vector2(0.65f, 0.7f), new Vector2(0.95f, 0.95f));
        GameObject infoSection = CreateSection(panelObj, "Info", new Vector2(0.65f, 0.05f), new Vector2(0.95f, 0.65f));
        
        // Create slot prefab
        GameObject slotPrefab = CreateSlotPrefab();
        
        // Setup InventoryPanelUI references
        inventoryPanelUI.inventoryPanel = panelObj;
        inventoryPanelUI.storageSlotContainer = storageSection.transform;
        inventoryPanelUI.equipmentSlotContainer = equipmentSection.transform;
        inventoryPanelUI.slotPrefab = slotPrefab;
        
        // Create UI elements
        CreateCloseButton(panelObj);
        CreateActionButtons(infoSection);
        CreateInfoTexts(infoSection);
        
        // Hide panel initially
        panelObj.SetActive(false);
    }
    
    private void CreateTitle(GameObject parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "INVENTORY";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }
    
    private GameObject CreateSection(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject section = new GameObject(name + "Section");
        section.transform.SetParent(parent.transform, false);
        
        Image sectionBg = section.AddComponent<Image>();
        sectionBg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        
        RectTransform sectionRect = section.GetComponent<RectTransform>();
        sectionRect.anchorMin = anchorMin;
        sectionRect.anchorMax = anchorMax;
        sectionRect.offsetMin = Vector2.zero;
        sectionRect.offsetMax = Vector2.zero;
        
        // Add section title
        GameObject titleObj = new GameObject(name + "Title");
        titleObj.transform.SetParent(section.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = name.ToUpper();
        titleText.fontSize = 16;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.yellow;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // Add grid layout for slots
        if (name == "Storage" || name == "Equipment")
        {
            GridLayoutGroup grid = section.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(80, 100);
            grid.spacing = new Vector2(5, 5);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            
            if (name == "Storage")
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
            }
            else
            {
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 1;
            }
        }
        
        return section;
    }
    
    private GameObject CreateSlotPrefab()
    {
        GameObject slotPrefab = new GameObject("InventorySlot");
        
        // Add slot background
        Image slotBg = slotPrefab.AddComponent<Image>();
        slotBg.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        
        RectTransform slotRect = slotPrefab.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(80, 100);
        
        // Add InventorySlotUI component
        InventorySlotUI slotUI = slotPrefab.AddComponent<InventorySlotUI>();
        slotUI.slotBackground = slotBg;
        
        // Create card image
        GameObject cardImageObj = new GameObject("CardImage");
        cardImageObj.transform.SetParent(slotPrefab.transform, false);
        Image cardImage = cardImageObj.AddComponent<Image>();
        cardImage.color = Color.white;
        slotUI.cardImage = cardImage;
        
        RectTransform cardRect = cardImageObj.GetComponent<RectTransform>();
        cardRect.anchorMin = Vector2.zero;
        cardRect.anchorMax = Vector2.one;
        cardRect.offsetMin = new Vector2(5, 20);
        cardRect.offsetMax = new Vector2(-5, -5);
        
        // Create material background
        GameObject matBgObj = new GameObject("MaterialBackground");
        matBgObj.transform.SetParent(slotPrefab.transform, false);
        matBgObj.transform.SetSiblingIndex(0);
        Image matBg = matBgObj.AddComponent<Image>();
        slotUI.materialBackground = matBg;
        
        RectTransform matRect = matBgObj.GetComponent<RectTransform>();
        matRect.anchorMin = Vector2.zero;
        matRect.anchorMax = Vector2.one;
        matRect.offsetMin = Vector2.zero;
        matRect.offsetMax = Vector2.zero;
        
        // Create card name text
        GameObject nameObj = new GameObject("CardName");
        nameObj.transform.SetParent(slotPrefab.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 8;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        slotUI.cardNameText = nameText;
        
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.2f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        
        // Create durability text
        GameObject durabilityObj = new GameObject("Durability");
        durabilityObj.transform.SetParent(slotPrefab.transform, false);
        TextMeshProUGUI durabilityText = durabilityObj.AddComponent<TextMeshProUGUI>();
        durabilityText.fontSize = 12;
        durabilityText.alignment = TextAlignmentOptions.Center;
        durabilityText.color = Color.green;
        durabilityText.fontStyle = FontStyles.Bold;
        slotUI.durabilityText = durabilityText;
        
        RectTransform durabilityRect = durabilityObj.GetComponent<RectTransform>();
        durabilityRect.anchorMin = new Vector2(0.7f, 0.7f);
        durabilityRect.anchorMax = new Vector2(1, 1);
        durabilityRect.offsetMin = Vector2.zero;
        durabilityRect.offsetMax = Vector2.zero;
        
        // Create empty slot indicator
        GameObject emptyObj = new GameObject("EmptyIndicator");
        emptyObj.transform.SetParent(slotPrefab.transform, false);
        TextMeshProUGUI emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
        emptyText.text = "EMPTY";
        emptyText.fontSize = 10;
        emptyText.alignment = TextAlignmentOptions.Center;
        emptyText.color = Color.gray;
        slotUI.emptySlotIndicator = emptyObj;
        
        RectTransform emptyRect = emptyObj.GetComponent<RectTransform>();
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = Vector2.zero;
        emptyRect.offsetMax = Vector2.zero;
        
        return slotPrefab;
    }
    
    private void CreateCloseButton(GameObject parent)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent.transform, false);
        
        Button closeButton = closeObj.AddComponent<Button>();
        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = Color.red;
        
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.95f, 0.95f);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.sizeDelta = new Vector2(40, 40);
        
        // Add close text
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeObj.transform, false);
        TextMeshProUGUI closeText = closeTextObj.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 16;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
        
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;
        
        inventoryPanelUI.closeButton = closeButton;
    }
    
    private void CreateActionButtons(GameObject parent)
    {
        string[] buttonNames = { "Equip", "Unequip", "Discard" };
        Button[] buttons = new Button[3];
        
        for (int i = 0; i < buttonNames.Length; i++)
        {
            GameObject buttonObj = new GameObject(buttonNames[i] + "Button");
            buttonObj.transform.SetParent(parent.transform, false);
            
            Button button = buttonObj.AddComponent<Button>();
            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.4f, 0.4f, 0.8f, 0.8f);
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            float yPos = 0.7f - (i * 0.15f);
            buttonRect.anchorMin = new Vector2(0.1f, yPos);
            buttonRect.anchorMax = new Vector2(0.9f, yPos + 0.1f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            // Add button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = buttonNames[i];
            buttonText.fontSize = 12;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            buttons[i] = button;
        }
        
        inventoryPanelUI.equipButton = buttons[0];
        inventoryPanelUI.unequipButton = buttons[1];
        inventoryPanelUI.discardButton = buttons[2];
    }

        
    private void CreateInfoTexts(GameObject parent)
    {
        // Inventory stats text
        GameObject statsObj = new GameObject("InventoryStats");
        statsObj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.fontSize = 10;
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.color = Color.white;
        
        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.05f, 0.25f);
        statsRect.anchorMax = new Vector2(0.95f, 0.4f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;
        
        inventoryPanelUI.inventoryStatsText = statsText;
        
        // Selected card info text
        GameObject infoObj = new GameObject("SelectedCardInfo");
        infoObj.transform.SetParent(parent.transform, false);
        TextMeshProUGUI infoText = infoObj.AddComponent<TextMeshProUGUI>();
        infoText.fontSize = 10;
        infoText.alignment = TextAlignmentOptions.TopLeft;
        infoText.color = Color.white;
        
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.05f, 0.05f);
        infoRect.anchorMax = new Vector2(0.95f, 0.2f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;
        
        inventoryPanelUI.selectedCardInfoText = infoText;
    }
    #endregion
    
    #region Test Buttons
    private void CreateTestButtons()
    {
        // Create inventory toggle button
        GameObject invButtonObj = new GameObject("InventoryButton");
        invButtonObj.transform.SetParent(parentCanvas.transform, false);
        
        inventoryButton = invButtonObj.AddComponent<Button>();
        Image invBg = invButtonObj.AddComponent<Image>();
        invBg.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        
        RectTransform invRect = invButtonObj.GetComponent<RectTransform>();
        invRect.anchorMin = new Vector2(0, 0.9f);
        invRect.anchorMax = new Vector2(0.15f, 1);
        invRect.offsetMin = Vector2.zero;
        invRect.offsetMax = Vector2.zero;
        
        // Add button text
        GameObject invTextObj = new GameObject("Text");
        invTextObj.transform.SetParent(invButtonObj.transform, false);
        TextMeshProUGUI invText = invTextObj.AddComponent<TextMeshProUGUI>();
        invText.text = "INVENTORY";
        invText.fontSize = 12;
        invText.alignment = TextAlignmentOptions.Center;
        invText.color = Color.white;
        
        RectTransform invTextRect = invTextObj.GetComponent<RectTransform>();
        invTextRect.anchorMin = Vector2.zero;
        invTextRect.anchorMax = Vector2.one;
        invTextRect.offsetMin = Vector2.zero;
        invTextRect.offsetMax = Vector2.zero;
        
        inventoryButton.onClick.AddListener(() => inventoryPanelUI.ToggleInventory());
        
        // Create add card button
        GameObject addButtonObj = new GameObject("AddTestCardButton");
        addButtonObj.transform.SetParent(parentCanvas.transform, false);
        
        testAddCardButton = addButtonObj.AddComponent<Button>();
        Image addBg = addButtonObj.AddComponent<Image>();
        addBg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        
        RectTransform addRect = addButtonObj.GetComponent<RectTransform>();
        addRect.anchorMin = new Vector2(0.15f, 0.9f);
        addRect.anchorMax = new Vector2(0.3f, 1);
        addRect.offsetMin = Vector2.zero;
        addRect.offsetMax = Vector2.zero;
        
        // Add button text
        GameObject addTextObj = new GameObject("Text");
        addTextObj.transform.SetParent(addButtonObj.transform, false);
        TextMeshProUGUI addText = addTextObj.AddComponent<TextMeshProUGUI>();
        addText.text = "ADD TEST CARD";
        addText.fontSize = 12;
        addText.alignment = TextAlignmentOptions.Center;
        addText.color = Color.white;
        
        RectTransform addTextRect = addTextObj.GetComponent<RectTransform>();
        addTextRect.anchorMin = Vector2.zero;
        addTextRect.anchorMax = Vector2.one;
        addTextRect.offsetMin = Vector2.zero;
        addTextRect.offsetMax = Vector2.zero;
        
        testAddCardButton.onClick.AddListener(AddTestCard);
    }
    
    private void AddTestCard()
    {
        TarotCardData cardToAdd = null;
        
        // Method 1: Try to get a random card from Resources/ScriptableObjects first
        TarotCardData randomOriginalCard = GetRandomCardFromAssets();
        
        if (randomOriginalCard != null)
        {
            // Create a runtime copy with original sprites
            cardToAdd = ScriptableObject.CreateInstance<TarotCardData>();
            cardToAdd.cardName = randomOriginalCard.cardName;
            cardToAdd.cardType = randomOriginalCard.cardType;
            cardToAdd.description = randomOriginalCard.description;
            cardToAdd.price = randomOriginalCard.price;
            cardToAdd.cardImage = randomOriginalCard.cardImage; // Use original sprite!
            
            Debug.Log($"✅ Using original card asset: {randomOriginalCard.name}");
        }
        else
        {
            // Method 2: Fallback to finding existing cards in scene
            TarotCard[] existingCards = FindObjectsOfType<TarotCard>();
            
            if (existingCards.Length > 0)
            {
                // Pick a random existing card
                TarotCard randomExisting = existingCards[Random.Range(0, existingCards.Length)];
                
                if (randomExisting.cardData != null)
                {
                    // Create a runtime copy
                    cardToAdd = ScriptableObject.CreateInstance<TarotCardData>();
                    cardToAdd.cardName = randomExisting.cardData.cardName;
                    cardToAdd.cardType = randomExisting.cardData.cardType;
                    cardToAdd.description = randomExisting.cardData.description;
                    cardToAdd.price = randomExisting.cardData.price;
                    cardToAdd.cardImage = randomExisting.cardData.cardImage; // Use original sprite!
                    
                    Debug.Log($"✅ Using scene card: {randomExisting.cardData.name}");
                }
            }
        }
        
        if (cardToAdd != null)
        {
            // Randomize material type for variety
            TarotMaterialType randomMaterialType = (TarotMaterialType)Random.Range(0, System.Enum.GetValues(typeof(TarotMaterialType)).Length);
            
            MaterialData material = ScriptableObject.CreateInstance<MaterialData>();
            material.materialName = randomMaterialType.ToString();
            material.materialType = randomMaterialType;
            
            // Set uses based on material type
            switch (randomMaterialType)
            {
                case TarotMaterialType.Paper: material.maxUses = 1; break;
                case TarotMaterialType.Cardboard: material.maxUses = 2; break;
                case TarotMaterialType.Wood: material.maxUses = 3; break;
                case TarotMaterialType.Copper: material.maxUses = 4; break;
                case TarotMaterialType.Silver: material.maxUses = 5; break;
                case TarotMaterialType.Gold: material.maxUses = 6; break;
                case TarotMaterialType.Platinum: material.maxUses = 7; break;
                case TarotMaterialType.Diamond: material.maxUses = 999; break; // "Unlimited"
                default: material.maxUses = 3; break;
            }
            
            // Create material background sprite
            material.backgroundSprite = CreateMaterialBackgroundSprite(randomMaterialType);
            
            cardToAdd.AssignMaterial(material);
            cardToAdd.maxUses = material.maxUses;
            cardToAdd.currentUses = 0; // Start fresh
            
            Debug.Log($"✅ Created test card: {cardToAdd.cardName} with {randomMaterialType} material");
            
            // Add to inventory
            if (inventoryManager != null)
            {
                bool success = inventoryManager.AddPurchasedCard(cardToAdd);
                if (success)
                {
                    // Save to PlayerPrefs after adding
                    SaveInventoryToPlayerPrefs();
                }
                Debug.Log(success ? $"✅ Added card: {cardToAdd.cardName}" : "❌ Failed to add card - inventory full!");
            }
        }
        else
        {
            Debug.LogError("❌ No card data found in assets or scene!");
            Debug.Log("💡 SOLUTION: Make sure there are TarotCardData assets in ScriptableObject folder or cards in the scene");
        }
    }
    #endregion
    
    #region UI Initialization Fix
    private void InitializeUIAfterSetup()
    {
        Debug.Log("🔄 Initializing UI after complete setup...");
        
        if (inventoryPanelUI == null)
        {
            Debug.LogError("❌ InventoryPanelUI is null - cannot initialize!");
            return;
        }
        
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("❌ InventoryManager.Instance is null - cannot initialize UI!");
            return;
        }
        
        // Force UI to reinitialize slots with current data
        Debug.Log("🔄 Forcing UI slot reinitialization...");
        
        // Wait a frame then refresh
        StartCoroutine(DelayedUIRefresh());
    }
    
    private System.Collections.IEnumerator DelayedUIRefresh()
    {
        yield return null; // Wait one frame
        
        Debug.Log("🔄 Performing delayed UI refresh...");
        
        if (inventoryPanelUI != null)
        {
            // Force reinitialize the inventory slots
            var method = inventoryPanelUI.GetType().GetMethod("InitializeInventorySlots", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(inventoryPanelUI, null);
                Debug.Log("✅ Forced UI slot reinitialization complete");
            }
            
            // Also force a display refresh
            var refreshMethod = inventoryPanelUI.GetType().GetMethod("RefreshInventoryDisplay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(inventoryPanelUI, null);
                Debug.Log("✅ Forced inventory display refresh complete");
            }
        }
        
        // Log current inventory state for debugging
        LogInventoryState();
    }
    
    private void LogInventoryState()
    {
        if (InventoryManager.Instance?.inventoryData == null) 
        {
            Debug.LogError("❌ Cannot log inventory state - data is null");
            return;
        }
        
        var data = InventoryManager.Instance.inventoryData;
        Debug.Log($"📊 INVENTORY STATE DEBUG:");
        Debug.Log($"   Storage slots: {data.storageSlots?.Count ?? 0}");
        Debug.Log($"   Equipment slots: {data.equipmentSlots?.Count ?? 0}");
        
        if (data.storageSlots != null)
        {
            for (int i = 0; i < data.storageSlots.Count; i++)
            {
                var slot = data.storageSlots[i];
                if (slot?.isOccupied == true && slot.storedCard != null)
                {
                    Debug.Log($"   📦 Storage[{i}]: {slot.storedCard.cardName} (Uses: {slot.storedCard.currentUses}/{slot.storedCard.assignedMaterial?.maxUses})");
                }
            }
        }
        
        var stats = InventoryManager.Instance.GetInventoryStats();
        Debug.Log($"   📈 Stats: Storage {stats.storageUsed}/{stats.storageTotal}, Equipment {stats.equipmentUsed}/{stats.equipmentTotal}");
    }
    
    [ContextMenu("Force UI Refresh")]
    public void ForceUIRefresh()
    {
        Debug.Log("🔄 Manual UI refresh triggered...");
        InitializeUIAfterSetup();
    }
    
    [ContextMenu("Log Inventory State")]  
    public void ManualLogInventoryState()
    {
        LogInventoryState();
    }
    
    [ContextMenu("Debug Raw Slot Data")]
    public void DebugRawSlotData()
    {
        if (InventoryManager.Instance?.inventoryData == null)
        {
            Debug.LogError("❌ No inventory data to debug");
            return;
        }
        
        var data = InventoryManager.Instance.inventoryData;
        Debug.Log("🔍 RAW SLOT DATA DEBUG:");
        
        for (int i = 0; i < data.storageSlots.Count; i++)
        {
            var slot = data.storageSlots[i];
            Debug.Log($"   Storage[{i}]: isOccupied={slot?.isOccupied}, storedCard={(slot?.storedCard?.cardName ?? "null")}, slotIndex={slot?.slotIndex}");
        }
        
        for (int i = 0; i < data.equipmentSlots.Count; i++)
        {
            var slot = data.equipmentSlots[i];
            Debug.Log($"   Equipment[{i}]: isOccupied={slot?.isOccupied}, storedCard={(slot?.storedCard?.cardName ?? "null")}, slotIndex={slot?.slotIndex}");
        }
        
        // Also check what would be saved
        Debug.Log("🔍 WHAT WOULD BE SAVED:");
        string currentSaveData = PlayerPrefs.GetString("InventoryData_v1", "");
        Debug.Log($"   Current PlayerPrefs: {currentSaveData}");
        
        // Force a manual save and see what gets generated
        if (InventoryManager.Instance != null)
        {
            var method = InventoryManager.Instance.GetType().GetMethod("SaveInventoryToPlayerPrefs", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(InventoryManager.Instance, null);
                string newSaveData = PlayerPrefs.GetString("InventoryData_v1", "");
                Debug.Log($"   After manual save: {newSaveData}");
            }
        }
    }
    
    [ContextMenu("Clear PlayerPrefs and Reset")]
    public void ClearPlayerPrefsAndReset()
    {
        PlayerPrefs.DeleteKey("InventoryData_v1");
        PlayerPrefs.Save();
        Debug.Log("🗑️ Cleared PlayerPrefs");
        
        // Add a test card to see if saving works
        if (InventoryManager.Instance != null)
        {
            AddTestCard();
            Debug.Log("🧪 Added test card after clearing PlayerPrefs");
        }
    }
    #endregion
    
    #region Testing & Controls
    private void TestEverything()
    {
        Debug.Log("🧪 Testing complete system...");
        
        if (inventoryManager == null)
        {
            Debug.LogError("❌ InventoryManager is null!");
            return;
        }
        
        if (inventoryPanelUI == null)
        {
            Debug.LogError("❌ InventoryPanelUI is null!");
            return;
        }
        
        if (inventoryData == null)
        {
            Debug.LogError("❌ InventoryData is null!");
            return;
        }
        
        var stats = inventoryManager.GetInventoryStats();
        Debug.Log($"📊 Inventory: {stats.storageUsed}/{stats.storageTotal} storage, {stats.equipmentUsed}/{stats.equipmentTotal} equipment");
        
        string savedData = PlayerPrefs.GetString("InventoryData_v1", "");
        Debug.Log(string.IsNullOrEmpty(savedData) ? "💾 No saved data (normal for first run)" : "💾 Found saved data - persistence working!");
        
        Debug.Log("✅ All tests passed!");
    }
    
    private void Update()
    {
        // Toggle inventory with 'I' key
        if (Input.GetKeyDown(KeyCode.I) && inventoryPanelUI != null)
        {
            inventoryPanelUI.ToggleInventory();
        }
    }
    
    [ContextMenu("Test Persistence")]
    public void TestPersistence()
    {
        string savedData = PlayerPrefs.GetString("FinalInventoryData_v1", "");
        if (string.IsNullOrEmpty(savedData))
        {
            Debug.LogWarning("❌ No inventory data in PlayerPrefs");
        }
        else
        {
            Debug.Log("✅ Inventory data found in PlayerPrefs!");
            Debug.Log($"📄 Data: {savedData.Substring(0, Mathf.Min(100, savedData.Length))}...");
        }
    }
    
    [ContextMenu("Clear All Data")]
    public void ClearAllData()
    {
        PlayerPrefs.DeleteKey("FinalInventoryData_v1");
        PlayerPrefs.Save();
        Debug.Log("🗑️ Cleared all saved data");
        
        // Clear runtime data too
        if (inventoryData != null)
        {
            foreach (var slot in inventoryData.storageSlots)
            {
                slot.RemoveCard();
            }
            foreach (var slot in inventoryData.equipmentSlots)
            {
                slot.RemoveCard();
            }
        }
    }
    
    [ContextMenu("Force Save to PlayerPrefs")]
    public void ForceSaveToPlayerPrefs()
    {
        SaveInventoryToPlayerPrefs();
        Debug.Log("💾 Forced save to PlayerPrefs");
    }
    
    [ContextMenu("Debug ScriptableObjects")]
    public void DebugScriptableObjects()
    {
        Debug.Log("🔍 DEBUGGING SCRIPTABLE OBJECTS:");
        
        // Check Resources folder
        TarotCardData[] allCards = Resources.LoadAll<TarotCardData>("");
        Debug.Log($"📁 Found {allCards.Length} TarotCardData in Resources root:");
        
        for (int i = 0; i < allCards.Length; i++)
        {
            var card = allCards[i];
            Debug.Log($"   [{i}] {card.name} - {card.cardName} (Type: {card.cardType}) - HasSprite: {card.cardImage != null}");
        }
        
        // Check ScriptableObject folder using Editor API
        #if UNITY_EDITOR
        Debug.Log("📁 Checking ScriptableObject folder:");
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TarotCardData", new[] {"Assets/ScriptableObject"});
        Debug.Log($"📁 Found {guids.Length} TarotCardData in ScriptableObject folder:");
        
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            TarotCardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<TarotCardData>(path);
            if (card != null)
            {
                Debug.Log($"   [{i}] {card.name} - {card.cardName} (Type: {card.cardType}) - HasSprite: {card.cardImage != null} - Path: {path}");
            }
        }
        #endif
        
        // Test the new methods
        Debug.Log("🧪 Testing GetRandomCardFromAssets:");
        TarotCardData randomCard = GetRandomCardFromAssets();
        if (randomCard != null)
        {
            Debug.Log($"✅ Got random card: {randomCard.name} - HasSprite: {randomCard.cardImage != null}");
        }
        else
        {
            Debug.LogError("❌ GetRandomCardFromAssets returned null!");
        }
    }
    
    [ContextMenu("Debug Materials")]
    public void DebugMaterials()
    {
        Debug.Log("🔍 DEBUGGING MATERIALS:");
        
        // Check all material types
        foreach (TarotMaterialType materialType in System.Enum.GetValues(typeof(TarotMaterialType)))
        {
            MaterialData material = LoadMaterialFromResources(materialType);
            if (material != null)
            {
                Debug.Log($"✅ {materialType}: {material.name} - HasSprite: {material.backgroundSprite != null}");
                if (material.backgroundSprite != null)
                {
                    Debug.Log($"   Sprite: {material.backgroundSprite.name}");
                }
            }
            else
            {
                Debug.LogWarning($"❌ {materialType}: Not found in Resources");
            }
        }
        
        // Also check all materials in Resources folder
        MaterialData[] allMaterials = Resources.LoadAll<MaterialData>("Materials");
        Debug.Log($"📁 Found {allMaterials.Length} MaterialData in Resources/Materials:");
        for (int i = 0; i < allMaterials.Length; i++)
        {
            var mat = allMaterials[i];
            Debug.Log($"   [{i}] {mat.name} - Type: {mat.materialType} - HasSprite: {mat.backgroundSprite != null}");
        }
    }
    
    private void OverrideInventoryManagerSaving()
    {
        // Subscribe to inventory events to trigger PlayerPrefs saving
        if (inventoryManager != null)
        {
            inventoryManager.OnCardAdded += (card) => SaveInventoryToPlayerPrefs();
            inventoryManager.OnCardRemoved += (card) => SaveInventoryToPlayerPrefs();
            inventoryManager.OnCardEquippedChanged += (card, equipped) => SaveInventoryToPlayerPrefs();
        }
    }
    
    private void FixShopManagerReference()
    {
        // Find and assign the ShopManager reference to InventoryManager
        if (inventoryManager != null)
        {
            ShopManager shop = FindObjectOfType<ShopManager>();
            if (shop != null)
            {
                // Directly assign the public shopManager field
                inventoryManager.shopManager = shop;
                Debug.Log("✅ Fixed ShopManager reference for tarot panel sync");
            }
            else
            {
                Debug.LogWarning("⚠️ ShopManager not found in scene");
            }
        }
    }
    
    private System.Collections.IEnumerator ForceUIRefreshAfterLoad()
    {
        yield return new WaitForSeconds(0.5f); // Wait a bit for UI to be ready
        
        // Find and refresh the inventory UI
        if (inventoryPanelUI != null)
        {
            Debug.Log("🔄 Forcing UI refresh after card loading...");
            
            var refreshMethod = inventoryPanelUI.GetType().GetMethod("RefreshInventoryDisplay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (refreshMethod != null)
            {
                refreshMethod.Invoke(inventoryPanelUI, null);
                Debug.Log("✅ UI refreshed after card loading");
            }
        }
    }
    #endregion
}

