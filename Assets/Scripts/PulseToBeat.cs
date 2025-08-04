using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseToBeat : MonoBehaviour
{
    [SerializeField] float _pulseSize = 1.15f;
    [SerializeField] float _returnSpeed = 5f;
    private Vector3 _targetScale;
    private Vector3 _originalScale;

    private void Start()
    {
        // 게임 시작 시 원래 스케일 저장
        _originalScale = transform.localScale;
        _targetScale = _originalScale; // 초기 목표 스케일은 원래 스케일
    }

    private void Update()
    {
        // 목표 스케일로 부드럽게 돌아가도록 Lerp
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _returnSpeed);
    }

    public void Pulse()
    {
        // 비트 발생 시, 목표 스케일을 커진 스케일로 설정
        _targetScale = _originalScale * _pulseSize;
        transform.localScale = _targetScale; // 즉시 스케일을 키움

        // 코루틴을 사용하여 일정 시간 후 목표 스케일을 원래 스케일로 되돌림
        // 이렇게 하면 Update()에서 다시 원래 크기로 되돌리는 Lerp가 시작됨
        StopAllCoroutines();
        StartCoroutine(ResetTargetScale());
    }

    private IEnumerator ResetTargetScale()
    {
        // 약간의 딜레이 후 원래 스케일로 돌아가기 시작
        yield return new WaitForSeconds(0.1f); // 딜레이 시간은 조절 가능
        _targetScale = _originalScale;
    }

    //[SerializeField] float _pulseSize = 1.15f;
    //[SerializeField] float _returnSpeed = 5f;
    //private Vector3 _startSize;

    //private void Start()
    //{
    //    _startSize = transform.localScale;
    //}

    //private void Update()
    //{
    //    transform.localScale = Vector3.Lerp(transform.localScale, _startSize, Time.deltaTime * _returnSpeed);
    //}

    //public void Pulse()
    //{
    //    transform.localScale = _startSize * _pulseSize;
    //}
}
