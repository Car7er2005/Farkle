using System.Collections;
using UnityEngine;

public class Dice : MonoBehaviour
{
    public bool isSaved = false;  // Track if the dice is saved
    public bool permanentlySaved = false;
    
    private Transform RoundSDiceParent;
    private Transform playableDiceParent;

    void Start()
    {
        
        RoundSDiceParent = GameObject.Find("RoundSDice").transform;
        playableDiceParent = GameObject.Find("PlayableDice").transform;
    }

    IEnumerator ReparentAfterFrame(Transform newParent)
    {
        yield return null; // Wait one frame
        transform.SetParent(newParent);
    }

    void OnMouseDown()
    {
        Gamemanager gameManager = GameObject.Find("GameManager").GetComponent<Gamemanager>();

        if (!gameManager.hasRolled) // Prevent saving before rolling at game start
        {
            Debug.Log("You must roll first before saving any dice!");
            return;
        }

        if (permanentlySaved)
        {
            Debug.Log(gameObject.name + " is permanently saved and cannot be moved!");
            return;
        }

        if (isSaved) // If the die is already saved, check if it can be removed
        {
            if (transform.parent == gameManager.currentSavedGroup.transform) // Only unsave if it's from this turn
            {
                transform.SetParent(playableDiceParent, true);
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
                isSaved = false;

                // Deduct score when die is removed
                gameManager.turnScore -= gameManager.CalculateDiceScore(new int[] { GetDiceValue() });
                gameManager.UpdateScoreBoard();
                Debug.Log(gameObject.name + " moved back to PlayableDice");

                // Check if any dice are still saved, if none, disable rolling
                bool anySaved = false;
                foreach (Transform group in GameObject.Find("RoundSDice").transform)
                {
                    if (group.childCount > 0)
                    {
                        anySaved = true;
                        break;
                    }
                }
                gameManager.hasSaved = anySaved;
                gameManager.addDice = false;
                gameManager.UpdateTurnScore();
            }
        }
        else // If the die is not saved, move it to the current round's saved dice group
        {
            transform.SetParent(gameManager.currentSavedGroup.transform, true);
            transform.position = new Vector3(transform.position.x, -4f, transform.position.z);
            isSaved = true;

            Debug.Log(gameObject.name + " moved to RoundSDice");

            gameManager.hasSaved = true; // A die has been saved, allow rolling again
            gameManager.addDice = true;
            gameManager.UpdateTurnScore();
        }
    }


    public int GetDiceValue()
    {
        string diceName = GetComponent<SpriteRenderer>().sprite.name;
        int diceNumber = int.Parse(diceName.Split('_')[1])+1;
        return diceNumber;
    }
}
