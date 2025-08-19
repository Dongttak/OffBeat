using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class UIChanger : MonoBehaviour
{
    public Image fadeOutImage;
    public Image fadeInImage;

    private void Start()
    {
        //SoundManager.instance.PlayBGM(SoundManager.instance.lobbyBGM);
        FadeInOnInGameScene();
    }
    public void OnClickStart_tutorial()
    {
        //SoundManager.instance.PlayClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TutorialScene");
    }
    public void OnClickStart_Title()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }
    public void OnClickStartGame()
    {
        if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 1)
        {
            Time.timeScale = 1f;
            // 이미 튜토리얼을 했으면 바로 인게임
            SceneManager.LoadScene("InGame");
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("TutorialScene");
        }
    }
    public void OnClickStart_main()
    {
        //SoundManager.instance.PlayClickSound();
        Time.timeScale = 1f;
        SceneManager.LoadScene("InGame");
    }

    public void OnClickQuit()
    {
        //SoundManager.instance.PlayClickSound();
        Application.Quit();
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void FadeOut()
    {
        TutorialGameManager.LockEsc(); // ✨ 페이드 동안 ESC 완전 차단
        fadeOutImage.gameObject.SetActive(true);
        fadeOutImage.DOFade(1f, 3f)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                SceneManager.LoadScene("InGame");
            });
    }

    public void FadeInOnInGameScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;
        if (sceneName == "InGame")
        {
            if (fadeInImage == null) return;
            GameManager.LockEsc(); // ✨ 페이드 동안 ESC 완전 차단
            fadeInImage.DOFade(0f, 3f)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    fadeInImage.gameObject.SetActive(false);
                    GameManager.UnlockEsc();
                });
        }
    }
}
