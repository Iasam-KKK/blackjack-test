using UnityEngine;
using UnityEngine.UI;

public class GameHistoryEntryUI : MonoBehaviour
{
    [Header("UI Components")]
    public Text roundText;
    public Text blindLevelText;
    public Text playerScoreText;
    public Text dealerScoreText;
    public Text betText;
    public Text balanceText;
    public Text outcomeText;
    
    public void SetupEntry(GameHistoryEntry entry)
    {
        if (roundText != null) roundText.text = entry.GetRoundText();
        if (blindLevelText != null) blindLevelText.text = entry.GetBlindText();
        if (playerScoreText != null) playerScoreText.text = entry.GetPlayerScoreText();
        if (dealerScoreText != null) dealerScoreText.text = entry.GetDealerScoreText();
        if (betText != null) betText.text = entry.GetBetText();
        if (balanceText != null) balanceText.text = entry.GetBalanceChangeText();
        if (outcomeText != null) outcomeText.text = entry.outcome;
        
        // Optional: Color code the outcome text
        if (outcomeText != null)
        {
            switch (entry.outcome.ToLower())
            {
                case "win":
                    outcomeText.color = Color.green;
                    break;
                case "lose":
                case "you lose!":
                    outcomeText.color = Color.red;
                    break;
                case "draw":
                    outcomeText.color = Color.yellow;
                    break;
                default:
                    outcomeText.color = Color.white;
                    break;
            }
        }
        
        // Optional: Color code the balance text based on gain/loss
        if (balanceText != null)
        {
            long change = (long)entry.balanceAfter - (long)entry.balanceBefore;
            if (change > 0)
            {
                balanceText.color = Color.green;
            }
            else if (change < 0)
            {
                balanceText.color = Color.red;
            }
            else
            {
                balanceText.color = Color.white;
            }
        }
    }
} 