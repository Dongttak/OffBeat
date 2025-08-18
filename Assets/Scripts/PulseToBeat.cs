// PulseToBeat.cs
using UnityEngine;

public class PulseToBeat : MonoBehaviour
{
    [SerializeField] float _pulseSize = 1.15f;
    [SerializeField] float _returnSpeed = 5f;
    private Vector3 _startSize;

    void Awake()
    {
        _startSize = transform.localScale;
    }

    void OnEnable()
    {
        if (BeatManager.Instance) BeatManager.Instance.RegisterPulseTarget(this);
    }

    void OnDisable()
    {
        if (BeatManager.Instance) BeatManager.Instance.UnregisterPulseTarget(this);
    }

    void Update()
    {
        // 타임스케일 무시로 부드럽게
        float dt = Time.unscaledDeltaTime;
        transform.localScale = Vector3.Lerp(transform.localScale, _startSize, dt * _returnSpeed);
    }


    public void Pulse()
    {
        transform.localScale = _startSize * _pulseSize;
    }
}
