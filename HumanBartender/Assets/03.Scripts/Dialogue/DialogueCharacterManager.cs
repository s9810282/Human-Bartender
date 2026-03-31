using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DialogueCharacterManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer portraitImage;

    [Tooltip("Body / Eyes / Mouth 순으로 등록")]
    [SerializeField] private CharacterPart[] parts;

    [Tooltip("리소스가 전혀 없을 때 사용할 fallback 스프라이트 주소")]
    [SerializeField] private string fallbackSpriteAddress = "Characters/Fallback";

    private AsyncOperationHandle? _handleFallback;

    private void Awake()
    {
        foreach (var part in parts)
            part.Initialize();
    }

    /// <summary>
    /// 모든 파트 애니메이션을 병렬 로드.
    /// 파트별로 리소스가 없으면 해당 파트만 비활성 처리.
    /// </summary>
    public async UniTask SetCharacterAsync(string speaker, string expression)
    {
        portraitImage.gameObject.SetActive(true);
        ReleaseFallback();

        var tasks = new UniTask[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            tasks[i] = parts[i].LoadAsync(speaker, expression);

        await UniTask.WhenAll(tasks);

        // 모든 파트가 비활성이면 fallback 스프라이트 표시
        if (!AnyPartActive())
        {
            Logger.LogWarning($"[CharacterManager] '{speaker}_{expression}' 파트 리소스 없음 → fallback");
            await LoadFallbackAsync();
        }
    }

    public void OffCharacter()
    {
        portraitImage.gameObject.SetActive(false);
    }

    public void ReleaseAll()
    {
        foreach (var part in parts)
            part.Release();

        ReleaseFallback();
    }

    private bool AnyPartActive()
    {
        foreach (var part in parts)
            if (part.Animator.enabled) return true;

        return false;
    }

    private async UniTask LoadFallbackAsync()
    {
        var handle = Addressables.LoadAssetAsync<Sprite>(fallbackSpriteAddress);
        await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _handleFallback = handle;
            portraitImage.sprite = handle.Result;
        }
        else
        {
            Addressables.Release(handle);
            Logger.LogError($"[CharacterManager] fallback 로드 실패: {fallbackSpriteAddress}");
        }
    }

    private void ReleaseFallback()
    {
        if (_handleFallback.HasValue && _handleFallback.Value.IsValid())
        {
            Addressables.Release(_handleFallback.Value);
            _handleFallback = null;
        }
    }

    private void OnDestroy() => ReleaseAll();
}
