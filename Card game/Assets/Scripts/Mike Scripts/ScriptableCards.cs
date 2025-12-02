using UnityEngine;

[CreateAssetMenu(fileName = "NewDirectionalCard", menuName = "Cards/DirectionalCard")]
public class DirectionalCardData : ScriptableObject {
    public string cardName;
    public Sprite cardImage;
    public Sprite cardBackside;
    public bool isStart;
    public bool isEnd;
    public bool isCorrect;
    public bool specialCard;
    
    [Header("Path Connections")]
    public bool connectsUp;
    public bool connectsDown;
    public bool connectsLeft;
    public bool connectsRight;
}