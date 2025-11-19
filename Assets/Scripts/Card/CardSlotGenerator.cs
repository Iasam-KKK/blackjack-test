using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper component to generate card slots for UI-based card hands.
/// Attach to DealerPanel or PlayerPanel and configure in Inspector.
/// </summary>
public class CardSlotGenerator : MonoBehaviour
{
    [Header("Slot Configuration")]
    [Tooltip("Number of card slots to generate")]
    public int slotCount = 10;
    
    [Tooltip("Width of each card slot")]
    public float slotWidth = 100f;
    
    [Tooltip("Height of each card slot")]
    public float slotHeight = 140f;
    
    [Tooltip("Spacing between slots")]
    public float slotSpacing = 20f;
    
    [Tooltip("Enable slight overlap for visual effect")]
    public bool enableOverlap = true;
    
    [Tooltip("Overlap amount (negative spacing)")]
    public float overlapAmount = -40f;
    
    [Header("References")]
    [Tooltip("CardHand component to assign slots to")]
    public CardHand targetHand;
    
    [Header("Auto-Setup")]
    [Tooltip("Automatically find CardHand on this GameObject")]
    public bool autoFindCardHand = true;

#if UNITY_EDITOR
    [ContextMenu("Generate Slots")]
    public void GenerateSlots()
    {
        // Auto-find CardHand if enabled
        if (autoFindCardHand && targetHand == null)
        {
            targetHand = GetComponent<CardHand>();
        }
        
        if (targetHand == null)
        {
            Debug.LogError("CardSlotGenerator: No CardHand component found! Please assign targetHand.");
            return;
        }
        
        // Clear existing slots
        ClearExistingSlots();
        
        // Create container for slots
        GameObject slotsContainer = new GameObject("CardSlots");
        RectTransform containerRect = slotsContainer.AddComponent<RectTransform>();
        slotsContainer.transform.SetParent(transform, false);
        
        // Center the container
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        
        // Calculate total width
        float actualSpacing = enableOverlap ? overlapAmount : slotSpacing;
        float totalWidth = (slotCount * slotWidth) + ((slotCount - 1) * actualSpacing);
        containerRect.sizeDelta = new Vector2(totalWidth, slotHeight);
        
        // Add HorizontalLayoutGroup for automatic spacing
        HorizontalLayoutGroup layoutGroup = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = actualSpacing;
        
        // Generate slots
        List<RectTransform> newSlots = new List<RectTransform>();
        
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slot = new GameObject($"CardSlot_{i + 1}");
            RectTransform slotRect = slot.AddComponent<RectTransform>();
            slot.transform.SetParent(slotsContainer.transform, false);
            
            // Set slot size
            slotRect.sizeDelta = new Vector2(slotWidth, slotHeight);
            
            // Optional: Add visual indicator (comment out if not needed)
            // Image slotImage = slot.AddComponent<Image>();
            // slotImage.color = new Color(1f, 1f, 1f, 0.1f); // Very transparent white
            
            newSlots.Add(slotRect);
        }
        
        // Assign slots to CardHand
        targetHand.cardSlots = newSlots;
        
        // Mark scene as dirty
        EditorUtility.SetDirty(targetHand);
        EditorUtility.SetDirty(gameObject);
        
        Debug.Log($"Generated {slotCount} card slots for {targetHand.gameObject.name}");
    }
    
    [ContextMenu("Clear Slots")]
    public void ClearExistingSlots()
    {
        // Find and destroy existing CardSlots container
        Transform existingContainer = transform.Find("CardSlots");
        if (existingContainer != null)
        {
            DestroyImmediate(existingContainer.gameObject);
        }
        
        // Clear slot references
        if (targetHand != null)
        {
            targetHand.cardSlots.Clear();
            EditorUtility.SetDirty(targetHand);
        }
        
        Debug.Log("Cleared existing card slots");
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(CardSlotGenerator))]
public class CardSlotGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CardSlotGenerator generator = (CardSlotGenerator)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. Configure slot settings above\n" +
            "2. Click 'Generate Slots' to create card slots\n" +
            "3. Slots will be automatically assigned to CardHand",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Slots", GUILayout.Height(40)))
        {
            generator.GenerateSlots();
        }
        
        if (GUILayout.Button("Clear Slots"))
        {
            generator.ClearExistingSlots();
        }
    }
}
#endif

