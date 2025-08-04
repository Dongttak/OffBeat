using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HandleCounter : MonoBehaviour
{
    [SerializeField] private GameObject counterUI;

    private void Start()
    {
        counterUI.SetActive(false);
        StartCoroutine(SpawnCounterRoutine());
    }

    private void Update()
    {
        // 괄호로 조건 명확히
        if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)) &&
            BeatState.Instance.CurrBeatState == BeatState.BeatType.OffBeat)
        {
            counterUI.SetActive(false);
        }
    }

    private IEnumerator SpawnCounterRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        ActivateCounter();

        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForSeconds(4.0f);
            ActivateCounter();
        }
    }

    private void ActivateCounter()
    {
        // 필요한 경우 리셋이나 애니메이션도 추가 가능
        counterUI.SetActive(true);
        Debug.Log("카운터 UI 활성화");
    }
}
