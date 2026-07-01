using System.Collections.Generic;
using UnityEngine;

/// <summary>NPC id와 해당 NPC의 일자별 데이터 SO를 짝지은 항목.</summary>
[System.Serializable]
public class OutsideCharacterData
{
    public string id;
    public NPCCharacterDayDataSO data;
}


/// <summary>
/// 실외 씬에서 사용하는 각종 JSON 데이터(오브젝트, 라디오, 컷씬 트리거, NPC별 일자 데이터)를
/// StreamingAssets에서 로드해 대응 ScriptableObject에 채워 넣는 로더.
/// </summary>
public class OutsideDataManager : MonoBehaviour
{
    [SerializeField] OutsideObjectDataSO objectData;
    [SerializeField] List<OutsideCharacterData> outsideCharacterDatas;
    [SerializeField] OutsideRadioDataSO radioData;
    [SerializeField] OutsideTriggerCutSceneSO triggerCutSceneSO;

    /// <summary>실외 씬 진입 시 필요한 모든 데이터를 StreamingAssets에서 읽어 SO에 반영한다.</summary>
    public void Load()
    {
        objectData.outsideObjectData = JsonManager<OutsideObjectDataBase>.
            LoadGameData_StreamingAssets("Outside/outside_objects.json");

        objectData.Cached();

        radioData.radioData = JsonManager<RadioDataBase>.
            LoadGameData_StreamingAssets("Outside/elevator_radio.json");

        triggerCutSceneSO.cutSceneEvent = JsonManager<CutSceneEventBase>.
           LoadGameData_StreamingAssets("cutscene_events.json");

        triggerCutSceneSO.Cached();

        foreach (var item in outsideCharacterDatas)
        {
            item.data.dayData = JsonManager<NPCCharacterDay>.LoadGameData_StreamingAssets(item.id);
        }
    }
}
