using Cysharp.Threading.Tasks;
using UnityEngine;

public class BuildManager : MonoBehaviour, IMiniGameController
{
    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    [SerializeField] Canvas buttonCanvas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void InitGame(UniTaskCompletionSource tcs)
    {
        
    }

    public void CompleteMade()
    {
        
    }
}
