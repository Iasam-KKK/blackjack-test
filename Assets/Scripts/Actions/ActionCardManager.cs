using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Manages equipped action cards and tracks limited-use actions across the game
/// Singleton that persists during gameplay
/// </summary>
public class ActionCardManager : MonoBehaviour
{
    public static ActionCardManager Instance { get; private set; }
    
    [Header("Configuration")]
    public const int MAX_EQUIPPED_CARDS = 4;
    private const string MINOR_HEAL_USES_KEY = "MinorHealRemainingUses";
    private const int DEFAULT_MINOR_HEAL_USES = 3;
    
    [Header("Equipped Action Cards")]
    [SerializeField] private List<ActionCardData> equippedCards = new List<ActionCardData>();
    
    [Header("All Available Action Cards")]
    [Tooltip("All action cards that can be equipped")]
    public List<ActionCardData> allActionCards = new List<ActionCardData>();
    
    [Header("UI References")]
    [Tooltip("Reference to the ActionCardWindowUI for displaying equipped cards during gameplay")]
    public ActionCardWindowUI actionCardWindow;
    
    [Header("Limited Use Tracking")]
    [SerializeField] private int minorHealRemainingUses;
    
    // Events
    public event Action<ActionCardData> OnActionCardEquipped;
    public event Action<ActionCardData> OnActionCardUnequipped;
    public event Action OnEquippedCardsChanged;
    public event Action<int> OnMinorHealUsesChanged;
    
