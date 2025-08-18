using System;
using UnityEngine;
using DG.Tweening;

public class PlayerInput : MonoBehaviour
{
    public static event Action AttackEvent;

    [Header("Lane Move")]
    [SerializeField] private Transform[] positions;   // [0]=왼, [1]=중앙, [2]=오른쪽
    [SerializeField] private float moveDuration = 0.2f;

    [Header("Attack")]
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;

    [Header("Counter")]
    [SerializeField] private CounterManager counterManager;
    [SerializeField] private KeyCode counterKey = KeyCode.K;

    [Header("Beat Input Window (판정창 길이)")]
    [Tooltip("OnBeat/OffBeat 이벤트 이후, 즉시 입력을 허용하는 시간(초, unscaled). 0.16 ~ 0.20 정도 추천")]
    [SerializeField] private float inputWindow = 0.18f;

    [Header("Input Buffer (놓치면 다음 박자에 실행)")]
    [Tooltip("버퍼 유지 시간(초). 이 시간 안에 들어온 입력은 다음 박자에 자동 실행")]

    [SerializeField] private float bufferHold = 0.25f;

    [Header("Optional test VFX")]
    [SerializeField] private CounterVFX counterVFX;

    private int posIndex = 1;

    // 창 상태
    private bool onOpen, offOpen;
    private bool onConsumed, offConsumed;
    private float onCloseAt, offCloseAt;

    // ── 입력 버퍼 ─────────────────────────────────────────
    struct BufferedCmd
    {
        public bool flag;
        public float t;  // 저장된 시각 (unscaled)
        public void Set() { flag = true; t = Time.unscaledTime; }
        public bool IsValid(float hold) => flag && (Time.unscaledTime - t) <= hold;
        public void Clear() { flag = false; }
    }
    private BufferedCmd bufLeft, bufRight, bufAttack, bufCounter;

    void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
        if (!attack) attack = FindObjectOfType<PlayerAttack>();
    }

    void OnEnable()
    {
        BeatManager.OnBeat += OpenOn;
        BeatManager.OffBeat += OpenOff;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= OpenOn;
        BeatManager.OffBeat -= OpenOff;
    }

    void Update()
    {
        // 1) 언제든지 입력을 버퍼에 저장 (창이 닫혀있어도 저장)
        if (Input.GetKeyDown(KeyCode.A)) bufLeft.Set();
        if (Input.GetKeyDown(KeyCode.D)) bufRight.Set();
        if (Input.GetKeyDown(attackKey)) bufAttack.Set();
        if (Input.GetKeyDown(counterKey)) bufCounter.Set();

        // (옵션) 테스트 VFX
        if (Input.GetKeyDown(KeyCode.T) && counterVFX != null)
            counterVFX.PlayVFX();

        // 2) 창 시간 관리(슬로모션 영향 없음)
        if (onOpen && Time.unscaledTime > onCloseAt) onOpen = false;
        if (offOpen && Time.unscaledTime > offCloseAt) offOpen = false;

        // 3) 정박 창 열려 있으면 즉시 처리 (각 창당 1회만)
        if (onOpen && !onConsumed)
        {
            if (TryConsumeOnBeatImmediate()) onConsumed = true;
        }

        // 4) 엇박 창 열려 있으면 즉시 처리
        if (offOpen && !offConsumed)
        {
            if (TryConsumeOffBeatImmediate()) offConsumed = true;
        }

        // 5) 창이 닫힌 상태에서도 버퍼는 유지됨.
        //    다음 OnBeat/OffBeat가 열릴 때 OpenOn/OpenOff 내부에서 자동으로 소진됨.
    }

    // ── 창 오픈 시점 처리 ─────────────────────────────────
    void OpenOn()
    {
        onOpen = true; onConsumed = false;
        onCloseAt = Time.unscaledTime + inputWindow;

        // 창이 열리는 순간 버퍼에 저장된 입력이 있으면 즉시 소진
        if (!onConsumed && TryConsumeOnBeatBuffered()) onConsumed = true;
    }

    void OpenOff()
    {
        offOpen = true; offConsumed = false;
        offCloseAt = Time.unscaledTime + inputWindow;

        if (!offConsumed && TryConsumeOffBeatBuffered()) offConsumed = true;
    }

    // ── 즉시 소비(창 열려 있을 때, 키다운 우선) ─────────────────
    bool TryConsumeOnBeatImmediate()
    {
        // 키가 지금 막 눌렸다면 최우선
        if (Input.GetKeyDown(KeyCode.A) && posIndex > 0) { MoveTo(posIndex - 1); return true; }
        if (Input.GetKeyDown(KeyCode.D) && posIndex < 2) { MoveTo(posIndex + 1); return true; }
        if (Input.GetKeyDown(attackKey)) { DoAttack(); return true; }

        // 바로 눌린 건 없지만 버퍼가 살아있다면 즉시 소비
        return TryConsumeOnBeatBuffered();
    }

    bool TryConsumeOffBeatImmediate()
    {
        if (Input.GetKeyDown(counterKey)) { DoCounter(); return true; }
        return TryConsumeOffBeatBuffered();
    }

    // ── 버퍼 소비(창이 막 열렸을 때나, 즉시 입력이 없을 때) ──────
    bool TryConsumeOnBeatBuffered()
    {
        bool consumed = false;
        // 이동 입력 우선 → 그 다음 공격 (우선순위는 취향대로 바꿔도 됨)
        if (!consumed && bufLeft.IsValid(bufferHold) && posIndex > 0) { MoveTo(posIndex - 1); bufLeft.Clear(); consumed = true; }
        if (!consumed && bufRight.IsValid(bufferHold) && posIndex < 2) { MoveTo(posIndex + 1); bufRight.Clear(); consumed = true; }
        if (!consumed && bufAttack.IsValid(bufferHold)) { DoAttack(); bufAttack.Clear(); consumed = true; }

        // 소비 못 했어도 버퍼는 유지 → 다음 비트에서 또 시도
        return consumed;
    }

    bool TryConsumeOffBeatBuffered()
    {
        if (bufCounter.IsValid(bufferHold))
        {
            DoCounter();
            bufCounter.Clear();
            return true;
        }
        return false;
    }

    // ── 실행 동작 ─────────────────────────────────────────
    void MoveTo(int next)
    {
        posIndex = next;
        transform.DOKill();
        transform.DOMove(positions[posIndex].position, moveDuration).SetEase(Ease.InOutQuad);
    }

    void DoAttack()
    {
        AttackEvent?.Invoke(); // AttackResolver가 듣는 이벤트
        if (attack) attack.Attack();
    }

    void DoCounter()
    {
        bool ok = counterManager && counterManager.TryCounter();
        Debug.Log(ok ? "[Counter] SUCCESS" : "[Counter] FAIL");
    }
    public void TriggerAttackFromTutorial()
    {
        DoAttack(); // 내부에서 AttackEvent?.Invoke() + attack.Attack() 처리
    }
    public void TriggerCounterFromTutorial() 
    { 
        DoCounter(); 
    }
}