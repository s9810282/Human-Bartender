using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using UnityEngine;
using VContainer;
public interface IConditionUtil
{
    bool Check(string when);
    void Set(string statement);
}

//databox를 채워 넣어야 정상 작동
public class ConditionUtil : IConditionUtil
{
    private readonly GameStateManager _gameStateManager;
    private readonly IPlayerDataReader _playerDataReader;
    private readonly IPlayerDataWriter _playerDataWriter;
    private readonly Dictionary<string, Func<object>> _databox;

    private readonly DataTable _dataTable = new DataTable();
    private readonly string _varPattern = @"\b[a-zA-Z_][a-zA-Z0-9_\.]*\b";

    [Inject]
    public ConditionUtil(
        GameStateManager gameStateManager,
        IPlayerDataReader playerDataReader,
        IPlayerDataWriter playerDataWriter)
    {
        _gameStateManager = gameStateManager;
        _playerDataReader = playerDataReader;
        _playerDataWriter = playerDataWriter;
        //======================================================================================================
        //이곳에 변수이름과 변수의 접근하는 람다식을 입력
        _databox = new Dictionary<string, Func<object>>
        {
            { "day", () => _gameStateManager.CurrentDay }
        };
    }

    /// <summary>
    /// "flag.shiba_met = true", "gold + 20", "day + 1" 등
    /// 대입식(=)과 증감/단순 연산식을 해석하여 적용합니다.
    /// </summary>
    public void Set(string statement)
    {
        if (string.IsNullOrWhiteSpace(statement)) return;

        try
        {
            string[] statements = statement.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var singleStatement in statements)
            {
                string trimmed = singleStatement.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                string targetKey = string.Empty;
                string expression = string.Empty;

                if (trimmed.Contains("="))
                {
                    int assignIdx = trimmed.IndexOf('=');
                    targetKey = trimmed.Substring(0, assignIdx).Trim();
                    expression = trimmed.Substring(assignIdx + 1).Trim();
                }
                else
                {
                    var match = Regex.Match(trimmed, @"^([a-zA-Z_][a-zA-Z0-9_\.]*)\s*([\+\-\*/].*)");
                    if (match.Success)
                    {
                        targetKey = match.Groups[1].Value.Trim();
                        expression = trimmed;
                    }
                    else
                    {
                        targetKey = trimmed;
                        expression = "true";
                    }
                }

                string parsedExpression = EvaluateVariablesInExpression(expression);
                object evalResult = _dataTable.Compute(parsedExpression, string.Empty);

                ApplyValue(targetKey, evalResult);

                Debug.Log($"[ConditionUtil] Set 성공 | '{targetKey}' <= \"{expression}\" (평가: {parsedExpression}) => {evalResult}");
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
            string parsedExpression = EvaluateVariablesInExpression(when);

            parsedExpression = parsedExpression
                .Replace("!=", " <> ")
                .Replace("==", " = ")
                .Replace("&&", " AND ")
                .Replace("||", " OR ");

            parsedExpression = Regex.Replace(parsedExpression, @"!(?!=)", " NOT ");

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

    /// <summary>
    /// 수식 문자열 내부의 변수명을 실제 값으로 치환해줍니다.
    /// </summary>
    private string EvaluateVariablesInExpression(string expression)
    {
        return Regex.Replace(expression, _varPattern, match =>
        {
            string varName = match.Value;

            if (varName.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                varName.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return varName;
            }

            object rawValue = GetValue(varName);

            if (rawValue == null)
            {
                Debug.LogWarning($"[ConditionUtil] '{varName}' 변수의 값을 가져올 수 없습니다. 0/false로 기본 처리합니다.");
                return "0";
            }

            return FormatValue(rawValue);
        });
    }

    /// <summary>
    /// 계산된 결과를 대상 Key(플래그 또는 데이터)에 반영합니다.
    /// </summary>
    private void ApplyValue(string targetKey, object value)
    {
        if (value is bool boolVal)
        {
            _playerDataWriter.AddFlag(targetKey, boolVal);
            return;
        }

        string strVal = value.ToString().ToLower();
        if (strVal == "true" || strVal == "false")
        {
            _playerDataWriter.AddFlag(targetKey, strVal == "true");
            return;
        }

        if (bool.TryParse(strVal, out bool parsedBool))
        {
            _playerDataWriter.AddFlag(targetKey, parsedBool);
        }
        else
        {
            // 숫자/기타 데이터 세팅용 (필요 시 playerDataWriter 연동 확장)
            Debug.LogWarning($"[ConditionUtil] '{targetKey}'에 non-bool 값 ({value}) 반영. 필요 시 IPlayerDataWriter 확장 권장.");
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