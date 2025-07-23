using System;
using System.Collections.Generic;
using System.Linq;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Events
{

    /// <summary>
    /// Simple event bus for decoupled communication between components
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        /// <summary>
        /// Subscribe to an event
        /// </summary>
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers = new List<Delegate>();
                    _handlers[eventType] = handlers;
                }

                handlers.Add(handler);
            }
        }

        /// <summary>
        /// Unsubscribe from an event
        /// </summary>
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (_handlers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                    
                    if (handlers.Count == 0)
                    {
                        _handlers.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        public void Publish<TEvent>(TEvent eventArgs) where TEvent : EventArgs
        {
            if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));

            List<Delegate> handlersToInvoke;
            
            lock (_lock)
            {
                var eventType = typeof(TEvent);
                if (!_handlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
                {
                    return;
                }

                // Create a copy to avoid collection modification during iteration
                handlersToInvoke = handlers.ToList();
            }

            // Invoke handlers outside of lock to prevent deadlocks
            foreach (var handler in handlersToInvoke)
            {
                try
                {
                    (handler as Action<TEvent>)?.Invoke(eventArgs);
                }
                catch (Exception ex)
                {
                    // Log exception but don't propagate to other handlers
                    UnityEngine.Debug.LogError($"EventBus handler exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all event subscriptions
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
        }

        /// <summary>
        /// Clear subscriptions for a specific event type
        /// </summary>
        public void Clear<TEvent>() where TEvent : EventArgs
        {
            lock (_lock)
            {
                _handlers.Remove(typeof(TEvent));
            }
        }

        /// <summary>
        /// Get the number of subscribers for an event type
        /// </summary>
        public int GetSubscriberCount<TEvent>() where TEvent : EventArgs
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    return handlers.Count;
                }
                return 0;
            }
        }
    }

    /// <summary>
    /// Interface for event bus
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs;
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs;
        void Publish<TEvent>(TEvent eventArgs) where TEvent : EventArgs;
        void Clear();
        void Clear<TEvent>() where TEvent : EventArgs;
    }
}