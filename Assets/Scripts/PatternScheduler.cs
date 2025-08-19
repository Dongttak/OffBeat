using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PatternScheduler
/// ------------------------------------------------------------
/// ¹ÚÀÚ ÀÎµ¦½º(Á¤¹Ú/¾ù¹Ú)¿¡ Çàµ¿À» ¸ÅÇÎÇØ¼­ ÀÚµ¿ ½ÇÇàÇØ ÁÖ´Â Å¸ÀÓ¶óÀÎ ½ºÄÉÁÙ·¯.
/// BeatManager.OnBeat / OffBeat ÀÌº¥Æ®¸¦ ±¸µ¶ÇÏ¿© ³»ºÎ ºñÆ® Ä«¿îÅÍ¸¦ Áõ°¡½ÃÅ°°í,
/// ÇöÀç ºñÆ®¿Í ¸ÅÄªµÇ´Â ¿£Æ®¸®(Spawn/Event/Counter/Animator/Pattern2 Toggle)¸¦ ½ÇÇàÇÕ´Ï´Ù.
///
/// # ¿ë¾î
/// - "Á¤¹Ú(OnBeat)"  : BeatManager°¡ ³»º¸³»´Â ¹ÚÀÚ °æ°è(0,1,2,3, ...)
/// - "¾ù¹Ú(OffBeat)" : Á¤¹ÚÀÇ Àý¹Ý À§Ä¡(0.5, 1.5, 2.5, ...)
///
/// # ºñÆ® ÁöÁ¤ ¹æ¹ý (TimelineEntryBase)
/// - beatIndex      : Àý´ë ºñÆ® ¹øÈ£(0ºÎÅÍ ½ÃÀÛ)
/// - measureBeat    : "¸¶µð:¹Ú" ¹®ÀÚ¿­. ¿¹) "3:2" ¡æ 3¸¶µð 2¹Ú(¸¶µð/¹ÚÀº 1ºÎÅÍ Ç¥±â)
///   * measureBeat°¡ ºñ¾îÀÖÁö ¾ÊÀ¸¸é beatIndexº¸´Ù ¿ì¼±ÇÕ´Ï´Ù.
/// - everyN         : ¹Ýº¹ ÁÖ±â(>=1 ÀÌ¸é firstBeatºÎÅÍ everyN ¹Ú¸¶´Ù ½ÇÇà)
/// - firstBeat      : ¹Ýº¹ ½ÃÀÛ Àý´ë ºñÆ®(±âº» 0). currentBeat°¡ firstBeat ÀÌ»óÀÏ ¶§ºÎÅÍ everyN ÁÖ±â·Î ½ÇÇà
///
/// ¿¹½Ã)
///   measureBeat="2:1", everyN=4, firstBeat=8
///   ¡æ 2¸¶µð 1¹Ú À§Ä¡·Î È¯»êµÈ 'target'°ú »ó°ü¾øÀÌ, 8, 12, 16, 20... ºñÆ®¿¡¼­ ½ÇÇà
///
/// ¡Ø ÇöÀç ±¸ÇöÀº `everyN > 0`ÀÌ¸é 'target'Àº ºñ±³¿¡ ¾²Áö ¾Ê½À´Ï´Ù(firstBeat/ everyN ±âÁØ ¹Ýº¹).
///   target¿¡¼­ 1È¸ ½ÇÇà ÈÄ ¹Ýº¹ÇÏ°í ½Í´Ù¸é, Ã¹ ¿£Æ®¸®¸¦ ´Ü¹ß¼º(everyN=0)À¸·Î Ãß°¡ÇÏ°í,
///   ÀÌ¾î¼­ ¹Ýº¹ ¿£Æ®¸®¸¦ º°µµ·Î Ãß°¡ÇÏ´Â ½ÄÀ¸·Î ±¸¼ºÇÏ¼¼¿ä.
/// ------------------------------------------------------------
/// </summary>
public class PatternScheduler : MonoBehaviour
{
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // [A] µ¥ÀÌÅÍ ±¸Á¶ (¿£Æ®¸® º£ÀÌ½º ¹× °¢ ¿£Æ®¸® Å¸ÀÔ)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    /// <summary>
    /// ¸ðµç Å¸ÀÓ¶óÀÎ ¿£Æ®¸®ÀÇ °øÅë ¿É¼Ç
    /// </summary>
    [Serializable]
    public abstract class TimelineEntryBase
    {
        [Header("Beat Address (µÑ Áß ÇÏ³ª »ç¿ë)")]
        [Tooltip("Àý´ë ºñÆ® ÀÎµ¦½º (0ºÎÅÍ ½ÃÀÛ). measureBeat°¡ ºñ¾îÀÖÀ» ¶§¸¸ »ç¿ëµË´Ï´Ù.")]
        public int beatIndex = -1;

