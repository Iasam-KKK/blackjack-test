using UnityEngine;

namespace Map
{
    public enum NodeType
    {
        Minion,      // Renamed from MinorEnemy
        Boss,
        Regen,       // Renamed from RestSite
        Treasure,    // Reward nodes with tarot cards
        Shop,        // Renamed from Store
    }
}

namespace Map
{
    [CreateAssetMenu(fileName = "NewNodeBlueprint", menuName = "Map/Node Blueprint")]
    public class NodeBlueprint : ScriptableObject
    {
        [Header("Visual")]
        public Sprite sprite;
        
        [Header("Node Info")]
        public string nodeName;
        [TextArea(2, 4)]
        public string description;
        public NodeType nodeType;
        
        [Header("Battle Data")]
        public BossType bossType;
        public MinionData minionData;
        
        [Header("Reward")]
        [Range(0f, 1f)]
        public float tarotCardChance = 0.5f;
    }
}