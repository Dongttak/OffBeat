using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BulletManager : MonoBehaviour
{
    public static BulletManager instance;
    public Transform activePosition; // �Ѿ� ��ȯ ��ġ
    public List<GameObject> bullets; // �Ѿ� ������Ʈ Ǯ
    private int index = 0; // �Ѿ� ����Ǯ �ε���
    public int capacity = 5; // �Ѿ� ����Ǯ ũ��
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
