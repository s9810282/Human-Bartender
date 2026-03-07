using UnityEngine;
using UnityEngine.Events;

public class GameEventListener<T> : MonoBehaviour
{
    [SerializeField] private GameEvent<T> channel;

    [SerializeField] private UnityEvent<T> response;


    private void OnEnable()
    {
        if(channel == null)
        {
            Logger.Log($"{name} null");
            return;
        }

        channel.RegisterListener(this);
    }
    private void OnDisable() => channel.UnregisterListener(this);
    public void OnEventRaised(T data)
    {
        response.Invoke(data);
    }
}