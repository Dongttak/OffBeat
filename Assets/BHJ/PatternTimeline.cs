using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FixedStep
{
    [Tooltip("시작 후 N초에 소환")] public float time;
    [Tooltip("레인: 0 또는 2만 사용")] [Range(0,2)] public int lane;
    [Tooltip("소환 프리팹")] public GameObject prefab;
    public Vector3 localOffset;
}

public class PatternTimeline : MonoBehaviour
{
    [Header("Timeline")]
    public List<FixedStep> steps = new List<FixedStep>();
    public float totalDuration = 90f;

    [Header("Lane")]
    public Transform[] laneAnchors = new Transform[3];

    [Header("Start")]
    public bool autoStart = true;
    float _startTime;

    void Start()
    {
        steps.Sort((a, b) => a.time.CompareTo(b.time));
        if (autoStart) Begin();
    }

    public void Begin()
    {
        _startTime = Time.time;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        foreach (var s in steps)
        {
            // 레인 안전검사(0/2만 허용)
            if (s.lane != 0 && s.lane != 2)
            {
                Debug.LogWarning($"[PatternTimeline] lane {s.lane}는 허용 안됨(0/2만). 건너뜀."); 
                continue;
            }
            yield return new WaitUntil(() => Time.time >= _startTime + s.time);
            Spawn(s);
        }
    }

    void Spawn(FixedStep s)
    {
        if (!s.prefab) return;
        var anchor = laneAnchors[s.lane];
        if (!anchor)
        {
            Debug.LogWarning("[PatternTimeline] lane anchor 없음."); return;
        }
        var go = Instantiate(s.prefab, anchor.position + s.localOffset, Quaternion.identity);
        var b = go.GetComponent<Pattern2>();
        if (b) b.laneIndex = s.lane;
        else Debug.LogWarning("[PatternTimeline] 프리팹에 BreakableSummon 필요.");
    }
}
