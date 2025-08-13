using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEditor.EditorTools;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject[] _firstSpawnObjects = new GameObject[5];
    private float _SpawnObjectZValue = 0f;

    [HideInInspector] public int backgroudnCount;
    [HideInInspector] public float backgroundLength = 33.5f;

    private void Awake()
    {
        backgroudnCount = _firstSpawnObjects.Length;
    }


    private void Start()
    {
        for (int i = 0; i < _firstSpawnObjects.Length; i++)
        {
            PoolManager.SpawnObject(_firstSpawnObjects[i], new Vector3(0f, 0f, _SpawnObjectZValue), Quaternion.identity);
            _SpawnObjectZValue += backgroundLength;
        }
    }

}