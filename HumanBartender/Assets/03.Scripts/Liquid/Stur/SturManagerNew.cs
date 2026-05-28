using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SturManagerNew : MonoBehaviour, IMiniGameController 
{
    [Header("Data")]
    [SerializeField] CraftStationData data;
    [SerializeField] CategoryColorData colorData;
    [SerializeField] CocktailDataSO cocktailDataSO;

    [Header("Manager")]
    [SerializeField] SturStrikeNode sturStrikeNode;
    [SerializeField] CircleLineCreator circleLineCreator;
    [SerializeField] AudioSource bgmSource;

    [Header("UI")]
    [SerializeField] Image center;
    [SerializeField] float radius = 2;
    [SerializeField] Camera canvasCamera;
    [SerializeField] Canvas gameCanvas;
    [SerializeField] Canvas buttonCanvas;

    [Header("Judge")]
    [SerializeField] float judgeRange = 1;
    [SerializeField] int totalJudge;
    [SerializeField] int successJudge;
    [SerializeField] int failJudge;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvasCamera = Camera.main;

        gameCanvas.worldCamera = canvasCamera;
        buttonCanvas.worldCamera = canvasCamera;

        data.targetCocktailData = cocktailDataSO.allCocktails[data.targetCocktailId];
        data.targetCraft_tolerance = 15;

        circleLineCreator.BuildCircle(radius, GetCenterWorldPosition());
    }

    // Update is called once per frame
    void Update()
    {
        sturStrikeNode.Handle();
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
        data.craftingResult.actionFailCount = successJudge;

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
        Logger.Log("Start Game");

        bgmSource.PlayScheduled(AudioSettings.dspTime + 0.1f);
        sturStrikeNode.InitToStart(GetCenterWorldPosition(), 60, 4, radius);
    }

    public void ClickEvent()
    {
        Logger.Log("Click Event");
       
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
