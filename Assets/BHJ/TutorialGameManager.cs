using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

public class TutorialGameManager : MonoBehaviour
{
    // ====== 튜토리얼 ======
    public enum Step { Move, Attack, Special, Done }
    public enum ProgressMode { ByCount, ByTime }

    [Header("진행 방식")]
    [SerializeField] private ProgressMode progressMode = ProgressMode.ByTime;

    [Header("시작 스텝 & 목표/시간")]
    [SerializeField] private Step startStep = Step.Move;
    [SerializeField] private int moveSuccessTarget = 5;
    [SerializeField] private int attackSuccessTarget = 5;
    [SerializeField] private int specialSuccessTarget = 5;  // 0이면 Special 스킵
    [SerializeField] private float moveDuration = 15f;      // ByTime 모드에서만 사용
    [SerializeField] private float attackDuration = 15f;
    [SerializeField] private float specialDuration = 15f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialUIGroup;
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
    [SerializeField] private KeyCode attackKey = KeyCode.J;   // 정박-공격
    [SerializeField] private KeyCode CounterKey = KeyCode.K;  // 엇박-카운터
                                                              // 추가: 수동 스폰용 키
    [Header("Attack Tutorial Spawner")]
    [SerializeField] private GameObject attackTutorialPrefab;
    [SerializeField] private Transform[] laneAnchors; // 0,1,2
    [SerializeField] private int attackSpawnEveryOnBeats = 2;
    [SerializeField] private bool clearAttackSpawnsOnExit = true;
    [SerializeField] private float attackAutoDestroyAfter = 0f;
    [SerializeField] private bool usePoolForAttack = false;

    [Header("Spawn Position")]
    [SerializeField, Tooltip("앵커(레인 Transform) 기준 Local Offset (X,Y,Z)")]
    private Vector3 attackLocalOffset = new Vector3(0f, 0.5f, 5f); private int _attackOnBeatCounter = 0;  // 박자 카운터
    [SerializeField, Tooltip("레인별로 별도 오프셋을 주고 싶으면 체크")]
    private bool usePerLaneOffset = false;

    [SerializeField, Tooltip("usePerLaneOffset 사용 시 0/1/2 각각의 추가 오프셋")]
    private Vector3[] perLaneLocalOffsets = new Vector3[3];  // 기본값(0,0,0)
    private int _attackLaneToggle = 0;     // 0 -> lane 0, 1 -> lane 2
    private readonly System.Collections.Generic.List<GameObject> _attackSpawned = new();

    [Header("액션 이벤트(옵션)")]
    public UnityEvent OnMoveLeftSuccess;
    public UnityEvent OnMoveRightSuccess;
    public UnityEvent OnAttackSuccess;
    public UnityEvent OnSpecialSuccess;

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
    [SerializeField] private Button SceneButton;

    // ====== 자동 카운터 연습 ======
    [Header("Counter Practice (Special 단계 자동)")]
    [SerializeField] private CounterManager counterManager;
    [SerializeField, Tooltip("몇 개의 OnBeat마다 카운터를 띄울지 (예: 4 = 4박마다)")]
    private int practiceEveryOnBeats = 4;
    [SerializeField, Tooltip("힌트(!) → 지연 후 창 오픈(true) / 바로 창만 오픈(false)")]
    private bool practiceAsHint = true;
    [SerializeField] private float practiceLeadSeconds = 0.25f;
    [SerializeField] private float practiceWindowSeconds = 0.35f;

    // 내부 상태
    private Step _step;
    private int _successCount;
    private int _missCount;
    private int _attemptCount;
    private bool isWaitingPrompt = false;
    private float _stepTimeLeft = 0f;
    private int _practiceOnBeatCounter = 0;
    // 전역 ESC 잠금 (어디서든 켜고 끌 수 있게)
    public static bool EscLocked { get; private set; } = false;
    public static void LockEsc() => EscLocked = true;
    public static void UnlockEsc() => EscLocked = false;
    // ───────────────── Unity Lifecycle ─────────────────
    private void Awake()
    {
        // 필요 시 초기화
    }

