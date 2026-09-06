using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TreeEditor;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// VContainer의 IAsyncStartable로 StreamingAssets/json 폴더에서 신규 JSON 게임 데이터를 로드하여
/// 각 New*DataSO에 주입하는 매니저. ProjectLifetimeScope에 엔트리포인트로 등록되어
/// 컨테이너 빌드 시점에 StartAsync가 호출된다(Awake 순서에 기대지 않음).
/// 2부 바 대본(script/bar/dayN.json)은 일차별로 캐싱해두고 TryGetBarScript(int)로 꺼내 쓴다.
/// json/script/day_N.json은 bar 대본으로 대체된 레거시 경로다 — 파일이 지워져 있고 읽는 코드도 없다.
/// dayNumbers를 비워 두면 로드를 건너뛰며, SwitchDay는 없는 일차를 만나면 경고만 남긴다.
/// json/script/common.json은 날짜와 무관하게 항상 쓰이는 공용 상호작용 스크립트라 별도 SO에 고정 로드한다.
///
/// 구형 DataLoadManager가 채우던 SO들도 여기서 이어받는다(Legacy 항목). 그래서 데이터 로더는 하나이고,
/// 구형 로더는 꺼 두어도 된다. IDataSwitcher도 이쪽 구현을 쓴다.
/// </summary>
public class NewDataLoadManager : MonoBehaviour, INewDataSwitcher, IDataSwitcher, IAsyncStartable
{
    [Header("Test")]
    [SerializeField] bool isTest;
    [SerializeField] int testDay = 1;

    [Header("Day Script (json/script/day_N.json)")]
    [SerializeField] int startDay = 1;
    [Tooltip("json/script/day_N.json이 있는 일차. 그 파일들은 bar 대본으로 대체돼 지금은 비어 있다.")]
    [SerializeField] List<int> dayNumbers = new();

    [Tooltip("2부 바 대본(script/bar/dayN.json)이 있는 일차. 목록에 있어도 파일이 없으면 그날 2부를 건너뛴다.")]
    [SerializeField] List<int> barDayNumbers = new() { 0, 1, 2, 3, 99 };

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
    [SerializeField] NewInteractPointDataSO homeinteractPointData;
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

    // ── 구형 SO ────────────────────────────────────────────────────
    //
    // 구형 DataLoadManager가 채우던 것들을 여기서 이어받는다. 그 SO들은 값을 프로퍼티로 들고 있어
    // Unity가 직렬화하지 못한다 — 에셋 파일은 비어 있고 오직 실행 중에 로더가 채운다.
    // 그래서 채우는 쪽이 사라지면 그 SO를 물고 있는 화면이 조용히 빈 곳을 짚는다.
    //
    // 신형에 대응 파일이 있는 셋(표정·인물·태그)은 신형에서 옮겨 담고, 나머지는 구형 파일을
    // 그대로 읽는다. 어느 쪽이든 정본은 하나이고 읽는 곳도 여기 하나다.

    [Header("Legacy SO (구형 DataLoadManager가 채우던 것)")]
    [Tooltip("신형 json/character_anim.json에서 옮겨 담는다. DialogueCharacterManager와 GuestCharacterView가 읽는다.")]
    [SerializeField] CharacterAnimSO legacyCharacterAnimConfig;

    [Tooltip("신형 json/characters.json에서 옮겨 담는다. DialogueSceneDirector가 읽는다.")]
    [SerializeField] CharacterDataSO legacyCharacterData;

    [Tooltip("신형 json/text_tags.json에서 색 태그만 옮겨 담는다. 대사창의 <name>/<world>/<order> 치환에 쓴다.")]
    [SerializeField] TextTagDataSO legacyTextTagData;

