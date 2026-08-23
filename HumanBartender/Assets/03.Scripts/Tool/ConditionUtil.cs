using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using UnityEngine;
using VContainer;

public interface IConditionUtil {
    bool Check(string when);
}

public class ConditionUtil : IConditionUtil
{
    private readonly GameStateManager _gameStateManager;
    private readonly IPlayerDataReader _playerDataReader;
    private readonly Dictionary<string, Func<object>> _databox;

    private readonly DataTable _dataTable = new DataTable();

    [Inject]
    public ConditionUtil(GameStateManager gameStateManager, IPlayerDataReader playerDataReader)
    {
        _gameStateManager = gameStateManager;
        _playerDataReader = playerDataReader;

        _databox = new Dictionary<string, Func<object>>
        {
            { "day", () => _gameStateManager.CurrentDay }
            // 기타 일반 변수 등록
        };
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
        // 1. 'flag.'으로 시작하는 경우 CheckFlag 호출
        if (key.StartsWith("flag.", StringComparison.OrdinalIgnoreCase))
        {
            // "flag." 뒷부분만 잘라서 전달하거나, 전체 key를 전달 (프로젝트 규격에 맞게 조정)
            string flagKey = key;
            return _playerDataReader.CheckFlag(flagKey);
        }

        // 2. 일반 변수인 경우 Databox 딕셔너리에서 검색
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