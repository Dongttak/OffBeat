using UnityEngine;

public class LaneJudge : MonoBehaviour
{
    public Transform[] lanePositions; // 0,1,2
    public PlayerInput playerInput;

    [Header("Block Gate")]
    [Tooltip("막혔을 때 PlayerInput을 비활성화할 프레임 수")]
    public int blockFramesOnReject = 3;
    [Tooltip("막혔을 때 다음 입력을 받기까지의 최소 시간(초)")]
    public float blockCooldown = 0.08f;

    int framesBlocked = 0;
    float nextInputTime = 0f;

    int GetNearestLaneIndex(Vector3 pos)
    {
        int best = 0; float bestDist = Mathf.Infinity;
        for (int i = 0; i < lanePositions.Length; i++)
        {
            if (!lanePositions[i]) continue;
            float d = (pos - lanePositions[i].position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    void Update()
    {
        if (!playerInput || lanePositions == null || lanePositions.Length < 3) return;

        // 게이트 유지
        if (framesBlocked > 0)
        {
            framesBlocked--;
            if (playerInput.enabled) playerInput.enabled = false;
            return;
        }
        else
        {
            if (!playerInput.enabled) playerInput.enabled = true;
        }

        if (Time.time < nextInputTime) return; // 쿨다운

        // 입력 읽기
        int cur = GetNearestLaneIndex(transform.position);
        int target = cur;
        bool left = Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.D);

        if (left) target = Mathf.Max(0, cur - 1);
        else if (right) target = Mathf.Min(2, cur + 1);
        else return;

        var lm = LaneLockManager.Instance;
        bool blocked = lm != null && (lm.IsLocked(target) || lm.IsSoftLockedByCar(target));

        if (blocked)
        {
            // 입력 차단: 다프레임 + 쿨다운
            framesBlocked = Mathf.Max(1, blockFramesOnReject);
            nextInputTime = Time.time + Mathf.Max(0f, blockCooldown);
            // 시각/사운드 피드백은 여기서
            return;
        }

        // 통과 → 실제 이동 호출 (프로젝트의 PlayerInput API에 맞게)
        // 예시: playerInput.TryMoveToLane(target);
    }
}
