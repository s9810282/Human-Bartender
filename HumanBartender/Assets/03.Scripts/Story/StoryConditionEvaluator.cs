using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대본의 when 조건식을 판정한다(2부 운영 명세 §9.2).
///
/// 공용 ConditionUtil을 쓰지 않고 따로 두는 이유가 둘 있다.
///
/// 하나는 등급이다. 대본은 <c>grade &gt;= excellent</c>처럼 등급 이름을 값으로 쓰는데, 식별자를 전부
/// 변수로 보는 평가기에서는 excellent가 "없는 변수"가 되어 조건이 통째로 무너진다. 등급을 값으로
/// 아는 것은 2부의 사정이라 공용 유틸에 넣을 것이 아니다.
///
/// 다른 하나는 계산 방식이다. 공용 유틸은 문자열을 DataTable에 넘겨 계산하는데, 값을 문자열로
/// 바꿔 넣는 과정에서 참·거짓과 숫자의 구분이 사라지고 AOT 빌드에서 식 계산이 막힐 수 있다.
/// 문법이 작아서(비교 · ! · &amp;&amp;) 직접 읽는 편이 짧고 확실하다.
///
/// 값을 모르는 키를 만나면 거짓으로 넘기지 않고 오류로 남긴다. 조건이 조용히 거짓이 되면
/// 그 분기가 통째로 사라지는데, 화면에는 "대사가 없는 것"과 똑같이 보인다.
/// </summary>
public class StoryConditionEvaluator
{
    readonly IPlayerDataReader playerData;

    /// <summary>서빙 결과. 잔이 나가기 전에는 null이고, 그때 결과 키를 물으면 오류다.</summary>
    public StoryResultContext Result { get; set; }

    public StoryConditionEvaluator(IPlayerDataReader playerData)
    {
        this.playerData = playerData;
    }

    /// <summary>
    /// 조건식을 판정한다. 빈 값은 참이다 — 조건을 적지 않은 씬과 스텝은 언제나 실행된다.
    /// 식을 읽지 못하면 거짓으로 두고 오류를 남긴다.
    /// </summary>
    public bool Check(string when)
    {
        if (string.IsNullOrWhiteSpace(when)) return true;

        try
        {
            var parser = new Parser(when, this);
            bool result = parser.ParseExpression().AsBool();

            parser.ExpectEnd();
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[StoryCondition] 조건식을 판정하지 못했습니다: '{when}' — {e.Message}");
            return false;
        }
    }

    // ── 값 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 식 안의 값 하나. 참·거짓과 수를 구분해 둔다 — 둘을 섞으면 flag가 0/1로 비교되면서
    /// 뜻이 없는 식도 통과한다.
    /// </summary>
    readonly struct Value
    {
        public readonly bool IsBool;
        readonly bool boolValue;
        readonly double number;

        Value(bool isBool, bool boolValue, double number)
        {
            IsBool = isBool;
            this.boolValue = boolValue;
            this.number = number;
        }

        public static Value Of(bool value) => new(true, value, 0d);
        public static Value Of(double value) => new(false, false, value);

        public bool AsBool() =>
            IsBool ? boolValue : throw new InvalidOperationException("참·거짓이 와야 할 자리에 수가 있습니다.");

        public double AsNumber() =>
            IsBool ? throw new InvalidOperationException("수가 와야 할 자리에 참·거짓이 있습니다.") : number;

        public bool EqualsValue(Value other)
        {
            if (IsBool != other.IsBool) throw new InvalidOperationException("참·거짓과 수는 견줄 수 없습니다.");

            return IsBool ? boolValue == other.boolValue : Math.Abs(number - other.number) < 0.0001d;
        }
    }

    /// <summary>
    /// 식별자 하나의 값을 찾는다. 등급 이름은 부르기 전에 걸러지므로 여기 오지 않는다.
    /// 모르는 키는 예외다 — 임의의 기본값을 만들지 않는다(§9.2).
    /// </summary>
    Value Resolve(string key)
    {
        switch (key)
        {
            case "true": return Value.Of(true);
            case "false": return Value.Of(false);

            // 서빙 결과. grade는 현행 대본 호환 키로 final_grade와 같은 값을 준다(§10.3).
            case "grade":
            case "final_grade": return Value.Of(StoryGrade.ToRank(RequireResult(key).FinalGrade));
            case "craft_grade": return Value.Of(StoryGrade.ToRank(RequireResult(key).CraftGrade));
            case "order_match": return Value.Of(RequireResult(key).OrderMatch);

            case "day": return Value.Of(GameStateManager.Instance.CurrentDay);
            case "money": return Value.Of(playerData.HasMoney());
        }

        if (key.StartsWith("flag.", StringComparison.Ordinal))
            return Value.Of(playerData.CheckFlag(key));

        if (key.StartsWith("affinity.", StringComparison.Ordinal))
            return Value.Of(playerData.GetCurCharacterAffinityValue(key.Substring("affinity.".Length)));

        throw new InvalidOperationException($"'{key}'는 조건에 쓸 수 있는 값이 아닙니다.");
    }

