using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CharacterData
{
    public string id;
    public string display_name;

    public string name_color;

    public string[] expressions;
    public bool is_player;
}
