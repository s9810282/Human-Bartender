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


    public void Load()
    {
        objectData.outsideObjectData = JsonManager<OutsideObjectDataBase>.
            LoadGameData_StreamingAssets("Outside\\outside_objects.json");

        objectData.Cached();

        foreach (var item in outsideCharacterDatas)
        {
            item.data.dayData = JsonManager<NPCCharacterDay>.LoadGameData_StreamingAssets(item.id);
        }
    }
}
