using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using LOADING_STATE = FMOD.Studio.LOADING_STATE; // (옵션) 위 상태코드도 별칭으로

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("FMOD Settings")]
    [SerializeField] private EventReference musicEvent;
    private EventInstance musicInstance;
    private EventDescription musicDesc;
    private EVENT_CALLBACK beatCallback;

    [Header("FMOD Loading & Latency")]
    [Tooltip("이벤트의 샘플 데이터를 사전에 로드")]
    [SerializeField] private bool preloadSampleData = true;
    [Tooltip("샘플 로드가 끝날 때까지 재생 시작을 대기")]
    [SerializeField] private bool waitForSampleDataBeforeStart = true;
    [Tooltip("샘플 로드 대기 타임아웃(초)")]
    [SerializeField] private float sampleLoadTimeout = 1.0f;
    [Tooltip("초기 N초 동안은 소프트 폴백(가상 타임라인) 비트 발생 금지")]
    [SerializeField] private float fallbackArmSeconds = 1.0f;

    [Header("BPM 변속")]
    [SerializeField, Range(0.5f, 2.0f)] private float currentSpeed = 1.0f;

    [Header("Tempo Settings")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;
    [Range(0f, 1f)][SerializeField] private float hitWindowPercent = 0.25f;

    [Header("Beat Source")]
    [SerializeField] private bool useFmodTempo = false;

    [Header("시작 딜레이(박자)")]
    [SerializeField] private int startDelayBeats = 0;

    [Header("판정 보정")]
    [SerializeField] private bool applyVisualOffsetToJudge = true;
    [SerializeField] private int visualOffsetMs = 0;

    // 계산 값
    private int intervalMs;
    private int hitRangeMs;
    private bool isInitialized;

    private float _lastTempoFromFmod = -1f;

    // 비트 방출 상태
    private int _lastBeatIndex = -1;
    private bool _offEmittedForThisBeat = false;

    // 폴백 타임라인
    private float _startUnscaledTime;
    private bool _useFallbackTime = false;
    private float _fallbackArmAt = 0f;   // 이 시점 전에는 폴백 금지
    private bool _sawAnyFmodBeat = false;

    // 판정 영역
    private struct JudgeZone { public int startMs, endMs; }
    private readonly List<JudgeZone> onBeatZones = new();
    private readonly List<JudgeZone> offBeatZones = new();

    // Pulse 대상
    public List<PulseToBeat> pulseTargets = new();

    // 이벤트
    public static event Action OnBeat;
    public static event Action OffBeat;

    private bool _suppressBeats = false;
    private volatile bool _tempoDirty = false;

    public bool IsInitialized => isInitialized;

    // 콜백 적재
    private int _pendingOnBeats = 0;
    private int _pendingHalfBeats = 0;

    [StructLayout(LayoutKind.Sequential)]
    struct TimelineBeatProperties
    {
        public int bar, beat, position;
        public float tempo;
        public int timesig_numerator, timesig_denominator;
    }

    private float _mainVolume = 1f;
    public float GetMainVolume() => _mainVolume;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (pulseTargets == null || pulseTargets.Count == 0)
            pulseTargets = new List<PulseToBeat>(FindObjectsOfType<PulseToBeat>(true));
    }

    void Start()
    {
        _mainVolume = PlayerPrefs.GetFloat("main_volume", 1f);
        StartCoroutine(Co_InitAndStartMusic());
    }

    IEnumerator Co_InitAndStartMusic()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicDesc = RuntimeManager.GetEventDescription(musicEvent);

        // 샘플 선로딩
        if (preloadSampleData && musicDesc.isValid())
        {
            musicDesc.loadSampleData();
            if (waitForSampleDataBeforeStart)
            {
                float end = Time.unscaledTime + sampleLoadTimeout;
                bool loaded = false;
                while (Time.unscaledTime < end)
                {
                    if (musicDesc.isValid())
                    {
                        FMOD.Studio.LOADING_STATE state;
                        musicDesc.getSampleLoadingState(out state);
                        if (state == FMOD.Studio.LOADING_STATE.LOADED) break;
                    }
                    RuntimeManager.StudioSystem.update();
                    yield return null;
                }
            }
        }

        int songLenMs = 180_000;
        if (musicDesc.isValid())
        {
            musicDesc.getLength(out songLenMs);
            if (songLenMs <= 0) songLenMs = 180_000;
        }

        RecalculateTiming();
        BuildJudgeZones(songLenMs);

        // 콜백 등록 (tempo 추출/비트 적재)
        beatCallback = TimelineBeatCallback;
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        // 재생
        musicInstance.start();
        musicInstance.setVolume(_mainVolume);

        // 폴백 지연 암(초기엔 FMOD 타임라인이 0일 수 있음)
        _fallbackArmAt = Time.unscaledTime + Mathf.Max(0f, fallbackArmSeconds);
        _useFallbackTime = false;
        _sawAnyFmodBeat = false;

        // 폴백 기준 시각
        musicInstance.getTimelinePosition(out int pos);
        if (pos <= 0) _startUnscaledTime = Time.unscaledTime;

        isInitialized = true;
        yield break;
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

    public void SetSpeed(float speedValue)
    {
        currentSpeed = Mathf.Max(0.1f, speedValue);
        if (musicInstance.isValid()) musicInstance.setPitch(currentSpeed);

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
        float basisBpm = (useFmodTempo && _lastTempoFromFmod > 0f) ? _lastTempoFromFmod : bpm;
        float effectiveBpm = basisBpm * currentSpeed;

        float stepIntervalSec = 60f / Mathf.Max(1e-4f, effectiveBpm * stepsPerBeat);
        intervalMs = Mathf.Max(1, Mathf.RoundToInt(stepIntervalSec * 1000f));
        float hitSec = stepIntervalSec * Mathf.Clamp01(hitWindowPercent);
        hitRangeMs = Mathf.Max(0, Mathf.RoundToInt(hitSec * 1000f));
    }

    void BuildJudgeZones(int songLenMs)
    {
        onBeatZones.Clear();
        offBeatZones.Clear();

        int delayMs = Mathf.Max(0, startDelayBeats) * Mathf.Max(1, intervalMs);

        for (int t = 0; t <= songLenMs; t += intervalMs)
            onBeatZones.Add(new JudgeZone { startMs = (t + delayMs) - hitRangeMs, endMs = (t + delayMs) + hitRangeMs });

        int offset = intervalMs / 2;
        for (int t = offset; t <= songLenMs; t += intervalMs)
            offBeatZones.Add(new JudgeZone { startMs = (t + delayMs) - hitRangeMs, endMs = (t + delayMs) + hitRangeMs });
    }

    void Update()
    {
        if (_tempoDirty)
        {
            RecalculateTiming();
            if (musicDesc.isValid())
            {
                musicDesc.getLength(out int songLenMs);
                if (songLenMs <= 0) songLenMs = 180_000;
                BuildJudgeZones(songLenMs);
            }
            _tempoDirty = false;
        }
        if (!isInitialized) return;

        if (_suppressBeats)
        {
            System.Threading.Interlocked.Exchange(ref _pendingOnBeats, 0);
            System.Threading.Interlocked.Exchange(ref _pendingHalfBeats, 0);
            return;
        }

        // 1) 콜백에서 쌓인 OnBeat
        int onCount = System.Threading.Interlocked.Exchange(ref _pendingOnBeats, 0);
        for (int i = 0; i < onCount; i++)
        {
            OnBeat?.Invoke();
            PulseAll();
            _lastBeatIndex++;
            _offEmittedForThisBeat = false;
        }

        // 2) 콜백에서 쌓인 반박 예약
        int halfCount = System.Threading.Interlocked.Exchange(ref _pendingHalfBeats, 0);
        for (int i = 0; i < halfCount; i++)
            StartCoroutine(Co_FireOffBeatHalfStep());

        // 3) FMOD가 아직 비트를 안 줬을 때만 “진짜로 필요할 때” 폴백
        if (onCount == 0 && halfCount == 0)
        {
            int tMs = GetTimelineMs();
            if (tMs <= 0) return;                 // 폴백 암 기간엔 그냥 대기
            if (intervalMs <= 0) return;

            int beatIndex = Mathf.FloorToInt(tMs / (float)intervalMs);

            if (beatIndex != _lastBeatIndex)
            {
                _lastBeatIndex = beatIndex;
                _offEmittedForThisBeat = false;

                OnBeat?.Invoke();
                PulseAll();
            }

            int halfPointMs = (_lastBeatIndex * intervalMs) + (intervalMs / 2);
            if (!_offEmittedForThisBeat && tMs >= halfPointMs)
            {
                _offEmittedForThisBeat = true;
                OffBeat?.Invoke();
            }
        }
    }

    void PulseAll()
    {
        for (int i = pulseTargets.Count - 1; i >= 0; i--)
        {
            var p = pulseTargets[i];
            if (p == null) { pulseTargets.RemoveAt(i); continue; }
            p.Pulse();
        }
    }

    public void SetBeatEmissionPaused(bool paused) => _suppressBeats = paused;

    int GetTimelineMs()
    {
        // 1) FMOD 타임라인 우선
        if (musicInstance.isValid())
        {
            musicInstance.getTimelinePosition(out int ms);
            if (ms > 0)
            {
                _useFallbackTime = false;
                return ms;
            }
        }

        // 2) 아직 FMOD에서 0만 나오고, 콜백도 안 왔고, 암 시간 미경과 → 폴백 금지
        if (!_sawAnyFmodBeat && Time.unscaledTime < _fallbackArmAt)
            return 0;

        // 3) 진짜로 타임라인을 못 받는 환경에서만 폴백
        if (!_useFallbackTime)
        {
            _useFallbackTime = true;
            _startUnscaledTime = Time.unscaledTime;
        }
        return Mathf.RoundToInt((Time.unscaledTime - _startUnscaledTime) * 1000f);
    }

    int GetJudgeMs()
    {
        int t = GetTimelineMs();
        if (applyVisualOffsetToJudge) t += visualOffsetMs;
        return t;
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
            if (Instance.useFmodTempo) Instance._tempoDirty = true;

            System.Threading.Interlocked.Increment(ref Instance._pendingOnBeats);
            System.Threading.Interlocked.Increment(ref Instance._pendingHalfBeats);

            Instance._sawAnyFmodBeat = true; // 실제 비트 수신 시작
        }
        return FMOD.RESULT.OK;
    }

    IEnumerator Co_FireOffBeatHalfStep()
    {
        while (_suppressBeats) yield return null;

        float stepIntervalSec = Mathf.Max(1, intervalMs) / 1000f;
        float wait = Mathf.Max(0f, (stepIntervalSec * 0.5f) + (visualOffsetMs / 1000f));

        float t = 0f;
        while (t < wait)
        {
            if (_suppressBeats) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        OffBeat?.Invoke();
    }

    void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.setCallback(null);
            musicInstance.stop(STOP_MODE.IMMEDIATE);
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
    public bool IsOnBeatNow() => IsInZone(GetJudgeMs(), onBeatZones);
    public bool IsOffBeatNow() => IsInZone(GetJudgeMs(), offBeatZones);

    public float GetEffectiveBpm()
    {
        float basis = (useFmodTempo && _lastTempoFromFmod > 0f) ? _lastTempoFromFmod : bpm;
        return basis * currentSpeed;
    }

    public float GetBeatDurationSec()
    {
        float bpmEff = GetEffectiveBpm();
        return 60f / Mathf.Max(1e-4f, bpmEff);
    }

    public void SetMusicPaused(bool paused)
    {
        if (musicInstance.isValid()) musicInstance.setPaused(paused);
        _suppressBeats = paused;
    }

    public bool IsMusicValid() => musicInstance.isValid();

    public void SetMainVolume(float v)
    {
        _mainVolume = Mathf.Clamp01(v);
        if (musicInstance.isValid()) musicInstance.setVolume(_mainVolume);
        PlayerPrefs.SetFloat("main_volume", _mainVolume);
    }
}
