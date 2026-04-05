using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using VContainer;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using System;


public interface IMiniGameController
{
    void InitGame(UniTaskCompletionSource tcs);
    void EndMiniGame();
}

public class CocktailCraftManager : MonoBehaviour
{
    [SerializeField] CraftDataSO craftDataSO;
    [SerializeField] CocktailDataSO cocktailDataSO;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] IngredientPanel ingredientPanel;

    [Inject] CutSceneManager cutSceneManager;

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
        foreach(var item in craftDataSO.craftData.CraftEvents)
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
        //빌드 시에는 컷씬연출로 대체. 이후 재료에 따라 수정될 거 같음.

    }
    public void StartShake()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        SpawnMiniGameAsync("shake", shakePrefab).Forget();
    }
    public void StartStur()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        SpawnMiniGameAsync("stir", stirPrefab).Forget();
    }


    //아래 두 함수는 컷씬 구조 정립 후 다시 정리하기
    private async UniTaskVoid SpawnMiniGameAsync(string style, GameObject prefab)
    {
        UniTaskCompletionSource miniGameEndTcs = new UniTaskCompletionSource();
        UniTaskCompletionSource miniGameInitTcs = new UniTaskCompletionSource();

        GameObject miniGameObj = null;

        //TODO : 여기도 진입할 때 컷씬 재생
        cutSceneManager.PlayCutSceneAsync
            (curCraftEventData.CraftCutscenes.craftEnterData[style], miniGameInitTcs).Forget();
        
        // TODO : 풀링.
        miniGameObj = Instantiate(prefab);
        IMiniGameController controller = miniGameObj.GetComponent<IMiniGameController>();

        //TODO 컷씬 중 생성해야하기 때문에 tcs 1회 사용은 필수. Init 시점 체크 필요.
        await miniGameInitTcs.Task;

        controller.InitGame(miniGameEndTcs);

        await miniGameEndTcs.Task;

        Destroy(miniGameObj);
        EndCraft().Forget();
    }


    /// <summary>
    /// Craft는 종료 후 다시 Main으
    /// </summary>
    public async UniTaskVoid EndCraft()
    {
        Logger.Log("컷씬 재생");
        Logger.Log("Shake 끝남 판정");
        Logger.Log("결과 판정 추후 진행 : 디폴트 B.");
        

        string nextDialogueId = "";
        GameStateManager.Instance.CurrentGameState = GameState.Play;
        ingredientPanel.gameObject.SetActive(false);

        //아 시발
        //판정 때리기
        string result = Evaluate();
        string targetCutsceneId = curCraftEventData.CraftCutscenes.craftFinishData.Default;
        if(result == "unknown")
        {
            if(curCraftEventData.CraftCutscenes.craftFinishData.Failed != null)
                targetCutsceneId = curCraftEventData.CraftCutscenes.craftFinishData.Failed;
        }
        else
        {
            if (curCraftEventData.CraftCutscenes.craftFinishData.ByCocktail[craftStation.targetCocktailData.Id] != null)
                targetCutsceneId = curCraftEventData.CraftCutscenes.craftFinishData.ByCocktail[craftStation.targetCocktailData.Id];
        }

        await cutSceneManager.PlayCutSceneAsync(targetCutsceneId);


        //이 컷씬이 끝났으면 ServeAnimation 실행하기 시발 스파인임.

        ReactionDetailData resultReaction = curCraftEventData.Reactions[result];

        if(resultReaction.CutsceneId != null)
        {
            cutSceneManager.PlayCutSceneAsync(resultReaction.CutsceneId).Forget();
        }
        
        nextDialogueId = resultReaction.CutsceneId;


        if (mainCraftingTcs != null)
        {
            Logger.Log("Craft tcs not null");
            mainCraftingTcs.TrySetResult(nextDialogueId);
            mainCraftingTcs = null;
        }

        curCraftEventData = default;
    }

    public CocktailData GetMatchingCocktails()
    {
        int inputIngredientCount = craftStation.ingredientDatas.Count;

        CocktailData? matchedCocktail = cocktailDataSO.allCocktails.Values.FirstOrDefault(cocktail =>
        {
            //재료 갯수가 다르다면 다음거
            if (cocktail.Recipe.Length != inputIngredientCount)
                return false;

            foreach (var recipeItem in cocktail.Recipe)
            {
                //재료 존재 여부 및 갯수 체크
                if (!craftStation.ingredientDatas.TryGetValue(recipeItem.Ingredient, out var stationData))
                    return false;

                if (stationData.value != recipeItem.Count)
                    return false;
            }

            return true;
        });

        return matchedCocktail ?? cocktailDataSO.allCocktails["unknown"];    
    }



    public string Evaluate()
    {
        if (craftStation.targetCocktailData.Id == "unknown") return "bad";

        CocktailData cocktail = craftStation.targetCocktailData;

        //제작법이 build 이거나 액션 횟수가 목표횟수 범위 내라면 제작 성공.
        bool craftSuccess = 
        (craftStation.craftingResult.selectMethod == "build" && cocktail.Method == "build")
       || (Mathf.Abs(craftStation.craftingResult.acionCount - cocktail.TargetCount)
       <= curCraftEventData.Evaluation.CraftTolerance);


        foreach (var rule in curCraftEventData.Evaluation.Rules)
        {
            if (rule.MatchValues == null || rule.MatchValues.Length == 0) continue;

            bool matched = rule.MatchType switch
            {
                "cocktail_id" => rule.MatchValues.Contains(cocktail.Id),
                "keyword" => rule.MatchValues.Any(kw => cocktail.Keywords.Contains(kw)),
                "base" => rule.MatchValues.Contains(cocktail.BaseIngredient),
                _ => false
            };

            if (!matched) continue;

            // require_craft_success가 true면 제조도 성공해야 통과
            if (rule.RequireCraftSuccess && !craftSuccess) continue;

            return rule.Result; // "perfect", "hidden", "normal"
        }

        // 아무 규칙에도 안 걸리면
        return "miss";
    }
}
