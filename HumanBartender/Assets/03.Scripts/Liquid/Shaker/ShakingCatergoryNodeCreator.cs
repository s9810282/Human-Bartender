using System.Collections.Generic;
using UnityEngine;


public class ShakingCatergoryNodeCreator : MonoBehaviour
{
    [SerializeField] ObjectPool targetNodePool;
    [SerializeField] ObjectPool effectPool;

    [SerializeField] float ratio = 1f;

    [SerializeField] float createTargetDelay;

    [SerializeField] float nodeLifeTime = 2f;

    [SerializeField] int createTargetNodeMinCount;
    [SerializeField] int createTargetNodeMaxCount;

    [SerializeField] Vector3[] targetPostions;
    [SerializeField] Color[] targetColors;
    [SerializeField] PatternData curPatternData;


    bool isStart = false;
    float curTargetNodeTime = 0f;

    
    List<CategoryNode> curActiveTargetNodes = new List<CategoryNode>();

    public void Init()
    {
        curActiveTargetNodes = new();

        effectPool.Init();
        targetNodePool.Init();
    }

    public void Handle()
    {
        if (!isStart) return;
;
        curTargetNodeTime += Time.deltaTime;

        if (curTargetNodeTime >= createTargetDelay * ratio)
        {
            curTargetNodeTime = 0f;
        }

       
        for (int i = curActiveTargetNodes.Count - 1; i >= 0; i--)
        {
            CategoryNode node = curActiveTargetNodes[i];

            if (Time.time - node.spawnTime >= node.lifeTime)
            {
                curActiveTargetNodes.RemoveAt(i);
                //targetNodePool.Return(node.gameObject);
            }
        }
    }

    public void InitToStart(Vector3[] dots, Color[] colors, PatternData patternData)
    {
        isStart = true;

        curPatternData = patternData;
        targetPostions = dots;
        targetColors = colors;
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
            curActiveTargetNodes.Remove(nearest);
            targetNodePool.Return(nearest.gameObject);
            return nearest;
        }


        return null;
    }


    public void CreateEffectNode(Vector3 vec, Color color)
    {
        NodeEffect node = effectPool.Get().GetComponent<NodeEffect>();
        node.transform.position = vec;
        node.SetNodeColor(color);
        node.PlayEffect();
    }


    public void SpawnPatternNode(bool isDown)
    {
        foreach(var item in curActiveTargetNodes)
        {
            targetNodePool.Return(item.gameObject);
        }
        curActiveTargetNodes.Clear();

        for (int i = 0; i < targetPostions.Length - 1; i++)
        {
            Vector3 a = targetPostions[i];
            Vector3 b = targetPostions[i + 1];

            for (int j = 0; j < curPatternData.patternTime.Length;j++)
            {
                float t = isDown ? curPatternData.patternTime[j] : (1 - curPatternData.patternTime[j]);
                SpawnNode(a, b, t);
            }

            SpawnNode(a, b, isDown ? 1 : 0);
        }
    }

    public void SpawnNode(Vector3 a, Vector3 b, float t)
    {
        Vector3 pos = GetSpawnPoint(a, b, t);

        CategoryNode node = targetNodePool.Get().GetComponent<CategoryNode>();

        node.transform.position = pos;
        node.spawnTime = Time.time;
        node.lifeTime = nodeLifeTime;
        node.Category = "target";

        int colorIndex = Random.Range(0, targetColors.Length);
        node.SetNodeColor(targetColors[colorIndex]);

        curActiveTargetNodes.Add(node);
    }


    public Vector3 GetSpawnPoint(Vector3 a, Vector3 b, float t)
    {
        Vector3 pos = Vector3.Lerp(a, b, t);
        return pos;
    }    
}


