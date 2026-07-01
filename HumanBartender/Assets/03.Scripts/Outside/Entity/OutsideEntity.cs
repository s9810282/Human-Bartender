using UnityEngine;

public interface IEntity
{
    Transform Transform { get;}
    GameObject GameObject { get; }
}

public abstract class OutsideEntity : MonoBehaviour, IEntity
{

    public Transform Transform { get => transform;}
    public GameObject GameObject => this.gameObject;
}
