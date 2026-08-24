using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>데이터 id와 씬에 배치된 오브젝트 엔티티를 짝짓는 항목.</summary>
[System.Serializable]
public struct ObjectEntity
{
    public string id;
    public InteractiveObjectEntity entity;
}

/// <summary>데이터 id, 일자별 데이터 SO, 씬에 배치된 NPC 엔티티를 짝짓는 항목.</summary>
[System.Serializable]
public struct NPCEntity
{
    public string id;
    public NPCCharacterDayDataSO data;
    public InteractiveNPCEntity entity;
}

/// <summary>데이터 id와 씬에 배치된 트리거(컷씬) 엔티티를 짝짓는 항목.</summary>
[System.Serializable]
public struct TriggetEntity
{
    public string id;
    public InteractiveTriggerEntity entity;
}

[System.Serializable]
public struct Entity
{
    public string id;
    public InteractiveEntity entity;
}

/// <summary>테스트 모드에서 강제로 세팅할 플래그 값.</summary>
[System.Serializable]
public struct TestFlag
{
    public string flag;
    public bool bValue;
}

/// <summary>
/// 실외 씬의 모든 상호작용 엔티티(오브젝트/NPC/트리거)를 현재 날짜·게임 흐름·조건에 맞춰
/// 스폰 여부와 표시 가능한 대사(FlowData)를 매 씬 진입 시 갱신하는 총괄 매니저.
/// </summary>
public class InteractiveEntityManager : MonoBehaviour
{
    [Header("Test")]
    [SerializeField] bool isTest = false;
    [SerializeField] int testDay = 0;
    [SerializeField] EGameFlow testFlow = EGameFlow.CommuteIn;
    [SerializeField] List<TestFlag> testFlags = new List<TestFlag>();

    [Header("Data")]
    [SerializeField] protected OutsideObjectDataSO obejctData;
    [SerializeField] protected OutsideTriggerCutSceneSO triggerData;
    //=================================================================
    [SerializeField] protected NewInteractPointDataSO InteractPointData;
    [SerializeField] protected NewStreetDataSO streetData;
    [SerializeField] protected NewSpotDataSO SpotData;

    [SerializeField] protected List<ObjectEntity> obejcts;
    [SerializeField] protected List<NPCEntity> npcs;
    [SerializeField] protected List<TriggetEntity> triggers;
    [SerializeField] protected List<Entity> entities;

    [Header("Player")]
    [SerializeField] GameObject player;
    [SerializeField] InteractEntrance barEntrance;
    [SerializeField] InteractEntrance homeEntrance;
    [SerializeField] OutsideElevator elevator;


    [Inject] IPlayerDataWriter testPlayerWriter;
    [Inject] IPlayerDataReader playerData;
    [Inject] ISoundManager soundManager;
    [Inject] IObjectResolver resolver;
    [Inject] IConditionUtil conditionUtil;
    //day 값 보고 검사 하기.
    void Awake()
    {
    }

    /// <summary>
    /// (테스트 모드면 날짜/흐름/플래그를 강제 세팅 후) BGM 재생, 출입구 활성화, 엘리베이터 위치,
    /// 플레이어 스폰 위치를 현재 GameFlow에 맞춰 초기화하고 엔티티 상태를 갱신한다.
    /// </summary>
    private void Start()
    {
        if (isTest)
        {
            GameStateManager.Instance.CurrentDay = testDay;
            GameStateManager.Instance.GameFlow = testFlow;

            foreach (var flag in testFlags)
            {
                testPlayerWriter.AddFlag(flag.flag, flag.bValue);
            }
        }

        Logger.Log("Entity Init");
        soundManager.PlayBGM("BGM_outside");

        barEntrance.IsAvaliable = GameStateManager.Instance.GameFlow == EGameFlow.CommuteIn;
        homeEntrance.IsAvaliable = GameStateManager.Instance.GameFlow == EGameFlow.CommuteOut;

        
        elevator.SetPosition(GameStateManager.Instance.GameFlow == EGameFlow.CommuteIn);

        if (GameStateManager.Instance.GameFlow == EGameFlow.CommuteIn)
            player.transform.position = homeEntrance.spawnPoint;
        else
            player.transform.position = barEntrance.spawnPoint;


        RefreshEntity();
        Debug.Log(conditionUtil.Check("day == 2 && !flag.d3_cat_seen"));
    }


