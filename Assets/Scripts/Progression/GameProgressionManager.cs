using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Map;

/// <summary>
/// SINGLE SOURCE OF TRUTH for all game state
/// Consolidates: BossProgressionManager, MinionEncounterManager, PlayerHealthManager
/// </summary>
public class GameProgressionManager : MonoBehaviour
{
    // Force recompilation to fix recognition issues
    public static GameProgressionManager Instance { get; private set; }
    
    /// <summary>
    /// Set the instance (for recovery scenarios)
    /// </summary>
    public static void SetInstance(GameProgressionManager instance)
    {
        if (Instance == null)
        {
            Instance = instance;
            Debug.Log("[GameProgressionManager] Instance set via SetInstance method");
        }
        else
        {
            Debug.LogWarning("[GameProgressionManager] Instance already exists, ignoring SetInstance call");
        }
    }
    
    [Header("Configuration")]
    public List<BossData> allBosses = new List<BossData>();
    public List<MinionData> allMinions = new List<MinionData>();
    
    // Minion-Boss associations are now handled directly in MinionData.associatedBossType
    
    [Header("Player State")]
    [Range(0f, 100f)]
    public float playerHealthPercentage = 100f;
    public float maxHealthPercentage = 100f;
    public float damagePerLoss = 10f;
    
    [Header("Current Encounter State")]
    public MinionData currentMinion;
    public BossData currentBoss;
    public BossType currentBossType;
    public int currentEncounterHealth;
    public bool isEncounterActive = false;
    public bool isMinion = false; // true = minion battle, false = boss battle
    public string currentNodeInstanceId = ""; // Track which node instance we're fighting
    
    [Header("Persistence")]
    public bool enablePersistence = true;
    private const string SAVE_KEY = "GameProgression_v2";
    
    [Header("Events")]
    public Action<float> OnPlayerHealthChanged;
    public Action OnPlayerGameOver;
    public Action<MinionData> OnMinionDefeated;
    public Action<BossData> OnBossDefeated;
    public Action<int> OnEncounterHealthChanged;
    
