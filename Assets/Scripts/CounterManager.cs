using System;
using UnityEngine;

/// <summary>
/// 카운터 판정/쿨다운을 관리하는 매니저.
/// - OpenWindow()로 창을 열면 UI가 켜지고(HandleCounter가 이벤트 구독)
/// - 플레이어가 TryCounter() 호출 시 성공/실패를 판단해서 이벤트 발행
/// - requireOffBeat=true면 엇박에서만 성공
/// - 타이밍은 Time.unscaledTime 기반이라 슬로모션에 영향 받지 않음
/// </summary>
public class CounterManager : MonoBehaviour
{
    [Header("Window")]
    [SerializeField] private float windowDuration = 0.35f; // 입력 가능 시간
    [SerializeField] private float cooldown = 0.5f;        // 성공/실패 후 재사용 지연
    [SerializeField] private bool requireOffBeat = true;   // 엇박에서만 성공

    // 상태 확인용 프로퍼티
    public bool IsWindowOpen => _windowOpen;
    public bool IsCountering => _isCountering;
    public float WindowDuration => windowDuration;

    // 이벤트: UI/연출이 구독
    public event Action OnCounterOpen;
    public event Action OnCounterSuccess;
    public event Action OnCounterFail;

    // 내부 상태
    bool _windowOpen;
    bool _isCountering;          // 쿨다운 중 포함(입력 잠김)
    float _windowEndUnscaled;    // 창 종료 시각(unscaled)

    /// <summary> 카운터 창 열기 (duration<0면 기본값 사용) </summary>
    public void OpenWindow(float duration = -1f)
    {
        if (_isCountering) return;                 // 쿨다운 중엔 무시
        if (duration > 0f) windowDuration = duration;

        _windowOpen = true;
        _windowEndUnscaled = Time.unscaledTime + windowDuration;
        OnCounterOpen?.Invoke();
        Debug.Log($"[Counter] OPEN (dur={windowDuration:0.###}s)");
    }

    /// <summary> 플레이어가 카운터 입력 시 호출 </summary>
    public bool TryCounter()
    {
        if (!_windowOpen) { Fail(); return false; }

        // 엇박만 허용 옵션이면 판정
        if (requireOffBeat)
        {
            bool offBeat;
            // BeatManager가 있으면 정확 판정
            if (BeatManager.Instance != null)
                offBeat = BeatManager.Instance.IsOffBeatNow();
            else
                offBeat = (BeatState.Instance?.CurrBeatState == BeatState.BeatType.OffBeat);

            if (!offBeat) { Fail(); return false; }
        }

        Success();
        return true;
    }

    void Update()
    {
        // 창 시간 초과
        if (_windowOpen && Time.unscaledTime > _windowEndUnscaled)
            Fail();
    }

    void Success()
    {
        _windowOpen = false;
        _isCountering = true;
        OnCounterSuccess?.Invoke();
        Debug.Log("[Counter] SUCCESS");
        Invoke(nameof(Ready), cooldown);
    }

    void Fail()
    {
        _windowOpen = false;
        _isCountering = true;
        OnCounterFail?.Invoke();
        Debug.Log("[Counter] FAIL");
        Invoke(nameof(Ready), cooldown);
    }

    void Ready()
    {
        _isCountering = false;
        // 필요하면 여기서 자동 재오픈/사운드 등 추가 가능
    }
}
