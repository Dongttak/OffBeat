using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("FMOD Bus Paths (프로젝트에 맞게 수정)")]
    [SerializeField] private string musicBusPath = "bus:/Music";
    [SerializeField] private string sfxBusPath   = "bus:/SFX";

    private Bus musicBus, sfxBus;

    private const string KEY_MUSIC_VOL = "vol_music";
    private const string KEY_SFX_VOL   = "vol_sfx";

    public System.Action<float,float> OnVolumeChanged; // (music, sfx)

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicBus = RuntimeManager.GetBus(musicBusPath);
        sfxBus   = RuntimeManager.GetBus(sfxBusPath);

        float music = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
        float sfx   = PlayerPrefs.GetFloat(KEY_SFX_VOL,   0.8f);
        musicBus.setVolume(music);
        sfxBus.setVolume(sfx);
    }

    public float GetMusicVolume() { musicBus.getVolume(out float v); return v; }
    public float GetSFXVolume()   { sfxBus.getVolume(out float v);   return v; }

    public void SetMusicVolume(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        musicBus.setVolume(v01);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, v01);
        OnVolumeChanged?.Invoke(v01, GetSFXVolume());
    }

    public void SetSFXVolume(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        sfxBus.setVolume(v01);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, v01);
        OnVolumeChanged?.Invoke(GetMusicVolume(), v01);
    }
}