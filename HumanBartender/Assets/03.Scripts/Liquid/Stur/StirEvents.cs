using UnityEngine;
using System.Collections.Generic;

namespace LiquidSimulation
{
    // ================================================================
    // SO 기반 이벤트 채널
    //
    // 사용법:
    //   1. Project 우클릭 → Create → Liquid Simulation → Stir Event 로 에셋 생성
    //   2. 발행 측: stirEvent.Raise()  또는 stirEvent.Raise(data)
    //   3. 구독 측: stirEvent.Register(listener) / Unregister(listener)
    //   4. 또는 GameEventListener 컴포넌트 사용
    // ================================================================

    /// <summary>
    /// 인자 없는 이벤트 채널 (완성, 페널티 등)
    /// </summary>
    [CreateAssetMenu(menuName = "Liquid Simulation/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> listeners = new List<GameEventListener>();

        public void Raise()
        {
            // 역순 순회: 리스너가 Raise 중 제거될 수 있으므로
            for (int i = listeners.Count - 1; i >= 0; i--)
                listeners[i].OnEventRaised();
        }

        public void Register(GameEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void Unregister(GameEventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    /// <summary>
    /// int 인자 이벤트 채널 (휘젓기 횟수 등)
    /// </summary>
    [CreateAssetMenu(menuName = "Liquid Simulation/Events/Int Event")]
    public class IntEvent : ScriptableObject
    {
        private readonly List<IntEventListener> listeners = new List<IntEventListener>();

        public void Raise(int value)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
                listeners[i].OnEventRaised(value);
        }

        public void Register(IntEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.Add(listener);
        }

        public void Unregister(IntEventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    // ================================================================
    // 리스너 컴포넌트 (인스펙터에서 UnityEvent 연결용)
    // ================================================================

    /// <summary>
    /// GameEvent 구독 → UnityEvent로 전달
    /// 
    /// 사용법:
    /// 1. 아무 오브젝트에 추가
    /// 2. Event에 SO 에셋 연결
    /// 3. Response()에 호출할 함수 연결
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent gameEvent;
        [SerializeField] private UnityEngine.Events.UnityEvent response;

        private void OnEnable() { if (gameEvent != null) gameEvent.Register(this); }
        private void OnDisable() { if (gameEvent != null) gameEvent.Unregister(this); }
        public void OnEventRaised() { response?.Invoke(); }
    }

    /// <summary>
    /// IntEvent 구독 → UnityEvent(int)로 전달
    /// </summary>
    public class IntEventListener : MonoBehaviour
    {
        [SerializeField] private IntEvent intEvent;
        [SerializeField] private UnityEngine.Events.UnityEvent<int> response;

        private void OnEnable() { if (intEvent != null) intEvent.Register(this); }
        private void OnDisable() { if (intEvent != null) intEvent.Unregister(this); }
        public void OnEventRaised(int value) { response?.Invoke(value); }
    }
}
