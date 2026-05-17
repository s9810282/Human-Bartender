using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class PoolPrefab
    {
        public string name;
        public GameObject prefab;
        public Transform uiParent;
        public int initialSize = 10;
    }

    [SerializeField] private List<PoolPrefab> uiPrefabs;


    private Dictionary<string, ObjectPool> uiPools;

    private void Awake()
    {

        uiPools = new Dictionary<string, ObjectPool>();

        foreach (var ui in uiPrefabs)
        {
            if (!uiPools.ContainsKey(ui.name))
            {
                uiPools.Add(ui.name, new ObjectPool(ui.prefab, ui.initialSize, ui.uiParent));
            }
        }
    }


    public GameObject GetUI(string name)
    {
        if (!uiPools.ContainsKey(name))
        {
            Debug.LogWarning($"'{name}' UI를 찾을 수 없습니다.");
            return null;
        }

        GameObject ui = uiPools[name].Get();
        return ui;
    }

    public void ReturnUI(string name, GameObject obj)
    {
        uiPools[name].Return(obj);
    }
}