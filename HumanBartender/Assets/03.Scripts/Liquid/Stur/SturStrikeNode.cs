using DG.Tweening;
using UnityEngine;
using System;

public class SturStrikeNode : MonoBehaviour
{
    [SerializeField] BoolEvent OnChangeSeqIndex;

    [SerializeField] Vector3 center;

    [SerializeField] Ease ease = Ease.InOutQuad;
    [SerializeField] float curBpm = 60;
    [SerializeField] float curBeatsPerLap = 4f;
    [SerializeField] float radius = 2f;

    double startDspTime;
    float beatDuration;

    bool isStart = false;

    double elapsed;
    double beats;
    double laps;
    float lapT;
    float easedT;
    float angle;

    Vector3 offset;

    double audioTime;
    double lastDsp;
    float dspDelta;

    public void Handle()
    {
        if (!isStart) return;

        double dsp = AudioSettings.dspTime;
        if (dsp != lastDsp)        // dsp가 갱신된 프레임
        {
            audioTime = dsp - startDspTime;
            lastDsp = dsp;
            dspDelta = 0f;
        }
        else
        {
            dspDelta += Time.unscaledDeltaTime; // 갱신 없는 프레임은 실시간으로 메움
        }

        elapsed = audioTime + dspDelta;
        if (elapsed < 0) return;

        beats = elapsed / beatDuration;
        laps = beats / curBeatsPerLap;     
        lapT = (float)(laps - Math.Floor(laps));
        angle = lapT * Mathf.PI * 2f;    
        offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        transform.position = center + offset;
    }

    public void InitToStart(Vector3 centerPos, float bpm, float beatLap, float rad)
    {
        center = centerPos;

        curBpm = bpm;
        curBeatsPerLap = beatLap;

        beatDuration = 60f / bpm;
        radius = rad;

        startDspTime = AudioSettings.dspTime + 0.1f;

        isStart = true;
    }
}
