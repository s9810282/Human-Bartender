using Spine;
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

public class InteractiveEntityManager : MonoBehaviour
{
    [Header("Test")]
    [SerializeField] bool isTest = false;
    [SerializeField] int testDay = 0;
    [SerializeField] EGameFlow testFlow = EGameFlow.CommuteIn;

    [Header("Data")]
    [SerializeField] OutsideDataManager outsideDataManager;
    [SerializeField] protected OutsideObjectDataSO obejctData;

    [SerializeField] protected List<ObjectEntity> obejcts;
    [SerializeField] protected List<NPCEntity> npcs;

    [Header("Player")]
    [SerializeField] GameObject player;
    [SerializeField] InteractEntrance barEntrance;
    [SerializeField] InteractEntrance homeEntrance;
    [SerializeField] OutsideElevator elevator;


    [Inject] IPlayerDataReader playerData;
    [Inject] ISoundManager soundManager;

    //day 값 보고 검사 하기.
    void Awake()
    {
        outsideDataManager.Load();

        if (isTest)
        {
            GameStateManager.Instance.CurrentDay = testDay;
            GameStateManager.Instance.GameFlow = testFlow;
        }
    }

    private void Start()
    {
        Logger.Log("Entity Init");
        soundManager.PlayBGM("BGM_outside");

        barEntrance.IsAvaliable = GameStateManager.Instance.GameFlow == EGameFlow.CommuteIn;
        homeEntrance.IsAvaliable = GameStateManager.Instance.GameFlow == EGameFlow.CommuteOut;

        
        elevator.SetPosition(GameStateManager.Instance.GameFlow == EGameFlow.CommuteIn);

        if (GameStateManager.Instance.GameFlow == EGameFlow.CommuteIn)
            player.transform.position = homeEntrance.spawnPoint;
        else
            player.transform.position = barEntrance.spawnPoint;



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

            if (flowData.Count == 0) isSpawn = false;

            if (isSpawn)
            {
                entity.entity.IsAvaliable = true;
                entity.entity.EntityLabel = entity.data.dayData.InteractData.Label;
                entity.entity.InjectDialogue(flowData, targetSelection);
                entity.entity.gameObject.SetActive(true);
            }
            else
            {
                entity.entity.IsAvaliable = false;
                entity.entity.gameObject.SetActive(false);
            }            
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
