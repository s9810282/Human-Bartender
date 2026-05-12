using UnityEngine;

public class ShakingStrikeNode : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] int currentIndex = 0;
    [SerializeField] Vector3 targetPos;

    int[] targetSequence = { 1, 2, 1, 0, 1 };

    public LineRenderer lr;

    public void Handle()
    {
        if (lr == null) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            currentIndex++;

            if (currentIndex >= targetSequence.Length)
                currentIndex = 0;

            targetPos = lr.GetPosition(targetSequence[currentIndex]);
        }
    }

    
    public void InitToStart(LineRenderer line)
    {
        lr = line;
        targetPos = lr.GetPosition(targetSequence[currentIndex]);
    }
}
