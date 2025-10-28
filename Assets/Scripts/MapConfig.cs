using System.Collections.Generic;
using OneLine;
using UnityEngine;

namespace Map
{
    [CreateAssetMenu(fileName = "NewMapConfig", menuName = "Map/Map Config")]
    public class MapConfig : ScriptableObject
    {
        [Header("Node Blueprints")]
        [Tooltip("All node blueprint assets used in this map")]
        public List<NodeBlueprint> nodeBlueprints;
        
        [Header("Reward Nodes")]
        [Tooltip("Nodes that will be randomly placed after minion layers (Shop, Regen, Treasure)")]
        public List<NodeType> rewardNodes = new List<NodeType>
            {NodeType.Shop, NodeType.Regen, NodeType.Treasure};
        
        [Header("Map Structure")]
        public int GridWidth => Mathf.Max(numOfRewardNodes.max, numOfMinionNodes.max);

        [OneLineWithHeader]
        [Tooltip("Number of reward nodes before boss (Shop/Regen/Treasure)")]
        public IntMinMax numOfRewardNodes = new IntMinMax { min = 2, max = 3 };
        
        [OneLineWithHeader]
        [Tooltip("Number of minion nodes per layer")]
        public IntMinMax numOfMinionNodes = new IntMinMax { min = 3, max = 4 };

        [Tooltip("Increase this number to generate more paths")]
        public int extraPaths = 1;
        
        [Header("Map Layers")]
        [Tooltip("Defines the structure of each layer in the map")]
        public List<MapLayer> layers;
    }
}