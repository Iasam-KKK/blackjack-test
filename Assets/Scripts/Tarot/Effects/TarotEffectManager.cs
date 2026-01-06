using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Central manager for all tarot card effects.
/// Registers effects and dispatches execution based on card type.
/// Singleton pattern for easy access throughout the codebase.
/// </summary>
public class TarotEffectManager : MonoBehaviour
{
    #region Singleton
    
    private static TarotEffectManager _instance;
    public static TarotEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TarotEffectManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TarotEffectManager");
                    _instance = go.AddComponent<TarotEffectManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    #endregion
    
    #region Fields
    
    // Dictionary mapping card types to their effect implementations
    private Dictionary<TarotCardType, ITarotEffect> _effects = new Dictionary<TarotCardType, ITarotEffect>();
    
    // Reference to the deck (cached for creating contexts)
    private Deck _deck;
    
    // Flag to track if effects have been registered
    private bool _initialized = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        RegisterAllEffects();
    }
    
    private void Start()
    {
        // Find deck reference
        _deck = FindObjectOfType<Deck>();
        if (_deck == null)
        {
            Debug.LogWarning("[TarotEffectManager] Deck not found in scene. Will search again when needed.");
        }
    }
    
    #endregion
    
    #region Effect Registration
    
    /// <summary>
    /// Register all tarot effects. Called once at initialization.
    /// </summary>
    private void RegisterAllEffects()
    {
        if (_initialized) return;
        
        Debug.Log("[TarotEffectManager] Registering all tarot effects...");
        
        // ===== PASSIVE EFFECTS (Suit Bonuses) =====
        RegisterEffect(new BotanistEffect());
        RegisterEffect(new AssassinEffect());
        RegisterEffect(new SecretLoverEffect());
        RegisterEffect(new JewelerEffect());
        RegisterEffect(new HouseKeeperEffect());
        RegisterEffect(new WitchDoctorEffect());
        RegisterEffect(new ArtificerEffect());
        
        // ===== PREVIEW EFFECTS =====
        RegisterEffect(new SpyEffect());
        RegisterEffect(new BlindSeerEffect());
        RegisterEffect(new CorruptJudgeEffect());
        RegisterEffect(new HitmanEffect());
        RegisterEffect(new FortuneTellerEffect());
        RegisterEffect(new MadWriterEffect());
        
        // ===== ACTIVE EFFECTS =====
        RegisterEffect(new EscapistEffect());
        RegisterEffect(new CursedHourglassEffect());
        RegisterEffect(new WhisperOfThePastEffect());
        RegisterEffect(new SaboteurEffect());
        RegisterEffect(new ScammerEffect());
        RegisterEffect(new MakeupArtistEffect());
        
        _initialized = true;
        Debug.Log($"[TarotEffectManager] Registered {_effects.Count} tarot effects");
    }
    
    /// <summary>
    /// Register a single effect
    /// </summary>
    public void RegisterEffect(ITarotEffect effect)
    {
        if (effect == null)
        {
            Debug.LogError("[TarotEffectManager] Cannot register null effect");
            return;
        }
        
        if (_effects.ContainsKey(effect.EffectType))
        {
            Debug.LogWarning($"[TarotEffectManager] Effect for {effect.EffectType} already registered, replacing...");
        }
        
        _effects[effect.EffectType] = effect;
        Debug.Log($"[TarotEffectManager] Registered effect: {effect.EffectType}");
    }
    
    /// <summary>
    /// Get an effect by type
    /// </summary>
    public ITarotEffect GetEffect(TarotCardType cardType)
    {
        if (_effects.TryGetValue(cardType, out ITarotEffect effect))
        {
            return effect;
        }
        return null;
    }
    
    /// <summary>
    /// Check if an effect is registered for the given card type
    /// </summary>
    public bool HasEffect(TarotCardType cardType)
    {
        return _effects.ContainsKey(cardType);
    }
    
    #endregion
    
    #region Effect Execution
    
    /// <summary>
    /// Execute a tarot effect by card type
    /// </summary>
    /// <param name="cardType">The type of tarot card</param>
    /// <param name="cardData">Optional card data for context</param>
    /// <returns>True if the effect was successfully executed</returns>
    public bool ExecuteEffect(TarotCardType cardType, TarotCardData cardData = null)
    {
        EnsureDeckReference();
        
        if (!_effects.TryGetValue(cardType, out ITarotEffect effect))
        {
            Debug.LogWarning($"[TarotEffectManager] No effect registered for card type: {cardType}");
            return false;
        }
        
        TarotEffectContext context = CreateContext(cardData);
        
        if (!effect.CanExecute(context))
        {
            string reason = effect.GetCannotExecuteReason(context);
            Debug.Log($"[TarotEffectManager] Cannot execute {cardType}: {reason}");
            return false;
        }
        
        // If effect requires coroutine, start it
        if (effect.RequiresCoroutine)
        {
            StartCoroutine(ExecuteEffectCoroutine(effect, context));
            return true;
        }
        
        // Otherwise execute synchronously
        return effect.Execute(context);
    }
    
    /// <summary>
    /// Execute a tarot effect as a coroutine
    /// </summary>
    public IEnumerator ExecuteEffectAsync(TarotCardType cardType, TarotCardData cardData = null)
    {
        EnsureDeckReference();
        
        if (!_effects.TryGetValue(cardType, out ITarotEffect effect))
        {
            Debug.LogWarning($"[TarotEffectManager] No effect registered for card type: {cardType}");
            yield break;
        }
        
        TarotEffectContext context = CreateContext(cardData);
        
        if (!effect.CanExecute(context))
        {
            string reason = effect.GetCannotExecuteReason(context);
            Debug.Log($"[TarotEffectManager] Cannot execute {cardType}: {reason}");
            yield break;
        }
        
        yield return effect.ExecuteCoroutine(context);
    }
    
    /// <summary>
    /// Check if an effect can be executed
    /// </summary>
    public bool CanExecuteEffect(TarotCardType cardType, TarotCardData cardData = null)
    {
        EnsureDeckReference();
        
        if (!_effects.TryGetValue(cardType, out ITarotEffect effect))
        {
            return false;
        }
        
        TarotEffectContext context = CreateContext(cardData);
        return effect.CanExecute(context);
    }
    
    /// <summary>
    /// Get the reason why an effect cannot be executed
    /// </summary>
    public string GetCannotExecuteReason(TarotCardType cardType, TarotCardData cardData = null)
    {
        EnsureDeckReference();
        
        if (!_effects.TryGetValue(cardType, out ITarotEffect effect))
        {
            return $"No effect registered for {cardType}";
        }
        
        TarotEffectContext context = CreateContext(cardData);
        return effect.GetCannotExecuteReason(context);
    }
    
    /// <summary>
    /// Internal coroutine wrapper for effect execution
    /// </summary>
    private IEnumerator ExecuteEffectCoroutine(ITarotEffect effect, TarotEffectContext context)
    {
        yield return effect.ExecuteCoroutine(context);
    }
    
    #endregion
    
    #region Passive Bonus Calculations
    
    /// <summary>
    /// Calculate total passive bonuses from all active passive effects
    /// </summary>
    public float CalculatePassiveBonuses(GameObject handOwner = null)
    {
        EnsureDeckReference();
        
        TarotEffectContext context = CreateContext(null);
        float totalBonus = 0f;
        
        // Get bonuses from each passive effect type
        totalBonus += CalculateSuitBonus(TarotCardType.Botanist, CardSuit.Clubs, context);
        totalBonus += CalculateSuitBonus(TarotCardType.Assassin, CardSuit.Spades, context);
        totalBonus += CalculateSuitBonus(TarotCardType.SecretLover, CardSuit.Hearts, context);
        totalBonus += CalculateSuitBonus(TarotCardType.Jeweler, CardSuit.Diamonds, context);
        totalBonus += CalculateHouseKeeperBonus(context);
        
        return totalBonus;
    }
    
    /// <summary>
    /// Calculate bonus for a specific suit
    /// </summary>
    private float CalculateSuitBonus(TarotCardType cardType, CardSuit suit, TarotEffectContext context)
    {
        // Check if player has and activated this card
        if (_deck == null) return 0f;
        
        if (!_deck.PlayerActuallyHasCard(cardType) || !_deck.PlayerHasActivatedCard(cardType))
            return 0f;
        
        // Count cards of this suit in player's hand
        List<CardInfo> handCards = _deck.GetHandCardInfo(_deck.player);
        int suitCount = 0;
        
        foreach (CardInfo card in handCards)
        {
            if (card.suit == suit)
            {
                suitCount++;
            }
        }
        
        return suitCount * Constants.SuitBonusAmount;
    }
    
    /// <summary>
    /// Calculate House Keeper bonus (face cards)
    /// </summary>
    private float CalculateHouseKeeperBonus(TarotEffectContext context)
    {
        if (_deck == null) return 0f;
        
        if (!_deck.PlayerActuallyHasCard(TarotCardType.HouseKeeper) || 
            !_deck.PlayerHasActivatedCard(TarotCardType.HouseKeeper))
            return 0f;
        
        // Count face cards (J, Q, K) in player's hand
        List<CardInfo> handCards = _deck.GetHandCardInfo(_deck.player);
        int faceCardCount = 0;
        
        foreach (CardInfo card in handCards)
        {
            // Jack=10, Queen=11, King=12 in suitIndex
            if (card.suitIndex == 10 || card.suitIndex == 11 || card.suitIndex == 12)
            {
                faceCardCount++;
            }
        }
        
        return faceCardCount * Constants.HouseKeeperBonusAmount;
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Ensure we have a valid deck reference
    /// </summary>
    private void EnsureDeckReference()
    {
        if (_deck == null)
        {
            _deck = FindObjectOfType<Deck>();
        }
    }
    
    /// <summary>
    /// Create an effect context
    /// </summary>
    private TarotEffectContext CreateContext(TarotCardData cardData)
    {
        TarotEffectContext context = TarotEffectContext.FromDeck(_deck);
        if (context != null && cardData != null)
        {
            context.cardData = cardData;
        }
        return context;
    }
    
    /// <summary>
    /// Set the deck reference (called by Deck.Start)
    /// </summary>
    public void SetDeck(Deck deck)
    {
        _deck = deck;
    }
    
    #endregion
}

