using UnityEngine;
using UnityEngine.UI;

public class OptionsAudioUIBinder : MonoBehaviour
{
    [SerializeField] private Slider mainSlider; // Min=0, Max=1, Whole Numbers 꺼짐

    void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    System.Collections.IEnumerator BindWhenReady()
    {
        // BeatManager가 Init 끝낼 때까지 대기 (씬 초기화 순서 대비)
        while (BeatManager.Instance == null || !BeatManager.Instance.IsInitialized)
            yield return null;

        var bm = BeatManager.Instance;
        mainSlider.SetValueWithoutNotify(bm.GetMainVolume());
        mainSlider.onValueChanged.RemoveAllListeners();
        mainSlider.onValueChanged.AddListener(v => bm.SetMainVolume(v));
    }
}
