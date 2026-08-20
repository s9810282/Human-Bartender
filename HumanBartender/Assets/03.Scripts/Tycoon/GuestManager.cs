using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 타이쿤(1부) 국면의 손님 슬롯(GuestSlot) 전체를 관리한다. 랜덤 손님(random_waves.json)과
/// 단골 손님(regular_slots.json)을 같은 day의 seq 순서로 합쳐 대기열을 구성하고, 빈 슬롯에 배정한다.
/// 손님이 자리를 떠나면 TycoonFlow에 응대 완료를 알려 남은 손님 수를 갱신시킨다.
/// targetCocktailId는 cocktails.json에서 같은 tier의 칵테일 중 무작위로 배정한다.
/// TODO: personality/max_rounds/branch_choice 반영 로직 연결.
/// </summary>
public class GuestManager : MonoBehaviour
{
    [SerializeField] NewBalanceDataSO configData;
    [SerializeField] NewRandomWaveDataSO randomWaveData;
    [SerializeField] NewRegularSlotDataSO regularSlotData;
    [SerializeField] NewCocktailDataSO cocktailData;
    [SerializeField] NewPersonalityDataSO personalityData;
    [SerializeField] NewGuestBodyDataSO guestBodyData;
    [SerializeField] NewBarkDataSO barkData;
    [SerializeField] TextTagDataSO textTagData;
    [SerializeField] NewCharacterDataSO characterData;
    [SerializeField] GuestSlot[] slots;

    [Header("Dialogue")]
    [SerializeField] UIDialogueTextView dialogueTextView; // ask_order 등 플레이어(바텐더) 대사를 띄우는 다이얼로그 말풍선

    [Header("Test")]
    [SerializeField] bool useTempAppearance; // true면 파츠 addressable 로딩을 생략하고 GuestSlot의 임시 오브젝트만 On/Off한다.

    const string PlayerCharacterId = "luna";
    const float PlayerBarkDurationSec = 3f;

    NewBalanceConfig config;
    readonly Queue<Guest> guestQueue = new();
    readonly Dictionary<GuestSlot, CancellationTokenSource> patienceCtsBySlot = new();
    CancellationTokenSource lunaBarkCts;

    public int QueuedGuestCount => guestQueue.Count;

    /// <summary>손님 한 명의 응대(정상 퇴장) 또는 이탈이 끝나 슬롯이 비워졌을 때 발생한다.</summary>
    public event Action<Guest> GuestReleased;

    public void Start()
    {
        config = configData.balanceData.Config;

        if (useTempAppearance)
            GameStateManager.Instance.CurrentDay = 2;

        foreach (var slot in slots)
            slot.SetTempAppearanceMode(useTempAppearance);

        BuildGuestQueue();
    }

    /// <summary>
    /// 오늘(GameStateManager.CurrentDay) 등장할 랜덤 손님과 단골 손님을 seq 순서로 합쳐 대기열을 구성한다.
    /// </summary>
    public void BuildGuestQueue()
    {
        int day = GameStateManager.Instance.CurrentDay;
        guestQueue.Clear();

        var randomGuests = randomWaveData.randomWaveData
            .Where(wave => wave.Day == day)
            .Select(wave => (wave.Seq, Guest: CreateFromRandomWave(wave)));

        var regularGuests = regularSlotData.regularSlotData
            .Where(slot => slot.Day == day)
            .Select(slot => (slot.Seq, Guest: CreateFromRegularSlot(slot)));

        foreach (var entry in randomGuests.Concat(regularGuests).OrderBy(entry => entry.Seq))
            guestQueue.Enqueue(entry.Guest);
    }

    /// <summary>대기열에서 다음 손님을 꺼낸다. 대기열이 비어있으면 false.</summary>
    public bool TryDequeueNextGuest(out Guest guest)
    {
        return guestQueue.TryDequeue(out guest);
    }

