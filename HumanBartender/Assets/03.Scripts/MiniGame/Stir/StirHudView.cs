using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아레나 바깥의 표시 전부 — 상단바 스탯, 라운드 타이머 카드, 사선 게이지, 시작 오버레이.
/// 판정은 하지 않고 StirManager가 넘겨준 값을 그리기만 한다.
///
/// 프로토타입(스터 미니게임.html)의 화면 구성을 그대로 따른다. 참조가 비어 있어도 게임은 돌아가므로
/// 필요 없는 요소는 인스펙터에서 비워두면 된다.
/// </summary>
public class StirHudView : MonoBehaviour
{
    [Header("Top Stats")]
    [SerializeField] TMP_Text elapsedText;
    [SerializeField] TMP_Text circleText;
    [SerializeField] TMP_Text comboText;

    [Header("Round Timer Card")]
    [SerializeField] TMP_Text roundLimitText;
    [SerializeField] TMP_Text roundTimeText;
    [Tooltip("남은 시간을 그리는 가로 막대. Filled/Horizontal이어야 한다.")]
    [SerializeField] Image roundTimeFill;
    [SerializeField] Color roundNormalColor = new Color(0.87f, 1f, 0.42f);
    [SerializeField] Color roundDangerColor = new Color(1f, 0.39f, 0.47f);
    [Tooltip("경고색으로 바뀌는 잔여 비율. 프로토타입과 같은 34%.")]
    [SerializeField] float dangerRatio = 0.34f;

    [Header("Gauge")]
    [SerializeField] StirGaugeView gauge;

    [Header("Overlay")]
    [Tooltip("시작 대기 카드. 첫 W 입력에 꺼진다.")]
    [SerializeField] GameObject startOverlay;
    [SerializeField] TMP_Text judgeText;

    /// <summary>제한시간을 카드 머리글에 한 번 박아둔다.</summary>
    public void SetRoundLimit(float seconds)
    {
        if (roundLimitText != null) roundLimitText.text = $"{seconds:0.00} SEC";
    }

    /// <summary>프로토타입과 같은 mm:ss.cc 표기.</summary>
    public void SetElapsed(float seconds)
    {
        if (elapsedText == null) return;

        int minutes = (int)(seconds / 60f);
        int secs = (int)(seconds % 60f);
        int hundredths = (int)((seconds - (int)seconds) * 100f);

        elapsedText.text = $"{minutes:00}:{secs:00}.{hundredths:00}";
    }

    public void SetCircle(int done, int total)
    {
        if (circleText != null) circleText.text = $"{done} / {total}";
    }

    public void SetCombo(int combo, int best)
    {
        if (comboText != null) comboText.text = $"{combo} / {best}";
    }

    public void SetRoundTime(float remainSeconds, float ratio)
    {
        if (roundTimeText != null) roundTimeText.text = $"{Mathf.Max(0f, remainSeconds):0.00}";

        if (roundTimeFill != null)
        {
            roundTimeFill.fillAmount = Mathf.Clamp01(ratio);
            roundTimeFill.color = ratio < dangerRatio ? roundDangerColor : roundNormalColor;
        }
    }

    public void SetGaugeResult(int index, bool success) => gauge?.SetResult(index, success);

    public void ResetGauge() => gauge?.ResetAll();

    public void SetStartOverlay(bool visible)
    {
        if (startOverlay != null) startOverlay.SetActive(visible);
    }

    public void SetJudge(string message)
    {
        if (judgeText != null) judgeText.text = message;
    }
}
