using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private RaycastHit hit;
    public Transform firePosition; // 총구 위치
    private bool attackRequired = false; // 공격이 입력되었는지 확인하는 변수
    
    void Awake()
    {
        PlayerInput.AttackEvent += () => attackRequired = true;
    }

    private void LateUpdate()
    {
        if (attackRequired)
        {
            Attack();
            attackRequired = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(firePosition.position, transform.forward * 30);
    }
    private void Attack()
    {
        if (Physics.Raycast(firePosition.position, Vector3.forward, out hit, 30f))
        {
            if (hit.transform.CompareTag("Enemy"))
            {
                hit.transform.GetComponent<MeshRenderer>().material.color = Color.red;
            }
        }
        else
        {
            Debug.Log("fail");
        }
    }
}
