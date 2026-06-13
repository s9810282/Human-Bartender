using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct IngrediantGauge
{
    public GameObject gaugeObj;
    public TextMeshProUGUI category;
    public GameObject[] gaugeBars;
}

[System.Serializable]
public struct CategoryToggle
{
    public GameObject toggleObj;
    public TextMeshProUGUI categoryText;
    public Image categoryBG;
    public Image categorySelectBG;
}


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

    public void Init()
    {
        isExpanded = startExpanded;
        gameObject.SetActive(startExpanded);
        SetHeight(isExpanded ? expandedHeight : 0f);
    }

    public void SetData(CocktailData d)
    {
        data = d;
    }
    public void SetImage(Sprite sprite)
    {
        cocktailImage.sprite = sprite;
    }
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
    public void SetSummary()
    {
        cocktailFlavorText.text = data.FlavorText;
    }
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

    public void SlideDetailPopup(bool isDown)
    {
        isExpanded = isDown;

        if (anim != null) StopCoroutine(anim);

        anim = StartCoroutine(AnimationHeight(body, isExpanded ? expandedHeight : 0f, duration));
    }
    private void SetHeight(float h)
    {
        Vector2 sd = body.sizeDelta;
        sd.y = h;
        body.sizeDelta = sd;
    }

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
