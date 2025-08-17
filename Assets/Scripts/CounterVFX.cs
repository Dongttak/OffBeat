using System.Collections;
using UnityEngine;

public class CounterVFX : MonoBehaviour
{
    [Header("DI")]
    [SerializeField] private CounterManager counterManager;
    [SerializeField] private Camera targetCamera;

    [Header("Boss Target")]
    [SerializeField] private Transform bossTarget;      // 보스의 중심(머리/가슴 등)
    [SerializeField] private Transform bossRoot;        // 보스 전체 렌더러 포함 루트

    [Header("Auto Framing")]
    [SerializeField] private bool useAutoFraming = true;
    [SerializeField] private float framingPadding = 1.15f;  // 1=딱, 1.15=여유
    [SerializeField] private bool fitByHeight = true;       // true: 세로 기준, false: 가로 기준
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 30f;
    [SerializeField] private Vector3 manualOffset = new Vector3(0f, 1.6f, -8f);

    [Header("Aim (LookAt)")]
    [SerializeField] private float aimYOffset = 0f;         // 보스 중심 대비 위(+)/아래(-) 보정

    [Header("Bounds Filter (for Auto Framing)")]
    [SerializeField] private LayerMask boundsLayer = ~0;    // 보스 레이어만 켜두면 안정적
    [SerializeField] private string requiredTag = "";       // 보스 렌더러에 "Boss" 태그가 있다면 지정

    [Header("Stabilize Shot")]
    [SerializeField] private bool freezeBoundsDuringShot = true; // 연출 중 바운드/중심 고정
    [SerializeField] private MonoBehaviour[] shakersToDisable;   // PulseToBeat 등 흔들림 스크립트

    [Header("Timing")]
    [SerializeField] private float focusInTime = 0.22f;
    [SerializeField] private float focusHoldTime = 0.35f;

    [Header("Camera FOV")]
    [SerializeField] private float fovKickAmount = 8f;
    [SerializeField] private float fovInTime = 0.12f;
    [SerializeField] private float fovOutTime = 0.2f;

    [Header("(Optional) Pause Camera Controllers")]
    [SerializeField] private Behaviour[] cameraControllersToDisable;

    [Header("Debug/Test")]
    [SerializeField] private bool enableTestKey = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;

    Coroutine running;

