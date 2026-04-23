using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    private GameObject prefab;
    private Stack<GameObject> pool = new Stack<GameObject>();
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

        GameObject obj = pool.Pop();
        obj.SetActive(true);
        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Push(obj);
    }

    private void CreateAndReturn()
    {
        GameObject newObj = Object.Instantiate(prefab, parentTransform);
        newObj.GetComponent<PooledObject>()?.SetPool(this);
        newObj.SetActive(false);
        pool.Push(newObj);
    }
}