using System.Collections;
using UnityEngine;

[AddComponentMenu("Offbeat/Gameplay/LaneBumpOnContact")]
public class LaneBumpOnContact : MonoBehaviour
{
    [Header("Refs")]
    public LaneLockManager lockMgr;          //자동으로 Instance 사용
    [Tooltip("Player가 속한 레이어 이름 (선택)")]
    public string playerLayerName = "Player";
    [Tooltip("Car 레이어 이름 (Trigger 방식에만 필요)")]
    public string carLayerName = "Car";

    [Header("Detection Modes")]
    [Tooltip("Collider(Trigger)로 감지할지 여부")]
    public bool useTriggerDetection = true;
    [Tooltip("콜라이더 없이 LaneBlockService로 겹침을 폴링")]
    public bool useOccupancyPolling = true;
    [Tooltip("Occupancy 폴링 주기(초)")]
    public float pollInterval = 0.05f;

    [Header("Bump Behavior")]
    [Tooltip("튕김 쿨다운(초) — 연속 튕김 방지")]
    public float bumpCooldown = 0.25f;
    [Tooltip("양 옆이 모두 막혔을 때, 튕김을 포기(기본) / 강제로 반대편으로 시도할지")]
    public bool tryOppositeIfBlocked = true;
    [Tooltip("튕김 시 플레이어 속도 제거(잔류로 인한 미끌림 방지)")]
    public bool zeroVelocityOnBump = true;

    int _carLayer = -1;
    float _nextBumpTime = 0f;

    void Awake()
    {
        if (!lockMgr) lockMgr = LaneLockManager.Instance;
        _carLayer = string.IsNullOrEmpty(carLayerName) ? -1 : LayerMask.NameToLayer(carLayerName);

        if (useOccupancyPolling) StartCoroutine(Co_PollOccupancy());
    }
    void OnTriggerEnter(Collider other) { if (useTriggerDetection) TryBumpFromTrigger(other); }
    void OnTriggerStay(Collider other) { if (useTriggerDetection) TryBumpFromTrigger(other); }

    void TryBumpFromTrigger(Collider other)
    {
        if (_carLayer >= 0 && other.gameObject.layer != _carLayer) return;
        if (!Ready()) return;

        // 충돌한 물체의 x를 기준으로 "반대쪽" 우선
        float carX = other.bounds.center.x;
        TryBumpDecide(carX);
    }

   
    IEnumerator Co_PollOccupancy()
    {
        var svc = LaneBlockService.I;
        while (true)
        {
            yield return new WaitForSeconds(pollInterval);
            if (!Ready() || svc == null) continue;

            //현재 레인에서 플레이어 z와 겹치는 차가 있는지 검사
            int cur = GetCurrentLane();
            if (cur < 0) continue;

            float pz = lockMgr.playerTransform.position.z;
            bool free = svc.IsLaneFree(cur, pz, lockMgr.playerHalfLenZ);
            if (!free)
            {
                //차의 x가 플레이어 왼/오른쪽 어디에 있는지 대략 판단(가까운 레인 앵커 x 비교)
                float carXApprox = EstimateCarXInLane(cur);
                TryBumpDecide(carXApprox);
            }
        }
    }

    //현재 레인 파악
    int GetCurrentLane()
    {
        if (lockMgr == null || lockMgr.laneAnchors == null || lockMgr.laneAnchors.Length < 3) return -1;
        var p = lockMgr.playerTransform.position;
        int best = 0; float bestDist = Mathf.Infinity;
        for (int i = 0; i < lockMgr.laneAnchors.Length; i++)
        {
            var a = lockMgr.laneAnchors[i];
            if (!a) continue;
            float d = Mathf.Abs(p.x - a.position.x);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    //같은 레인에서 차의 x를 근사(레인 앵커 x와 동일하다고 가정)
    float EstimateCarXInLane(int lane)
    {
        if (lockMgr == null || lockMgr.laneAnchors == null || lane < 0 || lane >= lockMgr.laneAnchors.Length) return lockMgr.playerTransform.position.x;
        var a = lockMgr.laneAnchors[lane];
        return a ? a.position.x : lockMgr.playerTransform.position.x;
    }

    bool Ready()
    {
        if (Time.time < _nextBumpTime) return false;
        if (!lockMgr || !lockMgr.playerTransform || lockMgr.laneAnchors == null || lockMgr.laneAnchors.Length < 3) return false;
        return true;
    }

    void TryBumpDecide(float carX)
    {
        var p = lockMgr.playerTransform.position;
        int cur = GetCurrentLane();
        if (cur < 0) return;

        //차가 플레이어의 왼쪽에 있으면 오른쪽으로 먼저 튕김, 반대도 동일
        bool preferRight = carX <= p.x;
        int right = Mathf.Min(2, cur + 1);
        int left = Mathf.Max(0, cur - 1);

        //우선순위에 따라 시도
        if (preferRight)
        {
            if (!Blocked(right)) { DoBump(right); return; }
            if (tryOppositeIfBlocked && !Blocked(left)) { DoBump(left); return; }
        }
        else
        {
            if (!Blocked(left)) { DoBump(left); return; }
            if (tryOppositeIfBlocked && !Blocked(right)) { DoBump(right); return; }
        }

        // 양 옆이 모두 막혀 있으면 아무 것도 안 함(혹은 여기서 스턴/이펙트)
        // Debug.Log("Both sides blocked; no bump.");
    }

    bool Blocked(int lane)
    {
        if (lane < 0 || lane > 2) return true;
        var lm = lockMgr;
        if (lm == null) return true;

        //하드락(패턴) 또는 소프트락(차 점유)이면 막힘
        return lm.IsLocked(lane) || lm.IsSoftLockedByCar(lane);
    }

    void DoBump(int targetLane)
    {
        var lm = lockMgr;
        if (lm == null || lm.playerTransform == null || lm.laneAnchors == null || lm.laneAnchors.Length < 3) return;

        targetLane = Mathf.Clamp(targetLane, 0, 2);
        var anchor = lm.laneAnchors[targetLane];
        if (!anchor) return;

        if (zeroVelocityOnBump)
        {
            var rb = lm.playerTransform.GetComponent<Rigidbody>();
            if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        }

        var p = lm.playerTransform.position;
        lm.playerTransform.position = new Vector3(anchor.position.x, p.y, p.z);
        if (lm.syncPosIndexViaReflection)
        {
            // LaneLockManager 안의 SetPlayerPosIndex가 internal이면 public으로 바꾸거나,
            // 여기서 리플렉션 한 번 더 써도 됩니다.
            var f = typeof(PlayerInput).GetField(lm.posIndexFieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pi = lm.playerTransform.GetComponent<PlayerInput>();
            if (f != null && pi != null) f.SetValue(pi, targetLane);
        }

        _nextBumpTime = Time.time + bumpCooldown;
    }
}
