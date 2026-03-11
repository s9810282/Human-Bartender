using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New CharacterDataBase", menuName = "Data/CharacterDataBase")]
public class CharacterDataSO : ScriptableObject
{
    public CharacterDataBase characterData;
}

[Serializable]
public class CharacterDataBase
{
    public CharacterData[] characters;
}

