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
    [SerializeField] GuestSlot[] slots;

    [Header("Test")]
    [SerializeField] bool useTempAppearance; // true면 파츠 addressable 로딩을 생략하고 GuestSlot의 임시 오브젝트만 On/Off한다.

    [Inject] TycoonFlow tycoonFlow;

    NewBalanceConfig config;
    readonly Queue<Guest> guestQueue = new();

    public int QueuedGuestCount => guestQueue.Count;

    public void Start()
    {
        config = configData.balanceData.Config;

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
    /// TODO: 빈 슬롯이 없을 때의 대기/재시도 처리.
    /// </summary>
    public async UniTask RunSpawnLoopAsync(CancellationToken token)
    {
        bool isFirstGuest = true;

        while (TryDequeueNextGuest(out Guest guest))
        {
            float delay = isFirstGuest ? config.FirstSpawnDelaySec : guest.delaySec;
            isFirstGuest = false;

            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);

            TrySeatGuest(guest);
        }
    }

    Guest CreateFromRandomWave(NewRandomWaveData wave)
    {
        var (tipMult, patienceMult) = GetPersonalityMultipliers(wave.Personality);

        var guest = new Guest
        {
            id = $"random_{wave.Day}_{wave.Seq}",
            targetCocktailId = PickTargetCocktailId(wave.Tier),
            difficultyLevel = wave.Tier,
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
            targetCocktailId = PickTargetCocktailId(slot.Tier),
            difficultyLevel = slot.Tier,
            tipMultiplier = 1f,
            hasPatience = false,
            maxRounds = slot.MaxRounds,
            delaySec = slot.DelaySec,
            isRegular = true,
        };
    }

    /// <summary>cocktails.json에서 tier가 같은 칵테일들 중 하나를 무작위로 골라 id를 반환한다. 없으면 null.</summary>
    string PickTargetCocktailId(int tier)
    {
        var candidates = cocktailData.cocktailData.Where(c => c.Tier == tier).ToArray();
        if (candidates.Length == 0) return null;

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
        return true;
    }

    /// <summary>barks.json에서 situation이 일치하는 대사 중 weight를 가중치로 하나를 뽑아 슬롯의 말풍선에 띄운다. 일치하는 대사가 없으면 아무것도 하지 않는다.</summary>
    public void ShowBark(GuestSlot slot, string situation, float durationSec = 3f)
    {
        var candidates = barkData.barkData.Where(b => b.Situation == situation).ToArray();
        if (candidates.Length == 0) return;

        slot.ShowBark(PickWeightedBark(candidates).Text.Ko, durationSec);
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

    /// <summary>지정 슬롯의 손님 응대가 끝났을 때 호출한다. 슬롯을 비우고 TycoonFlow에 알린다.</summary>
    public void ReleaseGuest(GuestSlot slot)
    {
        slot.Clear();
        tycoonFlow.OnCustomerHandled();
    }

    /// <summary>손님 id로 현재 앉아있는 슬롯을 찾는다. 없으면 null.</summary>
    public GuestSlot FindSlotByGuestId(string guestId)
    {
        return slots.FirstOrDefault(s => s.CurrentGuest?.id == guestId);
    }
}
