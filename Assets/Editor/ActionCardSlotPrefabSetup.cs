using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor utility to create Action Card Slot prefabs:
/// 1. DeckCardSlot prefab - For deck inspector with Equip button
/// 2. ActionCardSlotUI prefab - For gameplay window with Use button
/// Run from menu: Tools > Create Action Card Slot Prefab / Create Action Card Window Slot Prefab
/// </summary>
public class ActionCardSlotPrefabSetup : Editor
{
    [MenuItem("Tools/Create Action Card Slot Prefab")]
    public static void CreateActionCardSlotPrefab()
    {
        // Create the root GameObject
        GameObject slotObj = new GameObject("ActionCardSlot");
        
        // Add RectTransform
        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(120, 170);
        
        // Add background Image
        Image bgImage = slotObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Add DeckCardSlot component
        DeckCardSlot slotComponent = slotObj.AddComponent<DeckCardSlot>();
        
        // === Create Card Image ===
        GameObject cardImageObj = new GameObject("CardImage");
        cardImageObj.transform.SetParent(slotObj.transform, false);
        RectTransform cardImageRect = cardImageObj.AddComponent<RectTransform>();
        cardImageRect.anchorMin = Vector2.zero;
        cardImageRect.anchorMax = Vector2.one;
        cardImageRect.offsetMin = new Vector2(5, 35); // Leave space for button at bottom
        cardImageRect.offsetMax = new Vector2(-5, -5);
        Image cardImage = cardImageObj.AddComponent<Image>();
        cardImage.color = Color.white;
        slotComponent.cardImage = cardImage;
        
        // === Create Selection Border ===
        GameObject borderObj = new GameObject("SelectionBorder");
        borderObj.transform.SetParent(slotObj.transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(1f, 0.8f, 0f, 0f); // Gold, initially transparent
        borderImage.raycastTarget = false;
        // Make it an outline by using Sliced sprite type or just a frame
        slotComponent.selectionBorder = borderImage;
        borderObj.SetActive(false); // Hidden by default
        
        // === Create Equipped Indicator ===
        GameObject equippedObj = new GameObject("EquippedIndicator");
        equippedObj.transform.SetParent(slotObj.transform, false);
        RectTransform equippedRect = equippedObj.AddComponent<RectTransform>();
        equippedRect.anchorMin = new Vector2(1, 1);
        equippedRect.anchorMax = new Vector2(1, 1);
        equippedRect.pivot = new Vector2(1, 1);
        equippedRect.anchoredPosition = new Vector2(-5, -5);
        equippedRect.sizeDelta = new Vector2(25, 25);
        Image equippedImage = equippedObj.AddComponent<Image>();
        equippedImage.color = new Color(0.2f, 0.9f, 0.2f, 1f); // Green checkmark color
        equippedImage.raycastTarget = false;
        slotComponent.equippedIndicator = equippedObj;
        equippedObj.SetActive(false); // Hidden by default
        
        // Add checkmark text as placeholder
        GameObject checkTextObj = new GameObject("CheckText");
        checkTextObj.transform.SetParent(equippedObj.transform, false);
        RectTransform checkTextRect = checkTextObj.AddComponent<RectTransform>();
        checkTextRect.anchorMin = Vector2.zero;
        checkTextRect.anchorMax = Vector2.one;
        checkTextRect.offsetMin = Vector2.zero;
        checkTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI checkText = checkTextObj.AddComponent<TextMeshProUGUI>();
        checkText.text = "✓";
        checkText.fontSize = 18;
        checkText.color = Color.white;
        checkText.alignment = TextAlignmentOptions.Center;
        
        // === Create Equip Button (Embedded) ===
        GameObject equipBtnObj = new GameObject("EquipButton");
        equipBtnObj.transform.SetParent(slotObj.transform, false);
        RectTransform equipBtnRect = equipBtnObj.AddComponent<RectTransform>();
        equipBtnRect.anchorMin = new Vector2(0, 0);
        equipBtnRect.anchorMax = new Vector2(1, 0);
        equipBtnRect.pivot = new Vector2(0.5f, 0);
        equipBtnRect.anchoredPosition = new Vector2(0, 5);
        equipBtnRect.sizeDelta = new Vector2(-10, 28);
        
        Image equipBtnImage = equipBtnObj.AddComponent<Image>();
        equipBtnImage.color = new Color(0.3f, 0.6f, 0.3f, 1f); // Green button
        
        Button equipButton = equipBtnObj.AddComponent<Button>();
        ColorBlock colors = equipButton.colors;
        colors.normalColor = new Color(0.3f, 0.6f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.4f, 0.7f, 0.4f, 1f);
        colors.pressedColor = new Color(0.2f, 0.5f, 0.2f, 1f);
        equipButton.colors = colors;
        
        slotComponent.equipButton = equipButton;
        
        // Add button text
        GameObject equipTextObj = new GameObject("Text");
        equipTextObj.transform.SetParent(equipBtnObj.transform, false);
        RectTransform equipTextRect = equipTextObj.AddComponent<RectTransform>();
        equipTextRect.anchorMin = Vector2.zero;
        equipTextRect.anchorMax = Vector2.one;
        equipTextRect.offsetMin = Vector2.zero;
        equipTextRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI equipBtnText = equipTextObj.AddComponent<TextMeshProUGUI>();
        equipBtnText.text = "Equip";
        equipBtnText.fontSize = 14;
        equipBtnText.color = Color.white;
        equipBtnText.alignment = TextAlignmentOptions.Center;
        slotComponent.equipButtonText = equipBtnText;
        
        // Hide button by default
        equipBtnObj.SetActive(false);
        
        // === Create Empty Text ===
        GameObject emptyTextObj = new GameObject("EmptyText");
        emptyTextObj.transform.SetParent(cardImageObj.transform, false);
        RectTransform emptyTextRect = emptyTextObj.AddComponent<RectTransform>();
        emptyTextRect.anchorMin = Vector2.zero;
        emptyTextRect.anchorMax = Vector2.one;
        emptyTextRect.offsetMin = Vector2.zero;
        emptyTextRect.offsetMax = Vector2.zero;
        Text emptyText = emptyTextObj.AddComponent<Text>();
        emptyText.text = "Empty";
        emptyText.fontSize = 14;
        emptyText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        emptyText.alignment = TextAnchor.MiddleCenter;
        slotComponent.emptyText = emptyText;
        emptyTextObj.SetActive(false);
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/ActionCardSlot.prefab";
        
        // Ensure directory exists
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
        }
        
        // Check if prefab already exists
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists", 
                "ActionCardSlot prefab already exists. Replace it?", "Yes", "No"))
            {
                DestroyImmediate(slotObj);
                return;
            }
        }
        
        // Create the prefab
        PrefabUtility.SaveAsPrefabAsset(slotObj, prefabPath);
        
        // Clean up scene object
        DestroyImmediate(slotObj);
        
        Debug.Log($"[ActionCardSlotPrefabSetup] Created ActionCardSlot prefab at: {prefabPath}");
        Debug.Log("To use this prefab:");
        Debug.Log("1. Assign it to DeckInspectorPanel's 'Card Slot Prefab' field");
        Debug.Log("2. Or drag it into your scene and customize");
        
        // Select the prefab in project window
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
    
    [MenuItem("Tools/Add Equip Button to Existing Card Slot")]
    public static void AddEquipButtonToSelectedSlot()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection", 
                "Please select a DeckCardSlot GameObject in the hierarchy or prefab.", "OK");
            return;
        }
        
        DeckCardSlot slot = selected.GetComponent<DeckCardSlot>();
        if (slot == null)
        {
            EditorUtility.DisplayDialog("Invalid Selection", 
                "Selected object does not have a DeckCardSlot component.", "OK");
            return;
        }
        
        // Check if equip button already exists
        if (slot.equipButton != null)
        {
            EditorUtility.DisplayDialog("Button Exists", 
                "This slot already has an equip button assigned.", "OK");
            return;
        }
        
        // Create equip button
        GameObject equipBtnObj = new GameObject("EquipButton");
        equipBtnObj.transform.SetParent(selected.transform, false);
        
        RectTransform equipBtnRect = equipBtnObj.AddComponent<RectTransform>();
        equipBtnRect.anchorMin = new Vector2(0, 0);
        equipBtnRect.anchorMax = new Vector2(1, 0);
        equipBtnRect.pivot = new Vector2(0.5f, 0);
        equipBtnRect.anchoredPosition = new Vector2(0, 5);
        equipBtnRect.sizeDelta = new Vector2(-10, 28);
        
        Image equipBtnImage = equipBtnObj.AddComponent<Image>();
        equipBtnImage.color = new Color(0.3f, 0.6f, 0.3f, 1f);
        
        Button equipButton = equipBtnObj.AddComponent<Button>();
        slot.equipButton = equipButton;
        
        // Add text
        GameObject equipTextObj = new GameObject("Text");
        equipTextObj.transform.SetParent(equipBtnObj.transform, false);
        RectTransform equipTextRect = equipTextObj.AddComponent<RectTransform>();
        equipTextRect.anchorMin = Vector2.zero;
        equipTextRect.anchorMax = Vector2.one;
        equipTextRect.offsetMin = Vector2.zero;
        equipTextRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI equipBtnText = equipTextObj.AddComponent<TextMeshProUGUI>();
        equipBtnText.text = "Equip";
        equipBtnText.fontSize = 14;
        equipBtnText.color = Color.white;
        equipBtnText.alignment = TextAlignmentOptions.Center;
        slot.equipButtonText = equipBtnText;
        
        // Hide by default
        equipBtnObj.SetActive(false);
        
        EditorUtility.SetDirty(slot);
        
        Debug.Log("[ActionCardSlotPrefabSetup] Added equip button to " + selected.name);
    }
    
    /// <summary>
    /// Create ActionCardSlotUI prefab for the gameplay Action Card Window (with Use button)
    /// </summary>
    [MenuItem("Tools/Create Action Card Window Slot Prefab")]
    public static void CreateActionCardWindowSlotPrefab()
    {
        // Create the root GameObject
        GameObject slotObj = new GameObject("ActionCardWindowSlot");
        
        // Add RectTransform
        RectTransform slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(120, 170);
        
        // Add background Image
        Image bgImage = slotObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        
        // Add ActionCardSlotUI component
        ActionCardSlotUI slotComponent = slotObj.AddComponent<ActionCardSlotUI>();
        
        // === Create Card Background ===
        GameObject cardBgObj = new GameObject("CardBackground");
        cardBgObj.transform.SetParent(slotObj.transform, false);
        RectTransform cardBgRect = cardBgObj.AddComponent<RectTransform>();
        cardBgRect.anchorMin = new Vector2(0, 0.2f);
        cardBgRect.anchorMax = new Vector2(1, 1);
        cardBgRect.offsetMin = new Vector2(5, 0);
        cardBgRect.offsetMax = new Vector2(-5, -5);
        Image cardBgImage = cardBgObj.AddComponent<Image>();
        cardBgImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
        slotComponent.cardBackground = cardBgImage;
        
        // === Create Card Image (Icon) ===
        GameObject cardImageObj = new GameObject("CardImage");
        cardImageObj.transform.SetParent(cardBgObj.transform, false);
        RectTransform cardImageRect = cardImageObj.AddComponent<RectTransform>();
        cardImageRect.anchorMin = new Vector2(0.1f, 0.2f);
        cardImageRect.anchorMax = new Vector2(0.9f, 0.9f);
        cardImageRect.offsetMin = Vector2.zero;
        cardImageRect.offsetMax = Vector2.zero;
        Image cardImage = cardImageObj.AddComponent<Image>();
        cardImage.color = Color.white;
        cardImage.preserveAspect = true;
        slotComponent.cardImage = cardImage;
        
        // === Create Card Name Text ===
        GameObject nameObj = new GameObject("CardName");
        nameObj.transform.SetParent(cardBgObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0.2f);
        nameRect.offsetMin = new Vector2(2, 2);
        nameRect.offsetMax = new Vector2(-2, 0);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "Card Name";
        nameText.fontSize = 11;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        nameText.enableWordWrapping = true;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        slotComponent.cardNameText = nameText;
        
        // === Create Selection Highlight ===
        GameObject highlightObj = new GameObject("SelectionHighlight");
        highlightObj.transform.SetParent(slotObj.transform, false);
        RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
        highlightRect.anchorMin = Vector2.zero;
        highlightRect.anchorMax = Vector2.one;
        highlightRect.offsetMin = new Vector2(-3, -3);
        highlightRect.offsetMax = new Vector2(3, 3);
        Image highlightImage = highlightObj.AddComponent<Image>();
        highlightImage.color = new Color(1f, 0.8f, 0f, 0.8f); // Gold
        highlightImage.raycastTarget = false;
        highlightObj.transform.SetAsFirstSibling(); // Behind everything
        slotComponent.selectionHighlight = highlightObj;
        highlightObj.SetActive(false); // Hidden by default
        
        // === Create Use Button ===
        GameObject useBtnObj = new GameObject("UseButton");
        useBtnObj.transform.SetParent(slotObj.transform, false);
        RectTransform useBtnRect = useBtnObj.AddComponent<RectTransform>();
        useBtnRect.anchorMin = new Vector2(0, 0);
        useBtnRect.anchorMax = new Vector2(1, 0.2f);
        useBtnRect.offsetMin = new Vector2(5, 5);
        useBtnRect.offsetMax = new Vector2(-5, -2);
        
        Image useBtnImage = useBtnObj.AddComponent<Image>();
        useBtnImage.color = new Color(0.2f, 0.5f, 0.2f, 1f); // Green
        
        Button useButton = useBtnObj.AddComponent<Button>();
        ColorBlock colors = useButton.colors;
        colors.normalColor = new Color(0.2f, 0.5f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.6f, 0.3f, 1f);
        colors.pressedColor = new Color(0.15f, 0.4f, 0.15f, 1f);
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        useButton.colors = colors;
        slotComponent.useButton = useButton;
        
        // Use button text
        GameObject useTextObj = new GameObject("Text");
        useTextObj.transform.SetParent(useBtnObj.transform, false);
        RectTransform useTextRect = useTextObj.AddComponent<RectTransform>();
        useTextRect.anchorMin = Vector2.zero;
        useTextRect.anchorMax = Vector2.one;
        useTextRect.offsetMin = Vector2.zero;
        useTextRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI useBtnText = useTextObj.AddComponent<TextMeshProUGUI>();
        useBtnText.text = "Use";
        useBtnText.fontSize = 14;
        useBtnText.fontStyle = FontStyles.Bold;
        useBtnText.color = Color.white;
        useBtnText.alignment = TextAlignmentOptions.Center;
        slotComponent.useButtonText = useBtnText;
        
        // === Create Used Overlay ===
        GameObject usedOverlayObj = new GameObject("UsedOverlay");
        usedOverlayObj.transform.SetParent(slotObj.transform, false);
        RectTransform usedOverlayRect = usedOverlayObj.AddComponent<RectTransform>();
        usedOverlayRect.anchorMin = Vector2.zero;
        usedOverlayRect.anchorMax = Vector2.one;
        usedOverlayRect.offsetMin = Vector2.zero;
        usedOverlayRect.offsetMax = Vector2.zero;
        Image usedOverlayImage = usedOverlayObj.AddComponent<Image>();
        usedOverlayImage.color = new Color(0, 0, 0, 0.6f);
        usedOverlayImage.raycastTarget = false;
        slotComponent.usedOverlay = usedOverlayObj;
        usedOverlayObj.SetActive(false); // Hidden by default
        
        // Used text
        GameObject usedTextObj = new GameObject("UsedText");
        usedTextObj.transform.SetParent(usedOverlayObj.transform, false);
        RectTransform usedTextRect = usedTextObj.AddComponent<RectTransform>();
        usedTextRect.anchorMin = Vector2.zero;
        usedTextRect.anchorMax = Vector2.one;
        usedTextRect.offsetMin = Vector2.zero;
        usedTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI usedText = usedTextObj.AddComponent<TextMeshProUGUI>();
        usedText.text = "USED";
        usedText.fontSize = 16;
        usedText.fontStyle = FontStyles.Bold;
        usedText.color = new Color(1f, 0.3f, 0.3f, 1f);
        usedText.alignment = TextAlignmentOptions.Center;
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/ActionCardWindowSlot.prefab";
        
        // Ensure directory exists
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
        }
        
        // Check if prefab already exists
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists", 
                "ActionCardWindowSlot prefab already exists. Replace it?", "Yes", "No"))
            {
                DestroyImmediate(slotObj);
                return;
            }
        }
        
        // Create the prefab
        PrefabUtility.SaveAsPrefabAsset(slotObj, prefabPath);
        
        // Clean up scene object
        DestroyImmediate(slotObj);
        
        Debug.Log($"[ActionCardSlotPrefabSetup] Created ActionCardWindowSlot prefab at: {prefabPath}");
        Debug.Log("To use this prefab:");
        Debug.Log("1. Create an ActionCardWindowUI in your gameplay scene");
        Debug.Log("2. Assign this prefab to the 'Slot Prefab' field");
        Debug.Log("3. Set up a container with HorizontalLayoutGroup");
        
        // Select the prefab in project window
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}