    /// <summary>
    /// 대기열의 손님을 순서대로 등장시킨다. 첫 손님은 balance.json의 first_spawn_delay_sec만큼 기다린 뒤 등장하고,
    /// 이후 손님들은 각자의 delaySec(앞선 손님에 이어 등장하기까지의 대기 시간)만큼 기다린다.
    /// 슬롯이 모두 차있으면 빈 슬롯이 생길 때까지 대기한 뒤, 남은 delay를 마저 적용하고 착석시킨다.
    /// </summary>
    public async UniTask RunSpawnLoopAsync(CancellationToken token)
    {
        bool isFirstGuest = true;

        while (TryDequeueNextGuest(out Guest guest))
        {
            float delay = isFirstGuest ? config.FirstSpawnDelaySec : guest.delaySec;
            isFirstGuest = false;

            double delayEndTime = Time.timeAsDouble + delay;

            if (IsAllSlotsOccupied())
                await UniTask.WaitUntil(() => !IsAllSlotsOccupied(), cancellationToken: token);

            double remainingDelay = delayEndTime - Time.timeAsDouble;
            if (remainingDelay > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(remainingDelay), cancellationToken: token);

            TrySeatGuest(guest);
        }
    }

    /// <summary>슬롯이 하나도 비어있지 않은지 확인한다.</summary>
    bool IsAllSlotsOccupied()
    {
        return slots.All(s => !s.IsEmpty);
    }

    Guest CreateFromRandomWave(NewRandomWaveData wave)
    {
        var (tipMult, patienceMult) = GetPersonalityMultipliers(wave.Personality);

        var guest = new Guest
        {
            id = $"random_{wave.Day}_{wave.Seq}",
            targetCocktailId = ResolveOrderCocktailId(wave.Order),
            personality = wave.Personality,
            tipMultiplier = tipMult,
            patienceMultiplier = patienceMult,
            maxRounds = wave.MaxRounds,
            delaySec = wave.DelaySec,
            isRegular = false,
        };

        guest.appearance = PickRandomAppearance();

        if (!useTempAppearance)
            LoadAppearanceAsync(guest).Forget();

        return guest;
    }

    /// <summary>
    /// 성별을 먼저 무작위로 정한 뒤, guest_bodies.json의 bodies/outfits/eyes/hairs 각각을 같은 gender로 필터링해
    /// weight를 가중치로 하나씩 뽑는다. 파츠 4개가 항상 같은 성별로 맞춰진다.
    /// </summary>
    GuestBodyAppearance PickRandomAppearance()
    {
        var bodies = guestBodyData.guestBodyData;
        string gender = UnityEngine.Random.value < 0.5f ? "m" : "f";

        return new GuestBodyAppearance
        {
            Body = PickWeighted(FilterByGender(bodies.Bodies, gender)),
            Outfit = PickWeighted(FilterByGender(bodies.Outfits, gender)),
            Eyes = PickWeighted(FilterByGender(bodies.Eyes, gender)),
            Hair = PickWeighted(FilterByGender(bodies.Hairs, gender)),
        };
    }

    /// <summary>gender가 일치하는 파츠만 남긴다. 일치하는 항목이 없으면 필터링 없이 전체를 반환한다.</summary>
    static NewGuestBodyPartData[] FilterByGender(NewGuestBodyPartData[] parts, string gender)
    {
        var filtered = parts.Where(p => p.Gender == gender).ToArray();
        return filtered.Length > 0 ? filtered : parts;
    }

    static NewGuestBodyPartData PickWeighted(NewGuestBodyPartData[] parts)
    {
        int totalWeight = parts.Sum(p => p.Weight);
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var part in parts)
        {
            cumulative += part.Weight;
            if (roll < cumulative) return part;
        }

