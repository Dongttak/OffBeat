using UnityEngine;

[DisallowMultipleComponent]
public class LaneBlocker : MonoBehaviour
{
    [Header("Lane")]
    [Tooltip("이 차가 달리는 레인(0/1/2). 스폰 직후 코드에서 세팅 권장")]
    public int lane = -1;

    [Header("길이(점유) 추정")]
    [Tooltip("Z 방향 절반 길이(미터). <=0이면 Renderer.bounds로 자동 추정")]
    public float halfLenZ = -1f;

    [Tooltip("겹침 방지 여유 마진(Z)")]
    public float extraMarginZ = 0.3f;

    void OnEnable()
    {
        if (halfLenZ <= 0f)
            halfLenZ = ComputeHalfLenZFromRenderers();

        LaneBlockService.I?.Register(this); // 서비스 있으면 등록
    }

    void OnDisable()
    {
        LaneBlockService.I?.Unregister(this);
    }

    public void SetLane(int laneIndex)
    {
        // 등록된 게 있으면 먼저 해제(이 시점의 lane 값 기준)
        LaneBlockService.I?.Unregister(this);

        lane = Mathf.Clamp(laneIndex, 0, 2);

        // 새 레인으로 다시 등록
        LaneBlockService.I?.Register(this);
    }

    public float GetHalfZ() => Mathf.Max(0.1f, halfLenZ) + Mathf.Max(0f, extraMarginZ);

    float ComputeHalfLenZFromRenderers()
    {
        var rends = GetComponentsInChildren<Renderer>(true);
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return Mathf.Max(0.1f, b.extents.z);
        }
        return 1.5f; // 안전 기본값
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        float hz = (halfLenZ > 0f ? halfLenZ : ComputeHalfLenZFromRenderers()) + Mathf.Max(0f, extraMarginZ);
        Vector3 p = transform.position;
        Gizmos.DrawCube(new Vector3(p.x, p.y, p.z), new Vector3(2f, 1f, hz * 2f));
    }
#endif
}
