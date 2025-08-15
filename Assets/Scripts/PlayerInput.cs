using System;                  // 이벤트용
using DG.Tweening;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static event Action AttackEvent;   // 추가

    [Header("Move Lanes")]
    [SerializeField] private Transform[] positions;   // [0]=왼, [1]=중앙, [2]=오
    [SerializeField] private float moveDuration = 0.2f;

    [Header("Refs")]
    [SerializeField] private CounterManager counterManager; // 비워두면 자동 탐색

    private int posIndex = 1;

    // 같은 박자 창에서 1회만 허용(정박/엇박 래치 분리)
    private int _lastOnId = int.MinValue;
    private int _lastOffId = int.MinValue;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
    }

    void Update()
    {
        if (BeatManager.Instance == null) return;

        bool isOn = BeatManager.Instance.IsOnBeatNow();
        bool isOff = BeatManager.Instance.IsOffBeatNow();

        int windowId = isOn ? GetOnId()
                            : isOff ? GetOffId() : int.MinValue;

        // ===== 정박 전용: 이동/공격 =====
        if (isOn && windowId != _lastOnId)
        {
            if (Input.GetKeyDown(KeyCode.A) && posIndex > 0)
            {
                ConsumeOn(windowId);
                MoveTo(posIndex - 1);
            }
            else if (Input.GetKeyDown(KeyCode.D) && posIndex < 2)
            {
                ConsumeOn(windowId);
                MoveTo(posIndex + 1);
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                ConsumeOn(windowId);
                AttackEvent?.Invoke();                 
                Debug.Log("[PlayerInput] Attack (OnBeat) event fired");
            }
        }

        // ===== 엇박 전용: 카운터 =====
        if (isOff && windowId != _lastOffId)
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                _lastOffId = windowId;
                bool ok = counterManager && counterManager.TryCounter();
                Debug.Log(ok ? "[Counter] Try -> SUCCESS" : "[Counter] Try -> FAIL");
            }
        }
    }

    // 정박/엇박 ID (현재 타임라인과 interval로 계산)
    int GetOnId()
    {
        int interval = BeatManager.Instance.IntervalMs;
        int now = BeatManager.Instance.GetTimelineMs();
        return Mathf.RoundToInt(now / (float)interval) * 2; // 정박 = 짝수
    }
    int GetOffId()
    {
        int interval = BeatManager.Instance.IntervalMs;
        int now = BeatManager.Instance.GetTimelineMs();
        int half = interval / 2;
        return (Mathf.RoundToInt((now - half) / (float)interval) * 2) + 1; // 엇박 = 홀수
    }

    void ConsumeOn(int onId) => _lastOnId = onId;

    void MoveTo(int nextIndex)
    {
        transform.DOKill(); // 이전 트윈 끊기(씹힘 방지)
        posIndex = nextIndex;
        transform.DOMove(positions[posIndex].position, moveDuration)
                 .SetEase(Ease.InOutQuad);
    }
}
