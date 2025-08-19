using System;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("FMOD Paths (VCA 우선, 없으면 Bus 폴백)")]
    [SerializeField] private string musicVcaPath = "vca:/BGM";
    [SerializeField] private string sfxVcaPath   = "vca:/SFX";
    private VCA musicVca, sfxVca;
    private Bus musicBus, sfxBus, masterBus;

    private const string KEY_MUSIC_VOL = "vol_music";
    private const string KEY_SFX_VOL   = "vol_sfx";

    private float _musicVol = 0.8f;
    private float _sfxVol   = 0.8f;

    public event Action<float,float> OnVolumeChanged; // (music, sfx)

    public bool IsInitialized { get; private set; }
    public float MusicVolume => _musicVol;
    public float SFXVolume   => _sfxVol;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 저장값 로드(씬 전환에도 유지)
        _musicVol = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.8f);
        _sfxVol   = PlayerPrefs.GetFloat(KEY_SFX_VOL,   0.8f);

        RefreshHandles();
        ApplyVolumes();

        IsInitialized = true; // 핸들 바인딩/적용 완료 시점
        // 초기 브로드캐스트는 구독 타이밍 이슈 생길 수 있어 생략하거나, 바인더에서 강제 동기화합니다.
        // OnVolumeChanged?.Invoke(_musicVol, _sfxVol);
    }

    public void RefreshHandles()
    {
        musicVca = string.IsNullOrEmpty(musicVcaPath) ? default : RuntimeManager.GetVCA(musicVcaPath);
        sfxVca   = string.IsNullOrEmpty(sfxVcaPath)   ? default : RuntimeManager.GetVCA(sfxVcaPath);    }

    private void ApplyVolumes()
    {
        // Music: VCA 우선 → Bus → Master
        if (musicVca.isValid())        musicVca.setVolume(_musicVol);
        else if (musicBus.isValid())   musicBus.setVolume(_musicVol);
        else if (masterBus.isValid())  masterBus.setVolume(_musicVol); // 최후 폴백(주의)

        // SFX: VCA 우선 → Bus → Master(주의)
        if (sfxVca.isValid())          sfxVca.setVolume(_sfxVol);
        else if (sfxBus.isValid())     sfxBus.setVolume(_sfxVol);
        else if (masterBus.isValid())  masterBus.setVolume(Mathf.Max(_musicVol, _sfxVol)); // 충돌 최소화
    }

    // ------ 외부 API ------
    public void SetMusicVolume(float v01)
    {
        _musicVol = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL, _musicVol);
        PlayerPrefs.Save(); // 즉시 저장

        if (IsInitialized) ApplyVolumes();
        OnVolumeChanged?.Invoke(_musicVol, _sfxVol);
    }

    public void SetSFXVolume(float v01)
    {
        _sfxVol = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(KEY_SFX_VOL, _sfxVol);
        PlayerPrefs.Save(); // 즉시 저장

        if (IsInitialized) ApplyVolumes();
        OnVolumeChanged?.Invoke(_musicVol, _sfxVol);
    }
}