    [Tooltip("신형에 같은 모양이 없어 구형 파일을 그대로 읽는다.")]
    [SerializeField] CocktailDataSO legacyCocktailData;
    [SerializeField] IngredientDataSO legacyIngredientData;
    [SerializeField] CutSceneDataSO legacyCutSceneData;
    [SerializeField] SettlementDataSO legacySettlementData;
    [SerializeField] CharacterTierDataSO legacyCharacterTierData;
    [SerializeField] SkillTierDataSO legacySkillTierData;

    [Tooltip("구형 비주얼노벨 경로(VisualNovelFlow)가 읽는 일차/제조 대본. HomeManager가 IDataSwitcher로 갈아 끼운다.")]
    [SerializeField] DayDataSO legacyDayData;
    [SerializeField] CraftDataSO legacyCraftData;

    [Tooltip("실외 씬 데이터 로더. 구형 DataLoadManager가 마지막에 부르던 것이라 그 자리를 이어받는다.")]
    [SerializeField] OutsideDataManager outsideDataManager;

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
    [Tooltip("표정 정본. expressions.json이 이 이름으로 넘어왔고, 거리 행인 넉 명이 여기에만 있다.")]
    [SerializeField] string expressionFileName = "json/character_anim.json";
    [SerializeField] string fieldAnimFileName = "json/field_anims.json";
    [SerializeField] string guestBodyFileName = "json/guest_bodies.json";
    [SerializeField] string interactPointFileName = "json/interact_points_outside.json";
    [SerializeField] string HomeinteractPointFileName = "json/interact_points_home.json";
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

    [Header("Legacy DataFile Name (StreamingAssets/*.json)")]
    [SerializeField] string legacyCocktailFileName = "cocktails.json";
    [SerializeField] string legacyIngredientFileName = "ingredients.json";
    [SerializeField] string legacyCutSceneFileName = "cutscenes.json";
    [SerializeField] string legacySettlementFileName = "settlements.json";
    [SerializeField] string legacyCharacterTierFileName = "character_tiers.json";
    [SerializeField] string legacySkillTierFileName = "skill_tiers.json";
    [SerializeField] string textTagFileName = "json/text_tags.json";

    [Tooltip("구형 일차 대본. 시작 시 전부 읽어 두었다가 SwitchDay(dayFile, craftFile)로 갈아 끼운다.")]
    [SerializeField] List<string> legacyDayFiles = new();
    [SerializeField] List<string> legacyCraftFiles = new();
    [SerializeField] string legacyDayFileName = "day0.json";
    [SerializeField] string legacyCraftFileName = "day0_crafts.json";

    Dictionary<int, NewDayScriptBase> _dayScriptCache = new();

    /// <summary>구형 일차/제조 대본. 파일 이름이 곧 키다(구형 IDataSwitcher가 이름으로 부른다).</summary>
    readonly Dictionary<string, DayDatabBase> _legacyDayCache = new();
    readonly Dictionary<string, CraftDataBase> _legacyCraftCache = new();

    /// <summary>방금 읽은 신형 json/text_tags.json. 구형 SO로 옮겨 담는 것 말고 쓰는 곳이 없어 SO를 두지 않는다.</summary>
    Dictionary<string, NewTextTagData> _textTags;

    /// <summary>
    /// 2부 바 대본(script/bar/dayN.json). 일차별로 들고 있다가 2부가 시작할 때 그날 것을 꺼내 쓴다.
    ///
    /// day_N.json처럼 SO 한 칸에 갈아 끼우지 않는다. 그쪽은 "지금 보고 있는 하루" 하나만 있으면 되지만,
    /// 바 대본은 없는 날(Day 3)과 있는 날을 구분해야 해서 없다는 사실 자체가 값이다.
    ///
    /// static인 이유는 이 로더가 Play 씬에 없기 때문이다. VContainer 루트 스코프(ProjectLifeScope)에
    /// 얹혀 실행 중에 만들어지므로 씬의 오브젝트가 인스펙터로 꽂을 수 없고, 등록도
    /// AsImplementedInterfaces뿐이라 구체 타입으로 주입받을 수도 없다. 로딩 완료 신호와 같은 사정이다.
    /// </summary>
    static readonly Dictionary<int, NewDayScriptBase> _barScriptCache = new();

