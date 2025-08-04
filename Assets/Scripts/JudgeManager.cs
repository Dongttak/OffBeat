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

    // FMOD �̺�Ʈ �ν��Ͻ�
    private EventInstance musicInstance;
    private EventDescription musicDescription;

    // �ν����� - FMOD �̺�Ʈ ��� ����
    [SerializeField] private EventReference musicEventPath;

    // FMOD �ݹ��� ���� ��������Ʈ
    private EVENT_CALLBACK beatCallback;

    //// ������ �� ��Ʈ�� ��Ȯ�� �ð� (ms)
    //private int nextBeatTimeMs;
    // ��Ʈ ���� (ms)
    private int intervalDurationMs;
    // ���� ��Ʈ�� ���� ���� (ms)
    private int hitRangeMs;

    [Header("Rhythm Settings")]
    public float bpm = 120f;
    public float stepsPerBeat = 1;
    public float hitRangePercentage = 0.25f;    // ���� ���� (intervalDuration�� 25%)

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

    // ������ ������ ��Ʈ�� ���� �ð��� ������ ť
    private Queue<int> noteSpawnQueue = new Queue<int>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
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
        //beatCallback = new EVENT_CALLBACK(TimelineCallback);
        // BEAT Ÿ�� �ݹ�
        //musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
        // MARKER Ÿ�� �ݹ�
        //musicInstance.setCallback(beatCallback, EVENT_CALLBACK_TYPE.TIMELINE_MARKER);

        // ���� ��Ʈ �ð��� �ʱ�ȭ. ù ��Ʈ�� �����ϴ� 0ms�� �������� ����               -------- �ݹ� �����ϸ� ��� ����
        //nextBeatTimeMs = 0;

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

        // ���� ���
        musicInstance.start();
    }

    private void Update()
    {
        int currentTimeMs;
        musicInstance.getTimelinePosition(out currentTimeMs);       // ���� ���� ������ ���� Ÿ�Ӷ��� �ð��� �ҷ��� (Ms) -> audiosource ���� time ���� �ÿ��� �Է� �� unity scene ��� �ð��� �Է¹����� �ش� ������ Ÿ�Ӷ��� �ð��� �������Ƿ� loop�� ���� ����

        // �����̽��ٷ� ���� ����
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckHit(currentTimeMs);
        }
    }


    // ���� �ð��� ���� ���ڸ� �����ϴ� �Լ�
    public void CheckHit(int currentTimeMs)
    {
        // �̸� ������ ���� ���� ���� ����Ʈ�� ��ȸ�ϸ� ���� �ð��� �ش� ������ ���ϴ��� Ȯ��
        foreach (var zone in onBeatJudgeZones)
        {
            if (currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
            {
                // ���� ����
                Debug.Log($"���� ����! �Է� �ð�: {currentTimeMs}ms");
                return; // �� �� ���� �� ����
            }
        }

        // ���� ����
        Debug.Log($"���� ����! �Է� �ð�: {currentTimeMs}ms");
    }

    //private void CheckHit(int inputTimeMs)
    //{
    //    // ���� ��Ʈ ������ ������ �������� ���� (���� �������̳� ��)
    //    if (nextBeatTimeMs <= 0)
    //    {
    //        Debug.Log("������ ��Ʈ�� ����");
    //        return;
    //    }

    //    // �Է� �ð��� ���� ��Ʈ �ð��� ���Ͽ� ���� ���
    //    int timeDiff = Mathf.Abs(inputTimeMs - nextBeatTimeMs);

    //    // ������ ���� ���� ���� �ִ��� Ȯ��
    //    if(timeDiff <= hitRangeMs)
    //    {
    //        // ���� ����
    //        Debug.Log($"���� ����! �Է� �ð� : {inputTimeMs}ms, ���� ���� : {nextBeatTimeMs}ms, ���� : {timeDiff}ms");

    //        // �ߺ� ���� ���� - ������ ���ߴ� ���� ����, ������ ����
    //        //nextBeatTimeMs = 0;
    //    }
    //    else
    //    {
    //        // ���� ����
    //        Debug.Log($"���� ����! �Է� �ð� : {inputTimeMs}ms, ���� ���� : {nextBeatTimeMs}ms, ���� : {timeDiff}ms");
    //    }

    //// �̸� ������ ���� ���� ���� ����Ʈ�� ��ȸ�ϸ� ���� �ð��� �ش� ������ ���ϴ��� Ȯ��
    //foreach (var zone in onBeatJudgeZones)
    //{
    //    if(currentTimeMs >= zone.startTimeMs && currentTimeMs <= zone.endTimeMs)
    //    {
    //        // ���� ����
    //        Debug.Log("���� ����!");
    //        return; // �� �� ���� �� ����
    //    }
    //}

    //// ���� ����
    //Debug.Log("���� ����!");
    //}

    // Ÿ�Ӷ��� ��Ʈ �ݹ� - ���� ����
    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    //static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    //{
    //    if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT)
    //    {
    //        var properties = (TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_BEAT_PROPERTIES));

    //        // FMOD���� ������ ���� bpm�� ����Ͽ� ��Ʈ ���� ���� ���
    //        float currentTempo = properties.tempo;
    //        // 60000ms / (���� * stepsPerBeat)�� ���� ���
    //        instance.intervalDurationMs = (int)((60000f / currentTempo) / instance.stepsPerBeat);

    //        // �������� ���� ������ ������� ���� ��Ʈ�� ���� ���� ���
    //        instance.hitRangeMs = Mathf.RoundToInt(instance.intervalDurationMs * instance.hitRangePercentage);

    //        // ���� ��Ʈ�� ��ġ�� intervalDurationMs�� ���� ���� ��Ʈ�� �ð��� ����Ͽ� ����
    //        instance.nextBeatTimeMs = properties.position + Mathf.RoundToInt(instance.intervalDurationMs);
    //        //instance.nextBeatTimeMs = properties.position;

    //    }
    //    return FMOD.RESULT.OK;
    //}

    // Ÿ�Ӷ��� ��Ŀ ��ġ �ݹ����� ����
    //[AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    //static FMOD.RESULT TimelineCallback(EVENT_CALLBACK_TYPE type, System.IntPtr instancePtr, System.IntPtr parameterPtr)
    //{
    //    if (type == EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
    //    {
    //        var properties = (TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(TIMELINE_MARKER_PROPERTIES));

    //        instance.nextBeatTimeMs = properties.position;
    //        string markerName = properties.name.ToString();

    //       Debug.Log($"Marker Name : {markerName}, Position : {properties.position}ms");

    //        // ��� ���� ���� ����
    //    }
    //    return FMOD.RESULT.OK;
    //}

    private void OnDestroy()
    {
        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
