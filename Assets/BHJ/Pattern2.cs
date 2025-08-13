using UnityEngine;

public class Pattern2 : MonoBehaviour, IHittable
{
    [Header("Lane")]
    [Range(0, 2)] public int laneIndex;   // 0:좌, 1:중, 2:우 실제론 0/2만 사용)

    [Header("Break Settings")]
    public int hitsToBreak = 3;          // 맞춰야 깨짐
    public float timeToBlock = 4.0f;     // 유예시간 경과 시 봉인
    public float lockDuration = 4.0f;    // 봉인 유지시간

    [Header("Visual")]
    public Renderer colorRenderer;       // Sphere의 MeshRenderer 할당
    public Color telegraphColor = new Color(1, 0, 0, 0.4f); // 소환 직후 경고색
    public Color hitTintColor = Color.red;                  // 피격 누적 틴트
    [Range(0f, 1f)] public float hitTintIntensity = 0.6f;    // 피격 때 섞을 정도

    [Header("VFX/SFX on SUCCESS (제시간 내 파괴)")]
    public ParticleSystem successExplosionPrefab;  // 성공 이펙트
    public AudioClip successSfx;
    [Range(0, 1)] public float successSfxVolume = 0.9f;

    [Header("VFX/SFX on TIMEOUT (유예 초과 → 봉인)")]
    public ParticleSystem timeoutExplosionPrefab;  // 실패 이펙트
    public AudioClip timeoutSfx;
    [Range(0, 1)] public float timeoutSfxVolume = 0.9f;
    public Transform vfxSpawnPoint;               // 없으면 자기 transform 사용

    int _hits;
    bool _blocked;
    bool _destroyed;
    MaterialPropertyBlock _mpb;

    void Start()
    {
        if (!colorRenderer) colorRenderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
        ApplyBaseColor(telegraphColor);

        // 봉인 타이머 시작
        if (timeToBlock > 0f) Invoke(nameof(ApplyBlockIfAlive), timeToBlock);
    }

    void ApplyBlockIfAlive()
    {
        if (_destroyed) return;                  // 이미 파괴된 경우 무시
        if (_hits < hitsToBreak && !_blocked)
        {
            _blocked = true;
            LaneLockManager.Instance?.LockLane(laneIndex, lockDuration);
            PlayTimeoutVFXThenVanish();          // 실패 이펙트
        }
    }

    public void Hit(int damage = 1)
    {
        if (_destroyed) return;
        _hits += Mathf.Max(1, damage);

        // 피격 누적 색상
        if (colorRenderer)
        {
            Color mixed = Color.Lerp(telegraphColor, hitTintColor, Mathf.Clamp01((_hits / (float)hitsToBreak) * hitTintIntensity));
            ApplyBaseColor(mixed);
        }

        if (_hits >= hitsToBreak)
        {
            // 성공: 제시간 내 파괴(봉인 전)
            _destroyed = true;
            CancelInvoke(nameof(ApplyBlockIfAlive)); // 혹시 남아있을 예약 제거
            PlaySuccessVFXThenVanish();              // 성공 이펙트
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
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
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
