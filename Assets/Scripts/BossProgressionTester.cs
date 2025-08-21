using UnityEngine;
using UnityEngine.UI;

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
            int currentDefeated = bossManager.GetTotalBossesDefeated();
            BossType nextBossType = (BossType)(currentDefeated + 1);
            
            if (nextBossType <= BossType.TheThief)
            {
                bossManager.InitializeBoss(nextBossType);
                Debug.Log($"Test: Initialized next boss: {nextBossType}");
            }
            else
            {
                Debug.Log("Test: No more bosses to test");
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
            info += $"Unlock Order: {currentBoss?.unlockOrder ?? 0}\n";
            info += $"Tarot Allowed: {currentBoss?.allowTarotCards ?? false}\n";
            info += $"Active Mechanics: {currentBoss?.GetActiveMechanics().Count ?? 0}";
            
            debugInfoText.text = info;
        }
    }
}
