# Blackjack Tutorial System Setup Guide

This guide will walk you through setting up the tutorial system for your blackjack game. The tutorial system uses PlayerPrefs to track first-time users and provides step-by-step guidance through game mechanics.

## Files Created

1. **TutorialManager.cs** - Main tutorial logic and step management
2. **TutorialStepData.cs** - ScriptableObject for configurable tutorial steps
3. **TutorialHighlightOverlay.cs** - Handles highlighting UI elements during tutorial
4. **MainMenuManager.cs** - Updated with tutorial options

## Setup Instructions

### 1. Create Tutorial UI in Game Scene

In your `Blackjack` scene, create the following UI hierarchy:

```
Canvas
├── TutorialSystem (GameObject)
│   ├── TutorialManager (Add TutorialManager script)
│   ├── TutorialPanel (Panel)
│   │   ├── Background (Image - dark semi-transparent)
│   │   ├── Content (Panel)
│   │   │   ├── TitleText (Text)
│   │   │   ├── DescriptionText (Text)
│   │   │   └── ButtonsPanel (Panel)
│   │   │       ├── NextButton (Button)
│   │   │       ├── SkipButton (Button)
│   │   │       └── CloseButton (Button)
│   │   │
│   │   └── HighlightOverlay (Image - full screen)
│   │       └── TutorialHighlightOverlay (Add TutorialHighlightOverlay script)
```

### 2. Configure TutorialManager

In the TutorialManager component:

1. **Tutorial Panel**: Assign the TutorialPanel GameObject
2. **Title Text**: Assign the TitleText component
3. **Description Text**: Assign the DescriptionText component
4. **Next Button**: Assign the NextButton component
5. **Skip Button**: Assign the SkipButton component
6. **Close Button**: Assign the CloseButton component
7. **Highlight Overlay**: Assign the TutorialHighlightOverlay component

### 3. Configure TutorialHighlightOverlay

In the TutorialHighlightOverlay component:

1. Set the **Image** component to cover the full screen
2. Set **Color** to black with ~70% alpha (0, 0, 0, 0.7)
3. Configure **Highlight Padding** (default: 20)
4. Enable **Animate Highlight** for pulse effect
5. Set **Raycast Target** to true to block interactions

### 4. Update Main Menu (Optional)

If you want tutorial options in the main menu:

1. Add a "Tutorial" button to your main menu
2. Update MainMenuManager references:
   - **Tutorial Button**: Assign the new tutorial button
   - **Tutorial Settings Panel**: Create and assign settings panel
   - **Replay Tutorial Button**: Button to restart tutorial
   - **Tutorial Enabled Toggle**: Toggle for enabling/disabling tutorial for new players

### 5. Player Preferences Keys

The system uses these PlayerPrefs keys:

- `TutorialCompleted` (int): 1 if tutorial was completed, 0 otherwise
- `TutorialEnabledForNewPlayers` (int): 1 if tutorial should show for new players
- `TutorialCurrentStep` (int): Current step index (for future save/resume feature)

## Customization

### Adding New Tutorial Steps

To add new tutorial steps, modify the `InitializeTutorialSteps()` method in TutorialManager:

```csharp
tutorialSteps.Add(new TutorialStep
{
    title = "Your Custom Step",
    description = "Description of what the player should learn.",
    highlightTarget = someUIElement?.gameObject,
    waitForUserInput = true,
    triggerType = TutorialTrigger.Manual
});
```

### Creating Tutorial Step Data Assets

Use the ScriptableObject system for easier tutorial management:

1. Right-click in Project window
2. Create → Tutorial → Tutorial Step Data
3. Configure the step data
4. Reference in TutorialManager

### Customizing Highlight Effects

The TutorialHighlightOverlay supports:

- **Basic overlay**: Dark background with clear highlight area
- **Animated highlights**: Pulsing effect on highlighted elements
- **Custom colors**: Configurable overlay and highlight colors

## Advanced Features

### Conditional Tutorial Steps

Steps can be shown based on game state:

```csharp
public bool requiresGameState = false;
public string requiredGameState = "BettingPhase";
```

### Different Trigger Types

- **Manual**: User clicks "Next" button
- **ButtonClick**: Triggered by clicking specific UI elements
- **GameEvent**: Triggered by game events
- **AutoAdvance**: Automatically advance after delay

### Integration with Game Systems

The tutorial automatically integrates with:

- **Deck Controller**: For game state and UI references
- **Game History Manager**: For history-related tutorials
- **Game Menu Manager**: For pause/resume functionality

## Testing

### Testing First-Time User Experience

1. Clear PlayerPrefs: `PlayerPrefs.DeleteAll()` in console
2. Start game - tutorial should trigger automatically
3. Complete tutorial - it shouldn't show again

### Testing Tutorial Reset

1. Use "Replay Tutorial" button in main menu
2. Or call `TutorialManager.ResetTutorial()` from code

### Debug Options

Enable debug logging in TutorialManager to track:
- Tutorial initialization
- Step progression
- PlayerPrefs values
- Component references

## Troubleshooting

### Tutorial Not Showing

1. Check `enableTutorial` is true in TutorialManager
2. Verify PlayerPrefs: `TutorialCompleted` should be 0
3. Check `TutorialEnabledForNewPlayers` setting
4. Ensure TutorialManager is in the scene and enabled

### Highlighting Not Working

1. Verify TutorialHighlightOverlay component is assigned
2. Check if target GameObjects have RectTransform components
3. Ensure overlay covers the full screen
4. Verify Canvas render mode and camera setup

### UI References Missing

1. Use `FindObjectOfType<>()` calls in TutorialManager.Start()
2. Check that UI elements exist in the scene
3. Verify naming conventions match the code

## Performance Considerations

- Tutorial steps are generated at runtime (no memory overhead when not active)
- Highlight overlay only updates when active
- PlayerPrefs are only read/written at key moments
- Tutorial can be completely disabled via inspector

## Future Enhancements

Potential additions:
- Save/resume tutorial progress mid-session
- Tutorial step validation (ensure required elements exist)
- Localization support for multiple languages
- Tutorial recording/playback system
- Analytics integration for tutorial completion rates

## Integration Checklist

- [ ] TutorialManager added to game scene
- [ ] Tutorial UI hierarchy created
- [ ] Component references assigned
- [ ] MainMenu updated with tutorial options
- [ ] PlayerPrefs keys documented
- [ ] Testing completed for first-time users
- [ ] Testing completed for returning users
- [ ] Tutorial can be skipped
- [ ] Tutorial can be replayed
- [ ] All game UI elements can be highlighted
- [ ] Tutorial doesn't interfere with normal gameplay

The tutorial system is now ready for use! Players will automatically see the tutorial on their first game launch, and can replay it anytime from the main menu. 