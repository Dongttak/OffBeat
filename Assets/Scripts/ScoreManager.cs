using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public Image scoreBackgroundImage;
    // 결과 창에 표시할 스코어 텍스트 추가

    public int maxCount = 100;
    public int curCount;

    private void Start()
    {
        curCount = maxCount;
    }

    public void SubtractScore()
    {
        curCount--;
    }

    public string GetScoreToString()
    {
        float scoreValue = curCount / maxCount;
        string foratted00 = scoreValue.ToString("0.00");
        return foratted00;
    }
}
