using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.UIElements;
using System.Runtime.InteropServices;

public class JudgeManager : MonoBehaviour
{
    private static JudgeManager instance;

    // FMOD 이벤트 인스턴스
    private EventInstance musicInstance;
    private EventDescription musicDescription;

    // 인스펙터 - FMOD 이벤트 경로 설정
    [SerializeField] private EventReference musicEventPath;

    // FMOD 콜백을 위한 델리게이트
    private EVENT_CALLBACK beatCallback;

    //// 다음에 올 비트의 정확한 시간 (ms)
    //private int nextBeatTimeMs;
    // 비트 간격 (ms)
    private int intervalDurationMs;
    // 다음 비트의 판정 범위 (ms)
    private int hitRangeMs;

    [Header("Rhythm Settings")]
    public float bpm = 120f;
    public float stepsPerBeat = 1;
    public float hitRangePercentage = 0.25f;    // 판정 범위 (intervalDuration의 25%)

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

    // 앞으로 생성될 노트의 도착 시간을 저장할 큐
    private Queue<int> noteSpawnQueue = new Queue<int>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
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
        //beatCallback = new EVENT_CALLBACK(TimelineCallback);
        // BEAT 타입 콜백
        //musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
        // MARKER 타입 콜백
        //musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        // 다음 비트 시간을 초기화. 첫 비트가 시작하는 0ms를 기준으로 설정               -------- 콜백 사용안하면 사용 안함
        //nextBeatTimeMs = 0;

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

        // 음악 재생
        musicInstance.start();
    }

    private void Update()
    {
        int currentTimeMs;
        musicInstance.getTimelinePosition(out currentTimeMs);       // 실행 중인 음악의 현재 타임라인 시간을 불러옴 (Ms) -> audiosource 에서 time 접근 시에는 입력 시 unity scene 재생 시간을 입력받지만 해당 구문은 타임라인 시간을 가져오므로 loop에 대응 가능

        // 스페이스바로 판정 시작
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckHit(currentTimeMs);
        }
    }


    // 현재 시간에 대해 박자를 판정하는 함수
    public void CheckHit(int currentTimeMs)
    {
        // 미리 생성한 정박 판정 구간 리스트를 순회하며 현재 시간이 해당 구간에 속하는지 확인
        foreach (var zone in onBeatJudgeZones)
        {
            if (currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
            {
                // 판정 성공
                Debug.Log($"정박 성공! 입력 시간: {currentTimeMs}ms");
                return; // 한 번 판정 후 종료
            }
        }

        // 판정 실패
        Debug.Log($"정박 실패! 입력 시간: {currentTimeMs}ms");
    }

    //private void CheckHit(int inputTimeMs)
    //{
    //    // 다음 비트 정보가 없으면 판정하지 않음 (음악 시작전이나 끝)
    //    if (nextBeatTimeMs <= 0)
    //    {
    //        Debug.Log("판정할 비트가 없음");
    //        return;
    //    }

    //    // 입력 시간과 다음 비트 시간을 비교하여 오차 계산
    //    int timeDiff = Mathf.Abs(inputTimeMs - nextBeatTimeMs);

    //    // 오차가 판정 범위 내에 있는지 확인
    //    if(timeDiff <= hitRangeMs)
    //    {
    //        // 판정 성공
    //        Debug.Log($"정박 성공! 입력 시간 : {inputTimeMs}ms, 판정 기준 : {nextBeatTimeMs}ms, 오차 : {timeDiff}ms");

    //        // 중복 판정 방지 - 로직이 멈추는 문제 있음, 제거할 수도
    //        //nextBeatTimeMs = 0;
    //    }
    //    else
    //    {
    //        // 판정 실패
    //        Debug.Log($"정박 실패! 입력 시간 : {inputTimeMs}ms, 판정 기준 : {nextBeatTimeMs}ms, 오차 : {timeDiff}ms");
    //    }

    //// 미리 생성한 정박 판정 구간 리스트를 순회하며 현재 시간이 해당 구간에 속하는지 확인
    //foreach (var zone in onBeatJudgeZones)
    //{
    //    if(currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
    //    {
    //        // 판정 성공
    //        Debug.Log("정박 성공!");
    //        return; // 한 번 판정 후 종료
    //    }
    //}

    //// 판정 실패
    //Debug.Log("정박 실패!");
    //}

    // 타임라인 비트 콜백 - 박자 판정
    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    //static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    //{
    //    if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
    //    {
    //        var properties = (TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));

    //        // FMOD에서 가져온 현재 bpm을 사용하여 비트 간격 동적 계산
    //        float currentTempo = properties.tempo;
    //        // 60000ms / (템포 * stepsPerBeat)로 간격 계산
    //        instance.intervalDurationMs = (int)((60000f / currentTempo) / instance.stepsPerBeat);

    //        // 동적으로 계산된 간격을 기반으로 다음 비트의 판정 범위 계산
    //        instance.hitRangeMs = Mathf.RoundToInt(instance.intervalDurationMs * instance.hitRangePercentage);

    //        // 현재 비트의 위치에 intervalDurationMs를 더해 다음 비트의 시간을 계산하여 저장
    //        instance.nextBeatTimeMs = properties.position + Mathf.RoundToInt(instance.intervalDurationMs);
    //        //instance.nextBeatTimeMs = properties.position;

    //    }
    //    return FMOD.RESULT.OK;
    //}

    // 타임라인 마커 위치 콜백으로 판정
    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    //static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    //{
    //    if (type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
    //    {
    //        var properties = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));

    //        instance.nextBeatTimeMs = properties.position;
    //        string markerName = properties.name.ToString();

    //       Debug.Log($"Marker Name : {markerName}, Position : {properties.position}ms");

    //        // 모든 판정 범위 동일
    //    }
    //    return FMOD.RESULT.OK;
    //}

    private void OnDestroy()
    {
        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
