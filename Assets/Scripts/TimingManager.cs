using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimingManager : MonoBehaviour
{
    public static TimingManager instance;

    public AudioSource audioSource;
    public float bpm = 120f;

    private double beatInterval;
    private double dspStartTime;

    private double currentDSPTime;
    private double elapsed;
    private int nearestBeat;
    [SerializeField]
    private double nearestBeatTime;
    [SerializeField]
    private double timeDiff;

    public float hitRange = 0.04f; // 허용 오차

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(instance);
            instance = this;
            DontDestroyOnLoad(instance);
        }
    }
    void Start()
    {
        beatInterval = 60.0 / bpm;
        dspStartTime = AudioSettings.dspTime + 0.2f;
    }
    void Update()
    {
        currentDSPTime = AudioSettings.dspTime;
        elapsed = currentDSPTime - dspStartTime;

        nearestBeat = Mathf.RoundToInt((float)(elapsed / beatInterval));
        nearestBeatTime = dspStartTime + nearestBeat * beatInterval;

        timeDiff = Mathf.Abs((float)(currentDSPTime - nearestBeatTime));
    }

    public bool IsOntheBeat()
    {
        if (timeDiff <= hitRange) return true;
        else {
            Debug.Log(timeDiff);
            return false; 
        }
    }
}
