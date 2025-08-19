using UnityEngine;

public class PatternTimelineApplier : MonoBehaviour
{
    public PatternScheduler scheduler;
    public PatternTimelineAsset timeline;

    [ContextMenu("Apply Timeline To Scheduler")]
    public void Apply()
    {
        if (scheduler && timeline)
        {
            timeline.ApplyTo(scheduler);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(scheduler);
#endif
        }
    }
}
