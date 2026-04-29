using UnityEngine;

public class PlayerInteractor : MonoBehaviour, IInteractor
{
    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
}