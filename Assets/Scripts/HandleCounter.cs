using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandleCounter : MonoBehaviour
{
    [Header("DI")]
    [SerializeField] private CounterManager counterManager;   // 씬의 CounterManager 드래그 (비워두면 자동탐색)

    [Header("UI")]
    [SerializeField] private GameObject counterUI;            // "COUNTER" 표시 오브젝트(이미지/패널)
    [SerializeField] private CanvasGroup canvasGroup;         // 선택: 페이드 효과 주고 싶으면 연결
    [SerializeField] private float fadeIn = 0.08f;
    [SerializeField] private float fadeOut = 0.12f;

    [Header("Debug")]
    [SerializeField] private bool debugHotkey = true;         // 테스트용: J로 창 열기
    [SerializeField] private KeyCode openKey = KeyCode.J;

    Coroutine fading;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        if (counterUI) counterUI.SetActive(false);
        if (!canvasGroup && counterUI) canvasGroup = counterUI.GetComponent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        if (counterManager != null)
        {
            counterManager.OnCounterOpen += ShowUI;
            counterManager.OnCounterSuccess += HideUI;
            counterManager.OnCounterFail += HideUI;
        }
    }

    void OnDisable()
    {
        if (counterManager != null)
        {
            counterManager.OnCounterOpen -= ShowUI;
            counterManager.OnCounterSuccess -= HideUI;
            counterManager.OnCounterFail -= HideUI;
        }
    }

    void Update()
    {
        // 테스트용: J를 눌러 카운터 윈도우 강제 오픈
        if (debugHotkey && Input.GetKeyDown(openKey))
        {
            counterManager?.OpenWindow();
            Debug.Log("[CounterUI] Debug: OpenWindow()");
        }
    }

    // ===== UI 연출 =====
    void ShowUI()
    {
        if (!counterUI) return;
        counterUI.SetActive(true);
        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(CoFade(1f, fadeIn));
        Debug.Log("[CounterUI] Window OPEN");
    }

    void HideUI()
    {
        if (!counterUI) return;
        if (fading != null) StopCoroutine(fading);
        if (canvasGroup)
            fading = StartCoroutine(CoFade(0f, fadeOut, () => counterUI.SetActive(false)));
        else
            counterUI.SetActive(false);

        Debug.Log("[CounterUI] Window CLOSE");
    }

    IEnumerator CoFade(float target, float duration, System.Action onEnd = null)
    {
        if (!canvasGroup)
        {
            onEnd?.Invoke();
            yield break;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        canvasGroup.alpha = target;
        onEnd?.Invoke();
        fading = null;
    }
}
