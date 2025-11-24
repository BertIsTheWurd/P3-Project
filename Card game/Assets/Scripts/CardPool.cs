using System;
using System.Collections.Generic;
using UnityEngine;

public class CardPool : MonoBehaviour
{
    public int poolSize;
    public GameObject cardPrefab;
    public List<GameObject> cards;
    
    private void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject temp = Instantiate(cardPrefab, transform);
            temp.SetActive(false);
            cards.Add(temp);
        }
    }

    public GameObject ReturnCard()
    {
        for (int i = 0; i < poolSize; i++)
        {
            if (!cards[i].activeInHierarchy)
            {
                return cards[i];
            }
        }
        
        throw new Exception("No Card Available");
    }
}
