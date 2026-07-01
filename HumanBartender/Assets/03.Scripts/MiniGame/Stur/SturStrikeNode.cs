using DG.Tweening;
using UnityEngine;
using System;

public class SturStrikeNode : MonoBehaviour
{
    [SerializeField] Vector3 center;

    [SerializeField] Ease ease = Ease.InOutQuad;
    [SerializeField] float curBpm = 60;
    [SerializeField] float curBeatsPerLap = 4f;
    [SerializeField] float radiusX = 2.4f;
    [SerializeField] float radiusY = 2.0f;

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
        if (dsp != lastDsp)   
        {
            audioTime = dsp - startDspTime;
            lastDsp = dsp;
            dspDelta = 0f;
        }
        else
        {
            dspDelta += Time.unscaledDeltaTime; 
        }

        elapsed = audioTime + dspDelta;
        if (elapsed < 0) return;

        UpdatePosition();
    }

    public void UpdatePosition()
    {
        beats = elapsed / beatDuration;
        laps = beats / curBeatsPerLap;
        lapT = (float)(laps - Math.Floor(laps));

        angle = lapT * Mathf.PI * 2f;
        offset = new Vector3(
            Mathf.Cos(-angle) * radiusX,
            Mathf.Sin(-angle) * radiusY,
            0f
        );

        transform.position = center + offset;
    }

    public void Init(Vector3 centerPos, float bpm, float beatLap, float radX, float radY)
    {
        center = centerPos;
        curBpm = bpm;
        curBeatsPerLap = beatLap;
        beatDuration = 60f / bpm;
        radiusX = radX;
        radiusY = radY;

        UpdatePosition();
    }

    public void InitToStart()
    {
        startDspTime = AudioSettings.dspTime + 0.1f;
        isStart = true;
    }
}
