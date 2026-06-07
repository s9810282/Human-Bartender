using AutoGroupGenerator;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;



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
    [SerializeField] PlayerDataSO playerDataAsset;
    [SerializeField] CraftDataSO craftDataSO;
    [SerializeField] CocktailDataSO cocktailDataSO;
    [SerializeField] CraftStationData craftStation;

    [Inject] IEffectPlayer effectPlayer;
    [Inject] ICutScenePlayer cutScenePlayer;
    [Inject] ICameraControl cameraZoom;
    [Inject] IObjectResolver resolver;
    [Inject] ISettlementLog settlementLog;

    [Header("MiniGame Prefabs")]
    [SerializeField] private GameObject shakePrefab;
    [SerializeField] private GameObject stirPrefab;
    [SerializeField] private GameObject buildPrefab;

    [Header("UI")]
    [SerializeField] GameObject dialogueCanvas;
    [SerializeField] GameObject craftObject;

    [SerializeField] UICocktailPanel cocktailPanel;
    [SerializeField] UIIngredientPanel ingredientPanel;

    [SerializeField] TextMeshProUGUI orderText;
    [SerializeField] GameObject popupObj;
    [SerializeField] TextMeshProUGUI popupText;
    

    [SerializeField] CraftEventData curCraftEventData;
    
    private UniTaskCompletionSource<string> mainCraftingTcs;
    UniTaskCompletionSource cameraTcs;


    GameObject miniGameObj = null;
    IMiniGameController controller = null;

    public void Start()
    {
        cocktailPanel.Init();
        ingredientPanel.Init();
    }

    /// <summary>
    /// Command 에서 호출 되는 함수
    /// </summary>
    /// <param name="craftEventData"></param>
    public async UniTask<string> StartCraftAsync(string id)
    {
        Logger.Log(craftDataSO == null);
        CraftEventData craftEventData = craftDataSO.GetCraftDataByID(id);

        dialogueCanvas.gameObject.SetActive(false);
        orderText.text = "";


        if (craftEventData.AutoOpenRecipeUi)
        {
            popupObj.gameObject.SetActive(false);
            ingredientPanel.OnCategoryContents(0);
            craftObject.gameObject.SetActive(true);
        }

        if (craftEventData.Order != null)
        {
            TMPTypingEffect.Type(orderText, craftEventData.Order.ToString()).Forget();
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




    string curSelectMethod = "";
    public GameObject GetMethodObject(string method) => method switch
    {
        "shake" => shakePrefab,
        "stir" => stirPrefab,
        "build" => buildPrefab,
        _ => null,
    };
    public string GetMethodtoKOR(string method) => method switch
    {
        "shake" => "쉐이킹",
        "stir" => "스터",
        "build" => "빌드",
        _ => null,
    };

    public void OnClickMethod(string method)
    {
        if (craftStation.ingredientDatas.Count == 0)
        {
            popupText.text = "재료를 선택하지 않았습니다.";
        }
        else
        {
            popupText.text = GetMethodtoKOR(method) + "를 진행하시겠습니까";
            curSelectMethod = method;
        }
    }
    public void StartMethod()
    {
        if (craftStation.ingredientDatas.Count == 0) return;

        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        StartMiniGameAsync(curSelectMethod, GetMethodObject(curSelectMethod)).Forget();
    }


    //아래 두 함수는 컷씬 구조 정립 후 다시 정리하기
    private async UniTaskVoid StartMiniGameAsync(string style, GameObject prefab)
    {
        UniTaskCompletionSource miniGameEndTcs = new UniTaskCompletionSource();
        UniTaskCompletionSource miniGameInitTcs = new UniTaskCompletionSource();

        miniGameObj = null;

        craftObject.gameObject.SetActive(false);

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
        await miniGameInitTcs.Task;

        if (resolver != null)
            resolver.Inject(controller);

        controller.InitGame(miniGameEndTcs);

        await miniGameEndTcs.Task;
        await EndCraft();
    }


    public async UniTask EndCraft()
    {
        GameStateManager.Instance.CurrentGameState = GameState.Play;


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

            //제공 컷씬의 경우 Signal 기능 이용
        }
        else
            OnCutSceneEnd(new Void());

    }

    public void OnCutSceneEnd(Void v)
    {
        dialogueCanvas.gameObject.SetActive(true);
        controller.OnNextButton();
    }


    //아래 2개 버튼에 들어가야하는 함수.
    public async void CraftServe()
    {
        cutScenePlayer.OnContinueTimeline();

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

       
        playerDataAsset.AddSkillTier(resultReaction.Skill);
        playerDataAsset.AddCharacterAffinityAmount(curCraftEventData.TargetId, resultReaction.Affinity);
        playerDataAsset.AddCharacterKarmaAmount(curCraftEventData.TargetId, resultReaction.Karma);

        if (resultReaction.Payment.Payprice)
        {
            settlementLog.AddSalesQty(craftStation.targetCocktailId, 1);
            playerDataAsset.AddMoney(craftStation.targetCocktailData.Price);

            settlementLog.AddTip
                (craftStation.targetCocktailId, Mathf.RoundToInt(craftStation.targetCocktailData.Price * resultReaction.Payment.TipRate));
            playerDataAsset.AddMoney(
                Mathf.RoundToInt(craftStation.targetCocktailData.Price * resultReaction.Payment.TipRate));
        }

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
        craftStation.ResetCraftStation();
        curCraftEventData = default;

        cocktailPanel.ResetCocktailFilter();
        ingredientPanel.ResetCurrentSelectIngredient();

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
                if (stationData.value != recipeItem.Count)
                    return false;
            }

            return true;
        });

        
        if (matchedCocktail.Id != null)
            return matchedCocktail;
        else
            return cocktailDataSO.cocktailData.unknown_Cocktails;
    }


    public EResultType GetResultType(float currentValue, float maxValue)
    {
        // 백분율 계산 (0 ~ 100)
        float percentage = (currentValue / maxValue) * 100f;

        return percentage switch
        {
            <= 20f => EResultType.Perfect,
            <= 50f => EResultType.Normal,
            _ => EResultType.Failed
        };
    }
    public string Evaluate()
    {
        if (craftStation.targetCocktailData.Id == "unknown") return "bad";

        CocktailData cocktail = craftStation.targetCocktailData;

        //제작법이 build 이거나 액션 횟수가 목표횟수 범위 내라면 제작 성공.
        bool CheckBuild = (craftStation.craftingResult.selectMethod == "build" && cocktail.Method == "build");
        
        Logger.Log(craftStation.craftingResult.actionFailCount);
        Logger.Log(craftStation.targetCraft_tolerance);

        EResultType actionResult = GetResultType
            (craftStation.craftingResult.actionFailCount, craftStation.craftingResult.limitFailCount);

        foreach (var rule in curCraftEventData.Evaluation.Rules)    
        {
            if (rule.MatchValues == null || rule.MatchValues.Length == 0) continue;

            bool matched = rule.MatchType switch
            {
                EMatchType.Id => rule.MatchValues.Contains(cocktail.Id),
                EMatchType.Keyword => cocktail.Keywords?.Length > 0 && rule.MatchValues.Any(kw => cocktail.Keywords.Contains(kw)),
                EMatchType.Base => rule.MatchValues.Contains(cocktail.BaseIngredient),
                EMatchType.Any => true,
                _ => false
            };

            Logger.Log($"matched: {matched}");

            if (!matched) continue;
            if (rule.RequireCraftSuccess != actionResult) continue;

            return rule.Result; // "perfect", "hidden", "normal"
        }

        return "bad_nm_fail";
    }
}
