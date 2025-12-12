using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHistorySlotUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI bossNameText;
    public Image bossImage;
    public TextMeshProUGUI winLossAmountText;
    public TextMeshProUGUI timeText;

    /// <summary>
    /// Populates all UI elements from the history entry data
    /// </summary>
    public void SetupEntry(GameHistoryEntryV2 entry)
    {
        // Set boss name
        if (bossNameText != null)
        {
            bossNameText.text = entry.GetBossName();
        }

        // Set boss image
        if (bossImage != null)
        {
            Sprite portrait = entry.GetBossPortrait();
            if (portrait != null)
            {
                bossImage.sprite = portrait;
                bossImage.enabled = true;
            }
            else
            {
                bossImage.enabled = false;
            }
        }

        // Set win/loss amount with color coding
        if (winLossAmountText != null)
        {
            winLossAmountText.text = entry.GetWinLossText();
            
            // Color code based on outcome
            switch (entry.outcome.ToLower())
            {
                case "win":
                    winLossAmountText.color = Color.green;
                    break;
                case "lose":
                    winLossAmountText.color = Color.red;
                    break;
                case "draw":
                    winLossAmountText.color = Color.yellow;
                    break;
                default:
                    winLossAmountText.color = Color.white;
                    break;
            }
        }

        // Set timestamp
        if (timeText != null)
        {
            timeText.text = entry.GetTimeText();
        }
    }
}

