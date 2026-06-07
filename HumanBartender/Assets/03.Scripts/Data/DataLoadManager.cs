using System.Collections.Generic;
using UnityEngine;

public interface IDataSwitcher
{
    void SwitchDay(string dayId, string craftId);
}


public class DataLoadManager : MonoBehaviour, IDataSwitcher
{
    [SerializeField] bool isTest;
    [SerializeField] string testDayName;
    [SerializeField] string testCraftName;

    [SerializeField] List<string> dayFiles = new();
    [SerializeField] List<string> craftFiles = new();

    [SerializeField] DayDataSO dayData;
    [SerializeField] CraftDataSO craftData;
    [SerializeField] CocktailDataSO cocktailData;
    [SerializeField] IngredientDataSO ingredientDataSO;
    [SerializeField] CharacterDataSO characterData;
    [SerializeField] CharacterAnimSO characterAnimConfig;
    [SerializeField] CutSceneDataSO cutSceneData;
    [SerializeField] SettlementDataSO settlementDataSO;
    [SerializeField] TextTagDataSO textTagDataSO;
    [SerializeField] CharacterTierDataSO characterTierDataSO;
    [SerializeField] SkillTierDataSO skillTierDataSO;

    [Header("DataFile Name")]
    [SerializeField] string dayDataFileName = "day0.json";
    [SerializeField] string craftDataFileName = "day_test_crafts.json";
    [SerializeField] string cocktailDataFileName = "cocktails.json";
    [SerializeField] string characterDataFileName = "characters.json";
    [SerializeField] string characterAnimConfigFileName = "character_anim.json";
    [SerializeField] string ingredientDataFileName = "ingredients.json";
    [SerializeField] string cutSceneDataFileName = "cutscenes.json";
    [SerializeField] string settlementDataFileName = "settlements.json";
    [SerializeField] string textTagDataFileName = "text_styles.json";
    [SerializeField] string characterTierDataFileName = "character_tiers.json";
    [SerializeField] string skillTierDataFileName = "skill_tiers.json";

    Dictionary<string, DayDatabBase> _dayCache = new();
    Dictionary<string, CraftDataBase> _craftCache = new();

    void Awake()
    {
        Logger.Log("Load Data");

        foreach (var f in dayFiles)   // 알고 있는 파일 목록
            _dayCache[f] = JsonManager<DayDatabBase>.LoadGameData_StreamingAssets(f);
        foreach (var f in craftFiles)
            _craftCache[f] = JsonManager<CraftDataBase>.LoadGameData_StreamingAssets(f);


        SwitchDay(isTest ? testDayName : dayDataFileName, isTest ? testCraftName : craftDataFileName);

        cocktailData.cocktailData           = JsonManager<CocktailDataBase>.LoadGameData_StreamingAssets(cocktailDataFileName);
        characterData.characterData         = JsonManager<CharacterDataBase>.LoadGameData_StreamingAssets(characterDataFileName);
        characterAnimConfig.animConfig      = JsonManager<CharacterAnimBase>.LoadGameData_StreamingAssets(characterAnimConfigFileName);
        ingredientDataSO.ingredientData     = JsonManager<IngredientDataBase>.LoadGameData_StreamingAssets(ingredientDataFileName);
        cutSceneData.cutSceneData           = JsonManager<CutSceneDataBase>.LoadGameData_StreamingAssets(cutSceneDataFileName);
        settlementDataSO.settlementData     = JsonManager<SettlementDataBase>.LoadGameData_StreamingAssets(settlementDataFileName);
        textTagDataSO.textTagData           = JsonManager<TextTagDataBase>.LoadGameData_StreamingAssets(textTagDataFileName);
        characterTierDataSO.characterTiers  = JsonManager<CharacterTierDataBase>.LoadGameData_StreamingAssets(characterTierDataFileName);
        skillTierDataSO.skillTier           = JsonManager<SkillTierDataBase>.LoadGameData_StreamingAssets(skillTierDataFileName);

        settlementDataSO.Cached();
        cocktailData.Cached();
        cutSceneData.Cached();

        return;
    }
    public void SwitchDay(string dayFile, string craftFile)
    {
        dayData.dayData = _dayCache[dayFile];
        craftData.craftData = _craftCache[craftFile];
    }


    #region Log
    private void VerifyCharacterData()
    {
        var chars = characterData?.characterData?.Characters;
        if (chars == null || chars.Length == 0)
        {
            Debug.LogError("[캐릭터] 데이터가 비어있거나 로드 실패!");
            return;
        }

        Debug.Log($"<color=#4A90D9>[캐릭터]</color> 총 {chars.Length}명 로드 완료.");
        var first = chars[0];
        Debug.Log($"  ㄴ ID: {first.Id} | 이름: {first.DisplayName} | 표정 개수: {first.Expressions?.Length ?? 0}개 | 플레이어 여부: {first.IsPlayer}");
    }

