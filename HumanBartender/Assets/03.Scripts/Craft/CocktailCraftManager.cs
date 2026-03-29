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
    [SerializeField] CraftStationData craftStation;
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
        foreach(var item in dataSO.craftData.CraftEvents)
        {
            if(item.Id == id)
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
        if (craftEventData.AutoOpenRecipeUi)
        {
            ingredientPanel.ResetPanel();
            ingredientPanel.gameObject.SetActive(true);
        }
        curCraftEventData = craftEventData;
        TutorialData tutoData = craftEventData.Tutorial.Value;

        if (tutoData.Enabled)
        {
            TutorialStepData[] steps = tutoData.Steps;
            
            for(int i = 0; i < steps.Length; i++)
            {
                TutorialStep(steps[i]);
            }
        }

        mainCraftingTcs = new UniTaskCompletionSource<string>();

        return await mainCraftingTcs.Task;
    }

    public void TutorialStep(TutorialStepData data)
    {
        switch (data.Type)
        {
            case "highlight":

                Logger.Log("highLight를 어디에 넣으라는 거야" + data.Target);
                Logger.Log("text는 또 어디 띄우라는거임" + data.Text);

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


    //아래 두 함수는 컷씬 구조 정립 후 다시 정리하기
    private async UniTaskVoid SpawnMiniGameAsync(GameObject prefab)
    {
        UniTaskCompletionSource miniGameTcs = new UniTaskCompletionSource();
        miniGameTcs = new UniTaskCompletionSource();
        GameObject miniGameObj = null;

        await SceneTransitionManager.Instance.FadeOutAsync(1f);

        // TODO : 풀링.
        miniGameObj = Instantiate(prefab);

        IMiniGameController controller = miniGameObj.GetComponent<IMiniGameController>();
        controller.InitGame(miniGameTcs);

        SceneTransitionManager.Instance.FadeIn(3f);

        await miniGameTcs.Task;

        Destroy(miniGameObj);
        EndCraft();
    }


    /// <summary>
    /// Craft는 종료 후 다시 Main으
    /// </summary>
    public void EndCraft()
    {
        Logger.Log("컷씬 재생");
        Logger.Log("Shake 끝남 판정");
        Logger.Log("결과 판정 추후 진행 : 디폴트 B.");
        Logger.Log($"다이얼로그 재진입 {curCraftEventData.Reactions.B}");
        

        string nextDialogueId = "";

        nextDialogueId = curCraftEventData.Reactions.B.DialogueId;
        GameStateManager.Instance.CurrentGameState = GameState.Play;

        //TODO 여기서 판정하기.
        //결과에 따라 연출이 다르다면 여기서 처리하게 하는게 맞나?

        for (int i = 0; i < craftStation.targetCocktailData.Recipe.Length; i++)
        {
            
        }



        ingredientPanel.gameObject.SetActive(false);

        if (mainCraftingTcs != null)
        {
            Logger.Log("Craft tcs not null");
            mainCraftingTcs.TrySetResult(nextDialogueId);
            mainCraftingTcs = null;
        }

        curCraftEventData = default;
    }
}
