using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Laser
{
    [Tooltip("이 박자에 소환")]
    public int beattoSpawn;

    [Tooltip("레인 인덱스 (예: 0=좌, 1=중앙, 2=우)")]
    public int lane;

    [Tooltip("레이저 프리팹 (LaserObject 포함)")]
    public GameObject laserPrefab;
}

[AddComponentMenu("Offbeat/Pattern2/LaserSpawner")]
public class LaserSpawner : MonoBehaviour
{
    [Header("Timeline")]
    public List<Laser> laserTimeline = new();

    [Header("Lanes (좌/중/우 위치 Transform들)")]
    public List<Transform> laneTransform = new();

    int beatCnt;

    void OnEnable()
    {
        // 오름차순 정렬
        laserTimeline.Sort((a, b) => a.beattoSpawn.CompareTo(b.beattoSpawn));
        beatCnt = 0;
        BeatManager.OnBeat += OnBeatMethod;
    }

    void OnDisable()
    {
        BeatManager.OnBeat -= OnBeatMethod;
    }

    void OnBeatMethod()
    {
        // 같은 박자에 여러 개가 있으면 모두 소환
        while (laserTimeline.Count > 0 && laserTimeline[0].beattoSpawn == beatCnt)
        {
            SpawnLaser(laserTimeline[0]);
            laserTimeline.RemoveAt(0);
        }
        beatCnt++;
    }

    void SpawnLaser(Laser l)
    {
        if (l == null || l.laserPrefab == null) return;
        if (l.lane < 0 || l.lane >= laneTransform.Count || laneTransform[l.lane] == null)
        {
            Debug.LogWarning($"[LaserSpawner] 잘못된 레인 {l.lane}");
            return;
        }

        Instantiate(l.laserPrefab, laneTransform[l.lane].position, Quaternion.identity);
    }
}