    /// <summary>
    /// 그날의 2부 바 대본을 꺼낸다. 그 일차의 파일이 아예 없으면 false —
    /// 씬이 0개인 것(Day 3)과 파일이 없는 것은 다르므로, 부르는 쪽이 구분할 수 있게 나눠 돌려준다.
    /// </summary>
    public static bool TryGetBarScript(int day, out NewDayScriptBase script)
    {
        return _barScriptCache.TryGetValue(day, out script) && script != null;
    }

    static UniTaskCompletionSource loadCompletion = new();

    /// <summary>
    /// json 로딩이 끝났는지.
    ///
    /// 로딩은 IAsyncStartable로 프레임을 넘겨 가며 진행되는데, 씬의 MonoBehaviour들은 그와 무관하게
    /// Awake·Start를 먼저 마친다. 그래서 로딩보다 먼저 도는 쪽이 기다릴 수 있어야 한다.
    ///
    /// 기다리지 않으면 SO에 구워져 있는 빈 배열을 실제 데이터로 읽는다 — 예외도 나지 않고 그냥
    /// "오늘 손님 0명"이 되어 1부를 건너뛰는 식으로 조용히 어긋난다.
    /// </summary>
    public static bool IsLoaded { get; private set; }

    /// <summary>로딩이 끝날 때까지 기다린다. 이미 끝났으면 곧바로 돌아온다.</summary>
    public static UniTask WaitUntilLoadedAsync() => loadCompletion.Task;

    /// <summary>로딩을 다시 시작할 때 완료 신호를 되돌린다(타이틀로 나갔다 다시 들어오는 경우).</summary>
    static void BeginLoad()
    {
        if (!IsLoaded) return;

        IsLoaded = false;
        loadCompletion = new UniTaskCompletionSource();
    }

    static void EndLoad()
    {
        IsLoaded = true;
        loadCompletion.TrySetResult();
    }

    /// <summary>VContainer가 컨테이너 빌드 시점에 호출하는 엔트리포인트. LoadDataAsync를 기다린 뒤 완료된다.</summary>
    
    async UniTask IAsyncStartable.StartAsync(CancellationToken cancellation)
    {
        await LoadDataAsync();
    }

    static string DayScriptFileName(int day) => $"json/script/day_{day}.json";

    static string BarScriptFileName(int day) => $"json/script/bar/day{day}.json";

    // ══ 구형 데이터 ═══════════════════════════════════════════════════
    //
    // 구형 DataLoadManager가 하던 일을 그대로 이어받는다. 그쪽이 꺼져도 실외 씬, 컷씬, 정산,
    // 대사창 색 태그, 인물 그림이 서 있게 하려는 것이다.

    /// <summary>구형 파일들을 동기로 읽는다.</summary>
    void LoadLegacyData()
    {
        foreach (var file in legacyDayFiles)
            _legacyDayCache[file] = JsonManager<DayDatabBase>.LoadGameData_StreamingAssets(file);

        foreach (var file in legacyCraftFiles)
            _legacyCraftCache[file] = JsonManager<CraftDataBase>.LoadGameData_StreamingAssets(file);

        SwitchLegacyDayIfAny();

        if (legacyCocktailData != null)
            legacyCocktailData.cocktailData = JsonManager<CocktailDataBase>.LoadGameData_StreamingAssets(legacyCocktailFileName);

        if (legacyIngredientData != null)
            legacyIngredientData.ingredientData = JsonManager<IngredientDataBase>.LoadGameData_StreamingAssets(legacyIngredientFileName);

        if (legacyCutSceneData != null)
            legacyCutSceneData.cutSceneData = JsonManager<CutSceneDataBase>.LoadGameData_StreamingAssets(legacyCutSceneFileName);

        if (legacySettlementData != null)
            legacySettlementData.settlementData = JsonManager<SettlementDataBase>.LoadGameData_StreamingAssets(legacySettlementFileName);

        if (legacyCharacterTierData != null)
            legacyCharacterTierData.characterTiers = JsonManager<CharacterTierDataBase>.LoadGameData_StreamingAssets(legacyCharacterTierFileName);

        if (legacySkillTierData != null)
            legacySkillTierData.skillTier = JsonManager<SkillTierDataBase>.LoadGameData_StreamingAssets(legacySkillTierFileName);

        _textTags = JsonManager<Dictionary<string, NewTextTagData>>.LoadGameData_StreamingAssets(textTagFileName);

        ApplyLegacyData();
    }

