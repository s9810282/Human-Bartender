using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

public class UIContentsSpacer : MonoBehaviour
{
    [SerializeField] LayoutElement element;

    public void SetElementWidth(float v)
    {
        element.minWidth = v;
    }

    public void SetElementHeight(float v)
    {
        element.minHeight = v;
    }
}
