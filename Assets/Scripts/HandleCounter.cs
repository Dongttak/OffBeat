using UnityEngine;

public class HandleCounter : MonoBehaviour
{
    [SerializeField] private CounterManager mgr;
    [Header("Test Keys")]
    [SerializeField] private KeyCode hintKey = KeyCode.H;
    [SerializeField] private KeyCode openKey = KeyCode.J;

    void Awake()
    {
        if (!mgr) mgr = FindObjectOfType<CounterManager>();
    }

    void Update()
    {
        if (!mgr) return;

        if (Input.GetKeyDown(hintKey))
        {
            // 힌트 띄우고 0.3초 후 실제 창 오픈
            mgr.PreHint(0.3f);
            Debug.Log("[Counter] PreHint called");
        }

        if (Input.GetKeyDown(openKey))
        {
            // 바로 판정창만 열기(예: Phase2)
            mgr.OpenWindow(mgr.WindowDuration);
            Debug.Log("[Counter] OpenWindow called");
        }
    }
}
