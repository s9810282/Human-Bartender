using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// Play 씬 1부: 타이쿤형 영업 국면. 하루치 손님 수가 모두 소진되면 완료된다.
/// TODO: customerCount를 day 데이터(day{N}.json 등)에서 로드하도록 교체.
/// </summary>
public class TycoonFlow : MonoBehaviour, IPlayPhaseFlow
{
    [SerializeField] int customerCount = 5;

    [SerializeField] GuestManager guestManager;

    int remainingCustomers;
    UniTaskCompletionSource completionSource;

    public async UniTask RunAsync()
    {
        remainingCustomers = customerCount;
        completionSource = new UniTaskCompletionSource();

        guestManager.RunSpawnLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();

        await completionSource.Task;
    }

    /// <summary>손님 한 명의 응대(또는 이탈)가 끝났을 때 호출한다. 남은 손님이 0이 되면 1부를 종료한다.</summary>
    public void OnCustomerHandled()
    {
        remainingCustomers--;

        if (remainingCustomers <= 0)
        {
            completionSource.TrySetResult();
        }
    }
}
