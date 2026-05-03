using UnityEngine;

public interface IEntity
{
    Vector3 Position { get; }
}

public abstract class OutsideEntity : MonoBehaviour, IEntity
{

    public Vector3 Position => transform.position;
}
