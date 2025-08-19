using UnityEngine;

public class BossAnimationRelay : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private CounterManager counterMgr;

    [Header("Trigger Names (Animator Parameters)")]
    [SerializeField] private string pose1Trigger = "Pose1"; // 힌트(PreHint)
    [SerializeField] private string pose2Trigger = "Pose2"; // 창 오픈(WindowOpen)
    [SerializeField] private string shockTrigger = "Shock"; // 카운터 성공
    [SerializeField] private string stunTrigger = "Stun";  // 설치물 파괴 등

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
        if (!counterMgr) return;
        // CounterManager(원본) 이벤트 시그니처에 정확히 맞춤
        counterMgr.OnCounterHintOpen += HandleHintOpen;        // ⇒ Pose1
        counterMgr.OnWindowOpen += HandleWindowOpen;      // ⇒ Pose2
        counterMgr.OnCounterSuccess += HandleCounterSuccess;  // ⇒ Shock
        counterMgr.OnCounterFail += HandleCounterFail;     // (원하면 반응)
    }

    void OnDisable()
    {
        if (!counterMgr) return;
        counterMgr.OnCounterHintOpen -= HandleHintOpen;
        counterMgr.OnWindowOpen -= HandleWindowOpen;
        counterMgr.OnCounterSuccess -= HandleCounterSuccess;
        counterMgr.OnCounterFail -= HandleCounterFail;
    }

    // ===== CounterManager Events =====
    void HandleHintOpen() => Set(_pose1Id);
    void HandleWindowOpen(float duration) => Set(_pose2Id);
    void HandleCounterSuccess() => Set(_shockId);
    void HandleCounterFail() { /* 필요시 실패 리액션 트리거 추가 */ }

    // ===== 외부에서 직접 호출용 (설치물 파괴 등) =====
    public void OnInstallDestroyed() => Set(_stunId);

    // ===== 공통 =====
    void Set(int triggerId)
    {
        if (!animator) return;
        // 동시에 여러 트리거 섞이지 않게 모두 리셋 후 Set
        animator.ResetTrigger(_pose1Id);
        animator.ResetTrigger(_pose2Id);
        animator.ResetTrigger(_shockId);
        animator.ResetTrigger(_stunId);
        animator.SetTrigger(triggerId);
    }

#if UNITY_EDITOR
    [ContextMenu("TEST Pose1")] void _TestPose1() => Set(_pose1Id);
    [ContextMenu("TEST Pose2")] void _TestPose2() => Set(_pose2Id);
    [ContextMenu("TEST Shock")] void _TestShock() => Set(_shockId);
    [ContextMenu("TEST Stun")]  void _TestStun()  => Set(_stunId);
#endif
}
