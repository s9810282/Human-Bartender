using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 칵테일 한 잔을 만드는 흐름의 순서를 쥐고 있는 곳.
///
/// 메뉴에서 칵테일을 고르는 순간 제조 시도 하나를 열고, 준비가 끝나면 기믹 큐를 만들어 실행기에
/// 넘기고, 다 끝나면 기록을 확정해 알린다. 실제 일은 큐 빌더와 실행기가 하고 여기서는 순서만 안다.
///
/// 손님·코스터·정산은 다루지 않는다. 2부의 스토리 제조도 결국 같은 준비·기믹·계산을 쓰게 되는데,
/// 여기에 1부 전용 개념이 섞이면 그때 통째로 다시 만들어야 한다.
/// </summary>
public class CraftFlowController : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("칵테일을 고르는 좌측 메뉴. 여기서 고른 순간 제조가 시작된다.")]
    [SerializeField] CraftMenuPanel menuPanel;
    [Tooltip("메뉴가 들어 있는 좌측 슬라이드 패널. 제조가 시작되면 닫는다.")]
    [SerializeField] LeftSlidePanel craftPanel;
    [SerializeField] GimmickRunner runner;

    [Header("Data")]
    [SerializeField] NewCocktailDataSO cocktailData;
    [Tooltip("재료의 기본 동작(default_action)과 병 손질 여부(prep_action)를 읽는다.")]
    [SerializeField] NewShelfItemDataSO shelfData;
    [Tooltip("점수 구간표·가중치·감점값을 읽는다. 비우면 제조는 되지만 등급을 낼 수 없다.")]
    [SerializeField] NewBalanceDataSO balanceData;

    [Header("Test")]
    [Tooltip("제조 준비 화면이 아직 없어서, 켜 두면 정답 구성(잔·도구·재료)을 자동으로 골라 바로 기믹으로 넘어간다. " +
             "준비 화면이 생기면 끄고 지우면 된다.")]
    [SerializeField] bool autoPrepareForTest = true;

    /// <summary>진행 중인 제조 시도. 메뉴에서 고르기 전에는 null이다.</summary>
    public CraftSession Current { get; private set; }

    /// <summary>제조 준비 중인지. 준비 화면이 잔·도구·재료를 이 상태에서만 바꿀 수 있다.</summary>
    public bool IsPreparing => Current != null && Current.Phase == ECraftPhase.Preparing;

    /// <summary>제조가 시작돼 시도가 열렸을 때 발생한다. 준비 화면을 여는 쪽이 쓴다.</summary>
    public event Action<CraftSession> CraftBegan;

    /// <summary>모든 기믹이 끝나 기록이 확정됐을 때 발생한다. 판정 결과가 함께 온다.</summary>
    public event Action<CraftSession, CraftJudgement> CraftCompleted;

    void OnEnable()
    {
        if (menuPanel != null) menuPanel.CraftStarted += OnCraftStarted;
    }

    void OnDisable()
    {
        if (menuPanel != null) menuPanel.CraftStarted -= OnCraftStarted;
    }

    /// <summary>
    /// 메뉴에서 칵테일을 고르면 제조 시도를 연다.
    ///
    /// 이 시점에는 어느 손님에게 낼지 정해지지 않는다. 제조 화면은 손님별로 열리는 게 아니라
    /// 공통이고, 완성한 잔을 누구 앞에 놓을지는 다 만든 뒤에 정하기 때문이다.
    /// </summary>
    void OnCraftStarted(string cocktailId)
    {
        if (Current != null && Current.Phase == ECraftPhase.Playing)
        {
            Debug.LogWarning("[CraftFlow] 이미 제조 중입니다. 새 제조를 시작하지 않습니다.");
            return;
        }

        if (!cocktailData.TryGet(cocktailId, out _))
        {
            Debug.LogError($"[CraftFlow] '{cocktailId}' 칵테일을 데이터에서 찾지 못했습니다.");
            return;
        }

        // 고르는 일이 끝났으므로 메뉴를 접고 처음 화면으로 되돌린다. 상세 뷰를 켠 채로 닫으면
        // 다음에 열었을 때 지난번 칵테일 설명이 그대로 남아 있다.
        craftPanel?.Close();
        menuPanel?.ResetToMenu();

        Current = new CraftSession(cocktailId);
        CraftBegan?.Invoke(Current);

        if (autoPrepareForTest)
        {
            AutoPrepare();
            StartGimmicks();
        }
    }

    // ── 제조 준비 (준비 화면이 부를 자리) ───────────────────────────────

    public void SelectGlass(string glassId)
    {
        if (!IsPreparing) return;

        Current.Actual.SetGlass(glassId);
    }

    public void SelectTool(string toolId)
    {
        if (!IsPreparing) return;

        Current.Actual.SetTool(toolId);
    }

    /// <summary>재료를 고르거나 이미 고른 재료라면 뺀다. 선반과 하단 트레이가 같은 동작을 쓴다.</summary>
    public void ToggleIngredient(string ingredientId)
    {
        if (!IsPreparing) return;

        if (!Current.Actual.DeselectIngredient(ingredientId))
            Current.Actual.SelectIngredient(ingredientId);
    }

    /// <summary>기믹으로 넘어갈 수 있는지. 잔 하나와 재료 한 종류면 된다.</summary>
    public bool CanStartGimmicks => IsPreparing && Current.Actual.CanStartGimmicks;

    /// <summary>준비를 마치고 기믹 큐를 만들어 실행한다.</summary>
    public void StartGimmicks()
    {
        if (!CanStartGimmicks)
        {
            Debug.LogWarning("[CraftFlow] 잔과 재료를 최소한 하나씩 골라야 기믹으로 넘어갈 수 있습니다.");
            return;
        }

        RunAsync(Current).Forget();
    }

    async UniTaskVoid RunAsync(CraftSession session)
    {
        cocktailData.TryGet(session.SelectedCocktailId, out NewCocktailData selected);

        GimmickQueue queue = GimmickQueueBuilder.Build(selected, session.Actual, shelfData);
        LogQueue(session, queue);

        if (queue.Count == 0)
        {
            Debug.LogError("[CraftFlow] 만들어진 기믹이 없습니다. 재료의 default_action 데이터를 확인하세요.");
            return;
        }

        var display = new CraftRunDisplay(selected.TimeLimitSec, ShouldHideTime(selected, session));

        await runner.RunAsync(session, queue, display, this.GetCancellationTokenOnDestroy());

        CraftJudgement judgement = Judge(session, selected);

        CraftCompleted?.Invoke(session, judgement);
    }

    /// <summary>
    /// 만든 결과를 정답과 견주어 등급을 낸다. 결과는 로그로도 남긴다 —
    /// 결과 화면이 아직 없어서, 지금은 이게 점수를 확인할 수 있는 유일한 통로다.
    /// </summary>
    CraftJudgement Judge(CraftSession session, NewCocktailData selected)
    {
        if (balanceData == null || balanceData.balanceData == null)
        {
            Debug.LogWarning("[CraftFlow] balanceData가 없어 등급을 계산하지 못했습니다.");
            return null;
        }

        CraftJudgement judgement =
            CraftJudge.Evaluate(session, selected, shelfData, balanceData.balanceData);

        Debug.Log(judgement.BuildReport(session.SelectedCocktailId) + BuildActualReport(session));

        return judgement;
    }

    /// <summary>
    /// 무엇을 얼마나 넣었는지 그대로 늘어놓는다. 점수만 보면 왜 그렇게 나왔는지 알 수 없어서,
    /// 판정 근거가 된 실제 기록을 같이 남긴다.
    /// </summary>
    static string BuildActualReport(CraftSession session)
    {
        ActualCraft actual = session.Actual;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("  ── 실제 제조 ──");
        sb.AppendLine($"    잔 {actual.GlassId ?? "없음"} / 도구 {actual.ToolId ?? "없음"}");
        sb.AppendLine($"    고른 재료: {string.Join(", ", actual.IngredientIds)}");

        if (actual.AutoSqueezeIds.Count > 0 || actual.AutoPowderIds.Count > 0)
        {
            var auto = new List<string>();
            auto.AddRange(actual.AutoSqueezeIds);
            auto.AddRange(actual.AutoPowderIds);
            sb.AppendLine($"    자동 투입: {string.Join(", ", auto)}");
        }

        foreach (var gimmick in actual.GimmickResults)
        {
            string detail = gimmick.Type switch
            {
                ECraftGimmick.Open =>
                    $"{gimmick.AttemptCount}번째 시도에 성공 (실패 {gimmick.FailureCount})",
                ECraftGimmick.Shake or ECraftGimmick.Stir =>
                    $"성공 {gimmick.SuccessCount} / 실패 {gimmick.FailureCount} / 목표 {gimmick.TargetStackCount}",
                _ => gimmick.HasTarget
                    ? $"{gimmick.ActualValue:0.00} / {gimmick.TargetValue.Value:0.00}{gimmick.TargetUnit}"
                    : $"{gimmick.ActualValue:0.00} (목표 없음)",
            };

            sb.AppendLine($"    {gimmick.Type,-8} {gimmick.IngredientId ?? "-",-16} {detail}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 제조시간을 ???로 가릴지 정한다.
    ///
    /// 플레이어가 직접 고른 재료 중 정답이 하나도 없으면 이 잔에 대해 알려 줄 정답 정보가 없다.
    /// 그때는 목표 수량뿐 아니라 제한시간도 가린다. 자동으로 들어가는 재료는 언제나 레시피에서 오므로
    /// 이 판단에 넣지 않는다 — 그것까지 세면 정답을 하나도 못 골라도 시간이 보인다.
    /// </summary>
    static bool ShouldHideTime(NewCocktailData selected, CraftSession session)
    {
        foreach (string ingredientId in session.Actual.IngredientIds)
        {
            foreach (var step in selected.Recipe ?? System.Array.Empty<NewCocktailRecipeStep>())
            {
                if (step.IsSelectable && step.Ingredient == ingredientId) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 어떤 큐가 나왔는지 한 줄로 남긴다. 고른 것과 실제로 실행되는 것이 다를 때 —
    /// 오선택이나 재료 누락 — 원인을 바로 알아보기 위한 기록이다.
    /// </summary>
    void LogQueue(CraftSession session, GimmickQueue queue)
    {
        Debug.Log($"[CraftFlow] {session.SelectedCocktailId} 기믹 큐 ({queue.Count}): " +
                  string.Join(" → ", queue.Steps));

        if (queue.UnresolvedIngredientIds.Count > 0)
        {
            Debug.LogError("[CraftFlow] 기본 동작(default_action)을 몰라 큐에서 빠진 재료: " +
                           string.Join(", ", queue.UnresolvedIngredientIds));
        }
    }

    // ── 테스트용 자동 준비 ──────────────────────────────────────────────

    /// <summary>
    /// 제조 준비 화면이 생기기 전까지, 선택한 칵테일의 정답 구성을 그대로 골라 준다.
    /// 큐 생성과 기믹 실행을 실제로 돌려 보기 위한 임시 수단이라 준비 화면이 붙으면 지운다.
    /// </summary>
    void AutoPrepare()
    {
        if (!cocktailData.TryGet(Current.SelectedCocktailId, out NewCocktailData selected)) return;

        CraftPreset.ApplyTargetSetup(selected, Current.Actual);

        // 레시피에 직접 선택형 재료가 하나도 없으면 진입 조건을 못 채운다.
        // 필업 행이 아직 데이터에 없는 칵테일에서 실제로 일어날 수 있어 미리 알린다.
        if (!Current.Actual.CanStartGimmicks)
        {
            Debug.LogError($"[CraftFlow] '{Current.SelectedCocktailId}'의 정답 구성으로는 기믹에 들어갈 수 없습니다. " +
                           "레시피에 직접 선택형 재료가 있는지 확인하세요.");
        }
    }
}
