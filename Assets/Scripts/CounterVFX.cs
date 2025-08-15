using System.Collections;
using System.Linq;
using UnityEngine;

public class CounterVFX : MonoBehaviour
{
    [Header("DI")]
    [SerializeField] private CounterManager counterManager; // 비워두면 자동 할당
    [SerializeField] private Camera targetCamera;           // 비워두면 Camera.main

    [Header("Boss Target")]
    [SerializeField] private Transform bossTarget;          // 보스의 중심(가슴/머리 등)
    [SerializeField] private Transform bossRoot;            // 바운드 계산용 루트(보스 오브젝트)

    [Header("Auto Framing")]
    [SerializeField] private bool useAutoFraming = true;    // 보스 전체가 보이게 자동 거리 계산
    [SerializeField] private float framingPadding = 1.15f;  // 살짝 여유(1.0=딱맞춤, 1.15=15% 여유)
    [SerializeField] private bool fitByHeight = true;       // true=세로 기준, false=가로 기준
    [SerializeField] private float minDistance = 3f;        // 너무 붙지 않도록 하한
    [SerializeField] private float maxDistance = 30f;       // 너무 멀어지지 않도록 상한
    [SerializeField] private Vector3 manualOffset = new Vector3(0f, 1.6f, -8.0f); // 오토프레임 껐을 때 사용

    [Header("Timing")]
    [SerializeField] private float focusInTime = 0.22f;     // 보스 쪽으로 이동/회전 시간(실시간)
    [SerializeField] private float focusHoldTime = 0.35f;   // 포커스 유지 시간(실시간)

    [Header("Camera FOV")]
    [SerializeField] private float fovKickAmount = 8f;      // 기본 FOV에서 줄일 양(줌인 느낌)
    [SerializeField] private float fovInTime = 0.12f;
    [SerializeField] private float fovOutTime = 0.2f;

    [Header("Slow Motion (Global)")]
    [SerializeField] private float slowScale = 0.25f;       // 0.25배속 → 체감 크게
    [SerializeField] private float slowDuration = 0.9f;     // 실시간 유지 시간

    [Header("(Optional) 카메라 제어 스크립트 일시 비활성화")]
    [SerializeField] private Behaviour[] cameraControllersToDisable;

    [Header("Debug/Test")]
    [SerializeField] private bool enableTestKey = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;

