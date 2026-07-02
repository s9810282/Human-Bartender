using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;
using UnityEngine.Timeline;

/// <summary>Unity Timeline 에셋 재생/정지 인터페이스.</summary>
public interface ITimeLinePlayer
{
    public void PlayTimeline(TimelineAsset timeline);
    public void StopTimeline();
}


/// <summary>대화 씬에서 캐릭터 배치/제거/개수 조회를 담당하는 인터페이스.</summary>
public interface ICharacterSetter
{
    public UniTask SetCharacterAsync(string characterId, string expression, ESlotType slotType = ESlotType.Right);
    public int GetCharacterCount();
    public void ResetCharacter(ESlotType slot);
    public void ResetCharacter();
}

/// <summary>대화 캐릭터 슬롯의 페이드 인/아웃을 담당하는 인터페이스.</summary>
public interface IDialogueFader
{
    public UniTask FadeInAsync(ESlotType slot, CancellationToken token);
    public UniTask FadeOutAsync(ESlotType slot, CancellationToken token);
}


/// <summary>단일 오브젝트의 페이드 인/아웃을 담당하는 인터페이스.</summary>
public interface IFade
{
    public UniTask FadeIn(CancellationToken token);
    public UniTask FadeOut(CancellationToken token);
}


/// <summary>인게임(Bar) 씬 카메라의 줌/이동을 담당하는 인터페이스.</summary>
public interface ICameraControl
{
    
    public void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base, UniTaskCompletionSource tcs = null);
    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base);
    public void CameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f);

    public void CameraMove(ESlotType slot, float dur = 1f);
    public void CameraMove(Vector3 pos, float dur = 1);
}

/// <summary>Outside 씬 카메라의 줌/전환을 담당하는 인터페이스.</summary>
public interface ICameraControlNew
{
    public void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base, UniTaskCompletionSource tcs = null);
    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base);
    public void TransitionCameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f, AnimationCurve curve = null);
}


/// <summary>칵테일 제조 미니게임 시작 및 결과 반환을 담당하는 인터페이스.</summary>
public interface ICocktailCraft
{
    UniTask<string> StartCraftAsync(string id);
}



/// <summary>컷씬 재생/클리어/타임라인 계속 진행을 담당하는 인터페이스.</summary>
public interface ICutScenePlayer
{
    UniTask PlayCutScene(
        string id,        
        UniTaskCompletionSource tcs = null);

    public void ClearCutScene();
    public void OnContinueTimeline();
}



/// <summary>화면 이펙트(페이드/플래시/쉐이크 등)를 비동기로 재생하는 인터페이스.</summary>
public interface IEffectPlayer
{
    UniTask PlayEffectAsync(EEffectType type, float duration = 1f, float Intensity = 0f);
}

