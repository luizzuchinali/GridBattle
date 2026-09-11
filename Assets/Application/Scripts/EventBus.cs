using System;
using System.Collections.Generic;

namespace GridBattle
{
    public class EventBus
    {
        private static EventBus _instance;
        private static readonly object InstanceLock = new();

        private static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        _instance ??= new EventBus();
                    }
                }

                return _instance;
            }
        }

        private readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();
        private readonly object _lock = new object();

        private EventBus()
        {
        }

        public static void Subscribe<T>(Action<T> listener)
        {
            if (listener == null) return;

            lock (Instance._lock)
            {
                var type = typeof(T);
                if (Instance._events.TryGetValue(type, out var existingDelegate))
                {
                    Instance._events[type] = Delegate.Combine(existingDelegate, listener);
                }
                else
                {
                    Instance._events[type] = listener;
                }
            }
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            if (listener == null) return;

            lock (Instance._lock)
            {
                var type = typeof(T);
                if (Instance._events.TryGetValue(type, out var existingDelegate))
                {
                    var newDelegate = Delegate.Remove(existingDelegate, listener);
                    if (newDelegate == null)
                    {
                        Instance._events.Remove(type);
                    }
                    else
                    {
                        Instance._events[type] = newDelegate;
                    }
                }
            }
        }

        public static void Raise<T>(T eventData)
        {
            Delegate callbacks;
            lock (Instance._lock)
            {
                if (!Instance._events.TryGetValue(typeof(T), out callbacks))
                {
                    return;
                }
            }

            (callbacks as Action<T>)?.Invoke(eventData);
        }

        public static void Raise<T>() where T : new()
        {
            Raise(new T());
        }
    }
}