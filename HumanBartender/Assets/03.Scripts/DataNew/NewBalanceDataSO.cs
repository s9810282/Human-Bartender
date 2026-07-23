using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewBalanceConfig
{
    [field: SerializeField][JsonProperty("gold_start")] public int GoldStart { get; set; }
    [field: SerializeField][JsonProperty("reputation_start")] public int ReputationStart { get; set; }
    [field: SerializeField][JsonProperty("commute_in_time")] public string CommuteInTime { get; set; }
    [field: SerializeField][JsonProperty("commute_out_time")] public string CommuteOutTime { get; set; }
    [field: SerializeField][JsonProperty("coaster_base_sec")] public float CoasterBaseSec { get; set; }
    [field: SerializeField][JsonProperty("coaster_per_tier_sec")] public float CoasterPerTierSec { get; set; }
    [field: SerializeField][JsonProperty("coaster_min_sec")] public float CoasterMinSec { get; set; }
    [field: SerializeField][JsonProperty("serve_bonus_sec")] public float ServeBonusSec { get; set; }
    [field: SerializeField][JsonProperty("serve_per_tier_sec")] public float ServePerTierSec { get; set; }
    [field: SerializeField][JsonProperty("serve_min_bonus_sec")] public float ServeMinBonusSec { get; set; }
    [field: SerializeField][JsonProperty("warn_yellow_ratio")] public float WarnYellowRatio { get; set; }
    [field: SerializeField][JsonProperty("warn_red_ratio")] public float WarnRedRatio { get; set; }
    [field: SerializeField][JsonProperty("time_limit_base_sec")] public float TimeLimitBaseSec { get; set; }
    [field: SerializeField][JsonProperty("time_limit_per_gimmick")] public float TimeLimitPerGimmick { get; set; }
    [field: SerializeField][JsonProperty("spawn_delay_default")] public float SpawnDelayDefault { get; set; }
    [field: SerializeField][JsonProperty("next_round_delay_sec")] public float NextRoundDelaySec { get; set; }
    [field: SerializeField][JsonProperty("drunk_vomit_chance")] public float DrunkVomitChance { get; set; }
    [field: SerializeField][JsonProperty("wrong_cocktail_revenue_mult")] public float WrongCocktailRevenueMult { get; set; }
    [field: SerializeField][JsonProperty("leave_coaster_rep")] public int LeaveCoasterRep { get; set; }
    [field: SerializeField][JsonProperty("leave_serve_rep")] public int LeaveServeRep { get; set; }
    [field: SerializeField][JsonProperty("autosave_interval_step")] public int AutosaveIntervalStep { get; set; }
}

[Serializable]
public struct NewGradeCutData
{
    [field: SerializeField][JsonProperty("grade")] public ENewGrade Grade { get; set; }
    [field: SerializeField][JsonProperty("min_pct")] public float MinPct { get; set; }
    [field: SerializeField][JsonProperty("tier_override")] public string TierOverride { get; set; }
}

[Serializable]
public struct NewGradePayoutData
{
    [field: SerializeField][JsonProperty("revenue_mult")] public float RevenueMult { get; set; }
    [field: SerializeField][JsonProperty("tip_mult")] public float TipMult { get; set; }
}

[Serializable]
public struct NewAffinityMatrixRow
{
    [field: SerializeField][JsonProperty("taste_tier")] public ENewTasteTier TasteTier { get; set; }
    [field: SerializeField][JsonProperty("excellent")] public int Excellent { get; set; }
    [field: SerializeField][JsonProperty("good")] public int Good { get; set; }
    [field: SerializeField][JsonProperty("decent")] public int Decent { get; set; }
    [field: SerializeField][JsonProperty("poor")] public int Poor { get; set; }
    [field: SerializeField][JsonProperty("sewage")] public int Sewage { get; set; }
}

[Serializable]
public class NewBalanceDataBase
{
    [field: SerializeField][JsonProperty("config")] public NewBalanceConfig Config { get; set; }
    [field: SerializeField][JsonProperty("grade_cuts")] public NewGradeCutData[] GradeCuts { get; set; }
    [JsonProperty("grade_payout")] public Dictionary<string, NewGradePayoutData> GradePayout { get; set; }
    [field: SerializeField][JsonProperty("affinity_matrix")] public NewAffinityMatrixRow[] AffinityMatrix { get; set; }
}

/// <summary>StreamingAssets/json/balance.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewBalanceDataSO", menuName = "Data/New/BalanceDataSO")]
public class NewBalanceDataSO : ScriptableObject
{
    public NewBalanceDataBase balanceData;
}
