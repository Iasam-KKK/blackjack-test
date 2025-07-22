using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats instance;
    public List<TarotCardData> ownedCards;

    void Awake()
    {
        instance = this;
    }

    public bool PlayerHasCard(TarotCardType type)
    {
        return ownedCards.Any(card => card.cardType == type);
    }
}