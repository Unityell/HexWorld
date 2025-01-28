using System;
using System.Collections.Generic;
using System.Linq;

public class EventBus
{
    public Action Update;

    private readonly Dictionary<object, HashSet<Delegate>> EventHandlers = new Dictionary<object, HashSet<Delegate>>();
    private object GlobalKey = typeof(object);

    public void Subscribe<T>(Action<T> Listener)
    {
        if (Listener == null) return;

        var Key = typeof(T);
        if (!EventHandlers.ContainsKey(Key))
        {
            EventHandlers[Key] = new HashSet<Delegate>();
        }

        if (!EventHandlers[Key].Contains(Listener))
        {
            EventHandlers[Key].Add(Listener);
        }
    }

    public void SubscribeToAll<T>(Action<T> Listener)
    {
        if (Listener == null) return;

        if (!EventHandlers.ContainsKey(GlobalKey))
        {
            EventHandlers[GlobalKey] = new HashSet<Delegate>();
        }

        if (!EventHandlers[GlobalKey].Contains(Listener))
        {
            EventHandlers[GlobalKey].Add(Listener);
        }
    }

    public void Unsubscribe<T>(Action<T> Listener)
    {
        if (Listener == null) return;

        var Key = typeof(T);
        if (EventHandlers.ContainsKey(Key))
        {
            EventHandlers[Key].RemoveWhere(x => x == null || !ReferenceEquals(x.Target, Listener.Target));
            if (EventHandlers[Key].Count == 0)
            {
                EventHandlers.Remove(Key);
            }
        }
    }

    public void UnsubscribeFromAll<T>(Action<T> Listener)
    {
        if (Listener == null) return;

        foreach (var Key in EventHandlers.Keys.ToHashSet())
        {
            EventHandlers[Key] = EventHandlers[Key]
                .Where(x => x == null || !ReferenceEquals(x.Target, Listener.Target))
                .ToHashSet();
        }
    }

    public void Invoke<T>(T Payload)
    {
        var Key = typeof(T);

        if (EventHandlers.ContainsKey(Key))
        {
            EventHandlers[Key].RemoveWhere(x => x == null || x.Target == null);

            var ListenersCopy = new HashSet<Delegate>(EventHandlers[Key]);

            foreach (var Listener in ListenersCopy)
            {
                if (Listener is Action<T> TypedListener)
                {
                    TypedListener.Invoke(Payload);
                }
            }
        }

        if (EventHandlers.ContainsKey(GlobalKey))
        {
            EventHandlers[GlobalKey].RemoveWhere(x => x == null || x.Target == null);

            var GlobalListenersCopy = new HashSet<Delegate>(EventHandlers[GlobalKey]);

            foreach (var Listener in GlobalListenersCopy)
            {
                if (Listener is Action<T> TypedListener)
                {
                    TypedListener.Invoke(Payload);
                }
            }
        }

        Update?.Invoke();
    }

    public Dictionary<object, int> GetHandlersData()
    {
        return EventHandlers.ToDictionary(entry => entry.Key, entry => entry.Value.Count);
    }
}