    public void RefreshEntity()
    {
        foreach (var entity in entities)
        {
            //데이터를 캐싱 실패시 로그 반환
            if(!InteractPointData.TryGetData(entity.id,out NewInteractPointData curdata))
            {
                Debug.Log($"{entity.id}의 정보를 불러오는것에 실패 출처는 인터렉트엔티티메니저");
                continue;
            }
            //콘디션이 맞지않다면 엔티티를 비활성화후 탈출
            if (!conditionUtil.Check(curdata.When))
            {
                entity.entity.IsAvaliable = false;
                continue;
            }
            entity.entity.IsAvaliable = true;
            //스팟데이터를 참조 위치를 변경
            if(!SpotData.TryGetData(curdata.Spot,out NewSpotData spotdata))
            {
                Debug.Log($"{entity.id}가{curdata.Spot}의 정보를 불러오는것에 실패 출처는 인터렉트엔티티메니저");
                continue;
            }
            entity.entity.transform.position = spotdata.Position;
            entity.entity.transform.rotation = spotdata.Rotation;
            //엔티티의 행동 변수 조정
            entity.entity.kind = curdata.Kind;
            entity.entity.phase = curdata.Phase;
            entity.entity.trigger = curdata.Trigger;
            entity.entity.selection = curdata.Selection;
            if (!streetData.TryGetSceneData(curdata.SceneOrShop,out NewSceneData sceneData))
            {
                Debug.Log($"{entity.id}가{curdata.SceneOrShop}의 정보를 불러오는것에 실패 출처는 인터렉트엔티티메니저");
                continue;
            }

        }
    }

    /*
public void RefreshEntity()
{
    //Day가 null이면 늘 인터렉션 가능, 아닐 경우 적힌 날짜에만.
    foreach (var entity in obejcts)
    {
        bool isSpawn = false;

        if (obejctData.outsideObjects[entity.id].Day == null) isSpawn = true;
        else if (obejctData.outsideObjects[entity.id].Day == GameStateManager.Instance.CurrentDay) isSpawn = true;
        else isSpawn = false;

        entity.entity.IsAvaliable = isSpawn;
        entity.entity.gameObject.SetActive(isSpawn);

        if (!isSpawn) continue;

        List<FlowData> flowData = new();
        ESelectionType targetSelection = ESelectionType.Sequential;

        foreach (var item in obejctData.outsideObjects[entity.id].FlowData)
        {
            if (item.Conditions == null) flowData.Add(item);
            else if (CheckCondition(item.Conditions)) flowData.Add(item);
        }

        targetSelection = obejctData.outsideObjects[entity.id].Selection.Type;
        entity.entity.EntityLabel = obejctData.outsideObjects[entity.id].InteractLabel;
        entity.entity.InjectDialogue(flowData, targetSelection);
    }

    foreach (var entity in npcs)
    {
        //날짜, 출퇴근이 맞고, 스폰 조건이 맞으면 스폰,
        //인터렉션 여부는 flow로 결정. 조건이 맞는 flow가 여러개면 
        //curDay랑, Selection 기준으로 주입 변경.

        bool isSpawn = false;
        List<FlowData> flowData = new();
        ESelectionType targetSelection = ESelectionType.Sequential;

        //날짜, 출퇴근 상태가 안맞다면 검사 필요 X
        foreach (var days in entity.data.dayData.Days)
        {
            if (days.Day != GameStateManager.Instance.CurrentDay) continue;
            if (days.Route != GameStateManager.Instance.GameFlow) continue;

            isSpawn = CheckCondition(days.SpawnCondotion);

            if (isSpawn)
            {
                targetSelection = days.Selection.Type;

                foreach (var item in days.FlowData)
                {
                    if (item.Conditions == null) flowData.Add(item);
                    else if (item.Conditions.Value.Conditions != null)
                    {
                        bool isCheck = true;
                        foreach (var condition in item.Conditions.Value.Conditions)
                        {
                            if (CheckCondition(condition) == false)
                            {
                                isCheck = false;
                                break;
                            }
                        }

                        if (isCheck == false)
                        {
                            continue;
                        }

                        flowData.Add(item);
                    }
                    else if (CheckCondition(item.Conditions)) flowData.Add(item);
                }
            }
        }

        entity.entity.gameObject.SetActive(isSpawn);

        if (flowData.Count > 0)
        {
            entity.entity.IsAvaliable = true;
            entity.entity.EntityLabel = entity.data.dayData.InteractData.Value.Label;
            entity.entity.InjectDialogue(flowData, targetSelection);
        }
        else
        {
            entity.entity.IsAvaliable = false;
        }
    }


    foreach (var entity in triggers)
    {
        bool isSpawn = false;

        CutSceneEventData data = triggerData.cutSceneEventDic[entity.id];

        //날짜 및 출퇴근 시간이 맞지 않는 다면. false
        if (data.Day != GameStateManager.Instance.CurrentDay) isSpawn = false;
        else  if (data.timing != GameStateManager.Instance.GameFlow) isSpawn = false;


        //조건이 없으면 true
        else if (data.Conditions == null) isSpawn = true;
        //조건이 and라면 conditions 계산.
        else if (data.Conditions.Value.Type == EConditionCheckType.And)
        {
            bool isCheck = true;
            foreach (var condition in data.Conditions.Value.Conditions)
            {
                if (CheckCondition(condition) == false)
                {
                    isCheck = false;
                    break;
                }
            }

            if (isCheck == false) continue;
        }

        else if (CheckCondition(data.Conditions)) isSpawn = true;

        if (resolver == null)
        {
            Debug.LogError("DI 에러] DialogueManager가 resolver를 받지 못했습니다!");
        }

        resolver.Inject(entity.entity);

        entity.entity.gameObject.SetActive(isSpawn);
        entity.entity.SetId(data.CutSceneId);
        entity.entity.IsAvaliable = isSpawn;
    }
}
    */

