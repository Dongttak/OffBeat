using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    private int posIndex = 1; // 현재 위치 인덱스 0(왼쪽), 1(가운데), 2(오른쪽)
    public List<Transform> positions; // 라인(플레이어가 이동할 위치) 담을 리스트
    public static event Action AttackEvent; // 공격시 발동하는 이벤트

    [SerializeField]
    private int deactivatedLine = 0;
    [SerializeField]
    private bool deactivatedMove;

    void Update()
    {
        // 왼쪽 이동
        if (Input.GetKeyDown(KeyCode.A))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if (CheckMovable(posIndex - 1) && (currentBeat == BeatState.BeatType.OnBeat))
            {
                transform.DOMove(positions[--posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Left");
            }
        }

        // 오른쪽 이동
        if (Input.GetKeyDown(KeyCode.D))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if (CheckMovable(posIndex + 1) && (currentBeat == BeatState.BeatType.OnBeat))
            {
                transform.DOMove(positions[++posIndex].position, 0.2f).SetEase(Ease.InOutQuad);
                Debug.Log("Move Right");
            }
        }

        // 공격
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BeatState.BeatType currentBeat = BeatState.Instance.CurrBeatState;

            if (currentBeat == BeatState.BeatType.OnBeat)
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
    }

    private bool CheckMovable(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex > 2) return false;

        if (!deactivatedMove)
        {
            return true;
        }
        else
        {
            if (lineIndex == deactivatedLine)
            {
                return false;
            }
            else return true;
        }
    }

    public IEnumerator MoveDeactivate(int lineIndex)
    {
        Debug.Log("Deactivate");
        deactivatedLine = lineIndex;
        deactivatedMove = true;
        yield return new WaitForSeconds(2f);
        deactivatedMove = false;
        Debug.Log("Activate");
    }
    public void DeactivateTrigger(int lineIndex)
    {
        StartCoroutine(MoveDeactivate(lineIndex));
    }
}
