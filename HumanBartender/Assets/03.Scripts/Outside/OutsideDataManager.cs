using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OutsideCharacterData
{
    public string id;
    public NPCCharacterDayDataSO data;
}


public class OutsideDataManager : MonoBehaviour
{
    [SerializeField] OutsideObjectDataSO objectData;
    [SerializeField] List<OutsideCharacterData> outsideCharacterDatas;
    [SerializeField] OutsideRadioDataSO radioData;

    public void Load()
    {
        objectData.outsideObjectData = JsonManager<OutsideObjectDataBase>.
            LoadGameData_StreamingAssets("Outside\\outside_objects.json");

        objectData.Cached();

        radioData.radioData = JsonManager<RadioDataBase>.
            LoadGameData_StreamingAssets("Outside\\elevator_radio.json");

        foreach (var item in outsideCharacterDatas)
        {
            item.data.dayData = JsonManager<NPCCharacterDay>.LoadGameData_StreamingAssets(item.id);
        }
    }
}
