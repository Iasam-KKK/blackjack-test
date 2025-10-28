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
            mapManager.CurrentMap.path.Add(mapNode.Node.point);
            mapManager.SaveMap();
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
            // Get the minion data from blueprint
            MinionData minionData = mapNode.Blueprint.minionData;
            
            if (minionData == null)
            {
                Debug.LogError("Minion node has no MinionData assigned!");
                return;
            }

            Debug.Log($"Starting minion battle: {minionData.minionName}");
            
            // Store the current map state so we can return to it
            PlayerPrefs.SetString("ReturnToMap", "true");
            PlayerPrefs.SetString("CurrentNodeType", "Minion");
            PlayerPrefs.Save();
            
            // Set up the minion encounter
            if (MinionEncounterManager.Instance != null)
            {
                MinionEncounterManager.Instance.SetCurrentMinion(minionData);
            }
            
            // Load the blackjack scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
        }

        private static void HandleBossNode(MapNode mapNode)
        {
            BossType bossType = mapNode.Blueprint.bossType;
            
            Debug.Log($"Starting boss battle: {bossType}");
            
            // Store the current map state
            PlayerPrefs.SetString("ReturnToMap", "true");
            PlayerPrefs.SetString("CurrentNodeType", "Boss");
            PlayerPrefs.Save();
            
            // Check if boss is unlocked
            if (!BossProgressionManager.Instance.IsBossUnlocked(bossType))
            {
                Debug.LogWarning($"Boss {bossType} is not unlocked yet!");
                Instance.Locked = false;
                return;
            }
            
            // Set up the boss encounter
            BossData bossData = BossProgressionManager.Instance.GetBossData(bossType);
            if (bossData != null && BossManager.Instance != null)
            {
                BossManager.Instance.SetCurrentBoss(bossData);
            }
            
            // Load the blackjack scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Blackjack");
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