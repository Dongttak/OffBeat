using UnityEngine;

public class LaneJudge : MonoBehaviour
{
    public Transform[] lanePositions; // 0,1,2
    public PlayerInput playerInput;

    private int GetNearestLaneIndex(Vector3 pos)
    {
        int best = 0;
        float bestDist = Mathf.Infinity;
        for (int i = 0; i < lanePositions.Length; i++)
        {
            float d = (pos - lanePositions[i].position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private void Update()
    {
        if (!playerInput || lanePositions == null || lanePositions.Length < 3) return;

        // 의도한 목표 레인 계산
        int cur = GetNearestLaneIndex(transform.position);
        int target = cur;

        bool left = Input.GetKeyDown(KeyCode.A);
        bool right = Input.GetKeyDown(KeyCode.D);

        if (left) target = Mathf.Max(0, cur - 1);
        else if (right) target = Mathf.Min(2, cur + 1);
        else return; // 이동 입력이 없으면 아무 것도 안 함

        // 타겟 레인이 봉인이면 해당 프레임에 PlayerInput을 비활성화하여 입력무시
        if (LaneLockManager.Instance && LaneLockManager.Instance.IsLocked(target))
        {
            Debug.Log($"Lane {target} is locked!");
            // 이 프레임만 PlayerInput 비활성→활성
            playerInput.enabled = false;
            StartCoroutine(ReenableNextFrame());
        }
    }

    System.Collections.IEnumerator ReenableNextFrame()
    {
        yield return null; // 다음 프레임
        if (playerInput) playerInput.enabled = true;
    }
}
// 현재 이동 코드(PlayerInput) 따로 있어서 리팩토링 요망.