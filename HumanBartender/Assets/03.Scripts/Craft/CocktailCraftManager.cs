using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using VContainer;
using Cysharp.Threading.Tasks;


// 💡 모든 미니게임 매니저는 이 인터페이스를 상속받아야 합니다.
public interface IMiniGameController
{
    // "매니저가 널 띄우면, 이 TCS를 받고 초기화해라!" 라는 공통 명령
    void InitGame(UniTaskCompletionSource tcs);
}

public class CocktailCraftManager : MonoBehaviour
{
    [SerializeField] CraftDataSO dataSO;
    [SerializeField] IngredientPanel ingredientPanel;

    [Header("MiniGame Prefabs")]
    [SerializeField] private GameObject shakePrefab;
    [SerializeField] private GameObject stirPrefab;
    [SerializeField] private GameObject buildPrefab;

    [SerializeField] CraftEventData curCraftEventData;
    private UniTaskCompletionSource<string> mainCraftingTcs;

    public void Start()
    {

    }

    public CraftEventData GetCraftDataByID(string id)
    {
        foreach(var item in dataSO.craftData.craft_events)
        {
            if(item.id == id)
                return item;
        }
        
        return default;
    }


    /// <summary>
    /// Command 에서 호출 되는 함수
    /// </summary>
    /// <param name="craftEventData"></param>
    public async UniTask<string> StartCraftAsync(CraftEventData craftEventData)
    {
        if (craftEventData.auto_open_recipe_ui)
        {
            ingredientPanel.ResetPanel();
            ingredientPanel.gameObject.SetActive(true);
        }
        curCraftEventData = craftEventData;
        TutorialData tutoData = craftEventData.tutorial;

        if (tutoData.enabled)
        {
            TutorialStepData[] steps = tutoData.steps;
            
            for(int i = 0; i < steps.Length; i++)
            {
                TutorialStep(steps[i]);
            }
        }

        await UniTask.Yield();

        mainCraftingTcs = new UniTaskCompletionSource<string>();

        return await mainCraftingTcs.Task;
    }

    public void TutorialStep(TutorialStepData data)
    {
        switch (data.type)
        {
            case "highlight":

                Logger.Log("highLight를 어디에 넣으라는 거야" + data.target);
                Logger.Log("text는 또 어디 띄우라는거임" + data.text);

                break;
        }
    }


    public void ProcessCraftResult()
    {
        EndCraft();
    }

    /// <summary>
    /// 연출 있다는데 그건 그때 넣어야되는거고
    /// </summary>
    public void StartBuild()
    {
        SceneTransitionManager.Instance.LoadScene("Build", LoadSceneMode.Additive);

    }
    public void StartShake()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);
        SpawnMiniGameAsync(shakePrefab).Forget();
    }
    public void StartStur()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;

        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);
    }

    private async UniTaskVoid SpawnMiniGameAsync(GameObject prefab)
    {
        // 1. 버튼에 맞는 미니게임 프리팹 생성
        GameObject miniGameObj = Instantiate(prefab);
        IMiniGameController controller = miniGameObj.GetComponent<IMiniGameController>();

        // 2. 미니게임 전용 대기열(TCS) 생성 및 전달
        UniTaskCompletionSource miniGameTcs = new UniTaskCompletionSource();
        controller.InitGame(miniGameTcs);

        // 3. 해당 미니게임 프리팹이 끝날 때까지 여기서 대기
        await miniGameTcs.Task;

        // 4. 미니게임이 끝났으므로 최종 결과 처리 함수로 이동!
        EndCraft();
    }

    /// <summary>
    /// Craft는 종료 후 다시 Main으로 돌아왔을 때 호출할 필드이긴한데...
    /// </summary>
    public void EndCraft()
    {
        Logger.Log("Shake 끝남 판정");
        Logger.Log("결과 판정 추후 진행 : 디폴트 B.");
        Logger.Log($"다이얼로그 재진입 {curCraftEventData.reactions.B}");
        Logger.Log("컷씬 재생");

        string nextDialogueId = curCraftEventData.reactions.B;
        GameStateManager.Instance.CurrentGameState = GameState.Play;

        ingredientPanel.gameObject.SetActive(false);

        if (mainCraftingTcs != null)
        {
            mainCraftingTcs.TrySetResult(nextDialogueId);
            mainCraftingTcs = null;
        }

        curCraftEventData = default;
    }
}
