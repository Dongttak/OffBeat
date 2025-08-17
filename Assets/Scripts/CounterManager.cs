using System;
using UnityEngine;

public class CounterManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float windowDuration = 0.35f;   // 판정창 길이
    [SerializeField] private float cooldown = 0.50f;   // 성공/실패 후 잠금 시간
    [SerializeField] private bool requireOffBeat = true;    // 엇박만 허용 여부

    [Header("Behavior")]
    [SerializeField] private bool showHints = true;          // 힌트(!) 사용 여부 (Phase1에서 true)

    [Header("Debug / Test")]
    [SerializeField] private bool enableTestKey = true;   // ← 테스트 키 사용
    [SerializeField] private KeyCode testKey = KeyCode.T;
    [Tooltip("테스트에서 힌트 후 실제 판정창까지 기다릴 시간(초)")]
    [SerializeField] private float testLeadSeconds = 0.25f;
    [Tooltip("테스트에서 실제 판정창 길이(초)")]
    [SerializeField] private float testWindowSeconds = 0.35f;

    public bool ShowHints => showHints;
    public float WindowDuration => windowDuration;
    public bool IsWindowOpen => _windowOpen;
    public bool IsCountering => _busy;
    
    public event Action OnCounterHintOpen;     // 힌트(!) 표시
    public event Action<float> OnWindowOpen;          // 판정창 오픈(duration)
    public event Action OnCounterSuccess;
    public event Action OnCounterFail;

    bool _windowOpen;
    bool _busy;
    float _windowEndTime;

    public void SetShowHints(bool v) => showHints = v;

    /// <summary>보스 패턴 시작 시: 힌트(!) 띄우고, leadSeconds 후 자동으로 판정창 오픈</summary>
    public void PreHint(float leadSeconds = 0.25f)
    {
        if (!showHints || _busy) return;

        OnCounterHintOpen?.Invoke();

        if (leadSeconds > 0f)
            Invoke(nameof(OpenWindowInternal), leadSeconds);
        else
            OpenWindowInternal();
    }

    /// <summary>바로 판정창만 열고 싶을 때(Phase2)</summary>
    public void OpenWindow(float duration = -1f)
    {
        if (_busy) return;

        if (duration > 0f) windowDuration = duration;
        OpenWindowInternal();
    }

    void OpenWindowInternal()
    {
        if (_busy) return;
        _windowOpen = true;
        _windowEndTime = Time.unscaledTime + windowDuration;
        OnWindowOpen?.Invoke(windowDuration);
    }

    /// <summary>플레이어가 반응 시도 → 타이밍 판정</summary>
    public bool TryCounter()
    {
        if (!_windowOpen)
        { Fail(); return false; }

        if (requireOffBeat && BeatManager.Instance && !BeatManager.Instance.IsOffBeatNow())
        { Fail(); return false; }

        Success(); return true;
    }

    void Update()
    {
        // 창 시간 초과
        if (_windowOpen && Time.unscaledTime >= _windowEndTime)
            Fail();

        // ===== 테스트 트리거 =====
        if (enableTestKey && Input.GetKeyDown(testKey))
        {
            // 힌트(!)만 쓰고 싶으면 PreHint만 호출,
            // 바로 창만 보고 싶으면 OpenWindow(testWindowSeconds) 호출로 바꾸면 됩니다.
            if (showHints)
                PreHint(testLeadSeconds);      // 힌트 → testLeadSeconds 후 자동으로 창 오픈
            else
                OpenWindow(testWindowSeconds);  // Phase2처럼 즉시 창 오픈
        }
    }

    void Success()
    {
        EndCounterWindow();
        OnCounterSuccess?.Invoke();
    }

    void Fail()
    {
        EndCounterWindow();
        OnCounterFail?.Invoke();
    }

    void EndCounterWindow()
    {
        _windowOpen = false;
        _busy = true;
        Invoke(nameof(ResetState), cooldown);
    }

    void ResetState() => _busy = false;
}
