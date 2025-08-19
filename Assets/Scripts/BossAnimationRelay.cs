// BossAnimationRelay.cs (개선)
using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    [SerializeField] Animator animator;

    [Header("Trigger Names")]
    [SerializeField] string pose1Trigger = "Pose1";
    [SerializeField] string pose2Trigger = "Pose2";
    [SerializeField] string shockTrigger = "Shock";
    [SerializeField] string stunTrigger = "Stun";

    [Header("Optional")]
    [SerializeField] CounterManager counterMgr;

    int _pose1Id, _pose2Id, _shockId, _stunId;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!counterMgr) counterMgr = FindObjectOfType<CounterManager>();

        _pose1Id = Animator.StringToHash(pose1Trigger);
        _pose2Id = Animator.StringToHash(pose2Trigger);
        _shockId = Animator.StringToHash(shockTrigger);
        _stunId = Animator.StringToHash(stunTrigger);
    }

    void OnEnable()
    {
        if (counterMgr != null)
        {
            counterMgr.OnCounterSuccess += OnCounterSuccess; // Shock
            counterMgr.OnCounterFail += OnCounterFail;       // (선택) 실패 리액션 필요시
        }
    }

    void OnDisable()
    {
        if (counterMgr != null)
        {
            counterMgr.OnCounterSuccess -= OnCounterSuccess;
            counterMgr.OnCounterFail -= OnCounterFail;
        }
    }

    // ===== 외부 호출 API =====
    public void OnLaserStart() => Set(_pose1Id);
    public void OnPattern2Step() => Set(_pose2Id);
    public void OnInstallDestroyed() => Set(_stunId);

    // ===== 카운터 이벤트 =====
    void OnCounterSuccess() => Set(_shockId);
    void OnCounterFail() { /* 필요시 별도 트리거/애니 추가 */ }

    void Set(int triggerId)
    {
        if (!animator) return;
        animator.ResetTrigger(_pose1Id);
        animator.ResetTrigger(_pose2Id);
        animator.ResetTrigger(_shockId);
        animator.ResetTrigger(_stunId);
        animator.SetTrigger(triggerId);
    }
}
