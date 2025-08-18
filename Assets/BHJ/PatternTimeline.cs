using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FixedStep
{
    [Tooltip("시작 후 N번째 스텝에서 소환 (0부터 시작)")]
    public int stepIndex;

    [Tooltip("레인: 0 또는 2만 사용")]
    [Range(0, 2)] public int lane;

    [Tooltip("소환 프리팹")]
    public GameObject prefab;

    [Tooltip("레인 앵커 기준 오프셋")]
    public Vector3 localOffset;
}

public class PatternTimeline : MonoBehaviour
{
    public List<FixedStep> steps;
    public Transform[] laneAnchors;

    private int currentStep = 0;
    public void Begin()
    {
        currentStep = 0;
    }
    void OnEnable()
    {
        BeatManager.OnBeat += HandleOnBeat;
    }
    void OnDisable()
    {
        BeatManager.OnBeat -= HandleOnBeat;
    }

    void HandleOnBeat()
    {
        // 현재 stepIndex 와 일치하는 스텝 있으면 소환
        foreach (var s in steps)
        {
            if (s.stepIndex == currentStep)
            {
                Spawn(s);
            }
        }
        currentStep++;
    }

    void Spawn(FixedStep s)
    {
        if (!s.prefab) return;
        var anchor = laneAnchors[s.lane];
        var go = Instantiate(s.prefab, anchor.position + s.localOffset, Quaternion.identity);
        var b = go.GetComponent<Pattern2>();
        if (b) b.laneIndex = s.lane;
    }
}
