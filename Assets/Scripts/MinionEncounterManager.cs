using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages minion encounters - handles the flow of fighting minions before boss
/// Player must defeat 2 out of 3 minions to unlock the boss battle
/// </summary>
public class MinionEncounterManager : MonoBehaviour
{
    public static MinionEncounterManager Instance { get; private set; }
    
    [Header("Current Encounter")]
    public MinionData currentMinion;
    public BossType currentBossType;
    public int currentMinionHealth;
    public bool isMinionActive = false;
    public bool isMinionDefeated = false;
    
    [Header("Encounter State")]
    public int currentHand = 0;
    public int handsRemaining = 0;
    
    [Header("Events")]
    public System.Action<MinionData> OnMinionDefeated;
    public System.Action OnMinionVictory;
    public System.Action<int> OnMinionHealthChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MinionEncounterManager] Instance created");
        }
        else
        {
            Debug.LogWarning($"[MinionEncounterManager] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Set the current minion for the next battle (called from map)
    /// </summary>
    public void SetCurrentMinion(MinionData minion)
    {
        if (minion == null)
        {
            Debug.LogError("[MinionEncounterManager] Cannot set null minion");
            return;
        }
        
        currentMinion = minion;
        isMinionActive = true;
        isMinionDefeated = false;
        
        Debug.Log($"[MinionEncounterManager] Current minion set: {minion.minionName}");
    }
    
    /// <summary>
    /// Initialize a minion encounter
    /// </summary>
    public void InitializeMinion(MinionData minion, BossType bossType)
    {
        if (minion == null)
        {
            Debug.LogError("[MinionEncounterManager] Cannot initialize null minion");
            return;
        }
        
        currentMinion = minion;
        currentBossType = bossType;
        currentMinionHealth = minion.maxHealth;
        handsRemaining = minion.handsPerRound;
        currentHand = 0;
        isMinionActive = true;
        isMinionDefeated = false;
        
        Debug.Log($"[MinionEncounterManager] Minion initialized: {minion.minionName}");
        Debug.Log($"[MinionEncounterManager]   Health: {currentMinionHealth}/{minion.maxHealth}");
        Debug.Log($"[MinionEncounterManager]   Hands: {handsRemaining}");
        Debug.Log($"[MinionEncounterManager]   Boss Type: {bossType}");
        Debug.Log($"[MinionEncounterManager]   isMinionActive: {isMinionActive}");
        
        OnMinionHealthChanged?.Invoke(currentMinionHealth);
    }
    
    /// <summary>
    /// Handle player winning a round against minion
    /// </summary>
    public void OnPlayerWinRound()
    {
        if (!isMinionActive || isMinionDefeated)
        {
            Debug.LogWarning("[MinionEncounterManager] Cannot process win - minion not active");
            return;
        }
        
        currentMinionHealth--;
        Debug.Log($"[MinionEncounterManager] Player wins round! Minion health: {currentMinionHealth}/{currentMinion.maxHealth}");
        
        OnMinionHealthChanged?.Invoke(currentMinionHealth);
        
        if (currentMinionHealth <= 0)
        {
            DefeatMinion();
        }
    }
    
    /// <summary>
    /// Handle minion winning a round
    /// </summary>
    public void OnMinionWinRound()
    {
        if (!isMinionActive || isMinionDefeated)
        {
            Debug.LogWarning("[MinionEncounterManager] Cannot process minion win - minion not active");
            return;
        }
        
        Debug.Log($"[MinionEncounterManager] Minion wins round!");
        // Minions don't have health, player just loses the round
    }
    
    /// <summary>
    /// Advance to next hand
    /// </summary>
    public void NextHand()
    {
        if (!isMinionActive) return;
        
        currentHand++;
        handsRemaining--;
        
        Debug.Log($"[MinionEncounterManager] Hand {currentHand}, {handsRemaining} hands remaining");
        
        if (handsRemaining <= 0 && !isMinionDefeated)
        {
            // Out of hands, minion wins
            MinionVictory();
        }
    }
    
    /// <summary>
    /// Defeat the current minion
    /// </summary>
    private void DefeatMinion()
    {
        if (currentMinion == null) return;
        
        Debug.Log($"[MinionEncounterManager] Minion {currentMinion.minionName} defeated!");
        
        isMinionActive = false;
        isMinionDefeated = true;
        
        // Mark minion as defeated in progression
        if (BossProgressionManager.Instance != null)
        {
            BossProgressionManager.Instance.MarkMinionDefeated(currentBossType, currentMinion.minionName);
            
            int minionCount = BossProgressionManager.Instance.GetMinionDefeatedCount(currentBossType);
            Debug.Log($"[MinionEncounterManager] Minions defeated for {currentBossType}: {minionCount}/3");
            
            if (minionCount >= 2)
            {
                Debug.Log($"[MinionEncounterManager] 2+ minions defeated - boss {currentBossType} unlocked!");
            }
        }
        
        OnMinionDefeated?.Invoke(currentMinion);
    }
    
    /// <summary>
    /// Minion wins (player runs out of hands)
    /// </summary>
    private void MinionVictory()
    {
        Debug.Log($"[MinionEncounterManager] Minion {currentMinion.minionName} wins - player out of hands!");
        
        isMinionActive = false;
        
        OnMinionVictory?.Invoke();
    }
    
    /// <summary>
    /// Check if minion has a specific mechanic
    /// </summary>
    public bool HasMechanic(BossMechanicType mechanicType)
    {
        if (currentMinion == null) return false;
        return currentMinion.HasMechanic(mechanicType);
    }
    
    /// <summary>
    /// Get minion mechanic
    /// </summary>
    public BossMechanic GetMechanic(BossMechanicType mechanicType)
    {
        if (currentMinion == null) return null;
        return currentMinion.GetMechanic(mechanicType);
    }
    
    /// <summary>
    /// Apply minion mechanic based on trigger
    /// </summary>
    public void ApplyMechanicOnTrigger(string triggerType)
    {
        if (currentMinion == null || !isMinionActive) return;
        
        foreach (var mechanic in currentMinion.mechanics)
        {
            bool shouldTrigger = false;
            
            switch (triggerType)
            {
                case "CardDealt":
                    shouldTrigger = mechanic.triggersOnCardDealt;
                    break;
                case "PlayerAction":
                    shouldTrigger = mechanic.triggersOnPlayerAction;
                    break;
                case "RoundEnd":
                    shouldTrigger = mechanic.triggersOnRoundEnd;
                    break;
            }
            
            if (shouldTrigger && Random.value <= mechanic.activationChance)
            {
                Debug.Log($"[MinionEncounterManager] Triggering mechanic: {mechanic.mechanicName}");
                ExecuteMechanic(mechanic);
            }
        }
    }
    
    /// <summary>
    /// Execute a minion mechanic
    /// </summary>
    private void ExecuteMechanic(BossMechanic mechanic)
    {
        // This is a placeholder - actual mechanic execution will be handled by BossManager
        // or integrated into the game flow
        Debug.Log($"[MinionEncounterManager] Executing mechanic: {mechanic.mechanicName} ({mechanic.mechanicType})");
        
        // The actual implementation will depend on your existing game systems
        // For now, we just log it and let BossManager handle the mechanics
    }
    
    /// <summary>
    /// Get current minion info string
    /// </summary>
    public string GetMinionInfoString()
    {
        if (currentMinion == null) return "No active minion";
        
        return $"{currentMinion.minionName}\nHealth: {currentMinionHealth}/{currentMinion.maxHealth}\nHands: {handsRemaining}";
    }
    
    /// <summary>
    /// Check if current minion disables tarot cards
    /// </summary>
    public bool DisablesTarotCards()
    {
        if (currentMinion == null) return false;
        return currentMinion.disablesTarotCards;
    }
    
    /// <summary>
    /// Reset minion encounter state
    /// </summary>
    public void ResetEncounter()
    {
        currentMinion = null;
        currentMinionHealth = 0;
        currentHand = 0;
        handsRemaining = 0;
        isMinionActive = false;
        isMinionDefeated = false;
        
        Debug.Log("[MinionEncounterManager] Encounter reset");
    }
}

