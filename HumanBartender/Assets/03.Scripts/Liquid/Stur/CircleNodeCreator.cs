using UnityEngine;

public class CircleNodeCreator : MonoBehaviour
{
    [SerializeField] ObjectPool targetNodePool;
    [SerializeField] ObjectPool effectPool;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Init()
    {
        effectPool.Init();
        targetNodePool.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
