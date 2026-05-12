using UnityEngine;

public class CategoryNode : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Color curColor;

    public string Category;

    public void SetNodeColor(Color color)
    {
        curColor = color;
        spriteRenderer.color = curColor;
    }
}