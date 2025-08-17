using System.Collections;
using UnityEngine;

public class TimePhaseScheduler : MonoBehaviour
{
    [SerializeField] private CounterManager counterManager; // 옵션
    private IEnumerator Start()
    {
        // BeatManager 준비될 때까지 대기
        while (BeatManager.Instance == null || !BeatManager.Instance.IsInitialized)
            yield return null;

        // 곡 길이 90초 적용
        BeatManager.Instance.SetSongLengthSeconds(90f);

        // Phase 1: 0~60초 (120BPM)
        BeatManager.Instance.SetTempo(120f, 1f);     // stepsPerBeat=1 (4분음표)
        if (counterManager) counterManager.SetShowHints(true);
        yield return new WaitForSecondsRealtime(60f);

        // Phase 2: 60~90초 (150BPM)
        BeatManager.Instance.SetTempo(150f, 1f);
        if (counterManager) counterManager.SetShowHints(false);
        yield return new WaitForSecondsRealtime(30f);

        // 끝
        Debug.Log("[Phase] Song finished");
    }
}
