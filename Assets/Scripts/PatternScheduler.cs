using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 비트(정박/엇박) 인덱스에 액션을 매핑해서 실행하는 간단 스케줄러.
/// - BeatManager.OnBeat / OffBeat를 듣고 각 카운터 증가
/// - 같은 비트 번호에 등록한 액션을 모두 실행
/// - 프리팹 스폰, UnityEvent 호출, CounterManager 연동을 바로 지원
/// </summary>
public class PatternScheduler : MonoBehaviour
{
    [Header("Sync")]
    [Tooltip("비트 카운터를 0으로 맞출지 (씬 시작 시)")]
    [SerializeField] private bool resetOnStart = true;

    [Header("Measure Helper (옵션)")]
    [Tooltip("마디/박 표기에서 한 마디의 박 수(보통 4/4면 4)")]
    [SerializeField] private int beatsPerMeasure = 4;

    [Header("Spawn Settings")]
    [Tooltip("스폰된 오브젝트의 부모 (비워두면 월드 루트)")]
    [SerializeField] private Transform spawnParent;

    [Header("Counter (옵션)")]
    [SerializeField] private CounterManager counterManager;

    [Header("On-Beat Timeline (정박)")]
    public List<SpawnEntry> onBeatSpawns = new();
    public List<EventEntry> onBeatEvents = new();
    public List<CounterEntry> onBeatCounters = new();

    [Header("Off-Beat Timeline (엇박)")]
    public List<SpawnEntry> offBeatSpawns = new();
    public List<EventEntry> offBeatEvents = new();
    public List<CounterEntry> offBeatCounters = new();

    [Header("Animator Triggers (OnBeat)")]
    public List<AnimatorTriggerEntry> onBeatAnimator = new();
    [Header("Animator Triggers (OffBeat)")]
    public List<AnimatorTriggerEntry> offBeatAnimator = new();

    [Header("Pattern2 Toggles (OnBeat)")]
    public List<Pattern2ToggleEntry> onBeatPattern2 = new();
    [Header("Pattern2 Toggles (OffBeat)")]
    public List<Pattern2ToggleEntry> offBeatPattern2 = new();

    [Serializable]
    public class AnimatorTriggerEntry : TimelineEntryBase
    {
        [Header("Animator Trigger")]
        public Animator animator;
        public string trigger;
    }

    [Serializable]
    public class Pattern2ToggleEntry : TimelineEntryBase
    {
        [Header("Pattern2 Toggle")]
        [Tooltip("토글할 차량 오브젝트 (Pattern2CarCharger 프리팹 등)")]
        public GameObject car;

        [Tooltip("true = 활성화 / false = 비활성화")]
        public bool enable = true;
    }


    // 내부 카운터
    private int _onBeatIndex = -1;     // 첫 OnBeat 때 0으로 시작하도록 -1로
    private int _offBeatIndex = -1;

