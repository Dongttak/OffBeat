using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// ▼ FMOD
using FMODUnity;
using FMOD.Studio;

[AddComponentMenu("Offbeat/Pattern2/LaserObject")]
public class LaserObject : MonoBehaviour
{
    [Header("Beat Timing")]
    [Tooltip("경고 후 몇 박 뒤 발사할지")]
    [SerializeField] private int warningBeats = 2;

    [Tooltip("BeatManager가 없으면 이 값을 사용")]
    [SerializeField] private float fallbackBpm = 153f;

    [Header("UI (옵션)")]
    [SerializeField] private Image gauge; // 경고 게이지(0~1). 비워두면 미사용

    [Header("FX/Audio (옵션)")]
    [SerializeField] private GameObject laserEffect;

    // ▼▼▼ 여기부터 FMOD 전용 오디오 설정 ▼▼▼
    [Header("FMOD SFX")]
    [Tooltip("레이저 발사 사운드 FMOD 이벤트")]
    [SerializeField] private EventReference fireSfxEvent;

    [Tooltip("오디오가 끝날 때까지 파괴 지연(필요 시만 ON)")]
    [SerializeField] private bool waitForSfxEnd = false;

    private EventInstance _sfxInst;
    private bool _sfxInstValid = false;
    // ▲▲▲ FMOD 전용 오디오 설정 끝 ▲▲▲

    [Header("Hit Test")]
    [SerializeField] private LayerMask playerMask = ~0;
    [SerializeField] private float rayDistance = 30f;

    int beatsRemaining;
    bool fired;

    void OnEnable()
    {
        // 경고 시작
        beatsRemaining = Mathf.Max(1, warningBeats);
        fired = false;
        UpdateGauge();

        BeatManager.OnBeat += OnBeat;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= OnBeat;

        // FMOD 인스턴스 정리
        if (_sfxInstValid)
        {
            _sfxInst.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _sfxInst.release();
            _sfxInstValid = false;
        }
    }

    void OnBeat()
    {
        if (fired) return;

        beatsRemaining--;
        UpdateGauge();

        if (beatsRemaining <= 0)
        {
            fired = true;
            StartCoroutine(LaserAttack());
        }
    }

    void UpdateGauge()
    {
        if (!gauge) return;
        gauge.fillAmount = Mathf.Clamp01((float)beatsRemaining / Mathf.Max(1, warningBeats));
    }

    IEnumerator LaserAttack()
    {
        if (laserEffect) laserEffect.SetActive(true);

        // ▼ FMOD 사운드 재생
        PlayFMODSfx();

        // 로컬 forward 기준
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, playerMask))
        {
            if (hit.transform.CompareTag("Player"))
            {
                // TODO: 플레이어 피격 처리
                Debug.Log("Player Hit by Laser!");
            }
        }

        // 발광 이펙트 짧게
        yield return new WaitForSeconds(0.1f);
        if (laserEffect) laserEffect.SetActive(false);

        // (옵션) FMOD 재생 종료까지 대기
        if (waitForSfxEnd)
            yield return WaitFMODSfxEnd();

        Destroy(gameObject);
    }

    // ───────── FMOD helpers
    void PlayFMODSfx()
    {
        if (fireSfxEvent.IsNull) return;

        _sfxInst = RuntimeManager.CreateInstance(fireSfxEvent);
        // 3D 위치/방향 지정 (트랜스폼 따라감이 필요하면 Update에서 set3DAttributes 반복 호출)
        _sfxInst.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        _sfxInst.start();

        if (waitForSfxEnd)
        {
            // 끝날 때까지 잡고 있다가 release
            _sfxInstValid = true;
        }
        else
        {
            // 원샷처럼 바로 해제(사운드는 계속 재생됨)
            _sfxInst.release();
            _sfxInstValid = false;
        }
    }

    IEnumerator WaitFMODSfxEnd()
    {
        if (!_sfxInstValid) yield break;

        PLAYBACK_STATE st;
        // STOPPING/STOPPED가 될 때까지 대기
        while (true)
        {
            if (!_sfxInst.isValid()) break;
            _sfxInst.getPlaybackState(out st);
            if (st == PLAYBACK_STATE.STOPPED || st == PLAYBACK_STATE.STOPPING) break;
            yield return null;
        }
        if (_sfxInst.isValid()) _sfxInst.release();
        _sfxInstValid = false;
    }
}
