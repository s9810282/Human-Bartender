using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;



/// <summary>
/// 칵테일 제조 플로우 전체를 관장하는 매니저.
/// 재료 선택 -> 제조 방식(쉐이크/스터/빌드) 선택 -> 미니게임 진행 -> 결과 판정(Evaluate) -> 보상 지급 -> 초기화
/// 순서로 흐름을 진행하며, 중간중간 컷씬/카메라 줌/이펙트 연출을 트리거한다.
/// </summary>
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
    [SerializeField] GameObject noneIngrediantPopup;
    [SerializeField] GameObject playMethodPopup;
    [SerializeField] TextMeshProUGUI playMethodPopupText;


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
    ///
    /// (인수인계 메모) 제조 이벤트를 id로 조회해 UI(주문 텍스트, 레시피 패널, 튜토리얼)를 초기화하고,
    /// 플레이어가 CraftServe()를 호출해 결과를 확정할 때까지 대기한다(mainCraftingTcs).
    /// 반환값은 결과에 따른 다음 대사 id.
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
            playMethodPopup.gameObject.SetActive(false);
            noneIngrediantPopup.gameObject.SetActive(false);
            ingredientPanel.OnCategoryContents(0);
            craftObject.gameObject.SetActive(true);
            craftStation.ResetCraftStation();
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

    /// <summary>튜토리얼 스텝 하나를 실행한다. 현재 "highlight" 타입만 로그로 확인 중이며 실제 하이라이트 UI는 미구현.</summary>
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

    /// <summary>제조 방식 버튼 클릭 처리. 재료가 없으면 경고 팝업, 있으면 확인 팝업을 띄운다.</summary>
    public void OnClickMethod(string method)
    {
        if (craftStation.ingredientDatas.Count == 0)
        {
            noneIngrediantPopup.gameObject.SetActive(true);
        }
        else
        {
            playMethodPopupText.text = GetMethodtoKOR(method) + "를 진행하시겠습니까?";
            playMethodPopup.gameObject.SetActive(true);
            curSelectMethod = method;
        }
    }
    /// <summary>제조 방식 확인 시 현재 재료 조합에 맞는 칵테일을 미리 계산해두고 미니게임을 시작한다.</summary>
    public void StartMethod()
    {
        if (craftStation.ingredientDatas.Count == 0) return;

        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;

        craftStation.targetCocktailData = GetMatchingCocktails();
        craftStation.targetCocktailId = craftStation.targetCocktailData.Id;

        StartMiniGameAsync(curSelectMethod, GetMethodObject(curSelectMethod)).Forget();
    }


    //아래 두 함수는 컷씬 구조 정립 후 다시 정리하기
    /// <summary>
    /// 미니게임 진입 플로우: 페이드 인 -> 카메라 줌 -> (있다면) 진입 컷씬 재생 -> 미니게임 프리팹 생성/DI 주입/InitGame ->
    /// 미니게임 종료 대기 -> EndCraft로 마무리. style은 "shake"/"stir"/"build".
    /// </summary>
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

        //cameraTcs.TrySetResult();
        await miniGameInitTcs.Task;

        if (resolver != null)
            resolver.Inject(controller);

        controller.InitGame(miniGameEndTcs);

        await miniGameEndTcs.Task;
        await EndCraft();
    }


    /// <summary>
    /// 미니게임 종료 후 완성/실패 컷씬과 서빙 컷씬을 순서대로 재생한다.
    /// 매칭된 칵테일이 없으면(unknown) 실패 컷씬, 있으면 칵테일별 완성 애니메이션을 재생.
    /// </summary>
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

    /// <summary>서빙 컷씬(타임라인 시그널) 종료 콜백. 대사창을 다시 열고 미니게임의 다음 버튼을 노출한다.</summary>
    public void OnCutSceneEnd(Void v)
    {
        dialogueCanvas.gameObject.SetActive(true);
        controller.OnNextButton();
    }


    //아래 2개 버튼에 들어가야하는 함수.
    /// <summary>
    /// 서빙 버튼 클릭 시 최종 판정을 실행(Evaluate)하고, 결과 리액션(컷씬/대사/보상)을 적용한다.
    /// 보상 지급(스킬/호감도/카르마/재화) 후 StartCraftAsync의 대기를 완료시키고 제조 상태를 초기화한다.
    /// </summary>
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

    /// <summary>재시도 버튼 콜백 (현재 미구현).</summary>
    public void CraftRetry()
    {

    }

    /// <summary>진행 중인 컷씬을 정리하고 미니게임 오브젝트를 파괴한다.</summary>
    public void ResetCraftObj()
    {
        cutScenePlayer.ClearCutScene();

        if (miniGameObj != null)
            Destroy(miniGameObj);
    }

    /// <summary>제조 상태 전체(스테이션 데이터, 패널 필터/선택, 미니게임 참조)를 다음 제조를 위해 초기화한다.</summary>
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


    /// <summary>
    /// 현재 스테이션에 담긴 재료 조합과 정확히 일치하는(개수·종류 모두 동일) 레시피를 찾는다.
    /// 일치하는 칵테일이 없으면 unknown_Cocktails를 반환한다.
    /// </summary>
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


    /// <summary>실패 횟수 비율(currentValue/maxValue)을 백분율로 환산해 Perfect/Normal/Failed 등급을 매긴다.</summary>
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
    /// <summary>
    /// 최종 제조 결과를 판정한다. build 방식은 재료만 맞으면 무조건 perfect, 그 외에는 미니게임 실패 횟수로
    /// 등급을 계산한 뒤 이벤트에 정의된 규칙(Rules)을 순서대로 검사해 첫 매칭 결과를 반환한다.
    /// </summary>
    public string Evaluate()
    {
        if (craftStation.targetCocktailData.Id == "unknown") return "bad";

        CocktailData cocktail = craftStation.targetCocktailData;

        //제작법이 build 이거나 액션 횟수가 목표횟수 범위 내라면 제작 성공.
        if (craftStation.craftingResult.selectMethod == "build" && cocktail.Method == "build")
            return "perfect";
        
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
