using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Map
{
    /// <summary>
    /// Editor utility to fix node blueprint nodeType values
    /// Use this if you need to batch-update blueprints
    /// </summary>
    public class NodeBlueprintFixer : EditorWindow
    {
        [MenuItem("Tools/Map/Fix Node Blueprint Types")]
        public static void ShowWindow()
        {
            GetWindow<NodeBlueprintFixer>("Node Blueprint Fixer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Node Blueprint Type Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will scan all NodeBlueprint assets and help you verify/fix their nodeType values.\n\n" +
                "Expected values:\n" +
                "• Minion = 0\n" +
                "• Boss = 1\n" +
                "• Regen = 2\n" +
                "• Treasure = 3\n" +
                "• Shop = 4",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Scan All Node Blueprints"))
            {
                ScanBlueprints();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Auto-Fix Based on Name"))
            {
                AutoFixBlueprints();
            }
        }

        private void ScanBlueprints()
        {
            string[] guids = AssetDatabase.FindAssets("t:NodeBlueprint");
            var blueprints = guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => new { path, bp = AssetDatabase.LoadAssetAtPath<NodeBlueprint>(path) })
                .Where(x => x.bp != null)
                .ToList();

            Debug.Log($"=== Node Blueprint Scan ===");
            Debug.Log($"Found {blueprints.Count} NodeBlueprint assets");
            
            var grouped = blueprints.GroupBy(x => x.bp.nodeType);
            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                Debug.Log($"\nNodeType {group.Key} ({group.Key}):");
                foreach (var item in group)
                {
                    Debug.Log($"  - {item.bp.name} (BossType: {item.bp.bossType})");
                }
            }

            EditorUtility.DisplayDialog("Scan Complete", 
                $"Found {blueprints.Count} NodeBlueprint assets.\n\n" +
                "Check the Console for detailed results.", "OK");
        }

        private void AutoFixBlueprints()
        {
            string[] guids = AssetDatabase.FindAssets("t:NodeBlueprint");
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NodeBlueprint bp = AssetDatabase.LoadAssetAtPath<NodeBlueprint>(path);
                
                if (bp == null) continue;

                NodeType expectedType = GetExpectedNodeType(bp.name);
                
                if (bp.nodeType != expectedType)
                {
                    Debug.Log($"Fixing {bp.name}: {bp.nodeType} → {expectedType}");
                    bp.nodeType = expectedType;
                    EditorUtility.SetDirty(bp);
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Auto-Fix Complete", 
                $"Fixed {fixedCount} blueprint(s).\n\n" +
                "Check the Console for details.", "OK");
        }

        private NodeType GetExpectedNodeType(string name)
        {
            string lowerName = name.ToLower();

            if (lowerName.Contains("minion"))
                return NodeType.Minion;
            if (lowerName.Contains("boss"))
                return NodeType.Boss;
            if (lowerName.Contains("shop"))
                return NodeType.Shop;
            if (lowerName.Contains("regen") || lowerName.Contains("rest"))
                return NodeType.Regen;
            if (lowerName.Contains("treasure") || lowerName.Contains("reward"))
                return NodeType.Treasure;

            // Default to minion if unclear
            return NodeType.Minion;
        }
    }
}

