using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public GameObject scoreBackground;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI CounterScoreText;
    public TextMeshProUGUI ClearText;

    public int maxScoreCount = 100;
    [SerializeField] private int currentScoreCount;

    public int maxCounterScore = 20;
    [SerializeField] private int currentCounterScore;

    private void Start()
    {
        currentScoreCount = maxScoreCount;
        currentCounterScore = maxCounterScore;
    }

    public void SubtractCurrentScoreCount()
    {
        currentScoreCount--;
    }

    public void SubtractCurrentCounterScore()
    {
        currentCounterScore--;
    }

    public string GetTotalScoreToString()
    {
        float scoreValue = (float)currentScoreCount / maxScoreCount * 100f;
        string formatted00 = scoreValue.ToString("F2");
        return formatted00;
    }

    public int GetTotalCounterScore()
    {
        return currentCounterScore;
    }

    public string GetCounterScoreToString()
    {
        string formatted = currentCounterScore.ToString();
        return formatted;
    }

    public void Ending()
    {
        BeatManager.Instance.StopMusicOnEnd();
        scoreBackground.SetActive(true);

        scoreText.text = "score : " + GetTotalScoreToString() + "%";
        CounterScoreText.text = "counter : " + GetTotalCounterScore() + "/ " + maxCounterScore;

        if (currentCounterScore >= maxCounterScore / 2)
            ClearText.text = "클리어 성공!";
        else
            ClearText.text = "클리어 실패!";

        Time.timeScale = 0f;
    }
}