using System;
using UnityEngine;

public enum CardType2 { Start, Path, End, Peek, CoffeeBreak, Warrant, Bureaucracy, Censor, Uncensor, Tamper, Protocol }
public class Card : MonoBehaviour
{
    public string cardName;
    public CardType2 type;
    public Sprite cardImage;

    // Add this for end cards
    public bool isCorrect;

    public void CopyTo(Card other)
    {
        other.cardName = cardName;
        other.cardImage = cardImage;
        other.isCorrect = isCorrect;
        other.type = type;
    }

    public void OnEnable()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = cardImage;
    }
}