    /// <summary>구형 파일들을 비동기로 읽는다. 순서와 결과는 동기 경로와 같다.</summary>
    async UniTask LoadLegacyDataAsync()
    {
        foreach (var file in legacyDayFiles)
            _legacyDayCache[file] = await JsonManager<DayDatabBase>.LoadAsync<DayDatabBase>(file);

        foreach (var file in legacyCraftFiles)
            _legacyCraftCache[file] = await JsonManager<CraftDataBase>.LoadAsync<CraftDataBase>(file);

        SwitchLegacyDayIfAny();

        if (legacyCocktailData != null)
            legacyCocktailData.cocktailData = await JsonManager<CocktailDataBase>.LoadAsync<CocktailDataBase>(legacyCocktailFileName);

        if (legacyIngredientData != null)
            legacyIngredientData.ingredientData = await JsonManager<IngredientDataBase>.LoadAsync<IngredientDataBase>(legacyIngredientFileName);

        if (legacyCutSceneData != null)
            legacyCutSceneData.cutSceneData = await JsonManager<CutSceneDataBase>.LoadAsync<CutSceneDataBase>(legacyCutSceneFileName);

        if (legacySettlementData != null)
            legacySettlementData.settlementData = await JsonManager<SettlementDataBase>.LoadAsync<SettlementDataBase>(legacySettlementFileName);

        if (legacyCharacterTierData != null)
            legacyCharacterTierData.characterTiers = await JsonManager<CharacterTierDataBase>.LoadAsync<CharacterTierDataBase>(legacyCharacterTierFileName);

        if (legacySkillTierData != null)
            legacySkillTierData.skillTier = await JsonManager<SkillTierDataBase>.LoadAsync<SkillTierDataBase>(legacySkillTierFileName);

        _textTags = await JsonManager<Dictionary<string, NewTextTagData>>.LoadAsync<Dictionary<string, NewTextTagData>>(textTagFileName);

        ApplyLegacyData();
    }

    /// <summary>
    /// 신형에서 옮겨 담을 것을 옮기고, 구형 SO들이 요구하는 캐시를 만든다.
    ///
    /// 캐시(Cached)는 구형 SO가 조회용 딕셔너리를 따로 들고 있어서다. 로드만 하고 부르지 않으면
    /// 데이터는 들어와 있는데 조회만 빈다 — 터지지도 않아서 원인이 멀어진다.
    /// </summary>
    void ApplyLegacyData()
    {
        if (legacyCharacterAnimConfig != null)
        {
            legacyCharacterAnimConfig.animConfig = NewLegacyDataBridge.ToCharacterAnim(expressionData?.expressionData);

            if ((legacyCharacterAnimConfig.animConfig?.Characters?.Count ?? 0) == 0)
                Logger.LogWarning($"[New] 표정 데이터가 비어 구형 표정 SO를 채우지 못했습니다({expressionFileName}).");
        }

        if (legacyCharacterData != null)
            legacyCharacterData.characterData = NewLegacyDataBridge.ToCharacters(characterData?.characterData);

        if (legacyTextTagData != null)
            legacyTextTagData.textTagData = NewLegacyDataBridge.ToTextTags(_textTags);

        // 구형은 이 셋을 무조건 불렀다. 원본이 비었을 때 그 안에서 터지므로 여기서 걸러 준다.
        if (legacyCocktailData?.cocktailData != null) legacyCocktailData.Cached();
        if (legacyCutSceneData?.cutSceneData != null) legacyCutSceneData.Cached();
        if (legacySettlementData?.settlementData?.DailySettlements != null) legacySettlementData.Cached();

        outsideDataManager?.Load();
    }

