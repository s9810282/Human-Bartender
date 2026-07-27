using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>랜덤 손님 한 명에게 배정된 4분할 파츠(body/outfit/eyes/hair) 조합. guest_bodies.json의 원본 항목을 그대로 참조한다.</summary>
public class GuestBodyAppearance
{
    public NewGuestBodyPartData Body;
    public NewGuestBodyPartData Outfit;
    public NewGuestBodyPartData Eyes;
    public NewGuestBodyPartData Hair;
}

/// <summary>
/// GuestBodyAppearance를 addressable로 로드한 결과 스프라이트를 들고 있는 컨테이너.
/// 손님이 자리를 떠날 때 Release로 addressable 핸들을 반드시 반납해야 한다.
/// </summary>
public class GuestBodySprites
{
    public Sprite Body { get; private set; }
    public Sprite Outfit { get; private set; }
    public Sprite Eyes { get; private set; }
    public Sprite Hair { get; private set; }

    AsyncOperationHandle<Sprite>? bodyHandle;
    AsyncOperationHandle<Sprite>? outfitHandle;
    AsyncOperationHandle<Sprite>? eyesHandle;
    AsyncOperationHandle<Sprite>? hairHandle;

    public void SetBody(AsyncOperationHandle<Sprite>? handle) { bodyHandle = handle; Body = handle?.Result; }
    public void SetOutfit(AsyncOperationHandle<Sprite>? handle) { outfitHandle = handle; Outfit = handle?.Result; }
    public void SetEyes(AsyncOperationHandle<Sprite>? handle) { eyesHandle = handle; Eyes = handle?.Result; }
    public void SetHair(AsyncOperationHandle<Sprite>? handle) { hairHandle = handle; Hair = handle?.Result; }

    public void Release()
    {
        ResourceLoader.ReleaseHandle(ref bodyHandle);
        ResourceLoader.ReleaseHandle(ref outfitHandle);
        ResourceLoader.ReleaseHandle(ref eyesHandle);
        ResourceLoader.ReleaseHandle(ref hairHandle);
    }
}
