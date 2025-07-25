using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private RaycastHit hit;
    public Transform firePosition;
    private bool attackRequired = false;
    
    void Awake()
    {
        PlayerInput.Attack += () => attackRequired = true;
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
        Gizmos.DrawRay(firePosition.position, transform.forward * 15f);
    }
    private void Attack()
    {
        if (Physics.Raycast(firePosition.position, Vector3.forward, out hit, 15f))
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
