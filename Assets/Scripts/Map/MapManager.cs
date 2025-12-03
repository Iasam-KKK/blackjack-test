using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Map
{
    public class MapManager : MonoBehaviour
    {
        public MapConfig config;
        public MapView view;

        public Map CurrentMap { get; private set; }

        private void Start()
        {
            if (PlayerPrefs.HasKey("Map"))
            {
                string mapJson = PlayerPrefs.GetString("Map");
                Map map = JsonConvert.DeserializeObject<Map>(mapJson);
                
                // Handle interrupted encounters - clean up pending nodes from the path
                CleanupInterruptedEncounter(map);
                
                // Only regenerate if STAGE boss (final boss) was defeated
                var stageBossNode = map.GetStageBossNode();
                if (stageBossNode != null && map.path.Any(p => p.Equals(stageBossNode.point)))
                {
                    // Player has reached the stage boss, generate a new map
                    Debug.Log("[MapManager] Stage boss reached, generating new map");
                    GenerateNewMap();
                }
                else
                {
                    CurrentMap = map;
                    // Player has not reached the stage boss yet, continue existing map
                    Debug.Log("[MapManager] Continuing existing map");
                    view.ShowMap(map);
                }
            }
            else
            {
                GenerateNewMap();
            }
        }
        
        /// <summary>
        /// Clean up the map path if there was an interrupted encounter
        /// This removes the pending node that was added to the path but never completed
        /// </summary>
        private void CleanupInterruptedEncounter(Map map)
        {
            if (GameProgressionManager.Instance == null)
            {
                Debug.LogWarning("[MapManager] GameProgressionManager.Instance is null, cannot check for interrupted encounters");
                return;
            }
            
            // Check if there's an interrupted encounter
            if (!GameProgressionManager.Instance.HasInterruptedEncounter())
            {
                return;
            }
            
            string pendingNodePoint = GameProgressionManager.Instance.GetPendingNodePoint();
            if (string.IsNullOrEmpty(pendingNodePoint))
            {
                Debug.Log("[MapManager] Interrupted encounter found but no pending node point to clean up");
                // Still need to clear the interrupted encounter state
                GameProgressionManager.Instance.ClearPendingNodePoint();
                return;
            }
            
            // Parse the pending node point
            string[] parts = pendingNodePoint.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
            {
                Debug.LogError($"[MapManager] Failed to parse pending node point: {pendingNodePoint}");
                GameProgressionManager.Instance.ClearPendingNodePoint();
                return;
            }
            
            Vector2Int pendingPoint = new Vector2Int(x, y);
            
            // Remove the pending node from the path if it's there
            if (map.path.Count > 0 && map.path[map.path.Count - 1].Equals(pendingPoint))
            {
                map.path.RemoveAt(map.path.Count - 1);
                Debug.Log($"[MapManager] Removed interrupted encounter node from path: {pendingPoint}");
                
                // Save the cleaned up map
                string json = JsonConvert.SerializeObject(map, Formatting.Indented,
                    new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
                PlayerPrefs.SetString("Map", json);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log($"[MapManager] Pending node {pendingPoint} not found at end of path, no cleanup needed");
            }
            
            // Clear the pending node point in GameProgressionManager
            GameProgressionManager.Instance.ClearPendingNodePoint();
        }

        public void GenerateNewMap()
        {
            // Use sequential map configuration for proper boss progression
            var sequentialConfig = MapConfigGenerator.GenerateMapConfig(config.nodeBlueprints);
            Map map = MapGenerator.GetMap(sequentialConfig);
            CurrentMap = map;
            Debug.Log(map.ToJson());
            view.ShowMap(map);
        }

        public void SaveMap()
        {
            if (CurrentMap == null) return;

            string json = JsonConvert.SerializeObject(CurrentMap, Formatting.Indented,
                new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore});
            PlayerPrefs.SetString("Map", json);
            PlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            SaveMap();
        }
    }
}
