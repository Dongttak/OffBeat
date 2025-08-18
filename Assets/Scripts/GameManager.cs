using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;
using FMOD.Studio;
using TMPro;

public class GameManager : MonoBehaviour
{
    Bus MusicBus;

    public static GameManager instance;
    public PlayerInput playerInput;

    public enum GameState { Playing, Paused }
    public GameState CurrentGameState { get; private set; } = GameState.Playing;
    private bool isResuming = false; // 코루틴 진행 중인지 여부

    [Header("UI Panels")]
    public GameObject pausePopupUI;

    [Header("Countdown UI")]
    public TextMeshProUGUI countdownText; // 카운트다운 텍스트
    private bool isWaitingPrompt = false;
    [Header("FMOD 경로")]
    [SerializeField] private string musicBusPath = ""; // 예: "bus:/Music" (비워두면 미사용)
#if FMOD
    private FMOD.Studio.Bus musicBus;
#endif
    private void Awake()
    {
        if (instance == null) instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        //MusicBus = RuntimeManager.GetBus("bus:/Music"); // 실제 버스 경로
    }
    private void Start()
    {
        pausePopupUI?.SetActive(false);
        countdownText?.gameObject.SetActive(false);
        StartGame();
    }
    private void Update()
    {
        if (isWaitingPrompt)
        {
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
    }
    public void StartGame()
    {
        CurrentGameState = GameState.Playing;
        pausePopupUI?.SetActive(false);
        Time.timeScale = 1f;
        SetMusicPaused(false);
        SetPlayerInputEnabled(true);
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            pausePopupUI?.SetActive(true);
        }
        Time.timeScale = 0f;
        SetMusicPaused(true);
        SetPlayerInputEnabled(false);
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
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