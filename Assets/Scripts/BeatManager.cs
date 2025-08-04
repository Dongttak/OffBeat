using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System;

public class BeatManager : MonoBehaviour
{
    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float steps = 1f;

    [Header("FMOD Setting")]
    [SerializeField] private EventReference musicEvent; 
    [SerializeField] private GameObject targetObject;   

    private float intervalDuration;
    private float hitRange;

    private EventInstance instance;
    private List<float> OnBeatList = new List<float>();
    private List<float> OffBeatList = new List<float>();
    private int lastCheckedIndex_OnBeatList = 0;
    private int lastCheckedIndex_OffBeatList = 0;

    private float songLength = 180f;
    private bool isInitialized = false;

    public static BeatManager Instance { get; private set; }

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
        intervalDuration = 60f / (bpm * steps);
        hitRange = intervalDuration * 0.30f;

        // 비트 리스트 생성
        OnBeatList.Clear();
        OffBeatList.Clear();
        lastCheckedIndex_OnBeatList = 0;
        lastCheckedIndex_OffBeatList = 0;

        for (float t = 0f; t <= songLength; t += intervalDuration)
        {
            OnBeatList.Add(t);
        }

        for (float t = intervalDuration / 2f; t <= songLength; t += intervalDuration)
        {
            OffBeatList.Add(t);
        }

        instance = RuntimeManager.CreateInstance(musicEvent);
        instance.start();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            float currentTime = GetFMODTimelineSeconds();

            Debug.Log($"현재 FMOD 재생 시점 (초): {currentTime:F3}");
            CheckBeat(currentTime);
        }
    }

    private float GetFMODTimelineSeconds()
    {
        if (instance.isValid())
        {
            instance.getTimelinePosition(out int positionMs);
            return positionMs / 1000f;
        }

        return 0f;
    }

    /*private void CheckBeat(float currTime)
    {
        // 정박 판정
        for (int i = lastCheckedIndex_OnBeatList; i < OnBeatList.Count; i++)
        {
            float beatTime = OnBeatList[i];

            if (beatTime < currTime - hitRange)
            {
                lastCheckedIndex_OnBeatList = i + 1;
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                continue;
            }

            if (Mathf.Abs(currTime - beatTime) <= hitRange)
            {
                lastCheckedIndex_OnBeatList = i + 1;
                OnBeat();
                return;
            }
            else
            {
                Debug.Log("정박 실패!");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                continue;
                //return;
            }
        }

        // 엇박 판정
        for (int i = lastCheckedIndex_OffBeatList; i < OffBeatList.Count; i++)
        {
            float beatTime = OffBeatList[i];

            if (beatTime < currTime - hitRange)
            {
                lastCheckedIndex_OffBeatList = i + 1;
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                continue;
            }

            if (Mathf.Abs(currTime - beatTime) <= hitRange)
            {
                lastCheckedIndex_OffBeatList = i + 1;
                OffBeat();
                return;
            }
            else
            {
                Debug.Log("엇박 실패!");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                return;
            }
        }
    }
    */
    private void CheckBeat(float currTime)
    {
        // 정박 판정
        foreach (float beatTime in OnBeatList)
        {
            if (Mathf.Abs(currTime - beatTime) <= hitRange)
            {
                OnBeat();
                return; 
            }
        }

        // 엇박 판정
        foreach (float beatTime in OffBeatList)
        {
            if (Mathf.Abs(currTime - beatTime) <= hitRange)
            {
                OffBeat();
                return;
            }
        }

        // 둘 다 실패
        Debug.Log("정박/엇박 실패!");
        if (BeatState.Instance.CurrBeatState != BeatState.BeatType.OnBeat &&
            BeatState.Instance.CurrBeatState != BeatState.BeatType.OffBeat)
        {
            BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
        }
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

}
