using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI manager for displaying equipped action cards during gameplay.
/// Similar to TarotWindowUI - displays cards in a horizontal layout with Use buttons.
/// </summary>
public class ActionCardWindowUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform container; // Container with HorizontalLayoutGroup
    public GameObject slotPrefab; // ActionCardSlotUI prefab
    public HorizontalLayoutGroup layoutGroup;
    
    [Header("Layout Settings")]
    public float cardWidth = 120f;
    public float cardHeight = 170f;
    public float spacing = 10f;
    
    [Header("Display")]
    public TextMeshProUGUI slotsText; // Optional: "X/4 Equipped"
    public TextMeshProUGUI minorHealUsesText; // Optional: "Heals: X/3"
    
    private List<ActionCardSlotUI> cardSlots = new List<ActionCardSlotUI>();
    private ActionCardSlotUI selectedSlot;
    private const int MAX_SLOTS = 4;
    
    private void Awake()
    {
        // Find container if not assigned
        if (container == null)
        {
            container = transform;
        }
        
        // Find or add HorizontalLayoutGroup
        if (layoutGroup == null)
        {
            layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
        }
        
        // Configure layout group
        ConfigureLayoutGroup();
    }
    
    private void Start()
    {
        // Sync with ActionCardManager
        SyncWithManager();
        
        // Subscribe to ActionCardManager events
        if (ActionCardManager.Instance != null)
        {
            ActionCardManager.Instance.OnEquippedCardsChanged += OnEquippedCardsChanged;
            ActionCardManager.Instance.OnMinorHealUsesChanged += OnMinorHealUsesChanged;
        }
        
        UpdateDisplayTexts();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ActionCardManager.Instance != null)
        {
            ActionCardManager.Instance.OnEquippedCardsChanged -= OnEquippedCardsChanged;
            ActionCardManager.Instance.OnMinorHealUsesChanged -= OnMinorHealUsesChanged;
        }
    }
    
    /// <summary>
    /// Configure the horizontal layout group
    /// </summary>
    private void ConfigureLayoutGroup()
    {
        if (layoutGroup == null) return;
        
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = spacing;
        layoutGroup.padding = new RectOffset(10, 10, 5, 5);
        layoutGroup.enabled = true;
    }
    
    /// <summary>
    /// Sync slots with ActionCardManager equipped cards
    /// </summary>
    public void SyncWithManager()
    {
        if (ActionCardManager.Instance == null)
        {
            Debug.LogWarning("[ActionCardWindowUI] ActionCardManager not found!");
            return;
        }
        
        List<ActionCardData> equippedCards = ActionCardManager.Instance.EquippedCards;
        
        // Clear existing slots
        ClearSlots();
        
        // Create slots for equipped cards
        for (int i = 0; i < equippedCards.Count; i++)
        {
            CreateSlot(i, equippedCards[i]);
        }
        
        // Sort slots by index and assign original sibling indices
        SortSlotsByIndex();
        
        UpdateDisplayTexts();
        
        // Force layout rebuild
        StartCoroutine(ForceLayoutRebuildAfterFrame());
    }
    
    /// <summary>
    /// Create a slot for an equipped action card
    /// </summary>
    private void CreateSlot(int index, ActionCardData cardData)
    {
        if (cardData == null) return;
        
        GameObject slotObj;
        
        if (slotPrefab != null)
        {
            slotObj = Instantiate(slotPrefab, container);
        }
        else
        {
            // Create basic slot if no prefab
            slotObj = CreateDefaultSlot();
            slotObj.transform.SetParent(container, false);
        }
        
        slotObj.name = $"ActionCardSlot_{index}_{cardData.actionName}";
        
        // Set size
        RectTransform slotRect = slotObj.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.sizeDelta = new Vector2(cardWidth, cardHeight);
        }
        
        // Get or add ActionCardSlotUI component
        ActionCardSlotUI slotUI = slotObj.GetComponent<ActionCardSlotUI>();
        if (slotUI == null)
        {
            slotUI = slotObj.AddComponent<ActionCardSlotUI>();
        }
        
        // Initialize slot
        slotUI.Initialize(cardData, index, this);
        
        cardSlots.Add(slotUI);
    }
    
    /// <summary>
    /// Create a default slot if no prefab is assigned
    /// </summary>
    private GameObject CreateDefaultSlot()
    {
        GameObject slotObj = new GameObject("ActionCardSlot");
        
        // Add RectTransform
        RectTransform rect = slotObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);
        
        // Add background image
        Image bg = slotObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Create card image
        GameObject cardImageObj = new GameObject("CardImage");
        cardImageObj.transform.SetParent(slotObj.transform, false);
        RectTransform cardImageRect = cardImageObj.AddComponent<RectTransform>();
        cardImageRect.anchorMin = new Vector2(0, 0.25f);
        cardImageRect.anchorMax = new Vector2(1, 1);
        cardImageRect.offsetMin = new Vector2(5, 0);
        cardImageRect.offsetMax = new Vector2(-5, -5);
        Image cardImage = cardImageObj.AddComponent<Image>();
        
        // Create card name text
        GameObject nameObj = new GameObject("CardName");
        nameObj.transform.SetParent(slotObj.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.15f);
        nameRect.anchorMax = new Vector2(1, 0.25f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 10;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        
        // Create Use button
        GameObject buttonObj = new GameObject("UseButton");
        buttonObj.transform.SetParent(slotObj.transform, false);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(1, 0.15f);
        buttonRect.offsetMin = new Vector2(5, 5);
        buttonRect.offsetMax = new Vector2(-5, 0);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.5f, 0.3f, 1f);
        Button useButton = buttonObj.AddComponent<Button>();
        
        // Button text
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Use";
        buttonText.fontSize = 12;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        
        // Add ActionCardSlotUI and wire up references
        ActionCardSlotUI slotUI = slotObj.AddComponent<ActionCardSlotUI>();
        slotUI.cardImage = cardImage;
        slotUI.cardNameText = nameText;
        slotUI.useButton = useButton;
        
        return slotObj;
    }
    
    /// <summary>
    /// Clear all slots
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in cardSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        cardSlots.Clear();
        selectedSlot = null;
    }
    
    /// <summary>
    /// Update display texts
    /// </summary>
    private void UpdateDisplayTexts()
    {
        if (slotsText != null && ActionCardManager.Instance != null)
        {
            slotsText.text = $"{ActionCardManager.Instance.EquippedCount}/{MAX_SLOTS}";
        }
        
        if (minorHealUsesText != null && ActionCardManager.Instance != null)
        {
            minorHealUsesText.text = $"Heals: {ActionCardManager.Instance.MinorHealRemainingUses}/3";
        }
    }
    
    /// <summary>
    /// Handle card click
    /// </summary>
    public void OnCardClicked(ActionCardSlotUI slot)
    {
        if (slot == null) return;
        
        // Toggle selection
        if (selectedSlot == slot)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
        else
        {
            if (selectedSlot != null)
            {
                selectedSlot.SetSelected(false);
            }
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
        }
    }
    
    /// <summary>
    /// Called when a card is used
    /// </summary>
    public void OnCardUsed(ActionCardSlotUI slot)
    {
        // Refresh display
        SyncWithManager();
    }
    
    /// <summary>
    /// Event handler for equipped cards changed
    /// </summary>
    private void OnEquippedCardsChanged()
    {
        SyncWithManager();
    }
    
    /// <summary>
    /// Event handler for minor heal uses changed
    /// </summary>
    private void OnMinorHealUsesChanged(int remainingUses)
    {
        UpdateDisplayTexts();
    }
    
    /// <summary>
    /// Force layout rebuild after a frame
    /// </summary>
    private IEnumerator ForceLayoutRebuildAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        
        if (layoutGroup != null)
        {
            layoutGroup.enabled = true;
        }
        
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Public method to force refresh
    /// </summary>
    public void Refresh()
    {
        SyncWithManager();
    }
    
    // ============ BRING TO FRONT / SLOT ORDERING ============
    
    /// <summary>
    /// Sort slots in hierarchy by their slot index (so layout group positions them correctly)
    /// </summary>
    private void SortSlotsByIndex()
    {
        // Remove null entries
        cardSlots.RemoveAll(s => s == null);
        
        // Sort the list by slot index
        cardSlots.Sort((a, b) => {
            if (a == null || b == null) return 0;
            return a.slotIndex.CompareTo(b.slotIndex);
        });
        
        // Reorder in hierarchy - this ensures HorizontalLayoutGroup positions them in the right order
        // Only reorder non-selected cards (selected card stays at front)
        int siblingIndex = 0;
        foreach (var slot in cardSlots)
        {
            if (slot != null && !slot.IsSelected())
            {
                slot.transform.SetSiblingIndex(siblingIndex);
                slot.SetOriginalSiblingIndex(siblingIndex);
                siblingIndex++;
            }
        }
        
        // Selected card stays at the end (front)
        if (selectedSlot != null)
        {
            selectedSlot.transform.SetAsLastSibling();
        }
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Restore a slot to its correct position in the layout order based on slotIndex
    /// </summary>
    public void RestoreSlotOrder(ActionCardSlotUI slot)
    {
        if (slot == null) return;
        
        // Sort slots by index to ensure correct order
        SortSlotsByIndex();
        
        // Force layout rebuild
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Get the currently selected slot
    /// </summary>
    public ActionCardSlotUI GetSelectedSlot()
    {
        return selectedSlot;
    }
}

