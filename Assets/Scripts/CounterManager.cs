using System;
using UnityEngine;

public class CounterManager : MonoBehaviour
{
    [Header("Window")]
    [SerializeField] float windowDuration = 0.35f;
    [SerializeField] float cooldown = 0.5f;
    [SerializeField] bool requireOffBeat = true; // 엇박에만 성공

    public float WindowDuration => windowDuration;

    public event Action OnCounterOpen;
    public event Action OnCounterSuccess;
    public event Action OnCounterFail;

    bool _window;
    bool _busy;
    float _endTime;

    public void OpenWindow(float duration = -1f)
    {
        if (_busy) return;

        if (duration > 0f) windowDuration = duration;
        _window = true;
        _endTime = Time.time + windowDuration;
        OnCounterOpen?.Invoke();
    }

    public void TryCounter()
    {
        if (!_window) { Fail(); return; }

        // 엇박만 허용하고 싶으면 여기서 체크
        if (requireOffBeat && BeatState.Instance.CurrBeatState != BeatState.BeatType.OffBeat)
        {
            Fail(); return;
        }

        Success();
    }

    void Update()
    {
        if (_window && Time.time > _endTime)
            Fail();
    }

    void Success()
    {
        _window = false; _busy = true;
        OnCounterSuccess?.Invoke();
        Invoke(nameof(Ready), cooldown);
    }

    void Fail()
    {
        _window = false; _busy = true;
        OnCounterFail?.Invoke();
        Invoke(nameof(Ready), cooldown);
    }

    void Ready() => _busy = false;
}
