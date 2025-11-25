using System;
using System.Collections.Generic;
using UnityEngine;

public class DrawPile : MonoBehaviour
{
    private DiscardPile discardPile;
    public Stack<GameObject> drawPile = new Stack<GameObject>();

    private GameObject testingDeck;
    
    //This is mainly for testing but might be how we end up doing it later aswell
    private void Start()
    {
        testingDeck = GameObject.Find("Test Deck");
        foreach (Transform child in testingDeck.transform)
        {
            child.position = gameObject.transform.position;
            drawPile.Push(child.gameObject);
        }
    }

    //Call this function from elsewhere when you need the topmost card
    //Also ignore how only the draw pile can't be called through the game manager. Hindsight etc.
    public GameObject drawCard()
    {
        if (drawPile.Count == 0)
        {
            drawPile = discardPile.Shuffle();
        }
        return drawPile.Pop();
    }
}
