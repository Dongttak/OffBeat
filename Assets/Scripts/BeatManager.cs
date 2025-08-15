using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;                // 4분음표=1
    [SerializeField, Range(0f, 1f)] private float hitRangePercent = 0.25f;

    [Header("FMOD Setting")]
    [SerializeField] private EventReference musicEvent;

    // FMOD
    private EventInstance _instance;
    private EventDescription _desc;
    private EVENT_CALLBACK _callback;

    // 판정용 시간(ms)
    private int _intervalMs;   // 1스텝(=stepsPerBeat 기준 박자) 간격
    private int _hitRangeMs;   // 허용 범위

    // 외부에서 쓰라고 노출 (PlayerInput이 사용)
    public int IntervalMs => _intervalMs;
    public int HitRangeMs => _hitRangeMs;

    // 정박/엇박 구간
    private struct Zone { public int startMs, endMs; }
    private readonly List<Zone> _onZones = new List<Zone>();
    private readonly List<Zone> _offZones = new List<Zone>();

    // 오디오스레드 → 메인스레드 신호
    private int _pendingBeat;

    // (선택) 비주얼 펄스 타겟
    [SerializeField] private List<PulseToBeat> pulseTargets = new List<PulseToBeat>();

    // 외부 구독 이벤트(필요시 사용)
    public static event Action OnBeatTick;
    public static event Action OffBeatTick;

    public bool Initialized { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (pulseTargets == null || pulseTargets.Count == 0)
            pulseTargets = new List<PulseToBeat>(FindObjectsOfType<PulseToBeat>(true));
    }

    void Start()
    {
        InitAndStart();
    }

    void InitAndStart()
    {
        _instance = RuntimeManager.CreateInstance(musicEvent);
        _desc = RuntimeManager.GetEventDescription(musicEvent);
        _desc.getLength(out int songLenMs);

        float stepSec = 60f / (bpm * stepsPerBeat);
        float hitSec = stepSec * hitRangePercent;

        _intervalMs = Mathf.RoundToInt(stepSec * 1000f);
        _hitRangeMs = Mathf.RoundToInt(hitSec * 1000f);

        // 정박 구간
        _onZones.Clear();
        for (int t = 0; t <= songLenMs; t += _intervalMs)
            _onZones.Add(new Zone { startMs = t - _hitRangeMs, endMs = t + _hitRangeMs });

        // 엇박 구간(절반 시프트)
        _offZones.Clear();
        int half = _intervalMs / 2;
        for (int t = half; t <= songLenMs; t += _intervalMs)
            _offZones.Add(new Zone { startMs = t - _hitRangeMs, endMs = t + _hitRangeMs });

        _callback = TimelineCallback;
        _instance.setCallback(_callback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        _instance.start();
        Initialized = true;
    }

    void Update()
    {
        if (!Initialized) return;

        // 오디오스레드에서 들어온 비트 신호 처리(한 프레임 1회)
        if (_pendingBeat > 0)
        {
            _pendingBeat = 0;

            int now = GetTimelineMs();

            if (IsInZones(now, _onZones))
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                OnBeatTick?.Invoke();
            }
            else if (IsInZones(now, _offZones))
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
                OffBeatTick?.Invoke();
            }
            else
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
            }

            // 비주얼 펄스(선택)
            foreach (var p in pulseTargets)
                if (p != null) p.Pulse();
        }
    }

    // === 외부 헬퍼 ===
    public int GetTimelineMs()
    {
        if (_instance.isValid())
        {
            _instance.getTimelinePosition(out int ms);
            return ms;
        }
        return 0;
    }

    public bool IsOnBeatNow() => IsInZones(GetTimelineMs(), _onZones);
    public bool IsOffBeatNow() => IsInZones(GetTimelineMs(), _offZones);

    private static bool IsInZones(int t, List<Zone> zones)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (t < z.startMs) return false; // 아직 이르다
            if (t <= z.endMs) return true;  // 구간 안
        }
        return false;
    }

    // === FMOD 오디오스레드 콜백 ===
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT && Instance != null)
            Instance._pendingBeat++;
        return FMOD.RESULT.OK;
    }

    void OnDestroy()
    {
        if (_instance.isValid())
        {
            _instance.setCallback(null);
            _instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _instance.release();
        }
    }
}
