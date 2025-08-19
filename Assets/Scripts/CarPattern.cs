using System.Collections;
using UnityEngine;

[AddComponentMenu("Offbeat/Pattern2/CarPattern")]
public class CarPattern : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("박자마다 전진할 거리(미터)")]
    public float stepDistance = 3.0f;

    [Tooltip("한 스텝 이동에 걸리는 시간(초)")]
    public float stepTime = 0.18f;

    [Tooltip("전진 시작 전 리드 딜레이(초) - OnBeat 후 대기")]
    public float leadDelay = 0.00f;

    [Tooltip("최대 스텝 수 (0이면 제한 없음)")]
    public int stepsMax = 0;

    [Tooltip("최대 스텝 도달 시 오브젝트 파괴")]
    public bool destroyAtEnd = true;

    [Header("Activation")]
    [Tooltip("시작 시 활성화할지")]
    public bool activeOnStart = false;

    [Header("Boss Pose (선택)")]
    public Animator bossAnimator;
    public string poseTrigger = "Pose2";

    [Header("Hit Box")]
    public LayerMask playerLayer = ~0;
    public Vector3 hitHalfExtents = new Vector3(0.7f, 1.0f, 0.7f);
    public Vector3 hitOffset = new Vector3(0f, 0.5f, 0f);
    public float hitCooldown = 0.1f;

    bool _isActive;
    bool _moving;
    int _stepsDone;
    float _lastHitTime = -999f;
    Collider[] _buf = new Collider[4];

    void OnEnable()
    {
        _isActive = activeOnStart;
        BeatManager.OnBeat += OnBeat;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= OnBeat;
        StopAllCoroutines();
    }

    public void SetActive(bool v) => _isActive = v;

    void OnBeat()
    {
        if (!_isActive || _moving) return;

        // 최대 스텝 제한
        if (stepsMax > 0 && _stepsDone >= stepsMax)
        {
            if (destroyAtEnd) Destroy(gameObject);
            return;
        }

        StartCoroutine(StepOnce());
    }

    IEnumerator StepOnce()
    {
        _moving = true;

        // 보스 포즈 트리거
        if (bossAnimator && !string.IsNullOrEmpty(poseTrigger))
            bossAnimator.SetTrigger(poseTrigger);

        if (leadDelay > 0f) yield return new WaitForSeconds(leadDelay);

        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * stepDistance;

        float t = 0f;
        _lastHitTime = -999f;

        while (t < stepTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / stepTime);
            transform.position = Vector3.Lerp(start, end, k);

            // 히트 체크
            Vector3 center = transform.position + hitOffset;
            int n = Physics.OverlapBoxNonAlloc(center, hitHalfExtents, _buf, Quaternion.identity, playerLayer);
            if (n > 0 && (Time.time - _lastHitTime) > hitCooldown)
            {
                _lastHitTime = Time.time;
                // TODO: 플레이어 피격 처리
                Debug.Log("Player Hit by car!");
            }

            yield return null;
        }

        transform.position = end;
        _stepsDone++;
        _moving = false;

        if (stepsMax > 0 && _stepsDone >= stepsMax)
        {
            if (destroyAtEnd) Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 c = transform.position + hitOffset;
        Gizmos.DrawWireCube(c, hitHalfExtents * 2f);
    }
}
