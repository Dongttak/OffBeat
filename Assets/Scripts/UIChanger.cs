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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
