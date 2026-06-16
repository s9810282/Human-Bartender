using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class SettlementSection : MonoBehaviour
{
    [SerializeField] RectTransform sectionRect;

    [Header("Header")]
    [SerializeField] private TMP_Text labelText;     
    [SerializeField] private TMP_Text totalText;    
    [SerializeField] private TMP_Text toggleText;
    [SerializeField] private Image toggleImage;
    [SerializeField] private Sprite togglePlusSprite;
    [SerializeField] private Sprite toggleMinusSprite;

    [SerializeField] private RectTransform headerRect;

    [Header("Body")]
    [SerializeField] private RectTransform bodyRect;   
    [SerializeField] private Transform rowParent;   
    [SerializeField] private SettlementRow rowPrefab;

    [Header("Size Control")]
    [SerializeField] private float headerHeight = 65;
    [SerializeField] private float bodyHeight = 30;
    [SerializeField] private float padding = 20;

    private readonly List<SettlementRow> spawned = new();
    
    private bool isOpen;

   
   
   

    private Coroutine anim;


    public void Build(string label, IEnumerable<SettlementLine> lines, string totalStr)
    {
        if (labelText != null) labelText.text = label;
        if (totalText != null) totalText.text = totalStr;

        foreach (var r in spawned) if (r != null) Destroy(r.gameObject);
        spawned.Clear();
        bodyRect.sizeDelta = new Vector2(bodyRect.sizeDelta.x, 0f);

        if (lines == null) return;

        foreach (var line in lines)
        {
            var row = Instantiate(rowPrefab, rowParent);

            bodyRect.sizeDelta += new Vector2(0, 30f);

            row.Set(line);
            spawned.Add(row);
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else { Open(); }
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public bool IsOpen => isOpen;

    public void SetOpen(bool isopen)
    {
        if (anim != null) { StopCoroutine(anim); anim = null; }
        
        isOpen = isopen;
        
        if (toggleText != null) toggleText.text = isopen ? "−" : "+";

        toggleImage.sprite = isopen ? toggleMinusSprite : togglePlusSprite;

        if (isOpen)
            bodyRect.gameObject.SetActive(isopen);

        float start = isopen ? 0 : spawned.Count * bodyHeight;
        float target = isopen ? spawned.Count * bodyHeight : 0;

        //float target = isopen ? headerHeight + spawned.Count * bodyHeight + padding : headerHeight;

        anim = StartCoroutine(AnimationHeight(bodyRect, start, target, 0.5f));
    }

    public IEnumerator AnimationHeight(RectTransform rect, float start, float target, float duration)
    {
        
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration); // ease-in-out
            float h = Mathf.Lerp(start, target, k);

            Vector2 size = rect.sizeDelta;
            size.y = h;
            rect.sizeDelta = size;
            yield return null;
        }

        Vector2 end = rect.sizeDelta;
        end.y = target;
        rect.sizeDelta = end;

        if (!isOpen)
            bodyRect.gameObject.SetActive(isOpen);
    }
}