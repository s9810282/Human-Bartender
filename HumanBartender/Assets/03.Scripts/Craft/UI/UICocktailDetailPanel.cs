using System;
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
    [SerializeField] Image cocktailImage;
    [SerializeField] TextMeshProUGUI[] categoryText;
    [SerializeField] TextMeshProUGUI cocktailFlavorText;

    [SerializeField] CategoryGauge[] categoryGauges;

    CocktailData data;

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
        for(int i = 0; i < data.Keywords.Length; i++)
        {
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
}