    // 캐시(안정화용)
    Bounds _cachedBounds;
    Vector3 _cachedCenter;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        if (!targetCamera) targetCamera = Camera.main;
        if (!bossRoot && bossTarget) bossRoot = bossTarget.root;
    }

    void OnEnable()
    {
        if (counterManager) counterManager.OnCounterSuccess += PlayVFX;
    }

    void OnDisable()
    {
        if (counterManager) counterManager.OnCounterSuccess -= PlayVFX;
    }

    void Update()
    {
        if (enableTestKey && Input.GetKeyDown(testKey))
            PlayVFX();
    }

    /// <summary>외부/테스트에서 호출 가능</summary>
    public void PlayVFX()
    {
        if (!targetCamera || !bossTarget) return;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Co_VFX());
    }

    IEnumerator Co_VFX()
    {
        var camTr = targetCamera.transform;
        Vector3 startPos = camTr.position;
        Quaternion startRot = camTr.rotation;
        float startFov = targetCamera.fieldOfView;

        // 컨트롤/흔들림 정지
        SetControllersEnabled(false);
        SetShakersEnabled(false);

        // 중심/바운드 계산
        Vector3 centerForShot = bossTarget.position;
        if (useAutoFraming && bossRoot)
        {
            var b = CalcBoundsFiltered(bossRoot);
            centerForShot = b.center;
            if (freezeBoundsDuringShot) { _cachedBounds = b; _cachedCenter = b.center; }
        }

        // 목표 FOV
        float targetFov = Mathf.Max(1f, startFov - fovKickAmount);

        // 거리/위치/시선 결정
        Vector3 desiredPos;
        Vector3 focusPos;

        if (useAutoFraming && bossRoot)
        {
            var b = freezeBoundsDuringShot ? _cachedBounds : CalcBoundsFiltered(bossRoot);
            float height = Mathf.Max(0.01f, b.size.y);
            float width = Mathf.Max(0.01f, b.size.x);

            float vfovRad = targetFov * Mathf.Deg2Rad;
            float aspect = Mathf.Max(0.01f, targetCamera.aspect);
            float hfovRad = 2f * Mathf.Atan(Mathf.Tan(vfovRad * 0.5f) * aspect);
            float fovRad = fitByHeight ? vfovRad : hfovRad;

            float sizeToFit = fitByHeight ? height : width;
            float dist = (sizeToFit * 0.5f * framingPadding) / Mathf.Tan(fovRad * 0.5f);
            dist = Mathf.Clamp(dist, minDistance, maxDistance);

            // 수평 방향만 사용 (Y는 현재 카메라 높이 유지)
            Vector3 cen = freezeBoundsDuringShot ? _cachedCenter : b.center;
            Vector3 toCam = camTr.position - cen; // 현재 카메라에서 중심으로
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-4f)
            {
                Vector3 fb = -camTr.forward; fb.y = 0f;
                if (fb.sqrMagnitude < 1e-4f) fb = Vector3.back;
                toCam = fb.normalized;
            }
            else toCam.Normalize();

            desiredPos = new Vector3(cen.x, startPos.y, cen.z) + toCam * dist;

            // 보는 지점은 중심 + 오프셋(위/아래 보정)
            focusPos = new Vector3(cen.x, cen.y + aimYOffset, cen.z);
        }
        else
        {
            Vector3 targetWorld = bossTarget.TransformPoint(manualOffset);
            desiredPos = new Vector3(targetWorld.x, startPos.y, targetWorld.z);

            Vector3 tp = bossTarget.position;
            focusPos = new Vector3(tp.x, tp.y + aimYOffset, tp.z);
        }

        // 이동/회전 + 줌인
        float t = 0f;
        float totalIn = Mathf.Max(focusInTime, fovInTime);
        while (t < totalIn)
        {
            t += Time.unscaledDeltaTime;

            float kp = Mathf.Clamp01(t / focusInTime);
            camTr.position = Vector3.Lerp(startPos, desiredPos, kp);

            Quaternion look = Quaternion.LookRotation((focusPos - camTr.position).normalized, Vector3.up);
            camTr.rotation = Quaternion.Slerp(startRot, look, kp);

            float kf = Mathf.Clamp01(t / fovInTime);
            targetCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, kf);

            yield return null;
        }

        // 유지 중에도 약간 추적(중심/시선만 보정, 수평 거리 유지)
        float hold = 0f;
        while (hold < focusHoldTime)
        {
            hold += Time.unscaledDeltaTime;

            if (useAutoFraming && bossRoot)
            {
                Vector3 cen = freezeBoundsDuringShot ? _cachedCenter : CalcBoundsFiltered(bossRoot).center;
                Vector3 targetFocus = new Vector3(cen.x, cen.y + aimYOffset, cen.z);
                focusPos = Vector3.Lerp(focusPos, targetFocus, 0.15f);

                // 현재 수평 거리 유지하며 부드럽게 따라가기
                float curDist = Vector3.Distance(
                    new Vector3(camTr.position.x, 0f, camTr.position.z),
                    new Vector3(focusPos.x, 0f, focusPos.z)
                );
                Vector3 toCam2 = camTr.position - new Vector3(focusPos.x, camTr.position.y, focusPos.z);
                toCam2.y = 0f;

                if (toCam2.sqrMagnitude > 1e-4f)
                {
                    Vector3 follow = new Vector3(focusPos.x, camTr.position.y, focusPos.z) + toCam2.normalized * curDist;
                    camTr.position = Vector3.Lerp(camTr.position, follow, 0.15f);
                }

                camTr.rotation = Quaternion.Slerp(
                    camTr.rotation,
                    Quaternion.LookRotation((focusPos - camTr.position).normalized, Vector3.up),
                    0.15f
                );
            }

            yield return null;
        }

        // 복귀
        float t2 = 0f;
        while (t2 < fovOutTime)
        {
            t2 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t2 / fovOutTime);
            camTr.position = Vector3.Lerp(camTr.position, startPos, k);
            camTr.rotation = Quaternion.Slerp(camTr.rotation, startRot, k);
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, startFov, k);
            yield return null;
        }

        camTr.position = startPos;
        camTr.rotation = startRot;
        targetCamera.fieldOfView = startFov;

        // 복구
        SetControllersEnabled(true);
        SetShakersEnabled(true);
        running = null;
    }

    // ───────────────────────── helpers ─────────────────────────

    Bounds CalcBoundsFiltered(Transform root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        Renderer first = null;
        Bounds b = new Bounds(root.position, Vector3.one);

        foreach (var r in rends)
        {
            if (r == null || !r.enabled) continue;

            // 레이어 필터
            if ((boundsLayer.value & (1 << r.gameObject.layer)) == 0) continue;

            // 태그 필터(옵션)
            if (!string.IsNullOrEmpty(requiredTag) && !r.CompareTag(requiredTag)) continue;

            if (first == null)
            {
                first = r;
                b = new Bounds(r.bounds.center, r.bounds.size);
            }
            else
            {
                b.Encapsulate(r.bounds);
            }
        }

        if (first == null) return new Bounds(root.position, Vector3.one);
        return b;
    }

    void SetControllersEnabled(bool enabled)
    {
        if (cameraControllersToDisable == null) return;
        foreach (var c in cameraControllersToDisable)
            if (c) c.enabled = enabled;
    }

    void SetShakersEnabled(bool enabled)
    {
        if (shakersToDisable == null) return;
        foreach (var m in shakersToDisable)
            if (m) m.enabled = enabled;
    }
}