using System.Collections.Generic;
using UnityEngine;

public class DiscardPile : MonoBehaviour
{
    public List<GameObject> discardPile = new List<GameObject>();
    public Stack<GameObject> Shuffle()
    {
        Stack<GameObject> stack = new Stack<GameObject>();

        int c = discardPile.Count;
        while (c > 1)
        {
            c--;
            int index = Random.Range(0, c);
            stack.Push(discardPile[index]);
            discardPile.RemoveAt(index);
        }
        
        return stack;
    }
}
