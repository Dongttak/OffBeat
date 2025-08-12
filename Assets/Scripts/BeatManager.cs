//using FMOD.Studio;
//using FMODUnity;
//using System;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using UnityEngine;

//public class BeatManager : MonoBehaviour
//{
//    public static BeatManager Instance { get; private set; }

//    // FMOD 인스턴스
//    private EventInstance musicInstance;
//    private EventDescription musicDescription;

//    [Header("BPM Setting")]
//    [SerializeField] private float bpm = 120f;
//    [SerializeField] private float stepsPerBeat = 1f;
//    [SerializeField] private float hitRangePercentage = 0.25f;  // 판정 범위 (intervalDuration의 25%)

//    [Header("FMOD Setting")]
//    // FMOD 경로 설정
//    [SerializeField] private EventReference musicEventPath;
//    [SerializeField] private GameObject targetObject;

//    // FMOD 이벤트 콜백
//    private EVENT_CALLBACK beatCallback;

//    // 박자 간격 (ms)
//    private int intervalDurationMs;
//    // 판정 범위 (ms)
//    private int hitRangeMs;

//    // 정박 타이밍을 저장할 변수
//    private List<OnBeatJudgeZone> onBeatJudgeZones = new List<OnBeatJudgeZone>();

//    // 정박 판정을 위한 구조체
//    public struct OnBeatJudgeZone
//    {
//        public int startTimeMs;
//        public int endTimeMs;
//    }

//    // 엇박 타이밍을 저장할 변수
//    private List<OffBeatJudgeZone> offBeatJudgeZones = new List<OffBeatJudgeZone>();

//    // 엇박 판정을 위한 구조체
//    private struct OffBeatJudgeZone
//    {
//        private int startTimeMs;
//        private int endTimeMs;
//    }

//    public List<GameObject> pulseToBeatObjects = new List<GameObject>();

//    //private float songLength = 180f;
//    private bool isInitialized = false;

//    private void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else Destroy(gameObject);
//    }

//    void Start()
//    {
//        InitAndStartFMOD();
//    }

//    void InitAndStartFMOD()
//    {
//        // FMOD 인스턴스 생성
//        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
//        musicDescription = RuntimeManager.GetEventDescription(musicEventPath);

//        // 총 노래 길이 저장
//        int songLengthMs;
//        musicDescription.getLength(out songLengthMs);

//        // intervalDuration,hitRange ms 단위로 구하기
//        float intervalDuration_s = 60f / (bpm * stepsPerBeat);
//        float hitRange_s = intervalDuration_s * hitRangePercentage;
//        intervalDurationMs = Mathf.RoundToInt(intervalDuration_s * 1000f);
//        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

//        // 콜백 이벤트 등록
//        beatCallback = new EVENT_CALLBACK(TimelineCallback);

//        // BEAT 이벤트 콜백 타입 설정
//        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

//        for (int time = 0; time < songLengthMs; time += intervalDurationMs)
//        {
//            OnBeatJudgeZone newOnBeatZone = new OnBeatJudgeZone
//            {
//                startTimeMs = time - hitRangeMs,
//                endTimeMs = time + hitRangeMs,
//            };
//            onBeatJudgeZones.Add(newOnBeatZone);
//        }

//        Debug.Log($"총 적박 구간 개수: {onBeatJudgeZones.Count}개의 구간 존재");
//        Debug.Log($"총 노래 길이: {songLengthMs} ms");
//        Debug.Log($"intervalDuration: {intervalDurationMs} ms");

//        musicInstance.start();

//        isInitialized = true;
//    }

//    void Update()
//    {
//        if (!isInitialized) return;

//        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift))
//        {
//            int currentTimeMs = GetFMODTimelineSeconds();

//            CheckBeat(currentTimeMs);
//        }
//    }

//    private int GetFMODTimelineSeconds()
//    {
//        if (musicInstance.isValid())
//        {
//            musicInstance.getTimelinePosition(out int currentTimeMs);
//            return currentTimeMs;
//        }

//        return 0;
//    }

//    private void CheckBeat(float currentTimeMs)
//    {
//        foreach (var zone in onBeatJudgeZones)
//        {
//            if (currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
//            {
//                OnBeat();
//                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
//                Debug.Log($"입력된 시간 : {currentTimeMs}ms");
//                return; // 한 번 판정 후 종료
//            }
//            else
//            {
//                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
//                Debug.Log("박자 실패!");
//            }
//        }
//    }

