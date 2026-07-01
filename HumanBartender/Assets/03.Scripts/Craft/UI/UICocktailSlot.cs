using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>호버/포커스 등 상태에 따라 on/off 스프라이트를 바꿔주는 비주얼 요소 하나.</summary>
[System.Serializable]
public struct VisualUI
{
    public Image img;
    public Sprite onSprite;
    public Sprite offSprite;
}


/// <summary>칵테일 도감 목록의 슬롯 하나. 클릭/호버 시각 효과와 클릭 이벤트(OnClick)를 제공한다.</summary>
public class UICocktailSlot : MonoBehaviour, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data UI")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Image cocktailImage;

    [Header("Visual UI")]
    [SerializeField] VisualUI[] visualUIs;

    CocktailData data;
    public event Action<CocktailData> OnClick;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnOffVisual(false);
    }

    /// <summary>클릭 시 콜백으로 전달할 칵테일 데이터를 저장한다.</summary>
    public void SetData(CocktailData d)
    {
        data = d;
    }

    public void SetImage(Sprite sprite)
    {
        cocktailImage.sprite = sprite;
    }
    public void SetNameText(string text)
    {
        nameText.text = text;
    }

    /// <summary>호버 등 상태에 따라 비주얼 요소들의 스프라이트를 on/off로 전환한다.</summary>
    public void OnOffVisual(bool isOn)
    {
        foreach (var item in visualUIs)
        {
            item.img.sprite = isOn ? item.onSprite : item.offSprite;
            //cocktailImage.gameObject.SetActive(isOn);
        }
    }
    /// <summary>페이지 갱신 시 슬롯 자체를 사용/미사용 상태로 전환한다 (데이터가 없는 빈 슬롯 처리용).</summary>
    public void OnOffSlot(bool isOn)
    {
        foreach (var item in visualUIs)
        {
            item.img.sprite = item.offSprite;
            nameText.text = isOn ? data.Name : "";
            cocktailImage.gameObject.SetActive(isOn);
        }
    }
    public void OnPointerClick(PointerEventData e)
    {
        switch (e.button)
        {
            case PointerEventData.InputButton.Left:
                OnClick?.Invoke(data);
                OnOffVisual(false);
                break;
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        OnOffVisual(true);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        OnOffVisual(false);
    }
}