    private void OnEnable()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        if (counterManager != null)
        {
            counterManager.OnCounterSuccess += OnCounterSucceeded;
            counterManager.OnCounterFail += OnCounterFailed;
        }
        BeatManager.OnBeat += OnBeat_Bridge;
    }

    private void OnDisable()
    {
        if (counterManager != null)
        {
            counterManager.OnCounterSuccess -= OnCounterSucceeded;
            counterManager.OnCounterFail -= OnCounterFailed;
        }
        BeatManager.OnBeat -= OnBeat_Bridge;
    }
    private void OnBeat_Bridge()
    {
        AttackTutorial_OnBeat();       // 공격 튜토리얼용 스폰
        HandleOnBeat_ForPractice();    // 기존 Special(카운터) 연습용
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

        // 음악을 먼저 강제 PAUSE
        SetMusicPaused(true);

        ShowPrompt(_step);  // 프롬프트 먼저
        PauseGame();        // 입력/타임스케일 정지

        // BeatManager가 같은 프레임/다음 프레임에 start()를 눌러도 다시 꺼버리기
        StartCoroutine(EnsureMusicPausedThisFrame());
    }

    private IEnumerator EnsureMusicPausedThisFrame()
    {
        yield return null;          // 다음 프레임까지 대기
        SetMusicPaused(true);       // 한 번 더 확실히 정지
    }


    private void Update()
    {
        if (isWaitingPrompt)
        {
            tutorialUIGroup?.SetActive(false);
            if (CurrentGameState != GameState.Paused) PauseGame();
            return;
        }

        // ESC 제어(프롬프트/카운트다운/페이드 중엔 차단)
        bool blockEsc = isWaitingPrompt || isResuming || EscLocked; // ✨ 추가: EscLocked
        if (!blockEsc)
        {
            if (CurrentGameState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            { CurrentGameState = GameState.Paused; PauseGame(); return; }
            else if (CurrentGameState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape) && !isResuming)
            { StartCoroutine(ResumeAfterDelay(3f)); return; }
        }

        if (CurrentGameState == GameState.Paused || _step == Step.Done) return;

        if (progressMode == ProgressMode.ByTime)
        {
            _stepTimeLeft -= Time.deltaTime;
            UpdateTimeCounterUI();
            if (_stepTimeLeft <= 0f) { GoNextStep_ByTime(); return; }
        }

        switch (_step)
        {
            case Step.Move: HandleMoveStep(); break;
            case Step.Attack: HandleAttackStep(); break;
            case Step.Special: HandleSpecialStep(); break;
        }
    }

    // ───────────────── 자동 카운터 (Special 단계 전용) ─────────────────
    private void HandleOnBeat_ForPractice()
    {
        if (isWaitingPrompt || CurrentGameState != GameState.Playing) return;
        if (_step != Step.Special) return;
        if (!counterManager) return;

        _practiceOnBeatCounter++;
        if (_practiceOnBeatCounter % Mathf.Max(1, practiceEveryOnBeats) != 0) return;

        TriggerCounterOnce();
    }

    private void TriggerCounterOnce()
    {
        if (!counterManager) return;

        if (practiceAsHint)
            counterManager.PreHint(Mathf.Max(0f, practiceLeadSeconds)); // 힌트 → 지연 후 창 오픈
        else
            counterManager.OpenWindow(practiceWindowSeconds > 0f ? practiceWindowSeconds : counterManager.WindowDuration);
    }

    // ───────────────── 각 스텝 처리 ─────────────────
    private void HandleMoveStep()
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
        else UpdateTimeCounterUI();
    }

    void HandleAttackStep()
    {
        if (Input.GetKeyDown(attackKey))
            TryJudge(OnBeatCheck(), () =>
            {
                _successCount++;
                OnAttackSuccess?.Invoke();                 // 옵션: 외부 훅
                if (playerInput) playerInput.TriggerAttackFromTutorial(); // 실제 공격
            });

        if (progressMode == ProgressMode.ByCount)
        {
            UpdateCounter(attackSuccessTarget);
            if (_successCount >= attackSuccessTarget)
            {
                var next = (specialSuccessTarget > 0) ? Step.Special : Step.Done;
                ShowPrompt(next);
            }
        }
        else UpdateTimeCounterUI();
    }

    void HandleSpecialStep()
    {
        // 남은 UI 갱신
        if (progressMode == ProgressMode.ByCount)
        {
            UpdateCounter(specialSuccessTarget);
            if (_successCount >= specialSuccessTarget) ShowPrompt(Step.Done);
        }
        else UpdateTimeCounterUI();
    }



    // CounterManager 이벤트 → Special 성공/실패 카운트
    private void OnCounterSucceeded()
    {
        if (_step != Step.Special) return;

        _attemptCount++;
        _missCount = 0;
        tipText?.gameObject.SetActive(false);

        _successCount++;
        OnSpecialSuccess?.Invoke();
        ShowFeedback("", Color.cyan);
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

    private void OnCounterFailed()
    {
        if (_step != Step.Special) return;

        _attemptCount++;
        _missCount++;
        ShowFeedback("MISS", Color.red);
        if (_missCount >= 3)
        {
            if (_step == Step.Special) ShowTip("H 키를 정박, K 키를 엇박에 눌러보세요!", Color.red);
            else ShowTip("주변 배경을 잘 살펴보세요!", Color.red);
        }
        if (progressMode == ProgressMode.ByTime) UpdateTimeCounterUI();
        else UpdateCounter(specialSuccessTarget); // 성공률 표시용으로만
    }


    // ───────────────── 판정/프롬프트 공통 ─────────────────
    bool OnBeatCheck()
    {
        var bm = BeatManager.Instance;
        return bm && bm.IsOnBeatNow();
    }

    bool OffBeatCheck()
    {
        var bm = BeatManager.Instance;
        return bm && bm.IsOffBeatNow();
    }


    private void TryJudge(bool isOnBeat, System.Action onSuccess)
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

    private void SetStep(Step s)
    {
        if (_step == Step.Attack && s != Step.Attack)
            CleanupAttackSpawns();

        _step = s;
        _successCount = 0;
        _attemptCount = 0;
        _practiceOnBeatCounter = 0;

        switch (s)
        {
            case Step.Move:
                SetUI("이동", $"[{moveLeftKey}] / [{moveRightKey}] 로 이동", 0);
                InitStepTimerIfNeeded(moveDuration);
                break;
            case Step.Attack:
                SetUI("공격", $"[{attackKey}] 로 공격", 0);
                InitStepTimerIfNeeded(attackDuration);
                _attackOnBeatCounter = 0;
                _attackLaneToggle = 0;
                break;
            case Step.Special:
                SetUI("엇박 카운터", $"엇박 타이밍에 [{CounterKey}] 를 눌러 카운터하세요.", 0);
                InitStepTimerIfNeeded(specialDuration);
                break;
            case Step.Done:
                SetUI("", "", 0, hideCounter: true);
                tutorialUIGroup?.SetActive(false);
                break;
        }

        if (progressMode == ProgressMode.ByTime) UpdateTimeCounterUI();
        else
        {
            switch (s)
            {
                case Step.Move: UpdateCounter(moveSuccessTarget); break;
                case Step.Attack: UpdateCounter(attackSuccessTarget); break;
                case Step.Special: UpdateCounter(specialSuccessTarget); break;
            }
        }
    }
    private void CleanupAttackSpawns()
    {
        if (!clearAttackSpawnsOnExit) return;
        for (int i = _attackSpawned.Count - 1; i >= 0; --i)
        {
            var go = _attackSpawned[i];
            if (!go) continue;
            if (usePoolForAttack) PoolManager.ReturnObjectToPool(go);
            else Destroy(go);
        }
        _attackSpawned.Clear();
    }

    private void InitStepTimerIfNeeded(float duration)
    {
        if (progressMode != ProgressMode.ByTime) return;
        _stepTimeLeft = Mathf.Max(0f, duration);
    }

    private void SetUI(string title, string guide, int current, bool hideCounter = false)
    {
        titleText?.SetText(title);
        guideText?.SetText(guide);
        counterText?.gameObject.SetActive(!hideCounter);

        if (!hideCounter)
        {
            if (progressMode == ProgressMode.ByCount)
                counterText?.SetText("0% / ?");
            else
                counterText?.SetText($"0% | {FormatTime(_stepTimeLeft)}");
        }

        feedbackText?.SetText("");
        tutorialUIGroup?.SetActive(true);
    }

    private void UpdateCounter(int target)
    {
        float rate = (_attemptCount > 0) ? ((float)_successCount / _attemptCount * 100f) : 0f;
        counterText?.SetText($"성공률 : {rate:0}%, 남은 횟수 : {Mathf.Max(0, target - _successCount)}");
    }

    private void UpdateTimeCounterUI()
    {
        if (!counterText) return;
        float rate = (_attemptCount > 0) ? ((float)_successCount / _attemptCount * 100f) : 0f;
        counterText.SetText($"{rate:0}% | {FormatTime(Mathf.Max(0f, _stepTimeLeft))}");
    }

    private string FormatTime(float t)
    {
        int total = Mathf.CeilToInt(t);
        int m = total / 60;
        int s = total % 60;
        return $"{m:00}:{s:00}";
    }

    private void GoNextStep_ByTime()
    {
        switch (_step)
        {
            case Step.Move: ShowPrompt(Step.Attack); break;
            case Step.Attack: ShowPrompt((specialDuration > 0f) ? Step.Special : Step.Done); break;
            case Step.Special: ShowPrompt(Step.Done); break;
        }
    }

    private void ShowFeedback(string msg, Color c)
    {
        if (!feedbackText) return;
        feedbackText.color = c;
        feedbackText.SetText(msg);
    }

    private void ShowTip(string msg, Color c)
    {
        if (!tipText) return;
        tipText.gameObject.SetActive(true);
        tipText.color = c;
        tipText.SetText(msg);
    }

    // ───────────────── 프롬프트/컨트롤 ─────────────────
    private void ShowPrompt(Step stepToStart)
    {
        _step = stepToStart;
        isWaitingPrompt = true;

        PauseGame();
        if (promptGroup) promptGroup.SetActive(true);
        if (promptContinueButton)
        {
            promptContinueButton.gameObject.SetActive(true);
            var nav = new Navigation { mode = Navigation.Mode.None };
            promptContinueButton.navigation = nav;
        }

        var es = EventSystem.current;
        if (es)
        {
            es.SetSelectedGameObject(null);
            es.sendNavigationEvents = false;
        }

        switch (stepToStart)
        {
            case Step.Move:
                promptTitle?.SetText("이동 튜토리얼");
                promptBody?.SetText($"정박에 맞춰 [{moveLeftKey}] / [{moveRightKey}] 를 눌러 이동합니다.\n계속하려면 아래 버튼을 누르세요.");
                break;
            case Step.Attack:
                promptTitle?.SetText("공격 튜토리얼");
                promptBody?.SetText($"정박에 맞춰 [{attackKey}] 를 눌러 공격합니다.\n계속하려면 아래 버튼을 누르세요.");
                break;
            case Step.Special:
                promptTitle?.SetText("카운터 튜토리얼");
                promptBody?.SetText($"엇박 타이밍에 [{CounterKey}] 를 눌러 카운터합니다.\n계속하려면 아래 버튼을 누르세요.");
                break;
            case Step.Done:
                promptTitle?.SetText("튜토리얼 완료");
                promptBody?.SetText("좋아요! 이제 실전에 들어가보죠.");
                PlayerPrefs.SetInt("TutorialCompleted", 1);
                PlayerPrefs.Save();
                SceneButton?.gameObject.SetActive(true);
                break;
        }
    }

    // UI 버튼에서 연결
    public void ConfirmPrompt()
    {
        if (_step == Step.Done) { HidePrompt(); return; }
        HidePrompt();
        ResumeGame();
        SetStep(_step);
    }

    public void HidePrompt()
    {
        isWaitingPrompt = false;
        if (promptGroup) promptGroup.SetActive(false);

        var es = EventSystem.current;
        if (es) es.sendNavigationEvents = true;

        if (_step == Step.Done)
        {
            if (SceneButton)
            {
                SceneButton.interactable = false;
            }
            SetStep(Step.Done);
        }
    }

    // ───────────────── 게임 상태 ─────────────────
    public void StartGame()
    {
        CurrentGameState = GameState.Playing;
        pausePopupUI?.SetActive(false);
        Time.timeScale = 1f;
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
        SetMusicPaused(true);            //  추가
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
        SetMusicPaused(false);           //  추가
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

    // ───────────────── Helper ─────────────────
    void SetMusicPaused(bool paused)
    {
        // FMOD Bus 제어 대신, 곧바로 BeatManager의 이벤트 인스턴스 정지
        if (BeatManager.Instance != null && BeatManager.Instance.IsMusicValid())
        {
            BeatManager.Instance.SetMusicPaused(paused);
        }
    }

    // 추가: PlayerInput 활성/비활성 전환
    private void SetPlayerInputEnabled(bool enabled)
    {
        if (playerInput != null)
            playerInput.enabled = enabled;
    }
    private void AttackTutorial_OnBeat()
    {
        // 조건: 공격 단계 & 진행 중(일시정지/프롬프트 아님)
        if (_step != Step.Attack) return;
        if (isWaitingPrompt) return;
        if (CurrentGameState != GameState.Playing) return;
        if (!attackTutorialPrefab) return;
        if (laneAnchors == null || laneAnchors.Length < 3) return;

        _attackOnBeatCounter++;
        if (_attackOnBeatCounter % Mathf.Max(1, attackSpawnEveryOnBeats) != 0) return;

        // 0 ↔ 2 번갈아
        int lane = (_attackLaneToggle == 0) ? 0 : 2;
        _attackLaneToggle ^= 1;

        SpawnAttackAtLane(lane);
    }

    private void SpawnAttackAtLane(int lane)
    {
        lane = Mathf.Clamp(lane, 0, 2);
        var anchor = laneAnchors[lane];
        if (!anchor) return;

        // Local Offset 계산 (공통 + 선택적 레인별)
        Vector3 offset = attackLocalOffset;
        if (usePerLaneOffset && perLaneLocalOffsets != null && perLaneLocalOffsets.Length > lane)
            offset += perLaneLocalOffsets[lane];

        // 앵커(레인 Transform) 로컬 기준으로 배치
        Vector3 spawnPos = anchor.TransformPoint(offset);
        Quaternion spawnRot = anchor.rotation;

        GameObject go = usePoolForAttack
            ? PoolManager.SpawnObject(attackTutorialPrefab, spawnPos, spawnRot)
            : Instantiate(attackTutorialPrefab, spawnPos, spawnRot);

        var p2 = go.GetComponent<Pattern2>();
        if (p2) p2.laneIndex = lane;

        _attackSpawned.Add(go);

        if (attackAutoDestroyAfter > 0f)
        {
            if (usePoolForAttack) StartCoroutine(Co_ReturnAfter(go, attackAutoDestroyAfter));
            else Destroy(go, attackAutoDestroyAfter);
        }
    }

    // 풀 사용 시 자동 반납용 (없으면 무시됨)
    private IEnumerator Co_ReturnAfter(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go) PoolManager.ReturnObjectToPool(go);
    }

}