        [Tooltip("¸¶µð/¹Ú Ç¥±â. ¿¹: \"3:2\" (3¸¶µð 2¹Ú, °¢ °ªÀº 1ºÎÅÍ ½ÃÀÛ). ¼³Á¤ ½Ã beatIndexº¸´Ù ¿ì¼±ÇÕ´Ï´Ù.")]
        public string measureBeat = "";

        [Header("Repeat (¿É¼Ç)")]
        [Tooltip(">=1ÀÌ¸é ¹Ýº¹ ½ÇÇà. 'firstBeat'ºÎÅÍ everyN ¹Ú¸¶´Ù ½ÇÇàµË´Ï´Ù.")]
        public int everyN = 0;

        [Tooltip("¹Ýº¹ ½ÃÀÛ Àý´ë ºñÆ® ÀÎµ¦½º. 0ÀÌ¸é °î ½ÃÀÛ(0ºñÆ®)¿¡¼­ ¹Ýº¹À» ½ÃÀÛÇÕ´Ï´Ù.")]
        public int firstBeat = 0;

        /// <summary>
        /// ÇöÀç ºñÆ®(currentBeat)°¡ ÀÌ ¿£Æ®¸®ÀÇ ½ÇÇà Á¶°Ç°ú ÀÏÄ¡ÇÏ´ÂÁö ÆÇÁ¤
        /// </summary>
        public bool Match(int currentBeat, int beatsPerMeasureForParsing)
        {
            // ´Ü¹ß(¹Ýº¹ ¾øÀ½)ÀÎ °æ¿ì: target(Àý´ë ºñÆ®)¿Í Á¤È®È÷ ÀÏÄ¡ÇÏ¸é ½ÇÇà
            if (everyN <= 0)
            {
                int target = ResolveBeatIndex(beatsPerMeasureForParsing);
                return currentBeat == target;
            }

            // ¹Ýº¹ÀÎ °æ¿ì: firstBeatºÎÅÍ everyN ¹Ú¸¶´Ù ½ÇÇà
            if (currentBeat < firstBeat) return false;
            return (currentBeat - firstBeat) % everyN == 0;
        }

        /// <summary>
        /// measureBeat("M:B") ¡æ Àý´ë ºñÆ®·Î È¯»ê (¾øÀ¸¸é beatIndex »ç¿ë)
        /// </summary>
        public int ResolveBeatIndex(int beatsPerMeasureForParsing)
        {
            if (!string.IsNullOrWhiteSpace(measureBeat))
            {
                if (TryParseMeasureBeat(measureBeat, beatsPerMeasureForParsing, out int idx))
                    return idx;
            }
            return Mathf.Max(0, beatIndex);
        }

        /// <summary>
        /// "¸¶µð:¹Ú"(1ºÎÅÍ ½ÃÀÛ)À» Àý´ë ºñÆ® ÀÎµ¦½º(0ºÎÅÍ)·Î º¯È¯
        /// </summary>
        public static bool TryParseMeasureBeat(string s, int beatsPerMeasure, out int resultIndex)
        {
            resultIndex = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var parts = s.Split(':');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out int measure)) return false;
            if (!int.TryParse(parts[1], out int beat)) return false;

            measure = Mathf.Max(1, measure);
            beat = Mathf.Max(1, beat);