    // Properties
    public List<ActionCardData> EquippedCards => new List<ActionCardData>(equippedCards);
    public int EquippedCount => equippedCards.Count;
    public bool CanEquipMore => equippedCards.Count < MAX_EQUIPPED_CARDS;
    public int MinorHealRemainingUses => minorHealRemainingUses;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMinorHealUses();
            Debug.Log("[ActionCardManager] Instance created");
        }
        else
        {
            Debug.LogWarning("[ActionCardManager] Duplicate instance detected, destroying");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Equip an action card to the active slots
    /// </summary>
    public bool EquipCard(ActionCardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("[ActionCardManager] Cannot equip null card");
            return false;
        }
        
        if (equippedCards.Count >= MAX_EQUIPPED_CARDS)
        {
            Debug.LogWarning($"[ActionCardManager] Cannot equip more cards. Max {MAX_EQUIPPED_CARDS} reached.");
            return false;
        }
        
        if (equippedCards.Contains(card))
        {
            Debug.LogWarning($"[ActionCardManager] Card '{card.actionName}' is already equipped");
            return false;
        }
        
        equippedCards.Add(card);
        Debug.Log($"[ActionCardManager] Equipped: {card.actionName} ({equippedCards.Count}/{MAX_EQUIPPED_CARDS})");
        
        OnActionCardEquipped?.Invoke(card);
        OnEquippedCardsChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// Unequip an action card from the active slots
    /// </summary>
    public bool UnequipCard(ActionCardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("[ActionCardManager] Cannot unequip null card");
            return false;
        }
        
        if (!equippedCards.Contains(card))
        {
            Debug.LogWarning($"[ActionCardManager] Card '{card.actionName}' is not equipped");
            return false;
        }
        
        equippedCards.Remove(card);
        Debug.Log($"[ActionCardManager] Unequipped: {card.actionName} ({equippedCards.Count}/{MAX_EQUIPPED_CARDS})");
        
        OnActionCardUnequipped?.Invoke(card);
        OnEquippedCardsChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// Unequip action card at specific index
    /// </summary>
    public bool UnequipCardAtIndex(int index)
    {
        if (index < 0 || index >= equippedCards.Count)
        {
            Debug.LogWarning($"[ActionCardManager] Invalid index {index}");
            return false;
        }
        
        ActionCardData card = equippedCards[index];
        return UnequipCard(card);
    }
    
    /// <summary>
    /// Check if a specific card is equipped
    /// </summary>
    public bool IsCardEquipped(ActionCardData card)
    {
        return card != null && equippedCards.Contains(card);
    }
    
    /// <summary>
    /// Get equipped card at index
    /// </summary>
    public ActionCardData GetEquippedCardAt(int index)
    {
        if (index >= 0 && index < equippedCards.Count)
        {
            return equippedCards[index];
        }
        return null;
    }
    
    /// <summary>
    /// Clear all equipped cards
    /// </summary>
    public void ClearEquippedCards()
    {
        equippedCards.Clear();
        Debug.Log("[ActionCardManager] All equipped cards cleared");
        OnEquippedCardsChanged?.Invoke();
    }
    
    // ============ MINOR HEAL TRACKING ============
    
    /// <summary>
    /// Use one Minor Heal charge
    /// </summary>
    public bool UseMinorHeal()
    {
        if (minorHealRemainingUses <= 0)
        {
            Debug.LogWarning("[ActionCardManager] No Minor Heal uses remaining!");
            return false;
        }
        
        minorHealRemainingUses--;
        SaveMinorHealUses();
        
        Debug.Log($"[ActionCardManager] Minor Heal used. Remaining: {minorHealRemainingUses}");
        OnMinorHealUsesChanged?.Invoke(minorHealRemainingUses);
        
        return true;
    }
    
    /// <summary>
    /// Check if Minor Heal can be used
    /// </summary>
    public bool CanUseMinorHeal()
    {
        return minorHealRemainingUses > 0;
    }
    
    /// <summary>
    /// Reset Minor Heal uses (for new game)
    /// </summary>
    public void ResetMinorHealUses()
    {
        minorHealRemainingUses = DEFAULT_MINOR_HEAL_USES;
        SaveMinorHealUses();
        Debug.Log($"[ActionCardManager] Minor Heal uses reset to {DEFAULT_MINOR_HEAL_USES}");
        OnMinorHealUsesChanged?.Invoke(minorHealRemainingUses);
    }
    
    /// <summary>
    /// Save Minor Heal uses to PlayerPrefs
    /// </summary>
    private void SaveMinorHealUses()
    {
        PlayerPrefs.SetInt(MINOR_HEAL_USES_KEY, minorHealRemainingUses);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Load Minor Heal uses from PlayerPrefs
    /// </summary>
    private void LoadMinorHealUses()
    {
        minorHealRemainingUses = PlayerPrefs.GetInt(MINOR_HEAL_USES_KEY, DEFAULT_MINOR_HEAL_USES);
        Debug.Log($"[ActionCardManager] Minor Heal uses loaded: {minorHealRemainingUses}");
    }
    
    // ============ UTILITY METHODS ============
    
    /// <summary>
    /// Get all available action cards that are not equipped
    /// </summary>
    public List<ActionCardData> GetUnequippedCards()
    {
        List<ActionCardData> unequipped = new List<ActionCardData>();
        foreach (var card in allActionCards)
        {
            if (!equippedCards.Contains(card))
            {
                unequipped.Add(card);
            }
        }
        return unequipped;
    }
    
    /// <summary>
    /// Get action card by type
    /// </summary>
    public ActionCardData GetCardByType(ActionCardType type)
    {
        foreach (var card in allActionCards)
        {
            if (card.actionType == type)
            {
                return card;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if an action card type is equipped
    /// </summary>
    public bool IsActionTypeEquipped(ActionCardType type)
    {
        foreach (var card in equippedCards)
        {
            if (card.actionType == type)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Reset all tracking for new game
    /// </summary>
    public void ResetForNewGame()
    {
        ClearEquippedCards();
        ResetMinorHealUses();
        RefreshWindow();
        Debug.Log("[ActionCardManager] Reset for new game");
    }
    
    /// <summary>
    /// Reset action cards for a new hand (resets per-hand usage tracking)
    /// </summary>
    public void ResetForNewHand()
    {
        // Refresh the window to reset slot states
        RefreshWindow();
        Debug.Log("[ActionCardManager] Reset for new hand");
    }
    
    /// <summary>
    /// Refresh the ActionCardWindowUI
    /// </summary>
    public void RefreshWindow()
    {
        // Find window if not assigned
        if (actionCardWindow == null)
        {
            actionCardWindow = FindObjectOfType<ActionCardWindowUI>();
        }
        
        if (actionCardWindow != null)
        {
            actionCardWindow.Refresh();
        }
    }
    
    /// <summary>
    /// Find and assign the ActionCardWindowUI at runtime
    /// </summary>
    public void FindWindowUI()
    {
        if (actionCardWindow == null)
        {
            actionCardWindow = FindObjectOfType<ActionCardWindowUI>();
            if (actionCardWindow != null)
            {
                Debug.Log("[ActionCardManager] Found ActionCardWindowUI");
            }
        }
    }
}

