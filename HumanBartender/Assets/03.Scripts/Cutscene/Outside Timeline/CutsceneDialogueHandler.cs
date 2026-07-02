using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public struct CutsceneLine
{
    public TypingData data;              // isLunaSpeak로 화자 구분
    public InteractiveEntity npcSpeaker; // NPC 줄일 때 추적할 대상(플레이어 줄이면 비워둠)
    public float delay;
}


/// <summary>
/// Timeline Signal에서 호출되어 대사 라인을 순서대로 재생하는 핸들러.
/// PlayNextLine(): 대사 재생 후 Timeline 계속 진행.
/// PlayNextLinePause(): 대사 완료 전까지 Timeline을 일시 정지하고 완료 후 재개.
/// </summary>
public class CutsceneDialogueHandler : MonoBehaviour
{
    [SerializeField] PlayableDirector director;
    [SerializeField] UIDialogueTextView view;

    [Header("Trackers")]
    [SerializeField] UIOutsideTracker playerTracker; // 늘 플레이어
    [SerializeField] UIOutsideTracker npcTracker;    // NPC 공통(대상 교체)

    [SerializeField] List<CutsceneLine> lines;

    int _index = 0;
    CancellationTokenSource _cts;

    public void Start()
    {
        _index = 0;
    }

    public void Init(List<CutsceneLine> lines)
    {
        _index = 0;
        this.lines = lines;
    }

    public void PlayNextLine()  // 타임라인 Signal에서 호출
    {
        if (lines == null || _index >= lines.Count) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        RunAsync(lines[_index++], _cts.Token).Forget();
    }
    public void PlayNextLinePause()  // 타임라인 Signal에서 호출
    {
        if (lines == null || _index >= lines.Count) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        RunAsyncPause(lines[_index++], _cts.Token).Forget();
    }
    async UniTaskVoid RunAsync(CutsceneLine line, CancellationToken token)
    {
        Logger.Log("PlayNextLine");

        if (!line.data.isLunaSpeak && line.npcSpeaker != null)
        {
            line.npcSpeaker.IsAvaliable = true;
            npcTracker.SetTrackedTarget(line.npcSpeaker);
            npcTracker.gameObject.SetActive(true);
        }

        await view.StartType(line.data);

        await UniTask.Delay(TimeSpan.FromSeconds(line.delay));

        npcTracker.StopTracking();
        playerTracker.StopTracking();
    }
    async UniTaskVoid RunAsyncPause(CutsceneLine line, CancellationToken token)
    {
        director.Pause();

        Logger.Log("PlayNextLine Pause");

        if (line.data.isLunaSpeak && line.npcSpeaker != null)
        {
            npcTracker.SetTrackedTarget(line.npcSpeaker);
            npcTracker.gameObject.SetActive(true);
        }
        await view.StartType(line.data);

        await UniTask.Delay(TimeSpan.FromSeconds(line.delay));

        npcTracker.StopTracking();
        playerTracker.StopTracking();

        if (!token.IsCancellationRequested)
            director.Resume();
    }




    void OnDisable() { _cts?.Cancel(); _cts?.Dispose(); _cts = null; }
}