    /// <summary>
    /// 서빙 결과를 꺼낸다. 아직 잔이 나가지 않았으면 오류다(§10.3 SERVE_RESULT_CONTEXT_MISSING).
    /// 여기서 기본 등급을 지어내면 서빙 전에 반응 대사가 새어 나온다.
    /// </summary>
    StoryResultContext RequireResult(string key)
    {
        return Result ?? throw new InvalidOperationException(
            $"서빙 결과가 없는데 '{key}'를 물었습니다. serve보다 앞선 조건인지 확인하세요.");
    }

    // ── 읽기 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 식을 왼쪽부터 읽어 내려간다. 문법은 명세가 정한 만큼만 받는다 —
    /// 비교 · 부정(!) · 결합(&amp;&amp;)이고 OR은 없다(§9.2).
    /// </summary>
    class Parser
    {
        readonly string text;
        readonly StoryConditionEvaluator owner;
        int pos;

        public Parser(string text, StoryConditionEvaluator owner)
        {
            this.text = text;
            this.owner = owner;
        }

        public Value ParseExpression() => ParseAnd();

        Value ParseAnd()
        {
            Value left = ParseComparison();

            while (TryTake("&&"))
            {
                // 오른쪽도 반드시 읽는다. 왼쪽이 거짓이라고 건너뛰면 뒤에 있는 문법 오류를 놓친다.
                Value right = ParseComparison();
                left = Value.Of(left.AsBool() && right.AsBool());
            }

            return left;
        }

        Value ParseComparison()
        {
            Value left = ParseUnary();

            string op = TakeComparisonOperator();
            if (op == null) return left;

            Value right = ParseUnary();

            return op switch
            {
                "==" => Value.Of(left.EqualsValue(right)),
                "!=" => Value.Of(!left.EqualsValue(right)),
                ">=" => Value.Of(left.AsNumber() >= right.AsNumber()),
                "<=" => Value.Of(left.AsNumber() <= right.AsNumber()),
                ">" => Value.Of(left.AsNumber() > right.AsNumber()),
                _ => Value.Of(left.AsNumber() < right.AsNumber()),
            };
        }

        Value ParseUnary()
        {
            if (TryTake("!")) return Value.Of(!ParseUnary().AsBool());

            return ParsePrimary();
        }

        Value ParsePrimary()
        {
            SkipSpaces();

            if (TryTake("("))
            {
                Value inner = ParseExpression();

                if (!TryTake(")")) throw new InvalidOperationException("닫는 괄호가 없습니다.");

                return inner;
            }

            if (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '-')) return TakeNumber();

            string token = TakeIdentifier();

            // 등급 이름은 변수가 아니라 값이다. 변수로 찾으면 없는 키가 되어 식이 무너진다.
            if (StoryGrade.TryParseRank(token, out int rank)) return Value.Of(rank);

            return owner.Resolve(token);
        }

        Value TakeNumber()
        {
            int start = pos;
            if (text[pos] == '-') pos++;

            while (pos < text.Length && (char.IsDigit(text[pos]) || text[pos] == '.')) pos++;

            string raw = text.Substring(start, pos - start);

            return double.TryParse(raw, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out double value)
                ? Value.Of(value)
                : throw new InvalidOperationException($"'{raw}'를 수로 읽지 못했습니다.");
        }

        string TakeIdentifier()
        {
            SkipSpaces();

            int start = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_' || text[pos] == '.')) pos++;

            if (pos == start) throw new InvalidOperationException($"{start}번째 글자에서 읽을 것이 없습니다.");

            return text.Substring(start, pos - start);
        }

        string TakeComparisonOperator()
        {
            SkipSpaces();

            foreach (string op in Operators)
            {
                if (!Matches(op)) continue;

                pos += op.Length;
                return op;
            }

            return null;
        }

        // 두 글자짜리를 먼저 본다. ">"를 먼저 보면 ">="가 ">"와 "="로 잘린다.
        static readonly string[] Operators = { "==", "!=", ">=", "<=", ">", "<" };

        bool TryTake(string token)
        {
            SkipSpaces();

            if (!Matches(token)) return false;

            pos += token.Length;
            return true;
        }

        bool Matches(string token)
        {
            return pos + token.Length <= text.Length && string.CompareOrdinal(text, pos, token, 0, token.Length) == 0;
        }

        void SkipSpaces()
        {
            while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
        }

        /// <summary>다 읽었는지 확인한다. 남은 글자가 있으면 식을 잘못 읽은 것이다.</summary>
        public void ExpectEnd()
        {
            SkipSpaces();

            if (pos < text.Length)
                throw new InvalidOperationException($"'{text.Substring(pos)}'를 읽지 못하고 남겼습니다.");
        }
    }
}
