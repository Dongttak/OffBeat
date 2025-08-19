using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OptionsAudioUIBinder : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider; // Min=0, Max=1, Whole Numbers 꺼짐
    [SerializeField] private Slider sfxSlider; // Min=0, Max=1, Whole Numbers 꺼짐
    private bool bound;

    void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    IEnumerator BindWhenReady()
    {
        if (!bgmSlider) Debug.LogError("[AudioBinder] bgmSlider 미할당", this);
        if (!sfxSlider) Debug.LogError("[AudioBinder] sfxSlider 미할당", this);

        while (GameAudioManager.Instance == null || !GameAudioManager.Instance.IsInitialized)
            yield return null;

        var am = GameAudioManager.Instance;

        // (1) 구독 전에 슬라이더를 현재 값으로 '강제 동기화'
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(am.MusicVolume);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(am.SFXVolume);

        // (2) 기존 리스너 제거 후 재바인딩
        if (bgmSlider)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(am.SetMusicVolume);
        }
        if (sfxSlider)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(am.SetSFXVolume);
        }

        // (3) 외부 변경 동기화 구독(중복 방지)
        am.OnVolumeChanged -= OnVolumesChanged;
        am.OnVolumeChanged += OnVolumesChanged;
        bound = true;

        // (4) 혹시라도 초기 그려짐이 어긋난 경우 한 번 더 동기화
        OnVolumesChanged(am.MusicVolume, am.SFXVolume);
    }

    private void OnDisable()
    {
        var am = GameAudioManager.Instance;
        if (am != null && bound)
        {
            am.OnVolumeChanged -= OnVolumesChanged;
            bound = false;
        }
    }

    private void OnVolumesChanged(float music, float sfx)
    {
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(music);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(sfx);
    }
}
