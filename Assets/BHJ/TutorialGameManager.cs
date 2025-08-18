using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialGameManager : MonoBehaviour
{
    // ====== 튜토리얼 ======
    public enum Step { Move, Attack, Special, Done }

    // NEW: 진행 방식
    public enum ProgressMode { ByCount, ByTime }
    [Header("진행 방식")]
    [SerializeField] private ProgressMode progressMode = ProgressMode.ByTime; // 기본: 시간 모드

    [Header("목표 횟수 설정 (ByCount 모드에서만 사용)")]
    [SerializeField] private Step startStep = Step.Move;
    [SerializeField] private int moveSuccessTarget = 5;
    [SerializeField] private int attackSuccessTarget = 5;
    [SerializeField] private int specialSuccessTarget = 5; // 0이면 Special 스킵

    // NEW: 단계별 제한시간 (ByTime 모드)
    [Header("단계별 제한 시간 (초) - ByTime 모드")]
    [SerializeField] private float moveDuration = 15f;
    [SerializeField] private float attackDuration = 15f;
    [SerializeField] private float specialDuration = 15f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialUIGroup;   // 튜토리얼 전체 그룹
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI guideText;
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptGroup;
    [SerializeField] private TextMeshProUGUI promptTitle;
    [SerializeField] private TextMeshProUGUI promptBody;
    [SerializeField] private Button promptContinueButton;

    [Header("키 설정")]
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;

    [Header("추가옵션: 실제 플레이어 액션 연결")]
    public UnityEvent OnMoveLeftSuccess;
    public UnityEvent OnMoveRightSuccess;
    public UnityEvent OnAttackSuccess;
    public UnityEvent OnSpecialSuccess;

    private Step _step;
    private int _successCount;
    private int _missCount;
    private bool isWaitingPrompt = false;

    private float _stepTimeLeft = 0f;
    private int _attemptCount; //시도 횟수


    // ====== 게임 공통 ======
    public enum GameState { Playing, Paused }
    public GameState CurrentGameState { get; private set; } = GameState.Playing;

    [Header("플레이어 입력")]
    public PlayerInput playerInput;

    [Header("Pause/Popup UI")]
    public GameObject pausePopupUI;

    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText;
    private bool isResuming = false;

    [Header("튜토리얼 종료 후 UI")]
    [SerializeField] private GameObject SceneButton;

    [Header("FMOD 경로")]
    [SerializeField] private string musicBusPath = ""; // 예: "bus:/Music" (비워두면 미사용)
#if FMOD
    private FMOD.Studio.Bus musicBus;
#endif

    // ====== Unity Lifecycle ======
    private void Awake()
    {
#if FMOD
        if (!string.IsNullOrEmpty(musicBusPath))
        {
            try { musicBus = FMODUnity.RuntimeManager.GetBus(musicBusPath); }
            catch { musicBus.clearHandle(); }
        }
#endif
    }

    private void Start()
    {
        feedbackText?.SetText("");
        tipText?.gameObject.SetActive(false);
        promptGroup?.SetActive(false);
        pausePopupUI?.SetActive(false);
        countdownText?.gameObject.SetActive(false);
        SceneButton?.gameObject.SetActive(false);

        SetStep(startStep);
        StartGame();
        ShowPrompt(_step); // 먼저 안내
    }

    private void Update()
    {
        if (isWaitingPrompt)
        {
            tutorialUIGroup?.SetActive(false);
            if (CurrentGameState != GameState.Paused) PauseGame();
            return;
        }

        // ESC 입력: 프롬프트/카운트다운 중에는 차단
        bool blockEsc = isWaitingPrompt || isResuming;
        if (!blockEsc)
        {
            if (CurrentGameState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            {
                CurrentGameState = GameState.Paused;
                PauseGame();
                return;
            }
            else if (CurrentGameState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape) && !isResuming)
            {
                StartCoroutine(ResumeAfterDelay(3f));
                return;
            }
        }

        if (CurrentGameState == GameState.Paused) return;
        if (_step == Step.Done) return;

        if (progressMode == ProgressMode.ByTime)
        {
            _stepTimeLeft -= Time.deltaTime;
            UpdateTimeCounterUI(); // 남은시간 갱신

            if (_stepTimeLeft <= 0f)
            {
                GoNextStep_ByTime();
                return; // 다음 스텝 프롬프트로
            }
        }

        // 튜토리얼 단계 입력 처리
        switch (_step)
        {
            case Step.Move: HandleMoveStep(); break;
            case Step.Attack: HandleAttackStep(); break;
            case Step.Special: HandleSpecialStep(); break;
        }
    }
    public void ConfirmPrompt()
    {
        // Done이면 그냥 닫고 종료 UI 유지
        if (_step == Step.Done)
        {
            HidePrompt();
            return;
        }

        // 그 외 단계: 프롬프트 닫고 재생 + 해당 스텝 시작(타이머 초기화 포함)
        HidePrompt();
        ResumeGame();
        SetStep(_step);
    }
    // ====== 튜토리얼 로직 ======
    void HandleMoveStep()
    {
        if (Input.GetKeyDown(moveLeftKey))
            TryJudge(OnBeatCheck(), () => { _successCount++; OnMoveLeftSuccess?.Invoke(); });
        else if (Input.GetKeyDown(moveRightKey))
            TryJudge(OnBeatCheck(), () => { _successCount++; OnMoveRightSuccess?.Invoke(); });

        if (progressMode == ProgressMode.ByCount)
        {
            UpdateCounter(moveSuccessTarget);
            if (_successCount >= moveSuccessTarget) ShowPrompt(Step.Attack);
        }
        else
        {
            UpdateTimeCounterUI();
        }
    }

    void HandleAttackStep()
    {
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), () => { _successCount++; OnAttackSuccess?.Invoke(); });

        if (progressMode == ProgressMode.ByCount)
        {
            UpdateCounter(attackSuccessTarget);
            if (_successCount >= attackSuccessTarget)
            {
                var next = (specialSuccessTarget > 0) ? Step.Special : Step.Done;
                ShowPrompt(next);
            }
        }
        else
        {
            UpdateTimeCounterUI();
        }
    }

    void HandleSpecialStep()
    {
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), () => { _successCount++; OnSpecialSuccess?.Invoke(); });

        if (progressMode == ProgressMode.ByCount)
        {
            UpdateCounter(specialSuccessTarget);
            if (_successCount >= specialSuccessTarget) ShowPrompt(Step.Done);
        }
        else
        {
            UpdateTimeCounterUI();
        }
    }

    bool OnBeatCheck()
    {
        var bm = BeatManager.Instance;
        if (bm == null) return false;
        return bm.IsOnBeatNow();
    }

    void TryJudge(bool isOnBeat, System.Action onSuccess)
    {
        _attemptCount++;
        if (isOnBeat)
        {
            _missCount = 0;
            tipText?.gameObject.SetActive(false);
            onSuccess?.Invoke();
            ShowFeedback("GOOD", Color.cyan);
        }
        else
        {
            _missCount++;
            ShowFeedback("MISS", Color.red);
            if (_missCount >= 3) ShowTip("주변 배경을 잘 살펴보세요!", Color.red);
        }

        if (progressMode == ProgressMode.ByTime) UpdateTimeCounterUI();
        else
        {
            switch (_step)
            {
                case Step.Move: UpdateCounter(moveSuccessTarget); break;
                case Step.Attack: UpdateCounter(attackSuccessTarget); break;
                case Step.Special: UpdateCounter(specialSuccessTarget); break;
            }
        }
    }

    void SetStep(Step s)
    {
        _step = s;
        _successCount = 0;
        _attemptCount = 0; // 시도 횟수 초기화
        switch (s)
        {
            case Step.Move:
                SetUI("이동", $"[{moveLeftKey}] / [{moveRightKey}] 로 이동", 0);
                InitStepTimerIfNeeded(moveDuration);
                break;
            case Step.Attack:
                SetUI("공격", $"[{attackKey}] 로 공격", 0);
                InitStepTimerIfNeeded(attackDuration);
                break;
            case Step.Special:
                SetUI("엇박 카운터", $"[{attackKey}]", 0);
                InitStepTimerIfNeeded(specialDuration);
                break;
            case Step.Done:
                SetUI("", ".", 0, hideCounter: true);
                break;
        }

        if (progressMode == ProgressMode.ByTime)
        {
            UpdateTimeCounterUI(); // 시작 시점 UI 초기화
        }
        else
        {
            // 카운트 모드
            switch (s)
            {
                case Step.Move: UpdateCounter(moveSuccessTarget); break;
                case Step.Attack: UpdateCounter(attackSuccessTarget); break;
                case Step.Special: UpdateCounter(specialSuccessTarget); break;
            }
        }
    }

    // 스텝 타이머 초기화
    void InitStepTimerIfNeeded(float duration)
    {
        if (progressMode != ProgressMode.ByTime) return;
        _stepTimeLeft = Mathf.Max(0f, duration);
    }

    void SetUI(string title, string guide, int current, bool hideCounter = false)
    {
        titleText?.SetText(title);
        guideText?.SetText(guide);
        counterText?.gameObject.SetActive(!hideCounter);

        if (!hideCounter)
        {
            if (progressMode == ProgressMode.ByCount)
                counterText?.SetText("0% / ?"); // 초기값 성공률
            else
                counterText?.SetText($"0% | {FormatTime(_stepTimeLeft)}"); // 초기값 성공률
        }

        feedbackText?.SetText("");
        tutorialUIGroup?.SetActive(true);
    }

    void UpdateCounter(int target)
    {
        float rate = (_attemptCount > 0) ? ((float)_successCount / _attemptCount * 100f) : 0f;
        counterText?.SetText($"성공률 : {rate:0}%, 남은 횟수 : {target - _successCount}");
    }
    void UpdateTimeCounterUI()
    {
        if (!counterText) return;
        float rate = (_attemptCount > 0) ? ((float)_successCount / _attemptCount * 100f) : 0f;
        counterText.SetText($"{rate:0}% | {FormatTime(Mathf.Max(0f, _stepTimeLeft))}"); // 시간 모드: 성공률% | 남은시간
    }

    string FormatTime(float t)
    {
        int total = Mathf.CeilToInt(t);
        int m = total / 60;
        int s = total % 60;
        return $"{m:00}:{s:00}";
    }

    void GoNextStep_ByTime()
    {
        switch (_step)
        {
            case Step.Move:
                ShowPrompt(Step.Attack);
                break;
            case Step.Attack:
                ShowPrompt((specialDuration > 0f) ? Step.Special : Step.Done);
                break;
            case Step.Special:
                ShowPrompt(Step.Done);
                break;
        }
    }

    void ShowFeedback(string msg, Color c)
    {
        if (!feedbackText) return;
        feedbackText.color = c;
        feedbackText.SetText(msg);
        StopAllCoroutines(); // 다른 깜빡임 코루틴 등을 사용 중이면 정리
    }

    void ShowTip(string msg, Color c)
    {
        if (!tipText) return;
        tipText.gameObject.SetActive(true);
        tipText.color = c;
        tipText.SetText(msg);
    }

    void ShowPrompt(Step stepToStart)
    {
        _step = stepToStart;      // 다음에 시작할 스텝을 먼저 기록
        isWaitingPrompt = true;

        if (promptGroup) promptGroup.SetActive(true);
        if (promptContinueButton)
        {
            promptContinueButton.gameObject.SetActive(true);

            // 버튼 키보드 네비게이션 비활성화
            var nav = new Navigation { mode = Navigation.Mode.None };
            promptContinueButton.navigation = nav;
        }

        // 선택된 UI를 비우고, 네비게이션 이벤트 차단
        var es = EventSystem.current;
        if (es)
        {
            es.SetSelectedGameObject(null);
            es.sendNavigationEvents = false;   // Space/Enter 제출 방지
        }
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
                promptBody?.SetText("좋아요! 이제 실전에 들어가보죠.");
                PlayerPrefs.SetInt("TutorialCompleted", 1);
                PlayerPrefs.Save();  // 저장 강제 반영
                SceneButton?.gameObject.SetActive(true);
                break;
        }
    }

    public void HidePrompt()
    {
        isWaitingPrompt = false;
        if (promptGroup) promptGroup.SetActive(false);

        var es = EventSystem.current;
        if (es) es.sendNavigationEvents = true;

        if (_step == Step.Done)
        {
            SetStep(Step.Done);
        }
    }

    public void StartGame()
    {
        CurrentGameState = GameState.Playing;
        pausePopupUI?.SetActive(false);
        Time.timeScale = 1f;
        SetMusicPaused(false);
        SetPlayerInputEnabled(true);
        tutorialUIGroup?.SetActive(true);
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            pausePopupUI?.SetActive(true);
            tutorialUIGroup?.SetActive(false);
        }
        Time.timeScale = 0f;
        SetMusicPaused(true);
        SetPlayerInputEnabled(false);
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            tutorialUIGroup?.SetActive(true);
            CurrentGameState = GameState.Playing;
        }
        Time.timeScale = 1f;
        SetMusicPaused(false);
        SetPlayerInputEnabled(true);
    }

    private IEnumerator ResumeAfterDelay(float delay)
    {
        isResuming = true;
        countdownText?.gameObject.SetActive(true);
        pausePopupUI?.SetActive(false);
        float remaining = delay;
        while (remaining > 0f)
        {
            if (countdownText) countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSecondsRealtime(1f);
            remaining--;
        }

        countdownText?.gameObject.SetActive(false);
        isResuming = false;
        ResumeGame();
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ====== Helper ======
    void SetPlayerInputEnabled(bool enabled)
    {
        if (playerInput) playerInput.enabled = enabled;
    }

    void SetMusicPaused(bool paused)
    {
#if FMOD
        if (musicBus.isValid()) musicBus.setPaused(paused);
#endif
    }
}