            // Àý´ë ÀÎµ¦½º(0ºÎÅÍ): (¸¶µð-1)*¸¶µð´ç¹Ú + (¹Ú-1)
            resultIndex = (measure - 1) * beatsPerMeasure + (beat - 1);
            return true;
        }
    }

    /// <summary> Æ¯Á¤ À§Ä¡¿¡ ÇÁ¸®ÆÕÀ» »ý¼º </summary>
    [Serializable]
    public class SpawnEntry : TimelineEntryBase
    {
        [Header("Spawn")]
        [Tooltip("»ý¼ºÇÒ ÇÁ¸®ÆÕ")]
        public GameObject prefab;

        [Tooltip("½ºÆù À§Ä¡ ±âÁØÁ¡(ºñ¿ì¸é ¿ùµå ¿øÁ¡)")]
        public Transform point;

        [Tooltip("±âÁØÁ¡¿¡¼­ÀÇ À§Ä¡ ¿ÀÇÁ¼Â")]
        public Vector3 offset;

        [Tooltip("ÀÚµ¿ Á¦°Å±îÁö ´ë±â ½Ã°£(ÃÊ). 0ÀÌ¸é »èÁ¦ÇÏÁö ¾ÊÀ½")]
        public float autoDestroyAfter = 0f;
    }

    /// <summary> UnityEvent È£Ãâ </summary>
    [Serializable]
    public class EventEntry : TimelineEntryBase
    {
        [Header("Event")]
        public UnityEvent onTrigger;
    }

    /// <summary> CounterManager¿Í ¿¬µ¿: ÈùÆ®/ÆÇÁ¤Ã¢ ¿ÀÇÂ </summary>
    [Serializable]
    public class CounterEntry : TimelineEntryBase
    {
        [Header("Counter")]
        [Tooltip("true: ÈùÆ® ¿¹°í ÈÄ Ã¢ ¿ÀÇÂ / false: Áï½Ã ÆÇÁ¤Ã¢¸¸ ¿ÀÇÂ")]
        public bool openAsHint = true;

        [Tooltip("ÈùÆ® »ç¿ë ½Ã, ÈùÆ® ÈÄ Ã¢ ¿ÀÇÂ±îÁö Áö¿¬(ÃÊ)")]
        public float leadSeconds = 0.25f;

        [Tooltip("Áï½Ã ¿ÀÇÂ ½Ã Ã¢ Áö¼Ó½Ã°£(ÃÊ). 0ÀÌ¸é CounterManager ±âº»°ª »ç¿ë")]
        public float windowSeconds = 0f;
    }

    /// <summary> Animator¿¡ Trigger¸¦ ½ô </summary>
    [Serializable]
    public class AnimatorTriggerEntry : TimelineEntryBase
    {
        [Header("Animator Trigger")]
        [Tooltip("Æ®¸®°Å¸¦ º¸³¾ Animator")]
        public Animator animator;

        [Tooltip("Animator Trigger ÀÌ¸§")]
        public string trigger;
    }

    /// <summary> Pattern2(Â÷·® ÆÐÅÏ µî) on/off Åä±Û </summary>
    [Serializable]
    public class Pattern2ToggleEntry : TimelineEntryBase
    {
        [Header("Pattern2 Toggle")]
        [Tooltip("Åä±ÛÇÒ °ÔÀÓ¿ÀºêÁ§Æ®(¿¹: CarPattern ÇÁ¸®ÆÕ ·çÆ®)")]
        public GameObject car;

        [Tooltip("true=È°¼ºÈ­ / false=ºñÈ°¼ºÈ­")]
        public bool enable = true;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // [B] ÀÎ½ºÆåÅÍ ¼³Á¤°ª
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    [Header("Sync")]
    [Tooltip("¾À ½ÃÀÛ ½Ã ³»ºÎ ºñÆ® Ä«¿îÅÍ¸¦ 0À¸·Î ¸®¼ÂÇÒÁö ¿©ºÎ")]
    [SerializeField] private bool resetOnStart = true;

    [Header("Measure Helper (¿É¼Ç)")]
    [Tooltip("¸¶µð/¹Ú Ç¥±â¿¡¼­ ÇÑ ¸¶µðÀÇ ¹Ú ¼ö(¿¹: 4/4 ¡æ 4)")]
    [SerializeField] private int beatsPerMeasure = 4;

    [Header("Spawn Settings")]
    [Tooltip("½ºÆùµÈ ¿ÀºêÁ§Æ®ÀÇ ºÎ¸ð (ºñ¿öµÎ¸é ·çÆ®¿¡ »ý¼º)")]
    [SerializeField] private Transform spawnParent;

    [Header("Counter (¿É¼Ç)")]
    [SerializeField] private CounterManager counterManager;

    [Header("On-Beat Timeline (Á¤¹Ú)")]
    public List<SpawnEntry> onBeatSpawns = new();
    public List<EventEntry> onBeatEvents = new();
    public List<CounterEntry> onBeatCounters = new();
    public List<AnimatorTriggerEntry> onBeatAnimator = new();
    public List<Pattern2ToggleEntry> onBeatPattern2 = new();

    [Header("Off-Beat Timeline (¾ù¹Ú)")]
    public List<SpawnEntry> offBeatSpawns = new();
    public List<EventEntry> offBeatEvents = new();
    public List<CounterEntry> offBeatCounters = new();
    public List<AnimatorTriggerEntry> offBeatAnimator = new();
    public List<Pattern2ToggleEntry> offBeatPattern2 = new();

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // [C] ³»ºÎ »óÅÂ
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private int _onBeatIndex = -1;  // Ã¹ OnBeat¿¡¼­ 0ÀÌ µÇµµ·Ï -1·Î ½ÃÀÛ
    private int _offBeatIndex = -1;

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // [D] ¶óÀÌÇÁ»çÀÌÅ¬
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Awake()
    {
        if (!counterManager) counterManager = FindObjectOfType<CounterManager>();
    }

    private void OnEnable()
    {
        BeatManager.OnBeat += HandleOnBeat;
        BeatManager.OffBeat += HandleOffBeat;
    }

    private void OnDisable()
    {
        BeatManager.OnBeat -= HandleOnBeat;
        BeatManager.OffBeat -= HandleOffBeat;
    }

    private void Start()
    {
        if (resetOnStart) ResetTimeline();
    }

    /// <summary> ³»ºÎ Ä«¿îÅÍ¸¦ ÃÊ±âÈ­ (´ÙÀ½ OnBeat/OffBeat¿¡¼­ °¢°¢ 0ºÎÅÍ ½ÃÀÛ) </summary>
    public void ResetTimeline()
    {
        _onBeatIndex = -1;
        _offBeatIndex = -1;
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // [E] Beat ÇÚµé·¯
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void HandleOnBeat()
    {
        _onBeatIndex++;

        ExecuteSpawns(onBeatSpawns, _onBeatIndex);
        ExecuteEvents(onBeatEvents, _onBeatIndex);
        ExecuteCounters(onBeatCounters, _onBeatIndex);
        ExecuteAnimator(onBeatAnimator, _onBeatIndex);
        ExecutePattern2(onBeatPattern2, _onBeatIndex);
    }

    private void HandleOffBeat()
    {
        _offBeatIndex++;

        ExecuteSpawns(offBeatSpawns, _offBeatIndex);
        ExecuteEvents(offBeatEvents, _offBeatIndex);
        ExecuteCounters(offBeatCounters, _offBeatIndex);
        ExecuteAnimator(offBeatAnimator, _offBeatIndex);
        ExecutePattern2(offBeatPattern2, _offBeatIndex);
    }

    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // [F] ½ÇÇà±â(Executors)
    // ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡

    private void ExecuteSpawns(List<SpawnEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e.Match(beat, beatsPerMeasure)) continue;

            var pos = e.point ? e.point.position : Vector3.zero;
            var rot = e.point ? e.point.rotation : Quaternion.identity;

            var parent = spawnParent ? spawnParent : null;
            var go = Instantiate(e.prefab, pos + e.offset, rot, parent);

            if (e.autoDestroyAfter > 0f)
                Destroy(go, e.autoDestroyAfter);
        }
    }

    private void ExecuteEvents(List<EventEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e.Match(beat, beatsPerMeasure))
                e.onTrigger?.Invoke();
        }
    }

    private void ExecuteCounters(List<CounterEntry> list, int beat)
    {
        if (!counterManager) return;

        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e.Match(beat, beatsPerMeasure)) continue;

            if (e.openAsHint)
                counterManager.PreHint(e.leadSeconds);
            else
                counterManager.OpenWindow(e.windowSeconds > 0f ? e.windowSeconds : counterManager.WindowDuration);
        }
    }

    private void ExecuteAnimator(List<AnimatorTriggerEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e.Match(beat, beatsPerMeasure)) continue;

            if (e.animator && !string.IsNullOrEmpty(e.trigger))
                e.animator.SetTrigger(e.trigger);
        }
    }

    private void ExecutePattern2(List<Pattern2ToggleEntry> list, int beat)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (!e.Match(beat, beatsPerMeasure)) continue;

            if (e.car) e.car.SetActive(e.enable);
        }
    }
}
