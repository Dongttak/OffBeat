using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatternTimeline", menuName = "Offbeat/Pattern Timeline")]
public class PatternTimelineAsset : ScriptableObject
{
    public List<PatternScheduler.SpawnEntry> onBeatSpawns = new();
    public List<PatternScheduler.EventEntry> onBeatEvents = new();
    public List<PatternScheduler.CounterEntry> onBeatCounters = new();
    public List<PatternScheduler.AnimatorTriggerEntry> onBeatAnimator = new();
    public List<PatternScheduler.Pattern2ToggleEntry> onBeatPattern2 = new();

    public List<PatternScheduler.SpawnEntry> offBeatSpawns = new();
    public List<PatternScheduler.EventEntry> offBeatEvents = new();
    public List<PatternScheduler.CounterEntry> offBeatCounters = new();
    public List<PatternScheduler.AnimatorTriggerEntry> offBeatAnimator = new();
    public List<PatternScheduler.Pattern2ToggleEntry> offBeatPattern2 = new();

    // Scheduler로 복사 적용
    public void ApplyTo(PatternScheduler sched)
    {
        sched.onBeatSpawns = new(onBeatSpawns);
        sched.onBeatEvents = new(onBeatEvents);
        sched.onBeatCounters = new(onBeatCounters);
        sched.onBeatAnimator = new(onBeatAnimator);
        sched.onBeatPattern2 = new(onBeatPattern2);

        sched.offBeatSpawns = new(offBeatSpawns);
        sched.offBeatEvents = new(offBeatEvents);
        sched.offBeatCounters = new(offBeatCounters);
        sched.offBeatAnimator = new(offBeatAnimator);
        sched.offBeatPattern2 = new(offBeatPattern2);
    }
}
