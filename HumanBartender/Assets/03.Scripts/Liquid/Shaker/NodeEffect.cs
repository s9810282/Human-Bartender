using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class NodeEffect : PooledObject
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Vector3 effectfromSize;
    [SerializeField] Vector3 effectToSize;
    [SerializeField] Ease ease;

    void Start()
    {
        
    }
     
    public void SetNodeColor(Color color)
    {
        sprite.color = color;
    }
    public void PlayEffect()
    {
        sprite.transform.localScale = effectfromSize;
        ActiveEffect().Forget();
    }

    public async UniTaskVoid ActiveEffect()
    {
        float duraion = 0.5f;
        float t = 0f;

        Vector3 fromSize = sprite.transform.localScale;
        Vector3 toSize = effectToSize;

        while (t < duraion)
        {
            t += Time.deltaTime;
            float k = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01(t / duraion), ease);

            sprite.transform.localScale = Vector3.Lerp(fromSize, toSize, k);
            
            await UniTask.Yield();
        }

        parentPool.Return(this.gameObject);
        return;
    }
}
