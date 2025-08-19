using System.Collections.Generic;
using UnityEngine;
using static BeatSheetAsset;

[AddComponentMenu("Offbeat/Beat Sheet Runner")]
public class BeatSheetRunner : MonoBehaviour
{
    [Header("Sheet 참조")]
    public BeatSheetAsset sheet;

    [Header("옵션")]
    [Tooltip("씬 시작 시 내부 카운터 초기화")]
    public bool resetOnStart = true;

    [Tooltip("스폰된 오브젝트의 부모 (비우면 루트)")]
    public Transform spawnParent;

    [Tooltip("CounterManager 자동 찾기")]
    public CounterManager counterManager;

    // 내부 카운터
    int onBeatIdx = -1;
    int offBeatIdx = -1;

    // lookup 테이블 (절대비트 -> 프레임 리스트)
    Dictionary<int, List<BeatFrame>> onMap, offMap;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        BuildLookup();
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

    void Start()
    {
        if (resetOnStart) { onBeatIdx = -1; offBeatIdx = -1; }
    }

    void BuildLookup()
    {
        onMap = new Dictionary<int, List<BeatFrame>>();
        offMap = new Dictionary<int, List<BeatFrame>>();
        if (!sheet) return;

        foreach (var f in sheet.onBeatFrames)
        {
            int key = f.ResolveBeatIndex(sheet.beatsPerMeasure);
            if (!onMap.TryGetValue(key, out var list)) onMap[key] = list = new List<BeatFrame>();
            list.Add(f);
        }
        foreach (var f in sheet.offBeatFrames)
        {
            int key = f.ResolveBeatIndex(sheet.beatsPerMeasure);
            if (!offMap.TryGetValue(key, out var list)) offMap[key] = list = new List<BeatFrame>();
            list.Add(f);
        }
    }

    void HandleOnBeat()
    {
        onBeatIdx++;
        if (onMap != null && onMap.TryGetValue(onBeatIdx, out var frames))
            for (int i = 0; i < frames.Count; i++) ExecuteFrame(frames[i]);
    }

    void HandleOffBeat()
    {
        offBeatIdx++;
        if (offMap != null && offMap.TryGetValue(offBeatIdx, out var frames))
            for (int i = 0; i < frames.Count; i++) ExecuteFrame(frames[i]);
    }

    void ExecuteFrame(BeatFrame f)
    {
        // Spawn
        for (int i = 0; i < f.spawns.Count; i++)
        {
            var e = f.spawns[i];
            if (!e.prefab) continue;
            var pos = e.point ? e.point.position : Vector3.zero;
            var rot = e.point ? e.point.rotation : Quaternion.identity;
            var go = Instantiate(e.prefab, pos + e.offset, rot, spawnParent ? spawnParent : null);
            if (e.autoDestroyAfter > 0f) Destroy(go, e.autoDestroyAfter);
        }

        // Event
        for (int i = 0; i < f.events.Count; i++)
            f.events[i].onTrigger?.Invoke();

        // Counter
        if (counterManager != null)
        {
            for (int i = 0; i < f.counters.Count; i++)
            {
                var e = f.counters[i];
                if (e.openAsHint) counterManager.PreHint(e.leadSeconds);
                else counterManager.OpenWindow(e.windowSeconds > 0f ? e.windowSeconds : counterManager.WindowDuration);
            }
        }

        // Animator
        for (int i = 0; i < f.animators.Count; i++)
        {
            var e = f.animators[i];
            if (e.animator && !string.IsNullOrEmpty(e.trigger))
                e.animator.SetTrigger(e.trigger);
        }

        // Pattern2 toggle
        for (int i = 0; i < f.pattern2.Count; i++)
        {
            var e = f.pattern2[i];
            if (e.car) e.car.SetActive(e.enable);
        }
    }

#if UNITY_EDITOR
    // 에셋 바뀌면 런타임 중에도 다시 빌드하고 싶을 때 호출
    [ContextMenu("Rebuild Lookup")]
    void Rebuild() => BuildLookup();
#endif
}
