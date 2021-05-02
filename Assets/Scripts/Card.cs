using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Create New Card", order = 0)]
public class Card : ScriptableObject
{
    // Fields
    [SerializeField] private int attackValue;
    [SerializeField] private int defenseValue;
    [SerializeField] private int manaCost;
    [SerializeField] private string description;
    [SerializeField] private Sprite cardSprite;
    [SerializeField] private CardTier cardTier;

    // Properties
    public int AttackValue { get { return attackValue; } }
    public int DefenseValue { get { return defenseValue; } }
    public int ManaCost { get { return manaCost; } }
    public string Description { get { return description; } }
    public Sprite CardSprite { get { return cardSprite; } }
    public CardTier Tier { get { return cardTier; } }
}

public enum CardTier { Tier1, Tier2, Tier3 }