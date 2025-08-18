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
        StartGame();
    }

    private void Update()
    {
        if (CurrentGameState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
        {
            CurrentGameState = GameState.Paused;
            PauseGame();
        }
        else if (CurrentGameState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape) && !isResuming)
        {
            StartCoroutine(ResumeAfterDelay(3f)); // 카운트 후 재개
        }
    }

    public void StartGame()
    {
        CurrentGameState = GameState.Playing;
        pausePopupUI.SetActive(false);
        Time.timeScale = 1;
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            pausePopupUI.SetActive(true);
        }
        Time.timeScale = 0;
        MusicBus.setPaused(true);
        playerInput.enabled = false; // 입력 비활성화
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            pausePopupUI.SetActive(false);
            CurrentGameState = GameState.Playing;
        }
        Time.timeScale = 1;
        MusicBus.setPaused(false);
        playerInput.enabled = true; // 입력 비활성화
    }

    private IEnumerator ResumeAfterDelay(float delay)
    {
        isResuming = true;
        // 카운트다운 UI 활성화
        countdownText.gameObject.SetActive(true);

        float remaining = delay;
        while (remaining > 0)
        {
            countdownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSecondsRealtime(1f);
            remaining--;
        }
        countdownText.gameObject.SetActive(false);
        isResuming = false;
        ResumeGame();
    }

    public void ReturnToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
