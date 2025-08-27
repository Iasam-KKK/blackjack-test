using UnityEngine;

public class BossIntroTester : MonoBehaviour
{
    public BossIntroPreviewPanel bossIntroPanel;
    public BossManager bossManager;
    
    private void Start()
    {
        Debug.Log("BossIntroTester: Starting tests...");
        
        // Test 1: Check if references are assigned
        Debug.Log($"BossIntroTester: BossIntroPanel assigned: {bossIntroPanel != null}");
        Debug.Log($"BossIntroTester: BossManager assigned: {bossManager != null}");
        
        // Test 2: Check BossManager instance
        var instance = BossManager.Instance;
        Debug.Log($"BossIntroTester: BossManager.Instance: {instance != null}");
        
        if (instance != null)
        {
            var currentBoss = instance.GetCurrentBoss();
            Debug.Log($"BossIntroTester: Current boss: {currentBoss?.bossName ?? "null"}");
        }
    }
    
    [ContextMenu("Force Test Intro Panel")]
    public void ForceTestIntroPanel()
    {
        Debug.Log("BossIntroTester: Force testing intro panel...");
        
        if (bossIntroPanel != null)
        {
            bossIntroPanel.TestShowIntroPanel();
        }
        else
        {
            Debug.LogError("BossIntroTester: BossIntroPanel reference is null!");
        }
    }
    
    [ContextMenu("Test Boss Manager Connection")]
    public void TestBossManagerConnection()
    {
        Debug.Log("BossIntroTester: Testing BossManager connection...");
        
        var instance = BossManager.Instance;
        if (instance != null)
        {
            Debug.Log($"BossManager found: {instance.name}");
            var currentBoss = instance.GetCurrentBoss();
            if (currentBoss != null)
            {
                Debug.Log($"Current boss: {currentBoss.bossName}");
                
                if (bossIntroPanel != null)
                {
                    Debug.Log("Calling ShowBossIntro with current boss...");
                    bossIntroPanel.ShowBossIntro(currentBoss);
                }
                else
                {
                    Debug.LogError("BossIntroPanel reference is null!");
                }
            }
            else
            {
                Debug.LogError("No current boss found!");
            }
        }
        else
        {
            Debug.LogError("BossManager.Instance is null!");
        }
    }
}
