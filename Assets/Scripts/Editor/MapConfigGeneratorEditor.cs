using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Map
{
    /// <summary>
    /// Editor utility to generate map configurations
    /// Access via: Tools > Map > Generate Map Config
    /// </summary>
    public class MapConfigGeneratorEditor : EditorWindow
    {
        private string configName = "GeneratedMapConfig";
        private int numBosses = 5;
        private bool useSimpleConfig = false;

        [MenuItem("Tools/Map/Generate Map Config")]
        public static void ShowWindow()
        {
            GetWindow<MapConfigGeneratorEditor>("Map Config Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Map Config Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            configName = EditorGUILayout.TextField("Config Name", configName);
            useSimpleConfig = EditorGUILayout.Toggle("Simple Config (1 Boss)", useSimpleConfig);
            
            if (!useSimpleConfig)
            {
                numBosses = EditorGUILayout.IntField("Number of Bosses", numBosses);
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Generate Map Config"))
            {
                GenerateConfig();
            }

            GUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This will create a new MapConfig asset with the appropriate layer structure:\n" +
                "- Simple: 3 Minions → Rewards → Boss\n" +
                "- Full: (3 Minions → Rewards → Boss) × Number of Bosses\n\n" +
                "Make sure to assign NodeBlueprints to the config after creation!",
                MessageType.Info
            );
        }

        private void GenerateConfig()
        {
            // Find all NodeBlueprint assets
            string[] guids = AssetDatabase.FindAssets("t:NodeBlueprint");
            var blueprints = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<NodeBlueprint>(path))
                .Where(bp => bp != null)
                .ToList();

            if (blueprints.Count == 0)
            {
                EditorUtility.DisplayDialog("No Blueprints Found", 
                    "No NodeBlueprint assets found in the project. Create some first!", "OK");
                return;
            }

            MapConfig config;
            
            if (useSimpleConfig)
            {
                config = MapConfigGenerator.GenerateSimpleMapConfig(blueprints);
            }
            else
            {
                config = MapConfigGenerator.GenerateMapConfig(blueprints, numBosses);
            }

            config.name = configName;

            // Save the asset
            string path = $"Assets/MapObjects/{configName}.asset";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(config, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Select the newly created asset in the project window
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            EditorUtility.DisplayDialog("Success", 
                $"Map config created at: {uniquePath}\n\n" +
                $"Layers: {config.layers.Count}\n" +
                $"Blueprints: {config.nodeBlueprints.Count}\n\n" +
                "The config is now selected in the Project window.", "OK");
        }
    }
}

