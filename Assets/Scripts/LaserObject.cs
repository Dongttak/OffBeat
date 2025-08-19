using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LaserObject : MonoBehaviour
{
    [SerializeField] private double beattoLaser;
    [SerializeField] private double warningbeat;

    [SerializeField] private double timetoLaser;
    [SerializeField] private double warningtime;

    public double bpm = 153;
    [SerializeField]private double beattosec;
    int beatsPassed;

    public Image img;

    private RaycastHit hit;
    private bool attackrequired;
    private Vector3 tr;

    public AudioSource audio;
    public GameObject laserEffect;

    private void Update()
    {
        timetoLaser -= Time.deltaTime;
        img.fillAmount = (float)(timetoLaser / warningtime);
    }

    private void OnEnable()
    {
        Debug.Log("Spawn Laser");
        beattosec = 60.0 / bpm;
        timetoLaser = beattosec * warningbeat;
        warningtime = beattosec * warningbeat;
        beatsPassed = 2;
        BeatManager.OnBeat += OnBeat;
    }

    IEnumerator LaserAttack()
    {
        tr = transform.position + Vector3.up;
        audio.Play();
        if (Physics.Raycast(tr, Vector3.back, out hit, 30f))
        {
            if (hit.transform.CompareTag("Player"))
            {
                //레이저에 플레이어 피격
                Debug.Log("Player Hit!");
            }
        }
        attackrequired = false;
        laserEffect.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        laserEffect.SetActive(false);
        yield return new WaitUntil(() => !audio.isPlaying);
        Destroy(gameObject);
    }
    
    void OnDisable()
    {
        BeatManager.OnBeat -= OnBeat;
    }

    void OnBeat()
    {
        beatsPassed--;
        if (beatsPassed == 0)
            StartCoroutine(LaserAttack());
    }

}
