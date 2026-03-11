using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class PoolPrefab
    {
        public string name;
        public GameObject prefab;
        public int initialSize = 10;
    }

    [Header("Effect")]
    [SerializeField] private List<PoolPrefab> effectPrefabs;

    [Header("UI")]
    [SerializeField] private List<PoolPrefab> uiPrefabs;
    [SerializeField] private GameObject uiParent;
    [SerializeField] private GameObject uiBossParent;

    private Dictionary<string, ObjectPool> effectPools;
    private Dictionary<string, ObjectPool> uiPools;

    private void Awake()
    {
        effectPools = new Dictionary<string, ObjectPool>();
        foreach (var ef in effectPrefabs)
        {
            if (!effectPools.ContainsKey(ef.name))
            {
                var poolParent = new GameObject($"{ef.name} Pool").transform;
                poolParent.SetParent(this.transform);
                effectPools[ef.name] = new ObjectPool(ef.prefab, ef.initialSize, poolParent);
            }
        }


        uiPools = new Dictionary<string, ObjectPool>();

        foreach (var ef in uiPrefabs)
        {
            if (!uiPools.ContainsKey(ef.name))
            {
                if(ef.name.Contains("Boss"))
                    uiPools[ef.name] = new ObjectPool(ef.prefab, ef.initialSize, uiBossParent.transform);
                else
                    uiPools[ef.name] = new ObjectPool(ef.prefab, ef.initialSize, uiParent.transform);
            }
        }
    }

    /// <summary>
    /// 지정된 이름의 이펙트를 특정 위치와 회전으로 재생합니다.
    /// </summary>
    public void PlayEffect(string name, Vector3 position, Quaternion rotation)
    {
        if (!effectPools.ContainsKey(name))
        {
            Debug.LogWarning($"'{name}' 이펙트를 찾을 수 없습니다.");
            return;
        }

        GameObject effect = effectPools[name].Get();
        effect.transform.position = position;
        effect.transform.rotation = rotation;
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
}