using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    // FMOD 인스턴스
    private EventInstance musicInstance;
    private EventDescription musicDescription;

    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;
    [SerializeField] private float hitRangePercentage = 0.25f;  // 판정 범위 (intervalDuration의 25%)

    [Header("FMOD Setting")]
    // FMOD 경로 설정
    [SerializeField] private EventReference musicEventPath;
    [SerializeField] private GameObject targetObject;

    // FMOD 이벤트 콜백
    private EVENT_CALLBACK beatCallback;

    // 박자 간격 (ms)
    private int intervalDurationMs;
    // 판정 범위 (ms)
    private int hitRangeMs;

    // 정박 타이밍을 저장할 변수
    private List<OnBeatJudgeZone> onBeatJudgeZones = new List<OnBeatJudgeZone>();

    // 정박 판정을 위한 구조체
    public struct OnBeatJudgeZone
    {
        public int startTimeMs;
        public int endTimeMs;
    }

    // 엇박 타이밍을 저장할 변수
    private List<OffBeatJudgeZone> offBeatJudgeZones = new List<OffBeatJudgeZone>();

    // 엇박 판정을 위한 구조체
    private struct OffBeatJudgeZone
    {
        private int startTimeMs;
        private int endTimeMs;
    }

    public List<GameObject> pulseToBeatObjects = new List<GameObject>();

    //private float songLength = 180f;
    private bool isInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitAndStartFMOD();
    }

    void InitAndStartFMOD()
    {
        // FMOD 인스턴스 생성
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicDescription = RuntimeManager.GetEventDescription(musicEventPath);

        // 총 노래 길이 저장
        int songLengthMs;
        musicDescription.getLength(out songLengthMs);

        // intervalDuration,hitRange ms 단위로 구하기
        float intervalDuration_s = 60f / (bpm * stepsPerBeat);
        float hitRange_s = intervalDuration_s * hitRangePercentage;
        intervalDurationMs = Mathf.RoundToInt(intervalDuration_s * 1000f);
        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

        // 콜백 이벤트 등록
        beatCallback = new EVENT_CALLBACK(TimelineCallback);

        // BEAT 이벤트 콜백 타입 설정
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        for (int time = 0; time < songLengthMs; time += intervalDurationMs)
        {
            OnBeatJudgeZone newOnBeatZone = new OnBeatJudgeZone
            {
                startTimeMs = time - hitRangeMs,
                endTimeMs = time + hitRangeMs,
            };
            onBeatJudgeZones.Add(newOnBeatZone);
        }

        Debug.Log($"총 적박 구간 개수: {onBeatJudgeZones.Count}개의 구간 존재");
        Debug.Log($"총 노래 길이: {songLengthMs} ms");
        Debug.Log($"intervalDuration: {intervalDurationMs} ms");

        musicInstance.start();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            int currentTimeMs = GetFMODTimelineSeconds();

            CheckBeat(currentTimeMs);
        }
    }

    private int GetFMODTimelineSeconds()
    {
        if (musicInstance.isValid())
        {
            musicInstance.getTimelinePosition(out int currentTimeMs);
            return currentTimeMs;
        }

        return 0;
    }

    private void CheckBeat(float currentTimeMs)
    {
        foreach (var zone in onBeatJudgeZones)
        {
            if (currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
            {
                OnBeat();
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                Debug.Log($"입력된 시간 : {currentTimeMs}ms");
                return; // 한 번 판정 후 종료
            }
            else
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                Debug.Log("박자 실패!");
            }
        }
    }

    private void OnBeat()
    {
        //if (targetObject != null && targetObject.TryGetComponent(out PulseToBeat pulse))
        //{
        //    pulse.Pulse();
        //}

        BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
        Debug.Log("정박 성공!");
    }

    private void OffBeat()
    {
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
        Debug.Log("엇박 성공!");
    }

    // 비트 이벤트 콜백 등록 - 박자 판정
    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    //static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    //{
    //    if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
    //    {

    //        foreach (GameObject obj in _pulseToBeatObjects)
    //        {
    //            obj.GetComponent<PulseToBeat>().Pulse();
    //        }
    //    }
    //    return FMOD.RESULT.OK;
    //}

    FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
        {
            foreach (GameObject obj in pulseToBeatObjects)
            {
                obj.GetComponent<PulseToBeat>().Pulse();
            }
        }
        return FMOD.RESULT.OK;
    }

    private void OnDestroy()
    {
        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
        //musicInstance.setCallback(null);
    }
}