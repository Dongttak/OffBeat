using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class PlayerInput : MonoBehaviour
{
    private int posIndex = 1; // ���� ��ġ �ε��� 0(����), 1(���), 2(������)
    public List<Transform> positions; // ����(�÷��̾ �̵��� ��ġ) ���� ����Ʈ
    public static event Action AttackEvent; // ���ݽ� �ߵ��ϴ� �̺�Ʈ
    public GameObject gun;
    public List<Transform> gunPositions;

    void Update()
    { 
        // ���� �̵�
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (posIndex > 0 && BeatManager.Instance.IsOnBeatNow())
            {
                transform.DOMove(positions[--posIndex].position, 0.2f).SetEase(Ease.InOutQuad); // DOTween ���� ���� �Լ�
                       // DOMove(��ǥ ��ġ, �̵����� �ɸ��� �ð�).SetEase(...)
                Debug.Log("Move Left");
                if (posIndex == 1)
                {
                    gun.transform.position = gunPositions[1].position;
                }

            }
        }

        // ������ �̵�
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (posIndex < 2 && BeatManager.Instance.IsOnBeatNow())
            {
                transform.DOMove(positions[++posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Right"); 
                if (posIndex == 2)
                {
                    gun.transform.position = gunPositions[0].position;
                }
            }
        }

        // ����
        if (Input.GetKeyDown(KeyCode.Space) && BeatManager.Instance.IsOnBeatNow())
        {
            AttackEvent?.Invoke();
            Debug.Log("Attack Triggered");
        }

        // ȸ��
        if (Input.GetKeyDown(KeyCode.LeftShift) && BeatManager.Instance.IsOnBeatNow())
        {
            Debug.Log("Evade Triggered");
        }
    }
}