    // Serializable save data
    private GameProgressionData progressionData;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameProgressionManager] Instance created - SINGLE SOURCE OF TRUTH");
            
            LoadProgression();
            LoadAllBossData();
        }
        else
        {
            Debug.LogWarning($"[GameProgressionManager] Duplicate instance detected, destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadAllMinionData();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameProgressionManager] Scene loaded: {scene.name}");
        
        // Update UI when scene loads
        UpdatePlayerHealthUI();
        
        // If we're in Blackjack scene and have an active encounter, ensure it's properly initialized
        if (scene.name == "Blackjack" && isEncounterActive)
        {
            Debug.Log($"[GameProgressionManager] Blackjack scene loaded with active encounter: {(isMinion ? "Minion" : "Boss")}");
            Debug.Log($"[GameProgressionManager] Current encounter health: {currentEncounterHealth}");
            
            // Setup shop for minion/boss encounters
            SetupShopForEncounter();
        }
    }
    
    /// <summary>
    /// Setup shop when entering battle scene
    /// </summary>
    private void SetupShopForEncounter()
    {
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null)
        {
            if (isMinion && currentMinion != null)
            {
                Debug.Log($"[GameProgressionManager] Setting up shop for minion: {currentMinion.minionName} (disablesTarotCards: {currentMinion.disablesTarotCards})");
            }
            else if (!isMinion && currentBoss != null)
            {
                Debug.Log($"[GameProgressionManager] Setting up shop for boss: {currentBoss.bossName} (allowTarotCards: {currentBoss.allowTarotCards})");
            }
            
            shopManager.SetupShop(); // ShopManager will check minion.disablesTarotCards and boss.allowTarotCards internally
        }
        else
        {
            Debug.LogWarning("[GameProgressionManager] ShopManager not found in scene - tarot cards won't be available");
        }
    }
    
    /// <summary>
    /// Update player health UI across all scenes
    /// </summary>
    private void UpdatePlayerHealthUI()
    {
        // Find and update player health UI
        Text healthTextComponent = null;
        var healthTextObj = GameObject.Find("PlayerHealthText");
        if (healthTextObj != null)
        {
            healthTextComponent = healthTextObj.GetComponent<Text>();
        }
        
        if (healthTextComponent == null)
        {
            // Try finding as child of PlayerHealthPanel
            var panel = GameObject.Find("PlayerHealthPanel");
            if (panel != null)
            {
                healthTextComponent = panel.GetComponentInChildren<Text>();
            }
        }
        
        if (healthTextComponent != null)
        {
            healthTextComponent.text = $"Player Health {playerHealthPercentage:F0}%";
            Debug.Log($"[GameProgressionManager] Updated player health UI: {playerHealthPercentage:F0}%");
        }
        else
        {
            Debug.LogWarning("[GameProgressionManager] Could not find player health UI to update");
        }
        
        // Update health bar if it exists
        var healthBar = GameObject.Find("PlayerHealthBar");
        if (healthBar != null)
        {
            var image = healthBar.GetComponent<Image>();
            if (image != null)
            {
                image.fillAmount = playerHealthPercentage / 100f;
                Debug.Log($"[GameProgressionManager] Updated player health bar: {playerHealthPercentage:F0}%");
            }
        }
    }
    
    // ============================================================================
    // PLAYER HEALTH
    // ============================================================================
    
    public float GetPlayerHealth() => playerHealthPercentage;
    public bool IsPlayerAlive() => playerHealthPercentage > 0f;
    
    public void DamagePlayer(float damage)
    {
        float previousHealth = playerHealthPercentage;
        playerHealthPercentage = Mathf.Max(0f, playerHealthPercentage - damage);
        
        Debug.Log($"[GameProgressionManager] Player takes {damage} damage. Health: {previousHealth}% -> {playerHealthPercentage}%");
        
        // Update UI immediately
        UpdatePlayerHealthUI();
        
        SaveProgression();
        OnPlayerHealthChanged?.Invoke(playerHealthPercentage);
        
        if (playerHealthPercentage <= 0f && previousHealth > 0f)
        {
            TriggerGameOver();
        }
    }
    
    public void HealPlayer(float amount)
    {
        float previousHealth = playerHealthPercentage;
        playerHealthPercentage = Mathf.Min(maxHealthPercentage, playerHealthPercentage + amount);
        
        Debug.Log($"[GameProgressionManager] Player healed {amount}. Health: {previousHealth}% -> {playerHealthPercentage}%");
        
        SaveProgression();
        OnPlayerHealthChanged?.Invoke(playerHealthPercentage);
    }
    
    public void RestorePlayerHealth()
    {
        playerHealthPercentage = maxHealthPercentage;
        SaveProgression();
        OnPlayerHealthChanged?.Invoke(playerHealthPercentage);
        Debug.Log("[GameProgressionManager] Player health restored to full");
    }
    
    private void TriggerGameOver()
    {
        Debug.Log("[GameProgressionManager] GAME OVER - Player health depleted!");
        OnPlayerGameOver?.Invoke();
        
        ResetProgression();
        StartCoroutine(ReturnToMainMenuAfterDelay());
    }
    
    private System.Collections.IEnumerator ReturnToMainMenuAfterDelay()
    {
        // Use unscaled time so it works even when Time.timeScale = 0
        yield return new WaitForSecondsRealtime(3f);
        
        Debug.Log("[GameProgressionManager] Returning to main menu after game over");
        Time.timeScale = 1f; // Resume time before loading scene
        SceneManager.LoadScene("MainMenu");
    }
    
    // ============================================================================
    // ENCOUNTER MANAGEMENT (Minion/Boss)
    // ============================================================================
    
    public void StartMinionEncounter(MinionData minion, BossType bossType, string nodeInstanceId = "")
    {
        Debug.Log($"[GameProgressionManager] StartMinionEncounter called with minion: {minion?.minionName}, bossType: {bossType}, nodeInstanceId: {nodeInstanceId}");
        
        if (minion == null)
        {
            Debug.LogError("[GameProgressionManager] Cannot start null minion encounter");
            return;
        }
        
        // Validate minion data
        if (!ValidateMinionData(minion, bossType))
        {
            Debug.LogError($"[GameProgressionManager] Invalid minion data for {minion.minionName}");
            return;
        }
        
        currentMinion = minion;
        currentBoss = null;
        currentBossType = bossType;
        currentEncounterHealth = minion.maxHealth;
        isEncounterActive = true;
        isMinion = true;
        currentNodeInstanceId = nodeInstanceId;
        
        Debug.Log($"[GameProgressionManager] Minion encounter started: {minion.minionName}");
        Debug.Log($"  Health: {currentEncounterHealth}/{minion.maxHealth}");
        Debug.Log($"  Boss Type: {bossType}");
        Debug.Log($"  Node Instance ID: {nodeInstanceId}");
        Debug.Log($"  Portrait: {(minion.minionPortrait != null ? minion.minionPortrait.name : "NULL")}");
        Debug.Log($"  Mechanics: {minion.mechanics.Count}");
        Debug.Log($"[GameProgressionManager] Encounter state set - isEncounterActive: {isEncounterActive}, isMinion: {isMinion}");
        
        OnEncounterHealthChanged?.Invoke(currentEncounterHealth);
        
        // Force refresh Inspector values
        LogCurrentState();
    }
    
    /// <summary>
    /// Log current state for debugging Inspector issues
    /// </summary>
    public void LogCurrentState()
    {
        Debug.Log("=== GAMEPROGRESSION MANAGER STATE ===");
        Debug.Log($"Player Health: {playerHealthPercentage}%");
        Debug.Log($"Is Encounter Active: {isEncounterActive}");
        Debug.Log($"Is Minion: {isMinion}");
        Debug.Log($"Current Boss: {(currentBoss != null ? currentBoss.bossName : "None")}");
        Debug.Log($"Current Minion: {(currentMinion != null ? currentMinion.minionName : "None")}");
        Debug.Log($"Current Boss Type: {currentBossType}");
        Debug.Log($"Current Encounter Health: {currentEncounterHealth}");
        Debug.Log("=== END STATE ===");
    }
    
    /// <summary>
    /// Validate minion data before starting encounter
    /// </summary>
    private bool ValidateMinionData(MinionData minion, BossType bossType)
    {
        bool isValid = true;
        
        // Check if minion exists in our loaded data
        var loadedMinion = GetMinionData(minion.minionName, bossType);
        if (loadedMinion == null)
        {
            Debug.LogWarning($"[GameProgressionManager] Minion {minion.minionName} not found in loaded minion data for boss {bossType}");
            // Still allow it, but log warning
        }
        
        // Check for required fields
        if (string.IsNullOrEmpty(minion.minionName))
        {
            Debug.LogError("[GameProgressionManager] Minion has no name!");
            isValid = false;
        }
        
        if (minion.maxHealth <= 0)
        {
            Debug.LogError($"[GameProgressionManager] Minion {minion.minionName} has invalid health: {minion.maxHealth}");
            isValid = false;
        }
        
        if (minion.handsPerRound <= 0)
        {
            Debug.LogError($"[GameProgressionManager] Minion {minion.minionName} has invalid hands per round: {minion.handsPerRound}");
            isValid = false;
        }
        
        if (minion.minionPortrait == null)
        {
            Debug.LogWarning($"[GameProgressionManager] Minion {minion.minionName} has no portrait assigned!");
        }
        
        if (minion.associatedBossType != bossType)
        {
            Debug.LogWarning($"[GameProgressionManager] Minion {minion.minionName} boss type mismatch: {minion.associatedBossType} vs {bossType}");
            Debug.LogWarning($"[GameProgressionManager] Please update the minion's associatedBossType in the ScriptableObject");
        }
        
        return isValid;
    }
    
    public void StartBossEncounter(BossData boss)
    {
        if (boss == null)
        {
            Debug.LogError("[GameProgressionManager] Cannot start null boss encounter");
            return;
        }
        
        currentBoss = boss;
        currentMinion = null;
        currentBossType = boss.bossType;
        currentEncounterHealth = boss.maxHealth;
        isEncounterActive = true;
        isMinion = false;
        
        Debug.Log($"[GameProgressionManager] Boss encounter started: {boss.bossName}");
        Debug.Log($"  Health: {currentEncounterHealth}/{boss.maxHealth}");
        
        OnEncounterHealthChanged?.Invoke(currentEncounterHealth);
    }
    
    public void OnPlayerWinRound()
    {
        if (!isEncounterActive)
        {
            Debug.LogWarning("[GameProgressionManager] Cannot process win - no active encounter");
            return;
        }
        
        currentEncounterHealth--;
        
        Debug.Log($"[GameProgressionManager] Player wins round! Encounter health: {currentEncounterHealth}");
        
        OnEncounterHealthChanged?.Invoke(currentEncounterHealth);
        
        if (currentEncounterHealth <= 0)
        {
            if (isMinion)
            {
                CompleteMinionEncounter(true);
            }
            else
            {
                CompleteBossEncounter(true);
            }
        }
    }
    
    public void OnPlayerLoseRound()
    {
        if (!isEncounterActive)
        {
            Debug.LogWarning("[GameProgressionManager] Cannot process loss - no active encounter");
            return;
        }
        
        Debug.Log($"[GameProgressionManager] Player loses round!");
        
        // Damage player health
        DamagePlayer(damagePerLoss);
        
        // Note: Game over is already triggered in DamagePlayer() if health <= 0
        // Do NOT complete the encounter here - let TriggerGameOver() handle it
        // If player is still alive, continue the encounter
    }
    
    private void CompleteMinionEncounter(bool playerWon)
    {
        if (currentMinion == null) return;
        
        Debug.Log($"[GameProgressionManager] Minion encounter complete: {currentMinion.minionName}, Player won: {playerWon}");
        
        if (playerWon)
        {
            MarkMinionDefeated(currentBossType, currentMinion.minionName);
            OnMinionDefeated?.Invoke(currentMinion);
            
            // Mark the specific node instance as defeated
            if (!string.IsNullOrEmpty(currentNodeInstanceId))
            {
                MarkNodeInstanceDefeated(currentNodeInstanceId);
            }
            
            // Log minion statistics after defeat
            LogMinionStatistics();
        }
        
        ResetEncounter();
        
        // Return to map scene after encounter completion
        ReturnToMapScene();
    }
    
    private void CompleteBossEncounter(bool playerWon)
    {
        if (currentBoss == null) return;
        
        Debug.Log($"[GameProgressionManager] Boss encounter complete: {currentBoss.bossName}, Player won: {playerWon}");
        
        if (playerWon)
        {
            MarkBossDefeated(currentBoss.bossType);
            CompleteAct(currentBoss.bossType);
            OnBossDefeated?.Invoke(currentBoss);
        }
        
        ResetEncounter();
        
        // Return to map scene after encounter completion
        ReturnToMapScene();
    }
    
    private void ResetEncounter()
    {
        currentMinion = null;
        currentBoss = null;
        currentEncounterHealth = 0;
        isEncounterActive = false;
        isMinion = false;
        currentNodeInstanceId = "";
        
        Debug.Log("[GameProgressionManager] Encounter reset");
    }
    
    /// <summary>
    /// Return to map scene after encounter completion
    /// </summary>
    private void ReturnToMapScene()
    {
        Debug.Log("[GameProgressionManager] Returning to map scene after encounter completion");
        
        // Use MapReturnHandler if available
        if (Map.MapReturnHandler.Instance != null)
        {
            Map.MapReturnHandler.Instance.ReturnToMap();
        }
        else
        {
            // Fallback: directly load map scene
            Debug.LogWarning("[GameProgressionManager] MapReturnHandler not found, loading map scene directly");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
        }
    }
    
    // ============================================================================
    // BOSS PROGRESSION
    // ============================================================================
    
    public bool IsBossUnlocked(BossType bossType)
    {
        return progressionData.unlockedBosses.Contains(bossType.ToString());
    }
    
    public bool IsBossDefeated(BossType bossType)
    {
        return progressionData.defeatedBosses.Contains(bossType.ToString());
    }
    
    public void UnlockBoss(BossType bossType)
    {
        if (!IsBossUnlocked(bossType))
        {
            progressionData.unlockedBosses.Add(bossType.ToString());
            SaveProgression();
            Debug.Log($"[GameProgressionManager] Boss unlocked: {bossType}");
        }
    }
    
    public void MarkBossDefeated(BossType bossType)
    {
        if (!IsBossDefeated(bossType))
        {
            progressionData.defeatedBosses.Add(bossType.ToString());
            SaveProgression();
            Debug.Log($"[GameProgressionManager] Boss defeated: {bossType}");
        }
    }
    
    public void SelectBoss(BossType bossType)
    {
        if (!IsBossUnlocked(bossType))
        {
            Debug.LogWarning($"[GameProgressionManager] Cannot select locked boss: {bossType}");
            return;
        }
        
        progressionData.selectedBossType = bossType.ToString();
        SaveProgression();
        Debug.Log($"[GameProgressionManager] Boss selected: {bossType}");
    }
    
    public void ClearSelectedBoss()
    {
        progressionData.selectedBossType = "";
        SaveProgression();
        Debug.Log("[GameProgressionManager] Selected boss cleared");
    }
    
    public BossType? GetSelectedBoss()
    {
        if (string.IsNullOrEmpty(progressionData.selectedBossType))
            return null;
            
        return (BossType)Enum.Parse(typeof(BossType), progressionData.selectedBossType);
    }
    
    public BossData GetBossData(BossType bossType)
    {
        return allBosses.Find(b => b.bossType == bossType);
    }
    
    public List<BossData> GetUnlockedBosses()
    {
        return allBosses.Where(b => IsBossUnlocked(b.bossType)).ToList();
    }
    
    public List<BossData> GetDefeatedBosses()
    {
        return allBosses.Where(b => IsBossDefeated(b.bossType)).ToList();
    }
    
    public List<BossData> GetAvailableBosses()
    {
        return allBosses.Where(b => IsBossUnlocked(b.bossType) && !IsBossDefeated(b.bossType)).ToList();
    }
    
    // ============================================================================
    // MINION MANAGEMENT & PROGRESSION
    // ============================================================================
    
    /// <summary>
    /// Get all minions for a specific boss
    /// </summary>
    public List<MinionData> GetMinionsForBoss(BossType bossType)
    {
        return allMinions.Where(m => m.associatedBossType == bossType).ToList();
    }
    
    /// <summary>
    /// Get minion data by name
    /// </summary>
    public MinionData GetMinionData(string minionName)
    {
        return allMinions.Find(m => m.minionName == minionName);
    }
    
    /// <summary>
    /// Get minion data by name and boss type (more specific)
    /// </summary>
    public MinionData GetMinionData(string minionName, BossType bossType)
    {
        return allMinions.Find(m => m.minionName == minionName && m.associatedBossType == bossType);
    }
    
    /// <summary>
    /// Get minion data from map node blueprint
    /// </summary>
    public MinionData GetMinionDataFromMapNode(MapNode mapNode)
    {
        if (mapNode?.Blueprint?.minionData == null)
        {
            Debug.LogError("[GameProgressionManager] MapNode or Blueprint or minionData is null");
            return null;
        }
        
        var minionData = mapNode.Blueprint.minionData;
        var bossType = mapNode.Blueprint.bossType;
        
        // Validate that the minion is associated with the correct boss
        if (minionData.associatedBossType != bossType)
        {
            Debug.LogWarning($"[GameProgressionManager] Minion {minionData.minionName} boss type mismatch: {minionData.associatedBossType} vs {bossType}");
            Debug.LogWarning($"[GameProgressionManager] Please update the minion's associatedBossType in the ScriptableObject");
        }
        
        // Try to find the minion in our loaded data for validation
        var loadedMinion = GetMinionData(minionData.minionName, bossType);
        if (loadedMinion != null)
        {
            Debug.Log($"[GameProgressionManager] Found minion in loaded data: {loadedMinion.minionName}");
            return loadedMinion; // Use the loaded version for consistency
        }
        else
        {
            Debug.LogWarning($"[GameProgressionManager] Minion {minionData.minionName} not found in loaded data, using blueprint data");
            return minionData; // Fallback to blueprint data
        }
    }
    
    /// <summary>
    /// Check if a specific minion is defeated
    /// </summary>
    public bool IsMinionDefeated(string minionName, BossType bossType)
    {
        var actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        bool result = actState?.defeatedMinions.Contains(minionName) ?? false;
        
        // Debug logging
        if (actState != null)
        {
            Debug.Log($"[GameProgressionManager] IsMinionDefeated check: {minionName} for {bossType}");
            Debug.Log($"[GameProgressionManager] Defeated minions for {bossType}: {string.Join(", ", actState.defeatedMinions)}");
            Debug.Log($"[GameProgressionManager] Result: {result}");
        }
        else
        {
            Debug.Log($"[GameProgressionManager] IsMinionDefeated check: No act state found for {bossType}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Check if a specific minion is defeated (by name only)
    /// </summary>
    public bool IsMinionDefeated(string minionName)
    {
        foreach (var actState in progressionData.actStates)
        {
            if (actState.defeatedMinions.Contains(minionName))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get all defeated minions across all bosses
    /// </summary>
    public List<string> GetAllDefeatedMinions()
    {
        List<string> allDefeated = new List<string>();
        foreach (var actState in progressionData.actStates)
        {
            allDefeated.AddRange(actState.defeatedMinions);
        }
        return allDefeated;
    }
    
    /// <summary>
    /// Get minion defeat count for a specific boss
    /// </summary>
    public int GetMinionDefeatedCount(BossType bossType)
    {
        var actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        return actState?.defeatedMinions.Count ?? 0;
    }
    
    /// <summary>
    /// Get remaining minions for a boss (not yet defeated)
    /// </summary>
    public List<MinionData> GetRemainingMinionsForBoss(BossType bossType)
    {
        var allBossMinions = GetMinionsForBoss(bossType);
        var defeatedMinions = GetDefeatedMinions(bossType);
        
        return allBossMinions.Where(m => !defeatedMinions.Contains(m.minionName)).ToList();
    }
    
    /// <summary>
    /// Get defeated minions for a specific boss
    /// </summary>
    public List<string> GetDefeatedMinions(BossType bossType)
    {
        var actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        return actState?.defeatedMinions ?? new List<string>();
    }
    
    /// <summary>
    /// Get minion defeat progress for a boss (e.g., "2/3 minions defeated")
    /// </summary>
    public string GetMinionProgressString(BossType bossType)
    {
        var totalMinions = GetMinionsForBoss(bossType).Count;
        var defeatedCount = GetMinionDefeatedCount(bossType);
        return $"{defeatedCount}/{totalMinions} minions defeated";
    }
    
    /// <summary>
    /// Check if boss is unlocked based on minion defeats
    /// </summary>
    public bool IsBossUnlockedByMinions(BossType bossType)
    {
        var defeatedCount = GetMinionDefeatedCount(bossType);
        return defeatedCount >= 2; // 2+ minions defeated unlocks boss
    }
    
    /// <summary>
    /// Get all available minions (not yet defeated) for a boss
    /// </summary>
    public List<MinionData> GetAvailableMinionsForBoss(BossType bossType)
    {
        var allBossMinions = GetMinionsForBoss(bossType);
        var defeatedMinions = GetDefeatedMinions(bossType);
        
        return allBossMinions.Where(m => !defeatedMinions.Contains(m.minionName)).ToList();
    }
    
    /// <summary>
    /// Get minion completion percentage for a boss
    /// </summary>
    public float GetMinionCompletionPercentage(BossType bossType)
    {
        var totalMinions = GetMinionsForBoss(bossType).Count;
        if (totalMinions == 0) return 0f;
        
        var defeatedCount = GetMinionDefeatedCount(bossType);
        return (float)defeatedCount / totalMinions * 100f;
    }
    
    /// <summary>
    /// Check if all minions for a boss are defeated
    /// </summary>
    public bool AreAllMinionsDefeated(BossType bossType)
    {
        var totalMinions = GetMinionsForBoss(bossType).Count;
        var defeatedCount = GetMinionDefeatedCount(bossType);
        return totalMinions > 0 && defeatedCount >= totalMinions;
    }
    
    /// <summary>
    /// Get minion statistics for debugging
    /// </summary>
    public void LogMinionStatistics()
    {
        Debug.Log("=== MINION STATISTICS ===");
        
        foreach (var boss in allBosses)
        {
            var bossMinions = GetMinionsForBoss(boss.bossType);
            var defeatedCount = GetMinionDefeatedCount(boss.bossType);
            var progress = GetMinionCompletionPercentage(boss.bossType);
            
            Debug.Log($"Boss {boss.bossName}: {defeatedCount}/{bossMinions.Count} minions defeated ({progress:F1}%)");
            
            foreach (var minion in bossMinions)
            {
                bool isDefeated = IsMinionDefeated(minion.minionName, boss.bossType);
                Debug.Log($"  - {minion.minionName}: {(isDefeated ? "DEFEATED" : "Available")}");
            }
        }
        
        Debug.Log("=== END MINION STATISTICS ===");
    }
    
    /// <summary>
    /// Get comprehensive minion configuration summary
    /// </summary>
    public string GetMinionConfigurationSummary()
    {
        System.Text.StringBuilder summary = new System.Text.StringBuilder();
        summary.AppendLine("=== MINION CONFIGURATION SUMMARY ===");
        summary.AppendLine($"Total Minions Loaded: {allMinions.Count}");
        summary.AppendLine();
        
        foreach (var boss in allBosses)
        {
            var bossMinions = GetMinionsForBoss(boss.bossType);
            summary.AppendLine($"Boss: {boss.bossName} ({boss.bossType})");
            summary.AppendLine($"  Minions: {bossMinions.Count}");
            summary.AppendLine($"  Defeated: {GetMinionDefeatedCount(boss.bossType)}");
            summary.AppendLine($"  Progress: {GetMinionProgressString(boss.bossType)}");
            summary.AppendLine($"  Completion: {GetMinionCompletionPercentage(boss.bossType):F1}%");
            
            foreach (var minion in bossMinions)
            {
                bool isDefeated = IsMinionDefeated(minion.minionName, boss.bossType);
                summary.AppendLine($"    - {minion.minionName}: {(isDefeated ? "DEFEATED" : "Available")} (Health: {minion.maxHealth}, Hands: {minion.handsPerRound})");
            }
            summary.AppendLine();
        }
        
        summary.AppendLine("=== END MINION CONFIGURATION ===");
        return summary.ToString();
    }
    
    public void StartBossAct(BossType bossType)
    {
        var actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        if (actState == null)
        {
            actState = new ActState
            {
                bossType = bossType.ToString(),
                defeatedMinions = new List<string>(),
                bossUnlockedInAct = false,
                actStartedAt = DateTime.Now.ToString("o")
            };
            progressionData.actStates.Add(actState);
        }
        
        progressionData.currentActBoss = bossType.ToString();
        SaveProgression();
        
        Debug.Log($"[GameProgressionManager] Started act for boss: {bossType}");
        
        // Log minion statistics for this boss
        var bossMinions = GetMinionsForBoss(bossType);
        Debug.Log($"[GameProgressionManager] Boss {bossType} has {bossMinions.Count} minions available");
    }
    
    public void MarkMinionDefeated(BossType bossType, string minionName)
    {
        var actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        if (actState == null)
        {
            StartBossAct(bossType);
            actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        }
        
        if (actState != null && !actState.defeatedMinions.Contains(minionName))
        {
            actState.defeatedMinions.Add(minionName);
            
            // Get minion data for additional info
            var minionData = GetMinionData(minionName, bossType);
            string minionInfo = minionData != null ? $" ({minionData.minionName})" : "";
            
            // Check if 2+ minions defeated to unlock boss
            if (actState.defeatedMinions.Count >= 2 && !actState.bossUnlockedInAct)
            {
                actState.bossUnlockedInAct = true;
                UnlockBoss(bossType);
                Debug.Log($"[GameProgressionManager] 2+ minions defeated - boss {bossType} unlocked!");
            }
            
            SaveProgression();
            
            // Log detailed progress
            var totalMinions = GetMinionsForBoss(bossType).Count;
            var progress = GetMinionCompletionPercentage(bossType);
            Debug.Log($"[GameProgressionManager] Minion defeated: {minionName}{minionInfo} for boss {bossType}");
            Debug.Log($"[GameProgressionManager] Progress: {actState.defeatedMinions.Count}/{totalMinions} minions defeated ({progress:F1}%)");
            
            // Check if all minions defeated
            if (AreAllMinionsDefeated(bossType))
            {
                Debug.Log($"[GameProgressionManager] ALL minions defeated for boss {bossType}!");
            }
        }
        else if (actState != null && actState.defeatedMinions.Contains(minionName))
        {
            Debug.LogWarning($"[GameProgressionManager] Minion {minionName} already marked as defeated for boss {bossType}");
        }
    }
    
    public void CompleteAct(BossType bossType)
    {
        var actState = progressionData.actStates.Find(a => a.bossType == bossType.ToString());
        if (actState != null)
        {
            actState.actCompletedAt = DateTime.Now.ToString("o");
            SaveProgression();
            Debug.Log($"[GameProgressionManager] Act completed for boss: {bossType}");
        }
        
        // Clear current act - commented out as it might cause issues with minion battles after boss defeat
        // progressionData.currentActBoss = "";
        // SaveProgression();
        
        Debug.Log($"[GameProgressionManager] Act completed but keeping currentActBoss for minion battles");
    }
    
    // ============================================================================
    // NODE INSTANCE TRACKING
    // ============================================================================
    
    /// <summary>
    /// Check if a specific node instance is defeated
    /// </summary>
    public bool IsNodeInstanceDefeated(string nodeInstanceId)
    {
        return progressionData.defeatedNodeInstances.Contains(nodeInstanceId);
    }
    
    /// <summary>
    /// Mark a specific node instance as defeated
    /// </summary>
    public void MarkNodeInstanceDefeated(string nodeInstanceId)
    {
        if (!progressionData.defeatedNodeInstances.Contains(nodeInstanceId))
        {
            progressionData.defeatedNodeInstances.Add(nodeInstanceId);
            SaveProgression();
            Debug.Log($"[GameProgressionManager] Node instance defeated: {nodeInstanceId}");
        }
    }
    
    /// <summary>
    /// Get all defeated node instances
    /// </summary>
    public List<string> GetDefeatedNodeInstances()
    {
        return new List<string>(progressionData.defeatedNodeInstances);
    }
    
    /// <summary>
    /// Clear all defeated node instances (for testing/debugging)
    /// </summary>
    public void ClearDefeatedNodeInstances()
    {
        progressionData.defeatedNodeInstances.Clear();
        SaveProgression();
        Debug.Log("[GameProgressionManager] All defeated node instances cleared");
    }
    
    // ============================================================================
    // PERSISTENCE
    // ============================================================================
    
    private void LoadAllBossData()
    {
        if (allBosses.Count > 0) return;
        
        BossData[] bossDataArray1 = Resources.LoadAll<BossData>("ScriptableObjectsBosses");
        BossData[] bossDataArray2 = Resources.LoadAll<BossData>("ScriptableObject");
        
        List<BossData> allBossData = new List<BossData>();
        if (bossDataArray1 != null) allBossData.AddRange(bossDataArray1);
        if (bossDataArray2 != null) allBossData.AddRange(bossDataArray2);
        
        if (allBossData.Count > 0)
        {
            allBosses.AddRange(allBossData);
            Debug.Log($"[GameProgressionManager] Loaded {allBosses.Count} boss data files");
        }
    }
    
    private void LoadAllMinionData()
    {
        if (allMinions.Count > 0) return;
        
        // Load from multiple possible locations
        MinionData[] minionDataArray1 = Resources.LoadAll<MinionData>("ScriptableObjectsBosses");
        MinionData[] minionDataArray2 = Resources.LoadAll<MinionData>("ScriptableObject");
        MinionData[] minionDataArray3 = Resources.LoadAll<MinionData>("Minions");
        
        List<MinionData> allMinionData = new List<MinionData>();
        if (minionDataArray1 != null) allMinionData.AddRange(minionDataArray1);
        if (minionDataArray2 != null) allMinionData.AddRange(minionDataArray2);
        if (minionDataArray3 != null) allMinionData.AddRange(minionDataArray3);
        
        if (allMinionData.Count > 0)
        {
            allMinions.AddRange(allMinionData);
            Debug.Log($"[GameProgressionManager] Loaded {allMinions.Count} minion data files");
            
            // Log all loaded minions for verification
            foreach (var minion in allMinions)
            {
                Debug.Log($"[GameProgressionManager] Minion: {minion.minionName} (Boss: {minion.associatedBossType}, Health: {minion.maxHealth}, Hands: {minion.handsPerRound})");
            }
        }
        else
        {
            Debug.LogWarning("[GameProgressionManager] No minion data found! Please ensure MinionData ScriptableObjects are in Resources folders.");
        }
    }
    
    public void SaveProgression()
    {
        if (!enablePersistence) return;
        
        // Update player health in save data
        progressionData.playerHealthPercentage = playerHealthPercentage;
        progressionData.lastUpdated = DateTime.Now.ToString("o");
        
        try
        {
            string jsonData = JsonUtility.ToJson(progressionData, true);
            PlayerPrefs.SetString(SAVE_KEY, jsonData);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgressionManager] Failed to save progression: {e.Message}");
        }
    }
    
    private void LoadProgression()
    {
        try
        {
            string jsonData = PlayerPrefs.GetString(SAVE_KEY, "");
            
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("[GameProgressionManager] No saved progression found, creating new");
                progressionData = new GameProgressionData();
                InitializeFirstBoss();
                SaveProgression();
                return;
            }
            
            progressionData = JsonUtility.FromJson<GameProgressionData>(jsonData);
            playerHealthPercentage = progressionData.playerHealthPercentage;
            
            Debug.Log($"[GameProgressionManager] Progression loaded - {progressionData.defeatedBosses.Count} bosses defeated, Player health: {playerHealthPercentage}%");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameProgressionManager] Failed to load progression: {e.Message}");
            progressionData = new GameProgressionData();
            InitializeFirstBoss();
        }
    }
    
    private void InitializeFirstBoss()
    {
        // Unlock first boss by default (TheDrunkard)
        UnlockBoss(BossType.TheDrunkard);
        Debug.Log("[GameProgressionManager] First boss unlocked: TheDrunkard");
    }
    
    public void ResetProgression()
    {
        progressionData = new GameProgressionData();
        playerHealthPercentage = maxHealthPercentage;
        ResetEncounter();
        InitializeFirstBoss();
        SaveProgression();
        
        // Clear the map so a new one will be generated
        PlayerPrefs.DeleteKey("Map");
        PlayerPrefs.DeleteKey("ReturnToMap");
        PlayerPrefs.DeleteKey("CurrentNodeType");
        PlayerPrefs.DeleteKey("SelectedBoss");
        PlayerPrefs.Save();
        
        Debug.Log("[GameProgressionManager] Progression reset - map cleared for fresh start");
    }
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

[Serializable]
public class GameProgressionData
{
    // Player
    public float playerHealthPercentage = 100f;
    
    // Boss progression
    public List<string> unlockedBosses = new List<string>();
    public List<string> defeatedBosses = new List<string>();
    public string selectedBossType = "";
    
    // Minion progression
    public List<ActState> actStates = new List<ActState>();
    public string currentActBoss = "";
    
    // Node instance tracking
    public List<string> defeatedNodeInstances = new List<string>();
    
    // Metadata
    public string lastUpdated = "";
}

