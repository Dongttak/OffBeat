using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "BeatSheet", menuName = "Offbeat/Beat Sheet")]
public class BeatSheetAsset : ScriptableObject
{
    [Header("마디당 박 수 (measureBeat 파싱용)")]
    public int beatsPerMeasure = 4;

    [Header("정박 프레임들 (OnBeat에 실행)")]
    public List<BeatFrame> onBeatFrames = new();

    [Header("엇박 프레임들 (OffBeat에 실행)")]
    public List<BeatFrame> offBeatFrames = new();

    [Serializable]
    public class BeatFrame
    {
        [Header("Beat Address (둘 중 하나)")]
        [Tooltip("절대 비트 인덱스(0부터). measureBeat가 비어있을 때만 사용")]
        public int beatIndex = -1;

        [Tooltip("마디:박 (예: 3:2). 있으면 beatIndex보다 우선")]
        public string measureBeat = "";

        [Header("이 박자에 할 일들")]
        public List<SpawnEntry> spawns = new();
        public List<EventEntry> events = new();
        public List<CounterEntry> counters = new();
        public List<AnimatorTriggerEntry> animators = new();
        public List<Pattern2ToggleEntry> pattern2 = new();

        public int ResolveBeatIndex(int beatsPerMeasure)
        {
            if (!string.IsNullOrWhiteSpace(measureBeat) &&
                TimelineEntryBase.TryParseMeasureBeat(measureBeat, beatsPerMeasure, out int idx))
                return idx;
            return Mathf.Max(0, beatIndex);
        }
    }

    // ===== 기존 Entry 타입 재사용 =====
    [Serializable]
    public abstract class TimelineEntryBase
    {
        public static bool TryParseMeasureBeat(string s, int beatsPerMeasure, out int resultIndex)
        {
            resultIndex = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var p = s.Split(':');
            if (p.Length != 2) return false;
            if (!int.TryParse(p[0], out int measure)) return false;
            if (!int.TryParse(p[1], out int beat)) return false;
            measure = Mathf.Max(1, measure);
            beat = Mathf.Max(1, beat);
            resultIndex = (measure - 1) * beatsPerMeasure + (beat - 1);
            return true;
        }
    }

    [Serializable]
    public class SpawnEntry : TimelineEntryBase
    {
        public GameObject prefab;
        public Transform point;
        public Vector3 offset;
        public float autoDestroyAfter = 0f;
    }

    [Serializable]
    public class EventEntry : TimelineEntryBase
    {
        public UnityEvent onTrigger;
    }

    [Serializable]
    public class CounterEntry : TimelineEntryBase
    {
        public bool openAsHint = true;
        public float leadSeconds = 0.25f;
        public float windowSeconds = 0f;
    }

    [Serializable]
    public class AnimatorTriggerEntry : TimelineEntryBase
    {
        public Animator animator;
        public string trigger;
    }

    [Serializable]
    public class Pattern2ToggleEntry : TimelineEntryBase
    {
        public GameObject car;
        public bool enable = true;
    }
}
