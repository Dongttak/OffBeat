using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BeatManager : MonoBehaviour
{
    [SerializeField] private float bpm;    // 분당 Beat 수
    [SerializeField] private float steps;  // 한 박자를 어떻게 쪼갤지 나타내는 값 _steps = 2f -> 1/2 박자, _steps = 4f -> 1/4 박자 등등
                                           // TODO : 노래 재생 중간에 박자 변경 필요 시 steps 수정 메소드 추가
    //[SerializeField] private UnityEvent _onHit;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject obj;

    private float intervalDuration;
    private float hitRange; 

    private List<float> OnBeatList = new List<float>();
    private int index_OnBeatList = 0;
    private int lastCheckedIndex_OnBeatList;

    [HideInInspector] public List<float> OffBeatList = new List<float>();
    private int index_OffBeatList = 0;
    private int lastCheckedIndex_OffBeatList;

    public static BeatManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public bool IsOnBeatNow()
    {
        float currTime = audioSource.time;

        foreach (float beatTime in OnBeatList)
        {
            if (Mathf.Abs(currTime - beatTime) <= hitRange)
                return true;
            if (beatTime > currTime + hitRange)
                break; // 박자 범위를 지나쳤으면 중단
        }

        return false;
    }

    public bool IsOffBeatNow()
    {
        float currTime = audioSource.time;

        foreach (float beatTime in OffBeatList)
        {
            if (Mathf.Abs(currTime - beatTime) <= hitRange)
                return true;
            if (beatTime > currTime + hitRange)
                break;
        }

        return false;
    }



    private void Start()
    {
        InitOnStartNewMusic(audioSource);
        
        // 정박 list log
        for (int i = 0; i < OnBeatList.Count; i++)
        {
            Debug.Log("정박 list : " + OnBeatList[i]);
        }

        // 엇박 list log
        for (int i = 0; i < OffBeatList.Count; i++)
        {
            Debug.Log("엇박 list : " + OffBeatList[i]);
        }
    }

    private void Update()
    {
        // test
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            float currTime = audioSource.timeSamples / (float)audioSource.clip.frequency;
            //float currTime = _audioSource.time;   // 이후에 .time을 사용하였을 때 audiosource와 동기화 문제가 발생하지 않는지 확인할 것

            Debug.Log(currTime);
            CheckBeat(currTime);
        }
    }

    public void InitOnStartNewMusic(AudioSource _audioSource)    // 노래가 바뀔 때 bpm, steps, 박자 list를 초기화
    {
        // TODO : 해당 노래에 맞는 bpm 수정 메소드 추가
        intervalDuration = 60f / (bpm * steps);
        Debug.Log("intervalDuration : " + intervalDuration + " ");

        hitRange = intervalDuration * 0.25f;    // 판정 범위는 간격의 일정 %로 설정
        Debug.Log("hitRange : " + hitRange);

        // 각 박자 list 초기화
        OnBeatList.Clear();
        lastCheckedIndex_OnBeatList = 0;
        OffBeatList.Clear();
        lastCheckedIndex_OffBeatList = 0;

        float length = _audioSource.clip.length;
        //Debug.Log(length + "\n");

        // 정박 각 구간 list에 초기화
        for (float i = 0f; i <= length; i += intervalDuration)
        {
            OnBeatList.Add(i);
        }
        //Debug.Log(timingList.Count);

        // 엇박 각 구간 list에 초기화
        for (float i = intervalDuration / 2.0f; i <= length; i += intervalDuration)
        {
            OffBeatList.Add(i);
        }
    }

    private void CheckBeat(float currTime)
    {
        // 정박 확인
        for (int i = lastCheckedIndex_OnBeatList; i < OnBeatList.Count; i++)
        {
            float currBeat = OnBeatList[i];

            // 지나간 박자 처리
            if(currBeat < currTime - hitRange)
            {
                lastCheckedIndex_OnBeatList = i + 1;
                continue;
            }

            // 판정 성공
            if (Mathf.Abs(currTime - currBeat) <= hitRange)
            {
                //obj.CheckHit(currTime, bpm);
                lastCheckedIndex_OnBeatList = i + 1;
                OnBeat();
                break;
            }
            else    // 판정 실패
            {
                Debug.Log("정박 실패!");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                break;
            }
        }

        // 엇박 확인
        for (int i = lastCheckedIndex_OffBeatList; i < OffBeatList.Count; i++)
        {
            float currBeat = OffBeatList[i];

            // 지나간 박자 처리
            if (currBeat < currTime - hitRange)
            {
                lastCheckedIndex_OffBeatList = i + 1;
                continue;
            }

            // 판정 성공
            if (Mathf.Abs(currTime - currBeat) <= hitRange)
            {
                //obj.CheckHit(currTime, bpm);
                lastCheckedIndex_OffBeatList = i + 1;
                OffBeat();
                break;
            }
            else    // 판정 실패
            {
                Debug.Log("엇박 실패!");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                break;
            }
        }
    }

    private void OnBeat()
    {
        obj.GetComponent<PulseToBeat>().Pulse();
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
        Debug.Log("정박 성공!!");
    }

    private void OffBeat()
    {
        // TODO : counter UI 작성 및 성공/실패 추가
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
        Debug.Log("엇박 성공!!");
    }
}

public class BeatState : MonoBehaviour
{
    public static BeatState Instance { get; } = new BeatState();

    private BeatState() { }

    public enum BeatType
    {
        OnBeat,     // 정박
        OffBeat,    // 엇박
        Miss        // 박자 놓침
    }

    private BeatType _currBeatState;

    public BeatType CurrBeatState
    {
        get { return _currBeatState; }
        set { _currBeatState = value; }
    }

}

