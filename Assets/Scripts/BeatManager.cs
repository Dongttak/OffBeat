using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    // FMOD �ν��Ͻ�
    private EventInstance musicInstance;
    private EventDescription musicDescription;

    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;
    [SerializeField] private float hitRangePercentage = 0.25f;  // ���� ���� (intervalDuration�� 25%)

    [Header("FMOD Setting")]
    // FMOD ��� ����
    [SerializeField] private EventReference musicEventPath;
    [SerializeField] private GameObject targetObject;

    // FMOD �̺�Ʈ �ݹ�
    private EVENT_CALLBACK beatCallback;

    // ���� ���� (ms)
    private int intervalDurationMs;
    // ���� ���� (ms)
    private int hitRangeMs;

    // ���� Ÿ�̹��� ������ ����
    private List<OnBeatJudgeZone> onBeatJudgeZones = new List<OnBeatJudgeZone>();

    // ���� ������ ���� ����ü
    public struct OnBeatJudgeZone
    {
        public int startTimeMs;
        public int endTimeMs;
    }

    // ���� Ÿ�̹��� ������ ����
    private List<OffBeatJudgeZone> offBeatJudgeZones = new List<OffBeatJudgeZone>();

    // ���� ������ ���� ����ü
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
        // FMOD �ν��Ͻ� ����
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicDescription = RuntimeManager.GetEventDescription(musicEventPath);

        // �� �뷡 ���� ����
        int songLengthMs;
        musicDescription.getLength(out songLengthMs);

        // intervalDuration,hitRange ms ������ ���ϱ�
        float intervalDuration_s = 60f / (bpm * stepsPerBeat);
        float hitRange_s = intervalDuration_s * hitRangePercentage;
        intervalDurationMs = Mathf.RoundToInt(intervalDuration_s * 1000f);
        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

        // �ݹ� �̺�Ʈ ���
        beatCallback = new EVENT_CALLBACK(TimelineCallback);

        // BEAT �̺�Ʈ �ݹ� Ÿ�� ����
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

        Debug.Log($"�� ���� ���� ����: {onBeatJudgeZones.Count}���� ���� ����");
        Debug.Log($"�� �뷡 ����: {songLengthMs} ms");
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
                Debug.Log($"�Էµ� �ð� : {currentTimeMs}ms");
                return; // �� �� ���� �� ����
            }
            else
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                Debug.Log("���� ����!");
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
        Debug.Log("���� ����!");
    }

    private void OffBeat()
    {
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
        Debug.Log("���� ����!");
    }

    // ��Ʈ �̺�Ʈ �ݹ� ��� - ���� ����
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