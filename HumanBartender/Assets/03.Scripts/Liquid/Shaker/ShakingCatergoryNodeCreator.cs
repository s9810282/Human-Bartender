using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;






public class ShakingCatergoryNodeCreator : MonoBehaviour
{
    [SerializeField] CategoryColorData colorData;

    [SerializeField] ObjectPool nodePool;
    [SerializeField] ObjectPool targetNodePool;

    [SerializeField] float ratio = 1f;

    [SerializeField] float createNodeDelay;
    [SerializeField] float createTargetDelay;

    [SerializeField] float nodeLifeTime = 2f;

    [SerializeField] int createNodeMinCount;
    [SerializeField] int createNodeMaxCount;

    [SerializeField] int createTargetNodeMinCount;
    [SerializeField] int createTargetNodeMaxCount;

    bool isStart = false;

    float curNodeTime = 0f;
    float curTargetNodeTime = 0f;

    Vector3 spawnRangeTop;
    Vector3 spawnRangeMiddle;
    Vector3 spawnRangeBottom;

    List<CategoryNode> curActiveNodes = new List<CategoryNode>();
    List<CategoryNode> curActiveTargetNodes = new List<CategoryNode>();

    void Start()
    {
        curActiveNodes = new List<CategoryNode>();

        nodePool.Init();
        targetNodePool.Init();
    }

    public void Handle()
    {
        if (!isStart) return;

        curNodeTime += Time.deltaTime;
        curTargetNodeTime += Time.deltaTime;

        if (curNodeTime >= createNodeDelay * ratio)
        {
            curNodeTime = 0f;
            //CreateNode();
        }

        if (curTargetNodeTime >= createTargetDelay * ratio)
        {
            curTargetNodeTime = 0f;
            CreateTargetNode();
        }

        

        for (int i = curActiveTargetNodes.Count - 1; i >= 0; i--)
        {
            CategoryNode node = curActiveTargetNodes[i];

            if (Time.time - node.spawnTime >= node.lifeTime)
            {
                curActiveTargetNodes.RemoveAt(i);
                targetNodePool.Return(node.gameObject);
            }
        }
    }

    public void InitToStart(Vector3 a, Vector3 b, Vector3 c)
    {
        isStart = true;

        spawnRangeTop = a;
        spawnRangeMiddle = b;
        spawnRangeBottom = c;
    }

    public CategoryNode GetNearestNode(Vector3 pos, float judgeRange)
    {
        CategoryNode nearest = null;
        float minSqr = float.MaxValue;

        for (int i = 0; i < curActiveTargetNodes.Count; i++)
        {
            float sqr = (curActiveTargetNodes[i].transform.position - pos).sqrMagnitude;
            if (sqr < minSqr)
            {
                minSqr = sqr;
                nearest = curActiveTargetNodes[i];
            }
        }

        if (nearest != null && minSqr <= judgeRange * judgeRange)
        {
            //Judge(nearest);
            curActiveTargetNodes.Remove(nearest);
            targetNodePool.Return(nearest.gameObject);
            return nearest;
        }


        return null;
    }


    public void CreateNode()
    {
        int count = Random.Range(createNodeMinCount, createNodeMaxCount);

        for(int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, 2);
            RandomSpawnCategoryNode(rand);
        }
    }

    public void CreateTargetNode()
    {
        int count = Random.Range(createTargetNodeMinCount, createTargetNodeMaxCount);

        for (int i = 0; i < count; i++)
        {
            int rand = Random.Range(0, 2);
            RandomSpawnCategoryTargetNode(rand);
        }
    }


    public void RandomSpawnCategoryNode(int line)
    {
        Vector3 a = spawnRangeMiddle;
        Vector3 b = line == 0 ? spawnRangeTop : spawnRangeBottom;

        float t = Random.Range(0.1f, 0.9f);

        Vector3 pos = GetSpawnPoint(a, b, t);

        CategoryNode node = nodePool.Get().GetComponent<CategoryNode>();

        node.transform.position = pos;
        node.spawnTime = Time.time;
        node.lifeTime = nodeLifeTime;
        node.Category = "ad";

        curActiveNodes.Add(node);
    }

    public void RandomSpawnCategoryTargetNode(int line)
    {
        Vector3 a = spawnRangeMiddle;
        Vector3 b = line == 0 ? spawnRangeTop : spawnRangeBottom;

        float t = Random.value;

        Vector3 pos = GetSpawnPoint(a, b, t);

        CategoryNode node = targetNodePool.Get().GetComponent<CategoryNode>();

        node.transform.position = pos;
        node.spawnTime = Time.time;
        node.lifeTime = nodeLifeTime;
        node.Category = "target";

        curActiveTargetNodes.Add(node);
    }


    public Vector3 GetSpawnPoint(Vector3 a, Vector3 b, float t)
    {
        Vector3 pos = Vector3.Lerp(a, b, t);
        return pos;
    }    
}


