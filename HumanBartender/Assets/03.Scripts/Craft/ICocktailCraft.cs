using Cysharp.Threading.Tasks;

/// <summary>칵테일 제조 미니게임 시작 및 결과 반환을 담당하는 인터페이스.</summary>
public interface ICocktailCraft
{
    UniTask<string> StartCraftAsync(string id);
}
