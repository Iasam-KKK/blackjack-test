using System;

[System.Serializable]
public class GameHistoryEntry
{
    public int roundNumber;
    public string blindLevel;
    public int playerScore;
    public int dealerScore;
    public uint betAmount;
    public uint balanceBefore;
    public uint balanceAfter;
    public string outcome;
    public DateTime timestamp;

    public GameHistoryEntry(int round, string blind, int playerScore, int dealerScore, 
                           uint bet, uint balanceBefore, uint balanceAfter, string outcome)
    {
        this.roundNumber = round;
        this.blindLevel = blind;
        this.playerScore = playerScore;
        this.dealerScore = dealerScore;
        this.betAmount = bet;
        this.balanceBefore = balanceBefore;
        this.balanceAfter = balanceAfter;
        this.outcome = outcome;
        this.timestamp = DateTime.Now;
    }

    public string GetBalanceChangeText()
    {
        long change = (long)balanceAfter - (long)balanceBefore;
        if (change > 0)
        {
            return balanceAfter + "(+" + change + ")";
        }
        else if (change < 0)
        {
            return balanceAfter + "(" + change + ")";
        }
        else
        {
            return balanceAfter + "(+0)";
        }
    }

    public string GetRoundText()
    {
        return "R" + roundNumber;
    }

    public string GetBlindText()
    {
        // Convert full blind names to short versions
        switch (blindLevel.ToLower())
        {
            case "small blind":
                return "Small";
            case "big blind":
                return "Big";
            case "mega blind":
                return "Mega";
            case "super blind":
                return "Super";
            default:
                return blindLevel;
        }
    }

    public string GetPlayerScoreText()
    {
        return "P:" + playerScore;
    }

    public string GetDealerScoreText()
    {
        return "D:" + dealerScore;
    }

    public string GetBetText()
    {
        return "$" + betAmount;
    }
} 