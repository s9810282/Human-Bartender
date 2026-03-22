using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICraftButton : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private RectTransform panelRect; // 크기를 조절할 대상 패널
    [SerializeField] private List<Image> panelImages;
    [SerializeField] private float expandedSize = 130f; // 열렸을 때 크기
    [SerializeField] private float collapsedSize = 0f;  // 닫혔을 때 크기
    [SerializeField] private float duration = 0.3f;     // 애니메이션 진행 시간 (초)

    private bool isExpanded = false; // 현재 패널이 열려있는지 상태 저장
    private Coroutine animationCoroutine;

    // 💡 UI Button의 OnClick() 이벤트에 이 함수를 연결하세요!
    public void TogglePanel()
    {
        Logger.Log("Toggle");
        isExpanded = !isExpanded;

        for(int i = 0; i < panelImages.Count; i++)
            panelImages[i].raycastTarget = isExpanded;


        float targetHeight = isExpanded ? expandedSize : collapsedSize;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimatePanelSize(targetHeight));
    }

    private IEnumerator AnimatePanelSize(float targetHeight)
    {
        float elapsedTime = 0f;
        Vector2 startSize = panelRect.sizeDelta;

        // x축(너비)은 그대로 두고 y축(높이)만 목표값으로 설정합니다. 
        // (만약 좌우로 열리는 패널이라면 x와 y의 위치를 바꾸시면 됩니다)
        Vector2 targetSize = new Vector2(startSize.x, targetHeight);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // 0 ~ 1 사이의 진행도
            float t = elapsedTime / duration;

            // 💡 애니메이션을 고급스럽게: 끝날 때쯤 스르륵 감속하는 SmoothStep 적용
            t = t * t * (3f - 2f * t);

            // 현재 크기를 시작점과 목표점 사이에서 부드럽게 변경
            panelRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

            yield return null; // 다음 프레임까지 대기
        }

        // 코루틴 종료 시 오차를 없애기 위해 목표 크기로 정확히 고정
        panelRect.sizeDelta = targetSize;
        animationCoroutine = null;
    }
}
