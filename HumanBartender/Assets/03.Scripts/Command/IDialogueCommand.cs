using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// 대화 트리거 실행 단위 인터페이스.
/// IsSystemSwitch가 true이면 트리거 실행 중 대화 화면을 숨긴다.
/// ExecuteAsync는 완료 후 다음 대화 id를 반환한다(없으면 빈 문자열).
/// </summary>
public interface IDialogueCommand
{
    public bool IsSystemSwitch { get; set; }
    UniTask<string> ExecuteAsync(CancellationToken ct);
}
