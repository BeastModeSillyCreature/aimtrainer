using System;
using UnityEngine;

// Канал событий на ScriptableObject (паттерн "Наблюдатель", ЛР №5).
// Издатель и подписчики знают только об этом ассете, а не друг о друге.
// В редакторе в Inspector ассета видно, сколько раз событие сработало и с каким значением.
public abstract class GameEvent<T> : ScriptableObject
{
    private Action<T> _listeners;

#if UNITY_EDITOR
    [SerializeField] private int raiseCount;
    [SerializeField, TextArea] private string lastValue;
#endif

    public void Subscribe(Action<T> listener)
    {
        _listeners += listener;
    }

    public void Unsubscribe(Action<T> listener)
    {
        _listeners -= listener;
    }

    public void Raise(T value)
    {
#if UNITY_EDITOR
        raiseCount++;
        lastValue = value != null ? value.ToString() : "null";
#endif
        _listeners?.Invoke(value);
    }
}
