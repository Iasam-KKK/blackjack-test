using UnityEngine;
using UnityEngine.UI;
using System.Linq; // Added for OrderBy

public class BossProgressionTester : MonoBehaviour
{
    [Header("Test Controls")]
    public Button testPlayerWinButton;
    public Button testPlayerLoseButton;
    public Button testNextBossButton;
    public Button resetProgressButton;
    
    [Header("Debug Info")]
    public Text debugInfoText;
    
    private BossManager bossManager;
    
    private void Start()
    {
        bossManager = BossManager.Instance;
        
        if (testPlayerWinButton != null)
            testPlayerWinButton.onClick.AddListener(TestPlayerWin);
            
        if (testPlayerLoseButton != null)
            testPlayerLoseButton.onClick.AddListener(TestPlayerLose);
            
        if (testNextBossButton != null)
            testNextBossButton.onClick.AddListener(TestNextBoss);
            
        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(ResetProgress);
            
        UpdateDebugInfo();
    }
    
    private void Update()
    {
        UpdateDebugInfo();
    }
    
    private void TestPlayerWin()
    {
        if (bossManager != null)
        {
            bossManager.OnPlayerWin();
            Debug.Log("Test: Player Win triggered");
        }
    }
    
    private void TestPlayerLose()
    {
        if (bossManager != null)
        {
            bossManager.OnPlayerLose();
            Debug.Log("Test: Player Lose triggered");
        }
    }
    
    private void TestNextBoss()
    {
        if (bossManager != null)
        {
            var currentBoss = bossManager.GetCurrentBoss();
            if (currentBoss != null)
            {
                // Get the next boss based on unlock order
                int nextUnlockOrder = currentBoss.unlockOrder + 1;
                var availableBosses = bossManager.GetAvailableBosses();
                var nextBoss = availableBosses.Find(b => b.unlockOrder == nextUnlockOrder);
                
                if (nextBoss != null)
                {
                    bossManager.InitializeBoss(nextBoss.bossType);
                    Debug.Log($"Test: Progressed to next boss: {nextBoss.bossName} (Unlock Order: {nextBoss.unlockOrder})");
                }
                else
                {
                    Debug.Log($"Test: No boss found with unlock order {nextUnlockOrder}. Current boss unlock order: {currentBoss.unlockOrder}");
                    Debug.Log($"Available bosses: {string.Join(", ", availableBosses.Select(b => $"{b.bossName}({b.unlockOrder})"))}");
                }
            }
            else
            {
                Debug.Log("Test: No current boss found, starting with TheDrunkard");
                bossManager.InitializeBoss(BossType.TheDrunkard);
            }
        }
    }
    
    private void ResetProgress()
    {
        PlayerPrefs.DeleteKey("TotalBossesDefeated");
        PlayerPrefs.DeleteKey("CurrentBossType");
        PlayerPrefs.DeleteKey("CurrentBossHealth");
        PlayerPrefs.DeleteKey("CurrentHand");
        PlayerPrefs.Save();
        
        if (bossManager != null)
        {
            bossManager.InitializeBoss(BossType.TheDrunkard);
            Debug.Log("Test: Progress reset, starting with The Drunkard");
        }
    }
    
    private void UpdateDebugInfo()
    {
        if (debugInfoText != null && bossManager != null)
        {
            var currentBoss = bossManager.GetCurrentBoss();
            string info = $"Current Boss: {currentBoss?.bossName ?? "None"}\n";
            info += $"Boss Health: {bossManager.GetCurrentBossHealth()}/{currentBoss?.maxHealth ?? 0}\n";
            info += $"Total Defeated: {bossManager.GetTotalBossesDefeated()}\n";
            info += $"Unlock Order: {currentBoss?.unlockOrder ?? -1}\n";
            info += $"Tarot Allowed: {currentBoss?.allowTarotCards ?? false}\n";
            info += $"Active Mechanics: {currentBoss?.GetActiveMechanics().Count ?? 0}\n";
            
            // Add available bosses info
            var availableBosses = bossManager.GetAvailableBosses();
            info += $"Available Bosses ({availableBosses.Count}):\n";
            foreach (var boss in availableBosses.OrderBy(b => b.unlockOrder))
            {
                info += $"  {boss.bossName} (Order: {boss.unlockOrder})\n";
            }
            
            debugInfoText.text = info;
        }
    }
}
