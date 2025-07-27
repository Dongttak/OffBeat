using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BeatManager : MonoBehaviour
{
    [SerializeField] private float bpm;    // �д� Beat ��
    [SerializeField] private float steps;  // �� ���ڸ� ��� �ɰ��� ��Ÿ���� �� _steps = 2f -> 1/2 ����, _steps = 4f -> 1/4 ���� ���
                                           // TODO : �뷡 ��� �߰��� ���� ���� �ʿ� �� steps ���� �޼ҵ� �߰�
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
                break; // ���� ������ ���������� �ߴ�
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
        
        // ���� list log
        for (int i = 0; i < OnBeatList.Count; i++)
        {
            Debug.Log("���� list : " + OnBeatList[i]);
        }

        // ���� list log
        for (int i = 0; i < OffBeatList.Count; i++)
        {
            Debug.Log("���� list : " + OffBeatList[i]);
        }
    }

    private void Update()
    {
        // test
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            float currTime = audioSource.timeSamples / (float)audioSource.clip.frequency;
            //float currTime = _audioSource.time;   // ���Ŀ� .time�� ����Ͽ��� �� audiosource�� ����ȭ ������ �߻����� �ʴ��� Ȯ���� ��

            Debug.Log(currTime);
            CheckBeat(currTime);
        }
    }

    public void InitOnStartNewMusic(AudioSource _audioSource)    // �뷡�� �ٲ� �� bpm, steps, ���� list�� �ʱ�ȭ
    {
        // TODO : �ش� �뷡�� �´� bpm ���� �޼ҵ� �߰�
        intervalDuration = 60f / (bpm * steps);
        Debug.Log("intervalDuration : " + intervalDuration + " ");

        hitRange = intervalDuration * 0.25f;    // ���� ������ ������ ���� %�� ����
        Debug.Log("hitRange : " + hitRange);

        // �� ���� list �ʱ�ȭ
        OnBeatList.Clear();
        lastCheckedIndex_OnBeatList = 0;
        OffBeatList.Clear();
        lastCheckedIndex_OffBeatList = 0;

        float length = _audioSource.clip.length;
        //Debug.Log(length + "\n");

        // ���� �� ���� list�� �ʱ�ȭ
        for (float i = 0f; i <= length; i += intervalDuration)
        {
            OnBeatList.Add(i);
        }
        //Debug.Log(timingList.Count);

        // ���� �� ���� list�� �ʱ�ȭ
        for (float i = intervalDuration / 2.0f; i <= length; i += intervalDuration)
        {
            OffBeatList.Add(i);
        }
    }

    private void CheckBeat(float currTime)
    {
        // ���� Ȯ��
        for (int i = lastCheckedIndex_OnBeatList; i < OnBeatList.Count; i++)
        {
            float currBeat = OnBeatList[i];

            // ������ ���� ó��
            if(currBeat < currTime - hitRange)
            {
                lastCheckedIndex_OnBeatList = i + 1;
                continue;
            }

            // ���� ����
            if (Mathf.Abs(currTime - currBeat) <= hitRange)
            {
                //obj.CheckHit(currTime, bpm);
                lastCheckedIndex_OnBeatList = i + 1;
                OnBeat();
                break;
            }
            else    // ���� ����
            {
                Debug.Log("���� ����!");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                break;
            }
        }

        // ���� Ȯ��
        for (int i = lastCheckedIndex_OffBeatList; i < OffBeatList.Count; i++)
        {
            float currBeat = OffBeatList[i];

            // ������ ���� ó��
            if (currBeat < currTime - hitRange)
            {
                lastCheckedIndex_OffBeatList = i + 1;
                continue;
            }

            // ���� ����
            if (Mathf.Abs(currTime - currBeat) <= hitRange)
            {
                //obj.CheckHit(currTime, bpm);
                lastCheckedIndex_OffBeatList = i + 1;
                OffBeat();
                break;
            }
            else    // ���� ����
            {
                Debug.Log("���� ����!");
                BeatState.Instance.CurrBeatState = BeatState.BeatType.Miss;
                break;
            }
        }
    }

    private void OnBeat()
    {
        obj.GetComponent<PulseToBeat>().Pulse();
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OnBeat;
        Debug.Log("���� ����!!");
    }

    private void OffBeat()
    {
        // TODO : counter UI �ۼ� �� ����/���� �߰�
        BeatState.Instance.CurrBeatState = BeatState.BeatType.OffBeat;
        Debug.Log("���� ����!!");
    }
}

public class BeatState : MonoBehaviour
{
    public static BeatState Instance { get; } = new BeatState();

    private BeatState() { }

    public enum BeatType
    {
        OnBeat,     // ����
        OffBeat,    // ����
        Miss        // ���� ��ħ
    }

    private BeatType _currBeatState;

    public BeatType CurrBeatState
    {
        get { return _currBeatState; }
        set { _currBeatState = value; }
    }

}

