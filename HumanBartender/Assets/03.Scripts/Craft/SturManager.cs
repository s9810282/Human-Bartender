using Cysharp.Threading.Tasks;
using LiquidSimulation;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static UnityEngine.Rendering.ProbeAdjustmentVolume;

public class SturManager : MonoBehaviour, IMiniGameController
{
    [SerializeField] Text countText;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] StirringInteraction stir;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    private UniTaskCompletionSource tcs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitGame(UniTaskCompletionSource tcs)
    {
        this.tcs = tcs;
        craftStation.craftingResult.actionFailCount = 0; // 카운트 초기화
        if (countText != null) countText.text = "0";
    }



    public void CompleteMade()
    {
        craftStation.craftingResult.isResult = true;

        if (stir != null)
            craftStation.craftingResult.mixedColor = stir.GetFullyMixedColor();

        if (tcs != null)
        {
            Logger.Log("shakeManager tcs not null");
            tcs.TrySetResult();
        }
    }
    public void OnNextButton()
    {
        
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
