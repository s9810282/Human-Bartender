using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class OutsideElevatorRadio : InteractiveEntity
{
    [Header("Data")]
    [SerializeField] TextTagDataSO textTagData;
    [SerializeField] protected InteractableEvent OnTrackedText;

    //고민해보기
    //data는 so같은 형태로 변경 필요
    //Runner 및 Presenter DI로 받아야함 동적 생성 이유.

    [SerializeField] protected OutsideRadioDataSO radioData;
    [SerializeField] private DynamicSpeechBubble bubble;

    [SerializeField] protected bool isTalking = false;
    private CancellationTokenSource _playCts;

    public override void Interact(IInteractor player)
    {
        if (isTalking) return;

        int curday = GameStateManager.Instance.CurrentDay;
        EGameFlow curflow = GameStateManager.Instance.GameFlow;

        RadioData targetRadioData = default;
        bool found = false;
        foreach (var item in radioData.radioData.radioDatas)
        {
            if (item.Days != curday) continue;
            if (item.Route != curflow) continue;

            targetRadioData = item;
            found = true;
            break;
        }
        if (!found) return;


        Logger.Log("Start Radio");
        DynamicBubbleEffect.textTagData = textTagData;

        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        PlayRadio(targetRadioData, _playCts.Token).Forget();
    }

    public void EndInteract()
    {
        StopPlayback(); 
        OnTrackedText?.Raise(null);
        if (bubble != null) bubble.gameObject.SetActive(false);
    }

    private void StopPlayback()
    {
        isTalking = false;
        if (_playCts != null)
        {
            _playCts.Cancel();
            _playCts.Dispose();
            _playCts = null;
        }
    }

    public async UniTask PlayRadio(RadioData data, CancellationToken token)
    {
        if (data.Dialogues == null) return;

        OnTrackedText?.Raise(this);
        isTalking = true;

        try
        {
            foreach (var item in data.Dialogues)
            {
                
                token.ThrowIfCancellationRequested();

                await UniTask.Delay(TimeSpan.FromSeconds(item.Delay),
                                    cancellationToken: token);

                bubble.gameObject.SetActive(true);
                Logger.Log("Play Radio");
                await DynamicBubbleEffect.TypeSentenceTMP(
                    new TypingData(
                        item.Text,
                        null,
                        Vector2.zero,
                        Color.white,
                        true
                    ),
                    bubble,
                    token); 
            }
        }
        catch (OperationCanceledException)
        {
            // EndInteract / 파괴 등으로 중단됨 — 정상 종료 처리
        }
        finally
        {
            isTalking = false;
        }
    }

    void OnDisable()
    {
        StopPlayback();
    }
}