    /// <summary>읽어 둔 것이 있을 때만 구형 일차를 맞춘다.</summary>
    void SwitchLegacyDayIfAny()
    {
        if (_legacyDayCache.Count == 0 && _legacyCraftCache.Count == 0) return;

        SwitchDay(legacyDayFileName, legacyCraftFileName);
    }

    /// <summary>
    /// 구형 일차/제조 대본을 갈아 끼운다(IDataSwitcher). HomeManager가 하루를 넘길 때 부른다.
    ///
    /// 구형은 캐시에 없는 이름을 받으면 KeyNotFoundException으로 멈췄다. 여기서는 경고만 남기고
    /// 지나간다 — 이 경로는 신형 대본으로 대체된 레거시라, 없는 날 하나가 게임을 세울 이유가 없다.
    /// </summary>
    public void SwitchDay(string dayFile, string craftFile)
    {
        if (legacyDayData != null)
        {
            if (_legacyDayCache.TryGetValue(dayFile, out DayDatabBase day)) legacyDayData.dayData = day;
            else Logger.LogWarning($"[New] 구형 일차 대본을 읽어 두지 않았습니다: {dayFile}");
        }

        if (legacyCraftData != null)
        {
            if (_legacyCraftCache.TryGetValue(craftFile, out CraftDataBase craft)) legacyCraftData.craftData = craft;
            else Logger.LogWarning($"[New] 구형 제조 대본을 읽어 두지 않았습니다: {craftFile}");
        }
    }

    public void LoadData()
    {
        Logger.Log("[New] Load Data");
        BeginLoad();

        foreach (var day in dayNumbers)
            _dayScriptCache[day] = JsonManager<NewDayScriptBase>.LoadGameData_StreamingAssets(DayScriptFileName(day));

        _barScriptCache.Clear();
        foreach (var day in barDayNumbers)
        {
            var bar = JsonManager<NewDayScriptBase>.LoadGameData_StreamingAssets(BarScriptFileName(day));
            if (bar != null) _barScriptCache[day] = bar;
        }

        commonScriptData.dayScriptData = JsonManager<NewDayScriptBase>.LoadGameData_StreamingAssets(commonScriptFileName);

        SwitchDayIfAny();

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
        homeinteractPointData.interactPointData = JsonManager<NewInteractPointData[]>.LoadGameData_StreamingAssets(HomeinteractPointFileName);
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

        // 구형 데이터가 하나 삐끗해도 로딩이 여기서 멈추면 안 된다. 완료 신호(EndLoad)가 그 뒤에 있어서,
        // 예외가 새어 나가면 대본을 기다리던 쪽이 영영 안 깨어난다 — 아무 로그 없이 게임이 선다.
        try { LoadLegacyData(); }
        catch (Exception e) { Logger.LogError($"[New] 구형 데이터를 읽지 못했습니다. 그것을 쓰는 화면만 빕니다: {e}"); }

        // 완료를 알리기 전에 찍는다. UniTask는 TrySetResult 시점에 기다리던 쪽을 동기로 이어서 돌리기
        // 때문에, 순서를 바꾸면 로딩이 끝났다는 줄보다 그 뒤에 벌어지는 일이 먼저 찍힌다.
        Logger.Log("[New] Load end");
        EndLoad();
    }

