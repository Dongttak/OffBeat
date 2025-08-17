using UnityEngine;
using DG.Tweening;

public class PlayerInput : MonoBehaviour
{
    [Header("Lane Move")]
    [SerializeField] private Transform[] positions;   // [0]=왼, [1]=중앙, [2]=오른쪽
    [SerializeField] private float moveDuration = 0.2f;

    [Header("Attack")]
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private KeyCode attackKey = KeyCode.Space;

    [Header("Counter")]
    [SerializeField] private CounterManager counterManager;
    [SerializeField] private KeyCode counterKey = KeyCode.K;

    [Header("Beat Input Window")]
    [SerializeField] private float inputWindow = 0.12f;   // 이벤트 후 입력 허용 시간(초, unscaled)

    [SerializeField] private CounterVFX counterVFX;


    private int posIndex = 1;

    private bool onOpen, offOpen;
    private bool onConsumed, offConsumed;
    private float onCloseAt, offCloseAt;

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
        if (onOpen && Time.unscaledTime > onCloseAt) onOpen = false;
        if (offOpen && Time.unscaledTime > offCloseAt) offOpen = false;

        // 정박: 이동/공격
        if (onOpen && !onConsumed)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (counterVFX != null)
                {
                    Debug.Log("[Test] CounterVFX.PlayVFX() 실행됨");
                    counterVFX.SendMessage("PlayVFX"); // 또는 counterVFX.PlayVFX(); 이었을 수도 있음
                }
            }
            if (Input.GetKeyDown(KeyCode.A) && posIndex > 0)
            {
                onConsumed = true;
                MoveTo(posIndex - 1);
            }
            else if (Input.GetKeyDown(KeyCode.D) && posIndex < 2)
            {
                onConsumed = true;
                MoveTo(posIndex + 1);
            }
            else if (Input.GetKeyDown(attackKey))
            {
                onConsumed = true;
                if (attack) attack.Attack();
            }
        }

        // 엇박: 카운터
        if (offOpen && !offConsumed)
        {
            if (Input.GetKeyDown(counterKey))
            {
                offConsumed = true;
                bool ok = counterManager && counterManager.TryCounter();
                Debug.Log(ok ? "[Counter] SUCCESS" : "[Counter] FAIL");
            }
        }
    }

    void OpenOn()
    {
        onOpen = true; onConsumed = false;
        onCloseAt = Time.unscaledTime + inputWindow;
    }

    void OpenOff()
    {
        offOpen = true; offConsumed = false;
        offCloseAt = Time.unscaledTime + inputWindow;
    }

    void MoveTo(int next)
    {
        posIndex = next;
        transform.DOKill();
        transform.DOMove(positions[posIndex].position, moveDuration).SetEase(Ease.InOutQuad);
    }
}
