using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossAnimDriver : MonoBehaviour
{
    [Header("Target State / Sync")]
    [SerializeField] string idleStateName = "idle";
    [SerializeField] int animatorLayer = 0;
    [Tooltip("idle이 몇 박에 한 번 루프해야 하는지 (예: 2박 = 2)")]
    [SerializeField] int beatsPerLoop = 2;
    [Tooltip("매 OnBeat마다 살짝 재동기화할지")]
    [SerializeField] bool resyncOnBeat = true;
    [Tooltip("정규화 시간 오차 허용치(0~1). 이보다 어긋나면 0으로 살짝 끊어줌")]
    [SerializeField] float resyncTolerance = 0.15f;
    [Tooltip("재동기화시 크로스페이드 길이(초)")]
    [SerializeField] float resyncCrossFade = 0.02f;

    Animator anim;
    AnimationClip idleClip;
    float lastAppliedBpm = -1f;

    void Awake()
    {
        anim = GetComponent<Animator>();
        idleClip = FindClip(anim, idleStateName);
        if (idleClip == null)
            Debug.LogWarning($"[BossAnimDriver] '{idleStateName}' 클립을 Animator에서 찾지 못했습니다.");
    }

    void OnEnable()
    {
        BeatManager.OnBeat += HandleOnBeat;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= HandleOnBeat;
    }

    void Start()
    {
        ApplySpeedToIdle();   // 시작 시 한 번 세팅
    }

    void Update()
    {
        // BPM 변동(템포/피치 변경 등) 시 자동 반영
        float bpmNow = GetBpmSafe();
        if (!Mathf.Approximately(bpmNow, lastAppliedBpm))
        {
            ApplySpeedToIdle();
        }
    }

    void HandleOnBeat()
    {
        if (!resyncOnBeat || idleClip == null) return;

        var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
        if (!st.IsName(idleStateName)) return;

        // 현재 루프 내 위치(0~1)
        float phase = st.normalizedTime % 1f;

        // 목표는 "박자 경계마다 0으로 딱 떨어지게" 이지만,
        // beatsPerLoop 만큼의 분할 중 현재가 어느 세그먼트인지로 환산해 허용 오차 체크
        float segLen = 1f / Mathf.Max(1, beatsPerLoop); // 각 박자가 차지하는 정규화 길이
        float distToNearestEdge = Mathf.Min(phase % segLen, segLen - (phase % segLen));

        if (distToNearestEdge > resyncTolerance * segLen)
        {
            // 조금 어긋나면 짧게 0f로 크로스페이드해서 눈에 띄지 않게 붙여줌
            anim.CrossFade(idleStateName, resyncCrossFade, animatorLayer, 0f);
        }
    }

    void ApplySpeedToIdle()
    {
        if (idleClip == null) return;

        float bpmEff = GetBpmSafe();
        lastAppliedBpm = bpmEff;

        float beatDur = 60f / Mathf.Max(1e-4f, bpmEff);   // 1박 시간(초)
        float desiredLoopDuration = beatDur * Mathf.Max(1, beatsPerLoop); // idle이 돌아야 하는 목표 길이

        // Animator.speed = (원본클립길이 / 목표길이)
        // -> 실제 재생 시간(클립길이 / speed) = 목표길이
        float speed = idleClip.length / Mathf.Max(1e-4f, desiredLoopDuration);
        anim.speed = speed;
        // 현재 상태가 idle이 아니면 한 번 틀어줌(선택)
        var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
        if (!st.IsName(idleStateName))
        {
            anim.Play(idleStateName, animatorLayer, 0f);
        }
    }

    float GetBpmSafe()
    {
        if (BeatManager.Instance != null)
            return BeatManager.Instance.GetEffectiveBpm();
        return 120f; // 폴백
    }

    static AnimationClip FindClip(Animator animator, string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return null;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == clipName)
                return clip;
        return null;
    }
}
