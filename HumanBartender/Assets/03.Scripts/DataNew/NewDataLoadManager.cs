using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TreeEditor;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// VContainer의 IAsyncStartable로 StreamingAssets/json 폴더에서 신규 JSON 게임 데이터를 로드하여
/// 각 New*DataSO에 주입하는 매니저. ProjectLifetimeScope에 엔트리포인트로 등록되어
/// 컨테이너 빌드 시점에 StartAsync가 호출된다(Awake 순서에 기대지 않음).
/// json/script/day_N.json은 날짜별로 캐싱해두고 SwitchDay(int)로 현재 날짜의 스크립트를 교체하며,
/// json/script/common.json은 날짜와 무관하게 항상 쓰이는 공용 상호작용 스크립트라 별도 SO에 고정 로드한다.
/// </summary>
public class NewDataLoadManager : MonoBehaviour, INewDataSwitcher, IAsyncStartable
{
    [Header("Test")]
    [SerializeField] bool isTest;
    [SerializeField] int testDay = 1;

    [Header("Day Script (json/script/day_N.json)")]
    [SerializeField] int startDay = 1;
    [SerializeField] List<int> dayNumbers = new() { 1, 2, 3 };   // 알고 있는 날짜 목록

    [Header("Target SO")]
    [SerializeField] NewBalanceDataSO balanceData;
    [SerializeField] NewBarkDataSO barkData;
    [SerializeField] NewCharacterDataSO characterData;
    [SerializeField] NewCocktailDataSO cocktailData;
    [SerializeField] NewCutSceneDataSO cutSceneData;
    [SerializeField] NewDayInfoDataSO dayInfoData;
    [SerializeField] NewDayScriptDataSO dayScriptData;
    [SerializeField] NewDayScriptDataSO commonScriptData;
    [SerializeField] NewDossierDataSO dossierData;
    [SerializeField] NewEndingDataSO endingData;
    [SerializeField] NewExpressionDataSO expressionData;
    [SerializeField] NewFieldAnimDataSO fieldAnimData;
    [SerializeField] NewGuestBodyDataSO guestBodyData;
    [SerializeField] NewInteractPointDataSO interactPointData;
    [SerializeField] NewOrderRuleDataSO orderRuleData;
    [SerializeField] NewPersonalityDataSO personalityData;
    [SerializeField] NewQuestDataSO questData;
    [SerializeField] NewRandomWaveDataSO randomWaveData;
    [SerializeField] NewRegularSlotDataSO regularSlotData;
    [SerializeField] NewShelfItemDataSO shelfItemData;
    [SerializeField] NewSpotDataSO spotData;
    [SerializeField] NewTasteDataSO tasteData;
    [SerializeField] NewUIStringDataSO uiStringData;
    [SerializeField] NewStreetDataSO streetData;

    [Header("DataFile Name (json/*.json)")]
    [SerializeField] string balanceFileName = "json/balance.json";
    [SerializeField] string barkFileName = "json/barks.json";
    [SerializeField] string characterFileName = "json/characters.json";
    [SerializeField] string cocktailFileName = "json/cocktails.json";
    [SerializeField] string cutSceneFileName = "json/cutscenes.json";
    [SerializeField] string dayInfoFileName = "json/days.json";
    [SerializeField] string commonScriptFileName = "json/script/common.json";
    [SerializeField] string dossierFileName = "json/dossier.json";
    [SerializeField] string endingFileName = "json/endings.json";
    [SerializeField] string expressionFileName = "json/expressions.json";
    [SerializeField] string fieldAnimFileName = "json/field_anims.json";
    [SerializeField] string guestBodyFileName = "json/guest_bodies.json";
    [SerializeField] string interactPointFileName = "json/interact_points.json";
    [SerializeField] string orderRuleFileName = "json/order_rules.json";
    [SerializeField] string personalityFileName = "json/personalities.json";
    [SerializeField] string questFileName = "json/quests.json";
    [SerializeField] string randomWaveFileName = "json/random_waves.json";
    [SerializeField] string regularSlotFileName = "json/regular_slots.json";
    [SerializeField] string shelfItemFileName = "json/shelf_items.json";
    [SerializeField] string spotFileName = "json/spots.json";
    [SerializeField] string tasteFileName = "json/tastes.json";
    [SerializeField] string uiStringFileName = "json/ui_strings.json";
    [SerializeField] string streetFileName = "json/script/street.json";

