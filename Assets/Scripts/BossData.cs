using UnityEngine;
using System.Collections.Generic;

// Define boss types based on your list
public enum BossType
{
    TheDrunkard,        // An absolute average playing experience. Random use of regular cards. No tarot.
    TheFortuneTeller,   // Tarot cards introduction. Regular experience, with the use of basic Tarot cards.
    TheThief,           // Is able to steal cards from the player's hand, randomly acts upon it.
    TheForgetfulSeer,   // Can see the player's next 2 cards in hand, randomly acts upon it.
    TheHunter,          // Can target and destroy cards already laid out. If not metal, these are permanently destroyed.
    TheChiromancer,     // When it wins, it takes 1.5 of your bet, if 21, x2.
    TheCaptain,         // Its deck is comprised of J's and the whole set of spades, can nullify enemy Js.
    TheTraitor,         // Steals laid out cards, uses them, if it wins, they are permanently destroyed.
    TheDiplomat,        // When playing a K, it adds as many points as needed to win, unless effects nullify or alter it.
    TheMagician,        // Can return cards to its or the player's hand, if it does, seer skills are neutralized.
    TheSeductress,      // Ks and J's played go to the Seductress hand in first place, they add 10 or 1 at convenience.
    TheDegenerate,      // Permanently steals all Qs played. 
    TheCollector,       // Steals and never returns metal or gem cards.
    TheMadman,          // Rules are randomly picked as the game begins, based on previous profiles.
    TheInsatiable,      // If it wins, it takes the bet, also permanently takes as many cards used to win & ½ tarot.
    TheCorruptor,       // When a card is laid down, it can randomly inflate or deflate its value.
    TheLiar,            // Plays cards face down, flips the one before the last ones played. Can swap with next card.
    TheAlchemist,       // Can mutate its cards or the players, for benefit or damage respectively.
    TheSorcerer,        // Tarot cards don't work.
    TheNaughtyChild,    // Hides all consumables, steals cards (regular and tarot) temporarily.
    TheEmpress,         // Its own and player's Ks and Js add to its convenience, only Ks and Js are affected, Qs are immune.
    TheGypsy,           // Final Boss - All the previous and your multiplier applies to him.
    ThePyro             // As it wins rounds it burns all materials up to gold gradually (win streak).
}

// Define boss mechanics that can be applied
public enum BossMechanicType
{
    None,
    StealCards,           // Steal cards from player's hand
    DestroyCards,         // Destroy cards on the table
    ModifyBet,            // Modify bet amounts (like Chiromancer)
    ChiromancerBetting,   // Special betting for The Chiromancer (1.5x normal, 2x on 21)
    SpecialDeck,          // Use special deck composition
    CardValueManipulation, // Change card values
    FaceDownCards,        // Play cards face down
    DisableTarot,         // Disable tarot card usage
    TemporaryTheft,       // Temporarily steal cards/items
    MultiplierEffect,     // Apply special multiplier effects
    WinStreakEffect,      // Effects based on win streaks
    PeekNextCards,        // Peek at next cards and act upon them (Forgetful Seer)
    JackNullification,    // Nullify Jacks in player's hand (The Captain)
    StealLaidOutCards,    // Steal cards already laid out and use them (The Traitor)
    PermanentDestruction, // Permanently destroy cards if boss wins (The Traitor)
    DiplomaticKing,       // King adds exactly the points needed to win (The Diplomat)
    SeductressIntercept,  // Intercept Kings and Jacks, value them optimally (The Seductress)
    CorruptCard ,          // Randomly inflate or deflate card values when laid down (The Corruptor)
    PermanentlystealsallQueensplayed,, // Permanently steals all Queens played
    HideConsumables,       // Hide/disable all tarot cards (The Naughty Child)
    TemporaryCardTheft,    // Temporarily steal cards and return after round (The Naughty Child)
    EmpressIntercept       // Intercept Kings and Jacks for The Empress
    MutateCards  // The Alchemist - Mutates cards for benefit (boss) or damage (player)


}

[System.Serializable]
public class BossMechanic
{
    [Header("Mechanic Settings")]
    public BossMechanicType mechanicType;
    public string mechanicName;
    [TextArea(2, 4)]
    public string mechanicDescription;
    
    [Header("Activation")]
    public float activationChance = 1f; // 0-1, chance this mechanic activates
    public bool isPassive = false; // If true, mechanic is always active
    public bool triggersOnPlayerAction = false; // If true, triggers on player hit/stand
    public bool triggersOnCardDealt = false; // If true, triggers when cards are dealt
    public bool triggersOnRoundEnd = false; // If true, triggers when round ends
    
    
    [Header("Effect Parameters")]
    public int mechanicValue = 0; // Generic value for mechanic effects
    public float mechanicMultiplier = 1f; // Generic multiplier for mechanic effects
    public string[] mechanicTags; // Tags for specific mechanic targeting

   
}

[System.Serializable]
public class BossReward
{
    [Header("Reward Settings")]
    public string rewardName;
    [TextArea(2, 3)]
    public string rewardDescription;
    public Sprite rewardIcon;
    
    [Header("Reward Type")]
    public bool grantsTarotCard = false;
    public TarotCardType tarotCardType;
    public bool grantsPermanentUpgrade = false;
    public string upgradeName;
    public int upgradeValue = 0;
    public bool grantsBonusBalance = false;
    public uint bonusAmount = 0;
}

[CreateAssetMenu(fileName = "NewBoss", menuName = "BlackJack/Boss", order = 2)]
public class BossData : ScriptableObject
{
    [Header("Boss Identity")]
    public string bossName;
    public BossType bossType;
    public Sprite bossPortrait;
    public Sprite bossIntroPanelBg;  // Background image for boss intro panel
    [TextArea(3, 6)]
    public string bossDescription;
    
    [Header("Boss Stats")]
    public int maxHealth = 3; // Number of wins needed to defeat this boss
    public int handsPerRound = 9; // Total hands available in this level (no more rounds)
    public float difficultyMultiplier = 1f; // Affects rewards/penalties
    public int unlockOrder = 0; // Order in which this boss unlocks (0 = first)
    
    [Header("Boss Mechanics")]
    public List<BossMechanic> mechanics = new List<BossMechanic>();
    
    [Header("Boss Rewards")]
    public List<BossReward> rewards = new List<BossReward>();
    
    [Header("Visual & Audio")]
    public Color bossThemeColor = Color.white;
    public AudioClip bossMusic;
    public AudioClip bossDefeatSound;
    public AudioClip bossVictorySound;
    
    [Header("Animation")]
    public float animationDuration = 0.5f;
    public AnimationCurve animationCurve;
    
    [Header("Special Rules")]
    public bool disablesTarotCards = false; // For The Sorcerer
    public bool usesSpecialDeck = false; // For The Captain
    public bool hasWinStreakEffect = false; // For The Pyro
    public bool isFinalBoss = false; // For The Gypsy
    public bool allowTarotCards = true;

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
    
    public bool IsUnlocked(int defeatedBosses)
    {
        return defeatedBosses >= unlockOrder;
    }
    
    public string GetBossTitle()
    {
        return bossName + " - " + bossDescription;
    }
}
