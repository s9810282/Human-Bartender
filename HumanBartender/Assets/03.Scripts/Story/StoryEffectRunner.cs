using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대본의 effects를 적용한다(2부 운영 명세 §9.1).
///
/// 조건식과 달리 이쪽은 값을 바꾸는 일이라, 같은 것을 두 번 적용하면 호감도와 돈이 두 배가 된다.
/// 그래서 스텝·선택지마다 확정한 순간 한 번만 부르고, 이미 적용한 것은 토큰으로 걸러 낸다.
///
/// 문법은 데이터가 쓰는 만큼이다: <c>flag.x = true</c> · <c>affinity.chris += 5</c> ·
/// <c>money -= 25</c> · 세미콜론으로 이은 여러 개. 소지품을 주는 <c>give(...)</c>는 아직 담을 곳이 없어
/// 받지 않는다 — 2부 바 대본에는 없고 공용 대본에만 있다.
///
/// 모르는 대상은 조용히 넘기지 않고 오류로 남긴다. 관계값이 오르지 않은 것은 화면에 드러나지 않아서,
/// 로그가 없으면 한참 뒤에야 알게 된다.
/// </summary>
public class StoryEffectRunner
{
    readonly IPlayerDataWriter playerData;

    /// <summary>이미 적용한 것들. 같은 스텝을 두 번 확정해도 값이 두 번 움직이지 않게 한다.</summary>
    readonly HashSet<string> appliedTokens = new();

    public StoryEffectRunner(IPlayerDataWriter playerData)
    {
        this.playerData = playerData;
    }

    /// <summary>
    /// effects를 한 번만 적용한다. token은 그 스텝·선택지를 가리키는 고정 값이어야 한다.
    /// 이미 적용했거나 적용할 것이 없으면 false.
    /// </summary>
    public bool Apply(string effects, string token)
    {
        if (string.IsNullOrWhiteSpace(effects)) return false;

        if (!string.IsNullOrEmpty(token) && !appliedTokens.Add(token))
        {
            Debug.LogWarning($"[StoryEffect] 이미 적용한 effects라 건너뜁니다: {token}");
            return false;
        }

        foreach (string clause in effects.Split(';'))
        {
            string trimmed = clause.Trim();
            if (trimmed.Length == 0) continue;

            ApplyOne(trimmed, effects);
        }

        return true;
    }

    /// <summary>하루가 끝나 적용 기록을 비운다. 다음 날 같은 스텝이 다시 실행될 수 있다.</summary>
    public void ResetAppliedTokens()
    {
        appliedTokens.Clear();
    }

    void ApplyOne(string clause, string wholeExpression)
    {
        try
        {
            // 두 글자 기호를 먼저 본다. "="로 먼저 자르면 <c>money -= 25</c>가
            // <c>money -</c>와 <c>25</c>로 갈려서 엉뚱한 대상을 못 찾는다고 나온다.
            if (!TrySplit(clause, "+=", out string target, out string rawValue, out EOp op) &&
                !TrySplit(clause, "-=", out target, out rawValue, out op) &&
                !TrySplit(clause, "=", out target, out rawValue, out op))
            {
                throw new InvalidOperationException("= 또는 += 가 없습니다.");
            }

            ApplyTo(target, rawValue, op);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StoryEffect] '{clause}'를 적용하지 못했습니다 (전체: '{wholeExpression}') — {e.Message}");
        }
    }

    /// <summary>값을 어떻게 바꾸는지.</summary>
    enum EOp
    {
        Set,
        Add,
        Subtract,
    }

    /// <summary>대입 기호를 기준으로 좌우를 가른다.</summary>
    static bool TrySplit(string clause, string token, out string target, out string rawValue, out EOp op)
    {
        target = null;
        rawValue = null;
        op = token switch { "+=" => EOp.Add, "-=" => EOp.Subtract, _ => EOp.Set };

        int at = clause.IndexOf(token, StringComparison.Ordinal);
        if (at < 0) return false;

        target = clause.Substring(0, at).Trim();
        rawValue = clause.Substring(at + token.Length).Trim();

        return target.Length > 0 && rawValue.Length > 0;
    }

    void ApplyTo(string target, string rawValue, EOp op)
    {
        if (target.StartsWith("flag.", StringComparison.Ordinal))
        {
            if (op != EOp.Set) throw new InvalidOperationException("플래그는 더하거나 뺄 수 없습니다.");

            playerData.AddFlag(target, ParseBool(rawValue));
            Debug.Log($"[StoryEffect] {target} = {rawValue}");
            return;
        }

        if (target.StartsWith("affinity.", StringComparison.Ordinal))
        {
            string characterId = target.Substring("affinity.".Length);
            int amount = ParseInt(rawValue);

            switch (op)
            {
                case EOp.Add: playerData.AddCharacterAffinityAmount(characterId, amount); break;
                case EOp.Subtract: playerData.AddCharacterAffinityAmount(characterId, -amount); break;
                default: playerData.SetCharacterAffinityAmount(characterId, amount); break;
            }

            Debug.Log($"[StoryEffect] {target} {Symbol(op)} {amount}");
            return;
        }

        if (target == "money")
        {
            int amount = ParseInt(rawValue);

            switch (op)
            {
                case EOp.Add:
                    playerData.AddMoney(amount);
                    break;

                case EOp.Subtract:
                    // 모자라면 깎지 않는다. 살 수 있는지는 대본의 조건이 이미 봤어야 하는 것이라,
                    // 여기서 음수로 만들지 않고 그 사실을 남긴다.
                    if (!playerData.TrySpend(amount))
                        Debug.LogWarning($"[StoryEffect] 돈이 모자라 {amount}를 쓰지 못했습니다.");
                    break;

                // 대입은 지금까지 번 것을 지우는 뜻이 된다. 그렇게 쓴 데이터가 없어 실수로 본다.
                default:
                    throw new InvalidOperationException("돈은 += 또는 -= 로만 바꿀 수 있습니다.");
            }

            Debug.Log($"[StoryEffect] money {Symbol(op)} {amount}");
            return;
        }

        // 평판은 아직 담아 둘 곳이 없다. 1부의 BarReputation은 그 실행에서만 사는 값이라
        // 여기서 올려도 다음 날 남지 않는다. 조용히 넘기면 오르지 않은 것을 알아챌 수 없어 남긴다.
        if (target == "reputation")
            throw new InvalidOperationException("평판을 담아 둘 곳이 아직 없습니다. 저장 시스템이 붙을 때 잇습니다.");

        throw new InvalidOperationException($"'{target}'은 바꿀 수 있는 대상이 아닙니다.");
    }

    static string Symbol(EOp op) => op switch
    {
        EOp.Add => "+=",
        EOp.Subtract => "-=",
        _ => "=",
    };

    static bool ParseBool(string raw)
    {
        return raw switch
        {
            "true" => true,
            "false" => false,
            _ => throw new InvalidOperationException($"'{raw}'는 참·거짓이 아닙니다."),
        };
    }

    static int ParseInt(string raw)
    {
        return int.TryParse(raw, out int value)
            ? value
            : throw new InvalidOperationException($"'{raw}'를 수로 읽지 못했습니다.");
    }
}
