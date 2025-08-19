using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Laser
{
    public int beattoSpawn; // 몇 박에 소환할 건지
    public int lane; // 어느 라인에 소환할 건지 0 : 왼쪽 / 1 : 중앙 / 2 : 오른쪽
    public GameObject laserPrefab; // 레이져 오브젝트
}

public class LaserSpawner : MonoBehaviour
{
    public List<Laser> laserTimeline;
    [SerializeField] private int beatCnt;
    public List<Transform> laneTransform;

    void Awake()
    {
        BeatManager.OnBeat += OnBeatMethod;
        beatCnt = 0;
    }
    private void OnDisable()
    {
        BeatManager.OnBeat -= OnBeatMethod;
    }

    void SpawnLaser(Laser l)
    {
        GameObject lo = Instantiate(l.laserPrefab, laneTransform[l.lane].position, Quaternion.identity);
        laserTimeline.Remove(l);
    }

    void OnBeatMethod()
    {
        if (laserTimeline.Count > 0)
            if (beatCnt == laserTimeline[0].beattoSpawn)
            {
                SpawnLaser(laserTimeline[0]);
            }
        beatCnt++;
    }
}