    /// <summary>
    /// OutsideCondition(오브젝트/NPC 스폰 조건용) 하나를 검사한다. null이면 항상 통과.
    /// 아래 Condition 오버로드와 조건 검사 로직이 동일하게 중복 구현되어 있으니 함께 참고할 것.
    /// </summary>
    public bool CheckCondition(OutsideCondition? checkType)
    {
        if (checkType == null) return true;

        switch (checkType.Value.Type)
        {
            case EConditionCheckType.None:
                break;

            case EConditionCheckType.Affinity:
                int characterTier = playerData.GetCurCharacterAffinityValue(checkType.Value.Character);
                int targettier = checkType.Value.Min;
                return characterTier >= targettier;

            case EConditionCheckType.Skill:
                return playerData.GetSkillValue() >= checkType.Value.Min;

            case EConditionCheckType.Money:
                return playerData.HasEnoughMoney(checkType.Value.Min);

            case EConditionCheckType.Flag:
                return playerData.CheckFlag(checkType.Value.FlagId) == checkType.Value.BValue;
        }

        return false;
    }
    /// <summary>Condition(대사 FlowData 조건용) 하나를 검사한다. null이면 항상 통과.</summary>
    public bool CheckCondition(Condition? checkType)
    {
        if (checkType == null) return true;

        switch (checkType.Value.Type)
        {
            case EConditionCheckType.None:
                break;

            case EConditionCheckType.Affinity:
                int characterTier = playerData.GetCurCharacterAffinityValue(checkType.Value.Character);
                int targettier = checkType.Value.Min;
                return characterTier >= targettier;

            case EConditionCheckType.Skill:
                return playerData.GetSkillValue() >= checkType.Value.Min;

            case EConditionCheckType.Money:
                return playerData.HasEnoughMoney(checkType.Value.Min);

            case EConditionCheckType.Flag:
                return playerData.CheckFlag(checkType.Value.FlagId) == checkType.Value.BValue;
        }

        return false;
    }
}