        return parts[^1];
    }

    /// <summary>
    /// guest.appearance에 배정된 4개 파츠 스프라이트를 addressable로 병렬 로드해 guest.bodySprites에 채운다.
    /// 손님이 대기열에 들어간 직후(등장 전) 미리 호출되어, 실제 자리에 앉을 때는 이미 로드가 끝나있도록 한다.
    /// </summary>
    async UniTaskVoid LoadAppearanceAsync(Guest guest)
    {
        var token = this.GetCancellationTokenOnDestroy();
        var appearance = guest.appearance;

        var (bodyHandle, outfitHandle, eyesHandle, hairHandle) = await UniTask.WhenAll(
            ResourceLoader.TryLoadAsync<Sprite>(appearance.Body.Sprite, token),
            ResourceLoader.TryLoadAsync<Sprite>(appearance.Outfit.Sprite, token),
            ResourceLoader.TryLoadAsync<Sprite>(appearance.Eyes.Sprite, token),
            ResourceLoader.TryLoadAsync<Sprite>(appearance.Hair.Sprite, token));

        var sprites = new GuestBodySprites();
        sprites.SetBody(bodyHandle);
        sprites.SetOutfit(outfitHandle);
        sprites.SetEyes(eyesHandle);
        sprites.SetHair(hairHandle);

        guest.bodySprites = sprites;
    }

    Guest CreateFromRegularSlot(NewRegularSlotData slot)
    {
        // 단골은 personalities.json을 참조하지 않는다: 팁 배율은 항상 1, 인내심 개념 자체가 없어 hasPatience=false로 예외 처리한다.
        return new Guest
        {
            id = slot.Character,
            characterId = slot.Character,
            targetCocktailId = ResolveOrderCocktailId(slot.Order),
            tipMultiplier = 1f,
            hasPatience = false,
            maxRounds = slot.MaxRounds,
            delaySec = slot.DelaySec,
            isRegular = true,
        };
    }

    /// <summary>
    /// 이 손님이 주문할 칵테일을 정한다.
    ///
    /// 웨이브 데이터에 주문이 적혀 있으면 그대로 쓴다. 스토리상 특정 칵테일을 주문해야 하는 손님이
    /// 그렇게 지정되어 있다. 비어 있으면 오늘 만들 수 있는 칵테일 중에서 하나를 무작위로 고른다.
    /// </summary>
    string ResolveOrderCocktailId(string order)
    {
        if (!string.IsNullOrEmpty(order))
        {
            // 지정된 주문은 오늘 해금되지 않았더라도 그대로 따른다. 스토리가 요구한 주문이라
            // 시스템이 임의로 다른 칵테일로 바꾸면 그 장면이 성립하지 않는다.
            if (cocktailData.TryGet(order, out _)) return order;

            Logger.Log($"[Guest] 지정된 주문 '{order}'을 cocktails.json에서 찾지 못했습니다.");
        }

        return PickRandomAvailableCocktailId();
    }

    /// <summary>
    /// 오늘 기준으로 제조 가능한 칵테일 중 하나를 무작위로 고른다.
    /// 데이터가 아직 확정되지 않은(status=tbd) 칵테일은 주문에 넣지 않는다 — 레시피가 비어 있어
    /// 만들 방법이 없기 때문이다.
    /// </summary>
    string PickRandomAvailableCocktailId()
    {
        int day = GameStateManager.Instance.CurrentDay;

        var candidates = cocktailData.cocktailData
            .Where(c => c.UnlockDay <= day && c.Status == ENewDataStatus.Confirmed)
            .ToArray();

        if (candidates.Length == 0)
        {
            Logger.Log($"[Guest] {day}일차에 주문할 수 있는 칵테일이 없습니다.");
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Length)].Id;
    }

    /// <summary>personalities.json에서 personalityId와 일치하는 tip_mult/patience_mult를 찾는다. 없으면 기본값 (1.0, 1.0).</summary>
    (float tipMult, float patienceMult) GetPersonalityMultipliers(string personalityId)
    {
        if (!string.IsNullOrEmpty(personalityId))
        {
            foreach (var p in personalityData.personalityData)
            {
                if (p.Id == personalityId)
                    return (p.TipMult, p.PatienceMult);
            }
        }

        return (1f, 1f);
    }

    /// <summary>비어있는 슬롯을 찾아 손님을 배정한다. 자리가 없으면 false를 반환한다.</summary>
    public bool TrySeatGuest(Guest guest)
    {
        GuestSlot slot = slots.FirstOrDefault(s => s.IsEmpty);
        if (slot == null) return false;

        slot.Seat(guest);
        ShowBark(slot, "call");

        if (guest.hasPatience)
            StartPatienceTimer(slot, guest);

        return true;
    }

    /// <summary>
    /// 슬롯의 인내 타이머를 시작한다. 이미 돌고 있던 타이머가 있으면(재착석 등) 먼저 멈추고 새로 시작한다.
    /// </summary>
    void StartPatienceTimer(GuestSlot slot, Guest guest)
    {
        WatchPatienceAsync(slot, guest, RegisterSlotTimer(slot)).Forget();
    }

    /// <summary>
    /// 이 슬롯에 대해 진행 중이던 타이머(인내심/서빙 대기 등)를 멈추고, 새 타이머 하나가 쓸 CancellationToken을 등록한다.
    /// GuestManager가 파괴될 때(this.GetCancellationTokenOnDestroy)도 함께 취소된다.
    /// </summary>
    CancellationToken RegisterSlotTimer(GuestSlot slot)
    {
        StopPatienceTimer(slot);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        patienceCtsBySlot[slot] = cts;
        return cts.Token;
    }

    /// <summary>
    /// 진행 중인 슬롯 타이머(인내심 대기/서빙 대기)를 중간에 멈춘다. 예: 코스터를 받아 다음 단계로 넘어가거나,
    /// 응대가 끝나 더 이상 이탈 카운트다운이 필요 없어졌을 때 호출한다. 돌고 있는 타이머가 없으면 아무것도 하지 않는다.
    /// </summary>
    public void StopPatienceTimer(GuestSlot slot)
    {
        if (!patienceCtsBySlot.Remove(slot, out var cts)) return;

        cts.Cancel();
        cts.Dispose();
    }

    /// <summary>
    /// 손님이 착석한 시점을 기준으로 coaster_base_sec에 손님의 patience_mult를 곱한 시간(전체 인내 시간)
    /// 동안 call_urge/call_final/leave_coaster 흐름을 진행한다.
    ///
    /// 성격 배율을 곱한 뒤 coaster_min_sec ~ coaster_max_sec로 자른다. 배율이 아무리 커도 한 손님이
    /// 무한정 기다리지 않고, 아무리 작아도 반응할 틈은 남는다.
    /// </summary>
    async UniTaskVoid WatchPatienceAsync(GuestSlot slot, Guest guest, CancellationToken token)
    {
        float patienceSec = Mathf.Clamp(config.CoasterBaseSec * guest.patienceMultiplier,
                                        config.CoasterMinSec, config.CoasterMaxSec);
        await WatchLeaveTimerAsync(slot, guest, patienceSec, "call_urge", "call_final", "leave_coaster", token);
    }

    /// <summary>
    /// totalSec을 warn_yellow_ratio/warn_red_ratio 지점으로 나눠 각각 urgeBark/finalBark 대사를 띄우고, 전체 시간이
    /// 다 지날 때까지도 같은 손님이 그대로면(=처리되지 않았으면) leaveBark 대사와 함께 이탈시킨다.
    /// 대기 도중 슬롯이 비워지거나, 다른 손님으로 교체됐거나, StopPatienceTimer로 취소되면 그 시점에서 멈춘다.
    /// </summary>
    async UniTask WatchLeaveTimerAsync(GuestSlot slot, Guest guest, float totalSec, string urgeBark, string finalBark, string leaveBark, CancellationToken token)
    {
        float yellowSec = totalSec * config.WarnYellowRatio;
        float redSec = totalSec * config.WarnRedRatio;

        if (!await WaitWhileSeatedAsync(slot, guest, yellowSec, token)) return;
        ShowBark(slot, urgeBark);

        if (!await WaitWhileSeatedAsync(slot, guest, redSec - yellowSec, token)) return;
        ShowBark(slot, finalBark);

        if (!await WaitWhileSeatedAsync(slot, guest, totalSec - redSec, token)) return;

        const float leaveBarkDurationSec = 3f;
        ShowBark(slot, leaveBark, leaveBarkDurationSec);
        slot.MarkLeaving();

        // 손님이 바로 사라지지 않고, leave 말풍선이 떠있는 동안(leaveBarkDurationSec)은 자리에 남아있다가 그 뒤에 퇴장한다.
        await UniTask.Delay(TimeSpan.FromSeconds(leaveBarkDurationSec), cancellationToken: this.GetCancellationTokenOnDestroy());

        ReleaseGuest(slot);
    }

    /// <summary>
    /// delaySec만큼 기다린 뒤, 도중에 취소되지 않았고 슬롯에 여전히 같은 손님이 앉아있으면 true.
    /// 대기 중 StopPatienceTimer 등으로 취소되면 OperationCanceledException을 잡아 false를 반환한다.
    /// </summary>
    async UniTask<bool> WaitWhileSeatedAsync(GuestSlot slot, Guest guest, float delaySec, CancellationToken token)
    {
        if (delaySec > 0f)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySec), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return !token.IsCancellationRequested && slot.CurrentGuest == guest;
    }

    /// <summary>
    /// barks.json에서 situation과 손님의 personality(voice_id)가 모두 일치하는 대사 중 weight를 가중치로 하나를 뽑아
    /// 슬롯의 말풍선에 띄운다. personality가 일치하는 대사가 없으면 voice_id가 없는(공용) 대사 중에서 고른다.
    /// 그마저 없으면 아무것도 하지 않는다.
    /// </summary>
    public void ShowBark(GuestSlot slot, string situation, float durationSec = 3f)
    {
        Guest guest = slot.CurrentGuest;
        string voiceId = guest == null ? null : guest.isRegular ? guest.characterId : guest.personality;

        var situationBarks = barkData.barkData.Where(b => b.Situation == situation).ToArray();
        if (situationBarks.Length == 0) return;

        var candidates = situationBarks.Where(b => b.VoiceId == voiceId).ToArray();
        if (candidates.Length == 0)
            candidates = situationBarks.Where(b => string.IsNullOrEmpty(b.VoiceId)).ToArray();

        if (candidates.Length == 0) return;

        string cocktailName = GetCocktailName(guest?.targetCocktailId);
        string text = DialogueTypingService.ApplyCustomTags(PickWeightedBark(candidates).Text.Ko, textTagData, cocktailName);
        slot.ShowBark(text, durationSec);
    }

    /// <summary>cocktails.json에서 cocktailId에 해당하는 이름(한글)을 찾는다. 없으면 null.</summary>
    string GetCocktailName(string cocktailId)
    {
        if (string.IsNullOrEmpty(cocktailId)) return null;

        var cocktail = cocktailData.cocktailData.FirstOrDefault(c => c.Id == cocktailId);
        return cocktail.Id != null ? cocktail.Name.Ko : null;
    }

    static NewBarkData PickWeightedBark(NewBarkData[] barks)
    {
        int totalWeight = barks.Sum(b => b.Weight);
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var bark in barks)
        {
            cumulative += bark.Weight;
            if (roll < cumulative) return bark;
        }

        return barks[^1];
    }

    /// <summary>
    /// 슬롯 앞에 코스터가 놓였을 때 호출한다(CoasterDropZone에서 드롭 판정 후 호출).
    /// 손님이 코스터를 기다리는 상태(CanReceiveCoaster)일 때만 인내심 타이머를 멈추고 주문 대기 상태로 전환한다.
    /// 이미 주문 대기/이탈 상태이거나 손님이 없으면 아무것도 하지 않고 false를 반환한다.
    /// </summary>
    public bool TryPlaceCoaster(GuestSlot slot)
    {
        if (!slot.CanReceiveCoaster) return false;

        Logger.Log("Set Coaster");

        slot.MarkWaitingOrder();

        var token = RegisterSlotTimer(slot);
        ShowPlayerAskOrderBark(slot.CurrentGuest);
        WatchOrderAsync(slot, slot.CurrentGuest, token).Forget();
        return true;
    }

    /// <summary>
    /// 주문 대기 상태가 된 뒤 3초 후 order_think, 그로부터 다시 3초 후 order 대사를 띄운다.
    /// order 대사 출력 후에는 곧바로 서빙 대기(WatchServeAsync)로 이어진다.
    /// 대기 도중 슬롯이 비워지거나 다른 손님으로 바뀌면(=먼저 응대/이탈됨) 멈춘다.
    /// </summary>
    async UniTaskVoid WatchOrderAsync(GuestSlot slot, Guest guest, CancellationToken token)
    {
        const float orderThinkDelaySec = 3f;
        const float orderDelaySec = 3f;

        if (!await WaitWhileSeatedAsync(slot, guest, orderThinkDelaySec, token)) return;
        ShowBark(slot, "order_think");

        if (!await WaitWhileSeatedAsync(slot, guest, orderDelaySec, token)) return;
        ShowBark(slot, "order");

        await WatchServeAsync(slot, guest, token);
    }

    /// <summary>
    /// 주문(order) 대사가 나간 뒤 시작되는 서빙 대기 타이머. 손님이 주문한 칵테일의 제조 제한시간에
    /// 여유를 더한 시간 동안 serve_urge/serve_final 경고를 띄우고, 시간이 다 지나면 leave_serve 대사와
    /// 함께 이탈시킨다.
    /// </summary>
    async UniTask WatchServeAsync(GuestSlot slot, Guest guest, CancellationToken token)
    {
        float serveSec = ComputeServeTimeoutSec(guest);
        await WatchLeaveTimerAsync(slot, guest, serveSec, "serve_urge", "serve_final", "leave_serve", token);
    }

    /// <summary>
    /// 서빙 제한 시간 = 주문한 칵테일의 time_limit_sec + 여유.
    ///
    /// 여유는 손님이 아니라 진행 일차로 정해진다. serve_bonus_sec에서 하루가 지날 때마다
    /// serve_grace_per_day_sec만큼 깎고, serve_min_bonus_sec 아래로는 내려가지 않는다.
    /// 뒤로 갈수록 빡빡해지지만 최소한의 여유는 남는 구조다.
    ///
    /// 이전에는 손님의 tier로 여유를 줄였는데, 데이터에서 tier가 사라지고 serve_grace_per_day_sec가
    /// 들어오면서 기준이 손님에서 일차로 옮겨간 것으로 읽었다. 밸런스 담당 확인이 필요하다.
    /// </summary>
    float ComputeServeTimeoutSec(Guest guest)
    {
        var cocktail = cocktailData.cocktailData.FirstOrDefault(c => c.Id == guest.targetCocktailId);
        float timeLimitSec = cocktail.Id != null ? cocktail.TimeLimitSec : 0f;

        int day = GameStateManager.Instance.CurrentDay;
        float dayBonus = config.ServeBonusSec - day * config.ServeGracePerDaySec;

        return timeLimitSec + Mathf.Max(config.ServeMinBonusSec, dayBonus);
    }

    /// <summary>
    /// 코스터가 놓였을 때 플레이어(바텐더, luna)의 ask_order 대사를 손님 자리의 말풍선이 아니라
    /// 다이얼로그 말풍선(UIDialogueTextView.lunaSpeechBubble)에 띄운다. barks.json에서 situation=ask_order,
    /// voice_id=luna인 대사 중 weight로 하나를 골라 표시하며, {cocktail}은 손님의 targetCocktailId로 치환된다.
    /// </summary>
    void ShowPlayerAskOrderBark(Guest guest)
    {
        var candidates = barkData.barkData.Where(b => b.Situation == "ask_order" && b.VoiceId == PlayerCharacterId).ToArray();
        if (candidates.Length == 0) return;

        string rawText = PickWeightedBark(candidates).Text.Ko;
        string cocktailName = GetCocktailName(guest?.targetCocktailId);

        var player = characterData.characterData.FirstOrDefault(c => c.Id == PlayerCharacterId);
        string speakerName = player.Id != null ? player.Name.Ko : PlayerCharacterId;

        Color nameColor = Color.white;
        if (player.Id != null)
            ColorUtility.TryParseHtmlString(player.NameColor, out nameColor);

        var typingData = new TypingData(rawText, speakerName, Vector3.zero, nameColor, isLunaSpeak: true);

        lunaBarkCts?.Cancel();
        lunaBarkCts?.Dispose();
        lunaBarkCts = new CancellationTokenSource();
        HideLunaBarkAfterAsync(typingData, cocktailName, lunaBarkCts.Token).Forget();
    }

    /// <summary>
    /// 타이핑이 끝난 뒤 PlayerBarkDurationSec만큼 더 보여주다가 다이얼로그 말풍선을 끈다.
    /// 그 사이 새 ask_order 대사가 다시 뜨면(lunaBarkCts 교체) 조용히 중단한다.
    /// </summary>
    async UniTaskVoid HideLunaBarkAfterAsync(TypingData typingData, string cocktailName, CancellationToken token)
    {
        await dialogueTextView.StartType(typingData, cocktailName);

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(PlayerBarkDurationSec), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        dialogueTextView.lunaSpeechBubble.gameObject.SetActive(false);
    }

    /// <summary>지정 슬롯의 손님 응대가 끝났을 때 호출한다. 인내 타이머를 멈추고 슬롯을 비운 뒤 TycoonFlow에 알린다.</summary>
    public void ReleaseGuest(GuestSlot slot)
    {
        Guest guest = slot.CurrentGuest;
        StopPatienceTimer(slot);
        slot.Clear();
        GuestReleased?.Invoke(guest);
    }

    /// <summary>손님 id로 현재 앉아있는 슬롯을 찾는다. 없으면 null.</summary>
    public GuestSlot FindSlotByGuestId(string guestId)
    {
        return slots.FirstOrDefault(s => s.CurrentGuest?.id == guestId);
    }
}
