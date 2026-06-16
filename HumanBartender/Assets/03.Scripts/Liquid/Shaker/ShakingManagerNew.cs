using Cysharp.Threading.Tasks;
using Spine;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class ShakingManagerNew : MonoBehaviour, IMiniGameController
{
    [SerializeField] bool isTest = false;

    [Header("Data")]
    [SerializeField] CraftStationData data;
    [SerializeField] CategoryColorData colorData;
    [SerializeField] CocktailDataSO cocktailDataSO;

    [Header("Manager")]
    [SerializeField] ShakeLineCreator shakeLineCreator;
    [SerializeField] ShakingStrikeNode shakingStrikeNode;
    [SerializeField] ShakingCatergoryNodeCreator nodeCreator;
    [SerializeField] AnimSpeedController characterAnim;
    [SerializeField] AudioSource bgmSource;
    [SerializeField] GradientRatioController gageBar;

    [Header("UI")]
    [SerializeField] List<Image> dots;
    [SerializeField] Camera canvasCamera;
    [SerializeField] Canvas gameCanvas;
    [SerializeField] Canvas buttonCanvas;

    [Header("Judge")]
    [SerializeField] float judgeRange = 1;
    [SerializeField] int totalJudge;
    [SerializeField] int successJudge;
    [SerializeField] int failJudge;
    [SerializeField] int limitFailJudge;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    [Inject] ISoundManager soundManager;

    Vector3[] dotPositions;

    bool isPlay = false;

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

        shakeLineCreator.CreateLine();

        for (int i = 0; i < dots.Count; i++)
        {
            shakeLineCreator.SetLinePosition(i , GetDotWorldPosition(i));
        }

        dotPositions = new Vector3[dots.Count];
        for (int i = 0; i < dotPositions.Length; i++)
        {
            dotPositions[i] = GetDotWorldPosition(i);
        }


        shakingStrikeNode.transform.position = dotPositions[0];

        Color[] colors = new Color[data.targetCocktailData.Keywords.Length];
        for(int i = 0; i < colors.Length; i++)
        {
            int n = colorData.categorys.
                FindIndex(a => a.Contains(data.targetCocktailData.Keywords[i]));

            colors[i] = colorData.colors[n];
        }

        nodeCreator.Init();
        nodeCreator.InitToStart(
            dotPositions, 
            colors);

        totalJudge = Mathf.RoundToInt(data.targetCraft_tolerance * 1.3f);
        successJudge = 0;
        failJudge = 0;
        limitFailJudge = Mathf.RoundToInt(data.targetCraft_tolerance * 0.3f);

        gageBar.UpdateValues(totalJudge, 0, totalJudge, 0);

        isPlay = true;
    }

    
    void Update()
    {
        shakingStrikeNode.Handle();
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
        shakingStrikeNode.InitToStart(dotPositions, 60);
        characterAnim.Init();
    }

    public void ClickEvent()
    {
        if (!isPlay) return;

        Logger.Log("Click Event");
        CategoryNode node =  nodeCreator.GetNearestNode(shakingStrikeNode.transform.position, judgeRange);

        if (node != null)
        {          
            Logger.Log("Judge");
            soundManager.PlaySE("SFX_shaking");
            characterAnim.PlayAnim();
            nodeCreator.CreateEffectNode(node.transform.position, node.curColor);
            successJudge++;
        }
        else
        {
            Logger.Log("Judge Fail");
            nodeCreator.CreateEffectNode(shakingStrikeNode.transform.position, Color.white);
            failJudge++;
        }

        gageBar.UpdateValues(totalJudge, successJudge, totalJudge-successJudge-failJudge, failJudge);

        if(successJudge + failJudge >= totalJudge)
        {
            isPlay = false;
            CompleteMade();
        }
        else if(failJudge > limitFailJudge)
        {
            isPlay = false;
            CompleteMade();
        }
    }







    public Vector3 GetDotWorldPosition(int index)
    {
        Vector3 local = dots[index].rectTransform.position;

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint
            (canvasCamera, local);
        screenPos.z = 10f;
        Vector3 worldPos = canvasCamera.ScreenToWorldPoint(screenPos);


        return worldPos;
    }
}
