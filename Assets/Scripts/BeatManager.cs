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
    [SerializeField] private float stepsPerBeat = 1f;                 // 4분음표=1
    [SerializeField, Range(0f, 1f)] private float hitRangePercentage = 0.25f;

    [Header("FMOD Setting")]
    [SerializeField] private EventReference musicEventPath;

    private EventInstance musicInstance;
    private EventDescription musicDescription;
    private EVENT_CALLBACK beatCallback;

    private int intervalDurationMs;
    private int hitRangeMs;

    // 메인스레드에서 처리할 비트 신호(콜백에서 ++)
    private int _pendingBeatCount = 0;

    // GameObject 대신 컴포넌트 캐싱(Null 안전)
    [SerializeField] private List<PulseToBeat> pulseTargets = new List<PulseToBeat>();

    // 정박/엇박 판정 구간
    private struct JudgeZone { public int startMs, endMs; }
    private readonly List<JudgeZone> onBeatZones = new List<JudgeZone>();
    private readonly List<JudgeZone> offBeatZones = new List<JudgeZone>();

    private bool isInitialized;

   
    public static event Action OnBeat;     // 정박 틱
    public static event Action OffBeat;    // 엇박 틱

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        if (pulseTargets == null || pulseTargets.Count == 0)
            pulseTargets = new List<PulseToBeat>(FindObjectsOfType<PulseToBeat>(true));
    }

    void Start()
    {
        InitAndStartFMOD();
    }

    void InitAndStartFMOD()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicDescription = RuntimeManager.GetEventDescription(musicEventPath);
        musicDescription.getLength(out int songLenMs);

        float interval_s = 60f / (bpm * stepsPerBeat);
        float hitRange_s = interval_s * hitRangePercentage;
        intervalDurationMs = Mathf.RoundToInt(interval_s * 1000f);
        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

        // 정박 구간 구성
        onBeatZones.Clear();
        for (int t = 0; t <= songLenMs; t += intervalDurationMs)
            onBeatZones.Add(new JudgeZone { startMs = t - hitRangeMs, endMs = t + hitRangeMs });

        // 엇박 구간 구성(정박의 절반 시프트)
        offBeatZones.Clear();
        int halfMs = intervalDurationMs / 2;
        for (int t = halfMs; t <= songLenMs; t += intervalDurationMs)
            offBeatZones.Add(new JudgeZone { startMs = t - hitRangeMs, endMs = t + hitRangeMs });

        // FMOD 콜백 등록
        beatCallback = TimelineCallback;
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        musicInstance.start();
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // 1) 콜백에서 누적된 비트 신호를 메인스레드에서 처리
        if (_pendingBeatCount > 0)
        {
            _pendingBeatCount = 0;

            // 정박/엇박 상태 갱신 & 외부 이벤트 브로드캐스트
            int now = GetFMODTimelineMs();
            if (IsInZones(now, onBeatZones))
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                OnBeat?.Invoke();
            }
            else if (IsInZones(now, offBeatZones))
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
                OffBeat?.Invoke();
            }
            else
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
            }

      
            foreach (var p in pulseTargets)
                if (p != null) p.Pulse();
        }
    }

    private int GetFMODTimelineMs()
    {
        if (musicInstance.isValid())
        {
            musicInstance.getTimelinePosition(out int ms);
            return ms;
        }
        return 0;
    }

    // 정박/엇박 범위 체크 도우미
    private static bool IsInZones(int t, List<JudgeZone> zones)
    {
        // 시간 순서대로 정렬되어 있으니 앞에서부터 확인
        for (int i = 0; i < zones.Count; i++)
        {
            var z = zones[i];
            if (t < z.startMs) return false; // 아직 이르다 → 조기 종료
            if (t <= z.endMs) return true;   // 구간 안
        }
        return false;
    }

    // === FMOD 오디오 스레드 콜백
    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
        {
            // 메인 인스턴스에 신호만 남김
            if (Instance != null) Instance._pendingBeatCount++;
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

    // (원하면 외부에서 직접 물어보게 헬퍼 제공)
    public bool IsOnBeatNow() => IsInZones(GetFMODTimelineMs(), onBeatZones);
    public bool IsOffBeatNow() => IsInZones(GetFMODTimelineMs(), offBeatZones);
}

