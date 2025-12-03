using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Map
{
    public class MapPlayerTracker : MonoBehaviour
    {
        public bool lockAfterSelecting = false;
        public float enterNodeDelay = 1f;
        public MapManager mapManager;
        public MapView view;

        public static MapPlayerTracker Instance;

        public bool Locked { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SelectNode(MapNode mapNode)
        {
            if (Locked) return;

            // Debug.Log("Selected node: " + mapNode.Node.point);
            
            // Check if this is a replayable node (can be selected regardless of current position)
            bool isReplayable = GameProgressionManager.Instance != null && 
                               GameProgressionManager.Instance.IsNodeInstanceReplayable(mapNode.Node.nodeInstanceId);
            
            if (isReplayable)
            {
                // Allow replaying any defeated node that's in the path or first layer
                bool isInPath = mapManager.CurrentMap.path.Any(p => p.Equals(mapNode.Node.point));
                bool isFirstLayer = mapNode.Node.point.y == 0;
                
                if (isInPath || isFirstLayer)
                {
                    Debug.Log($"[MapPlayerTracker] Replaying node: {mapNode.Node.nodeInstanceId}");
                    SendPlayerToNode(mapNode);
                    return;
                }
            }

            if (mapManager.CurrentMap.path.Count == 0)
            {
                // player has not selected the node yet, he can select any of the nodes with y = 0
                if (mapNode.Node.point.y == 0)
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
            else
            {
                Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
                Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

                if (currentNode != null && currentNode.outgoing.Any(point => point.Equals(mapNode.Node.point)))
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
        }

        private void SendPlayerToNode(MapNode mapNode)
        {
            Locked = lockAfterSelecting;
            
            // Check if this is a battle node (Minion or Boss) - we delay path updates for these
            bool isBattleNode = mapNode.Node.nodeType == NodeType.Minion || mapNode.Node.nodeType == NodeType.Boss;
            
            // Check if replaying a defeated node
            bool isReplaying = GameProgressionManager.Instance != null && 
                              GameProgressionManager.Instance.IsNodeInstanceReplayable(mapNode.Node.nodeInstanceId);
            
            if (isBattleNode && !isReplaying)
            {
                // For battle nodes: Add to path but track as pending (can be reverted if battle interrupted)
                mapManager.CurrentMap.path.Add(mapNode.Node.point);
                mapManager.SaveMap();
                
                Debug.Log($"[MapPlayerTracker] Battle node added to path as pending: {mapNode.Node.point}");
            }
            else if (!isBattleNode)
            {
                // For non-battle nodes: Add to path immediately (no risk of interruption)
                mapManager.CurrentMap.path.Add(mapNode.Node.point);
                mapManager.SaveMap();
            }
            // For replay nodes: Don't modify the path at all
            
            view.SetAttainableNodes();
            view.SetLineColors();
            mapNode.ShowSwirlAnimation();

            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() => EnterNode(mapNode));
        }

        private static void EnterNode(MapNode mapNode)
        {
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);
            
            switch (mapNode.Node.nodeType)
            {
                case NodeType.Minion:
                    HandleMinionNode(mapNode);
                    break;
                case NodeType.Boss:
                    HandleBossNode(mapNode);
                    break;
                case NodeType.Regen:
                    HandleRegenNode(mapNode);
                    break;
                case NodeType.Treasure:
                    HandleTreasureNode(mapNode);
                    break;
                case NodeType.Shop:
                    HandleShopNode(mapNode);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    private static void HandleMinionNode(MapNode mapNode)
    {
        // Ensure GameProgressionManager exists
        if (GameProgressionManager.Instance == null)
        {
            Debug.LogError("[MapPlayerTracker] GameProgressionManager.Instance is null! Creating one...");
            
            // Try to find existing GameProgressionManager
            var existingManager = FindObjectOfType<GameProgressionManager>();
            if (existingManager != null)
            {
                Debug.Log("[MapPlayerTracker] Found existing GameProgressionManager, setting instance");
                GameProgressionManager.SetInstance(existingManager);
            }
            else
            {
                Debug.LogError("[MapPlayerTracker] No GameProgressionManager found in scene! Please add one to the scene.");
                return;
            }
        }
        
        // Get minion data from centralized manager
        MinionData minionData = GameProgressionManager.Instance.GetMinionDataFromMapNode(mapNode);
        
        if (minionData == null)
        {
            Debug.LogError("[MapPlayerTracker] Could not get minion data from GameProgressionManager!");
            return;
        }

        Debug.Log($"[MapPlayerTracker] Starting minion battle: {minionData.minionName}");
        
        // Get the boss type from the blueprint
        BossType bossType = mapNode.Blueprint.bossType;
        Debug.Log($"[MapPlayerTracker] Boss type from blueprint: {bossType}");
        
        // Check if this specific node instance is already defeated
        bool isDefeated = GameProgressionManager.Instance.IsNodeInstanceDefeated(mapNode.Node.nodeInstanceId);
        bool isReplayable = GameProgressionManager.Instance.IsNodeInstanceReplayable(mapNode.Node.nodeInstanceId);
        Debug.Log($"[MapPlayerTracker] Is node instance {mapNode.Node.nodeInstanceId} defeated? {isDefeated}, replayable? {isReplayable}");
        
        // Allow entering if: not defeated OR (defeated AND replayable)
        bool isReplaying = isDefeated && isReplayable;
        if (isDefeated && !isReplayable)
        {
            Debug.LogWarning($"[MapPlayerTracker] Node instance {mapNode.Node.nodeInstanceId} already defeated and not replayable");
            Instance.Locked = false;
            return;
        }
        
        if (isReplaying)
        {
            Debug.Log($"[MapPlayerTracker] REPLAYING minion battle: {minionData.minionName}");
        }
        
        // Store the current map state so we can return to it
        Debug.Log("[MapPlayerTracker] Storing map return state");
        PlayerPrefs.SetString("ReturnToMap", "true");
        PlayerPrefs.SetString("CurrentNodeType", "Minion");
        PlayerPrefs.SetString("IsReplayingNode", isReplaying ? "true" : "false");
        PlayerPrefs.Save();
        
        // Serialize the pending node point for path cleanup on interrupted battles
        string pendingNodePoint = isReplaying ? "" : $"{mapNode.Node.point.x},{mapNode.Node.point.y}";
        
        // Start minion encounter via GameProgressionManager
        Debug.Log("[MapPlayerTracker] Clearing selected boss");
        GameProgressionManager.Instance.ClearSelectedBoss();
        
        Debug.Log($"[MapPlayerTracker] Starting minion encounter for {minionData.minionName}");
        GameProgressionManager.Instance.StartMinionEncounter(minionData, bossType, mapNode.Node.nodeInstanceId, pendingNodePoint);
        Debug.Log($"[MapPlayerTracker] Minion encounter started via GameProgressionManager");
        
        // Load the blackjack scene
        Debug.Log("[MapPlayerTracker] Loading Blackjack scene...");
        try
        {
            // Force save PlayerPrefs before scene load
            PlayerPrefs.Save();
            
            // Try using GameSceneManager first if available
            if (GameSceneManager.Instance != null)
            {
                Debug.Log("[MapPlayerTracker] Using GameSceneManager to load scene");
                GameSceneManager.Instance.LoadGameScene();
            }
            else
            {
                Debug.Log("[MapPlayerTracker] Using direct SceneManager.LoadScene");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
            }
            Debug.Log("[MapPlayerTracker] LoadScene called for Blackjack");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MapPlayerTracker] Failed to load Blackjack scene: {e.Message}");
            Debug.LogError($"[MapPlayerTracker] Stack trace: {e.StackTrace}");
            
            // Try fallback loading
            Debug.Log("[MapPlayerTracker] Attempting fallback scene load...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }
    }

    private static void HandleBossNode(MapNode mapNode)
    {
        BossType bossType = mapNode.Blueprint.bossType;
        
        Debug.Log($"Starting boss battle: {bossType}");
        
        // Use GameProgressionManager (SINGLE SOURCE OF TRUTH)
        if (GameProgressionManager.Instance == null)
        {
            Debug.LogError("[MapPlayerTracker] GameProgressionManager.Instance is null!");
            Instance.Locked = false;
            return;
        }
        
        // Check if this specific boss node instance is already defeated and replayable
        bool isDefeated = GameProgressionManager.Instance.IsNodeInstanceDefeated(mapNode.Node.nodeInstanceId);
        bool isReplayable = GameProgressionManager.Instance.IsNodeInstanceReplayable(mapNode.Node.nodeInstanceId);
        bool isReplaying = isDefeated && isReplayable;
        
        Debug.Log($"[MapPlayerTracker] Boss node {mapNode.Node.nodeInstanceId} - defeated: {isDefeated}, replayable: {isReplayable}");
        
        // Check if boss is unlocked (either globally OR by defeating 2+ minions) - skip for replays
        if (!isReplaying)
        {
            bool isGloballyUnlocked = GameProgressionManager.Instance.IsBossUnlocked(bossType);
            bool isUnlockedByMinions = GameProgressionManager.Instance.IsBossUnlockedByMinions(bossType);
            
            if (!isGloballyUnlocked && !isUnlockedByMinions)
            {
                Debug.LogWarning($"Boss {bossType} is not unlocked yet! Global: {isGloballyUnlocked}, ByMinions: {isUnlockedByMinions}");
                Instance.Locked = false;
                return;
            }
            
            Debug.Log($"Boss {bossType} is unlocked - Global: {isGloballyUnlocked}, ByMinions: {isUnlockedByMinions}");
        }
        else
        {
            Debug.Log($"[MapPlayerTracker] REPLAYING boss battle: {bossType}");
        }
        
        // Store the current map state
        PlayerPrefs.SetString("ReturnToMap", "true");
        PlayerPrefs.SetString("CurrentNodeType", "Boss");
        PlayerPrefs.SetString("IsReplayingNode", isReplaying ? "true" : "false");
        PlayerPrefs.Save();
        
        // Serialize the pending node point for path cleanup on interrupted battles
        string pendingNodePoint = isReplaying ? "" : $"{mapNode.Node.point.x},{mapNode.Node.point.y}";
        
        // Get boss data and start encounter
        BossData bossData = GameProgressionManager.Instance.GetBossData(bossType);
        if (bossData != null)
        {
            GameProgressionManager.Instance.SelectBoss(bossType);
            GameProgressionManager.Instance.StartBossEncounter(bossData, mapNode.Node.nodeInstanceId, pendingNodePoint);
            Debug.Log($"[MapPlayerTracker] Boss encounter started via GameProgressionManager: {bossType}");
        }
        else
        {
            Debug.LogError($"[MapPlayerTracker] Boss data not found for {bossType}");
            Instance.Locked = false;
            return;
        }
        
        // Load the blackjack scene
        try
        {
            // Force save PlayerPrefs before scene load
            PlayerPrefs.Save();
            
            // Try using GameSceneManager first if available
            if (GameSceneManager.Instance != null)
            {
                Debug.Log("[MapPlayerTracker] Using GameSceneManager to load scene for boss");
                GameSceneManager.Instance.LoadGameScene();
            }
            else
            {
                Debug.Log("[MapPlayerTracker] Using direct SceneManager.LoadScene for boss");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MapPlayerTracker] Failed to load Blackjack scene for boss: {e.Message}");
            // Try fallback loading
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }
    }

        private static void HandleRegenNode(MapNode mapNode)
        {
            Debug.Log("Regen node - restore player health");
            
            // TODO: Show regen UI panel
            // For now, just unlock the map immediately
            Instance.Locked = false;
            
            // Example: Restore player health/resources
            // PlayerStats.Instance.RestoreHealth();
        }

        private static void HandleTreasureNode(MapNode mapNode)
        {
            Debug.Log("Treasure node - reward player with tarot card");
            
            float chance = mapNode.Blueprint.tarotCardChance;
            
            // TODO: Show treasure/reward UI panel
            // Roll for tarot card based on chance
            if (UnityEngine.Random.value <= chance)
            {
                Debug.Log($"Player gets a tarot card! (Chance was {chance})");
                // TarotCardManager.Instance.GrantRandomTarotCard();
            }
            
            Instance.Locked = false;
        }

        private static void HandleShopNode(MapNode mapNode)
        {
            Debug.Log("Shop node - open shop UI");
            
            // TODO: Show shop UI panel
            Instance.Locked = false;
            
            // ShopManager.Instance.OpenShop();
        }

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
    }
}