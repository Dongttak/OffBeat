using UnityEngine;

[AddComponentMenu("Offbeat/Pattern2/CarPattern")]
public class CarPattern : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("박자마다 전진할 거리(미터)")]
    public float stepDistance = 3f;

    [Tooltip("시작 전에 대기할 박자 수")]
    public int leadBeats = 0;

    [Tooltip("전진할 총 스텝 수 (-1은 무제한)")]
    public int stepsMax = -1;

    [Header("Lifecycle")]
    [Tooltip("활성화(OnEnable)될 때 자동 시작(토글/풀링 대비)")]
    public bool autoStartOnEnable = true;

    [Tooltip("풀링 사용 시 Destroy 대신 반납")]
    public bool usePooling = true;

    [Tooltip("풀 미사용 시 끝에서 Destroy")]
    public bool destroyAtEnd = false;

    [Header("Direction")]
    [Tooltip("월드 Z+로 전진(프리팹 forward가 틀릴 때 ON)")]
    public bool useWorldZForward = false;

    [Tooltip("진행 방향 반전(필요 시)")]
    public bool invertDirection = false;    // ★ 누락돼서 CS0103 났던 필드

    // 내부 상태
    private bool running;
    private int stepsDone;
    private int leadLeft;

    void OnEnable()
    {
        // BeatManager는 고정: static event로 구독
        BeatManager.OnBeat += HandleBeat;

        if (autoStartOnEnable)
            StartRun();
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= HandleBeat;
        running = false;
    }

    public void StartRun()
    {
        running = true;
        stepsDone = 0;
        leadLeft = Mathf.Max(0, leadBeats);
    }

    public void StopRun() => running = false;

    // PatternTimeline에서 carPattern.SetActive(true/false) 호출하는 하위호환 래퍼
    public void SetActive(bool active)
    {
        if (active)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true); // OnEnable에서 구독
            StartRun(); // 확실히 달리기 시작
        }
        else
        {
            StopRun();
            if (usePooling)
                PoolManager.ReturnObjectToPool(gameObject);
            else
                gameObject.SetActive(false); // OnDisable에서 구독 해제
        }
    }

    // UnityEvent로도 받을 수 있게 공개
    public void OnBeatFromUnityEvent() => HandleBeat();

    private void HandleBeat()
    {
        if (!running) return;

        if (leadLeft > 0)
        {
            leadLeft--;
            return;
        }

        // 이 박자 기준 진행 방향 계산
        Vector3 dir = useWorldZForward ? Vector3.forward : transform.forward;
        if (invertDirection) dir = -dir;

        if (stepDistance != 0f)
            transform.position += dir * stepDistance;

        if (stepsMax >= 0 && ++stepsDone >= stepsMax)
        {
            running = false;
            if (usePooling)
                PoolManager.ReturnObjectToPool(gameObject);
            else if (destroyAtEnd)
                Destroy(gameObject);
        }
    }
}
