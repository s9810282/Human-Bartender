using UnityEngine;
using UnityEngine.UI;

public class UICocktailStyleTextPanel : MonoBehaviour
{
    [SerializeField] UIContentsText uIContentsText;
    [SerializeField] Button panelButton;


    public UIContentsText GetUIContentsText() { return uIContentsText; }
    public Button GetButton()
    {
        return panelButton;
    }
}
