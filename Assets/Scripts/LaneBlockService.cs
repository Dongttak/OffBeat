using System.Collections.Generic;
using UnityEngine;

public class LaneBlockService : MonoBehaviour
{
    public static LaneBlockService I { get; private set; }

    // 0/1/2 레인 별 점유자 리스트
    readonly List<LaneBlocker>[] lists = { new(), new(), new() };

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public void Register(LaneBlocker b)
    {
        if (!b) return;
        int l = Mathf.Clamp(b.lane, 0, 2);
        if (!lists[l].Contains(b)) lists[l].Add(b);
    }

    public void Unregister(LaneBlocker b)
    {
        if (!b) return;
        // lane 필드에 의존하지 않고 모든 리스트에서 제거
        for (int l = 0; l < lists.Length; l++)
            lists[l].Remove(b);
    }

    /// <summary>lane에 playerZ 기준으로 겹치는 차가 없으면 true</summary>
    public bool IsLaneFree(int lane, float playerZ, float playerHalfZ)
    {
        if (lane < 0 || lane > 2) return true;
        var arr = lists[lane];

        for (int i = arr.Count - 1; i >= 0; i--)
        {
            var b = arr[i];
            if (!b || !b.isActiveAndEnabled) { arr.RemoveAt(i); continue; }

            float dz = Mathf.Abs(b.transform.position.z - playerZ);
            if (dz <= (b.GetHalfZ() + playerHalfZ))
                return false; // 겹침 → 막힘
        }
        return true;
    }
}
