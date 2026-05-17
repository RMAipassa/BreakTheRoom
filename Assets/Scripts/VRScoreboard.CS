using TMPro;
using UnityEngine;

public class VRScoreBoard : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float startTime = 120f;

    private int score;
    private float timeRemaining;
    private bool running = true;

    private void Start()
    {
        timeRemaining = startTime;
        UpdateBoard();
    }

    private void Update()
    {
        if (!running) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            running = false;
        }

        UpdateBoard();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateBoard();
    }

    private void UpdateBoard()
    {
        scoreText.text = $"SCORE: {score}";

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timerText.text = $"TIME: {minutes:00}:{seconds:00}";
    }
}