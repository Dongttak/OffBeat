using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FmodManager : MonoBehaviour
{
    public static FmodManager Instance { get; private set; }

    [SerializeField] private EventReference musicEvent;

    private EventInstance musicInstance;
    private bool isPlaying;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (isPlaying) return;
        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();
        isPlaying = true;
    }

    public void StopMusic()
    {
        if (!isPlaying) return;
        musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        musicInstance.release();
        isPlaying = false;
    }

    public void SetVolume(float volume)
    {
        musicInstance.setVolume(volume);
    }
}
