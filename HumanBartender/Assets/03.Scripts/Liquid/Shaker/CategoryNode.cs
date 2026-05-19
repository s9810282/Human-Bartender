using UnityEngine;

public class CategoryNode : PooledObject
{
    [SerializeField] SpriteRenderer outLineSprite;
    [SerializeField] SpriteRenderer centerSprite;


    public Color curColor;
    public float spawnTime = 0;
    public float lifeTime = 0;

    public string Category;

    public void SetNodeColor(Color color)
    {
        curColor = color;
        outLineSprite.color = curColor;
        centerSprite.color = curColor;
    }
}