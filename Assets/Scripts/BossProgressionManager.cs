using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages boss progression, unlocks, and reward states across the game
/// Persists data using JSON instead of PlayerPrefs
/// </summary>
public class BossProgressionManager : MonoBehaviour
{
    public static BossProgressionManager Instance { get; private set; }
    
    [Header("Configuration")]
    public List<BossData> allBosses = new List<BossData>();
    
    [Header("Persistence")]
    public bool enablePersistence = true;
    private const string PROGRESSION_SAVE_KEY = "BossProgressionData_v1";
    
    [Header("Current State")]
    public BossProgressionData progressionData;
    
    [Header("Events")]
    public Action<BossData> OnBossDefeated;
    public Action<BossData> OnBossUnlocked;
    public Action<BossRewardState> OnRewardClaimed;
    public Action OnProgressionUpdated;
    
    private void Awake()
    {
        // Singleton pattern - prevent duplicates across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[BossProgressionManager] Instance created and set to DontDestroyOnLoad");
            InitializeProgression();
        }
        else
        {
            Debug.LogWarning($"[BossProgressionManager] Duplicate instance detected in scene, destroying {gameObject.name}");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Initialize progression system
    /// </summary>
    private void InitializeProgression()
    {
        // Load all boss data if not assigned
        if (allBosses == null || allBosses.Count == 0)
        {
            LoadAllBossData();
        }
        
        // Load saved progression or create new
        if (enablePersistence)
        {
            LoadProgression();
        }
        else
        {
            progressionData = new BossProgressionData();
            InitializeFirstBoss();
        }
        
        Debug.Log($"[BossProgressionManager] Initialized with {allBosses.Count} bosses");
        Debug.Log($"[BossProgressionManager] Bosses defeated: {progressionData.defeatedBosses.Count}, Unlocked: {progressionData.unlockedBosses.Count}");
    }
    
    /// <summary>
    /// Load all boss ScriptableObjects
    /// </summary>
    private void LoadAllBossData()
    {
        Debug.Log("[BossProgressionManager] Loading boss data...");
        
        // Load from Resources folders
        BossData[] bossDataArray1 = Resources.LoadAll<BossData>("ScriptableObjectsBosses");
        BossData[] bossDataArray2 = Resources.LoadAll<BossData>("ScriptableObject");
        
        List<BossData> loadedBosses = new List<BossData>();
        if (bossDataArray1 != null) loadedBosses.AddRange(bossDataArray1);
        if (bossDataArray2 != null) loadedBosses.AddRange(bossDataArray2);
        
        // Sort by unlock order
        allBosses = loadedBosses.OrderBy(b => b.unlockOrder).ToList();
        
        #if UNITY_EDITOR
        // Fallback to AssetDatabase in editor
        if (allBosses.Count == 0)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BossData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                BossData bossData = UnityEditor.AssetDatabase.LoadAssetAtPath<BossData>(path);
                if (bossData != null && !allBosses.Contains(bossData))
                {
                    allBosses.Add(bossData);
                }
            }
            allBosses = allBosses.OrderBy(b => b.unlockOrder).ToList();
        }
        #endif
        
