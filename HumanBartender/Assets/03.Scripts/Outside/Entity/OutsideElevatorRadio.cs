using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

/// <summary>
/// 엘리베이터 탑승 중 재생되는 라디오 대사. 현재 날짜/게임 흐름에 맞는 RadioData를 찾아 말풍선으로
/// 순차 타이핑 출력하며, EndInteract 또는 파괴 시 재생을 취소한다.
/// </summary>
public class OutsideElevatorRadio : InteractiveEntity
{
    [Header("Data")]
    [SerializeField] TextTagDataSO textTagData;
    [SerializeField] protected ITrackedbleEvent OnTrackedText;

    //고민해보기
    //data는 so같은 형태로 변경 필요
    //Runner 및 Presenter DI로 받아야함 동적 생성 이유.

    [SerializeField] protected OutsideRadioDataSO radioData;
    [SerializeField] private DynamicSpeechBubble bubble;

    [SerializeField] protected bool isTalking = false;
    private CancellationTokenSource _playCts;

    /// <summary>현재 날짜/게임 흐름에 맞는 라디오 데이터를 찾아 재생을 시작한다 (OutsideElevator.Interact에서 함께 호출됨).</summary>
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

        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy());

        PlayRadio(targetRadioData, _playCts.Token).Forget();
    }

    /// <summary>엘리베이터 도착 등으로 라디오 재생을 강제 종료하고 말풍선을 숨긴다.</summary>
    public void EndInteract()
    {
        StopPlayback();
        OnTrackedText?.Raise(null);
        if (bubble != null) bubble.gameObject.SetActive(false);
    }

    /// <summary>진행 중인 재생을 취소한다.</summary>
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

    /// <summary>라디오 대사 목록을 각 항목의 지연 시간(Delay) 후 순차적으로 말풍선에 타이핑 출력한다.</summary>
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
                
                await DialogueTypingService.TypeSentenceTMP(
                    new TypingData(
                        item.Text,
                        null,
                        Vector2.zero,
                        Color.white,
                        true
                    ),
                    bubble,
                    textTagData,
                    token: token);
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