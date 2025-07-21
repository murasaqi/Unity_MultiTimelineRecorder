using System;
using System.Collections.Generic;
using System.Linq;
using MultiTimelineRecorder.Core.Events;

namespace MultiTimelineRecorder.Tests.Helpers
{
    /// <summary>
    /// Mock implementation of IEventBus for testing
    /// </summary>
    public class MockEventBus : IEventBus
    {
        private readonly List<object> _publishedEvents = new List<object>();
        private readonly Dictionary<Type, List<object>> _subscriptions = new Dictionary<Type, List<object>>();
        
        public void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            var type = typeof(T);
            if (!_subscriptions.ContainsKey(type))
            {
                _subscriptions[type] = new List<object>();
            }
            _subscriptions[type].Add(handler);
        }
        
        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            var type = typeof(T);
            if (_subscriptions.ContainsKey(type))
            {
                _subscriptions[type].Remove(handler);
            }
        }
        
        public void Publish<T>(T @event) where T : IEvent
        {
            _publishedEvents.Add(@event);
            
            var type = typeof(T);
            if (_subscriptions.ContainsKey(type))
            {
                foreach (var handler in _subscriptions[type].ToList())
                {
                    ((Action<T>)handler)(@event);
                }
            }
        }
        
        /// <summary>
        /// Checks if an event of type T was published
        /// </summary>
        public bool WasPublished<T>() where T : IEvent
        {
            return _publishedEvents.Any(e => e is T);
        }
        
        /// <summary>
        /// Gets all published events of type T
        /// </summary>
        public List<T> GetPublishedEvents<T>() where T : IEvent
        {
            return _publishedEvents.OfType<T>().ToList();
        }
        
        /// <summary>
        /// Gets the count of published events of type T
        /// </summary>
        public int GetPublishedCount<T>() where T : IEvent
        {
            return _publishedEvents.Count(e => e is T);
        }
        
        /// <summary>
        /// Clears all published events and subscriptions
        /// </summary>
        public void Clear()
        {
            _publishedEvents.Clear();
            _subscriptions.Clear();
        }
        
        /// <summary>
        /// Disposes the event bus
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}