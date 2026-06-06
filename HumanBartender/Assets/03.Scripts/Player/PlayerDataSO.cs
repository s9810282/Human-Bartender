using System.Collections.Generic;
using UnityEngine;

public interface IPlayerDataReader
{
    int HasMoney();
    bool HasEnoughMoney(int cost);

    EAffinityTier GetCurCharacterAffinityTier(string id);
    int GetCurCharacterAffinityValue(string id);

    ESkillTier GetSkillTier();
    int GetSkillValue();

    bool CheckFlag(string id);
}

public interface IPlayerDataWriter
{
    void AddMoney(int val);
    bool TrySpend(int cost);

    void SetCharacterAffinityAmount(string id, int val);
    void AddCharacterAffinityAmount(string id, int val);
    void SetCharacterKarmaAmount(string id, int val);
    void AddCharacterKarmaAmount(string id, int val);

    void AddSkillTier(int val);

    void AddFlag(string id, bool value);
}


[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerDataSO : ScriptableObject, IPlayerDataReader, IPlayerDataWriter
{
    [Header("Header")]
    [SerializeField] int money;
    [SerializeField] IntEvent addMoneyEvent;
    [SerializeField] IntEvent setMoneyEvent;

    [Header("Character Tier")]
    [Tooltip("인스펙터 보여주기 용, 내부적으로 Dic 사용")]
    [SerializeField] CharacterTierDataSO characterTierDataSO;
    [SerializeField] List<CharacterTierData> characterTierDatas = new();
    Dictionary<string, CharacterTierData> characterTierDics = new();


    [Header("Skill Tier")]
    [SerializeField] SkillTierDataSO skillTierDataSO;
    [SerializeField] int skillTierAmount = 0;



    [Header("Flag")]
    [SerializeField] Dictionary<string, bool> flagList = new();

    public void Init()
    {
        characterTierDatas = new List<CharacterTierData>();
        characterTierDics = new Dictionary<string, CharacterTierData>();

        flagList = new Dictionary<string, bool>();

        money = 0;
        skillTierAmount = 0;
    }

    #region Money
    public void AddMoney(int val)
    {
        money += val;
        addMoneyEvent?.Raise(val);

        if (money <= 0) money = 0;
    }
    public int HasMoney()
    {
        return money;
    }

    public bool TrySpend(int cost)
    {
        if (HasEnoughMoney(100))
        {
            AddMoney(-100);
            return true;
        }

        return false;
    }
    public bool HasEnoughMoney(int val) { return money >= val; }


    #endregion

    #region CharacterTier

    public void AddNewCharacter(string id, int defaultVal = 0)
    {
        if(!characterTierDics.ContainsKey(id))
        {
            characterTierDics.Add(id, new CharacterTierData(id, defaultVal, defaultVal));
            characterTierDatas.Add(characterTierDics[id]);
        }
    }
    public void AddCharacterAffinityAmount(string id, int val = 0)
    {
        if (!characterTierDics.ContainsKey(id))
        {
            AddNewCharacter(id, val);
        }
        else
        {
            characterTierDics[id].affinityAmount += val;
        }
    }
    public void AddCharacterKarmaAmount(string id, int val = 0)
    {
        if (!characterTierDics.ContainsKey(id))
        {
            AddNewCharacter(id, val);
        }
        else
        {
            characterTierDics[id].karamaAmount += val;
        }
    }
    public void SetCharacterAffinityAmount(string id, int val)
    {
        if (!characterTierDics.ContainsKey(id))
        {
            AddNewCharacter(id, val);
        }
        else
        {
            characterTierDics[id].affinityAmount = val;
        }
    }
    public void SetCharacterKarmaAmount(string id, int val)
    {
        if (!characterTierDics.ContainsKey(id))
        {
            AddNewCharacter(id, val);
        }
        else
        {
            characterTierDics[id].karamaAmount = val;
        }
    }
    public EAffinityTier GetCurCharacterAffinityTier(string id)
    {
        CharacterAffinityData data = characterTierDataSO.characterTiers.Characters[id];

        if(!characterTierDics.ContainsKey(id)) return EAffinityTier.Very_Low;

        int curTierAmount = characterTierDics[id].affinityAmount;

        foreach (var item in data.Affinity)
        {
            if (item.Min > curTierAmount) continue;
            if (item.Max < curTierAmount) continue;

            return item.Tier;
        }

        return EAffinityTier.Very_Low;
    }
    public int GetCurCharacterAffinityValue(string id)
    {
        if (!characterTierDics.ContainsKey(id)) return 0;

        return characterTierDics[id].affinityAmount;
    }

    #endregion


    #region SkillTier

    public void AddSkillTier(int val)
    {
        skillTierAmount += val;
    }
    public ESkillTier GetSkillTier()
    {
        foreach(var item in skillTierDataSO.skillTier.Tiers)
        {
            if (item.Value.Min > skillTierAmount) continue;
            if (item.Value.Max < skillTierAmount) continue;

            return item.Key;
        }

        return ESkillTier.Beginner;
    }

    public int GetSkillValue()
    {
        return skillTierAmount;
    }

    #endregion


    #region Flag

    public void AddFlag(string id, bool value)
    {
        if (flagList.ContainsKey(id))
            flagList[id] = value;
        else
            flagList.Add(id, value);

    }
    public bool CheckFlag(string id)
    {
        if (!flagList.ContainsKey(id)) return false;

        return flagList[id];
    }


    #endregion
}


[System.Serializable]
public class CharacterTierData
{
    public string characterId;
    public int affinityAmount;
    public int karamaAmount;

    public CharacterTierData(string characterId, int affinityAmount, int karamaAmount)
    {
        this.characterId = characterId;
        this.affinityAmount = affinityAmount;
        this.karamaAmount = karamaAmount;
    }
}
