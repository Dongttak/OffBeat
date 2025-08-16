using System;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Runtime.InteropServices;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("FMOD Settings")]
    [SerializeField] private EventReference musicEvent;
    private EventInstance musicInstance;
    private EventDescription musicDesc;
    private EVENT_CALLBACK beatCallback;
    private int songLengthMs = 0;

    
    [Header("Tempo Settings")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f; // 4분음표 = 1, 8분음표 = 2
    [SerializeField, Range(0f, 1f)] private float hitWindowPercent = 0.25f;

    private int intervalMs;
    private int hitRangeMs;
    private bool isInitialized;

    private int _pendingBeatCount = 0;

    private readonly List<JudgeZone> onBeatZones = new();
    private readonly List<JudgeZone> offBeatZones = new();

    public List<PulseToBeat> pulseTargets = new();

    public static event Action OnBeat;
    public static event Action OffBeat;

    public bool IsInitialized => isInitialized;

    private struct JudgeZone { public int startMs, endMs; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (pulseTargets == null || pulseTargets.Count == 0)
            pulseTargets = new List<PulseToBeat>(FindObjectsOfType<PulseToBeat>(true));
    }

    private void Start()
    {
        InitAndStartMusic();
    }

    private void InitAndStartMusic()
    {
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicDesc = RuntimeManager.GetEventDescription(musicEvent);
        musicDesc.getLength(out int songLengthMs);

        RecalculateTiming();
        BuildJudgeZones(songLengthMs);

        beatCallback = TimelineBeatCallback;
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        musicInstance.start();
        isInitialized = true;
    }
    public void SetSongLengthSeconds(float seconds)
    {
        songLengthMs = Mathf.RoundToInt(seconds * 1000f);
        BuildJudgeZones(songLengthMs);  // 판정 구간 갱신
    }
    public void SetTempo(float newBpm, float newStepsPerBeat = -1f, float? newHitWindowPercent = null)
    {
        if (newBpm > 0f) bpm = newBpm;
        if (newStepsPerBeat > 0f) stepsPerBeat = newStepsPerBeat;
        if (newHitWindowPercent.HasValue)
            hitWindowPercent = Mathf.Clamp01(newHitWindowPercent.Value);

        RecalculateTiming();

        if (musicDesc.isValid())
        {
            musicDesc.getLength(out int songLenMs);
            BuildJudgeZones(songLenMs);
        }
    }

    private void RecalculateTiming()
    {
        float intervalSec = 60f / (bpm * stepsPerBeat);
        intervalMs = Mathf.RoundToInt(intervalSec * 1000f);
        hitRangeMs = Mathf.RoundToInt(intervalSec * hitWindowPercent * 1000f);
    }

    private void BuildJudgeZones(int songLenMs)
    {
        onBeatZones.Clear();
        offBeatZones.Clear();

        for (int t = 0; t <= songLenMs; t += intervalMs)
        {
            onBeatZones.Add(new JudgeZone { startMs = t - hitRangeMs, endMs = t + hitRangeMs });
        }

        int offset = intervalMs / 2;
        for (int t = offset; t <= songLenMs; t += intervalMs)
        {
            offBeatZones.Add(new JudgeZone { startMs = t - hitRangeMs, endMs = t + hitRangeMs });
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (_pendingBeatCount > 0)
        {
            _pendingBeatCount = 0;

            int now = GetTimelineMs();

            if (IsInZone(now, onBeatZones))
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                OnBeat?.Invoke();
            }
            else if (IsInZone(now, offBeatZones))
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

    private int GetTimelineMs()
    {
        if (musicInstance.isValid())
        {
            musicInstance.getTimelinePosition(out int ms);
            return ms;
        }
        return 0;
    }

    private static bool IsInZone(int t, List<JudgeZone> zones)
    {
        foreach (var z in zones)
        {
            if (t < z.startMs) return false;
            if (t <= z.endMs) return true;
        }
        return false;
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT TimelineBeatCallback(EVENT_CALLBACK_TYPE type, IntPtr inst, IntPtr param)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT && Instance != null)
            Instance._pendingBeatCount++;
        return FMOD.RESULT.OK;
    }

    private void OnDestroy()
    {
        if (musicInstance.isValid())
        {
            musicInstance.setCallback(null);
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            musicInstance.release();
        }
    }

    public bool IsOnBeatNow() => IsInZone(GetTimelineMs(), onBeatZones);
    public bool IsOffBeatNow() => IsInZone(GetTimelineMs(), offBeatZones);
}
