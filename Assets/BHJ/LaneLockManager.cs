using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class LaneLockManager : MonoBehaviour
{
    [Header("Force-Center On Lock")]
    [Tooltip("봉인된 레인에 플레이어가 서 있으면 중앙 레인으로 강제 이동")]
    public bool forceCenterWhenLocked = true;

    [Tooltip("강제 이동 대상 플레이어")]
    public Transform playerTransform;

    [Tooltip("레인 앵커(0/1/2)")]
    public Transform[] laneAnchors = new Transform[3];

    [Tooltip("중앙 레인 인덱스 (기본 1)")]
    [Range(0, 2)] public int centerLaneIndex = 1;

    [Header("I-Frames")]
    [Tooltip("강제 이동 시 1비트 동안 무적 부여")]
    public bool grantIFramesForOneBeat = true;

    [Tooltip("비트 길이(초). BeatState에 SecondsPerBeat가 있으면 자동 사용, 없으면 이 값을 사용")]
    public float fallbackBeatSeconds = 0.5f;

    [Header("PosIndex Sync (no edit to PlayerInput)")]
    public bool syncPosIndexViaReflection = true;
    [Tooltip("PlayerInput 내부 필드명(현재 코드 기준)")]
    public string posIndexFieldName = "posIndex";
    public static bool IsPlayerInvulnerable { get; private set; }
    public static LaneLockManager Instance { get; private set; }

    private float[] unlockTimes = new float[3] { 0, 0, 0 };
    // 비트(스텝) 기반 락 남은 스텝
    private readonly Dictionary<int, int> _stepsRemain = new();
    private bool _listeningBeats = false;
    public event Action<int, bool, float> OnLaneLockChanged;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsLocked(int lane)
    {
        if (Time.time < unlockTimes[lane]) return true;         // 초 기반
        if (_stepsRemain.TryGetValue(lane, out int steps) && steps > 0) return true; // 비트 기반
        return false;
    }

    public void LockLane(int lane, float duration)
    {
        float until = Mathf.Max(unlockTimes[lane], Time.time + duration);
        unlockTimes[lane] = until;
        OnLaneLockChanged?.Invoke(lane, true, until - Time.time);
        TryForceCenterIfPlayerOn(lane);
    }
    // ===== 비트(스텝) 기반 락 =====
    public void LockLaneBeats(int lane, int steps)
    {
        if (steps <= 0) { UnlockLane(lane); return; }

        // 기존 비트 락이 있으면 더 긴 쪽으로 유지
        if (_stepsRemain.TryGetValue(lane, out int cur))
            _stepsRemain[lane] = Mathf.Max(cur, steps);
        else
            _stepsRemain[lane] = steps;

        // 초 기반과 병행 가능: IsLocked는 둘 중 하나라도 잠그면 true
        OnLaneLockChanged?.Invoke(lane, true, GetRemain(lane));

        TryForceCenterIfPlayerOn(lane);
        EnsureBeatListening(true); // 비트 이벤트 구독 시작/유지
    }
    // Beat 이벤트 수신 등록/해제
    void EnsureBeatListening(bool forceOn = false)
    {
        bool need = forceOn || _stepsRemain.Count > 0;
        if (need && !_listeningBeats)
        {
            BeatManager.OnBeat += OnBeatTick;
            _listeningBeats = true;
        }
        else if (!need && _listeningBeats)
        {
            BeatManager.OnBeat -= OnBeatTick;
            _listeningBeats = false;
        }
    }
    void OnBeatTick()
    {
        if (_stepsRemain.Count == 0) { EnsureBeatListening(false); return; }

        var lanes = new List<int>(_stepsRemain.Keys);
        foreach (var lane in lanes)
        {
            _stepsRemain[lane]--;
            if (_stepsRemain[lane] <= 0)
            {
                _stepsRemain.Remove(lane);
                // 비트 락이 풀려도 초 기반 남아있을 수 있으니 UnlockLane 호출 대신 상태 갱신
                if (!IsLocked(lane))
                {
                    unlockTimes[lane] = 0f;
                    OnLaneLockChanged?.Invoke(lane, false, 0);
                    // 자동 복귀가 필요하면 여기서 호출:
                    // TryReturnToLaneIfCenter(lane);
                }
                else
                {
                    // 아직 초 기반으로 잠겨있다면 남은 초 알림
                    OnLaneLockChanged?.Invoke(lane, true, GetRemain(lane));
                }
            }
            else
            {
                OnLaneLockChanged?.Invoke(lane, true, GetRemain(lane));
            }
        }

        if (_stepsRemain.Count == 0) EnsureBeatListening(false);
    }

    public void UnlockLane(int lane)
    {
        bool wasLocked = IsLocked(lane);

        unlockTimes[lane] = 0f;
        _stepsRemain.Remove(lane);

        if (wasLocked)
            OnLaneLockChanged?.Invoke(lane, false, 0);

        if (_stepsRemain.Count == 0)
            EnsureBeatListening(false);
    }

    public float GetRemain(int lane)
    {
        float secRemain = Mathf.Max(0, unlockTimes[lane] - Time.time);

        if (_stepsRemain.TryGetValue(lane, out int steps) && steps > 0)
            secRemain = Mathf.Max(secRemain, steps * GetOneBeatSeconds());

        return secRemain;
    }
    void TryForceCenterIfPlayerOn(int lockedLane)
    {
        if (!forceCenterWhenLocked) return;
        if (!playerTransform) return;
        if (laneAnchors == null || laneAnchors.Length < 3) return;

        int current = GetNearestLaneIndex(playerTransform.position);
        if (current != lockedLane) return; // 봉인된 레인에 서 있지 않으면 스킵

        var center = laneAnchors[Mathf.Clamp(centerLaneIndex, 0, 2)];
        if (center)
        {
            playerTransform.position = new Vector3(
                center.position.x,
                playerTransform.position.y,
                playerTransform.position.z
            );
            if (syncPosIndexViaReflection) SetPlayerPosIndex(centerLaneIndex);

        }

        // 1비트 무적
        if (grantIFramesForOneBeat)
        {
            float beatSec = GetOneBeatSeconds();
            StopCoroutineSafe(nameof(Co_IFrames));
            StartCoroutine(Co_IFrames(beatSec));
        }
    }
    void TryReturnToLaneIfCenter(int targetLane)
    {
        if (!playerTransform) return;
        if (laneAnchors == null || laneAnchors.Length < 3) return;

        int cur = GetNearestLaneIndex(playerTransform.position);
        if (cur != centerLaneIndex) return; // 중앙에 있을 때만 자동 복귀

        var anchor = laneAnchors[Mathf.Clamp(targetLane, 0, 2)];
        if (!anchor) return;

        // 즉시 스냅(원하면 트윈)
        playerTransform.position = new Vector3(
            anchor.position.x,
            playerTransform.position.y,
            playerTransform.position.z
        );

        // ★ posIndex를 '복귀 레인'으로 동기화
        if (syncPosIndexViaReflection) SetPlayerPosIndex(targetLane);
    }

    int GetNearestLaneIndex(Vector3 pos)
    {
        int best = 0;
        float bestDist = Mathf.Infinity;
        for (int i = 0; i < laneAnchors.Length; i++)
        {
            if (!laneAnchors[i]) continue;
            float d = (pos - laneAnchors[i].position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    float GetOneBeatSeconds()
    {
        // 프로젝트에 BeatState 자동 사용
        try
        {
            var beatState = BeatState.Instance;
            var prop = beatState.GetType().GetProperty("SecondsPerBeat", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                var v = prop.GetValue(beatState, null);
                if (v is float f && f > 0f) return f;
                if (v is double d && d > 0.0) return (float)d;
            }
        }
        catch { /* 무시하고 폴백 사용 */ }

        return Mathf.Max(0.05f, fallbackBeatSeconds);
    }

    IEnumerator Co_IFrames(float duration)
    {
        IsPlayerInvulnerable = true;
        yield return new WaitForSeconds(duration);
        IsPlayerInvulnerable = false;
    }

    void StopCoroutineSafe(string routineName)
    {
        try { StopCoroutine(routineName); } catch { }
    }
    void SetPlayerPosIndex(int newIndex)
    {
        if (!playerTransform) return;
        var pi = playerTransform.GetComponent<PlayerInput>();
        if (!pi) return;

        // private int posIndex 를 리플렉션으로 갱신
        var f = typeof(PlayerInput).GetField(posIndexFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int))
        {
            f.SetValue(pi, Mathf.Clamp(newIndex, 0, 2));
        }
    }
    void OnDestroy()
    {
        if (_listeningBeats)
            BeatManager.OnBeat -= OnBeatTick;
    }
}
