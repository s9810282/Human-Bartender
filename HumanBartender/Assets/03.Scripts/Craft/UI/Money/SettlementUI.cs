using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;





/// <summary>
/// 정산 패널 전체 컨트롤러.
/// SettlementData 를 넘기면 판매/팁 아코디언, 총수입, 유지비, 최종 수령액을 채우고 보여준다.
/// </summary>
public class SettlementUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;     // 패널 전체 (Show/Hide 대상)
    [SerializeField] private CanvasGroup panelGroup;   // 페이드인용 (선택)
    [SerializeField] private CocktailDataSO cocktailData;
    [SerializeField] private PlayerSettlement settlementData;

    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private string title = "오늘의 정산";


    [Header("Sections")]
    [SerializeField] private SettlementSection salesSection; // 판매 내역
    [SerializeField] private SettlementSection tipSection;   // 팁 내역

    [Header("Calc Rows")]
    [SerializeField] private TMP_Text grossText;       // 총 수입
    [SerializeField] private TMP_Text costText;        // 가게 유지비 (−)
    [SerializeField] private TMP_Text costBadgeText;   // "고정 $80" 같은 배지 (선택)
    [SerializeField] private TMP_Text finalText;       // 최종 수령액

    [Header("Confirm")]
    [SerializeField] private Button confirmButton;
    [SerializeField] VoidEvent OnConfirmed;

    [Header("Format")]
    [SerializeField] private string currencySymbol = "$";
    [SerializeField] private string numberFormat = "N0"; // 천단위 콤마. "0" 으로 바꾸면 콤마 없음
    [SerializeField] private Color positiveColor = new Color(0.16f, 0.87f, 0.54f); // 초록
    [SerializeField] private Color negativeColor = new Color(1f, 0.29f, 0.29f);    // 빨강

    private void Awake()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>데이터를 받아 패널을 채우고 표시한다.</summary>
    public void Show(PlayerSettlement data)
    {
        if (data == null) return;

        if (titleText != null) titleText.text = title;
        if (dateText != null) dateText.text = GetDateLabel();

        // 판매 라인 조립 (단가는 SO 에서 조회, 금액 = 단가 × 수량)
        var salesLines = new List<SettlementLine>();
        int salesTotal = 0;
        foreach (var kv in data.salesQty)
        {
            int qty = kv.Value;
            int unit = cocktailData != null ? cocktailData.allCocktails[kv.Key].Price: 0;
            int amount = unit * qty;
            salesTotal += amount;

            string display = cocktailData != null ? cocktailData.allCocktails[kv.Key].Name : kv.Key;

            Sprite icon = null; // cocktailData != null ? cocktailData.GetIcon(kv.Key) : null;
            salesLines.Add(new SettlementLine(icon, display, qty, Money(amount)));
        }

        // 팁 라인 조립 (금액은 Dictionary 값 그대로, 수량 미표시)
        var tipLines = new List<SettlementLine>();
        int tipTotal = 0;
        foreach (var kv in data.tips)
        {
            tipTotal += kv.Value;
            string display = cocktailData != null ? cocktailData.allCocktails[kv.Key].Name : kv.Key;

            Sprite icon = null; //cocktailData != null ? cocktailData.GetIcon(kv.Key) : null;
            tipLines.Add(new SettlementLine(icon, display, 0, Money(kv.Value)));
        }

        int gross = salesTotal + tipTotal;
        int final = gross - data.maintenanceCost;

        if (salesSection != null) salesSection.Build("판매 내역", salesLines, Money(salesTotal));
        if (tipSection != null) tipSection.Build("팁 내역", tipLines, Money(tipTotal));

        // 계산 영역
        if (grossText != null)
        {
            grossText.text = Money(gross);
            grossText.color = positiveColor;
        }
        if (costText != null)
        {
            costText.text = "−" + Money(data.maintenanceCost);
            costText.color = negativeColor;
        }
        if (costBadgeText != null)
            costBadgeText.text = "고정 " + currencySymbol + data.maintenanceCost.ToString(numberFormat);

        // 최종 수령액
        if (finalText != null)
        {
            finalText.text = (final >= 0 ? "+" : "−") + Money(final);
            finalText.color = final >= 0 ? positiveColor : negativeColor;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        if (panelGroup != null) StartCoroutine(FadeIn());
    }
    public void Show()
    {
        PlayerSettlement data = settlementData;

        if (data == null) return;
        
        if (titleText != null) titleText.text = title;
        if (dateText != null) dateText.text = GetDateLabel();

        if (data.salesQty == null)
        {
            ShowDefault(data);
            return;
        }

        // 판매 라인 조립 (단가는 SO 에서 조회, 금액 = 단가 × 수량)
        var salesLines = new List<SettlementLine>();
        int salesTotal = 0;
        foreach (var kv in data.salesQty)
        {
            int qty = kv.Value;
            int unit = cocktailData != null ? cocktailData.allCocktails[kv.Key].Price : 0;
            int amount = unit * qty;
            salesTotal += amount;

            string display = cocktailData != null ? cocktailData.allCocktails[kv.Key].Name : kv.Key;

            Sprite icon = null; // cocktailData != null ? cocktailData.GetIcon(kv.Key) : null;
            salesLines.Add(new SettlementLine(icon, display, qty, Money(amount)));
        }

        // 팁 라인 조립 (금액은 Dictionary 값 그대로, 수량 미표시)
        var tipLines = new List<SettlementLine>();
        int tipTotal = 0;
        foreach (var kv in data.tips)
        {
            tipTotal += kv.Value;
            string display = cocktailData != null ? cocktailData.allCocktails[kv.Key].Name : kv.Key;

            Sprite icon = null; //cocktailData != null ? cocktailData.GetIcon(kv.Key) : null;
            tipLines.Add(new SettlementLine(icon, display, 0, Money(kv.Value)));
        }

        int gross = salesTotal + tipTotal;
        int final = gross + data.maintenanceCost;

        if (salesSection != null) salesSection.Build("판매 내역", salesLines, Money(salesTotal));
        if (tipSection != null) tipSection.Build("팁 내역", tipLines, Money(tipTotal));

        // 계산 영역
        if (grossText != null)
        {
            grossText.text = Money(gross);
            grossText.color = positiveColor;
        }
        if (costText != null)
        {
            costText.text = "−" + Money(data.maintenanceCost);
            costText.color = negativeColor;
        }
        if (costBadgeText != null)
            costBadgeText.text = "고정 " + currencySymbol + data.maintenanceCost.ToString(numberFormat);

        // 최종 수령액
        if (finalText != null)
        {
            finalText.text = (final >= 0 ? "+" : "−") + Money(final);
            finalText.color = final >= 0 ? positiveColor : negativeColor;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        if (panelGroup != null) StartCoroutine(FadeIn());
    }

    public void ShowDefault(PlayerSettlement data)
    {
        int final = data.maintenanceCost;

        if (salesSection != null) salesSection.Build("판매 내역", null, "0");
        if (tipSection != null) tipSection.Build("팁 내역", null, "0");

        if (grossText != null)
        {
            grossText.text = "0";
            grossText.color = positiveColor;
        }
        if (costText != null)
        {
            costText.text = "−" + Money(data.maintenanceCost);
            costText.color = negativeColor;
        }
        if (costBadgeText != null)
            costBadgeText.text = "고정 " + currencySymbol + data.maintenanceCost.ToString(numberFormat);

        // 최종 수령액
        if (finalText != null)
        {
            finalText.text = (final >= 0 ? "+" : "−") + Money(final);
            finalText.color = final >= 0 ? positiveColor : negativeColor;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        if (panelGroup != null) StartCoroutine(FadeIn());
    }


    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void HandleConfirm()
    {
        OnConfirmed?.Raise(new Void());
        Hide();
    }

    /// <summary>int 금액 -> "$1,234" 형식 (절댓값 기준).</summary>
    private string Money(int v) => currencySymbol + Mathf.Abs(v).ToString(numberFormat);

    private static string GetDateLabel()
    {
        var d = DateTime.Now;
        return $"{d.Year}.{d.Month}.{d.Day} 퇴근";
    }

    private IEnumerator FadeIn()
    {
        panelGroup.alpha = 0f;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            panelGroup.alpha = Mathf.Clamp01(t / 0.3f);
            yield return null;
        }
        panelGroup.alpha = 1f;
    }
}