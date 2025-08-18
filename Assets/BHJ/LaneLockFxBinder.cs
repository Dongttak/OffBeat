using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneLockFxBinder : MonoBehaviour
{
    [Header("Lock FX")]
    public GameObject lockFxPrefab;              // VFX Graph 또는 ParticleSystem 프리팹
    public Transform[] laneFxParents = new Transform[3];
    public Vector3 fxLocalOffset = Vector3.zero;
    public bool fallbackToAnchorsFromManager = true;

    [Header("Debug / Quick Test")]
    public bool autoTestLockOnPlay = false;      // 플레이 직후 자동으로 0번 레인 잠금(3초)

    readonly Dictionary<int, GameObject> activeFx = new Dictionary<int, GameObject>();
    readonly Dictionary<int, Coroutine> timers = new Dictionary<int, Coroutine>();
    bool _subscribed;

    void OnEnable()
    {
        StartCoroutine(Co_EnsureSubscribed());
    }

    IEnumerator Co_EnsureSubscribed()
    {
        while (LaneLockManager.Instance == null) yield return null;

        if (!_subscribed)
        {
            LaneLockManager.Instance.OnLaneLockChanged += HandleLaneLockChanged;
            _subscribed = true;
        }

        // 이미 잠겨있는 레인 즉시 반영
        for (int i = 0; i < 3; i++)
            if (LaneLockManager.Instance.IsLocked(i))
                SpawnOrRefresh(i, LaneLockManager.Instance.GetRemain(i));

        // 빠른 자가 테스트
        if (autoTestLockOnPlay)
        {
            // 살짝 대기 후 잠금(구독 타이밍 보장)
            yield return null;
            LaneLockManager.Instance.LockLane(0, 3f);
        }
    }

    void OnDisable()
    {
        if (_subscribed && LaneLockManager.Instance != null)
            LaneLockManager.Instance.OnLaneLockChanged -= HandleLaneLockChanged;
        _subscribed = false;

        foreach (var kv in timers) if (kv.Value != null) StopCoroutine(kv.Value);
        timers.Clear();
        foreach (var kv in activeFx) if (kv.Value) Destroy(kv.Value);
        activeFx.Clear();
    }

    void HandleLaneLockChanged(int lane, bool isLocked, float remainSeconds)
    {
        if (isLocked) SpawnOrRefresh(lane, remainSeconds);
        else StopAndDestroy(lane);
    }

    void SpawnOrRefresh(int lane, float remainSeconds)
    {
        if (!lockFxPrefab) return;

        if (activeFx.TryGetValue(lane, out var fx) && fx)
        {
            RestartTimer(lane, remainSeconds);
            return;
        }

        Transform parent = null;
        if (laneFxParents != null && lane < laneFxParents.Length)
            parent = laneFxParents[lane];

        if (!parent && fallbackToAnchorsFromManager && LaneLockManager.Instance &&
            LaneLockManager.Instance.laneAnchors != null && lane < LaneLockManager.Instance.laneAnchors.Length)
        {
            parent = LaneLockManager.Instance.laneAnchors[lane];
        }

        Vector3 pos = parent ? parent.position : Vector3.zero;
        Quaternion rot = parent ? parent.rotation : Quaternion.identity;

        fx = Instantiate(lockFxPrefab, pos, rot);
        if (parent) fx.transform.SetParent(parent, worldPositionStays: true);
        fx.transform.position = (parent ? parent.position : fx.transform.position) + fxLocalOffset;
        fx.SetActive(true);

        // PS 또는 VFX Graph 모두 재생 시도
        TryPlayParticleSystem(fx);
        TryPlayVisualEffect(fx);

        activeFx[lane] = fx;
        RestartTimer(lane, remainSeconds);
    }

    void RestartTimer(int lane, float remainSeconds)
    {
        if (timers.TryGetValue(lane, out var co) && co != null)
            StopCoroutine(co);
        timers[lane] = StartCoroutine(Co_StopAfter(lane, Mathf.Max(0.01f, remainSeconds)));
    }

    IEnumerator Co_StopAfter(int lane, float sec)
    {
        yield return new WaitForSeconds(sec);
        StopAndDestroy(lane);
    }

    void StopAndDestroy(int lane)
    {
        if (timers.TryGetValue(lane, out var co) && co != null)
            StopCoroutine(co);
        timers.Remove(lane);

        if (activeFx.TryGetValue(lane, out var fx) && fx)
        {
            // PS/VFX 모두 정지 시도 후 파괴
            TryStopParticleSystem(fx, out float psTail);
            TryStopVisualEffect(fx, out float vfxTail);

            float tail = Mathf.Max(psTail, vfxTail, 0.25f);
            Destroy(fx, tail);
        }
        activeFx.Remove(lane);
    }

    // -------- Helpers: PS / VFX 둘 다 핸들 --------
    void TryPlayParticleSystem(GameObject go)
    {
        var ps = go.GetComponent<ParticleSystem>();
        if (ps) ps.Play(true);
    }

    void TryStopParticleSystem(GameObject go, out float tail)
    {
        tail = 0f;
        var ps = go.GetComponent<ParticleSystem>();
        if (!ps) return;

        var main = ps.main;
        tail = main.startLifetime.constantMax;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void TryPlayVisualEffect(GameObject go)
    {
        // VisualEffect 의존성 없이 반사로 호출 (VFX Graph 없으면 그냥 패스)
        var vfx = go.GetComponent("VisualEffect");
        if (vfx != null)
        {
            var m = vfx.GetType().GetMethod("Play", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            m?.Invoke(vfx, null);
        }
    }

    void TryStopVisualEffect(GameObject go, out float tail)
    {
        tail = 0f;
        var vfx = go.GetComponent("VisualEffect");
        if (vfx != null)
        {
            var m = vfx.GetType().GetMethod("Stop", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            m?.Invoke(vfx, null);
            // 필요하면 여기서 VFX Graph 출력 수명 파라미터를 읽어 tail에 반영
        }
    }
}
