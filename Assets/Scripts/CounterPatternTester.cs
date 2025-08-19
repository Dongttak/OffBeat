using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterPatternTester : MonoBehaviour
{
    [System.Serializable]
    public struct CounterStep
    {
        [Tooltip("이 카운터가 열릴 목표 비트 인덱스(0부터 시작)")]
        public int targetBeatIndex;
        [Tooltip("목표 비트 몇 비트 전에 힌트를 줄지")]
        public float preHintBeats;
        [Tooltip("카운터 창 지속(비트)")]
        public float windowBeats;
        [Tooltip("이 스텝에서 requireOffBeat 강제 (옵션)")]
        public bool requireOffBeat;
    }

    [Header("Refs")]
    public CounterManager counter;   // 프로젝트에 있는 CounterManager 참조 드롭

    [Header("Pattern")]
    public List<CounterStep> steps = new List<CounterStep>()
    {
        new CounterStep { targetBeatIndex = 4,  preHintBeats = 1.0f, windowBeats = 0.5f },
        new CounterStep { targetBeatIndex = 8,  preHintBeats = 0.5f, windowBeats = 0.5f },
        new CounterStep { targetBeatIndex = 12, preHintBeats = 1.5f, windowBeats = 0.5f },
    };

    [Header("Options")]
    [Tooltip("true면 CounterManager에 비트 단위로 전달. false면 초 단위로 변환")]
    public bool unitsAreBeats = true;
    [Tooltip("패턴 시작을 지연(비트)")]
    public int startAfterBeats = 0;
    [Tooltip("끝나면 다시 반복할지")]
    public bool loop = false;
    [Tooltip("테스트 중 일시정지용")]
    public bool muteEmission = false;

    // 내부 상태
    private int _beatCount = -1;
    private int _lastBeatSeen = -1;
    private bool _running;

    private void OnEnable()
    {
        BeatManager.OnBeat += OnBeat;
    }

    private void OnDisable()
    {
        BeatManager.OnBeat -= OnBeat;
        _running = false;
        StopAllCoroutines();
    }

    private void Start()
    {
        if (counter == null)
        {
            counter = FindAnyObjectByType<CounterManager>(FindObjectsInactive.Include);
        }
        StartCoroutine(Co_Run());
    }

    private void OnBeat()
    {
        _beatCount++;
    }

    private IEnumerator Co_Run()
    {
        // BeatManager 준비 대기
        while (BeatManager.Instance == null || !BeatManager.Instance.IsInitialized)
            yield return null;

        // 첫 비트 기준점 리셋
        _beatCount = -1;
        _lastBeatSeen = -1;
        _running = true;

        do
        {
            // 시작 지연
            if (startAfterBeats > 0)
                yield return WaitBeats(startAfterBeats);

            // 스텝 정렬
            steps.Sort((a, b) => a.targetBeatIndex.CompareTo(b.targetBeatIndex));

            // 각 스텝 실행
            foreach (var s in steps)
            {
                if (!_running) yield break;
                // 힌트 타이밍 (목표 비트 - preHint)
                float hintAt = Mathf.Max(0, s.targetBeatIndex - s.preHintBeats);
                yield return WaitToBeat(hintAt);

                if (!muteEmission)
                {
                    // 필요 시 requireOffBeat 세팅(옵션)
                    TrySetRequireOffBeat(s.requireOffBeat);

                    if (unitsAreBeats)
                        counter.PreHint(s.preHintBeats);
                    else
                        counter.PreHint(s.preHintBeats * BeatManager.Instance.GetBeatDurationSec());
                }

                // 목표 비트까지 이동 후 창 오픈
                yield return WaitToBeat(s.targetBeatIndex);

                if (!muteEmission)
                {
                    if (unitsAreBeats)
                        counter.OpenWindow(s.windowBeats);
                    else
                        counter.OpenWindow(s.windowBeats * BeatManager.Instance.GetBeatDurationSec());
                }
            }

            // 다음 루프에서 다시 시작
        } while (loop && _running);
    }

    // ---- 헬퍼들 ----

    // 정수/실수 비트 모두 지원: 현재 비트가 target 이상이 될 때까지 대기
    private IEnumerator WaitToBeat(float targetBeat)
    {
        // 먼저 정수 비트 경계까지 OnBeat로 이동
        int targetWhole = Mathf.FloorToInt(targetBeat);
        int need = Mathf.Max(0, targetWhole - Mathf.Max(0, _beatCount));
        if (need > 0) yield return WaitBeats(need);

        // 분수 부분이 있으면 그만큼 초로 추가 대기
        float frac = Mathf.Max(0f, targetBeat - Mathf.Max(0, targetWhole));
        if (frac > 0f)
        {
            float sec = frac * BeatManager.Instance.GetBeatDurationSec();
            yield return new WaitForSecondsRealtime(sec);
        }
    }

    // 정확히 n개의 OnBeat를 기다림
    private IEnumerator WaitBeats(int n)
    {
        for (int i = 0; i < n; i++)
        {
            int start = _beatCount;
            yield return new WaitUntil(() => _beatCount > start);
        }
    }

    // CounterManager에 requireOffBeat 옵션이 있으면 반영(없어도 안전)
    private void TrySetRequireOffBeat(bool required)
    {
        try
        {
            var field = counter.GetType().GetField("requireOffBeat");
            if (field != null && field.FieldType == typeof(bool))
                field.SetValue(counter, required);

            var prop = counter.GetType().GetProperty("RequireOffBeat");
            if (prop != null && prop.PropertyType == typeof(bool))
                prop.SetValue(counter, required);
        }
        catch { /* 무시: 테스트 도구이므로 실패해도 진행 */ }
    }
}
