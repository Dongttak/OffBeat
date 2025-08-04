using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class PlayerInput : MonoBehaviour
{
    private int posIndex = 1; // 현재 위치 인덱스 0(왼쪽), 1(가운데), 2(오른쪽)
    public List<Transform> positions; // 라인(플레이어가 이동할 위치) 담을 리스트
    public static event Action AttackEvent; // 공격시 발동하는 이벤트
    public GameObject gun;
    public List<Transform> gunPositions;

    void Update()
    {
        // 왼쪽 이동
        if (Input.GetKeyDown(KeyCode.A))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if (posIndex > 0 && (currentBeat == BeatState.BeatType.OnBeat))
            {
                transform.DOMove(positions[--posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Left");
                if (posIndex == 1)
                {
                    gun.transform.position = gunPositions[1].position;
                }
            }
        }

        // 오른쪽 이동
        if (Input.GetKeyDown(KeyCode.D))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if (posIndex < 2 && (currentBeat == BeatState.BeatType.OnBeat))
            {
                transform.DOMove(positions[++posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Right");
                if (posIndex == 2)
                {
                    gun.transform.position = gunPositions[0].position;
                }
            }
        }

        // 공격
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if(currentBeat == BeatState.BeatType.OnBeat)
            {
                AttackEvent?.Invoke();
                Debug.Log("Attack Triggered");
            }

        }

        // 회피
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if (currentBeat == BeatState.BeatType.OnBeat)
            {
                Debug.Log("Evade Triggered");
            }
        }

        //// 왼쪽 이동
        //if (Input.GetKeyDown(KeyCode.A))
        //{
        //    if (posIndex > 0 && BeatManager.Instance.IsOnBeatNow())
        //    {
        //        transform.DOMove(positions[--posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
        //        Debug.Log("Move Left");
        //        if (posIndex == 1)
        //        {
        //            gun.transform.position = gunPositions[1].position;
        //        }

        //    }
        //}

        //// 오른쪽 이동
        //if (Input.GetKeyDown(KeyCode.D))
        //{
        //    if (posIndex < 2 && BeatManager.Instance.IsOnBeatNow())
        //    {
        //        transform.DOMove(positions[++posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
        //        Debug.Log("Move Right"); 
        //        if (posIndex == 2)
        //        {
        //            gun.transform.position = gunPositions[0].position;
        //        }
        //    }
        //}

        //// 공격
        //if (Input.GetKeyDown(KeyCode.Space) && BeatManager.Instance.IsOnBeatNow())
        //{
        //    AttackEvent?.Invoke();
        //    Debug.Log("Attack Triggered");
        //}

        //// 회피
        //if (Input.GetKeyDown(KeyCode.LeftShift) && BeatManager.Instance.IsOnBeatNow())
        //{
        //    Debug.Log("Evade Triggered");
        //}
    }
}
