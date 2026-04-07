using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DialogueCharacterManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CharacterAnimSO animConfig;

    [Header("Parts")]
    [SerializeField] private CharacterPart[] parts; // eyes / mouth / body 순

    [Header("Fallback")]
    [SerializeField] private string fallbackSpriteAddress = "Characters/Fallback";

    private AsyncOperationHandle<Sprite>? _handleFallback;

    private void Awake()
    {
        foreach (var part in parts)
            part.Initialize();
    }

    

    public async UniTask SetCharacterAsync(string characterId, string expression)
    {
        gameObject.SetActive(true);
        ReleaseFallback();

        var tasks = new UniTask[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            PartAnimData data       = animConfig.GetPartData(characterId, expression, parts[i].PartName);
            PartAnimData defaultData = animConfig.GetDefaultPartData(characterId, parts[i].PartName);
            tasks[i] = LoadPartWithFallbackAsync(parts[i], data, defaultData);
        }

        await UniTask.WhenAll(tasks);
    }

    /// <summary>대사 출력 시작 — OnDialogue 파트 활성화</summary>
    public void OnDialogueStart()
    {
        foreach (var part in parts)
            part.OnDialogueStart();
    }

    /// <summary>대사 출력 완료 — OnDialogue 파트 정지</summary>
    public void OnDialogueEnd()
    {
        foreach (var part in parts)
            part.OnDialogueEnd();
    }

    public void OffCharacter()
    {
        gameObject.SetActive(false);
    }

    public void ReleaseAll()
    {
        foreach (var part in parts)
            part.Release();
        ReleaseFallback();
    }

    // ── 파트 로드 ────────────────────────────────────────────────────────

    private async UniTask LoadPartWithFallbackAsync(
        CharacterPart part,
        PartAnimData  data,
        PartAnimData  defaultData)
    {
        if (data == null)
            Logger.Log("DATA null");

        if (data != null && await part.LoadAsync(data))
            return;

        if (defaultData != null && defaultData != data)
        {
            Logger.LogWarning($"[CharacterManager:{part.PartName}] 로드 실패 → default 폴백");
            if (await part.LoadAsync(defaultData))
                return;
        }

        Logger.LogWarning($"[CharacterManager:{part.PartName}] default도 없음 → fallback sprite");
        //await LoadFallbackAsync();
    }


    /// <summary>
    /// TODO : 이걸 굳이 여기서 따로 할 필요가 있는 지 검토 필요,
    /// </summary>
    /// <returns></returns>
    private async UniTask LoadFallbackAsync()
    {
        if (_handleFallback.HasValue) return;

        var handle = Addressables.LoadAssetAsync<Sprite>(fallbackSpriteAddress);
        try
        {
            await handle.ToUniTask();
            if (handle.Status == AsyncOperationStatus.Succeeded)
                _handleFallback = handle;
            else
            {
                if (handle.IsValid()) Addressables.Release(handle);
                Logger.LogError($"[CharacterManager] fallback 로드 실패: {fallbackSpriteAddress}");
            }
        }
        catch
        {
            if (handle.IsValid()) Addressables.Release(handle);
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
