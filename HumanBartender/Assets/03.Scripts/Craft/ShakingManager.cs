using LiquidSimulation;
using UnityEngine;

public class ShakingManager : MonoBehaviour
{
    [SerializeField] CraftStationData craftStation;
    [SerializeField] ShakerInteraction shaker;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public void AddActionCount()
    {
        craftStation.craftingResult.acionCount++;
    }

    public void EndShake()
    {
        craftStation.craftingResult.mixedColor = shaker.GetFullyMixedColor();
    }
}
