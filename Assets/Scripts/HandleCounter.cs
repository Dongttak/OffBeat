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
        // ��ȣ�� ���� ��Ȯ��
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
        // �ʿ��� ��� �����̳� �ִϸ��̼ǵ� �߰� ����
        counterUI.SetActive(true);
        Debug.Log("ī���� UI Ȱ��ȭ");
    }
}
