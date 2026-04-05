using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICraftButton : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private RectTransform panelRect; 
    [SerializeField] private List<Image> panelImages;
    [SerializeField] private float expandedSize = 130f;
    [SerializeField] private float collapsedSize = 0f;
    [SerializeField] private float duration = 0.3f;

    private bool isExpanded = false;
    private Coroutine animationCoroutine;

    public void TogglePanel()
    {
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
        Vector2 targetSize = new Vector2(startSize.x, targetHeight);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // 애니메이션을 고급스럽게: 끝날 때쯤 스르륵 감속하는 SmoothStep 적용
            t = t * t * (3f - 2f * t);
            panelRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

            yield return null;
        }

        panelRect.sizeDelta = targetSize;
        animationCoroutine = null;
    }
}
