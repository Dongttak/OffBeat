using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public Image scoreBackgroundImage;
    // 결과 창에 표시할 스코어 텍스트 추가

    public int maxScoreCount = 100;
    private int currentScoreCount;

    public int maxCounterScore = 100;
    private int currentCounterScore;

    private void Start()
    {
        currentScoreCount = maxScoreCount;
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
        float scoreValue = currentScoreCount / maxScoreCount;
        string foratted00 = scoreValue.ToString("0.00");
        return foratted00;
    }

    public string GetCounterScoreToString()
    {
        return "";
    }
}
