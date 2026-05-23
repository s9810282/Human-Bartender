using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using VContainer;
using VContainer.Unity;
using static UnityEngine.Rendering.DebugUI.MessageBox;


public interface IMiniGameController
{
    public void InitGame(UniTaskCompletionSource tcs);
    public void CompleteMade();
    public void OnNextButton();
    public void Serve();
    public void Retry();
}


public class CocktailCraftManager : MonoBehaviour, ICocktailCraft
{
    [SerializeField] CraftDataSO craftDataSO;
    [SerializeField] CocktailDataSO cocktailDataSO;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] IngredientPanel ingredientPanel;

    [Inject] IEffectPlayer effectPlayer;
    [Inject] ICutScenePlayer cutScenePlayer;
    [Inject] ICameraControl cameraZoom;

    [Header("MiniGame Prefabs")]
    [SerializeField] private GameObject shakePrefab;
    [SerializeField] private GameObject stirPrefab;
    [SerializeField] private GameObject buildPrefab;

    [SerializeField] CraftEventData curCraftEventData;
    
    private UniTaskCompletionSource<string> mainCraftingTcs;
    UniTaskCompletionSource cameraTcs;


    GameObject miniGameObj = null;
    IMiniGameController controller = null;

    public void Start()
    {

    }

    /// <summary>
    /// Command 에서 호출 되는 함수
    /// </summary>
    /// <param name="craftEventData"></param>
    public async UniTask<string> StartCraftAsync(string id)
    {
        Logger.Log(craftDataSO == null);
        CraftEventData craftEventData = craftDataSO.GetCraftDataByID(id);

        ingredientPanel.ClearCurrentSelectIngredient();

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

            for (int i = 0; i < steps.Length; i++)
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

    /// <summary>
    /// 연출 있다는데 그건 그때 넣어야되는거고
    /// </summary>
    public void StartBuild()
    {
        //빌드 시에는 컷씬연출로 대체. 이후 재료에 따라 수정될 거 같음.
        EndBuild();
    }
    //요청에 따른 임시 함수
    public async void EndBuild()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        StartMiniGameAsync("build", buildPrefab).Forget();
    }


    public void StartShake()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        StartMiniGameAsync("shake", shakePrefab).Forget();
    }
    public void StartStur()
    {
        return;

        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        StartMiniGameAsync("stir", stirPrefab).Forget();
    }


    //아래 두 함수는 컷씬 구조 정립 후 다시 정리하기
    private async UniTaskVoid StartMiniGameAsync(string style, GameObject prefab)
    {
        UniTaskCompletionSource miniGameEndTcs = new UniTaskCompletionSource();
        UniTaskCompletionSource miniGameInitTcs = new UniTaskCompletionSource();

        miniGameObj = null;

        cameraTcs = new UniTaskCompletionSource();

        await effectPlayer.PlayEffectAsync(EEffectType.FadeIn, 1f);
        cameraZoom.ActionZoomAndBack(ECameraZoomType.Sub, cameraTcs);

        //TODO : 여기도 진입할 때 컷씬 
        if (curCraftEventData.CraftCutscenes.craftEnterData[style] != null)
        {
            await cutScenePlayer.PlayCutScene
            (curCraftEventData.CraftCutscenes.craftEnterData[style], miniGameInitTcs);
        }
        else
        {
            miniGameInitTcs.TrySetResult();
        }

        craftStation.craftingResult.selectMethod = style;
        craftStation.targetCraft_tolerance = curCraftEventData.Evaluation.CraftTolerance;

        // TODO : 풀링?
        miniGameObj = Instantiate(prefab);
        Vector3 vec = Camera.main.transform.position;
        vec.z = 0;
        miniGameObj.transform.position = vec;
        controller = miniGameObj.GetComponent<IMiniGameController>();


        if (style != "build" && curCraftEventData.CraftCutscenes.craftEnterData[style] == null)
        {
            cutScenePlayer.ClearCutScene();
        }

        cameraTcs.TrySetResult();
        //cameraZoom.ActionZoom(CameraZoomType.Base); //게임용 화면 1280 720 전환

        await miniGameInitTcs.Task;

        controller.InitGame(miniGameEndTcs);

        await miniGameEndTcs.Task;
        await EndCraft();
    }


    /// <summary>
    /// Craft는 종료 후 다시 Main으로
    /// </summary>
    public async UniTask EndCraft()
    {
        GameStateManager.Instance.CurrentGameState = GameState.Play;
        ingredientPanel.gameObject.SetActive(false);


        string targetCutsceneId = curCraftEventData.CraftCutscenes.craftFinishData.Default;

        if (craftStation.targetCocktailData.Id == "unknown")
        {
            if (curCraftEventData.CraftCutscenes.craftFinishData.Failed != null)
                targetCutsceneId = curCraftEventData.CraftCutscenes.craftFinishData.Failed;
        }
        else
        {
            if (craftStation.targetCocktailData.Finish_animation != null)
                targetCutsceneId = craftStation.targetCocktailData.Finish_animation;
        }

        //Finished CutScene
        await effectPlayer.PlayEffectAsync(EEffectType.FadeIn, 1f);


        cameraZoom.ActionZoom(ECameraZoomType.Sub);
        await cutScenePlayer.PlayCutScene(targetCutsceneId);


        //ServeAnimation CutScene
        if (craftStation.targetCocktailData.Serve_animation != null)
        {
            targetCutsceneId = craftStation.targetCocktailData.Serve_animation;
            await cutScenePlayer.PlayCutScene(targetCutsceneId);
        }

        //컷씬 끝났으면 버튼 활성화.
        controller.OnNextButton();
    }


    //아래 2개 버튼에 들어가야하는 함수.
    public async void CraftServe()
    {
        string result = Evaluate();
        Logger.Log(result);

        ReactionDetailData resultReaction = curCraftEventData.Reactions[result];
        string nextDialogueId = resultReaction.DialogueId;

        //Reaction CutScene
        if (resultReaction.CutsceneId != null)
        {
            UniTaskCompletionSource react = new UniTaskCompletionSource();

            cutScenePlayer.PlayCutScene(resultReaction.CutsceneId, react);
            ResetCraftObj();

            await react.Task;
        }

        //카메라 되돌리기
        cameraTcs.TrySetResult();
       
        //추후 카르마 판정

        if (mainCraftingTcs != null)
        {
            Logger.Log($"Craft tcs not null : {nextDialogueId}");
            mainCraftingTcs.TrySetResult(nextDialogueId);
            mainCraftingTcs = null;

            //카메라 되돌리기
            cameraTcs.TrySetResult();
            cameraTcs = null;
        }

        ResetCraft();
    }

    public void CraftRetry()
    {

    }


    public void ResetCraftObj()
    {
        cutScenePlayer.ClearCutScene();

        if (miniGameObj != null)
            Destroy(miniGameObj);
    }

    public void ResetCraft()
    {
        ResetCraftObj();
        curCraftEventData = default;

        miniGameObj = null;
        controller = null;
        mainCraftingTcs = null;
    }


    public CocktailData GetMatchingCocktails()
    {
        int inputIngredientCount = craftStation.ingredientDatas.Count;

        foreach (var item in craftStation.ingredientDatas)
        {
            Logger.Log(item.Key + "  " + item.Value.value);
        }
        
        CocktailData matchedCocktail = cocktailDataSO.allCocktails.Values.FirstOrDefault(cocktail =>
        {
            //재료 갯수가 다르다면 다음거
            if (cocktail.Recipe.Length != inputIngredientCount)
                return false;

            foreach (var recipeItem in cocktail.Recipe)
            {
                //재료 존재 여부 및 갯수 체크
                Logger.Log(recipeItem.Ingredient);
                if (!craftStation.ingredientDatas.TryGetValue(recipeItem.Ingredient, out var stationData))
                    return false;

                //재료 담는 단위는 10 단위. 추후 통일하기.
                if (stationData.value != (recipeItem.Count * 10))
                    return false;
            }

            return true;
        });

        
        if (matchedCocktail.Id != null)
            return matchedCocktail;
        else
            return cocktailDataSO.cocktailData.unknown_Cocktails;
    }
    public string Evaluate()
    {
        if (craftStation.targetCocktailData.Id == "unknown") return "bad";

        CocktailData cocktail = craftStation.targetCocktailData;

        //제작법이 build 이거나 액션 횟수가 목표횟수 범위 내라면 제작 성공.
        bool CheckBuild = (craftStation.craftingResult.selectMethod == "build" && cocktail.Method == "build");
        bool CheckCount = (Mathf.Abs(craftStation.craftingResult.actionCount - craftStation.targetCraft_tolerance) <= 5);

        bool craftSuccess = CheckBuild || CheckCount;

        Logger.Log(craftStation.craftingResult.actionCount); //
        Logger.Log(cocktail.TargetCount);
        Logger.Log(craftStation.targetCraft_tolerance); //

        Logger.Log($"craft Success : {craftSuccess}");

        foreach (var rule in curCraftEventData.Evaluation.Rules)
        {
            if (rule.MatchValues == null || rule.MatchValues.Length == 0) continue;

            bool matched = rule.MatchType switch
            {
                EMatchType.Id => rule.MatchValues.Contains(cocktail.Id),
                EMatchType.Keyword => cocktail.Keywords?.Length > 0 && rule.MatchValues.Any(kw => cocktail.Keywords.Contains(kw)),
                EMatchType.Base => rule.MatchValues.Contains(cocktail.BaseIngredient),
                _ => false
            };

            Logger.Log($"matched: {matched}");

            if (!matched) continue;

            // require_craft_success가 true면 제조도 성공해야 통과
            if (rule.RequireCraftSuccess && !craftSuccess) continue;

            return rule.Result; // "perfect", "hidden", "normal"
        }

        // 아무 규칙에도 안 걸리면
        return "miss";
    }
}
