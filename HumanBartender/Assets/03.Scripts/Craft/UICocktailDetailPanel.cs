using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using NUnit.Framework;
using System.Collections;


public class UICocktailDetailPanel : MonoBehaviour
{
    [SerializeField] Image cocktailImage;
    [SerializeField] Text cocktailNameText;

    [SerializeField] int titleTextSize;
    [SerializeField] int summaryTextSize;

    [SerializeField] int currentContentsCount = 0;

    [SerializeField] CocktailData currentCocktailData;

    [Header("Select Event")]
    [SerializeField] CocktailDataEvent selectEvent;

    [Header("UI 할당 (인스펙터용)")]
    [SerializeField] private List<UIContentsSpacer> inspectorSpacerList;
    [SerializeField] private List<UIContentsText> inspectorContextList;


    Queue<UIContentsSpacer> contentsSpacerList = new();
    Queue<UIContentsText> contentsContextList = new();

    Queue<UIContentsSpacer> usedUISpacers = new Queue<UIContentsSpacer>();
    Queue<UIContentsText> usedUIContexts = new Queue<UIContentsText>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        contentsSpacerList = new Queue<UIContentsSpacer>(inspectorSpacerList);
        contentsContextList = new Queue<UIContentsText>(inspectorContextList);

        inspectorSpacerList.Clear();
        inspectorContextList.Clear();

        ResetPanel();
    }



    public void OnClickSelectButton()
    {
        selectEvent?.Raise(currentCocktailData);
    }

    public void SetCocktailData(CocktailData data)
    {
        currentCocktailData = data;
    }
    public void SetImage(Sprite sprite)
    {
        cocktailImage.gameObject.SetActive(true);
        cocktailImage.sprite = sprite;
    }    
    public void SetCocktailName(string text)
    {
        cocktailNameText.text = text;
    }
    
    /// <summary>
    /// Spacer 3개 On
    /// TitleText 작성
    /// Spacer 2개 On
    /// </summary>
    /// <param name="text"></param>
    public void AddContentsTitle(string text)
    {
        AddSpacer(30);

        if (contentsContextList.Count > 0)
        {
            var context = contentsContextList.Dequeue();
            context.gameObject.SetActive(true);
            context.transform.SetSiblingIndex(currentContentsCount);

            context.GetComponent<Text>().text = text;
            context.SetFontSize(titleTextSize);

            usedUIContexts.Enqueue(context);
            currentContentsCount++;
        }
        else
        {
            Debug.LogWarning("사용 가능한 Context UI가 큐에 부족합니다!");
        }

        AddSpacer(20);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    public void AddContentsSummary(string text)
    {
        if (contentsContextList.Count > 0)
        {
            var context = contentsContextList.Dequeue();
            context.gameObject.SetActive(true);
            context.transform.SetSiblingIndex(currentContentsCount);

            context.GetComponent<Text>().text = text;
            context.SetFontSize(summaryTextSize);

            usedUIContexts.Enqueue(context);
            currentContentsCount++;
        }
        else
        {
            Debug.LogWarning("사용 가능한 Context UI가 큐에 부족합니다!");
        }
        
        AddSpacer(10);
    }
    private void AddSpacer(int size)
    {
        if (contentsSpacerList.Count > 0)
        {

            var spacer = contentsSpacerList.Dequeue();
            spacer.gameObject.SetActive(true);
            spacer.transform.SetSiblingIndex(currentContentsCount);
            spacer.SetElementHeight(size);

            usedUISpacers.Enqueue(spacer);
            currentContentsCount++;
        }
        else
        {
            Debug.LogWarning("사용 가능한 Spacer UI가 큐에 부족합니다!");
            return;
        }
    }
    public void ResetPanel()
    {
        currentCocktailData = default;
        cocktailImage.sprite = null;
        cocktailNameText.text = "";

        foreach (var spacer in usedUISpacers)
        {
            spacer.gameObject.SetActive(false);
            contentsSpacerList.Enqueue(spacer);
        }

        usedUISpacers.Clear();

        foreach (var context in usedUIContexts)
        {
            context.gameObject.SetActive(false);
            contentsContextList.Enqueue(context);
        }

        usedUIContexts.Clear();

        currentContentsCount = 0;
    }
}
