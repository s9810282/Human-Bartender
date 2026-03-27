using UnityEngine;

public class DataLoadManager : MonoBehaviour
{
    [SerializeField] CocktailDataSO cocktailData;
    [SerializeField] IngredientDataSO ingredientDataSO;
    [SerializeField] CharacterDataSO characterData;
    [SerializeField] DayDataSO dayData;
    [SerializeField] CraftDataSO craftData;
    [SerializeField] CutSceneDataSO cutSceneData;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cocktailData.cocktailData = JsonManager<CocktailDataBase>.LoadGameData_StreamingAssets("cocktails.json");
        characterData.characterData = JsonManager<CharacterDataBase>.LoadGameData_StreamingAssets("characters.json");
        ingredientDataSO.ingredientData = JsonManager<IngredientDataBase>.LoadGameData_StreamingAssets("ingredients.json");
        dayData.dayData = JsonManager<DayDatabBase>.LoadGameData_StreamingAssets("day1.json");
        craftData.craftData = JsonManager<CraftDataBase>.LoadGameData_StreamingAssets("day1_crafts.json");
        cutSceneData.cutSceneData = JsonManager<CutSceneDataBase>.LoadGameData_StreamingAssets("cutscenes.json");

        foreach (var item in cutSceneData.cutSceneData.position_presets)
        {
            Logger.Log(item.Key);
            Logger.Log(item.Value);
        }


        cocktailData.Cached();
    }
}