    private void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
    }

    private void OnEnable()
    {
        BeatManager.OnBeat += HandleOnBeat;
        BeatManager.OffBeat += HandleOffBeat;
    }

    private void OnDisable()
    {
        BeatManager.OnBeat -= HandleOnBeat;
        BeatManager.OffBeat -= HandleOffBeat;
    }

    private void Start()
    {
        if (resetOnStart) ResetTimeline();
    }

    public void ResetTimeline()
    {
        _onBeatIndex = -1;
        _offBeatIndex = -1;
    }

    // ───────────────────────── 핸들러

    // HandleOnBeat/HandleOffBeat 안에서 실행 추가
    private void HandleOnBeat()
    {
        _onBeatIndex++;
        ExecuteSpawns(onBeatSpawns, _onBeatIndex);
        ExecuteEvents(onBeatEvents, _onBeatIndex);
        ExecuteCounters(onBeatCounters, _onBeatIndex);
        ExecuteAnimator(onBeatAnimator, _onBeatIndex);
        ExecutePattern2(onBeatPattern2, _onBeatIndex);
    }

    private void HandleOffBeat()
    {
        _offBeatIndex++;
        ExecuteSpawns(offBeatSpawns, _offBeatIndex);
        ExecuteEvents(offBeatEvents, _offBeatIndex);
        ExecuteCounters(offBeatCounters, _offBeatIndex);
        ExecuteAnimator(offBeatAnimator, _offBeatIndex);
        ExecutePattern2(offBeatPattern2, _offBeatIndex);
    }


    // ───────────────────────── 실행기들

    private void ExecuteSpawns(List<SpawnEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.Match(beat, beatsPerMeasure))
            {
                var pos = e.point ? e.point.position : Vector3.zero;
                var rot = e.point ? e.point.rotation : Quaternion.identity;
                var go = Instantiate(e.prefab, pos + e.offset, rot, spawnParent ? spawnParent : null);

                if (e.autoDestroyAfter > 0f)
                    Destroy(go, e.autoDestroyAfter);
            }
        }
    }

    private void ExecuteEvents(List<EventEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.Match(beat, beatsPerMeasure))
                e.onTrigger?.Invoke();
        }
    }

    private void ExecuteCounters(List<CounterEntry> list, int beat)
    {
        if (!counterManager) return;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e.Match(beat, beatsPerMeasure)) continue;

            if (e.openAsHint)
                counterManager.PreHint(e.leadSeconds);
            else
                counterManager.OpenWindow(e.windowSeconds > 0f ? e.windowSeconds : counterManager.WindowDuration);
        }
    }
    // 실행부
    private void ExecuteAnimator(List<AnimatorTriggerEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.Match(beat, beatsPerMeasure))
                if (e.animator && !string.IsNullOrEmpty(e.trigger))
                    e.animator.SetTrigger(e.trigger);
        }
    }
    private void ExecutePattern2(List<Pattern2ToggleEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.Match(beat, beatsPerMeasure) && e.car)
                e.car.SetActive(e.enable);
        }
    }

    // ───────────────────────── 데이터 구조

    // ── TimelineEntryBase에 “반복/시작 지연” 옵션 추가 ──
    [Serializable]
    public abstract class TimelineEntryBase
    {
        [Header("Beat Address")]
        public int beatIndex = -1;
        public string measureBeat = "";

        [Header("Repeat (옵션)")]
        [Tooltip(">=1이면 ‘firstBeat’부터 everyN 박마다 실행")]
        public int everyN = 0;
        [Tooltip("반복 시작 오프셋(절대 비트 인덱스). 0이면 첫 실행이 0비트")]
        public int firstBeat = 0;

        public bool Match(int currentBeat, int beatsPerMeasure)
        {
            int target = ResolveBeatIndex(beatsPerMeasure);

            // 단일 지점 지정(기존 동작)
            if (everyN <= 0) return currentBeat == target;

            // 반복 실행: currentBeat가 firstBeat 이후이며 everyN 배수인 경우
            if (currentBeat < firstBeat) return false;
            return (currentBeat - firstBeat) % everyN == 0;
        }

        public int ResolveBeatIndex(int bpmBeatsPerMeasure)
        {
            if (!string.IsNullOrWhiteSpace(measureBeat))
            {
                if (TryParseMeasureBeat(measureBeat, bpmBeatsPerMeasure, out int idx))
                    return idx;
            }
            return Mathf.Max(0, beatIndex);
        }

        public static bool TryParseMeasureBeat(string s, int beatsPerMeasure, out int resultIndex)
        {
            resultIndex = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int measure)) return false;
            if (!int.TryParse(parts[1], out int beat)) return false;
            measure = Mathf.Max(1, measure);
            beat = Mathf.Max(1, beat);
            resultIndex = (measure - 1) * beatsPerMeasure + (beat - 1);
            return true;
        }
    }

    [Serializable]
    public class SpawnEntry : TimelineEntryBase
    {
        [Header("Spawn")]
        public GameObject prefab;
        public Transform point;
        public Vector3 offset;
        [Tooltip("이 시간이 지나면 자동 제거(0이면 유지)")]
        public float autoDestroyAfter = 0f;
    }

    [Serializable]
    public class EventEntry : TimelineEntryBase
    {
        [Header("Event")]
        public UnityEvent onTrigger;
    }

    [Serializable]
    public class CounterEntry : TimelineEntryBase
    {
        [Header("Counter")]
        [Tooltip("true면 힌트(!) → leadSeconds 후 창 오픈 / false면 즉시 판정창만 오픈")]
        public bool openAsHint = true;

        [Tooltip("힌트 사용 시, 힌트 후 창이 열릴 때까지 지연(초)")]
        public float leadSeconds = 0.25f;

        [Tooltip("즉시 창만 열 때의 길이(초). 0이면 CounterManager의 기본값 사용")]
        public float windowSeconds = 0f;
    }
}
