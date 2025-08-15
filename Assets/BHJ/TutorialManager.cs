using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public enum Step { Move, Attack, Special, Done }
    [Header("Order & Targets")]
    [SerializeField] private Step startStep = Step.Move;
    [SerializeField] private int moveSuccessTarget = 5;
    [SerializeField] private int attackSuccessTarget = 5;
    [SerializeField] private int specialSuccessTarget = 3; // 구현 대기 시 0으로 두면 스킵됨

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;     // 단계 안내
    [SerializeField] private TextMeshProUGUI guideText;     // 키 가이드
    [SerializeField] private TextMeshProUGUI counterText;   // 성공/목표
    [SerializeField] private TextMeshProUGUI feedbackText;  // GOOD / MISS

    [Header("Key Bindings")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;

    [Header("Optional: Hook actual player actions")]
    public UnityEvent OnMoveLeftSuccess;
    public UnityEvent OnMoveRightSuccess;
    public UnityEvent OnAttackSuccess;
    public UnityEvent OnSpecialSuccess;

    private Step _step;
    private int _successCount;

    void Start()
    {
        SetStep(startStep);
        feedbackText?.SetText("");
    }

    void Update()
    {
        if (_step == Step.Done) return;

        switch (_step)
        {
            case Step.Move:
                HandleMoveStep();
                break;
            case Step.Attack:
                HandleAttackStep();
                break;
            case Step.Special:
                HandleSpecialStep(); // 추후 특수 입력/행동으로 교체
                break;
        }
    }

    // === Steps ===
    void HandleMoveStep()
    {
        if (Input.GetKeyDown(moveLeftKey))
            TryJudge(OnBeatCheck(), onSuccess: () => { _successCount++; OnMoveLeftSuccess?.Invoke(); });
        else if (Input.GetKeyDown(moveRightKey))
            TryJudge(OnBeatCheck(), onSuccess: () => { _successCount++; OnMoveRightSuccess?.Invoke(); });

        UpdateCounter(moveSuccessTarget);
        if (_successCount >= moveSuccessTarget)
            Advance(Step.Attack);
    }

    void HandleAttackStep()
    {
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), onSuccess: () => { _successCount++; OnAttackSuccess?.Invoke(); });

        UpdateCounter(attackSuccessTarget);
        if (_successCount >= attackSuccessTarget)
            Advance(specialSuccessTarget > 0 ? Step.Special : Step.Done);
    }

    void HandleSpecialStep()
    {
        // 임시: 공격키를 특수키 대용으로 사용
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), onSuccess: () => { _successCount++; OnSpecialSuccess?.Invoke(); });

        UpdateCounter(specialSuccessTarget);
        if (_successCount >= specialSuccessTarget)
            Advance(Step.Done);
    }

    // === Helpers ===
    bool OnBeatCheck()
    {
        var bm = BeatManager.Instance;
        if (bm == null) return false; // 안전장치
        return bm.IsOnBeatNow();
    }

    void TryJudge(bool isOnBeat, System.Action onSuccess)
    {
        if (isOnBeat)
        {
            onSuccess?.Invoke();
            ShowFeedback("GOOD", Color.cyan);
        }
        else
        {
            ShowFeedback("MISS", Color.red);
        }
    }

    void SetStep(Step s)
    {
        _step = s;
        _successCount = 0;

        switch (s)
        {
            case Step.Move:
                SetUI("이동 튜토리얼", $"정박에 맞춰 [{moveLeftKey}] / [{moveRightKey}] 이동 입력", 0);
                break;
            case Step.Attack:
                SetUI("공격 튜토리얼", $"정박에 맞춰 [{attackKey}] 공격 입력", 0);
                break;
            case Step.Special:
                SetUI("엇박 카운터 튜토리얼", $"엇박에 맞춰서! [{attackKey}]", 0);
                break;
            case Step.Done:
                SetUI("튜토리얼 완료", "좋아요! 이제 실전에 들어가보죠.", 0, hideCounter:true);
                break;
        }
    }

    void Advance(Step next) => SetStep(next);

    void SetUI(string title, string guide, int current, bool hideCounter=false)
    {
        titleText?.SetText(title);
        guideText?.SetText(guide);
        counterText?.gameObject.SetActive(!hideCounter);
        if (!hideCounter) counterText?.SetText($"0 / ?");
        feedbackText?.SetText("");
    }

    void UpdateCounter(int target)
    {
        counterText?.SetText($"{_successCount} / {target}");
    }

    void ShowFeedback(string msg, Color c)
    {
        if (feedbackText == null) return;
        feedbackText.color = c;
        feedbackText.SetText(msg);
        StopAllCoroutines();
        StartCoroutine(ClearFeedbackAfter(0.5f));
    }

    System.Collections.IEnumerator ClearFeedbackAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (feedbackText != null) feedbackText.SetText("");
    }
}
