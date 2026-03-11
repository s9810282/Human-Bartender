using UnityEngine;

public class DataLoadManager : MonoBehaviour
{
    [SerializeField] CocktailDataSO cocktailData;
    [SerializeField] CharacterDataSO characterData;
    [SerializeField] DayDataSO dayData;
    [SerializeField] CraftDataSO craftData;
        

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cocktailData.cocktailData = JsonManager<CocktailDataBase>.LoadGameData_StreamingAssets("cocktails.json");
        characterData.characterData = JsonManager<CharacterDataBase>.LoadGameData_StreamingAssets("characters.json");
        dayData.dayData = JsonManager<DayDatabBase>.LoadGameData_StreamingAssets("day1.json");
        craftData.craftData = JsonManager<CraftDataBase>.LoadGameData_StreamingAssets("day1_crafts.json");

    }
}
