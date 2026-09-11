using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// json/text_tags.json의 항목 하나. 태그 이름 → 이 값의 딕셔너리로 읽는다.
///
/// value의 타입이 kind에 따라 달라진다(color는 "#RRGGBB", speed_ms·size_pct는 숫자)라서 object로 받는다.
/// 문자열로 못 박으면 숫자 항목에서 터지고, 그 예외는 뒤따르는 파일 로드를 전부 막는다.
/// </summary>
public struct NewTextTagData
{
    [JsonProperty("kind")] public string Kind { get; set; }
    [JsonProperty("value")] public object Value { get; set; }
}

/// <summary>
/// 신형 데이터(json/*.json)를 구형 SO가 읽는 모양으로 옮겨 담는 곳.
///
/// 구형 SO를 물고 있는 화면들(UIDialogueTextView, DialogueCharacterManager,
/// GuestCharacterView 등)은 인스펙터 연결과 호출부가 여럿이라, 데이터 출처를 옮기는 일이
/// 뷰를 손보는 일이 되면 안 된다. 그래서 뷰는 그대로 두고 "누가 채우느냐"만 바꾼다.
///
/// 옮겨 담기는 신형 파일이 구형 파일을 확실히 대체하는 것에만 쓴다. 신형에 대응이 없는 데이터
/// (컷씬 프리셋, 정산, 등급표 등)는 구형 파일을 그대로 읽는다 — 없는 대응을 지어내면
/// 그럴듯한 가짜 값이 화면까지 흘러간다.
/// </summary>
public static class NewLegacyDataBridge
{
    // ── 표정 애니메이션 ─────────────────────────────────────────────
    //
    // 두 모양은 같은 것을 다르게 적은 것이다. 신형은 캐릭터 → 표정 → {mode, sprite, parts{부위}},
    // 구형은 캐릭터 → {base_body, expressions → 부위별 필드}. parts 한 겹과 base_body를 빼면 다르지 않다.
    //
    // base_body는 신형에 없다. 구형에서도 로그 한 줄에만 쓰였으므로 비워 둔다.
    // talk_anim도 구형에 자리가 없어 버린다 — 필요해지면 그때 구형 모델에 칸을 낸다.

    /// <summary>신형 표정 딕셔너리를 구형 CharacterAnimBase로 변환한다. 원본이 없으면 null.</summary>
    public static CharacterAnimBase ToCharacterAnim(Dictionary<string, Dictionary<string, NewExpressionEntry>> source)
    {
        if (source == null) return null;

        var characters = new Dictionary<string, CharacterAnimData>(source.Count);

        foreach (var character in source)
        {
            var expressions = new Dictionary<string, ExpressionAnimData>();

            if (character.Value != null)
            {
                foreach (var expression in character.Value)
                    expressions[expression.Key] = ToExpression(expression.Value);
            }

            characters[character.Key] = new CharacterAnimData
            {
                BaseBody = null,
                Expressions = expressions,
            };
        }

        return new CharacterAnimBase { Characters = characters };
    }

    static ExpressionAnimData ToExpression(NewExpressionEntry entry)
    {
        // 구형은 mode를 문자열로 보고 "sprite"인지만 따진다(CharacterAnimSO.CheckExpressionPortailSprite).
        var data = new ExpressionAnimData
        {
            Type = entry.Mode == ENewExpressionMode.Sprite ? "sprite" : "parts_anim",
            SpritePath = entry.Sprite,
        };

        if (entry.Parts == null) return data;

        foreach (var part in entry.Parts)
        {
            var clip = new PartAnimData
            {
                Clip = part.Value.Clip,
                Loop = ToLoopMode(part.Value.Loop),
            };

            switch (part.Key)
            {
                case "eyes": data.Eyes = clip; break;
                case "eyebrows": data.Eyebrows = clip; break;
                case "upper_face": data.Upper_face = clip; break;
                case "lower_face": data.Lower_face = clip; break;
                case "body": data.Body = clip; break;
                case "extra": data.Extra = clip; break;
                case "etc": data.Etc = clip; break;

                // 구형 모델에 자리가 없는 부위다. 조용히 버리면 그림 한 조각이 이유 없이 빠진다.
                default:
                    Debug.LogWarning($"[Bridge] 모르는 파츠 '{part.Key}' — 구형 모델에 담을 자리가 없어 건너뜁니다.");
                    break;
            }
        }

        return data;
    }

    static EAnimLoopMode ToLoopMode(ENewAnimLoopMode loop) => loop switch
    {
        ENewAnimLoopMode.Always => EAnimLoopMode.Always,
        ENewAnimLoopMode.AlwaysOnDialogue => EAnimLoopMode.Always_OnDialogue,
        ENewAnimLoopMode.SpecialOnDialogue => EAnimLoopMode.Special_OnDialogue,
        ENewAnimLoopMode.OnDialogue => EAnimLoopMode.Special_OnDialogue,
        _ => EAnimLoopMode.None,
    };

    // ── 인물 정보 ───────────────────────────────────────────────────

    // ── 텍스트 색상 태그 ─────────────────────────────────────────────

    /// <summary>
    /// 신형 text_tags.json을 구형 TextTagDataBase로 변환한다.
    ///
    /// 구형은 태그 하나가 곧 색 하나였다. 신형에는 색 말고 speed_ms(타이핑 속도)와 size_pct(글자 크기)도
    /// 있는데 구형 모델에 담을 자리도, 그것을 읽는 코드도 없다. 그래서 색만 옮기고 나머지는 남긴다 —
    /// 담을 데가 없는 값을 색인 척 넣으면 대사에 엉뚱한 색이 칠해진다.
    ///
    /// 옮겨지지 않은 종류는 로그로 남긴다. 대본이 &lt;slow&gt;를 쓰기 시작하면 조용히 무시되는 대신
    /// 이 줄이 보여야 한다.
    /// </summary>
    public static TextTagDataBase ToTextTags(Dictionary<string, NewTextTagData> source)
    {
        if (source == null) return null;

        var tags = new Dictionary<string, TextTagData>();
        var skipped = new List<string>();

        foreach (var tag in source)
        {
            if (tag.Value.Kind != "color")
            {
                skipped.Add($"{tag.Key}({tag.Value.Kind})");
                continue;
            }

            tags[tag.Key] = new TextTagData
            {
                Color = tag.Value.Value?.ToString(),
                Description = null,
            };
        }

        if (skipped.Count > 0)
            Debug.Log($"[Bridge] 구형 모델에 자리가 없어 색 태그만 옮겼습니다. 남은 것: {string.Join(", ", skipped)}");

        return new TextTagDataBase { TextTags = tags };
    }
}
