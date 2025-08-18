using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Laser
{
    public float time; // 몇 초에 소환할 건지
    public int lane; // 어느 라인에 소환할 건지 0 : 왼쪽 / 1 : 중앙 / 2 : 오른쪽
    public GameObject laserPrefab; // 레이져 오브젝트
}

public class LaserSpawner : MonoBehaviour
{
    public List<Laser> laserTimeline;
    public double initTime;
    public List<Transform> laneTransform;

    void Awake()
    {
        initTime = Time.time;
    }

    void Update()
    {
        if(laserTimeline.Count > 0)
            if (Time.time >= initTime + laserTimeline[0].time)
            {
                SpawnLaser(laserTimeline[0]);
            }
    }
    void SpawnLaser(Laser l)
    {
        GameObject lo = Instantiate(l.laserPrefab, laneTransform[l.lane].position, Quaternion.identity);
        laserTimeline.Remove(l);
    }
}