//    private void OnBeat()
//    {
//        //if (targetObject != null && targetObject.TryGetComponent(out PulseToBeat pulse))
//        //{
//        //    pulse.Pulse();
//        //}

//        BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
//        Debug.Log("정박 성공!");
//    }

//    private void OffBeat()
//    {
//        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
//        Debug.Log("엇박 성공!");
//    }

//    // 비트 이벤트 콜백 등록 - 박자 판정
//    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
//    //static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
//    //{
//    //    if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
//    //    {

//    //        foreach (GameObject obj in _pulseToBeatObjects)
//    //        {
//    //            obj.GetComponent<PulseToBeat>().Pulse();
//    //        }
//    //    }
//    //    return FMOD.RESULT.OK;
//    //}

//    FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
//    {
//        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
//        {
//            foreach (GameObject obj in pulseToBeatObjects)
//            {
//                obj.GetComponent<PulseToBeat>().Pulse();
//            }
//        }
//        return FMOD.RESULT.OK;
//    }

//    private void OnDestroy()
//    {
//        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
//        musicInstance.release();
//        //musicInstance.setCallback(null);
//    }
//}
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System;
using System.Runtime.InteropServices;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;
    [SerializeField, Range(0f, 1f)] private float hitRangePercentage = 0.25f;

    [Header("FMOD Setting")]
    [SerializeField] private EventReference musicEventPath;

    private EventInstance musicInstance;
    private EventDescription musicDescription;

    private EVENT_CALLBACK beatCallback;

    private int intervalDurationMs;
    private int hitRangeMs;

    // 메인스레드에서 처리할 비트 신호
    private int _pendingBeatCount = 0;

    // 바로 GameObject 대신, 컴포넌트를 캐싱
    [SerializeField] private List<PulseToBeat> pulseTargets = new List<PulseToBeat>();

    private List<OnBeatJudgeZone> onBeatJudgeZones = new List<OnBeatJudgeZone>();
    public struct OnBeatJudgeZone { public int startTimeMs, endTimeMs; }

    private bool isInitialized = false;

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }

        // 비워져 있으면 자동 수집(선택)
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

        musicDescription.getLength(out int songLengthMs);

        float interval_s = 60f / (bpm * stepsPerBeat);
        float hitRange_s = interval_s * hitRangePercentage;
        intervalDurationMs = Mathf.RoundToInt(interval_s * 1000f);
        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

        // 콜백 등록(IL2CPP 대비 static 함수 사용)
        beatCallback = TimelineCallback;
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        // 판정 구간 만들기
        onBeatJudgeZones.Clear();
        for (int t = 0; t <= songLengthMs; t += intervalDurationMs)
            onBeatJudgeZones.Add(new OnBeatJudgeZone { startTimeMs = t - hitRangeMs, endTimeMs = t + hitRangeMs });

        musicInstance.start();
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // 콜백에서 누적된 비트 신호 처리(메인 스레드)
        if (_pendingBeatCount > 0)
        {
            // 여러 개가 한 프레임에 들어와도 한 번만 연출
            _pendingBeatCount = 0;
            foreach (var p in pulseTargets)
            {
                if (p != null) p.Pulse();
            }
        }

        // 입력 판정 예시
        if (Input.anyKeyDown)
        {
            int currentTimeMs = GetFMODTimelineMs();
            CheckBeat(currentTimeMs);
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

    private void CheckBeat(int currentTimeMs)
    {
        // 불필요한 반복 로그 제거, 구간 매치만 처리
        for (int i = 0; i < onBeatJudgeZones.Count; i++)
        {
            var z = onBeatJudgeZones[i];
            if (currentTimeMs < z.startTimeMs) break; // 아직 이르다 → 종료
            if (currentTimeMs <= z.endTimeMs)
            {
                // 정박 성공
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                return;
            }
        }
        BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
    }

    // === FMOD 오디오 스레드 콜백(여기서는 Unity API 절대 호출 금지) ===
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
            musicInstance.setCallback(null); // 콜백 해제
            musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); 
            musicInstance.release();
        }
    }
}
