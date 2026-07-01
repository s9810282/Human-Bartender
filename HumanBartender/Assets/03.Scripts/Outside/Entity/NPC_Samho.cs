using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;



/// <summary>NPC "삼호" 개별 엔티티.</summary>
public class NPC_Samho : InteractiveNPCEntity
{
    /// <summary>컷씬 중 특정 flow의 대사를 재생하기 위한 훅으로 보이나 현재 미구현.</summary>
    public async UniTask PlayDialogueForCutscene(int flowIndex, CancellationToken token = default)
    {

    }
}
