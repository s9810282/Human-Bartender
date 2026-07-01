using UnityEngine;

/// <summary>실외 씬의 모든 엔티티(오브젝트/NPC 등)가 구현하는 최소 공통 인터페이스.</summary>
public interface IEntity
{
    Transform Transform { get;}
    GameObject GameObject { get; }
}

/// <summary>실외 씬 엔티티들의 공통 베이스. Transform/GameObject 접근자만 제공하는 최소 구현.</summary>
public abstract class OutsideEntity : MonoBehaviour, IEntity
{

    public Transform Transform { get => transform;}
    public GameObject GameObject => this.gameObject;
}
