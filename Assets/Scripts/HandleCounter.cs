using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HandleCounter : MonoBehaviour
{
    public BeatManager BeatManager;

    public GameObject Counter;

    void Start()
    {
        Counter.GetComponent<GameObject>().SetActive(false);

        StartCoroutine(SpawnCounterRoutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)
            && BeatState.Instance.CurrBeatState == BeatState.BeatType.OffBeat)
        {
            Counter.GetComponent<GameObject>().SetActive(false);
        }
    }

    IEnumerator SpawnCounterRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        Counter.GetComponent<GameObject>().SetActive(true);

        yield return new WaitForSeconds(4.0f);
        Counter.GetComponent<GameObject>().SetActive(true);

        yield return new WaitForSeconds(4.0f);
        Counter.GetComponent<GameObject>().SetActive(true);

        yield return new WaitForSeconds(4.0f);
        Counter.GetComponent<GameObject>().SetActive(true);

        yield return new WaitForSeconds(4.0f);
        Counter.GetComponent<GameObject>().SetActive(true);

    }

}
