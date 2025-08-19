using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("FMOD Settings")]
    [SerializeField] private EventReference musicEvent;
    private EventInstance musicInstance;
    private EventDescription musicDesc;
    private EVENT_CALLBACK beatCallback;

    [Header("BPM 변속")]
    [SerializeField]
    [Range(0.5f, 2.0f)] private float currentSpeed = 1.0f;

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

    // 판정에도 같은 오프셋을 쓸지(권장: true)
    [SerializeField] private bool applyVisualOffsetToJudge = true;

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

    private bool _suppressBeats = false;
    private volatile bool _tempoDirty = false;


    public bool IsInitialized => isInitialized;
    // 콜백에서 적재할 플래그(메인스레드에서 꺼냄)
    private int _pendingOnBeats = 0;
    private int _pendingHalfBeats = 0;
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
    private float _mainVolume = 1f;  // 0~1
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
        InitAndStartMusic();
    }

    void InitAndStartMusic()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicDesc = RuntimeManager.GetEventDescription(musicEvent);

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
        musicInstance.setVolume(_mainVolume);
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

    // 노래 속도 조절, 1.0f 기본
    public void SetSpeed(float speedValue)
    {
        currentSpeed = Mathf.Max(0.1f, speedValue);

        if (musicInstance.isValid())
        {
            musicInstance.setPitch(currentSpeed);
        }

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

        float effectiveBpm = basisBpm * currentSpeed;

        float stepIntervalSec = 60f / Mathf.Max(1e-4f, effectiveBpm * stepsPerBeat);
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

        // === 일시정지면 비트/펄스 완전 차단 ===
        if (_suppressBeats)
        {
            // 콜백에서 쌓인 것들도 비워버림(방출 금지)
            System.Threading.Interlocked.Exchange(ref _pendingOnBeats, 0);
            System.Threading.Interlocked.Exchange(ref _pendingHalfBeats, 0);
            return;
        }
        // 1) 콜백에서 쌓인 OnBeat 처리
        int onCount = System.Threading.Interlocked.Exchange(ref _pendingOnBeats, 0);
        for (int i = 0; i < onCount; i++)
        {
            OnBeat?.Invoke();
            PulseAll();
            _lastBeatIndex++;
        }

        // 2) 콜백에서 쌓인 반박 처리
        int halfCount = System.Threading.Interlocked.Exchange(ref _pendingHalfBeats, 0);
        for (int i = 0; i < halfCount; i++)
            StartCoroutine(Co_FireOffBeatHalfStep());

        // 3) === 폴백 방출 ===
        // 이 프레임에 콜백 기반 OnBeat/OffBeat 예약이 전혀 없으면,
        // 타임라인(ms)로 직접 On/OffBeat를 방출해준다.
        if (onCount == 0 && halfCount == 0)
        {
            // 현재 시간(ms) 기준으로 박자 인덱스 계산
            int t = GetTimelineMs();
            if (intervalMs <= 0) return;

            int beatIndex = Mathf.FloorToInt(t / (float)intervalMs);

            // OnBeat 경계 통과
            if (beatIndex != _lastBeatIndex)
            {
                _lastBeatIndex = beatIndex;
                _offEmittedForThisBeat = false;

                OnBeat?.Invoke();
                PulseAll();
            }

            // 반 박자 시점에서 OffBeat
            int halfPointMs = (_lastBeatIndex * intervalMs) + (intervalMs / 2);
            if (!_offEmittedForThisBeat && t >= halfPointMs)
            {
                _offEmittedForThisBeat = true;
                OffBeat?.Invoke();
            }
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

    public void SetBeatEmissionPaused(bool paused)
    {
        _suppressBeats = paused;
    }

    int GetTimelineMs()
    {
        if (!_useFallbackTime && musicInstance.isValid())
        {
            musicInstance.getTimelinePosition(out int ms);
            if (ms > 0) return ms;
        }
        return Mathf.RoundToInt((Time.unscaledTime - _startUnscaledTime) * 1000f);
    }
    int GetJudgeMs()
    {
        int t = GetTimelineMs();
        if (applyVisualOffsetToJudge) t += visualOffsetMs;   // 시각과 판정 기준 일치
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

    // 옵션: 화면 보정용 (시각 이펙트를 조금 당기거나 늦추고 싶을 때)
    [SerializeField] private int visualOffsetMs = 0;

    // TIMELINE_BEAT 콜백 → OnBeat 예약 및 tempo 갱신
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    static FMOD.RESULT TimelineBeatCallback(EVENT_CALLBACK_TYPE type, IntPtr inst, IntPtr param)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT && Instance != null && param != IntPtr.Zero)
        {
            var props = Marshal.PtrToStructure<TimelineBeatProperties>(param);

            // FMOD가 내려준 템포 저장
            Instance._lastTempoFromFmod = props.tempo;
            if (Instance.useFmodTempo) Instance._tempoDirty = true;

            // 여기서 "정박" 1회 적재
            System.Threading.Interlocked.Increment(ref Instance._pendingOnBeats);

            // 엇박은 반 박자 뒤에 예약
            // (콜백 스레드 → 메인에서 소모)
            System.Threading.Interlocked.Increment(ref Instance._pendingHalfBeats);
        }
        return FMOD.RESULT.OK;
    }
    // 반 박자 뒤 OffBeat (타임스케일 무시)
    IEnumerator Co_FireOffBeatHalfStep()
    {
        // 일시정지 중이면 먼저 대기
        while (_suppressBeats) yield return null;

        float stepIntervalSec = intervalMs / 1000f;
        float half = stepIntervalSec * 0.5f;
        float visual = visualOffsetMs / 1000f;
        float wait = Mathf.Max(0f, half + visual);

        float t = 0f;
        while (t < wait)
        {
            if (_suppressBeats) yield break; // 도중에 다시 일시정지되면 중단
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
    public bool IsOnBeatNow() => IsInZone(GetJudgeMs(), onBeatZones);
    public bool IsOffBeatNow() => IsInZone(GetJudgeMs(), offBeatZones);
    public float GetEffectiveBpm()
    {
        float basis = (useFmodTempo && _lastTempoFromFmod > 0f) ? _lastTempoFromFmod : bpm;
        return basis * currentSpeed;  // 피치/속도 반영
    }

    public float GetBeatDurationSec()
    {
        float bpmEff = GetEffectiveBpm();
        return 60f / Mathf.Max(1e-4f, bpmEff);
    }
    // BeatManager.cs 내부
    public void SetMusicPaused(bool paused)
    {
        if (musicInstance.isValid())
            musicInstance.setPaused(paused);
        _suppressBeats = paused; // 음악 정지 시 비트 방출도 잠금
    }

    public bool IsMusicValid() => musicInstance.isValid();
    public void SetMainVolume(float v)
    {
        _mainVolume = Mathf.Clamp01(v);
        if (musicInstance.isValid())
            musicInstance.setVolume(_mainVolume);
        PlayerPrefs.SetFloat("main_volume", _mainVolume); // (선택) 저장
    }
}
