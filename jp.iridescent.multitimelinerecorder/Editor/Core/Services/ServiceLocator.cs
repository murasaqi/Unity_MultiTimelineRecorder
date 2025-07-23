using System;
using System.Collections.Generic;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Controllers;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Simple service locator for dependency management
    /// </summary>
    public class ServiceLocator : IDisposable
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private bool _isInitialized = false;

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceLocator();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton
        /// </summary>
        private ServiceLocator()
        {
        }

        /// <summary>
        /// Initializes the service locator with all required services
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            // Create core services
            var logger = new UnityConsoleLogger();
            var eventBus = new EventBus();
            var notificationService = new EditorNotificationService(logger);
            var errorHandler = new ErrorHandlingService(logger, notificationService);
            
            // Register core services
            Register<ILogger>(logger);
            Register<IEventBus>(eventBus);
            Register<INotificationService>(notificationService);
            Register<IErrorHandlingService>(errorHandler);
            
            // Create business services
            var timelineService = new TimelineService(logger);
            var configurationService = new ConfigurationService(logger);
            var recordingService = new RecordingService(logger, errorHandler);
            
            // Create wildcard services
            var wildcardRegistry = new WildcardRegistry();
            var wildcardProcessor = new EnhancedWildcardProcessor(logger, wildcardRegistry);
            
            // Create reference services
            var gameObjectReferenceService = new GameObjectReferenceService(logger, errorHandler);
            
            // Register business services
            Register<ITimelineService>(timelineService);
            Register<IConfigurationService>(configurationService);
            Register<IRecordingService>(recordingService);
            Register<WildcardRegistry>(wildcardRegistry);
            Register<IWildcardProcessor>(wildcardProcessor);
            Register<IGameObjectReferenceService>(gameObjectReferenceService);
            
            // Create UI controllers
            var mainController = new MainWindowController(
                recordingService,
                timelineService,
                configurationService,
                logger,
                errorHandler,
                eventBus);
                
            var recorderController = new RecorderConfigurationController(
                logger,
                errorHandler,
                eventBus);
            
            // Register controllers
            Register<MainWindowController>(mainController);
            Register<RecorderConfigurationController>(recorderController);
            
            _isInitialized = true;
        }

        /// <summary>
        /// Registers a service
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type.Name} is already registered");
            }

            _services[type] = service;
        }

        /// <summary>
        /// Gets a service
        /// </summary>
        public T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            throw new InvalidOperationException($"Service of type {type.Name} is not registered");
        }

        /// <summary>
        /// Tries to get a service
        /// </summary>
        public bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var serviceObj))
            {
                service = serviceObj as T;
                return service != null;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Resets the service locator
        /// </summary>
        public void Reset()
        {
            Dispose();
            _services.Clear();
            _isInitialized = false;
        }

        /// <summary>
        /// Disposes all disposable services
        /// </summary>
        public void Dispose()
        {
            // Dispose controllers first
            DisposeService<RecorderConfigurationController>();
            DisposeService<MainWindowController>();
            
            // Then dispose services
            DisposeService<IGameObjectReferenceService>();
            DisposeService<IWildcardProcessor>();
            DisposeService<WildcardRegistry>();
            DisposeService<IRecordingService>();
            DisposeService<IConfigurationService>();
            DisposeService<ITimelineService>();
            DisposeService<IErrorHandlingService>();
            DisposeService<INotificationService>();
            DisposeService<IEventBus>();
            DisposeService<ILogger>();
            
            _services.Clear();
        }

        /// <summary>
        /// Disposes a specific service if it implements IDisposable
        /// </summary>
        private void DisposeService<T>() where T : class
        {
            if (TryGet<T>(out var service) && service is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error disposing service {typeof(T).Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Static method to reset the singleton instance
        /// </summary>
        public static void ResetInstance()
        {
            _instance?.Dispose();
            _instance = null;
        }
    }

    /// <summary>
    /// Extension methods for ServiceLocator
    /// </summary>
    public static class ServiceLocatorExtensions
    {
        /// <summary>
        /// Gets the logger service
        /// </summary>
        public static ILogger GetLogger(this ServiceLocator locator)
        {
            return locator.Get<ILogger>();
        }

        /// <summary>
        /// Gets the event bus service
        /// </summary>
        public static IEventBus GetEventBus(this ServiceLocator locator)
        {
            return locator.Get<IEventBus>();
        }

        /// <summary>
        /// Gets the error handler service
        /// </summary>
        public static IErrorHandlingService GetErrorHandler(this ServiceLocator locator)
        {
            return locator.Get<IErrorHandlingService>();
        }

        /// <summary>
        /// Gets the main window controller
        /// </summary>
        public static MainWindowController GetMainController(this ServiceLocator locator)
        {
            return locator.Get<MainWindowController>();
        }

        /// <summary>
        /// Gets the recorder configuration controller
        /// </summary>
        public static RecorderConfigurationController GetRecorderController(this ServiceLocator locator)
        {
            return locator.Get<RecorderConfigurationController>();
        }
    }
}