using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Deck deckController;
    
    [Header("Blind System UI")]
    public Text blindText;
    public Text roundText;
    public Text goalText;
    public Button nextRoundButton; // This should reference the Play Again button in the inspector
    
    private void Start()
    {
        if (deckController == null)
        {
            deckController = FindObjectOfType<Deck>();
            if (deckController == null)
            {
                Debug.LogError("No Deck controller found in scene!");
                return;
            }
        }
        
        // Set up text references in Deck script
        if (blindText != null)
            deckController.blindText = blindText;
        else
            Debug.LogWarning("Blind Text not assigned in UI Manager!");
            
        if (roundText != null)
            deckController.roundText = roundText;
        else
            Debug.LogWarning("Round Text not assigned in UI Manager!");
            
        if (goalText != null)
            deckController.goalText = goalText;
        else
            Debug.LogWarning("Goal Text not assigned in UI Manager!");
            
        // Make sure the Play Again button is properly named
        if (nextRoundButton != null)
        {
            Text buttonText = nextRoundButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Next Round";
            }
            
            // Connect Play Again button if not already connected
            if (deckController.playAgainButton == null)
            {
                deckController.playAgainButton = nextRoundButton;
            }
        }
        
        // Force an update of UI elements
        deckController.UpdateRoundDisplay();
        deckController.UpdateBlindDisplay();
        deckController.UpdateGoalProgress();
    }
} 