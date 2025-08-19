using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PatternTimeline : MonoBehaviour
{
    // Lane은 0,1,2만 사용
    const int MAX_LANE_INDEX = 2; // 0..2

    [Header("Lane / Common")]
    [Tooltip("레인 기준점들 (0=좌, 1=중앙, 2=우) — 반드시 3개!")]
    public Transform[] laneAnchors;

    [Tooltip("스폰된 오브젝트의 부모 (비우면 루트)")]
    public Transform spawnParent;

    [Tooltip("카운터 매니저(없으면 자동 탐색)")]
    public CounterManager counterManager;

    [Header("Measure Parsing (옵션)")]
    [Tooltip("measureBeat(\"마디:박\")를 쓸 때 마디당 박 수. 4/4면 4")]
    public int beatsPerMeasure = 4;

    [Header("Steps (이 리스트만 채우면 됩니다)")]
    public List<FixedStep> steps = new();

    // 내부 스텝 카운터 (정박/엇박)
    int _onStep = -1;
    int _offStep = -1;

    // 빠른 조회용 맵
    Dictionary<int, List<FixedStep>> _onMap, _offMap;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        BuildLookups();

        if (laneAnchors == null || laneAnchors.Length < 3)
            Debug.LogWarning("[PatternTimeline] laneAnchors는 최소 3개(0,1,2)여야 합니다.");
    }

    public void Begin()
    {
        _onStep = -1;
        _offStep = -1;
    }

    void OnEnable()
    {
        BeatManager.OnBeat += HandleOnBeat;
        BeatManager.OffBeat += HandleOffBeat;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= HandleOnBeat;
        BeatManager.OffBeat -= HandleOffBeat;
    }

    void HandleOnBeat()
    {
        _onStep++;
        if (_onMap != null && _onMap.TryGetValue(_onStep, out var list))
            for (int i = 0; i < list.Count; i++) ExecuteStep(list[i]);
    }

    void HandleOffBeat()
    {
        _offStep++;
        if (_offMap != null && _offMap.TryGetValue(_offStep, out var list))
            for (int i = 0; i < list.Count; i++) ExecuteStep(list[i]);
    }

    // ───────────────────────── Build
    void BuildLookups()
    {
        _onMap = new Dictionary<int, List<FixedStep>>();
        _offMap = new Dictionary<int, List<FixedStep>>();

        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            int idx = s.ResolveStepIndex(beatsPerMeasure);
            var map = (s.when == FixedStep.When.OnBeat) ? _onMap : _offMap;

            if (!map.TryGetValue(idx, out var list)) map[idx] = list = new List<FixedStep>();
            list.Add(s);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Lookups")]
    void RebuildLookups_Editor() => BuildLookups();
    void OnValidate()
    {
        if (!Application.isPlaying) BuildLookups();
        // 0..2 범위 보정
        if (steps != null)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                steps[i].lane = Mathf.Clamp(steps[i].lane, 0, MAX_LANE_INDEX);
                if (steps[i].spawns != null)
                    for (int k = 0; k < steps[i].spawns.Count; k++)
                        steps[i].spawns[k].lane = Mathf.Clamp(steps[i].spawns[k].lane, 0, MAX_LANE_INDEX);
            }
        }
    }
