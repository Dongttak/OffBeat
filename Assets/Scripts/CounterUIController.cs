using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CounterUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] CounterManager mgr;
    [SerializeField] CanvasGroup hintGroup;      // 힌트 아이콘
    [SerializeField] CanvasGroup windowGroup;    // 판정창 UI
    [SerializeField] Image windowFill;           // 0~1
    [SerializeField] CanvasGroup successGroup;   // 성공 문구/이펙트
    [SerializeField] float fadeTime = 0.1f;

    Coroutine windowCo;

    void Awake()
    {
        if (!mgr) mgr = FindObjectOfType<CounterManager>();
        SetAlpha(hintGroup, 0);
        SetAlpha(windowGroup, 0);
        SetAlpha(successGroup, 0);
    }

    void OnEnable()
    {
        if (!mgr) return;
        mgr.OnCounterHintOpen += ShowHint;
        mgr.OnWindowOpen += ShowWindow;
        mgr.OnCounterSuccess += ShowSuccess;
        mgr.OnCounterFail += HideAll;
    }

    void OnDisable()
    {
        if (!mgr) return;
        mgr.OnCounterHintOpen -= ShowHint;
        mgr.OnWindowOpen -= ShowWindow;
        mgr.OnCounterSuccess -= ShowSuccess;
        mgr.OnCounterFail -= HideAll;
    }

    void ShowHint()
    {
        if (!mgr.ShowHints) return;
        FadeTo(hintGroup, 1f);
    }

    void ShowWindow(float duration)
    {
        FadeTo(hintGroup, 0f);
        if (windowCo != null) StopCoroutine(windowCo);
        windowCo = StartCoroutine(Co_Window(duration));
    }

    IEnumerator Co_Window(float duration)
    {
        FadeTo(windowGroup, 1f);
        float end = Time.unscaledTime + duration;
        while (Time.unscaledTime < end)
        {
            if (windowFill)
                windowFill.fillAmount = Mathf.Clamp01((end - Time.unscaledTime) / duration);
            yield return null;
        }
        FadeTo(windowGroup, 0f);
        windowCo = null;
    }

    void ShowSuccess()
    {
        FadeTo(hintGroup, 0f);
        FadeTo(windowGroup, 0f);
        StartCoroutine(Co_SuccessFlash());
    }

    IEnumerator Co_SuccessFlash()
    {
        FadeTo(successGroup, 1f);
        yield return new WaitForSecondsRealtime(0.35f);
        FadeTo(successGroup, 0f);
    }

    void HideAll()
    {
        FadeTo(hintGroup, 0f);
        FadeTo(windowGroup, 0f);
        // 실패 이펙트 원하면 여기 추가
    }

    // helpers
    void SetAlpha(CanvasGroup g, float a)
    { if (g) { g.alpha = a; g.blocksRaycasts = a > 0; g.interactable = a > 0; } }

    void FadeTo(CanvasGroup g, float a)
    { if (g) StartCoroutine(Co_Fade(g, a)); }

    IEnumerator Co_Fade(CanvasGroup g, float a)
    {
        float start = g.alpha, t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(start, a, t / fadeTime);
            yield return null;
        }
        g.alpha = a; g.blocksRaycasts = a > 0; g.interactable = a > 0;
    }
}
