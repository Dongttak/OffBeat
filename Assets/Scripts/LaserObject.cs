using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LaserObject : MonoBehaviour
{
    [SerializeField] private float timetoLaser;
    [SerializeField] private float warningtime;
    public Image img;
    private RaycastHit hit;
    private bool attackrequired;
    Vector3 tr;

    private void Update()
    {
        timetoLaser -= Time.deltaTime;
        img.fillAmount = timetoLaser / warningtime;
    }

    private void OnEnable()
    {
        Debug.Log("Spawn Laser");
        timetoLaser = warningtime;
        StartCoroutine(Pattern());
    }

    private void LateUpdate()
    {
        if (attackrequired)
        {
            LaserAttack();
        }
    }

    void OnDrawGizmos()
    {
        tr = transform.position + Vector3.up;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(tr, Vector3.back * 30);
    }

    IEnumerator Pattern()
    {
        yield return new WaitUntil(() => timetoLaser <= 0);
        attackrequired = true;
    }

    void LaserAttack()
    {
        tr = transform.position + Vector3.up;
        if (Physics.Raycast(tr, Vector3.back, out hit, 30f))
        {
            if (hit.transform.CompareTag("Player"))
            {
                //레이저에 플레이어 피격
                Debug.Log("Player Hit!");
            }
        }
        attackrequired = false;
        Destroy(gameObject);
    }
}
