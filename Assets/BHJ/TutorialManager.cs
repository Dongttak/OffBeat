using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public enum Step { Move, Attack, Special, Done }
    [Header("Order & Targets")]
    [SerializeField] private Step startStep = Step.Move;
    [SerializeField] private int moveSuccessTarget = 5; // 이동 성공 목표
    [SerializeField] private int attackSuccessTarget = 5;   // 공격 성공 목표
    [SerializeField] private int specialSuccessTarget = 3; // 구현 대기 시 0으로 두면 스킵됨

    [Header("UI")]
    [SerializeField] private GameObject TutorialUIGroup;    // 튜토리얼 UI(그룹)
    [SerializeField] private TextMeshProUGUI titleText;     // 단계 안내
    [SerializeField] private TextMeshProUGUI guideText;     // 키 가이드
    [SerializeField] private TextMeshProUGUI counterText;   // 성공/목표
    [SerializeField] private TextMeshProUGUI feedbackText;  // GOOD / MISS
    [SerializeField] private TextMeshProUGUI tipText;       // 팁

    [Header("Prompt UI (Press Any Key)")]
    [SerializeField] private GameObject promptGroup;        // 안내 패널(그룹)
    [SerializeField] private TextMeshProUGUI promptTitle;   // 안내 제목
    [SerializeField] private TextMeshProUGUI promptBody;    // 안내 본문
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
    private int _misscount = 0; // 미스 카운트
    private bool isWaitingPrompt = false; // 프롬프트에서 아무 키 입력 대기 중인지

    public PlayerInput playerInput;
    public GameManager gameManager;

    void Start()
    {
        SetStep(startStep);
        feedbackText?.SetText("");
    }

    void Update()
    {
        if (isWaitingPrompt)
        {
            TutorialUIGroup.SetActive(false);
            gameManager.PauseGame();
            if (Input.anyKeyDown)
            {
                gameManager.ResumeGame();
                HidePrompt();
                SetStep(_step); // 실제 단계 시작
            }
            return;
        }
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
        {
            // 다음 단계로 넘어가기 전 안내 표시
            ShowPrompt(Step.Attack);
        }
    }

    void HandleAttackStep()
    {
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), onSuccess: () => { _successCount++; OnAttackSuccess?.Invoke(); });

        UpdateCounter(attackSuccessTarget);
        if (_successCount >= attackSuccessTarget)
        {
            var next = (specialSuccessTarget > 0) ? Step.Special : Step.Done;
            ShowPrompt(next);
        }
    }

    void HandleSpecialStep()
    {
        // Special은 '엇박' 판정으로 진행
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), onSuccess: () => { _successCount++; OnSpecialSuccess?.Invoke(); });

        UpdateCounter(specialSuccessTarget);
        if (_successCount >= specialSuccessTarget)
        {
            ShowPrompt(Step.Done);
        }
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
            _misscount = 0; // 리셋
            tipText?.gameObject.SetActive(false);
            onSuccess?.Invoke();
            ShowFeedback("GOOD", Color.cyan);
        }
        else
        {
            _misscount++;
            ShowFeedback("MISS", Color.red);
            if (_misscount >= 3)
            {
                ShowTip("주변 배경을 잘 살펴보세요!", Color.red);
            }
        }
    }

    void SetStep(Step s)
    {
        _step = s;
        _successCount = 0;

        switch (s)
        {
            case Step.Move:
                SetUI("이동", $"[{moveLeftKey}] / [{moveRightKey}] 로 이동", 0);
                break;
            case Step.Attack:
                SetUI("공격", $"[{attackKey}] 로 공격", 0);
                break;
            case Step.Special:
                SetUI("엇박 카운터", $"[{attackKey}]", 0);
                break;
            case Step.Done:
                SetUI("튜토리얼 완료", "좋아요! 이제 실전에 들어가보죠.", 0, hideCounter: true);
                break;
        }
    }

    void Advance(Step next) => SetStep(next);

    void SetUI(string title, string guide, int current, bool hideCounter = false)
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
    }
    void ShowTip(string msg, Color c)
    {
        tipText?.gameObject.SetActive(true);
        if (tipText == null) return;
        tipText.color = c;
        tipText.SetText(msg);
    }
    void ShowPrompt(Step stepToStart)
    {
        // 다음에 시작할 스텝을 먼저 기록
        _step = stepToStart;
        isWaitingPrompt = true;

        if (promptGroup != null) promptGroup.SetActive(true);

        // 프롬프트 문구 구성
        switch (stepToStart)
        {
            case Step.Move:
                promptTitle?.SetText("이동 튜토리얼");
                promptBody?.SetText($"정박에 맞춰 [{moveLeftKey}] / [{moveRightKey}] 를 눌러 이동합니다.\n아무 키나 누르면 시작합니다.");
                break;
            case Step.Attack:
                promptTitle?.SetText("공격 튜토리얼");
                promptBody?.SetText($"정박에 맞춰 [{attackKey}] 를 눌러 공격합니다.\n아무 키나 누르면 시작합니다.");
                break;
            case Step.Special:
                promptTitle?.SetText("카운터 튜토리얼");
                promptBody?.SetText($"엇박 타이밍에 [{attackKey}] 를 눌러 카운터합니다.\n아무 키나 누르면 시작합니다.");
                break;
            case Step.Done:
                promptTitle?.SetText("튜토리얼 완료");
                promptBody?.SetText("좋아요! 이제 실전에 들어가보죠.\n아무 키나 누르면 종료합니다.");
                break;
        }
    }

    void HidePrompt()
    {
        isWaitingPrompt = false;
        if (promptGroup != null) promptGroup.SetActive(false);

        // Done인 경우엔 실제 단계 시작 대신 UI만 남기고 종료
        if (_step == Step.Done)
        {
            SetStep(Step.Done);
        }
    }
}
