using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "CharacterResourceData", menuName = "Scriptable Objects/CharacterResourceData")]
public class CharacterResourceData : ScriptableObject
{
    public AssetReferenceT<AnimationClip> clip;
}