        Debug.Log($"[BossProgressionManager] Loaded {allBosses.Count} bosses: {string.Join(", ", allBosses.Select(b => b.bossName))}");
    }
    
    /// <summary>
    /// Initialize first boss as unlocked
    /// </summary>
    private void InitializeFirstBoss()
    {
        if (allBosses.Count > 0)
        {
            var firstBoss = allBosses.FirstOrDefault(b => b.unlockOrder == 0);
            if (firstBoss != null)
            {
                UnlockBoss(firstBoss.bossType);
                Debug.Log($"[BossProgressionManager] First boss unlocked: {firstBoss.bossName}");
            }
        }
    }
    
    /// <summary>
    /// Mark a boss as defeated and unlock next boss
    /// </summary>
    public void MarkBossDefeated(BossType bossType)
    {
        if (IsBossDefeated(bossType))
        {
            Debug.LogWarning($"[BossProgressionManager] Boss {bossType} already defeated");
            return;
        }
        
        // Add to defeated list
        if (!progressionData.defeatedBosses.Contains(bossType.ToString()))
        {
            progressionData.defeatedBosses.Add(bossType.ToString());
            Debug.Log($"[BossProgressionManager] Boss {bossType} marked as defeated");
        }
        
        // Find boss data
        BossData defeatedBoss = GetBossData(bossType);
        if (defeatedBoss != null)
        {
            // Unlock rewards for this boss
            UnlockBossRewards(defeatedBoss);
            
            // Unlock next boss
            UnlockNextBoss(defeatedBoss);
            
            // Trigger event
            OnBossDefeated?.Invoke(defeatedBoss);
        }
        
        // Save progression
        SaveProgression();
        OnProgressionUpdated?.Invoke();
    }
    
    /// <summary>
    /// Unlock a specific boss
    /// </summary>
    public void UnlockBoss(BossType bossType)
    {
        string bossKey = bossType.ToString();
        
        if (!progressionData.unlockedBosses.Contains(bossKey))
        {
            progressionData.unlockedBosses.Add(bossKey);
            Debug.Log($"[BossProgressionManager] Boss {bossType} unlocked");
            
            BossData bossData = GetBossData(bossType);
            if (bossData != null)
            {
                OnBossUnlocked?.Invoke(bossData);
            }
            
            SaveProgression();
            OnProgressionUpdated?.Invoke();
        }
    }
    
    /// <summary>
    /// Unlock next boss based on unlock order
    /// </summary>
    private void UnlockNextBoss(BossData currentBoss)
    {
        int nextUnlockOrder = currentBoss.unlockOrder + 1;
        BossData nextBoss = allBosses.FirstOrDefault(b => b.unlockOrder == nextUnlockOrder);
        
        if (nextBoss != null)
        {
            UnlockBoss(nextBoss.bossType);
            Debug.Log($"[BossProgressionManager] Next boss unlocked: {nextBoss.bossName} (order: {nextUnlockOrder})");
        }
        else
        {
            Debug.Log($"[BossProgressionManager] No more bosses to unlock. All bosses completed!");
        }
    }
    
    /// <summary>
    /// Unlock rewards for a defeated boss
    /// </summary>
    private void UnlockBossRewards(BossData boss)
    {
        if (boss.rewards == null || boss.rewards.Count == 0)
        {
            Debug.Log($"[BossProgressionManager] Boss {boss.bossName} has no rewards");
            return;
        }
        
        foreach (var reward in boss.rewards)
        {
            string rewardId = GenerateRewardId(boss.bossType, reward);
            
            // Check if reward already exists
            var existingReward = progressionData.rewardStates.FirstOrDefault(r => r.rewardId == rewardId);
            
            if (existingReward == null)
            {
                // Create new reward state
                BossRewardState rewardState = new BossRewardState
                {
                    rewardId = rewardId,
                    bossType = boss.bossType.ToString(),
                    rewardName = reward.rewardName,
                    rewardDescription = reward.rewardDescription,
                    isUnlocked = true,
                    isClaimed = false,
                    grantsTarotCard = reward.grantsTarotCard,
                    tarotCardType = reward.tarotCardType.ToString(),
                    grantsBonusBalance = reward.grantsBonusBalance,
                    bonusAmount = reward.bonusAmount,
                    grantsPermanentUpgrade = reward.grantsPermanentUpgrade,
                    upgradeName = reward.upgradeName,
                    upgradeValue = reward.upgradeValue
                };
                
                progressionData.rewardStates.Add(rewardState);
                Debug.Log($"[BossProgressionManager] Reward unlocked: {reward.rewardName} for boss {boss.bossName}");
            }
            else
            {
                existingReward.isUnlocked = true;
                Debug.Log($"[BossProgressionManager] Reward re-unlocked: {reward.rewardName}");
            }
        }
        
        SaveProgression();
    }
    
    /// <summary>
    /// Claim a reward (adds to inventory/balance)
    /// </summary>
    public bool ClaimReward(string rewardId)
    {
        var rewardState = progressionData.rewardStates.FirstOrDefault(r => r.rewardId == rewardId);
        
        if (rewardState == null)
        {
            Debug.LogWarning($"[BossProgressionManager] Reward {rewardId} not found");
            return false;
        }
        
        if (!rewardState.isUnlocked)
        {
            Debug.LogWarning($"[BossProgressionManager] Reward {rewardId} is not unlocked yet");
            return false;
        }
        
        if (rewardState.isClaimed)
        {
            Debug.LogWarning($"[BossProgressionManager] Reward {rewardId} already claimed");
            return false;
        }
        
        // Process reward based on type
        bool success = ProcessRewardClaim(rewardState);
        
        if (success)
        {
            rewardState.isClaimed = true;
            rewardState.claimedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            Debug.Log($"[BossProgressionManager] Reward claimed: {rewardState.rewardName}");
            
            OnRewardClaimed?.Invoke(rewardState);
            SaveProgression();
            OnProgressionUpdated?.Invoke();
        }
        
        return success;
    }
    
    /// <summary>
    /// Process the actual reward claim (add to inventory/balance)
    /// </summary>
    private bool ProcessRewardClaim(BossRewardState rewardState)
    {
        // Tarot card reward
        if (rewardState.grantsTarotCard)
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[BossProgressionManager] InventoryManager not available");
                return false;
            }
            
            // Check inventory space
            if (!InventoryManager.Instance.HasStorageSpace())
            {
                Debug.LogWarning("[BossProgressionManager] Inventory full - cannot claim tarot card reward");
                return false;
            }
            
            // Find the tarot card data
            TarotCardType cardType = (TarotCardType)Enum.Parse(typeof(TarotCardType), rewardState.tarotCardType);
            TarotCardData cardData = FindTarotCardData(cardType);
            
            if (cardData != null)
            {
                // Create instance and assign material
                TarotCardData cardCopy = Instantiate(cardData);
                MaterialData randomMaterial = MaterialManager.GetRandomMaterial();
                cardCopy.AssignMaterial(randomMaterial);
                
                // Add to inventory
                bool added = InventoryManager.Instance.AddPurchasedCard(cardCopy);
                
                if (added)
                {
                    Debug.Log($"[BossProgressionManager] Tarot card {cardType} added to inventory");
                    return true;
                }
            }
            else
            {
                Debug.LogError($"[BossProgressionManager] Tarot card {cardType} not found");
                return false;
            }
        }
        
        // Bonus balance reward
        if (rewardState.grantsBonusBalance)
        {
            // Find Deck to add balance
            Deck deck = FindObjectOfType<Deck>();
            if (deck != null)
            {
                deck.Balance += rewardState.bonusAmount;
                Debug.Log($"[BossProgressionManager] Added {rewardState.bonusAmount} balance. New balance: {deck.Balance}");
                return true;
            }
            else
            {
                Debug.LogWarning("[BossProgressionManager] Deck not found - saving balance for later");
                // Store balance bonus for later (when Deck is available)
                PlayerPrefs.SetInt("PendingBalanceBonus", PlayerPrefs.GetInt("PendingBalanceBonus", 0) + (int)rewardState.bonusAmount);
                PlayerPrefs.Save();
                return true;
            }
        }
        
        // Permanent upgrade reward
        if (rewardState.grantsPermanentUpgrade)
        {
            // Store upgrade in PlayerPrefs for now
            // This can be expanded to a proper upgrade system later
            PlayerPrefs.SetInt($"Upgrade_{rewardState.upgradeName}", rewardState.upgradeValue);
            PlayerPrefs.Save();
            Debug.Log($"[BossProgressionManager] Permanent upgrade granted: {rewardState.upgradeName} = {rewardState.upgradeValue}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Find tarot card data by type
    /// </summary>
    private TarotCardData FindTarotCardData(TarotCardType cardType)
    {
        // Try loading from Resources
        TarotCardData[] allCards = Resources.LoadAll<TarotCardData>("");
        TarotCardData foundCard = allCards.FirstOrDefault(c => c.cardType == cardType);
        
        if (foundCard != null)
        {
            return foundCard;
        }
        
        // Try from ShopManager if available
        ShopManager shopManager = FindObjectOfType<ShopManager>();
        if (shopManager != null && shopManager.availableTarotCards != null)
        {
            foundCard = shopManager.availableTarotCards.FirstOrDefault(c => c.cardType == cardType);
            if (foundCard != null)
            {
                return foundCard;
            }
        }
        
        Debug.LogWarning($"[BossProgressionManager] Tarot card {cardType} not found in Resources or ShopManager");
        return null;
    }
    
    /// <summary>
    /// Select a boss for the next battle
    /// </summary>
    public void SelectBoss(BossType bossType)
    {
        if (!IsBossUnlocked(bossType))
        {
            Debug.LogWarning($"[BossProgressionManager] Cannot select locked boss: {bossType}");
            return;
        }
        
        progressionData.selectedBossType = bossType.ToString();
        SaveProgression();
        
        Debug.Log($"[BossProgressionManager] Boss selected: {bossType}");
    }
    
    /// <summary>
    /// Get the currently selected boss
    /// </summary>
    public BossType? GetSelectedBoss()
    {
        if (string.IsNullOrEmpty(progressionData.selectedBossType))
        {
            return null;
        }
        
        return (BossType)Enum.Parse(typeof(BossType), progressionData.selectedBossType);
    }
    
    /// <summary>
    /// Get boss data by type
    /// </summary>
    public BossData GetBossData(BossType bossType)
    {
        return allBosses.FirstOrDefault(b => b.bossType == bossType);
    }
    
    /// <summary>
    /// Check if boss is unlocked
    /// </summary>
    public bool IsBossUnlocked(BossType bossType)
    {
        return progressionData.unlockedBosses.Contains(bossType.ToString());
    }
    
    /// <summary>
    /// Check if boss is defeated
    /// </summary>
    public bool IsBossDefeated(BossType bossType)
    {
        return progressionData.defeatedBosses.Contains(bossType.ToString());
    }
    
    /// <summary>
    /// Get all unlocked bosses
    /// </summary>
    public List<BossData> GetUnlockedBosses()
    {
        return allBosses.Where(b => IsBossUnlocked(b.bossType)).ToList();
    }
    
    /// <summary>
    /// Get all defeated bosses
    /// </summary>
    public List<BossData> GetDefeatedBosses()
    {
        return allBosses.Where(b => IsBossDefeated(b.bossType)).ToList();
    }
    
    /// <summary>
    /// Get all available (unlocked but not defeated) bosses
    /// </summary>
    public List<BossData> GetAvailableBosses()
    {
        return allBosses.Where(b => IsBossUnlocked(b.bossType) && !IsBossDefeated(b.bossType)).ToList();
    }
    
    /// <summary>
    /// Get unclaimed rewards
    /// </summary>
    public List<BossRewardState> GetUnclaimedRewards()
    {
        return progressionData.rewardStates.Where(r => r.isUnlocked && !r.isClaimed).ToList();
    }
    
    /// <summary>
    /// Get rewards for a specific boss
    /// </summary>
    public List<BossRewardState> GetBossRewards(BossType bossType)
    {
        return progressionData.rewardStates.Where(r => r.bossType == bossType.ToString()).ToList();
    }
    
    /// <summary>
    /// Generate unique reward ID
    /// </summary>
    private string GenerateRewardId(BossType bossType, BossReward reward)
    {
        return $"{bossType}_{reward.rewardName.Replace(" ", "_")}";
    }
    
    /// <summary>
    /// Reset all progression (for testing or new game+)
    /// </summary>
    [ContextMenu("Reset Progression")]
    public void ResetProgression()
    {
        progressionData = new BossProgressionData();
        InitializeFirstBoss();
        SaveProgression();
        OnProgressionUpdated?.Invoke();
        Debug.Log("[BossProgressionManager] Progression reset");
    }
    
    /// <summary>
    /// Save progression to JSON
    /// </summary>
    public void SaveProgression()
    {
        if (!enablePersistence) return;
        
        try
        {
            string jsonData = JsonUtility.ToJson(progressionData, true);
            PlayerPrefs.SetString(PROGRESSION_SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            Debug.Log("[BossProgressionManager] Progression saved");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BossProgressionManager] Failed to save progression: {e.Message}");
        }
    }
    
    /// <summary>
    /// Load progression from JSON
    /// </summary>
    private void LoadProgression()
    {
        try
        {
            string jsonData = PlayerPrefs.GetString(PROGRESSION_SAVE_KEY, "");
            
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("[BossProgressionManager] No saved progression found, creating new");
                progressionData = new BossProgressionData();
                InitializeFirstBoss();
                SaveProgression();
                return;
            }
            
            progressionData = JsonUtility.FromJson<BossProgressionData>(jsonData);
            Debug.Log($"[BossProgressionManager] Progression loaded - {progressionData.defeatedBosses.Count} bosses defeated");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BossProgressionManager] Failed to load progression: {e.Message}");
            progressionData = new BossProgressionData();
            InitializeFirstBoss();
        }
    }
    
    /// <summary>
    /// Get progression statistics
    /// </summary>
    public ProgressionStats GetProgressionStats()
    {
        return new ProgressionStats
        {
            totalBosses = allBosses.Count,
            unlockedBosses = progressionData.unlockedBosses.Count,
            defeatedBosses = progressionData.defeatedBosses.Count,
            totalRewards = progressionData.rewardStates.Count,
            claimedRewards = progressionData.rewardStates.Count(r => r.isClaimed),
            unclaimedRewards = progressionData.rewardStates.Count(r => r.isUnlocked && !r.isClaimed)
        };
    }
    
    // ============================================================================
    // MINION TRACKING
    // ============================================================================
    
    /// <summary>
    /// Start a new act for a boss (initialize minion progression)
    /// </summary>
    public void StartBossAct(BossType bossType)
    {
        string bossKey = bossType.ToString();
        
        // Check if act already exists
        ActState existingAct = progressionData.actStates.Find(a => a.bossType == bossKey);
        if (existingAct != null)
        {
            Debug.Log($"[BossProgressionManager] Act for {bossType} already exists, resuming");
            progressionData.currentActBoss = bossKey;
            return;
        }
        
        // Create new act
        ActState newAct = new ActState
        {
            bossType = bossKey,
            defeatedMinions = new List<string>(),
            bossUnlockedInAct = false,
            actStartedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            actCompletedAt = ""
        };
        
        progressionData.actStates.Add(newAct);
        progressionData.currentActBoss = bossKey;
        
        Debug.Log($"[BossProgressionManager] Started new act for {bossType}");
        SaveProgression();
    }
    
    /// <summary>
    /// Mark a minion as defeated in the current act
    /// </summary>
    public void MarkMinionDefeated(BossType bossType, string minionName)
    {
        string bossKey = bossType.ToString();
        ActState act = progressionData.actStates.Find(a => a.bossType == bossKey);
        
        if (act == null)
        {
            Debug.LogWarning($"[BossProgressionManager] No act found for {bossType}, creating one");
            StartBossAct(bossType);
            act = progressionData.actStates.Find(a => a.bossType == bossKey);
        }
        
        // Add minion to defeated list if not already there
        if (!act.defeatedMinions.Contains(minionName))
        {
            act.defeatedMinions.Add(minionName);
            Debug.Log($"[BossProgressionManager] Minion {minionName} defeated ({act.defeatedMinions.Count}/3)");
            
            // Check if boss should be unlocked (2+ minions defeated)
            if (act.defeatedMinions.Count >= 2 && !act.bossUnlockedInAct)
            {
                act.bossUnlockedInAct = true;
                Debug.Log($"[BossProgressionManager] Boss {bossType} unlocked in act (2+ minions defeated)");
            }
            
            SaveProgression();
        }
    }
    
    /// <summary>
    /// Check if a minion has been defeated
    /// </summary>
    public bool IsMinionDefeated(BossType bossType, string minionName)
    {
        string bossKey = bossType.ToString();
        ActState act = progressionData.actStates.Find(a => a.bossType == bossKey);
        
        if (act == null) return false;
        
        return act.defeatedMinions.Contains(minionName);
    }
    
    /// <summary>
    /// Check if boss is unlocked in current act (2+ minions defeated)
    /// </summary>
    public bool IsBossUnlockedInAct(BossType bossType)
    {
        string bossKey = bossType.ToString();
        ActState act = progressionData.actStates.Find(a => a.bossType == bossKey);
        
        if (act == null) return false;
        
        return act.bossUnlockedInAct;
    }
    
    /// <summary>
    /// Get number of minions defeated for a boss
    /// </summary>
    public int GetMinionDefeatedCount(BossType bossType)
    {
        string bossKey = bossType.ToString();
        ActState act = progressionData.actStates.Find(a => a.bossType == bossKey);
        
        if (act == null) return 0;
        
        return act.defeatedMinions.Count;
    }
    
    /// <summary>
    /// Get list of defeated minion names for a boss
    /// </summary>
    public List<string> GetDefeatedMinions(BossType bossType)
    {
        string bossKey = bossType.ToString();
        ActState act = progressionData.actStates.Find(a => a.bossType == bossKey);
        
        if (act == null) return new List<string>();
        
        return new List<string>(act.defeatedMinions);
    }
    
    /// <summary>
    /// Complete the act (boss defeated, reset minion progress)
    /// </summary>
    public void CompleteAct(BossType bossType)
    {
        string bossKey = bossType.ToString();
        ActState act = progressionData.actStates.Find(a => a.bossType == bossKey);
        
        if (act != null)
        {
            act.actCompletedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Debug.Log($"[BossProgressionManager] Act completed for {bossType}");
        }
        
        progressionData.currentActBoss = "";
        SaveProgression();
    }
    
    /// <summary>
    /// Get the current act boss
    /// </summary>
    public BossType? GetCurrentActBoss()
    {
        if (string.IsNullOrEmpty(progressionData.currentActBoss))
            return null;
            
        if (System.Enum.TryParse<BossType>(progressionData.currentActBoss, out BossType bossType))
        {
            return bossType;
        }
        
        return null;
    }
}

