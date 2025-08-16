using UnityEngine;
using FMODUnity;
using System;

public class CounterAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CounterManager counterManager;
    [SerializeField] private Transform emitter; // 3D 사운드 기준

    [Header("Play Timing")]
    [SerializeField] private bool playOnHintOpen = true;
    [SerializeField] private bool playOnWindowOpen = false;

    [Header("FMOD Events")]
    [SerializeField] private EventReference sfxOpen;
    [SerializeField] private EventReference sfxSuccess;
    [SerializeField] private EventReference sfxFail;

    private void Awake()
    {
        if (!counterManager)
        {
            counterManager = FindObjectOfType<CounterManager>();
            if (!counterManager)
                Debug.LogWarning("[CounterAudio] CounterManager not found in scene.");
        }

        if (!emitter) emitter = transform;
    }

    private void OnEnable()
    {
        if (counterManager != null)
            RegisterEvents();
    }

    private void OnDisable()
    {
        if (counterManager != null)
            UnregisterEvents();
    }

    private void RegisterEvents()
    {
        if (playOnHintOpen)
            counterManager.OnCounterHintOpen += PlayOpen;

        if (playOnWindowOpen)
            counterManager.OnWindowOpen += HandleWindowOpen;

        counterManager.OnCounterSuccess += PlaySuccess;
        counterManager.OnCounterFail += PlayFail;
    }

    private void UnregisterEvents()
    {
        if (playOnHintOpen)
            counterManager.OnCounterHintOpen -= PlayOpen;

        if (playOnWindowOpen)
            counterManager.OnWindowOpen -= HandleWindowOpen;

        counterManager.OnCounterSuccess -= PlaySuccess;
        counterManager.OnCounterFail -= PlayFail;
    }

    // 실제 핸들러
    private void HandleWindowOpen(float _) => PlayOpen();

    private void PlayOpen()
    {
        if (IsValid(sfxOpen)) Play(sfxOpen);
    }

    private void PlaySuccess()
    {
        if (IsValid(sfxSuccess)) Play(sfxSuccess);
    }

    private void PlayFail()
    {
        if (IsValid(sfxFail)) Play(sfxFail);
    }

    private void Play(EventReference evt)
    {
        if (emitter != null)
            RuntimeManager.PlayOneShotAttached(evt, emitter.gameObject);
        else
            RuntimeManager.PlayOneShot(evt);

#if UNITY_EDITOR
        Debug.Log($"[CounterAudio] Played: {evt.Path}");
#endif
    }

    private bool IsValid(EventReference e) => !e.IsNull;
}
