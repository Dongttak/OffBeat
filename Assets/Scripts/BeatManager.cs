using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    // FMOD 이벤트 인스턴스
    private EventInstance musicInstance;
    private EventDescription musicDescription;

    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;
    [SerializeField] private float hitRangePercentage = 0.25f;  // 판정 범위 (intervalDuration의 25%)

    [Header("FMOD Setting")]
    // 인스펙터에서 FMOD 이벤트 경로 설정
    [SerializeField] private EventReference musicEventPath; 
    [SerializeField] private GameObject targetObject;

    // FMOD 콜백을 위한 델리게이트
    private EVENT_CALLBACK beatCallback;

    // 비트 간격 (ms)
    private int intervalDurationMs;
    private float intervalDuration;     // 기존
    // 박자 판정 범위 (ms)
    private int hitRangeMs;
    private float hitRange;             // 기존

    // 정박 판정 구간을 미리 저장할 리스트
    private List<OnBeatJudgeZone> onBeatJudgeZones = new List<OnBeatJudgeZone>();

    // 정박 판정 구간 구조체
    public struct OnBeatJudgeZone
    {
        public int startTimeMs;    // 판정 구간 시작 시간 (ms)
        public int endTimeMs;      // 판정 구간 끝 시간 (ms)
    }

    // 엇박 판정 구간을 미리 저장할 리스트
    private List<OffBeatJudgeZone> offBeatJudgeZones = new List<OffBeatJudgeZone>();

    // 엇박 판정 구간 구조체
    private struct OffBeatJudgeZone
    {
        private int startTimeMs;    // 판정 구간 시작 시간 (ms)
        private int endTimeMs;      // 판정 구간 끝 시간 (ms)
    }

    [SerializeField] private List<GameObject> pulseToBeatObjects = new List<GameObject>();
    //private static List<GameObject> _pulseToBeatObjects = new List<GameObject>();

    // --------------------- 기존 코드
    private List<float> OnBeatList = new List<float>();
    private List<float> OffBeatList = new List<float>();
    //private int lastCheckedIndex_OnBeatList = 0;
    //private int lastCheckedIndex_OffBeatList = 0;

    private float songLength = 180f;
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
        // FMOD 이벤트 인스턴스와 디스크립션 생성
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicDescription = RuntimeManager.GetEventDescription(musicEventPath);

        // 이벤트 전체 길이 가져오기
        int songLengthMs;
        musicDescription.getLength(out songLengthMs);

        // intervalDuration과 hitRange를 ms 단위로 계산
        float intervalDuration_s = 60f / (bpm * stepsPerBeat);
        float hitRange_s = intervalDuration_s * hitRangePercentage;
        intervalDurationMs = Mathf.RoundToInt(intervalDuration_s * 1000f);
        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

        // 콜백 설정
        beatCallback = new EVENT_CALLBACK(TimelineCallback);

        // BEAT 타입 콜백
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        // 전체 타임라인에 대해 판정 구간을 미리 생성
        // TODO: start에서 작성된 내용이므로, 이후에 music이 바뀌는 상황이 생기면 코드 변경 필요
        for (int time = 0; time < songLengthMs; time += intervalDurationMs)
        {
            OnBeatJudgeZone newOnBeatZone = new OnBeatJudgeZone
            {
                startTimeMs = time - hitRangeMs,
                endTimeMs = time + hitRangeMs,
            };
            onBeatJudgeZones.Add(newOnBeatZone);
        }

        Debug.Log($"해당 노래에 {onBeatJudgeZones.Count} 개의 정박 구간 생성됨");
        Debug.Log($"해당 노래의 전체 길이: {songLengthMs} ms");
        Debug.Log($"intervalDuration: {intervalDurationMs} ms");

        // 리스트 초기화
        //_pulseToBeatObjects.Clear(); // 혹시 모를 데이터를 초기화
        //_pulseToBeatObjects.AddRange(pulseToBeatObjects);

        // 음악 재생
        musicInstance.start();

        ///// ----------------------- 기존 코드
        //intervalDuration = 60f / (bpm * stepsPerBeat);
        //hitRange = intervalDuration * 0.25f;

        // 비트 리스트 생성
        //OnBeatList.Clear();
        //OffBeatList.Clear();
        //lastCheckedIndex_OnBeatList = 0;
        //lastCheckedIndex_OffBeatList = 0;

        //for (float t = 0f; t <= songLength; t += intervalDuration)
        //{
        //    OnBeatList.Add(t);
        //}

        //for (float t = intervalDuration / 2f; t <= songLength; t += intervalDuration)
        //{
        //    OffBeatList.Add(t);
        //}

        //musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        //musicInstance.start();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            int currentTimeMs = GetFMODTimelineSeconds();

            Debug.Log($"현재 FMOD 재생 시점 (초): {currentTimeMs:F3}");
            CheckBeat(currentTimeMs);
        }
    }

    private int GetFMODTimelineSeconds()
    {
        if (musicInstance.isValid())
        {
            // 실행 중인 음악의 현재 타임라인 시간을 불러옴
            // (Ms) -> audiosource 에서 time 접근 시에는 입력 시 unity scene 재생 시간을 입력받지만 해당 구문은 타임라인 시간을 가져오므로 loop에 대응 가능
            musicInstance.getTimelinePosition(out int currentTimeMs);
            return currentTimeMs;
        }

        return 0;
    }

    private void CheckBeat(float currentTimeMs)
    {
        // 미리 생성한 정박 판정 구간 리스트를 순회하며 현재 시간이 해당 구간에 속하는지 확인
        foreach (var zone in onBeatJudgeZones)
        {
            if (currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
            {
                // 판정 성공
                Debug.Log($"정박 성공! 입력 시간: {currentTimeMs}ms");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                return; // 한 번 판정 후 종료
            }
            else
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
            }
        }

        // 판정 실패
        Debug.Log($"정박 실패! 입력 시간: {currentTimeMs}ms");

        ///// ------------------------- 기존

        // 정박 판정
        //for (int i = lastCheckedIndex_OnBeatList; i < OnBeatList.Count; i++)
        //{
        //    float beatTime = OnBeatList[i];

        //    if (beatTime < currentTimeMs - hitRange)
        //    {
        //        lastCheckedIndex_OnBeatList = i + 1;
        //        BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
        //        continue;
        //    }

        //    if (Mathf.Abs(currentTimeMs - beatTime) <= hitRange)
        //    {
        //        lastCheckedIndex_OnBeatList = i + 1;
        //        OnBeat();
        //        return;
        //    }
        //    else
        //    {
        //        Debug.Log("정박 실패!");
        //        BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
        //        continue;
        //        //return;
        //    }
        //}

        //// 엇박 판정
        //for (int i = lastCheckedIndex_OffBeatList; i < OffBeatList.Count; i++)
        //{
        //    float beatTime = OffBeatList[i];

        //    if (beatTime < currentTimeMs - hitRange)
        //    {
        //        lastCheckedIndex_OffBeatList = i + 1;
        //        BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
        //        continue;
        //    }

        //    if (Mathf.Abs(currentTimeMs - beatTime) <= hitRange)
        //    {
        //        lastCheckedIndex_OffBeatList = i + 1;
        //        OffBeat();
        //        return;
        //    }
        //    else
        //    {
        //        Debug.Log("엇박 실패!");
        //        BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
        //        return;
        //    }
        //}
    }

    private void OnBeat()
    {
        if (targetObject != null && targetObject.TryGetComponent(out PulseToBeat pulse))
        {
            pulse.Pulse();
        }

        BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
        Debug.Log("정박 성공!");
    }

    private void OffBeat()
    {
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
        Debug.Log("엇박 성공!");
    }

    //현재 어떤 상황인지 다른 곳으로 알려주기 위한 함수들
    public bool IsOnBeatNow()
    {
        float currTime = GetFMODTimelineSeconds();

        foreach (float beatTime in OnBeatList)
        {
            if (Mathf.Abs(currTime - beatTime) <= hitRange)
                return true;
            if (beatTime > currTime + hitRange)
                break;
        }

        return false;
    }
    public bool IsOffBeatNow()
    {
        float currTime = GetFMODTimelineSeconds();

        foreach (float beatTime in OffBeatList)
        {
            if (Mathf.Abs(currTime - beatTime) <= hitRange)
                return true;
            if (beatTime > currTime + hitRange)
                break;
        }

        return false;
    }

    // 타임라인 비트 콜백 - 박자 판정
    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
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
        musicInstance.setCallback(null);
    }
}
