using System.Collections.Generic;
using System.Linq;
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

            // Group minions by their associated boss
            var minionsByBoss = GroupMinionsByBoss(allNodeBlueprints);
            var bossBlueprints = allNodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList();

            // For each boss, create: 3 minion layers + 1 reward layer + 1 boss layer = 5 layers per boss
            for (int bossIndex = 0; bossIndex < numBosses && bossIndex < bossBlueprints.Count; bossIndex++)
            {
                var bossBlueprint = bossBlueprints[bossIndex];
                var bossType = bossBlueprint.bossType;

                Debug.Log($"[MapConfigGenerator] Creating map section for boss: {bossType} (boss {bossIndex + 1}/{numBosses})");

                // Add 3 minion layers with minions associated to this boss
                for (int minionLayer = 0; minionLayer < 3; minionLayer++)
                {
                    config.layers.Add(CreateMinionLayerForBoss(minionLayer, bossType, minionsByBoss, bossIndex == 0 && minionLayer == 0));
                }

                // Add 1 reward layer (Shop, Regen, Treasure randomly placed)
                config.layers.Add(CreateRewardLayer());

                // Add 1 boss layer for this specific boss
                config.layers.Add(CreateBossLayer(bossIndex == numBosses - 1, bossType));
                Debug.Log($"[MapConfigGenerator] Added boss layer for {bossType}");
            }

            return config;
        }
        
        /// <summary>
        /// Group minion blueprints by their associated boss type
        /// </summary>
        private static Dictionary<BossType, List<NodeBlueprint>> GroupMinionsByBoss(List<NodeBlueprint> allNodeBlueprints)
        {
            var minionsByBoss = new Dictionary<BossType, List<NodeBlueprint>>();
            
            var minionBlueprints = allNodeBlueprints.Where(b => b.nodeType == NodeType.Minion).ToList();
            
            foreach (var minionBlueprint in minionBlueprints)
            {
                if (minionBlueprint.minionData != null)
                {
                    var bossType = minionBlueprint.minionData.associatedBossType;

                    if (!minionsByBoss.ContainsKey(bossType))
                    {
                        minionsByBoss[bossType] = new List<NodeBlueprint>();
                    }

                    minionsByBoss[bossType].Add(minionBlueprint);
                    Debug.Log($"[MapConfigGenerator] Grouped minion '{minionBlueprint.nodeName}' under boss '{bossType}'");
                }
            }
            
            return minionsByBoss;
        }

        /// <summary>
        /// Create a minion layer for a specific boss
        /// </summary>
        private static MapLayer CreateMinionLayerForBoss(int layerIndex, BossType bossType, Dictionary<BossType, List<NodeBlueprint>> minionsByBoss, bool isFirstLayer)
        {
            MapLayer layer = new MapLayer
            {
                nodeType = NodeType.Minion,
                distanceFromPreviousLayer = new FloatMinMax
                {
                    min = isFirstLayer ? 0f : 3f,
                    max = isFirstLayer ? 0f : 4f
                },
                nodesApartDistance = 2f,
                randomizePosition = 0.1f,
                randomizeNodes = 0f, // No randomization - always minions
                bossTypeFilter = bossType,
                useBossFilter = true
            };

            // Verify minions exist for this boss
            if (minionsByBoss.ContainsKey(bossType))
            {
                var bossMinions = minionsByBoss[bossType];
                Debug.Log($"[MapConfigGenerator] Created layer for boss {bossType} with {bossMinions.Count} available minions");
            }
            else
            {
                Debug.LogWarning($"[MapConfigGenerator] No minions found for boss {bossType}, layer will be empty!");
            }

            return layer;
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
        private static MapLayer CreateBossLayer(bool isFinalBoss, BossType bossType = BossType.TheDrunkard)
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
                randomizeNodes = 0f, // No randomization - always boss
                bossTypeFilter = bossType,
                useBossFilter = true
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
        /// Generate a branching map config where all 5 acts are visible simultaneously
        /// Structure: 2 starting branches -> random splits -> 5 boss nodes -> 1 stage boss
        /// </summary>
        public static MapConfig GenerateBranchingMapConfig(List<NodeBlueprint> allNodeBlueprints, int numBosses = 5)
        {
            MapConfig config = ScriptableObject.CreateInstance<MapConfig>();
            config.nodeBlueprints = allNodeBlueprints;
            config.rewardNodes = new List<NodeType> { NodeType.Shop, NodeType.Regen, NodeType.Treasure };
            config.extraPaths = 2; // More paths for branching
            config.layers = new List<MapLayer>();

            var bossBlueprints = allNodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList();
            Debug.Log($"[MapConfigGenerator] Creating branching map with {bossBlueprints.Count} bosses");

            // Layer 0: 2 starting minion nodes (initial branch point)
            config.layers.Add(CreateBranchingMinionLayer(0, true, 2));

            // Layer 1-2: More minion layers with random additional splits (3-5 nodes)
            config.layers.Add(CreateBranchingMinionLayer(1, false, Random.Range(3, 6)));
            config.layers.Add(CreateBranchingMinionLayer(2, false, Random.Range(3, 6)));

            // Layer 3: Reward layer (Shop/Regen/Treasure) - 4-6 nodes
            config.layers.Add(CreateBranchingRewardLayer(3, Random.Range(4, 7)));

            // Layer 4: 5 boss nodes (one per boss type) - paths converge here
            config.layers.Add(CreateBossConvergenceLayer(4, numBosses));

            // Layer 5: Single stage boss node (final convergence)
            config.layers.Add(CreateStageBossLayer(5));

            Debug.Log($"[MapConfigGenerator] Created branching map with {config.layers.Count} layers");
            return config;
        }

        /// <summary>
        /// Create a minion layer for branching map
        /// </summary>
        private static MapLayer CreateBranchingMinionLayer(int layerIndex, bool isFirstLayer, int nodeCount)
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

            // Set GridWidth to nodeCount for this layer
            // Note: This will be handled in MapGenerator by using the layer's nodeCount
            return layer;
        }

        /// <summary>
        /// Create a reward layer for branching map
        /// </summary>
        private static MapLayer CreateBranchingRewardLayer(int layerIndex, int nodeCount)
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
        /// Create boss convergence layer where all 5 bosses are placed
        /// </summary>
        private static MapLayer CreateBossConvergenceLayer(int layerIndex, int numBosses)
        {
            MapLayer layer = new MapLayer
            {
                nodeType = NodeType.Boss,
                distanceFromPreviousLayer = new FloatMinMax
                {
                    min = 4f,
                    max = 6f
                },
                nodesApartDistance = 3f,
                randomizePosition = 0.1f,
                randomizeNodes = 0f // No randomization - always boss
            };

            return layer;
        }

        /// <summary>
        /// Create stage boss layer (final boss)
        /// </summary>
        private static MapLayer CreateStageBossLayer(int layerIndex)
        {
            MapLayer layer = new MapLayer
            {
                nodeType = NodeType.Boss,
                distanceFromPreviousLayer = new FloatMinMax
                {
                    min = 5f,
                    max = 7f
                },
                nodesApartDistance = 3f,
                randomizePosition = 0.1f,
                randomizeNodes = 0f // No randomization - always boss
            };

            return layer;
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
                else if (blueprint.nodeType == NodeType.Minion && blueprint.minionData != null && blueprint.minionData.associatedBossType == bossType)
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

        /// <summary>
        /// Debug method to validate map config generation
        /// </summary>
        public static void ValidateMapConfig(MapConfig config)
        {
            Debug.Log($"[MapConfigValidator] Validating config with {config.layers.Count} layers and {config.nodeBlueprints.Count} blueprints");

            var bossBlueprints = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList();
            var minionBlueprints = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Minion).ToList();

            Debug.Log($"[MapConfigValidator] Found {bossBlueprints.Count} boss blueprints and {minionBlueprints.Count} minion blueprints");

            // Group minions by boss for validation
            var minionsByBoss = new System.Collections.Generic.Dictionary<BossType, List<NodeBlueprint>>();
            foreach (var minion in minionBlueprints)
            {
                if (minion.minionData != null)
                {
                    var bossType = minion.minionData.associatedBossType;
                    if (!minionsByBoss.ContainsKey(bossType))
                        minionsByBoss[bossType] = new List<NodeBlueprint>();
                    minionsByBoss[bossType].Add(minion);
                }
            }

            Debug.Log($"[MapConfigValidator] Minions grouped by boss:");
            foreach (var kvp in minionsByBoss)
            {
                Debug.Log($"[MapConfigValidator]   {kvp.Key}: {kvp.Value.Count} minions");
            }

            // Validate layers
            for (int i = 0; i < config.layers.Count; i++)
            {
                var layer = config.layers[i];
                Debug.Log($"[MapConfigValidator] Layer {i}: {layer.nodeType}, bossFilter={layer.useBossFilter}, bossType={layer.bossTypeFilter}");

                if (layer.useBossFilter)
                {
                    if (layer.nodeType == NodeType.Minion)
                    {
                        var availableMinions = config.nodeBlueprints.Where(b =>
                            b.nodeType == NodeType.Minion &&
                            b.minionData != null &&
                            b.minionData.associatedBossType == layer.bossTypeFilter).ToList();

                        Debug.Log($"[MapConfigValidator]   Layer {i} expects minions for {layer.bossTypeFilter}, found {availableMinions.Count} available");
                    }
                    else if (layer.nodeType == NodeType.Boss)
                    {
                        var availableBosses = config.nodeBlueprints.Where(b =>
                            b.nodeType == NodeType.Boss &&
                            b.bossType == layer.bossTypeFilter).ToList();

                        Debug.Log($"[MapConfigValidator]   Layer {i} expects boss {layer.bossTypeFilter}, found {availableBosses.Count} available");
                    }
                }
            }
        }
    }
}

