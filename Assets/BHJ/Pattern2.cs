using UnityEngine;
using System.Collections;

public class Pattern2 : MonoBehaviour, IHittable
{
    [Header("Lane")]
    [Range(0, 2)] public int laneIndex;   // 0:좌, 1:중, 2:우 (실제론 0/2만 사용)

    [Header("Break Settings")]
    public int hitsToBreak = 3;          
    public float timeToBlock = 4.0f;     // 유예시간(착지 후부터 계산)
    public float lockDuration = 4.0f;    

    [Header("Spawn Fall (낙하 연출)")]
    public float spawnFromHeight = 6f;    // 위에서 얼마나 떨어질지
    public float fallDuration = 0.5f;     // 요청대로 0.5초
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0,0,1,1);
    public ParticleSystem landingVFX;     // 착지 먼지 등(선택)
    public AudioClip landingSfx;          // 착지 소리(선택)
    [Range(0,1)] public float landingSfxVolume = 0.9f;

    [Header("Visual")]
    public Renderer colorRenderer;
    public Color telegraphColor = new Color(1, 0, 0, 0.4f);
    public Color hitTintColor = Color.red;
    [Range(0f, 1f)] public float hitTintIntensity = 0.6f;

    [Header("VFX/SFX on SUCCESS (제시간 내 파괴)")]
    public ParticleSystem successExplosionPrefab;
    public AudioClip successSfx;
    [Range(0, 1)] public float successSfxVolume = 0.9f;

    [Header("VFX/SFX on TIMEOUT (유예 초과 → 봉인)")]
    public ParticleSystem timeoutExplosionPrefab;
    public AudioClip timeoutSfx;
    [Range(0, 1)] public float timeoutSfxVolume = 0.9f;
    public Transform vfxSpawnPoint;

    int _hits;
    bool _blocked;
    bool _destroyed;
    bool _landed; // 착지 여부
    MaterialPropertyBlock _mpb;
    Collider[] _colliders;

    void Awake()
    {
        if (!colorRenderer) colorRenderer = GetComponentInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>(includeInactive:true);
        _mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        // 경고색 적용
        ApplyBaseColor(telegraphColor);

        // 낙하 시작: 현재 위치를 착지 지점으로 사용
        StartCoroutine(FallInThenArmTimer());
    }

    IEnumerator FallInThenArmTimer()
    {
        Vector3 targetPos = transform.position;
        Vector3 startPos = targetPos + Vector3.up * spawnFromHeight;

        // 낙하 중에는 충돌 꺼두기
        SetCollidersEnabled(false);
        transform.position = startPos;

        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / fallDuration);
            float k = fallCurve != null ? fallCurve.Evaluate(u) : u;
            transform.position = Vector3.LerpUnclamped(startPos, targetPos, k);
            yield return null;
        }

        transform.position = targetPos;
        _landed = true;

        // 착지 연출
        SpawnVFX(landingVFX);
        PlaySfx(landingSfx, landingSfxVolume);

        // 충돌 가능하게
        SetCollidersEnabled(true);

        // "착지한 시점"부터 유예 타이머 시작
        if (timeToBlock > 0f) Invoke(nameof(ApplyBlockIfAlive), timeToBlock);
    }

    void SetCollidersEnabled(bool on)
    {
        if (_colliders == null) return;
        foreach (var c in _colliders) if (c) c.enabled = on;
    }

    void ApplyBlockIfAlive()
    {
        if (_destroyed) return;
        if (_hits < hitsToBreak && !_blocked)
        {
            _blocked = true;
            LaneLockManager.Instance?.LockLane(laneIndex, lockDuration);
            PlayTimeoutVFXThenVanish();
        }
    }

    public void Hit(int damage = 1)
    {
        // 낙하 중에는 히트 무시 (원하면 제거)
        if (!_landed || _destroyed) return;

        _hits += Mathf.Max(1, damage);

        if (colorRenderer)
        {
            float p = Mathf.Clamp01((_hits / (float)hitsToBreak) * hitTintIntensity);
            Color mixed = Color.Lerp(telegraphColor, hitTintColor, p);
            ApplyBaseColor(mixed);
        }

        if (_hits >= hitsToBreak)
        {
            _destroyed = true;
            CancelInvoke(nameof(ApplyBlockIfAlive));
            PlaySuccessVFXThenVanish();
        }
    }

    void PlaySuccessVFXThenVanish()
    {
        SpawnVFX(successExplosionPrefab);
        PlaySfx(successSfx, successSfxVolume);
        DisableVisualsAndColliders();
        Destroy(gameObject, 0.02f);
    }

    void PlayTimeoutVFXThenVanish()
    {
        SpawnVFX(timeoutExplosionPrefab);
        PlaySfx(timeoutSfx, timeoutSfxVolume);
        DisableVisualsAndColliders();
        Destroy(gameObject, 0.02f);
    }

    void SpawnVFX(ParticleSystem prefab)
    {
        if (!prefab) return;
        Vector3 pos = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
        Instantiate(prefab, pos, Quaternion.identity);
    }

    void PlaySfx(AudioClip clip, float vol)
    {
        if (!clip) return;
        Vector3 pos = vfxSpawnPoint ? vfxSpawnPoint.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, pos, vol);
    }

    void DisableVisualsAndColliders()
    {
        if (colorRenderer) colorRenderer.enabled = false;
        SetCollidersEnabled(false);
    }

    void ApplyBaseColor(Color c)
    {
        if (!colorRenderer) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        colorRenderer.GetPropertyBlock(_mpb);
        if (colorRenderer.sharedMaterial && colorRenderer.sharedMaterial.HasProperty("_BaseColor"))
            _mpb.SetColor("_BaseColor", c);  // URP
        else
            _mpb.SetColor("_Color", c);      // Built-in
        colorRenderer.SetPropertyBlock(_mpb);
    }
}
