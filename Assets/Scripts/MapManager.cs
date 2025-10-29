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

        public void GenerateNewMap()
        {
            // Use branching map configuration instead of sequential
            var branchingConfig = MapConfigGenerator.GenerateBranchingMapConfig(config.nodeBlueprints);
            Map map = MapGenerator.GetMap(branchingConfig);
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
