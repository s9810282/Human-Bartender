using System.Collections.Generic;
using UnityEngine;

public interface IPlayerDataReader
{
    bool HasEnoughMoney(int cost);
    EAffinityTier GetCurCharacterTier(string id);
    ESkillTier GetSkillTier();
}

public interface IPlayerDataWriter
{
    void AddMoney(int val);
    bool TrySpend(int cost);
    void SetCharacterTierAmount(string id, int val);
    void AddCharacterTierAmount(string id, int val);
    void AddSkillTier(int val);
}


[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerDataSO : ScriptableObject, IPlayerDataReader, IPlayerDataWriter
{
    [Header("Header")]
    [SerializeField] int money;

    [Header("Character Tier")]
    [Tooltip("인스펙터 보여주기 용, 내부적으로 Dic 사용")]
    [SerializeField] CharacterTierDataSO characterTierDataSO;
    [SerializeField] List<CharacterTierData> characterTierDatas = new();

    [Header("Skill Tier")]
    [SerializeField] SkillTierDataSO skillTierDataSO;
    [SerializeField] int skillTierAmount = 0;

    Dictionary<string, CharacterTierData> characterTierDics = new();


    #region Money
    public void AddMoney(int val)
    {
        money += val;

        if (money <= 0) money = 0;
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
            characterTierDics.Add(id, new CharacterTierData(id, defaultVal));
            characterTierDatas.Add(characterTierDics[id]);
        }
    }
    public void AddCharacterTierAmount(string id, int val)
    {
        if (!characterTierDics.ContainsKey(id))
        {
            AddNewCharacter(id, val);
        }
        else
        {
            characterTierDics[id].tierAmount += val;
        }
    }
    public void SetCharacterTierAmount(string id, int val)
    {
        if (!characterTierDics.ContainsKey(id))
        {
            AddNewCharacter(id, val);
        }
        else
        {
            characterTierDics[id].tierAmount = val;
        }
    }
    public EAffinityTier GetCurCharacterTier(string id)
    {
        CharacterAffinityData data = characterTierDataSO.characterTiers.Characters[id];

        if(!characterTierDics.ContainsKey(id)) return EAffinityTier.Very_Low;

        int curTierAmount = characterTierDics[id].tierAmount;

        foreach (var item in data.Affinity)
        {
            if (item.Min > curTierAmount) continue;
            if (item.Max < curTierAmount) continue;

            return item.Tier;
        }

        return EAffinityTier.Very_Low;
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

    #endregion
}


[System.Serializable]
public class CharacterTierData
{
    public string characterId;
    public int tierAmount;

    public CharacterTierData(string characterId, int tierAmount)
    {
        this.characterId = characterId;
        this.tierAmount = tierAmount;
    }
}
