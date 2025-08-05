using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    // FMOD �̺�Ʈ �ν��Ͻ�
    private EventInstance musicInstance;
    private EventDescription musicDescription;

    [Header("BPM Setting")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float stepsPerBeat = 1f;
    [SerializeField] private float hitRangePercentage = 0.25f;  // ���� ���� (intervalDuration�� 25%)

    [Header("FMOD Setting")]
    // �ν����Ϳ��� FMOD �̺�Ʈ ��� ����
    [SerializeField] private EventReference musicEventPath; 
    [SerializeField] private GameObject targetObject;

    // FMOD �ݹ��� ���� ��������Ʈ
    private EVENT_CALLBACK beatCallback;

    // ��Ʈ ���� (ms)
    private int intervalDurationMs;
    private float intervalDuration;     // ����
    // ���� ���� ���� (ms)
    private int hitRangeMs;
    private float hitRange;             // ����

    // ���� ���� ������ �̸� ������ ����Ʈ
    private List<OnBeatJudgeZone> onBeatJudgeZones = new List<OnBeatJudgeZone>();

    // ���� ���� ���� ����ü
    public struct OnBeatJudgeZone
    {
        public int startTimeMs;    // ���� ���� ���� �ð� (ms)
        public int endTimeMs;      // ���� ���� �� �ð� (ms)
    }

    // ���� ���� ������ �̸� ������ ����Ʈ
    private List<OffBeatJudgeZone> offBeatJudgeZones = new List<OffBeatJudgeZone>();

    // ���� ���� ���� ����ü
    private struct OffBeatJudgeZone
    {
        private int startTimeMs;    // ���� ���� ���� �ð� (ms)
        private int endTimeMs;      // ���� ���� �� �ð� (ms)
    }

    [SerializeField] private List<GameObject> pulseToBeatObjects = new List<GameObject>();
    //private static List<GameObject> _pulseToBeatObjects = new List<GameObject>();

    // --------------------- ���� �ڵ�
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
        // FMOD �̺�Ʈ �ν��Ͻ��� ��ũ���� ����
        musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        musicDescription = RuntimeManager.GetEventDescription(musicEventPath);

        // �̺�Ʈ ��ü ���� ��������
        int songLengthMs;
        musicDescription.getLength(out songLengthMs);

        // intervalDuration�� hitRange�� ms ������ ���
        float intervalDuration_s = 60f / (bpm * stepsPerBeat);
        float hitRange_s = intervalDuration_s * hitRangePercentage;
        intervalDurationMs = Mathf.RoundToInt(intervalDuration_s * 1000f);
        hitRangeMs = Mathf.RoundToInt(hitRange_s * 1000f);

        // �ݹ� ����
        beatCallback = new EVENT_CALLBACK(TimelineCallback);

        // BEAT Ÿ�� �ݹ�
        musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);

        // ��ü Ÿ�Ӷ��ο� ���� ���� ������ �̸� ����
        // TODO: start���� �ۼ��� �����̹Ƿ�, ���Ŀ� music�� �ٲ�� ��Ȳ�� ����� �ڵ� ���� �ʿ�
        for (int time = 0; time < songLengthMs; time += intervalDurationMs)
        {
            OnBeatJudgeZone newOnBeatZone = new OnBeatJudgeZone
            {
                startTimeMs = time - hitRangeMs,
                endTimeMs = time + hitRangeMs,
            };
            onBeatJudgeZones.Add(newOnBeatZone);
        }

        Debug.Log($"�ش� �뷡�� {onBeatJudgeZones.Count} ���� ���� ���� ������");
        Debug.Log($"�ش� �뷡�� ��ü ����: {songLengthMs} ms");
        Debug.Log($"intervalDuration: {intervalDurationMs} ms");

        // ����Ʈ �ʱ�ȭ
        //_pulseToBeatObjects.Clear(); // Ȥ�� �� �����͸� �ʱ�ȭ
        //_pulseToBeatObjects.AddRange(pulseToBeatObjects);

        // ���� ���
        musicInstance.start();

        ///// ----------------------- ���� �ڵ�
        //intervalDuration = 60f / (bpm * stepsPerBeat);
        //hitRange = intervalDuration * 0.25f;

        // ��Ʈ ����Ʈ ����
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

            Debug.Log($"���� FMOD ��� ���� (��): {currentTimeMs:F3}");
            CheckBeat(currentTimeMs);
        }
    }

    private int GetFMODTimelineSeconds()
    {
        if (musicInstance.isValid())
        {
            // ���� ���� ������ ���� Ÿ�Ӷ��� �ð��� �ҷ���
            // (Ms) -> audiosource ���� time ���� �ÿ��� �Է� �� unity scene ��� �ð��� �Է¹����� �ش� ������ Ÿ�Ӷ��� �ð��� �������Ƿ� loop�� ���� ����
            musicInstance.getTimelinePosition(out int currentTimeMs);
            return currentTimeMs;
        }

        return 0;
    }

    private void CheckBeat(float currentTimeMs)
    {
        // �̸� ������ ���� ���� ���� ����Ʈ�� ��ȸ�ϸ� ���� �ð��� �ش� ������ ���ϴ��� Ȯ��
        foreach (var zone in onBeatJudgeZones)
        {
            if (currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
            {
                // ���� ����
                Debug.Log($"���� ����! �Է� �ð�: {currentTimeMs}ms");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
                return; // �� �� ���� �� ����
            }
            else
            {
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
            }
        }

        // ���� ����
        Debug.Log($"���� ����! �Է� �ð�: {currentTimeMs}ms");

        ///// ------------------------- ����

        // ���� ����
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
        //        Debug.Log("���� ����!");
        //        BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
        //        continue;
        //        //return;
        //    }
        //}

        //// ���� ����
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
        //        Debug.Log("���� ����!");
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
        Debug.Log("���� ����!");
    }

    private void OffBeat()
    {
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
        Debug.Log("���� ����!");
    }

    //���� � ��Ȳ���� �ٸ� ������ �˷��ֱ� ���� �Լ���
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

    // Ÿ�Ӷ��� ��Ʈ �ݹ� - ���� ����
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
