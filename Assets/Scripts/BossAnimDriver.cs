using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BossAnimDriver : MonoBehaviour
{
    [Header("Target State / Sync")]
    [SerializeField] string idleStateName = "idle"; // 상태 이름
    [SerializeField] int animatorLayer = 0;
    [SerializeField] int beatsPerLoop = 2;
    [SerializeField] bool resyncOnBeat = true;
    [Range(0f, 1f)][SerializeField] float resyncTolerance = 0.15f;
    [SerializeField] float resyncCrossFade = 0.02f;

    [Header("Clip Resolve (robust)")]
    [Tooltip("여기에 직접 지정하면 이름 매칭 없이 이 클립 길이를 사용")]
    [SerializeField] AnimationClip idleClipOverride;

    Animator anim;
    float lastAppliedBpm = -1f;
    float cachedIdleLen = -1f;   // 초

    void Awake()
    {
        anim = GetComponent<Animator>();
        cachedIdleLen = ResolveIdleClipLength(anim, idleStateName, idleClipOverride);

        if (cachedIdleLen <= 0f)
            Debug.LogWarning($"[BossAnimDriver] '{idleStateName}'용 idle 클립 길이를 찾지 못했습니다. Override를 지정하거나 상태/클립 이름을 맞춰주세요.", this);
    }

    void OnEnable() { BeatManager.OnBeat += HandleOnBeat; }
    void OnDisable() { BeatManager.OnBeat -= HandleOnBeat; }

    void Start() { ApplySpeedToIdle(); }

    void Update()
    {
        float bpmNow = GetBpmSafe();
        if (!Mathf.Approximately(bpmNow, lastAppliedBpm))
            ApplySpeedToIdle();
    }

    void HandleOnBeat()
    {
        if (!resyncOnBeat) return;

        var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
        // 상태 이름이 서브스테이트일 수 있으므로 부분 경로 허용
        if (!(st.IsName(idleStateName) || st.IsTag(idleStateName)))
            return;

        float segLen = 1f / Mathf.Max(1, beatsPerLoop);
        float phase = st.normalizedTime % 1f;
        float mod = phase % segLen;
        float distToEdge = Mathf.Min(mod, segLen - mod);

        if (distToEdge > resyncTolerance * segLen)
        {
            // 동일 상태로 매우 짧게 붙여서 티 안 나게 리싱크
            anim.CrossFade(idleStateName, resyncCrossFade, animatorLayer, 0f);
        }
    }

    void ApplySpeedToIdle()
    {
        float bpmEff = GetBpmSafe();
        lastAppliedBpm = bpmEff;

        // idle 길이 재해결(오버라이드/컨트롤러가 바뀌었을 수 있음)
        float idleLen = cachedIdleLen > 0f ? cachedIdleLen : ResolveIdleClipLength(anim, idleStateName, idleClipOverride);
        if (idleLen <= 0f)
        {
            anim.speed = 1f; // 폴백
            return;
        }

        float beatDur = 60f / Mathf.Max(1e-4f, bpmEff);
        float desiredLoopDuration = beatDur * Mathf.Max(1, beatsPerLoop);

        anim.speed = idleLen / Mathf.Max(1e-4f, desiredLoopDuration);

        // 현재 상태가 idle 아니면 틀어주기(선택)
        var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
        if (!(st.IsName(idleStateName) || st.IsTag(idleStateName)))
            anim.Play(idleStateName, animatorLayer, 0f);
    }

    float GetBpmSafe()
    {
        return BeatManager.Instance ? BeatManager.Instance.GetEffectiveBpm() : 120f;
    }

    // ───────────────────────── helpers

    static float ResolveIdleClipLength(Animator animator, string stateName, AnimationClip overrideClip)
    {
        if (overrideClip) return Mathf.Max(overrideClip.length, 0f);
        if (!animator || !animator.runtimeAnimatorController) return -1f;

        // 1) 런타임에 가능한 범위: 이름 대소문자 무시/부분일치로 클립 탐색
        var clips = animator.runtimeAnimatorController.animationClips;
        AnimationClip byName = FindByNameLoose(clips, stateName);
        if (byName) return Mathf.Max(byName.length, 0f);

        // 2) (에디터에서만) 상태→모션 역추적
#if UNITY_EDITOR
        var ctrl = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        if (ctrl != null)
        {
            var (foundClip, foundLen) = FindClipFromState(ctrl, stateName);
            if (foundClip) return Mathf.Max(foundLen, 0f);
        }
#endif
        // 3) 마지막 폴백: 현재 상태의 클립 길이(가중치>0인 첫 클립)
        var info = animator.GetCurrentAnimatorClipInfo(0);
        if (info != null && info.Length > 0 && info[0].clip)
            return Mathf.Max(info[0].clip.length, 0f);

        return -1f;
    }

    static AnimationClip FindByNameLoose(AnimationClip[] clips, string key)
    {
        if (clips == null || clips.Length == 0 || string.IsNullOrEmpty(key)) return null;

        // 정확히 동일(대소문자 무시)
        foreach (var c in clips)
            if (c && string.Equals(c.name, key, System.StringComparison.OrdinalIgnoreCase))
                return c;

        // 부분 일치(Idle, Idle_A 등)
        foreach (var c in clips)
            if (c && c.name.ToLowerInvariant().Contains(key.ToLowerInvariant()))
                return c;

        return null;
    }

#if UNITY_EDITOR
    static (AnimationClip clip, float length) FindClipFromState(UnityEditor.Animations.AnimatorController ctrl, string stateName)
    {
        foreach (var layer in ctrl.layers)
        {
            var sm = layer.stateMachine;
            var clip = FindInStateMachine(sm, stateName);
            if (clip) return (clip, clip.length);
        }
        return (null, -1f);
    }

    static AnimationClip FindInStateMachine(UnityEditor.Animations.AnimatorStateMachine sm, string stateName)
    {
        // 직속 스테이트
        foreach (var child in sm.states)
        {
            var st = child.state;
            if (st.name == stateName || st.tag == stateName)
            {
                var motion = st.motion;
                if (motion is AnimationClip ac) return ac;
                if (motion is UnityEditor.Animations.BlendTree bt)
                {
                    // 첫 child 모션 사용(필요 시 가중치 최대 항목 선택으로 개선 가능)
                    for (int i = 0; i < bt.children.Length; i++)
                        if (bt.children[i].motion is AnimationClip c) return c;
                }
            }
        }
        // 서브스테이트머신 재귀
        foreach (var sub in sm.stateMachines)
        {
            var hit = FindInStateMachine(sub.stateMachine, stateName);
            if (hit) return hit;
        }
        return null;
    }
#endif
}
