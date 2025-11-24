using UnityEngine;

public class TestScript : MonoBehaviour
{
    public KeyCode drawToMax = KeyCode.D;
    public KeyCode playCard= KeyCode.P;
    private GameManager gameManager;
    private DrawPile drawPile;
    public GameObject testCard;
    public int nextSpotX, nextSpotZ;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager =  GameObject.Find("Game Manager").GetComponent<GameManager>();
        drawPile = GameObject.Find("Draw Pile").GetComponent<DrawPile>();
    }

    // Update is called once per frame
    void Update()
    {
        //This is abhorrent, do not do this for actual scripts
        if (Input.GetKeyDown(drawToMax))
        {
            gameManager.DrawToMax();
        }

        if (Input.GetKeyDown(playCard))
        {
            gameManager.PlayCard(testCard.GetComponent<Card>(), nextSpotX, nextSpotZ);

                nextSpotX++;
                if (nextSpotX >= gameManager.gridX)
                {
                    nextSpotX = 0;
                    nextSpotZ++;
                }
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            gameManager.DebugCubes();
        }

        //Move top card from Drawpile to Discardpile
        if (Input.GetKeyDown(KeyCode.E))
        {
            gameManager.DiscardCard(drawPile.drawCard());
        }
    }
}
