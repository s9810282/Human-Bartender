using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public class SettlementSection : MonoBehaviour
{
    [SerializeField] RectTransform sectionRect;

    [Header("Header")]
    [SerializeField] private TMP_Text labelText;     
    [SerializeField] private TMP_Text totalText;    
    [SerializeField] private TMP_Text toggleText;   
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

        if (lines == null) return;

        foreach (var line in lines)
        {
            var row = Instantiate(rowPrefab, rowParent);
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

        bodyRect.gameObject.SetActive(isopen);

        float target = isopen ? headerHeight + spawned.Count * bodyHeight + padding : headerHeight;
        anim = StartCoroutine(AnimationHeight(sectionRect, target, 0.5f));
    }

    public IEnumerator AnimationHeight(RectTransform rect, float target, float duration)
    {
        float start = rect.sizeDelta.y;
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
    }
}