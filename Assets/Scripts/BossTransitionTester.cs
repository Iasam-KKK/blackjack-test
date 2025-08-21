using UnityEngine;
using UnityEngine.UI;

public class BossTransitionTester : MonoBehaviour
{
    [Header("Test Controls")]
    public Button testBossDefeatButton;
    public Button testBossTransitionButton;
    public Button testPreviewUpdateButton;
    
    [Header("Debug Info")]
    public Text debugInfoText;
    
    private BossManager bossManager;
    private NewBossPanel newBossPanel;
    private BossPreviewPanel bossPreviewPanel;
    
    private void Start()
    {
        bossManager = BossManager.Instance;
        newBossPanel = FindObjectOfType<NewBossPanel>();
        bossPreviewPanel = FindObjectOfType<BossPreviewPanel>();
        
        if (testBossDefeatButton != null)
            testBossDefeatButton.onClick.AddListener(TestBossDefeat);
            
        if (testBossTransitionButton != null)
            testBossTransitionButton.onClick.AddListener(TestBossTransition);
            
        if (testPreviewUpdateButton != null)
            testPreviewUpdateButton.onClick.AddListener(TestPreviewUpdate);
            
        UpdateDebugInfo();
    }
    
    private void Update()
    {
        UpdateDebugInfo();
    }
    
    private void TestBossDefeat()
    {
        if (bossManager != null)
        {
            // Simulate defeating the current boss
            bossManager.OnPlayerWin();
            Debug.Log("Test: Boss defeat triggered");
        }
    }
    
    private void TestBossTransition()
    {
        if (bossManager != null && newBossPanel != null)
        {
            // Find next boss
            int currentDefeated = bossManager.GetTotalBossesDefeated();
            var nextBoss = bossManager.allBosses.Find(b => b.unlockOrder == currentDefeated);
            
            if (nextBoss != null)
            {
                newBossPanel.ShowNextBossIntroduction(nextBoss);
                Debug.Log($"Test: Showing next boss introduction for {nextBoss.bossName}");
            }
            else
            {
                Debug.Log("Test: No next boss found");
            }
        }
    }
    
    private void TestPreviewUpdate()
    {
        if (bossPreviewPanel != null)
        {
            bossPreviewPanel.UpdateBossPreviewWithAnimation();
            Debug.Log("Test: Preview update triggered");
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
            info += $"NewBossPanel: {(newBossPanel != null ? "Found" : "Missing")}\n";
            info += $"BossPreviewPanel: {(bossPreviewPanel != null ? "Found" : "Missing")}";
            
            debugInfoText.text = info;
        }
    }
}
