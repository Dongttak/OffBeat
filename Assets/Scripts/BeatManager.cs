using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("FMOD Settings")]
    [SerializeField] private EventReference musicEvent;
    private EventInstance musicInstance;
    private EventDescription musicDesc;
    private EVENT_CALLBACK beatCallback;

    [Header("Tempo Settings")]
    [Tooltip("원하는 체감 BPM. useFmodTempo를 끄면 이 값 기준으로 OnBeat/OffBeat이 발생")]
    [SerializeField] private float bpm = 120f;

    [Tooltip("한 박자당 스텝 수 (4분음표=1, 8분음표=2)")]
    [SerializeField] private float stepsPerBeat = 1f;

    [Tooltip("판정 윈도우 폭 (스텝 길이의 %)")]
    [Range(0f, 1f)][SerializeField] private float hitWindowPercent = 0.25f;

    [Header("Beat Source")]
    [Tooltip("FMOD 콜백에서 내려주는 tempo를 쓸지 여부. 끄면 위의 bpm 고정")]
    [SerializeField] private bool useFmodTempo = false;

    private int intervalMs;   // 스텝 간격(ms)
    private int hitRangeMs;   // 판정 반경(ms)
    private bool isInitialized;

    private float _lastTempoFromFmod = -1f;

    // 타임라인 기반 beat emission
    private int _lastBeatIndex = -1;
    private bool _offEmittedForThisBeat = false;

    // 폴백용 (FMOD 타임라인이 0만 주는 환경)
    private float _startUnscaledTime;
    private bool _useFallbackTime = false;

    // 판정 영역
    private struct JudgeZone { public int startMs, endMs; }
    private readonly List<JudgeZone> onBeatZones = new();
    private readonly List<JudgeZone> offBeatZones = new();

    // Pulse 대상
    public List<PulseToBeat> pulseTargets = new();

    // 이벤트
    public static event Action OnBeat;
    public static event Action OffBeat;

    public bool IsInitialized => isInitialized;

    // FMOD 콜백에서 내려주는 속성
    [StructLayout(LayoutKind.Sequential)]
    struct TimelineBeatProperties
    {
        public int bar;
        public int beat;
        public int position;   // ms
        public float tempo;
        public int timesig_numerator;
        public int timesig_denominator;
    }
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (pulseTargets == null || pulseTargets.Count == 0)
            pulseTargets = new List<PulseToBeat>(FindObjectsOfType<PulseToBeat>(true));
    }
    
    void Start()
    {
        InitAndStartMusic();
    }

    void InitAndStartMusic()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicDesc = RuntimeManager.GetEventDescription(musicEvent);

        // 기본 곡 길이
        int songLenMs = 180_000;
        if (musicDesc.isValid())
        {
            musicDesc.getLength(out songLenMs);
            if (songLenMs <= 0) songLenMs = 180_000;
        }

        RecalculateTiming();
        BuildJudgeZones(songLenMs);

        // 콜백 등록 (tempo 추출용)
        beatCallback = TimelineBeatCallback;
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        musicInstance.start();
        isInitialized = true;

        // FMOD 타임라인이 0만 줄 경우 대비
        musicInstance.getTimelinePosition(out int pos);
        if (pos == 0)
        {
            _useFallbackTime = true;
            _startUnscaledTime = Time.unscaledTime;
        }
    }

    public void SetSongLengthSeconds(float seconds)
    {
        int ms = Mathf.RoundToInt(seconds * 1000f);
        BuildJudgeZones(ms);
    }

    public void SetTempo(float newBpm, float newStepsPerBeat = -1f, float? newHitWindowPercent = null)
    {
        if (newBpm > 0f) bpm = newBpm;
        if (newStepsPerBeat > 0f) stepsPerBeat = newStepsPerBeat;
        if (newHitWindowPercent.HasValue) hitWindowPercent = Mathf.Clamp01(newHitWindowPercent.Value);

        RecalculateTiming();

        if (musicDesc.isValid())
        {
            musicDesc.getLength(out int songLenMs);
            if (songLenMs <= 0) songLenMs = 180_000;
            BuildJudgeZones(songLenMs);
        }
    }

    void RecalculateTiming()
    {
        // 120 BPM 고정으로 쓰려면 useFmodTempo를 꺼두고 bpm=120, stepsPerBeat=1 유지
        float basisBpm = (useFmodTempo && _lastTempoFromFmod > 0f) ? _lastTempoFromFmod : bpm;

        float stepIntervalSec = 60f / Mathf.Max(1e-4f, basisBpm * stepsPerBeat);
        intervalMs = Mathf.RoundToInt(stepIntervalSec * 1000f);
        float hitSec = stepIntervalSec * Mathf.Clamp01(hitWindowPercent);
        hitRangeMs = Mathf.RoundToInt(hitSec * 1000f);
    }

    void BuildJudgeZones(int songLenMs)
    {
        onBeatZones.Clear();
        offBeatZones.Clear();

        for (int t = 0; t <= songLenMs; t += intervalMs)
            onBeatZones.Add(new JudgeZone { startMs = t - hitRangeMs, endMs = t + hitRangeMs });

        int offset = intervalMs / 2;
        for (int t = offset; t <= songLenMs; t += intervalMs)
            offBeatZones.Add(new JudgeZone { startMs = t - hitRangeMs, endMs = t + hitRangeMs });
    }

    void Update()
    {
        if (!isInitialized || intervalMs <= 0) return;

        int t = GetTimelineMs();
        int beatIndex = Mathf.FloorToInt(t / (float)intervalMs);

        // OnBeat
        if (beatIndex != _lastBeatIndex)
        {
            _lastBeatIndex = beatIndex;
            _offEmittedForThisBeat = false;

            OnBeat?.Invoke();
            PulseAll();
        }

        // OffBeat
        int halfPointMs = (_lastBeatIndex * intervalMs) + (intervalMs / 2);
        if (!_offEmittedForThisBeat && t >= halfPointMs)
        {
            _offEmittedForThisBeat = true;
            OffBeat?.Invoke();
        }
    }

    void PulseAll()
    {
        // null 들어있을 수 있으니 한번 정리
        for (int i = pulseTargets.Count - 1; i >= 0; i--)
        {
            var p = pulseTargets[i];
            if (p == null) { pulseTargets.RemoveAt(i); continue; }
            p.Pulse();
        }
    }

    int GetTimelineMs()
    {
        if (!_useFallbackTime && musicInstance.isValid())
        {
            musicInstance.getTimelinePosition(out int ms);
            if (ms > 0) return ms;
        }

        // fallback
        return Mathf.RoundToInt((Time.unscaledTime - _startUnscaledTime) * 1000f);
    }

    static bool IsInZone(int t, List<JudgeZone> zones)
    {
        foreach (var z in zones)
        {
            if (t < z.startMs) return false;
            if (t <= z.endMs) return true;
        }
        return false;
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT TimelineBeatCallback(EVENT_CALLBACK_TYPE type, IntPtr inst, IntPtr param)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT && Instance != null && param != IntPtr.Zero)
        {
            var props = Marshal.PtrToStructure<TimelineBeatProperties>(param);
            Instance._lastTempoFromFmod = props.tempo;
            if (Instance.useFmodTempo) Instance.RecalculateTiming();
        }
        return FMOD.RESULT.OK;
    }

    void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.setCallback(null);
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
    }
    public void RegisterPulseTarget(PulseToBeat p)
    {
        if (p == null) return;
        if (!pulseTargets.Contains(p)) pulseTargets.Add(p);
    }
    public void UnregisterPulseTarget(PulseToBeat p)
    {
        if (p == null) return;
        pulseTargets.Remove(p);
    }
    // 외부 판정용
    public bool IsOnBeatNow() => IsInZone(GetTimelineMs(), onBeatZones);
    public bool IsOffBeatNow() => IsInZone(GetTimelineMs(), offBeatZones);
}
