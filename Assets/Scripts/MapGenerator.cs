using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{
    public static class MapGenerator
    {
        private static MapConfig config;

        private static List<float> layerDistances;
        // ALL nodes by layer:
        private static readonly List<List<Node>> nodes = new List<List<Node>>();

        public static Map GetMap(MapConfig conf)
        {
            if (conf == null)
            {
                Debug.LogWarning("Config was null in MapGenerator.Generate()");
                return null;
            }

            config = conf;
            nodes.Clear();

            GenerateLayerDistances();

            for (int i = 0; i < conf.layers.Count; i++)
                PlaceLayer(i);

            List<List<Vector2Int>> paths = GeneratePaths();

            RandomizeNodePositions();

            SetUpConnections(paths);

            RemoveCrossConnections();

            // select all the nodes with connections:
            List<Node> nodesList = nodes.SelectMany(n => n).Where(n => n.incoming.Count > 0 || n.outgoing.Count > 0).ToList();

            // Log map structure for debugging
            LogMapStructure(nodesList);

            // pick a random name of the boss level for this map:
            string bossNodeName = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList().Random().name;
            return new Map(conf.name, bossNodeName, nodesList, new List<Vector2Int>());
        }

        private static void GenerateLayerDistances()
        {
            layerDistances = new List<float>();
            foreach (MapLayer layer in config.layers)
                layerDistances.Add(layer.distanceFromPreviousLayer.GetValue());
        }

        private static float GetDistanceToLayer(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex > layerDistances.Count) return 0f;

            return layerDistances.Take(layerIndex + 1).Sum();
        }

        /// <summary>
        /// Determine how many nodes to place on a specific layer
        /// </summary>
        private static int GetNodeCountForLayer(int layerIndex)
        {
            if (layerIndex >= config.layers.Count) return 0;

            MapLayer layer = config.layers[layerIndex];
            
            // For branching maps, determine node count based on layer type
            if (layer.nodeType == NodeType.Boss)
            {
                // Boss layer: count how many unique bosses we have
                var bossBlueprints = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList();
                
                // If this is the stage boss layer (last layer), only 1 node
                if (layerIndex == config.layers.Count - 1)
                {
                    return 1;
                }
                // Otherwise, place all bosses
                else
                {
                    return Mathf.Min(bossBlueprints.Count, 5); // Max 5 bosses
                }
            }
            else if (layer.nodeType == NodeType.Minion)
            {
                // Minion layers: 2-5 nodes for branching
                if (layerIndex == 0) return 2; // Starting branches
                return Random.Range(3, 6); // Random splits
            }
            else
            {
                // Reward layers: 4-6 nodes
                return Random.Range(4, 7);
            }
        }

        private static void PlaceLayer(int layerIndex)
        {
            MapLayer layer = config.layers[layerIndex];
            List<Node> nodesOnThisLayer = new List<Node>();

            // Determine how many nodes to place on this layer
            int nodeCount = GetNodeCountForLayer(layerIndex);
            
            // offset of this layer to make all the nodes centered:
            float offset = layer.nodesApartDistance * nodeCount / 2f;

            for (int i = 0; i < nodeCount; i++)
            {
                var supportedRandomNodeTypes =
                    config.rewardNodes.Where(t => config.nodeBlueprints.Any(b => b.nodeType == t)).ToList();
                NodeType nodeType = Random.Range(0f, 1f) < layer.randomizeNodes && supportedRandomNodeTypes.Count > 0
                    ? supportedRandomNodeTypes.Random()
                    : layer.nodeType;
                
                // Get all blueprints of this type
                var matchingBlueprints = GetFilteredBlueprints(nodeType, layerIndex);
                
                if (matchingBlueprints.Count == 0)
                {
                    Debug.LogError($"[MapGenerator] No blueprints found for NodeType {nodeType} on layer {layerIndex}. " +
                                   $"Make sure you have NodeBlueprints with the correct nodeType:\n" +
                                   $"Minion=0, Boss=1, Regen=2, Treasure=3, Shop=4");
                    return;
                }
                
                // For boss convergence layer, use specific boss for each position
                NodeBlueprint selectedBlueprint;
                if (nodeType == NodeType.Boss && layerIndex == config.layers.Count - 2)
                {
                    // Use the i-th unique boss blueprint
                    selectedBlueprint = i < matchingBlueprints.Count ? matchingBlueprints[i] : matchingBlueprints.Random();
                    Debug.Log($"[MapGenerator] Placing boss {selectedBlueprint.bossType} at position {i}");
                }
                else
                {
                    selectedBlueprint = matchingBlueprints.Random();
                }
                
                Node node = new Node(nodeType, selectedBlueprint.name, new Vector2Int(i, layerIndex))
                {
                    position = new Vector2(-offset + i * layer.nodesApartDistance, GetDistanceToLayer(layerIndex))
                };
                nodesOnThisLayer.Add(node);
            }

            nodes.Add(nodesOnThisLayer);
        }
        
        /// <summary>
        /// Get filtered blueprints based on layer context and boss associations
        /// </summary>
        private static List<NodeBlueprint> GetFilteredBlueprints(NodeType nodeType, int layerIndex)
        {
            var allMatchingBlueprints = config.nodeBlueprints.Where(b => b.nodeType == nodeType).ToList();
            
            // For boss layers in branching maps, we want to place different bosses
            if (nodeType == NodeType.Boss)
            {
                // For the boss convergence layer (second to last), place all unique bosses
                if (layerIndex == config.layers.Count - 2)
                {
                    // Get unique boss blueprints (one per boss type)
                    var uniqueBosses = new List<NodeBlueprint>();
                    var usedBossTypes = new HashSet<BossType>();
                    
                    foreach (var blueprint in allMatchingBlueprints)
                    {
                        if (!usedBossTypes.Contains(blueprint.bossType))
                        {
                            uniqueBosses.Add(blueprint);
                            usedBossTypes.Add(blueprint.bossType);
                        }
                    }
                    
                    Debug.Log($"[MapGenerator] Found {uniqueBosses.Count} unique boss types for convergence layer");
                    return uniqueBosses;
                }
                // For stage boss layer (last layer), just return all bosses (will pick one randomly)
                else if (layerIndex == config.layers.Count - 1)
                {
                    Debug.Log($"[MapGenerator] Stage boss layer - using all boss blueprints");
                    return allMatchingBlueprints;
                }
            }
            
            // For minions, try to filter by boss association if we can determine the boss for this layer
            if (nodeType == NodeType.Minion)
            {
                var bossForLayer = GetBossForLayer(layerIndex);
                if (bossForLayer != null)
                {
                    var bossMinions = allMatchingBlueprints.Where(b => 
                        b.minionData != null && 
                        b.minionData.associatedBossType == bossForLayer.Value).ToList();
                    
                    if (bossMinions.Count > 0)
                    {
                        Debug.Log($"[MapGenerator] Filtered to {bossMinions.Count} minions for boss {bossForLayer.Value} on layer {layerIndex}");
                        return bossMinions;
                    }
                    else
                    {
                        Debug.LogWarning($"[MapGenerator] No minions found for boss {bossForLayer.Value} on layer {layerIndex}, using all minions");
                    }
                }
            }
            
            return allMatchingBlueprints;
        }
        
        /// <summary>
        /// Determine which boss this layer should be associated with
        /// </summary>
        private static BossType? GetBossForLayer(int layerIndex)
        {
            // Find the boss layer that comes after this minion layer
            for (int i = layerIndex + 1; i < config.layers.Count; i++)
            {
                if (config.layers[i].nodeType == NodeType.Boss)
                {
                    // Find the boss blueprint for this layer
                    var bossBlueprint = config.nodeBlueprints.FirstOrDefault(b => b.nodeType == NodeType.Boss);
                    if (bossBlueprint != null)
                    {
                        return bossBlueprint.bossType;
                    }
                }
            }
            
            return null;
        }

        private static void RandomizeNodePositions()
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                List<Node> list = nodes[index];
                MapLayer layer = config.layers[index];
                float distToNextLayer = index + 1 >= layerDistances.Count
                    ? 0f
                    : layerDistances[index + 1];
                float distToPreviousLayer = layerDistances[index];

                foreach (Node node in list)
                {
                    float xRnd = Random.Range(-0.5f, 0.5f);
                    float yRnd = Random.Range(-0.5f, 0.5f);

                    float x = xRnd * layer.nodesApartDistance;
                    float y = yRnd < 0 ? distToPreviousLayer * yRnd: distToNextLayer * yRnd;

                    node.position += new Vector2(x, y) * layer.randomizePosition;
                }
            }
        }

        private static void SetUpConnections(List<List<Vector2Int>> paths)
        {
            foreach (List<Vector2Int> path in paths)
            {
                for (int i = 0; i < path.Count - 1; ++i)
                {
                    Node node = GetNode(path[i]);
                    Node nextNode = GetNode(path[i + 1]);
                    node.AddOutgoing(nextNode.point);
                    nextNode.AddIncoming(node.point);
                }
            }
        }

        private static void RemoveCrossConnections()
        {
            for (int i = 0; i < config.GridWidth - 1; ++i)
                for (int j = 0; j < config.layers.Count - 1; ++j)
                {
                    Node node = GetNode(new Vector2Int(i, j));
                    if (node == null || node.HasNoConnections()) continue;
                    Node right = GetNode(new Vector2Int(i + 1, j));
                    if (right == null || right.HasNoConnections()) continue;
                    Node top = GetNode(new Vector2Int(i, j + 1));
                    if (top == null || top.HasNoConnections()) continue;
                    Node topRight = GetNode(new Vector2Int(i + 1, j + 1));
                    if (topRight == null || topRight.HasNoConnections()) continue;

                    // Debug.Log("Inspecting node for connections: " + node.point);
                    if (!node.outgoing.Any(element => element.Equals(topRight.point))) continue;
                    if (!right.outgoing.Any(element => element.Equals(top.point))) continue;

                    // Debug.Log("Found a cross node: " + node.point);

                    // we managed to find a cross node:
                    // 1) add direct connections:
                    node.AddOutgoing(top.point);
                    top.AddIncoming(node.point);

                    right.AddOutgoing(topRight.point);
                    topRight.AddIncoming(right.point);

                    float rnd = Random.Range(0f, 1f);
                    if (rnd < 0.2f)
                    {
                        // remove both cross connections:
                        // a) 
                        node.RemoveOutgoing(topRight.point);
                        topRight.RemoveIncoming(node.point);
                        // b) 
                        right.RemoveOutgoing(top.point);
                        top.RemoveIncoming(right.point);
                    }
                    else if (rnd < 0.6f)
                    {
                        // a) 
                        node.RemoveOutgoing(topRight.point);
                        topRight.RemoveIncoming(node.point);
                    }
                    else
                    {
                        // b) 
                        right.RemoveOutgoing(top.point);
                        top.RemoveIncoming(right.point);
                    }
                }
        }

        private static Node GetNode(Vector2Int p)
        {
            if (p.y >= nodes.Count) return null;
            if (p.x >= nodes[p.y].Count) return null;

            return nodes[p.y][p.x];
        }

        private static Vector2Int GetFinalNode()
        {
            int y = config.layers.Count - 1;
            int nodeCount = GetNodeCountForLayer(y);
            
            if (nodeCount % 2 == 1)
                return new Vector2Int(nodeCount / 2, y);

            return Random.Range(0, 2) == 0
                ? new Vector2Int(nodeCount / 2, y)
                : new Vector2Int(nodeCount / 2 - 1, y);
        }

        private static List<List<Vector2Int>> GeneratePaths()
        {
            Vector2Int finalNode = GetFinalNode();
            var paths = new List<List<Vector2Int>>();
            
            // For branching maps, create paths from each starting node to each boss
            int numOfStartingNodes = GetNodeCountForLayer(0);
            int numOfBossNodes = GetNodeCountForLayer(config.layers.Count - 2); // Second to last layer (boss convergence)
            
            List<int> candidateXs = new List<int>();
            for (int i = 0; i < numOfStartingNodes; i++)
                candidateXs.Add(i);

            candidateXs.Shuffle();
            List<Vector2Int> startingPoints = (from x in candidateXs select new Vector2Int(x, 0)).ToList();

            // Create paths from each starting node to each boss node
            for (int startIdx = 0; startIdx < numOfStartingNodes; startIdx++)
            {
                for (int bossIdx = 0; bossIdx < numOfBossNodes; bossIdx++)
                {
                    Vector2Int startNode = startingPoints[startIdx];
                    Vector2Int bossNode = new Vector2Int(bossIdx, config.layers.Count - 2);
                    List<Vector2Int> path = Path(startNode, bossNode);
                    path.Add(finalNode); // Add stage boss at the end
                    paths.Add(path);
                }
            }

            return paths;
        }

        // Generates a random path bottom up.
        private static List<Vector2Int> Path(Vector2Int fromPoint, Vector2Int toPoint)
        {
            int toRow = toPoint.y;
            int toCol = toPoint.x;

            int lastNodeCol = fromPoint.x;

            List<Vector2Int> path = new List<Vector2Int> { fromPoint };
            List<int> candidateCols = new List<int>();
            for (int row = 1; row < toRow; ++row)
            {
                candidateCols.Clear();

                int verticalDistance = toRow - row;
                int horizontalDistance;
                int maxCols = GetNodeCountForLayer(row);

                int forwardCol = lastNodeCol;
                horizontalDistance = Mathf.Abs(toCol - forwardCol);
                if (horizontalDistance <= verticalDistance && forwardCol < maxCols)
                    candidateCols.Add(lastNodeCol);

                int leftCol = lastNodeCol - 1;
                horizontalDistance = Mathf.Abs(toCol - leftCol);
                if (leftCol >= 0 && horizontalDistance <= verticalDistance)
                    candidateCols.Add(leftCol);

                int rightCol = lastNodeCol + 1;
                horizontalDistance = Mathf.Abs(toCol - rightCol);
                if (rightCol < maxCols && horizontalDistance <= verticalDistance)
                    candidateCols.Add(rightCol);

                if (candidateCols.Count == 0)
                {
                    // Fallback: pick any valid column for this row
                    candidateCols.Add(Random.Range(0, maxCols));
                }

                int randomCandidateIndex = Random.Range(0, candidateCols.Count);
                int candidateCol = candidateCols[randomCandidateIndex];
                Vector2Int nextPoint = new Vector2Int(candidateCol, row);

                path.Add(nextPoint);

                lastNodeCol = candidateCol;
            }

            path.Add(toPoint);

            return path;
        }

        /// <summary>
        /// Log the map structure for debugging
        /// </summary>
        private static void LogMapStructure(List<Node> nodesList)
        {
            Debug.Log("=== MAP STRUCTURE DEBUG ===");
            
            var nodesByLayer = nodesList.GroupBy(n => n.point.y).OrderBy(g => g.Key);
            
            foreach (var layer in nodesByLayer)
            {
                Debug.Log($"Layer {layer.Key}: {layer.Count()} nodes");
                foreach (var node in layer)
                {
                    string nodeInfo = $"{node.nodeType} - {node.blueprintName} (ID: {node.nodeInstanceId})";
                    if (node.nodeType == NodeType.Boss)
                    {
                        // Find the blueprint to get boss type
                        var blueprint = config.nodeBlueprints.FirstOrDefault(b => b.name == node.blueprintName);
                        if (blueprint != null)
                        {
                            nodeInfo += $" - BossType: {blueprint.bossType}";
                        }
                    }
                    Debug.Log($"  {nodeInfo}");
                }
            }
            
            Debug.Log("=== END MAP STRUCTURE ===");
        }
    }
}