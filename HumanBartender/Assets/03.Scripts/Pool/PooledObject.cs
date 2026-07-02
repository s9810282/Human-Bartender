using UnityEngine;

/// <summary>
/// 오브젝트 풀에서 관리되는 오브젝트의 기반 클래스.
/// 자신이 속한 ObjectPool 참조를 보유하며 서브클래스에서 반납 시 활용한다.
/// </summary>
public class PooledObject : MonoBehaviour
{
    protected ObjectPool parentPool;

    public void SetPool(ObjectPool pool)
    {
        parentPool = pool;
    }
}
