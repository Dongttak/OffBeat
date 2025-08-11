using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsAudioUIBinder : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void Start()
    {
        if (!GameAudioManager.Instance) return;

        musicSlider.SetValueWithoutNotify(GameAudioManager.Instance.GetMusicVolume());
        sfxSlider.SetValueWithoutNotify(GameAudioManager.Instance.GetSFXVolume());

        musicSlider.onValueChanged.AddListener(v => GameAudioManager.Instance.SetMusicVolume(v));
        sfxSlider.onValueChanged.AddListener(v => GameAudioManager.Instance.SetSFXVolume(v));

        GameAudioManager.Instance.OnVolumeChanged += (m, s) =>
        {
            musicSlider.SetValueWithoutNotify(m);
            sfxSlider.SetValueWithoutNotify(s);
        };
    }
}