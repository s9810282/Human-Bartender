using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SturManagerNew : MonoBehaviour, IMiniGameController 
{
    [SerializeField] bool isTest = false;

    [Header("Data")]
    [SerializeField] CraftStationData data;
    [SerializeField] CategoryColorData colorData;
    [SerializeField] CocktailDataSO cocktailDataSO;

    [Header("Manager")]
    [SerializeField] SturStrikeNode sturStrikeNode;
    [SerializeField] CircleLineCreator circleLineCreator;
    [SerializeField] CircleNodeCreator nodeCreator;
    [SerializeField] AudioSource bgmSource;
    [SerializeField] GradientRatioController gageBar;

    [Header("UI")]
    [SerializeField] Image center;
    [SerializeField] float radiusX = 2.4f;
    [SerializeField] float radiusY = 2.0f;
    [SerializeField] Camera canvasCamera;
    [SerializeField] Canvas gameCanvas;
    [SerializeField] Canvas buttonCanvas;
    [SerializeField] int bpm = 60;
    [SerializeField] int beatCount = 4;


    [Header("Judge")]
    [SerializeField] float judgeRange = 1;
    [SerializeField] int totalJudge;
    [SerializeField] int successJudge;
    [SerializeField] int failJudge;
    [SerializeField] int limitFailJudge;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    bool isPlay = false;
    Color[] colors;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasCamera = Camera.main;

        gameCanvas.worldCamera = canvasCamera;
        buttonCanvas.worldCamera = canvasCamera;


        if (isTest)
        {
            data.targetCocktailData = cocktailDataSO.allCocktails[data.targetCocktailId];
            data.targetCraft_tolerance = 15;
        }

        colors = new Color[data.targetCocktailData.Keywords.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            Logger.Log(data.targetCocktailData.Keywords[i]);
            int n = colorData.categorys.
                FindIndex(a => a.Contains(data.targetCocktailData.Keywords[i]));

            colors[i] = colorData.colors[n];
        }


        circleLineCreator.BuildCircle(radiusX, radiusY, GetCenterWorldPosition());
        nodeCreator.Init(
            radiusX, radiusY,
            GetCenterWorldPosition(),
            colors);


        sturStrikeNode.Init(GetCenterWorldPosition(), bpm, beatCount, radiusX, radiusY);


        totalJudge = Mathf.RoundToInt(data.targetCraft_tolerance * 1.3f);
        successJudge = 0;
        failJudge = 0;
        limitFailJudge = Mathf.RoundToInt(data.targetCraft_tolerance * 0.3f);

        gageBar.UpdateValues(totalJudge, 0, totalJudge, 0);

        isPlay = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPlay) return;

        sturStrikeNode.Handle();
        nodeCreator.Handle();
    }


    private UniTaskCompletionSource tcs;
    public void InitGame(UniTaskCompletionSource tcs)
    {
        this.tcs = tcs;
        data.craftingResult.actionFailCount = 0;
    }

    public void CompleteMade()
    {
        bgmSource.Stop();
        data.craftingResult.isResult = true;
        data.craftingResult.actionFailCount = failJudge;
        data.craftingResult.limitFailCount = limitFailJudge;

        if (tcs != null)
        {
            Logger.Log("shakeManager tcs not null");
            tcs.TrySetResult();
        }
    }

    public void OnNextButton()
    {
        buttonCanvas.gameObject.SetActive(true);
    }

    public void Serve()
    {
        craftServe?.Raise(new Void());
    }

    public void Retry()
    {
        craftRetry?.Raise(new Void());
    }

    public void StartGame()
    {
        if (!isPlay) return;

        Logger.Log("Start Game");

        bgmSource.PlayScheduled(AudioSettings.dspTime + 0.1f);
        sturStrikeNode.InitToStart();
        nodeCreator.InitToStart();
    }

    public void OnPressEvent()
    {
        if (!isPlay) return;

        Logger.Log("Click Event");
        CategoryNode node = nodeCreator.GetNearestNode(sturStrikeNode.transform.position, judgeRange);

        if (node != null)
        {
            Logger.Log("Judge");
            nodeCreator.CreateEffectNode(node.transform.position, node.curColor);
            successJudge++;
        }
        else
        {
            Logger.Log("Judge Fail");
            nodeCreator.CreateEffectNode(sturStrikeNode.transform.position, Color.white);
            failJudge++;
        }

        gageBar.UpdateValues(totalJudge, successJudge, totalJudge - successJudge - failJudge, failJudge);

        if (successJudge + failJudge >= totalJudge)
        {
            isPlay = false;
            CompleteMade();
        }
        else if (failJudge > limitFailJudge)
        {
            isPlay = false;
            CompleteMade();
        }
    }

    public void OnReleaseEvent()
    {
        if (!isPlay) return;
    }



    public Vector3 GetCenterWorldPosition()
    {
        Vector3 local = center.rectTransform.position;

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint
            (canvasCamera, local);
        screenPos.z = 10f;
        Vector3 worldPos = canvasCamera.ScreenToWorldPoint(screenPos);


        return worldPos;
    }
}
