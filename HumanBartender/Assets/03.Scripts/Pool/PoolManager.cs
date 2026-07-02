using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이름 기반으로 UI 프리팹 오브젝트 풀을 관리하는 매니저.
/// Awake 시 uiPrefabs 목록으로 ObjectPool을 초기화하고, GetUI/ReturnUI로 오브젝트를 대여/반납한다.
/// </summary>
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