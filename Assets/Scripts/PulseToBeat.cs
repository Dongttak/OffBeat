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
        transform.localScale = Vector3.Lerp(transform.localScale, _startSize, Time.deltaTime * _returnSpeed);
    }

    public void Pulse()
    {
        transform.localScale = _startSize * _pulseSize;
    }
}