    Dictionary<int, NewDayScriptBase> _dayScriptCache = new();

    /// <summary>VContainer가 컨테이너 빌드 시점에 호출하는 엔트리포인트. LoadDataAsync를 기다린 뒤 완료된다.</summary>
    
    async UniTask IAsyncStartable.StartAsync(CancellationToken cancellation)
    {
        await LoadDataAsync();
    }

    static string DayScriptFileName(int day) => $"json/script/day_{day}.json";

    public void LoadData()
    {
        Logger.Log("[New] Load Data");

        foreach (var day in dayNumbers)
            _dayScriptCache[day] = JsonManager<NewDayScriptBase>.LoadGameData_StreamingAssets(DayScriptFileName(day));

        commonScriptData.dayScriptData = JsonManager<NewDayScriptBase>.LoadGameData_StreamingAssets(commonScriptFileName);

        SwitchDay(isTest ? testDay : startDay);

        balanceData.balanceData = JsonManager<NewBalanceDataBase>.LoadGameData_StreamingAssets(balanceFileName);
        barkData.barkData = JsonManager<NewBarkData[]>.LoadGameData_StreamingAssets(barkFileName);
        characterData.characterData = JsonManager<NewCharacterData[]>.LoadGameData_StreamingAssets(characterFileName);
        cocktailData.cocktailData = JsonManager<NewCocktailData[]>.LoadGameData_StreamingAssets(cocktailFileName);
        cutSceneData.cutSceneData = JsonManager<NewCutSceneRefData[]>.LoadGameData_StreamingAssets(cutSceneFileName);
        dayInfoData.dayInfoData = JsonManager<NewDayInfoData[]>.LoadGameData_StreamingAssets(dayInfoFileName);
        dossierData.dossierData = JsonManager<NewDossierData[]>.LoadGameData_StreamingAssets(dossierFileName);
        endingData.endingData = JsonManager<NewEndingData[]>.LoadGameData_StreamingAssets(endingFileName);
        expressionData.expressionData = JsonManager<Dictionary<string, Dictionary<string, NewExpressionEntry>>>.LoadGameData_StreamingAssets(expressionFileName);
        fieldAnimData.fieldAnimData = JsonManager<NewFieldAnimData[]>.LoadGameData_StreamingAssets(fieldAnimFileName);
        guestBodyData.guestBodyData = JsonManager<NewGuestBodyDataBase>.LoadGameData_StreamingAssets(guestBodyFileName);
        interactPointData.interactPointData = JsonManager<NewInteractPointData[]>.LoadGameData_StreamingAssets(interactPointFileName);
        orderRuleData.orderRuleData = JsonManager<NewOrderRuleData[]>.LoadGameData_StreamingAssets(orderRuleFileName);
        personalityData.personalityData = JsonManager<NewPersonalityData[]>.LoadGameData_StreamingAssets(personalityFileName);
        questData.questData = JsonManager<NewQuestDataBase>.LoadGameData_StreamingAssets(questFileName);
        randomWaveData.randomWaveData = JsonManager<NewRandomWaveData[]>.LoadGameData_StreamingAssets(randomWaveFileName);
        regularSlotData.regularSlotData = JsonManager<NewRegularSlotData[]>.LoadGameData_StreamingAssets(regularSlotFileName);
        shelfItemData.shelfItemData = JsonManager<NewShelfItemData[]>.LoadGameData_StreamingAssets(shelfItemFileName);
        spotData.spotData = JsonManager<NewSpotData[]>.LoadGameData_StreamingAssets(spotFileName);
        tasteData.tasteData = JsonManager<NewTasteData[]>.LoadGameData_StreamingAssets(tasteFileName);
        uiStringData.uiStringData = JsonManager<Dictionary<string, LocalizedText>>.LoadGameData_StreamingAssets(uiStringFileName);
        streetData.newStreetData = JsonManager<NewStreetData>.LoadGameData_StreamingAssets(streetFileName);
        Logger.Log("[New] Load end");
    }

