using System.Collections.Generic;
using UnityEngine;

public class PatternCsvLoader : MonoBehaviour
{
    [Tooltip("CSV 파일 (Resources 또는 Inspector에 넣어줌)")]
    public TextAsset csvFile;

    [Tooltip("Prefab 매핑 (type 이름 → 프리팹)")]
    public List<PrefabEntry> prefabMap;

    [Tooltip("타겟 타임라인 (없으면 자기 자신)")]
    public PatternTimeline target;

    [System.Serializable]
    public class PrefabEntry
    {
        public string type;
        public GameObject prefab;
    }

    void Awake()
    {
        csvFile = Resources.Load<TextAsset>("PatternCsv");
        if (!target) target = GetComponent<PatternTimeline>();
        if (csvFile) LoadCsvToTimeline(csvFile, target);
    }

    void LoadCsvToTimeline(TextAsset csv, PatternTimeline timeline)
    {
        var steps = new List<PatternTimeline.FixedStep>();
        var prefabDict = new Dictionary<string, GameObject>();
        foreach (var p in prefabMap) prefabDict[p.type] = p.prefab;

        string[] lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++) // 첫줄은 header
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Trim().Split(',');

            if (cols.Length < 4) continue;

            int stepIndex = int.Parse(cols[0]);
            var when = (cols[1].Trim() == "OffBeat") ?
                PatternTimeline.FixedStep.When.OffBeat :
                PatternTimeline.FixedStep.When.OnBeat;

            string type = cols[2].Trim();
            int lane = int.Parse(cols[3]);

            var step = new PatternTimeline.FixedStep
            {
                stepIndex = stepIndex - 6,
                when = when,
                lane = lane
            };

            if (prefabDict.TryGetValue(type, out var prefab))
                step.prefab = prefab;
            else
                Debug.LogWarning($"[PatternCsvLoader] CSV에서 type={type} 을 찾을 수 없습니다.");

            steps.Add(step);
        }

        timeline.steps = steps;
        // lookups 갱신
        var mi = typeof(PatternTimeline).GetMethod("RebuildLookups_Editor",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        mi?.Invoke(timeline, null);
    }
}