    float defaultFixedDelta;
    Coroutine running;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        if (!targetCamera) targetCamera = Camera.main;
        if (!bossRoot && bossTarget) bossRoot = bossTarget.root;
        defaultFixedDelta = Time.fixedDeltaTime;
    }

    void OnEnable()
    {
        if (counterManager != null)
            counterManager.OnCounterSuccess += PlayVFX;
    }
    void OnDisable()
    {
        if (counterManager != null)
            counterManager.OnCounterSuccess -= PlayVFX;
    }

    void Update()
    {
        if (enableTestKey && Input.GetKeyDown(testKey))
            PlayVFX();
    }

    void PlayVFX()
    {
        if (!targetCamera) { Debug.LogWarning("[CounterVFX] targetCamera is null"); return; }
        if (!bossTarget) { Debug.LogWarning("[CounterVFX] bossTarget is null"); return; }

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Co_CounterCinematic());
    }

    IEnumerator Co_CounterCinematic()
    {
        Transform camTr = targetCamera.transform;
        Vector3 startPos = camTr.position;
        Quaternion startRot = camTr.rotation;
        float startFov = targetCamera.fieldOfView;

        // (1) 카메라 컨트롤 중지
        SetControllersEnabled(false);

        // (2) 프레이밍용 타깃 위치/거리 계산
        Vector3 focusPos = bossTarget.position; // 바라볼 지점
        float targetFov = Mathf.Max(1f, startFov - fovKickAmount);
        Vector3 desiredCamPos;

        if (useAutoFraming && bossRoot)
        {
            Bounds b = CalcBounds(bossRoot);
            // 보스의 높이/너비
            float height = Mathf.Max(0.01f, b.size.y);
            float width = Mathf.Max(0.01f, b.size.x);

            // 세로/가로 중 선택해서 화면에 딱 맞게 들어오도록 거리 계산
            float vfovRad = targetFov * Mathf.Deg2Rad;
            float aspect = Mathf.Max(0.01f, targetCamera.aspect);

            float sizeToFit = fitByHeight ? height : width;
            // 가로 기준이면 수평 FOV 필요
            float hfovRad = 2f * Mathf.Atan(Mathf.Tan(vfovRad * 0.5f) * aspect);
            float fovRad = fitByHeight ? vfovRad : hfovRad;

            float dist = (sizeToFit * 0.5f * framingPadding) / Mathf.Tan(fovRad * 0.5f);
            dist = Mathf.Clamp(dist, minDistance, maxDistance);

            // 현재 카메라가 보고 있던 방향을 유지한 채, 보스 뒤로 dist만큼 물러난 위치
            Vector3 dir = (camTr.position - b.center).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = (camTr.forward * -1f).normalized; // 혹시 같은 위치면 뒤로
            desiredCamPos = b.center + dir * dist;
            focusPos = b.center; // 보스 중심을 보게
        }
        else
        {
            // 수동 오프셋
            desiredCamPos = bossTarget.TransformPoint(manualOffset);
            focusPos = bossTarget.position;
        }

        // (3) 슬로모션 시작(전체 게임)
        Time.timeScale = slowScale;
        Time.fixedDeltaTime = defaultFixedDelta * slowScale;

        // (4) 이동/회전 + FOV 줌인
        float t = 0f;
        float totalIn = Mathf.Max(focusInTime, fovInTime);
        while (t < totalIn)
        {
            t += Time.unscaledDeltaTime;

            float kPos = Mathf.Clamp01(t / focusInTime);
            camTr.position = Vector3.Lerp(startPos, desiredCamPos, kPos);

            Quaternion desiredRot = Quaternion.LookRotation((focusPos - camTr.position).normalized, Vector3.up);
            camTr.rotation = Quaternion.Slerp(startRot, desiredRot, kPos);

            float kFov = Mathf.Clamp01(t / fovInTime);
            targetCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, kFov);

            yield return null;
        }

        // (5) 포커스 유지
        float hold = 0f;
        while (hold < focusHoldTime)
        {
            hold += Time.unscaledDeltaTime;

            // 유지 중에도 보스가 움직이면 조금 따라가기(부드러운 보정)
            if (useAutoFraming && bossRoot)
            {
                Bounds b = CalcBounds(bossRoot);
                focusPos = b.center;
                // 현재 거리 유지하며 중심만 따라가게
                float dist = (camTr.position - focusPos).magnitude;
                Vector3 dir = (camTr.position - focusPos).normalized;
                Vector3 followPos = focusPos + dir * dist;
                camTr.position = Vector3.Lerp(camTr.position, followPos, 0.2f);
                camTr.rotation = Quaternion.Slerp(camTr.rotation,
                    Quaternion.LookRotation((focusPos - camTr.position).normalized, Vector3.up), 0.2f);
            }

            yield return null;
        }

        // (6) 슬로모션 해제
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;

        // (7) 원위치/FOV 복귀
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

        // (8) 카메라 컨트롤 복구
        SetControllersEnabled(true);
        running = null;
    }

    Bounds CalcBounds(Transform root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0)
            return new Bounds(root.position, Vector3.one); // 없으면 기본치

        Bounds b = new Bounds(rends[0].bounds.center, rends[0].bounds.size);
        for (int i = 1; i < rends.Length; i++)
            b.Encapsulate(rends[i].bounds);
        return b;
    }

    void SetControllersEnabled(bool enabled)
    {
        if (cameraControllersToDisable == null) return;
        foreach (var c in cameraControllersToDisable)
            if (c) c.enabled = enabled;
    }
}
