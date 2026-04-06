using Cysharp.Threading.Tasks;
using LiquidSimulation;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class ShakingManager : MonoBehaviour, IMiniGameController
{
    [SerializeField] Canvas gameCanvas;
    [SerializeField] Canvas buttonCanvas;

    [SerializeField] Text countText;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] ShakerInteraction shaker;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    [SerializeField] string sceneName = "Shake";

    private UniTaskCompletionSource tcs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameCanvas.worldCamera = Camera.main;
        buttonCanvas.worldCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitGame(UniTaskCompletionSource tcs)
    {
        this.tcs = tcs;
        craftStation.craftingResult.acionCount = 0; // 카운트 초기화
        if (countText != null) countText.text = "0";
    }


    public void AddActionCount()
    {
        craftStation.craftingResult.acionCount++;
        countText.text = craftStation.craftingResult.acionCount.ToString();
    }

    public void CompleteMade()
    {
        craftStation.craftingResult.isResult = true;

        if (shaker != null)
            craftStation.craftingResult.mixedColor = shaker.GetFullyMixedColor();
     
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
}
