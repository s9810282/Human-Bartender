using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using UnityEngine;
using VContainer;

public interface IConditionUtil {
    bool Check(string when);
    public void Set(string statement);
}
public class ConditionUtil : IConditionUtil
{
    private readonly GameStateManager _gameStateManager;
    private readonly IPlayerDataReader _playerDataReader;
    private readonly IPlayerDataWriter _playerDataWriter;
    private readonly Dictionary<string, Func<object>> _databox;

    private readonly DataTable _dataTable = new DataTable();

    [Inject]
    public ConditionUtil(
        GameStateManager gameStateManager,
        IPlayerDataReader playerDataReader,
        IPlayerDataWriter playerDataWriter)
    {
        _gameStateManager = gameStateManager;
        _playerDataReader = playerDataReader;
        _playerDataWriter = playerDataWriter;

        _databox = new Dictionary<string, Func<object>>
        {
            { "day", () => _gameStateManager.CurrentDay }
        };
    }

    /// <summary>
    /// "flag.shiba_met = true" 같은 문자열 구문을 해석해 플래그를 변경합니다.
    /// </summary>
    public void Set(string statement)
    {
        if (string.IsNullOrWhiteSpace(statement)) return;

        try
        {
            // 1. 세미콜론(;) 또는 쉼표(,)를 기준으로 문장을 여러 개로 분할
            string[] statements = statement.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var singleStatement in statements)
            {
                string trimmed = singleStatement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // 2. '=' 또는 ':' 기준으로 키와 값 분리
                string[] parts = trimmed.Split(new[] { '=', ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    Debug.LogWarning($"[ConditionUtil] 올바르지 않은 Set 구문입니다: '{trimmed}'");
                    continue;
                }

                string flagKey = parts[0].Trim();
                string rawValue = parts[1].Trim().ToLower();

                // 3. bool 값 변환 (true, 1 지원)
                bool boolValue = rawValue == "true" || rawValue == "1";

                _playerDataWriter.AddFlag(flagKey, boolValue);
                Debug.Log($"[ConditionUtil] 플래그 설정 성공 | '{flagKey}' => {boolValue}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConditionUtil] Set 구문 처리 실패: '{statement}' | 에러: {ex.Message}");
        }
    }

    public bool Check(string when)
    {
        if (string.IsNullOrWhiteSpace(when)) return true;

        try
        {
            // 1. 수식 내 변수명/플래그 파싱
            string pattern = @"\b[a-zA-Z_][a-zA-Z0-9_\.]*\b";

            string parsedExpression = Regex.Replace(when, pattern, match =>
            {
                string varName = match.Value;

                // 예약어 유지
                if (varName.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    varName.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return varName;
                }

                object rawValue = GetValue(varName);

                if (rawValue == null)
                {
                    Debug.LogWarning($"[ConditionEvaluator] '{varName}' 키의 값을 평가할 수 없습니다.");
                    return "false";
                }

                return FormatValue(rawValue);
            });

            // 2. 연산자 변환
            parsedExpression = parsedExpression
                .Replace("!=", " <> ")
                .Replace("==", " = ")
                .Replace("&&", " AND ")
                .Replace("||", " OR ");

            parsedExpression = Regex.Replace(parsedExpression, @"!(?!=)", " NOT ");

            // 3. 수식 계산
            object evalResult = _dataTable.Compute(parsedExpression, string.Empty);
            Debug.Log($"[ConditionEvaluator] 조건 평가 성공 | 원본: \"{when}\" -> 평가식: \"{parsedExpression}\" => 결과: {evalResult}");
            return Convert.ToBoolean(evalResult);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConditionEvaluator] 수식 계산 실패: '{when}' | 에러: {ex.Message}");
            return false;
        }
    }

    public object GetValue(string key)
    {
        if (key.StartsWith("flag.", StringComparison.OrdinalIgnoreCase))
        {
            return _playerDataReader.CheckFlag(key);
        }

        if (_databox.TryGetValue(key, out Func<object> getter))
        {
            return getter.Invoke();
        }

        return null;
    }

    private string FormatValue(object value)
    {
        if (value is bool boolVal)
            return boolVal ? "true" : "false";
        if (value is string strVal)
            return $"'{strVal}'";

        return value.ToString();
    }
}