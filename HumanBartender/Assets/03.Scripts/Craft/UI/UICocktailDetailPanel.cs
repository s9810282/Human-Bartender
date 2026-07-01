using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>재료 하나의 사용량을 막대 개수로 표시하는 게이지 UI 묶음.</summary>
[System.Serializable]
public struct IngrediantGauge
{
    public GameObject gaugeObj;
    public TextMeshProUGUI category;
    public GameObject[] gaugeBars;
}

/// <summary>칵테일 키워드(카테고리) 하나를 표시하는 토글형 배지 UI.</summary>
[System.Serializable]
public struct CategoryToggle
{
    public GameObject toggleObj;
    public TextMeshProUGUI categoryText;
    public Image categoryBG;
    public Image categorySelectBG;
}


/// <summary>
/// 칵테일 목록에서 슬롯을 선택했을 때 펼쳐지는 상세 정보 패널(이미지, 키워드, 재료 게이지, 설명).
/// 세로 높이를 애니메이션으로 늘렸다 줄이며 펼침/접힘 연출을 담당한다.
/// </summary>
public class UICocktailDetailPanel : MonoBehaviour
{
    [SerializeField] RectTransform body;
    [SerializeField] Image cocktailImage;

    [SerializeField] TextMeshProUGUI cocktailFlavorText;
    [SerializeField] CategoryColorData colorData;

    [SerializeField] IngrediantGauge[] ingrediantGauges;
    [SerializeField] CategoryToggle[] categoryToggles;

    [SerializeField] private float expandedHeight = 300f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private bool startExpanded = false;

    CocktailData data;

    private bool isExpanded;
    private Coroutine anim;

    /// <summary>패널을 시작 상태(펼침/접힘)로 초기화한다.</summary>
    public void Init()
    {
        isExpanded = startExpanded;
        gameObject.SetActive(startExpanded);
        SetHeight(isExpanded ? expandedHeight : 0f);
    }

    /// <summary>표시할 칵테일 데이터를 지정한다. 실제 UI 반영은 Set* 메서드들이 담당.</summary>
    public void SetData(CocktailData d)
    {
        data = d;
    }
    public void SetImage(Sprite sprite)
    {
        cocktailImage.sprite = sprite;
    }
    /// <summary>data.Keywords에 맞춰 카테고리 토글을 활성화하고, colorData에서 매칭되는 색상을 적용한다.</summary>
    public void SetCategorys()
    {
        for (int i = 0; i < categoryToggles.Length; i++)
        {
            categoryToggles[i].toggleObj.gameObject.SetActive(false);
        }

        for (int i = 0; i < data.Keywords.Length; i++)
        {
            categoryToggles[i].toggleObj.gameObject.SetActive(true);

            categoryToggles[i].categoryText.text = data.Keywords[i];

            int n = colorData.categorys.
               FindIndex(a => a.Contains(data.Keywords[i]));

            categoryToggles[i].categoryBG.color = colorData.colors[n];
            categoryToggles[i].categorySelectBG.color = colorData.colors[n];
        }
    }
    /// <summary>칵테일 설명(FlavorText)을 표시한다.</summary>
    public void SetSummary()
    {
        cocktailFlavorText.text = data.FlavorText;
    }
    /// <summary>레시피(data.Recipe)에 맞춰 재료별 게이지를 표시한다. 최대 5개 재료까지만 지원.</summary>
    public void SetIngrediantGauge()
    {
        foreach (var item in ingrediantGauges)
            item.gaugeObj.gameObject.SetActive(false);

        for (int i = 0; i < data.Recipe.Length; i++)
        {
            if (i >= 5) return;

            foreach (var item in ingrediantGauges[i].gaugeBars)
                item.gameObject.SetActive(false);

            ingrediantGauges[i].gaugeObj.gameObject.SetActive(true);
            ingrediantGauges[i].category.text = data.Recipe[i].DisplayName;

            
            for (int j = 0; j < data.Recipe[i].Count; j++)
            {
                ingrediantGauges[i].gaugeBars[j].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>패널을 펼치거나(isDown=true) 접는(isDown=false) 높이 애니메이션을 시작한다.</summary>
    public void SlideDetailPopup(bool isDown)
    {
        isExpanded = isDown;

        if (anim != null) StopCoroutine(anim);

        anim = StartCoroutine(AnimationHeight(body, isExpanded ? expandedHeight : 0f, duration));
    }
    /// <summary>body의 높이를 즉시 h로 설정한다 (애니메이션 없음).</summary>
    private void SetHeight(float h)
    {
        Vector2 sd = body.sizeDelta;
        sd.y = h;
        body.sizeDelta = sd;
    }

    /// <summary>rect의 현재 높이에서 target까지 duration초 동안 ease-in-out으로 보간한다.</summary>
    public IEnumerator AnimationHeight(RectTransform rect, float target, float duration)
    {
        float start = rect.sizeDelta.y;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration); // ease-in-out
            float h = Mathf.Lerp(start, target, k);

            Vector2 size = rect.sizeDelta;
            size.y = h;
            rect.sizeDelta = size;
            yield return null;
        }

        Vector2 end = rect.sizeDelta;
        end.y = target;
        rect.sizeDelta = end;
    }
}
