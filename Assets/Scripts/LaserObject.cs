using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.VisualScripting;

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
    [SerializeField] private AudioSource sfx;
    [SerializeField] private GameObject laserEffect;

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
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up, Vector3.back * 30);
    }
    IEnumerator LaserAttack()
    {
        if (laserEffect) laserEffect.SetActive(true);
        if (sfx) sfx.Play();

        // 로컬 forward 기준(보스가 회전해도 맞게 나감)
        Vector3 origin = transform.position + Vector3.up;
        Vector3 dir = Vector3.back;

        yield return new WaitForSeconds(0.1f);
        if (laserEffect) laserEffect.SetActive(false);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, playerMask))
        {
            if (hit.transform.CompareTag("Player"))
            {
                // TODO: 플레이어 피격 처리(데미지/무적 체크 등)
                Debug.Log("Player Hit by Laser!");
            }
        }

        if (sfx) yield return new WaitUntil(() => !sfx.isPlaying);

        Destroy(gameObject);
    }
}
