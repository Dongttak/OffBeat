using System;
using UnityEngine;

public class CounterManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float windowDuration = 0.35f;   // 판정창 길이
    [SerializeField] private float cooldown = 0.50f;         // 성공/실패 후 잠금 시간
    [SerializeField] private bool requireOffBeat = true;     // 엇박만 허용 여부

    [Header("Behavior")]
    [SerializeField] private bool showHints = true;

    public bool ShowHints => showHints;
    public float WindowDuration => windowDuration;
    public bool IsWindowOpen => _windowOpen;
    public bool IsCountering => _busy;

    public event Action OnCounterHintOpen;
    public event Action<float> OnWindowOpen;
    public event Action OnCounterSuccess;
    public event Action OnCounterFail;

    private bool _windowOpen;
    private bool _busy;
    private float _windowEndTime;

    public void SetShowHints(bool v) => showHints = v;

    /// <summary>힌트 → 실제 판정창이 열릴 시간을 예약</summary>
    public void PreHint(float leadSeconds = 0.25f)
    {
        if (!showHints || _busy) return;

        OnCounterHintOpen?.Invoke();

        if (leadSeconds > 0f)
            Invoke(nameof(OpenWindowInternal), leadSeconds);
        else
            OpenWindowInternal();
    }

    /// <summary>즉시 판정창 오픈 요청 (예: Phase2 보스 직접 호출)</summary>
    public void OpenWindow(float duration = -1f)
    {
        if (_busy) return;

        if (duration > 0f)
            windowDuration = duration;

        OpenWindowInternal();
    }

    private void OpenWindowInternal()
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
        {
            Fail();
            return false;
        }

        if (requireOffBeat && BeatManager.Instance && !BeatManager.Instance.IsOffBeatNow())
        {
            Fail();
            return false;
        }

        Success();
        return true;
    }

    private void Update()
    {
        if (_windowOpen && Time.unscaledTime >= _windowEndTime)
        {
            Fail();
        }
    }

    private void Success()
    {
        EndCounterWindow();
        OnCounterSuccess?.Invoke();
    }

    private void Fail()
    {
        EndCounterWindow();
        OnCounterFail?.Invoke();
    }

    private void EndCounterWindow()
    {
        _windowOpen = false;
        _busy = true;
        Invoke(nameof(ResetState), cooldown);
    }

    private void ResetState()
    {
        _busy = false;
    }
}
