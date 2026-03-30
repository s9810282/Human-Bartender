using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DialogueCharacterManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer portraitImage;
    [SerializeField] private Animator animator;

    [Tooltip("스프라이트/애니메이션 둘 다 없을 때 사용할 더미 리소스 주소")]
    [SerializeField] private string fallbackSpriteAddress = "Characters/Fallback";

    private AsyncOperationHandle? _currentHandle;

    /// <summary>
    /// 규칙: {speaker}_{expression}
    /// 1. AnimatorController 존재 → 애니메이션 실행
    /// 2. Sprite 존재             → 이미지 교체
    /// 3. 둘 다 없음              → fallback 스프라이트 로드
    /// </summary>
    public async UniTask SetCharacterAsync(string speaker, string expression)
    {
        portraitImage.gameObject.SetActive(true);

        string address = $"{speaker}_{expression}";

        if (await TryLoadAnimatorAsync(address)) return;
        if (await TryLoadSpriteAsync(address)) return;

        Logger.LogWarning($"[CharacterManager] '{address}' 리소스 없음 → fallback 로드");
        await TryLoadSpriteAsync(fallbackSpriteAddress);
    }

    //TODO : 내일 addressable 작업하기
    private async UniTask<bool> TryLoadAnimatorAsync(string address)
    {
        var handle = Addressables.LoadAssetAsync<RuntimeAnimatorController>(address);
        await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            ReleaseCurrentHandle();
            _currentHandle = handle;

            animator.runtimeAnimatorController = handle.Result;
            animator.Play(0);
            return true;
        }

        Addressables.Release(handle);
        return false;
    }

    private async UniTask<bool> TryLoadSpriteAsync(string address)
    {
        var handle = Addressables.LoadAssetAsync<Sprite>(address);
        await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            ReleaseCurrentHandle();
            _currentHandle = handle;

            portraitImage.sprite = handle.Result;
            animator.runtimeAnimatorController = null; // 이전 애니메이션 정리
            return true;
        }

        Addressables.Release(handle);
        return false;
    }

    public void OffCharacter()
    {
        portraitImage.gameObject.SetActive(false);
    }

    public void ReleaseAll()
    {
        ReleaseCurrentHandle();
    }

    private void ReleaseCurrentHandle()
    {
        if (_currentHandle.HasValue && _currentHandle.Value.IsValid())
        {
            Addressables.Release(_currentHandle.Value);
            _currentHandle = null;
        }
    }

    private void OnDestroy() => ReleaseAll();
}
