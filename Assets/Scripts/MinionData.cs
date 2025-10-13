using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Minion data - simplified boss mechanics for pre-boss encounters
/// Player must defeat 2 out of 3 minions to unlock the boss battle
/// </summary>
[CreateAssetMenu(fileName = "NewMinion", menuName = "BlackJack/Minion", order = 3)]
public class MinionData : ScriptableObject
{
    [Header("Minion Identity")]
    public string minionName;
    public Sprite minionPortrait;
    [TextArea(2, 4)]
    public string minionDescription;
    
    [Header("Minion Stats")]
    public int maxHealth = 1; // Number of wins needed to defeat this minion (usually 1-2)
    public int handsPerRound = 5; // Hands available for this minion battle (less than boss)
    public float difficultyMultiplier = 0.5f; // Easier than bosses
    
    [Header("Minion Mechanics")]
    public List<BossMechanic> mechanics = new List<BossMechanic>();
    
    [Header("Visual & Audio")]
    public Color minionThemeColor = Color.white;
    public AudioClip minionMusic;
    public AudioClip minionDefeatSound;
    
    [Header("Special Rules")]
    public bool disablesTarotCards = false;
    public bool usesSpecialDeck = false;
    
    // Helper methods
    public bool HasMechanic(BossMechanicType mechanicType)
    {
        return mechanics.Exists(m => m.mechanicType == mechanicType);
    }
    
    public BossMechanic GetMechanic(BossMechanicType mechanicType)
    {
        return mechanics.Find(m => m.mechanicType == mechanicType);
    }
    
    public List<BossMechanic> GetActiveMechanics()
    {
        return mechanics.FindAll(m => m.mechanicType != BossMechanicType.None);
    }
}