    public async UniTask LoadDataAsync()
    {
        Logger.Log("[New] Load Data");
        BeginLoad();

        foreach (var day in dayNumbers)
            _dayScriptCache[day] = await LoadOptionalAsync(DayScriptFileName(day));

        _barScriptCache.Clear();
        foreach (var day in barDayNumbers)
        {
            NewDayScriptBase bar = await LoadOptionalAsync(BarScriptFileName(day));
            if (bar != null) _barScriptCache[day] = bar;
        }

        commonScriptData.dayScriptData = await JsonManager<NewDayScriptBase>.LoadAsync<NewDayScriptBase>(commonScriptFileName);

        SwitchDayIfAny();

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
        homeinteractPointData.interactPointData = await JsonManager<NewInteractPointData[]>.LoadAsync<NewInteractPointData[]>(HomeinteractPointFileName);
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

        // 구형 데이터가 하나 삐끗해도 로딩이 여기서 멈추면 안 된다. 완료 신호(EndLoad)가 그 뒤에 있어서,
        // 예외가 새어 나가면 대본을 기다리던 쪽이 영영 안 깨어난다 — 아무 로그 없이 게임이 선다.
        try { await LoadLegacyDataAsync(); }
        catch (Exception e) { Logger.LogError($"[New] 구형 데이터를 읽지 못했습니다. 그것을 쓰는 화면만 빕니다: {e}"); }

        // 완료를 알리기 전에 찍는다. UniTask는 TrySetResult 시점에 기다리던 쪽을 동기로 이어서 돌리기
        // 때문에, 순서를 바꾸면 로딩이 끝났다는 줄보다 그 뒤에 벌어지는 일이 먼저 찍힌다.
        Logger.Log("[New] Load end");
        EndLoad();
    }

    /// <summary>
    /// 대본 파일 하나를 읽되, 없으면 예외 대신 null을 돌려준다.
    ///
    /// 로딩은 파일을 순서대로 기다리며 진행하기 때문에, 중간에 하나가 예외를 던지면 그 뒤 파일이
    /// 통째로 로드되지 않는다. 대본은 일차에 따라 없을 수 있는 데이터라(빈 날, 아직 안 쓴 날)
    /// 그 하나 때문에 게임 전체 데이터가 비는 것은 맞지 않는다.
    /// </summary>
    static async UniTask<NewDayScriptBase> LoadOptionalAsync(string fileName)
    {
        try
        {
            return await JsonManager<NewDayScriptBase>.LoadAsync<NewDayScriptBase>(fileName);
        }
        catch (Exception e)
        {
            Logger.LogWarning($"[New] 대본을 읽지 못해 건너뜁니다: {fileName} / {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// day_N.json을 읽어 두었을 때만 현재 일차를 맞춘다.
    ///
    /// 그 대본은 bar 대본으로 대체된 레거시라 지금은 dayNumbers가 비어 있고 파일도 없다. 그런데도
    /// SwitchDay를 부르면 "대본이 없다"는 경고가 매 실행 뜨는데, 없는 것이 정상인 상태라 그 경고는
    /// 진짜 문제를 가린다.
    /// </summary>
    void SwitchDayIfAny()
    {
        if (dayNumbers == null || dayNumbers.Count == 0) return;

        SwitchDay(isTest ? testDay : startDay);
    }

    /// <summary>
    /// day_N.json을 dayScriptData에 주입한다. common.json은 날짜와 무관하게 commonScriptData에 항상 고정되어 있다.
    ///
    /// day_N.json은 bar 대본으로 대체된 레거시라 지금은 파일이 없다. 읽는 쪽도 없어서 비어 있어도
    /// 문제가 되지 않지만, 부르는 곳이 남아 있으므로 없는 일차를 만나도 멈추지 않게 둔다.
    /// </summary>
    public void SwitchDay(int day)
    {
        if (!_dayScriptCache.TryGetValue(day, out NewDayScriptBase script) || script == null)
        {
            Logger.LogWarning($"[New] {day}일차 대본(day_{day}.json)이 없어 현재 대본을 비웁니다.");
            dayScriptData.dayScriptData = null;
            return;
        }

        dayScriptData.dayScriptData = script;
    }


}
