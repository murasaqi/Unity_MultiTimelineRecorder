using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Services;

namespace MultiTimelineRecorder.Tests.Helpers
{
    /// <summary>
    /// Base test fixture with common setup and teardown
    /// </summary>
    public abstract class TestFixtureBase
    {
        protected ServiceLocator ServiceLocator { get; private set; }
        protected MockEventBus EventBus { get; private set; }
        protected MockLogger Logger { get; private set; }
        protected MockNotificationService NotificationService { get; private set; }
        protected IErrorHandlingService ErrorHandler { get; private set; }
        
        [SetUp]
        public virtual void SetUp()
        {
            // Reset service locator
            ServiceLocator.ResetInstance();
            ServiceLocator = ServiceLocator.Instance;
            
            // Create mock services
            EventBus = new MockEventBus();
            Logger = new MockLogger();
            NotificationService = new MockNotificationService();
            ErrorHandler = new ErrorHandlingService(Logger, NotificationService);
            
            // Register mock services
            ServiceLocator.Register<IEventBus>(EventBus);
            ServiceLocator.Register<ILogger>(Logger);
            ServiceLocator.Register<INotificationService>(NotificationService);
            ServiceLocator.Register<IErrorHandlingService>(ErrorHandler);
        }
        
        [TearDown]
        public virtual void TearDown()
        {
            // Clean up
            ServiceLocator.ResetInstance();
            EventBus.Clear();
            Logger.Clear();
        }
        
        /// <summary>
        /// Helper method to create a test GameObject
        /// </summary>
        protected GameObject CreateTestGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            return go;
        }
        
        /// <summary>
        /// Helper method to assert an event was published
        /// </summary>
        protected void AssertEventPublished<T>() where T : IEvent
        {
            Assert.IsTrue(EventBus.WasPublished<T>(), 
                $"Expected event {typeof(T).Name} was not published");
        }
        
        /// <summary>
        /// Helper method to assert an event was not published
        /// </summary>
        protected void AssertEventNotPublished<T>() where T : IEvent
        {
            Assert.IsFalse(EventBus.WasPublished<T>(), 
                $"Unexpected event {typeof(T).Name} was published");
        }
        
        /// <summary>
        /// Helper method to assert a log message was recorded
        /// </summary>
        protected void AssertLogContains(string message, LogLevel level = LogLevel.Info)
        {
            Assert.IsTrue(Logger.ContainsLog(message, level), 
                $"Expected log message '{message}' with level {level} was not found");
        }
    }
}