#endif

    // ───────────────────────── Execute
    void ExecuteStep(FixedStep s)
    {
        // 0) (하위 호환) 단일 스폰 필드가 있으면 우선 실행
        if (s.prefab)
        {
            SpawnAtLane(s.prefab, s.lane, s.localOffset, 0f);
        }
        for (int i = 0; i < s.spawns.Count; i++)
        {
            var a = s.spawns[i];
            if (!a.prefab) continue;

            Transform anchor = a.point;
            int laneToUse = Mathf.Clamp(a.lane, 0, MAX_LANE_INDEX);

            if (!anchor && laneAnchors != null && laneAnchors.Length > laneToUse)
                anchor = laneAnchors[laneToUse];

            Vector3 pos = (anchor ? anchor.position : Vector3.zero) + a.offset;
            Quaternion rot = anchor ? anchor.rotation : Quaternion.identity;

            // 풀 사용 여부에 따라 스폰
            GameObject go = a.usePool
                ? PoolManager.SpawnObject(a.prefab, pos, rot)
                : Instantiate(a.prefab, pos, rot, spawnParent ? spawnParent : null);

            // 자동 반환/삭제
            if (a.autoDestroyAfter > 0f)
            {
                if (a.usePool) StartCoroutine(Co_ReturnAfter(go, a.autoDestroyAfter));
                else Destroy(go, a.autoDestroyAfter);
            }

            // LaneBlocker 레인 주입(재등록 포함)
            var lb = go.GetComponent<LaneBlocker>();
            if (lb) lb.SetLane(laneToUse);

            // (선택) Pattern2 호환
            var p2 = go.GetComponent<Pattern2>();
            if (p2) p2.laneIndex = laneToUse;
        }

        // 2) Events
        for (int i = 0; i < s.events.Count; i++)
            s.events[i]?.Invoke();

        // 3) Counters
        if (counterManager)
        {
            for (int i = 0; i < s.counters.Count; i++)
            {
                var c = s.counters[i];
                if (c.openAsHint) counterManager.PreHint(c.leadSeconds);
                else counterManager.OpenWindow(c.windowSeconds > 0f ? c.windowSeconds : counterManager.WindowDuration);
            }
        }

        // 4) Animators
        for (int i = 0; i < s.animators.Count; i++)
        {
            var an = s.animators[i];
            if (an && !string.IsNullOrEmpty(s.animatorTrigger))
                an.SetTrigger(s.animatorTrigger);
        }

        // 5) Toggles
        for (int i = 0; i < s.toggles.Count; i++)
        {
            var t = s.toggles[i];
            if (!t.target) continue;

            t.target.SetActive(t.enable);

            // CarPattern 있으면 직접 활성화도 호출(옵션)
            if (t.callCarSetActive && t.target.TryGetComponent<CarPattern>(out var car))
                car.SetActive(t.enable);
        }
    }

    void SpawnAtLane(GameObject prefab, int lane, Vector3 localOffset, float autoDestroyAfter)
    {
        if (!prefab || laneAnchors == null || laneAnchors.Length == 0) return;

        int ln = Mathf.Clamp(lane, 0, MAX_LANE_INDEX);
        if (laneAnchors.Length <= ln) { Debug.LogWarning("[PatternTimeline] laneAnchors 인덱스 범위 초과"); return; }

        var anchor = laneAnchors[ln];
        if (!anchor) return;

        var go = Instantiate(prefab, anchor.position + localOffset, Quaternion.identity, spawnParent ? spawnParent : null);
        if (autoDestroyAfter > 0f) Destroy(go, autoDestroyAfter);

        // ✅ 패턴 훅: Pattern2 laneIndex 세팅
        var p2 = go.GetComponent<Pattern2>();
        if (p2) p2.laneIndex = ln;
    }

    // ───────────────────────── Data
    [Serializable]
    public class FixedStep
    {
        public enum When { OnBeat, OffBeat }

        [Header("언제 실행할지")]
        [Tooltip("정박(OnBeat) / 엇박(OffBeat)")]
        public When when = When.OnBeat;

        [Header("주소 (둘 중 하나)")]
        [Tooltip("시작 후 N번째 스텝 (0부터)")]
        public int stepIndex = 0;

        [Tooltip("마디:박 표기 (예: 3:2). 있으면 stepIndex보다 우선")]
        public string measureBeat = "";

        [Header("단일 스폰 (하위 호환)")]
        [Tooltip("이 값이 있으면 아래 Spawns보다 먼저 1개 스폰")]
        public GameObject prefab;

        [Tooltip("레인 인덱스 (0,1,2만 사용)")]
        [Range(0, MAX_LANE_INDEX)] public int lane = 1;

        [Tooltip("레인 앵커 기준 오프셋")]
        public Vector3 localOffset;

        [Header("여러 스폰 (레이저/차 등 여러 개 동시에)")]
        public List<SpawnAction> spawns = new();

        [Header("토글 (차 on/off 등)")]
        public List<ToggleAction> toggles = new();

        [Header("애니메이터 트리거")]
        [Tooltip("SetTrigger를 보낼 Animator들")]
        public List<Animator> animators = new();
        [Tooltip("모든 Animator에 보낼 Trigger 이름")]
        public string animatorTrigger = "";

        [Header("카운터")]
        public List<CounterAction> counters = new();

        [Header("Unity Events")]
        public List<UnityEvent> events = new();

        // 주소 계산
        public int ResolveStepIndex(int beatsPerMeasure)
        {
            if (!string.IsNullOrWhiteSpace(measureBeat) &&
                TryParseMeasureBeat(measureBeat, beatsPerMeasure, out int idx))
                return idx;
            return Mathf.Max(0, stepIndex);
        }

        static bool TryParseMeasureBeat(string s, int beatsPerMeasure, out int result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int m)) return false;
            if (!int.TryParse(parts[1], out int b)) return false;
            m = Mathf.Max(1, m); b = Mathf.Max(1, b);
            result = (m - 1) * beatsPerMeasure + (b - 1);
            return true;
        }
    }

    [Serializable]
    public class SpawnAction
    {
        [Tooltip("생성할 프리팹 (레이저/차/적 등)")]
        public GameObject prefab;

        [Tooltip("레인 인덱스 (0,1,2) — point가 없을 때 사용")]
        [Range(0, MAX_LANE_INDEX)] public int lane = 1;

        [Tooltip("직접 위치 기준 Transform (있으면 이것 우선)")]
        public Transform point;

        [Tooltip("기준점에서의 위치 오프셋")]
        public Vector3 offset;

        [Tooltip("자동 제거까지 대기(초). 0=삭제 안함")]
        public float autoDestroyAfter = 0f;

        public bool usePool = false;
    }

    [Serializable]
    public class ToggleAction
    {
        [Tooltip("활성/비활성할 타겟 (예: Car 루트)")]
        public GameObject target;

        [Tooltip("true=켜기 / false=끄기")]
        public bool enable = true;

        [Tooltip("CarPattern 컴포넌트가 있으면 SetActive(bool)도 호출")]
        public bool callCarSetActive = true;
    }

    [Serializable]
    public class CounterAction
    {
        [Tooltip("true=힌트→지연 후 창 오픈 / false=즉시 창만 오픈")]
        public bool openAsHint = true;

        [Tooltip("힌트 사용 시, 힌트 후 창 오픈까지 지연(초)")]
        public float leadSeconds = 0.25f;

        [Tooltip("즉시 오픈 시 창 지속시간(초). 0이면 CounterManager 기본값")]
        public float windowSeconds = 0f;
    }
    IEnumerator Co_ReturnAfter(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go) PoolManager.ReturnObjectToPool(go);
    }
}
