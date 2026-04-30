using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OutsideCharacterData
{
    public string id;
    public DayDataSO data;
}


public class OutsideDataManager : MonoBehaviour
{
    [SerializeField] List<OutsideCharacterData> outsideCharacterDatas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //비동기처리하기 OS 시간에 배움 
        foreach(var item in outsideCharacterDatas)
        {
            item.data.dayData = JsonManager<DayDatabBase>.LoadGameData_StreamingAssets(item.id);
            Logger.Log(item.data.dayData.Day);
            Logger.Log(item.data.dayData.Scenes.Length);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
