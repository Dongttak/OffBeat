using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class TitleBGMPlayer : MonoBehaviour
{
    [SerializeField] private EventReference titleBGM;
    private EventInstance bgm;

    void OnEnable()
    {
        if (titleBGM.IsNull) return;
        bgm = RuntimeManager.CreateInstance(titleBGM);
        bgm.start();
    }

    void OnDisable()
    {
        if (bgm.isValid())
        {
            bgm.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            bgm.release();
        }
    }
}
