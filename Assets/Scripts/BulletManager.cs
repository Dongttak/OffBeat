using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BulletManager : MonoBehaviour
{
    public static BulletManager instance;
    public Transform activePosition; // 총알 소환 위치
    public List<GameObject> bullets; // 총알 오브젝트 풀
    private int index = 0; // 총알 옵젝풀 인덱스
    public int capacity = 5; // 총알 옵젝풀 크기
    private void Awake()
    {
        if (!instance)
        {
            Destroy(instance);
        }
        instance = this;
    }

    public void Attack()
    {
        if (bullets[index].activeInHierarchy == false)
        {
            bullets[index].transform.position = activePosition.position;
            bullets[index++].SetActive(true);
            if (index >= capacity) index = 0;
        }
    }
}
