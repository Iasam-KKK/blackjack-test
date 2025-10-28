using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map
{
    /// <summary>
    /// Handles returning to the map scene after completing a battle or node interaction
    /// Add this script to the Blackjack scene or call it when battle ends
    /// </summary>
    public class MapReturnHandler : MonoBehaviour
    {
        public static MapReturnHandler Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Call this when the player wins or loses a battle
        /// </summary>
        public void ReturnToMap()
        {
            if (PlayerPrefs.GetString("ReturnToMap", "false") == "true")
            {
                Debug.Log("[MapReturnHandler] Returning to map scene");
                
                // Clear the return flag
                PlayerPrefs.SetString("ReturnToMap", "false");
                PlayerPrefs.Save();
                
                // Load the map scene
                SceneManager.LoadScene("BossMap");
            }
            else
            {
                Debug.LogWarning("[MapReturnHandler] No map return flag set");
            }
        }

        /// <summary>
        /// Call this when player wins a minion/boss battle
        /// </summary>
        public void OnBattleWon()
        {
            string nodeType = PlayerPrefs.GetString("CurrentNodeType", "");
            
            Debug.Log($"[MapReturnHandler] Battle won! Node type: {nodeType}");
            
            // The progression is already handled by MinionEncounterManager/BossManager
            // Just return to map
            ReturnToMap();
        }

        /// <summary>
        /// Call this when player loses a minion/boss battle
        /// </summary>
        public void OnBattleLost()
        {
            string nodeType = PlayerPrefs.GetString("CurrentNodeType", "");
            
            Debug.Log($"[MapReturnHandler] Battle lost! Node type: {nodeType}");
            
            // Return to map - player can try again or choose different path
            ReturnToMap();
        }

        /// <summary>
        /// Auto-check on Start if we need to return to map
        /// </summary>
        private void Start()
        {
            // If we just loaded the Blackjack scene and there's no active battle setup,
            // this might be a bug - log it
            if (SceneManager.GetActiveScene().name == "Blackjack")
            {
                bool hasMinion = MinionEncounterManager.Instance != null && 
                                 MinionEncounterManager.Instance.isMinionActive;
                bool hasBoss = BossManager.Instance != null && 
                               BossManager.Instance.IsBossActive();
                
                if (!hasMinion && !hasBoss)
                {
                    Debug.LogWarning("[MapReturnHandler] In Blackjack scene but no active encounter!");
                }
            }
        }
    }
}

