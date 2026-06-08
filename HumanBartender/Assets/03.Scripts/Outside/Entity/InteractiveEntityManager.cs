using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

[System.Serializable]
public struct ObjectEntity
{
    public string id;
    public InteractiveObjectEntity entity;
}

[System.Serializable]
public struct NPCEntity
{
    public string id;
    public NPCCharacterDayDataSO data;
    public InteractiveNPCEntity entity;
}

[System.Serializable]
public struct TriggetEntity
{
    public string id;
    public InteractiveTriggerEntity entity;
}

[System.Serializable]
public struct TestFlag
{
    public string flag;
    public bool bValue;
}

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

    [SerializeField] protected List<ObjectEntity> obejcts;
    [SerializeField] protected List<NPCEntity> npcs;
    [SerializeField] protected List<TriggetEntity> triggers;

    [Header("Player")]
    [SerializeField] GameObject player;
    [SerializeField] InteractEntrance barEntrance;
    [SerializeField] InteractEntrance homeEntrance;
    [SerializeField] OutsideElevator elevator;


    [Inject] IPlayerDataWriter testPlayerWriter;
    [Inject] IPlayerDataReader playerData;
    [Inject] ISoundManager soundManager;
    [Inject] IObjectResolver resolver;

    //day 값 보고 검사 하기.
    void Awake()
    {
        
    }

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
    }



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
