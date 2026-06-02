using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct CategoryGauge
{
    public GameObject gaugeObj;
    public TextMeshProUGUI category;
    public GameObject[] gaugeBars;
}


public class UICocktailDetailPanel : MonoBehaviour
{
    [SerializeField] RectTransform body;
    [SerializeField] Image cocktailImage;
    [SerializeField] TextMeshProUGUI[] categoryText;
    [SerializeField] TextMeshProUGUI cocktailFlavorText;

    [SerializeField] CategoryGauge[] categoryGauges;

    [SerializeField] private float expandedHeight = 300f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private bool startExpanded = false;

    CocktailData data;

    private bool isExpanded;
    private Coroutine anim;

    private void Start()
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
        for (int i = 0; i < categoryText.Length; i++) 
            categoryText[i].gameObject.SetActive(false);

        for (int i = 0; i < data.Keywords.Length; i++)
        {
            categoryText[i].gameObject.SetActive(true);
            categoryText[i].text = data.Keywords[i];
        }
    }
    public void SetSummary()
    {
        cocktailFlavorText.text = data.FlavorText;
    }
    public void SetIngrediantGauge()
    {
        foreach (var item in categoryGauges)
            item.gaugeObj.gameObject.SetActive(false);

        for (int i = 0; i < data.Recipe.Length; i++)
        {
            if (i >= 5) return;

            foreach (var item in categoryGauges[i].gaugeBars)
                item.gameObject.SetActive(false);

            categoryGauges[i].gaugeObj.gameObject.SetActive(true);
            categoryGauges[i].category.text = data.Recipe[i].DisplayName;

            
            for (int j = 0; j < data.Recipe[i].Count; j++)
            {
                categoryGauges[i].gaugeBars[j].gameObject.SetActive(true);
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
