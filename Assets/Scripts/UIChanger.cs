using UnityEngine;
using UnityEngine.SceneManagement;

public class UIChanger : MonoBehaviour
{
    private void Start()
    {
        //SoundManager.instance.PlayBGM(SoundManager.instance.lobbyBGM);
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
}
