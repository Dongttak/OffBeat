using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAttack : MonoBehaviour
{
    [SerializeField]
    private GameObject AttackObject;
    public List<Transform> lines;

    private void Awake()
    {
        AttackObject = transform.GetChild(0).gameObject;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Attack3();
        }
    }

    private void Attack3()
    {
        BossAttackObject bao = AttackObject.GetComponent<BossAttackObject>();
        int lineIndex = Random.Range(0, 2);
        switch (lineIndex)
        {
            case 0:
                AttackObject.transform.position = lines[0].position;
                bao.SetIndex(0);
                break;
            case 1:
                AttackObject.transform.position = lines[1].position;
                bao.SetIndex(2);
                break;
        }
        AttackObject.SetActive(true);
    }
}