    public async UniTask LoadDataAsync()
    {
        Logger.Log("[New] Load Data");

        foreach (var day in dayNumbers)
            _dayScriptCache[day] = await JsonManager<NewDayScriptBase>.LoadAsync<NewDayScriptBase>(DayScriptFileName(day));

        commonScriptData.dayScriptData = await JsonManager<NewDayScriptBase>.LoadAsync<NewDayScriptBase>(commonScriptFileName);

        SwitchDay(isTest ? testDay : startDay);

        balanceData.balanceData = await JsonManager<NewBalanceDataBase>.LoadAsync<NewBalanceDataBase>(balanceFileName);
        barkData.barkData = await JsonManager<NewBarkData[]>.LoadAsync<NewBarkData[]>(barkFileName);
        characterData.characterData = await JsonManager<NewCharacterData[]>.LoadAsync<NewCharacterData[]>(characterFileName);
        cocktailData.cocktailData = await JsonManager<NewCocktailData[]>.LoadAsync<NewCocktailData[]>(cocktailFileName);
        cutSceneData.cutSceneData = await JsonManager<NewCutSceneRefData[]>.LoadAsync<NewCutSceneRefData[]>(cutSceneFileName);
        dayInfoData.dayInfoData = await JsonManager<NewDayInfoData[]>.LoadAsync<NewDayInfoData[]>(dayInfoFileName);
        dossierData.dossierData = await JsonManager<NewDossierData[]>.LoadAsync<NewDossierData[]>(dossierFileName);
        endingData.endingData = await JsonManager<NewEndingData[]>.LoadAsync<NewEndingData[]>(endingFileName);
        expressionData.expressionData = await JsonManager<Dictionary<string, Dictionary<string, NewExpressionEntry>>>.LoadAsync<Dictionary<string, Dictionary<string, NewExpressionEntry>>>(expressionFileName);
        fieldAnimData.fieldAnimData = await JsonManager<NewFieldAnimData[]>.LoadAsync<NewFieldAnimData[]>(fieldAnimFileName);
        guestBodyData.guestBodyData = await JsonManager<NewGuestBodyDataBase>.LoadAsync<NewGuestBodyDataBase>(guestBodyFileName);
        interactPointData.interactPointData = await JsonManager<NewInteractPointData[]>.LoadAsync<NewInteractPointData[]>(interactPointFileName);
        orderRuleData.orderRuleData = await JsonManager<NewOrderRuleData[]>.LoadAsync<NewOrderRuleData[]>(orderRuleFileName);
        personalityData.personalityData = await JsonManager<NewPersonalityData[]>.LoadAsync<NewPersonalityData[]>(personalityFileName);
        questData.questData = await JsonManager<NewQuestDataBase>.LoadAsync<NewQuestDataBase>(questFileName);
        randomWaveData.randomWaveData = await JsonManager<NewRandomWaveData[]>.LoadAsync<NewRandomWaveData[]>(randomWaveFileName);
        regularSlotData.regularSlotData = await JsonManager<NewRegularSlotData[]>.LoadAsync<NewRegularSlotData[]>(regularSlotFileName);
        shelfItemData.shelfItemData = await JsonManager<NewShelfItemData[]>.LoadAsync<NewShelfItemData[]>(shelfItemFileName);
        spotData.spotData = await JsonManager<NewSpotData[]>.LoadAsync<NewSpotData[]>(spotFileName);
        tasteData.tasteData = await JsonManager<NewTasteData[]>.LoadAsync<NewTasteData[]>(tasteFileName);
        uiStringData.uiStringData = await JsonManager<Dictionary<string, LocalizedText>>.LoadAsync<Dictionary<string, LocalizedText>>(uiStringFileName);
        streetData.newStreetData = await JsonManager<NewStreetData>.LoadAsync<NewStreetData>(streetFileName);
        Logger.Log("[New] Load end");
    }

    /// <summary>day_N.json을 dayScriptData에 주입한다. common.json은 날짜와 무관하게 commonScriptData에 항상 고정되어 있다.</summary>
    public void SwitchDay(int day)
    {
        dayScriptData.dayScriptData = _dayScriptCache[day];
    }


}
