using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CounterUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] CounterManager mgr;
    [SerializeField] CanvasGroup hintGroup;      // 힌트 아이콘
    [SerializeField] CanvasGroup windowGroup;    // 판정창 UI
    [SerializeField] Image windowFill;     // 0~1
    [SerializeField] CanvasGroup successGroup;   // 성공 문구/이펙트

    [Header("Timings")]
    [SerializeField] float fadeTime = 0.12f;
    [SerializeField] float successFlashTime = 0.35f;

    [Header("Show Toggles")]
    [Tooltip("경고 UI를 표시할지(패턴 경고만 하고 싶으면 OFF)")]
    [SerializeField] bool showHintUI = false;
    [Tooltip("실제 판정창 UI를 표시할지(성공시에만 UI를 보여주려면 OFF)")]
    [SerializeField] bool showWindowUI = false;
    [Tooltip("성공 UI 표시 여부")]
    [SerializeField] bool showSuccessUI = true;

    // 내부 상태/코루틴 핸들
    Coroutine hintFadeCo, windowFadeCo, successFadeCo, windowCo, successFlashCo;

    void Awake()
    {
        if (!mgr) mgr = FindObjectOfType<CounterManager>();
        ForceHideAll();                 // 씬 시작 시 무조건 다 끔
    }

    void OnEnable()
    {
        if (!mgr) return;
        mgr.OnCounterHintOpen += OnHintEvent;
        mgr.OnWindowOpen += OnWindowEvent;
        mgr.OnCounterSuccess += OnSuccessEvent;
        mgr.OnCounterFail += OnFailEvent;

        // 토글 재적용(에디터에서 값 바꿨을 때도 반영)
        ApplyToggles();
    }

    void OnDisable()
    {
        if (!mgr) return;
        mgr.OnCounterHintOpen -= OnHintEvent;
        mgr.OnWindowOpen -= OnWindowEvent;
        mgr.OnCounterSuccess -= OnSuccessEvent;
        mgr.OnCounterFail -= OnFailEvent;
    }

    // ───────────────── events

    void OnHintEvent()
    {
        // 힌트 UI를 안 쓸 땐 항상 꺼둔다
        if (!showHintUI || !mgr.ShowHints) { FadeTo(hintGroup, 0f, ref hintFadeCo); return; }
        FadeTo(hintGroup, 1f, ref hintFadeCo);
    }

    void OnWindowEvent(float duration)
    {
        // 힌트는 닫음
        FadeTo(hintGroup, 0f, ref hintFadeCo);

        // 창 UI를 안 쓰면 강제 숨김 + 진행 코루틴 정리
        if (!showWindowUI)
        {
            FadeTo(windowGroup, 0f, ref windowFadeCo);
            StopAndNull(ref windowCo);
            return;
        }

        // 창 타이머 코루틴 재시작
        StopAndNull(ref windowCo);
        windowCo = StartCoroutine(Co_Window(duration));
    }

    void OnSuccessEvent()
    {
        // 힌트/창은 닫고
        FadeTo(hintGroup, 0f, ref hintFadeCo);
        FadeTo(windowGroup, 0f, ref windowFadeCo);
        StopAndNull(ref windowCo);

        // 성공 UI만 잠깐 보여주기
        if (!showSuccessUI) { FadeTo(successGroup, 0f, ref successFadeCo); return; }

        StopAndNull(ref successFlashCo);
        successFlashCo = StartCoroutine(Co_SuccessFlash());
    }

    void OnFailEvent()
    {
        // 실패 시엔 모두 끔(원하면 실패 전용 연출을 여기 추가)
        ForceHideAll();
    }

    // ───────────────── coroutines

    IEnumerator Co_Window(float duration)
    {
        FadeTo(windowGroup, 1f, ref windowFadeCo);

        float end = Time.unscaledTime + duration;
        while (Time.unscaledTime < end)
        {
            if (windowFill)
                windowFill.fillAmount = Mathf.Clamp01((end - Time.unscaledTime) / duration);
            yield return null;
        }

        FadeTo(windowGroup, 0f, ref windowFadeCo);
        windowCo = null;
    }

    IEnumerator Co_SuccessFlash()
    {
        FadeTo(successGroup, 1f, ref successFadeCo);
        yield return new WaitForSecondsRealtime(successFlashTime);
        FadeTo(successGroup, 0f, ref successFadeCo);
        successFlashCo = null;
    }

    // ───────────────── helpers

    void ApplyToggles()
    {
        // 토글 OFF면 해당 그룹을 즉시 끔(겹친 코루틴도 정리)
        if (!showHintUI) { FadeImmediate(hintGroup, 0f, ref hintFadeCo); }
        if (!showWindowUI) { FadeImmediate(windowGroup, 0f, ref windowFadeCo); StopAndNull(ref windowCo); }
        if (!showSuccessUI) { FadeImmediate(successGroup, 0f, ref successFadeCo); StopAndNull(ref successFlashCo); }
    }

    void ForceHideAll()
    {
        FadeImmediate(hintGroup, 0f, ref hintFadeCo);
        FadeImmediate(windowGroup, 0f, ref windowFadeCo);
        FadeImmediate(successGroup, 0f, ref successFadeCo);
        StopAndNull(ref windowCo);
        StopAndNull(ref successFlashCo);
    }

    void FadeTo(CanvasGroup g, float a, ref Coroutine slot)
    {
        if (!g) return;
        StopAndNull(ref slot);
        slot = StartCoroutine(Co_Fade(g, a));
    }

    void FadeImmediate(CanvasGroup g, float a, ref Coroutine slot)
    {
        if (!g) return;
        StopAndNull(ref slot);
        g.alpha = a;
        g.blocksRaycasts = a > 0f;
        g.interactable = a > 0f;
    }

    IEnumerator Co_Fade(CanvasGroup g, float a)
    {
        float start = g.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, a, t / fadeTime);
            yield return null;
        }
        g.alpha = a;
        g.blocksRaycasts = a > 0f;
        g.interactable = a > 0f;
    }

    void StopAndNull(ref Coroutine co)
    {
        if (co != null) StopCoroutine(co);
        co = null;
    }

    // 필요하면 인스펙터/다른 스크립트에서 런타임 토글
    public void SetShowHintUI(bool v) { showHintUI = v; ApplyToggles(); }
    public void SetShowWindowUI(bool v) { showWindowUI = v; ApplyToggles(); }
    public void SetShowSuccessUI(bool v) { showSuccessUI = v; ApplyToggles(); }
}
