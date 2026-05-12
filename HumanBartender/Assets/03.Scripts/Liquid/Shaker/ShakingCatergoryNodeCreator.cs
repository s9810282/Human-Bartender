using UnityEngine;

public class ShakingCatergoryNodeCreator : MonoBehaviour
{
    [SerializeField] CategoryNode categoryNodePrefab;

    [SerializeField] float ratio = 1f;
    [SerializeField] float createDelay;

    [SerializeField] int createNodeMinCount;
    [SerializeField] int createNodeMaxCount;

    [SerializeField] int createTargetNodeMinCount;
    [SerializeField] int createTargetNodeMaxCount;

    public LineRenderer lr;
    
    
    void Start()
    {
        
    }

    public void Handle()
    {
        if (lr == null) return;
    }

    public void InitToStart(LineRenderer line)
    {
        lr = line; 
    }

    public void SpawnAt(float t)
    {
        Vector3 pos = GetPointOnLine(t);
        Instantiate(categoryNodePrefab, pos, Quaternion.identity);
    }

    public Vector3 GetPointOnLine(float t)
    {
        int count = lr.positionCount;
        if (count < 2) return lr.GetPosition(0);

        float totalLength = 0f;
        float[] lengths = new float[count - 1];
        for (int i = 0; i < count - 1; i++)
        {
            lengths[i] = Vector3.Distance(lr.GetPosition(i), lr.GetPosition(i + 1));
            totalLength += lengths[i];
        }

        float targetDist = t * totalLength;
        float accumulated = 0f;

        for (int i = 0; i < count - 1; i++)
        {
            if (accumulated + lengths[i] >= targetDist)
            {
                float segmentT = (targetDist - accumulated) / lengths[i];
                Vector3 p = Vector3.Lerp(lr.GetPosition(i), lr.GetPosition(i + 1), segmentT);
                return lr.useWorldSpace ? p : lr.transform.TransformPoint(p);
            }
            accumulated += lengths[i];
        }

        return lr.GetPosition(count - 1);
    }
}


