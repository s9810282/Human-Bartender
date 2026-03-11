using UnityEngine;

public class PooledObject : MonoBehaviour
{
    protected ObjectPool parentPool;

    public void SetPool(ObjectPool pool)
    {
        parentPool = pool;
    }
}