// ============================================================================
// DATA STRUCTURES
// ============================================================================

/// <summary>
/// Main progression data structure (serializable)
/// </summary>
[Serializable]
public class BossProgressionData
{
    public List<string> unlockedBosses = new List<string>();
    public List<string> defeatedBosses = new List<string>();
    public List<BossRewardState> rewardStates = new List<BossRewardState>();
    public string selectedBossType = "";
    public string lastUpdated = "";
    
    // Minion progression tracking
    public List<ActState> actStates = new List<ActState>();
    public string currentActBoss = ""; // Which boss's minions are currently active
}

/// <summary>
/// Act state tracking for minion progression (serializable)
/// Tracks which minions have been defeated for each boss
/// </summary>
[Serializable]
public class ActState
{
    public string bossType;
    public List<string> defeatedMinions = new List<string>(); // Names of defeated minions
    public bool bossUnlockedInAct = false; // True if 2+ minions defeated
    public string actStartedAt;
    public string actCompletedAt;
}

/// <summary>
/// Reward state tracking (serializable)
/// </summary>
[Serializable]
public class BossRewardState
{
    public string rewardId;
    public string bossType;
    public string rewardName;
    public string rewardDescription;
    public bool isUnlocked;
    public bool isClaimed;
    public string claimedAt;
    
    // Reward details
    public bool grantsTarotCard;
    public string tarotCardType;
    public bool grantsBonusBalance;
    public uint bonusAmount;
    public bool grantsPermanentUpgrade;
    public string upgradeName;
    public int upgradeValue;
}

/// <summary>
/// Progression statistics
/// </summary>
public struct ProgressionStats
{
    public int totalBosses;
    public int unlockedBosses;
    public int defeatedBosses;
    public int totalRewards;
    public int claimedRewards;
    public int unclaimedRewards;
}