    private void VerifyCharacterAnimConfig()
    {
        var characters = characterAnimConfig?.animConfig?.Characters;
        if (characters == null || characters.Count == 0)
        {
            Debug.LogError("[캐릭터 애니] 데이터가 비어있거나 로드 실패!");
            return;
        }

        Debug.Log($"<color=#C8A2C8>[캐릭터 애니]</color> 총 {characters.Count}명 로드 완료.");
        foreach (var kv in characters)
        {
            int exprCount = kv.Value.Expressions?.Count ?? 0;
            Debug.Log($"  ㄴ ID: {kv.Key} | base_body: {kv.Value.BaseBody} | expression 수: {exprCount}개");
        }
    }

    private void VerifyCocktailData()
    {
        var cocktails = cocktailData?.cocktailData?.Cocktails;
        if (cocktails == null || cocktails.Length == 0)
        {
            Debug.LogError("[칵테일] 데이터가 비어있거나 로드 실패!");
            return;
        }

        Debug.Log($"<color=#FF7F50>[칵테일]</color> 총 {cocktails.Length}개 로드 완료.");
        var first = cocktails[0];
        Debug.Log($"  ㄴ ID: {first.Id} | 이름: {first.Name} | 기법: {first.Method} | 필요 재료 수: {first.Recipe?.Length ?? 0}가지 | 키워드 수: {first.Keywords?.Length ?? 0}개");
    }

    private void VerifyIngredientData()
    {
        var ingredients = ingredientDataSO?.ingredientData?.Ingredients;
        if (ingredients == null || ingredients.Length == 0)
        {
            Debug.LogError("[재료] 데이터가 비어있거나 로드 실패!");
            return;
        }

        Debug.Log($"<color=#8FBC8F>[재료]</color> 총 {ingredients.Length}개 로드 완료.");
        var first = ingredients[0];
        Debug.Log($"  ㄴ ID: {first.Id} | 이름: {first.Name} | 카테고리: {first.Category} | 최대 소지량: {first.MaxCount}");
    }

    private void VerifyDayData()
    {
        var day = dayData?.dayData;
        if (day == null || day.Scenes == null || day.Scenes.Length == 0)
        {
            Debug.LogError("[Day] 데이터가 비어있거나 로드 실패!");
            return;
        }

        Debug.Log($"<color=#DDA0DD>[Day 데이터]</color> Day {day.Day} 로드 완료. (총 씬 {day.Scenes.Length}개)");
        var firstScene = day.Scenes[0];
        Debug.Log($"  ㄴ 씬 ID: {firstScene.SceneId} | 등장 손님: {firstScene.Customer} | 대화문 개수: {firstScene.Dialogues?.Length ?? 0}개");

        if (firstScene.Dialogues != null && firstScene.Dialogues.Length > 0)
            Debug.Log($"  ㄴ 첫 번째 대사 ID: {firstScene.Dialogues[0].Id} | 텍스트: {firstScene.Dialogues[0].Text}");
    }

    private void VerifyCraftData()
    {
        var events = craftData?.craftData?.CraftEvents;
        if (events == null || events.Length == 0)
        {
            Debug.LogError("[크래프트] 이벤트 데이터가 비어있거나 로드 실패!");
            return;
        }
        Debug.Log($"<color=#F0E68C>[크래프트]</color> 총 {events.Length}개 이벤트 로드 완료.");
        var first = events[0];
        Debug.Log($"  ㄴ 이벤트 ID: {first.Id} | UI 자동오픈: {first.AutoOpenRecipeUi} | 평가 룰 개수: {first.Evaluation.Rules.Length}개");
    }

    private void VerifyCutSceneData()
    {
        var cut = cutSceneData?.cutSceneData;
        if (cut == null)
        {
            Debug.LogError("[컷신] 데이터가 로드되지 않았습니다!");
            return;
        }

        int posCount    = cut.PositionPresets?.Count ?? 0;
        int layoutCount = cut.LayoutPresets?.Count  ?? 0;
        int sceneCount  = cut.SpriteCutscenes?.Length     ?? 0;

        Debug.Log($"<color=#ADD8E6>[컷신]</color> 데이터 로드 완료.");
        Debug.Log($"  ㄴ 컷신 본문 총 {sceneCount}개");

        if (sceneCount > 0)
        {
            var firstScene = cut.SpriteCutscenes[0];
            Debug.Log($"  ㄴ 첫 컷신 ID: {firstScene.Id} | 타입: {firstScene.Type}개");
        }
    }
    #endregion
}
