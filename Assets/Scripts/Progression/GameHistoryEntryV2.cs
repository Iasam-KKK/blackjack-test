using System;
using UnityEngine;

[System.Serializable]
public class GameHistoryEntryV2
{
    public BossData bossData;
    public float winLossAmount; // Positive for wins, negative for losses, 0 for draws
    public DateTime timestamp;
    public string outcome; // "Win", "Lose", or "Draw"
    public int playerScore;
    public int dealerScore;

    public GameHistoryEntryV2(BossData boss, float winLossAmount, string outcome, int playerScore, int dealerScore)
    {
        this.bossData = boss;
        this.winLossAmount = winLossAmount;
        this.outcome = outcome;
        this.playerScore = playerScore;
        this.dealerScore = dealerScore;
        this.timestamp = DateTime.Now;
    }

    /// <summary>
    /// Formats the win/loss amount as "Won - X SOL", "Lost - X SOL", or "Draw - 0 SOL"
    /// </summary>
    public string GetWinLossText()
    {
        if (outcome == "Win")
        {
            return $"Won - {Mathf.Abs(winLossAmount):F0} SOL";
        }
        else if (outcome == "Lose")
        {
            return $"Lost - {Mathf.Abs(winLossAmount):F0} SOL";
        }
        else // Draw
        {
            return "Draw - 0 SOL";
        }
    }

    /// <summary>
    /// Formats the timestamp as "MMM dd, h:mm AM/PM" (e.g., "Apr 15, 6:27 AM")
    /// </summary>
    public string GetTimeText()
    {
        return timestamp.ToString("MMM dd, h:mm tt");
    }

    /// <summary>
    /// Gets the boss name, or "Unknown Boss" if bossData is null
    /// </summary>
    public string GetBossName()
    {
        return bossData != null ? bossData.bossName : "Unknown Boss";
    }

    /// <summary>
    /// Gets the boss portrait sprite, or null if bossData is null
    /// </summary>
    public Sprite GetBossPortrait()
    {
        return bossData != null ? bossData.bossPortrait : null;
    }
}

