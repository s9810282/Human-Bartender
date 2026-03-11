using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private GameObject prefab;
    private Queue<GameObject> pool = new Queue<GameObject>();
    private Transform parentTransform;

    public ObjectPool(GameObject prefab, int initialSize, Transform parent)
    {
        this.prefab = prefab;
        this.parentTransform = parent;
        for (int i = 0; i < initialSize; i++)
        {
            CreateAndReturn();
        }
    }

    public GameObject Get()
    {
        if (pool.Count == 0)
        {
            CreateAndReturn();
        }

        GameObject obj = pool.Dequeue();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }

    private void CreateAndReturn()
    {
        GameObject newObj = Object.Instantiate(prefab, parentTransform);
        newObj.GetComponent<PooledObject>()?.SetPool(this);
        newObj.SetActive(false);
        pool.Enqueue(newObj);
    }
}