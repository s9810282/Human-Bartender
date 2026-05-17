using Unity.VisualScripting;
using UnityEngine;

public class ShakingStrikeNode : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] int currentIndex = 0;
    [SerializeField] Vector3 targetPos;

    int[] targetSequence = { 1, 0, 1, 2, 1 };
    Vector3[] targetPostions;

    bool isStart = false;


    public void Handle()
    {
        if (!isStart) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            currentIndex++;

            if (currentIndex >= targetSequence.Length)
                currentIndex = 0;

            targetPos = targetPostions[targetSequence[currentIndex]]; 
        }
    }

    public void InitToStart(Vector3[] line)
    {
        currentIndex = 1;
        targetPostions = line;

        targetPos = targetPostions[targetSequence[currentIndex]];

        isStart = true;
    }
}
