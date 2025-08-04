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
        // ���� ���� �� ���� ������ ����
        _originalScale = transform.localScale;
        _targetScale = _originalScale; // �ʱ� ��ǥ �������� ���� ������
    }

    private void Update()
    {
        // ��ǥ �����Ϸ� �ε巴�� ���ư����� Lerp
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _returnSpeed);
    }

    public void Pulse()
    {
        // ��Ʈ �߻� ��, ��ǥ �������� Ŀ�� �����Ϸ� ����
        _targetScale = _originalScale * _pulseSize;
        transform.localScale = _targetScale; // ��� �������� Ű��

        // �ڷ�ƾ�� ����Ͽ� ���� �ð� �� ��ǥ �������� ���� �����Ϸ� �ǵ���
        // �̷��� �ϸ� Update()���� �ٽ� ���� ũ��� �ǵ����� Lerp�� ���۵�
        StopAllCoroutines();
        StartCoroutine(ResetTargetScale());
    }

    private IEnumerator ResetTargetScale()
    {
        // �ణ�� ������ �� ���� �����Ϸ� ���ư��� ����
        yield return new WaitForSeconds(0.1f); // ������ �ð��� ���� ����
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
