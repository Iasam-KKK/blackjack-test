using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// Helper class to generate map configurations for the blackjack game
    /// Creates a map with: 3 Minions -> Reward Nodes (Shop/Regen/Treasure) -> Boss, repeated for all bosses
    /// </summary>
    public static class MapConfigGenerator
    {
        /// <summary>
        /// Generate a complete map config for the game's progression
        /// </summary>
        /// <param name="allNodeBlueprints">All node blueprints (minions, bosses, rewards)</param>
        /// <param name="numBosses">Number of bosses in the game (default 5)</param>
        /// <returns>MapConfig with proper layer structure</returns>
        public static MapConfig GenerateMapConfig(List<NodeBlueprint> allNodeBlueprints, int numBosses = 5)
        {
            MapConfig config = ScriptableObject.CreateInstance<MapConfig>();
            config.nodeBlueprints = allNodeBlueprints;
            config.rewardNodes = new List<NodeType> { NodeType.Shop, NodeType.Regen, NodeType.Treasure };
            config.extraPaths = 1;
            config.layers = new List<MapLayer>();

            // For each boss, create: 3 minion layers + 1 reward layer + 1 boss layer = 5 layers per boss
            for (int bossIndex = 0; bossIndex < numBosses; bossIndex++)
            {
                // Add 3 minion layers
                for (int minionLayer = 0; minionLayer < 3; minionLayer++)
                {
                    config.layers.Add(CreateMinionLayer(minionLayer, bossIndex == 0 && minionLayer == 0));
                }

                // Add 1 reward layer (Shop, Regen, Treasure randomly placed)
                config.layers.Add(CreateRewardLayer());

                // Add 1 boss layer
                config.layers.Add(CreateBossLayer(bossIndex == numBosses - 1));
            }

            return config;
        }

        /// <summary>
        /// Create a minion layer
        /// </summary>
        private static MapLayer CreateMinionLayer(int layerIndex, bool isFirstLayer)
        {
            MapLayer layer = new MapLayer
            {
                nodeType = NodeType.Minion,
                distanceFromPreviousLayer = new FloatMinMax
                {
                    min = isFirstLayer ? 1f : 3f,
                    max = isFirstLayer ? 2f : 5f
                },
                nodesApartDistance = 2f,
                randomizePosition = isFirstLayer ? 0.1f : 0.3f,
                randomizeNodes = 0f // No randomization for minion layers - always minions
            };

            return layer;
        }

        /// <summary>
        /// Create a reward layer (Shop, Regen, Treasure)
        /// </summary>
        private static MapLayer CreateRewardLayer()
        {
            MapLayer layer = new MapLayer
            {
                nodeType = NodeType.Shop, // Default, but will be randomized
                distanceFromPreviousLayer = new FloatMinMax
                {
                    min = 3f,
                    max = 5f
                },
                nodesApartDistance = 2.5f,
                randomizePosition = 0.4f,
                randomizeNodes = 1f // 100% randomization - always pick from reward nodes
            };

            return layer;
        }

        /// <summary>
        /// Create a boss layer
        /// </summary>
        private static MapLayer CreateBossLayer(bool isFinalBoss)
        {
            MapLayer layer = new MapLayer
            {
                nodeType = NodeType.Boss,
                distanceFromPreviousLayer = new FloatMinMax
                {
                    min = isFinalBoss ? 5f : 4f,
                    max = isFinalBoss ? 7f : 6f
                },
                nodesApartDistance = 3f,
                randomizePosition = 0.1f,
                randomizeNodes = 0f // No randomization - always boss
            };

            return layer;
        }

        /// <summary>
        /// Create a simplified map config (for testing or single boss)
        /// Structure: Start -> 3 Minions -> Rewards -> Boss
        /// </summary>
        public static MapConfig GenerateSimpleMapConfig(List<NodeBlueprint> allNodeBlueprints)
        {
            MapConfig config = ScriptableObject.CreateInstance<MapConfig>();
            config.nodeBlueprints = allNodeBlueprints;
            config.rewardNodes = new List<NodeType> { NodeType.Shop, NodeType.Regen, NodeType.Treasure };
            config.extraPaths = 1;
            config.layers = new List<MapLayer>
            {
                // Layer 0: Starting minions
                CreateMinionLayer(0, true),
                
                // Layer 1: Second minion layer
                CreateMinionLayer(1, false),
                
                // Layer 2: Third minion layer
                CreateMinionLayer(2, false),
                
                // Layer 3: Reward nodes (Shop/Regen/Treasure)
                CreateRewardLayer(),
                
                // Layer 4: Boss
                CreateBossLayer(false)
            };

            return config;
        }

        /// <summary>
        /// Get node blueprints by boss type from a list
        /// </summary>
        public static List<NodeBlueprint> GetNodeBlueprintsForBoss(List<NodeBlueprint> allBlueprints, BossType bossType)
        {
            List<NodeBlueprint> filtered = new List<NodeBlueprint>();
            
            foreach (var blueprint in allBlueprints)
            {
                // Include if it's the boss itself
                if (blueprint.nodeType == NodeType.Boss && blueprint.bossType == bossType)
                {
                    filtered.Add(blueprint);
                }
                // Include if it's a minion for this boss
                else if (blueprint.nodeType == NodeType.Minion && blueprint.bossType == bossType)
                {
                    filtered.Add(blueprint);
                }
                // Include all reward nodes (they're not boss-specific)
                else if (blueprint.nodeType == NodeType.Shop || 
                         blueprint.nodeType == NodeType.Regen || 
                         blueprint.nodeType == NodeType.Treasure)
                {
                    filtered.Add(blueprint);
                }
            }
            
            return filtered;
        }
    }
}

