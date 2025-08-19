using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneLockManager : MonoBehaviour
{
    [Header("Force-Center On Lock")]
    public bool forceCenterWhenLocked = true;
    public bool enforceWhileLocked = true;
    public Transform playerTransform;
    public Transform[] laneAnchors = new Transform[3];
    [Range(0, 2)] public int centerLaneIndex = 1;

    [Header("I-Frames")]
    public bool grantIFramesForOneBeat = true;
    public float fallbackBeatSeconds = 0.5f;

    [Header("PosIndex Sync (no edit to PlayerInput)")]
    public bool syncPosIndexViaReflection = true;
    public string posIndexFieldName = "posIndex";

    [Header("Dynamic Car Occupancy")]
    public bool useCarOccupancyLock = true;
    [Tooltip("플레이어 Z 반길이(겹침 여유)")]
    public float playerHalfLenZ = 0.8f;

    [Header("Car Soft-Lock Timing")]
    [Tooltip("차 점유 소프트락을 '정박 순간'에만 적용")]
    public bool carSoftLockOnBeatOnly = true;

    public static bool IsPlayerInvulnerable { get; private set; }
    public static LaneLockManager Instance { get; private set; }

    private float[] unlockTimes = new float[3] { 0, 0, 0 };
    private readonly Dictionary<int, int> _stepsRemain = new();
    private bool _listeningBeats = false;

    public event Action<int, bool, float> OnLaneLockChanged;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!enforceWhileLocked || !forceCenterWhenLocked) return;
        if (!playerTransform || laneAnchors == null || laneAnchors.Length < 3) return;

        int cur = GetNearestLaneIndex(playerTransform.position);
        if (cur < 0 || cur >= laneAnchors.Length) return;

        if (IsLocked(cur)) // ※ 소프트락(차 점유)은 여기서 밀지 않음
        {
            int dest = Mathf.Clamp(centerLaneIndex, 0, 2);
            var center = laneAnchors[dest];
            if (!center) return;

            const float eps = 0.01f;
            if (Mathf.Abs(playerTransform.position.x - center.position.x) > eps)
            {
                playerTransform.position = new Vector3(center.position.x, playerTransform.position.y, playerTransform.position.z);
                if (syncPosIndexViaReflection) SetPlayerPosIndex(dest);

                if (grantIFramesForOneBeat)
                {
                    float beatSec = GetOneBeatSeconds();
                    StopCoroutineSafe(nameof(Co_IFrames));
                    StartCoroutine(Co_IFrames(beatSec));
                }
            }
        }
    }

    // ===== 공개 API =====
    public bool IsLocked(int lane)
    {
        if (lane < 0 || lane > 2) return false;
        if (Time.time < unlockTimes[lane]) return true;
        if (_stepsRemain.TryGetValue(lane, out int steps) && steps > 0) return true;
        return false;
    }

    public bool IsSoftLockedByCar(int lane)
    {
        if (!useCarOccupancyLock) return false;
        if (LaneBlockService.I == null || !playerTransform) return false;

        // ★ 정박 순간에만 소프트락 적용
        if (carSoftLockOnBeatOnly)
        {
            // BeatManager가 있다면 OnBeat 판정 창 안에서만 막기
            if (BeatManager.Instance != null && !BeatManager.Instance.IsOnBeatNow())
                return false; // 지금은 박자 창 아님 → 통과
        }

        // 차 점유 여부
        return !LaneBlockService.I.IsLaneFree(lane, playerTransform.position.z, playerHalfLenZ);
    }

    public void LockLane(int lane, float duration)
    {
        lane = Mathf.Clamp(lane, 0, 2);
        float until = Mathf.Max(unlockTimes[lane], Time.time + duration);
        unlockTimes[lane] = until;
        OnLaneLockChanged?.Invoke(lane, true, until - Time.time);
        TryForceCenterIfPlayerOn(lane);
    }

    public void LockLaneBeats(int lane, int steps)
    {
        lane = Mathf.Clamp(lane, 0, 2);
        if (steps <= 0) { UnlockLane(lane); return; }

        if (_stepsRemain.TryGetValue(lane, out int cur))
            _stepsRemain[lane] = Mathf.Max(cur, steps);
        else
            _stepsRemain[lane] = steps;

        OnLaneLockChanged?.Invoke(lane, true, GetRemain(lane));
        TryForceCenterIfPlayerOn(lane);
        EnsureBeatListening(true);
    }

    public void UnlockLane(int lane)
    {
        lane = Mathf.Clamp(lane, 0, 2);
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
        lane = Mathf.Clamp(lane, 0, 2);
        float secRemain = Mathf.Max(0, unlockTimes[lane] - Time.time);
        if (_stepsRemain.TryGetValue(lane, out int steps) && steps > 0)
            secRemain = Mathf.Max(secRemain, steps * GetOneBeatSeconds());
        return secRemain;
    }

    // ===== 내부 유틸 =====
    void EnsureBeatListening(bool forceOn = false)
    {
        bool need = forceOn || _stepsRemain.Count > 0;
        if (need && !_listeningBeats)
        {
            BeatManager.OnBeat += OnBeatTick; // BeatManager는 static event
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
                if (!IsLocked(lane))
                {
                    unlockTimes[lane] = 0f;
                    OnLaneLockChanged?.Invoke(lane, false, 0);
                }
                else
                {
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

    void TryForceCenterIfPlayerOn(int lockedLane)
    {
        if (!forceCenterWhenLocked || !playerTransform || laneAnchors == null || laneAnchors.Length < 3) return;

        int current = GetNearestLaneIndex(playerTransform.position);
        if (current != lockedLane) return;

        var center = laneAnchors[Mathf.Clamp(centerLaneIndex, 0, 2)];
        if (!center) return;

        playerTransform.position = new Vector3(center.position.x, playerTransform.position.y, playerTransform.position.z);
        if (syncPosIndexViaReflection) SetPlayerPosIndex(centerLaneIndex);

        if (grantIFramesForOneBeat)
        {
            float beatSec = GetOneBeatSeconds();
            StopCoroutineSafe(nameof(Co_IFrames));
            StartCoroutine(Co_IFrames(beatSec));
        }
    }

    int GetNearestLaneIndex(Vector3 pos)
    {
        if (laneAnchors == null || laneAnchors.Length < 3) return 1;
        int best = 0; float bestDist = Mathf.Infinity;
        for (int i = 0; i < laneAnchors.Length; i++)
        {
            var a = laneAnchors[i];
            if (!a) continue;
            float d = (pos - a.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    float GetOneBeatSeconds()
    {
        // BeatManager에 GetBeatDurationSec()가 있으므로 우선 사용
        try
        {
            if (BeatManager.Instance) return Mathf.Max(0.05f, BeatManager.Instance.GetBeatDurationSec());
        }
        catch { /* ignore */ }
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
        var pi = playerTransform.GetComponent<PlayerInput>(); // 당신 프로젝트의 PlayerInput
        if (!pi) return;

        var f = typeof(PlayerInput).GetField(posIndexFieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(int))
            f.SetValue(pi, Mathf.Clamp(newIndex, 0, 2));
    }

    void OnDestroy()
    {
        if (_listeningBeats) BeatManager.OnBeat -= OnBeatTick;
    }
}
