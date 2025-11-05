using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStepData", menuName = "Tutorial/Tutorial Step Data")]
public class TutorialStepData : ScriptableObject
{
    [Header("Step Content")]
    public string stepTitle = "Tutorial Step";
    
    [TextArea(3, 6)]
    public string stepDescription = "Description of what the player should learn in this step.";
    
    [Header("Step Configuration")]
    public bool waitForUserInput = true;
    public float autoAdvanceDelay = 3f;
    public TutorialTrigger triggerType = TutorialTrigger.Manual;
    public string triggerData = "";
    
    [Header("Highlighting")]
    public string highlightTargetName = ""; // Name of the GameObject to highlight
    public Vector2 panelOffset = Vector2.zero;
    
    [Header("Step Conditions")]
    public bool requiresGameState = false;
    public string requiredGameState = ""; // e.g., "BettingPhase", "GamePlaying", etc.
} 