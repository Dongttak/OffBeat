using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundAction : MonoBehaviour
{
    private Spawner _spawner;

    // 이동 속도 조절 변수
    public float speed = 5.0f;

    private void Awake()
    {
        _spawner = FindObjectOfType<Spawner>();
    }


    void Update()
    {
        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
    }


    private void OnTriggerEnter(Collider other)
    {
        // 일정 시간 이동 후 Remover에 접촉 시 pool에 반환
        if (other.gameObject.CompareTag("Remover"))
        {
            //PoolManager.ReturnObjectToPool(gameObject);

            float moveDistance = _spawner.backgroudnCount * _spawner.backgroundLength;

            transform.position += new Vector3(0f, 0f, moveDistance);
        }
    }

    private void OnEnable()
    {
        //_spawner.lastSpawnedObject = gameObject;

        for (int i = 0; i <= 8; i++)
        {
            BeatManager.Instance.pulseTargets.Add(transform.GetChild(i).GetComponent<PulseToBeat>());
        }

    }

    private void OnDisable()
    {
        //BeatManager.Instance.pulseTargets.RemoveRange(0, 9);
    }
}
