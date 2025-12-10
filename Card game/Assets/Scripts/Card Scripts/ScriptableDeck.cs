using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DeckDefinition", menuName = "Cards/DeckDefinition")]
public class DeckDefinition : ScriptableObject {
    public List<DirectionalCardData> cards;
    public List<int> quantities; // Same index as cards list
}