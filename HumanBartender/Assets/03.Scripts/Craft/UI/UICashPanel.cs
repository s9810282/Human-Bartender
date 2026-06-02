using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class UICashPanel : MonoBehaviour
{
    [Header("Main Display")]
    [SerializeField] private TMP_Text amountText;        // 메인 "191V" 텍스트
    [SerializeField] private string suffix = "V";        // 단위 표기
    [SerializeField] private string numberFormat = "N0"; // "N0" => 1,234 / "0" => 1234
    [SerializeField] private int currentAmount = 0;      // 현재 보유량

    [Header("Gain Popups")]
    [SerializeField] private Canvas canvas;              // 소속 Canvas (비우면 부모에서 자동 검색)
    [SerializeField] private CurrencyGainText gainPrefab; // 팝업 프리팹
    [SerializeField] private RectTransform gainLayer;     // 팝업이 생성될 컨테이너 (★ LayoutGroup 없는 빈 RectTransform)
    [SerializeField] private RectTransform mergeTarget;   // 병합 도착점 (비우면 amountText 사용)
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -55f); // 도착점 기준 시작 위치(아래쪽)
    [SerializeField] private float stackSpacing = 38f;    // 연속 획득 시 세로 간격

    [Header("Punch On Merge")]
    [SerializeField] private float punchScale = 1.12f;    // 병합 시 메인 텍스트 살짝 커지는 효과
    [SerializeField] private float punchTime = 0.12f;

    private int activeGainCount;
    private Coroutine punchRoutine;

    private void Awake()
    {
        if (mergeTarget == null && amountText != null)
            mergeTarget = amountText.rectTransform;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
    }

    private void Start() => Refresh();

    /// <summary>재화 획득(+) 또는 차감(-). 팝업이 메인 텍스트에 도달하면 값이 반영된다.</summary>
    public void AddCurrency(int amount)
    {
        if (amount == 0 || gainPrefab == null || gainLayer == null) return;

        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        // 메인 텍스트의 월드 위치를 gainLayer 로컬 좌표로 변환 → 좌표계 일치
        Vector2 target = WorldToLocal(gainLayer, mergeTarget.position, cam);
        Vector2 start = target + spawnOffset + new Vector2(0f, -stackSpacing * activeGainCount);
        activeGainCount++;

        CurrencyGainText popup = Instantiate(gainPrefab, gainLayer);
        popup.Play(amount, suffix, start, target, () =>
        {
            currentAmount += amount;   // 겹치는 순간 값 반영
            Refresh();
            Punch();
            activeGainCount = Mathf.Max(0, activeGainCount - 1);
        });
    }

    /// <summary>팝업 없이 즉시 값 설정 (세이브 로드 등).</summary>
    public void SetAmount(int amount)
    {
        currentAmount = amount;
        Refresh();
    }

    public int CurrentAmount => currentAmount;

    private void Refresh()
    {
        if (amountText != null)
            amountText.text = currentAmount.ToString(numberFormat) + suffix;
    }

    private void Punch()
    {
        if (amountText == null) return;
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchCo(amountText.rectTransform));
    }

    private IEnumerator PunchCo(RectTransform rt)
    {
        Vector3 baseScale = Vector3.one;
        float half = Mathf.Max(0.001f, punchTime * 0.5f);
        float t = 0f;
        while (t < half) { t += Time.deltaTime; rt.localScale = Vector3.Lerp(baseScale, baseScale * punchScale, t / half); yield return null; }
        t = 0f;
        while (t < half) { t += Time.deltaTime; rt.localScale = Vector3.Lerp(baseScale * punchScale, baseScale, t / half); yield return null; }
        rt.localScale = baseScale;
        punchRoutine = null;
    }

    private static Vector2 WorldToLocal(RectTransform area, Vector3 world, Camera cam)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, cam, out Vector2 local);
        return local;
    }
}
