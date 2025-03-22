using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class Gamemanager : MonoBehaviour
{
    [SerializeField] Sprite[] diceImages;
    [SerializeField] SpriteRenderer[] inPlay;
    [SerializeField] Button roll;
    private Dictionary<Transform, Vector3> ogPositions = new Dictionary<Transform, Vector3>();
    private Transform diceParent;
    private Transform roundSavedDiceParent;
    public GameObject currentSavedGroup;

    public Text turnScoreText, totalScoreText;
    
    public int turnScore, totalScore, turnNumber;
    public bool hasSaved = true, hasRolled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        diceParent = GameObject.Find("PlayableDice").transform;
        foreach (Transform dice in diceParent)
        {
            ogPositions[dice] = dice.position; // Store original position
        }
        roundSavedDiceParent = GameObject.Find("RoundSDice").transform;

        CreateNewSavedGroup();
        //throwDice();
    }

    public void CreateNewSavedGroup()
    {
        string groupName = "SavedDice Turn" + turnNumber;

        Transform existingGroup = GameObject.Find(groupName)?.transform;

        if (existingGroup == null)
        {
            // Only create a new group if it doesn't exist
            GameObject newGroup = new GameObject(groupName);
            newGroup.transform.SetParent(GameObject.Find("RoundSDice").transform);
            currentSavedGroup = newGroup;
        }
        else
        {
            // Use the existing group instead of replacing it
            currentSavedGroup = existingGroup.gameObject;
        }
    }


    public void throwDice()
    {
        if (!hasSaved && hasRolled)
        {
            Debug.Log("You must save at least one die before rolling!");
            return;
        }

        hasRolled = true;

        // Move previous round's saved dice to the left and make them smaller
        float startX = -7; // Left side
        float startY = 3;  // Start stacking down from here
        float yOffset = Random.Range(0.1f,1.5f); // Space between dice

        int savedDiceCount = 0; // Track how many dice have been moved

        foreach (Transform group in GameObject.Find("RoundSDice").transform)
        {
            if (group != currentSavedGroup) // Ignore the current round's saved dice
            {
                foreach (Transform dice in group)
                {
                    dice.localScale = new Vector3(0.1f, 0.1f, 1); // Shrink size
                    dice.position = new Vector3(startX, startY + savedDiceCount * yOffset, dice.position.z);
                    savedDiceCount++;
                }
            }
        }


        CreateNewSavedGroup();
        Transform playableDiceParent = GameObject.Find("PlayableDice")?.transform;

        if (playableDiceParent == null)
        {
            Debug.LogError("PlayableDice object not found");
            return;
        }

        Debug.Log("Rolling dice in PlayableDice...");

        List<Dice> diceObjects = new List<Dice>();

        foreach (Transform dice in playableDiceParent)
        {
            yOffset = (Random.Range(-5, 15) * 0.1f % 1.5f) + 1;
            dice.position = new Vector3(dice.position.x, yOffset, dice.position.z);

            int randDie = Random.Range(0, diceImages.Length);
            dice.GetComponent<SpriteRenderer>().sprite = diceImages[randDie];

            Debug.Log("Rolled: " + dice.name + " → Face: " + randDie);

            Dice diceComponent = dice.GetComponent<Dice>();
            diceObjects.Add(diceComponent);
        }

        Dice[] diceArray = diceObjects.ToArray();

        if (CalculateDiceScore(diceArray) > 0)
        {
            Debug.Log("Scoring dice found!");
        }
        else
        {
            Debug.Log("Farkle! No scoring dice.");
            turnScore = 0;
            Bank();
            UpdateScoreBoard();
        }

        hasSaved = false; // Reset so a die must be saved before rolling again        
    }


    public void Bank()
    {
        totalScore += turnScore;
        turnScore = 0;


        List<Transform> diceToMove = new List<Transform>();

        // Collect all dice first
        foreach (Transform group in roundSavedDiceParent)
        {
            foreach (Transform dice in group)
            {
                diceToMove.Add(dice);
            }
        }

        // Move all dice back to PlayableDice
        foreach (Transform dice in diceToMove)
        {
            dice.SetParent(diceParent, true);
            Dice diceScript = dice.GetComponent<Dice>();
            diceScript.diceStatus = Dice.status.inPlay;
            if (ogPositions.ContainsKey(dice))
            {
                dice.localScale = new Vector3(0.3f, 0.3f, 1);
                dice.position = ogPositions[dice];
            }
            
        }

        Transform RoundSaved = GameObject.Find("RoundSDice").transform;

        foreach (Transform turn in RoundSaved)
        {
            Destroy(turn.gameObject);
        }

        //CreateNewSavedGroup();
        UpdateScoreBoard();
        throwDice();
    }

    public void UpdateScoreBoard()
    {
        turnScoreText.text = "Turn Score: " + turnScore;
        totalScoreText.text = "Total Score: " + totalScore;
    }

    public void UpdateTurnScore()
    {
        List<Dice> savedDice = new List<Dice>();

        Transform RoundSaved = GameObject.Find("RoundSDice").transform.Find("SavedDice Turn" + turnNumber);

        if(RoundSaved != null)
        {
            foreach(Transform dice in RoundSaved)
            {
                Dice diceComponent = dice.GetComponent<Dice>();
                if(diceComponent != null)
                {
                    savedDice.Add(diceComponent);
                }
            }
        }
        else
        {
            Debug.LogWarning("No saved dice found for this turn");
        }

        turnScore = CalculateDiceScore(savedDice.ToArray());

        UpdateScoreBoard();
        Debug.Log("Turn Score Updated: " + turnScore);
    }

    public int CalculateDiceScore(Dice[] diceArray)
    {
        Dictionary<int, int> diceCounts = new Dictionary<int, int>();
        foreach (Dice dice in diceArray)
        {
            if (dice.diceStatus == Dice.status.inPlay)
                continue; // Ignore dice that are still in play

            int value = dice.GetDiceValue();
            if (diceCounts.ContainsKey(value))
                diceCounts[value]++;
            else
                diceCounts[value] = 1;
        }

        int score = 0;

        foreach (var pair in diceCounts)
        {
            int diceValue = pair.Key;
            int count = pair.Value;

            if (diceValue == 1)
            {
                if (count >= 3)
                    score += 1000 + (count - 3) * 100;
                else
                    score += count * 100;
            }
            else if (diceValue == 5)
            {
                if (count >= 3)
                    score += 500 + (count - 3) * 50;
                else
                    score += count * 50;
            }
            else if (count >= 3)
            {
                score += diceValue * 100;
            }
        }

        Debug.Log("Score Calculated: " + score);
        return score;
    }

}
