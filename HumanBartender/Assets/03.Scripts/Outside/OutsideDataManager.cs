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
    [SerializeField] List<OutsideCharacterData> outsideCharacterDatas;


    void Start()
    {
        foreach(var item in outsideCharacterDatas)
        {
            item.data.dayData = JsonManager<NPCCharacterDay>.LoadGameData_StreamingAssets(item.id);
        }
    }
}
