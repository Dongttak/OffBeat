using UnityEngine;

public class BeatState : MonoBehaviour
{
    public static BeatState Instance { get; private set; }

    public enum BeatType { OnBeat, OffBeat, Miss}
    public BeatType CurrBeatState { get; set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        CurrBeatState = BeatType.Miss;
    }
}
