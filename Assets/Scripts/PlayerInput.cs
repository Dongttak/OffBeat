using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class PlayerInput : MonoBehaviour
{
    private int posIndex = 1;
    public List<Transform> positions;
    public static event Action Attack;
    private bool isAttack;
    
    void Update()
    { 
        // ���� �̵�
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (posIndex > 0 && BeatManager.Instance.IsOnBeatNow())
            {
                transform.DOMove(positions[--posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Left");
            }
        }

        // ������ �̵�
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (posIndex < 2 && BeatManager.Instance.IsOnBeatNow())
            {
                transform.DOMove(positions[++posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Right");
            }
        }

        // ����
        if (Input.GetKeyDown(KeyCode.Space) && BeatManager.Instance.IsOnBeatNow())
        {
            Attack?.Invoke();
            Debug.Log("Attack Triggered");
        }

        // ȸ��
        if (Input.GetKeyDown(KeyCode.LeftShift) && BeatManager.Instance.IsOnBeatNow())
        {
            Debug.Log("Evade Triggered");
        }
    }
}
