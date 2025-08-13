using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttackObject : MonoBehaviour
{
    private GameObject player;
    private PlayerInput playerInput;
    private int lineIndex;
    public float speed;

    private void Awake()
    {
        player = GameObject.Find("Player");
        playerInput = player.gameObject.GetComponent<PlayerInput>();
        gameObject.SetActive(false);
    }
    void Update()
    {
        transform.Translate(-Vector3.forward * Time.deltaTime * speed);
        //오브젝트가 플레이어를 지나가면
        if ((player.transform.position - gameObject.transform.position).z > 0)
        {
            //라인 이동 금지시키기
            playerInput.DeactivateTrigger(lineIndex);
            gameObject.SetActive(false);
        }
    }

    public void SetIndex(int index)
    {
        if (index == 0 || index == 2)
            lineIndex = index;
        else return;
    }
}
