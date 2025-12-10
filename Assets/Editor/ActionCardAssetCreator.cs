using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor utility to create ActionCardData ScriptableObject assets
/// Run from menu: Tools > Create Action Card Assets
/// </summary>
public class ActionCardAssetCreator : Editor
{
    private const string ASSET_PATH = "Assets/ScriptableObject/ActionCards/";
    
    [MenuItem("Tools/Create Action Card Assets")]
    public static void CreateActionCardAssets()
    {
        // Ensure directory exists
        if (!Directory.Exists(ASSET_PATH))
        {
            Directory.CreateDirectory(ASSET_PATH);
        }
        
        // Create ValuePlusOne
        CreateActionCard(
            "ValuePlusOne",
            ActionCardType.ValuePlusOne,
            "+1 Value",
            "Add +1 to any selected card in your hand. Cards cannot exceed value 10.",
            1,
            false,
            new Color(0.2f, 0.6f, 1f, 1f) // Blue
        );
        
        // Create MinorSwapWithDealer
        CreateActionCard(
            "MinorSwapWithDealer",
            ActionCardType.MinorSwapWithDealer,
            "Minor Swap",
            "Swap one of your cards with a dealer's face-up card. Select your card first, then the dealer's card.",
            1,
            false,
            new Color(0.8f, 0.4f, 0.8f, 1f) // Purple
        );
        
        // Create ShieldCard
        CreateActionCard(
            "ShieldCard",
            ActionCardType.ShieldCard,
            "Shield",
            "Protect a selected card from curses and boss effects for the remainder of the hand.",
            1,
            false,
            new Color(0.4f, 0.8f, 0.4f, 1f) // Green
        );
        
        // Create MinorHeal (limited uses across game)
        var minorHeal = CreateActionCard(
            "MinorHeal",
            ActionCardType.MinorHeal,
            "Minor Heal",
            "Restore 10 health points. Limited to 3 uses per entire game session.",
            1,
            false,
            new Color(1f, 0.5f, 0.5f, 1f) // Red/Pink
        );
        
        // Set limited game uses for Minor Heal
        if (minorHeal != null)
        {
            minorHeal.hasLimitedGameUses = true;
            minorHeal.maxGameUses = 3;
            EditorUtility.SetDirty(minorHeal);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[ActionCardAssetCreator] Created 4 action card assets in " + ASSET_PATH);
    }
    
    private static ActionCardData CreateActionCard(
        string fileName,
        ActionCardType actionType,
        string actionName,
        string description,
        int actionsRequired,
        bool canBeUsedMultipleTimes,
        Color cardColor)
    {
        string fullPath = ASSET_PATH + fileName + ".asset";
        
        // Check if asset already exists
        ActionCardData existingAsset = AssetDatabase.LoadAssetAtPath<ActionCardData>(fullPath);
        if (existingAsset != null)
        {
            Debug.Log($"[ActionCardAssetCreator] Asset already exists: {fileName}");
            return existingAsset;
        }
        
        // Create new asset
        ActionCardData newCard = ScriptableObject.CreateInstance<ActionCardData>();
        newCard.actionName = actionName;
        newCard.actionType = actionType;
        newCard.actionDescription = description;
        newCard.actionsRequired = actionsRequired;
        newCard.canBeUsedMultipleTimes = canBeUsedMultipleTimes;
        newCard.cardColor = cardColor;
        newCard.isStarterAction = true;
        newCard.unlockLevel = 0;
        
        // Save asset
        AssetDatabase.CreateAsset(newCard, fullPath);
        Debug.Log($"[ActionCardAssetCreator] Created: {fullPath}");
        
        return newCard;
    }
    
    [MenuItem("Tools/Setup ActionCardManager with Assets")]
    public static void SetupActionCardManager()
    {
        // Find or create ActionCardManager in scene
        ActionCardManager manager = FindObjectOfType<ActionCardManager>();
        
        if (manager == null)
        {
            Debug.LogWarning("[ActionCardAssetCreator] ActionCardManager not found in scene. Please add one first.");
            return;
        }
        
        // Clear existing list
        manager.allActionCards.Clear();
        
        // Load all action card assets
        string[] assetPaths = new string[]
        {
            ASSET_PATH + "ValuePlusOne.asset",
            ASSET_PATH + "MinorSwapWithDealer.asset",
            ASSET_PATH + "ShieldCard.asset",
            ASSET_PATH + "MinorHeal.asset"
        };
        
        foreach (string path in assetPaths)
        {
            ActionCardData card = AssetDatabase.LoadAssetAtPath<ActionCardData>(path);
            if (card != null)
            {
                manager.allActionCards.Add(card);
                Debug.Log($"[ActionCardAssetCreator] Added {card.actionName} to ActionCardManager");
            }
            else
            {
                Debug.LogWarning($"[ActionCardAssetCreator] Could not load asset: {path}");
            }
        }
        
        EditorUtility.SetDirty(manager);
        Debug.Log($"[ActionCardAssetCreator] ActionCardManager configured with {manager.allActionCards.Count} action cards");
    }
}

