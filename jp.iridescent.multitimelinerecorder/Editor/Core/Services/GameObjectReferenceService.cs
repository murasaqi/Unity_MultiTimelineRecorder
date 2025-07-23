using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MultiTimelineRecorder.Core.Interfaces;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service for managing GameObject references across scene changes and serialization
    /// </summary>
    public class GameObjectReferenceService : IGameObjectReferenceService
    {
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private readonly IErrorHandlingService _errorHandler;
        private readonly Dictionary<string, GameObjectReference> _referenceCache = new Dictionary<string, GameObjectReference>();
        private bool _isSceneChanging = false;

        public GameObjectReferenceService(MultiTimelineRecorder.Core.Interfaces.ILogger logger, IErrorHandlingService errorHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            
            // Subscribe to scene change events
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        /// <inheritdoc />
        public GameObjectReference CreateReference(GameObject gameObject)
        {
            if (gameObject == null)
            {
                _logger.LogWarning("Cannot create reference for null GameObject", LogCategory.Reference);
                return new GameObjectReference();
            }

            var reference = new GameObjectReference
            {
                GameObject = gameObject
            };

            // Cache the reference
            string key = GetReferenceKey(gameObject);
            _referenceCache[key] = reference;

            _logger.LogVerbose($"Created reference for GameObject: {gameObject.name}", LogCategory.Reference);
            return reference;
        }

        /// <inheritdoc />
        public GameObjectReference CreateReference(Transform transform)
        {
            if (transform == null)
            {
                _logger.LogWarning("Cannot create reference for null Transform", LogCategory.Reference);
                return new GameObjectReference();
            }

            return CreateReference(transform.gameObject);
        }

        /// <inheritdoc />
        public bool TryRestoreReference(GameObjectReference reference, out GameObject gameObject)
        {
            gameObject = null;

            if (reference == null)
            {
                _logger.LogWarning("Cannot restore null reference", LogCategory.Reference);
                return false;
            }

            try
            {
                gameObject = reference.GameObject;
                
                if (gameObject != null)
                {
                    _logger.LogVerbose($"Successfully restored reference to GameObject: {gameObject.name}", LogCategory.Reference);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to restore GameObject reference - object not found", LogCategory.Reference);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring GameObject reference: {ex.Message}", LogCategory.Reference);
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryRestoreTransform(GameObjectReference reference, out Transform transform)
        {
            transform = null;

            if (TryRestoreReference(reference, out GameObject gameObject))
            {
                transform = gameObject.transform;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public void ValidateReference(GameObjectReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            GameObject gameObject = reference.GameObject;
            
            if (gameObject == null)
            {
                throw new ReferenceRestoreException("GameObject reference could not be restored");
            }

            _logger.LogVerbose($"Reference validated successfully for GameObject: {gameObject.name}", LogCategory.Reference);
        }

        /// <inheritdoc />
        public void ValidateReferences(IEnumerable<GameObjectReference> references)
        {
            if (references == null)
            {
                throw new ArgumentNullException(nameof(references));
            }

            var failedReferences = new List<string>();

            foreach (var reference in references)
            {
                try
                {
                    ValidateReference(reference);
                }
                catch (ReferenceRestoreException)
                {
                    failedReferences.Add("Unknown GameObject");
                }
            }

            if (failedReferences.Count > 0)
            {
                throw new ReferenceRestoreException($"Failed to restore {failedReferences.Count} GameObject references");
            }
        }

        /// <inheritdoc />
        public void RefreshAllReferences()
        {
            _logger.LogInfo("Refreshing all GameObject references", LogCategory.Reference);

            var refreshedCount = 0;
            var failedCount = 0;

            foreach (var kvp in _referenceCache.ToList())
            {
                var reference = kvp.Value;
                if (reference != null)
                {
                    // Force refresh by accessing the GameObject property
                    var gameObject = reference.GameObject;
                    if (gameObject != null)
                    {
                        refreshedCount++;
                    }
                    else
                    {
                        failedCount++;
                        _logger.LogWarning($"Failed to refresh reference: {kvp.Key}", LogCategory.Reference);
                    }
                }
            }

            _logger.LogInfo($"Reference refresh complete. Refreshed: {refreshedCount}, Failed: {failedCount}", LogCategory.Reference);
        }

        /// <inheritdoc />
        public void ClearCache()
        {
            _referenceCache.Clear();
            _logger.LogInfo("GameObject reference cache cleared", LogCategory.Reference);
        }

        /// <summary>
        /// Handle scene opened event
        /// </summary>
        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (_isSceneChanging)
            {
                _isSceneChanging = false;
                return;
            }

            _logger.LogInfo($"Scene opened: {scene.name}. Refreshing GameObject references.", LogCategory.Reference);
            
            // Delay refresh to allow scene to fully load
            EditorApplication.delayCall += () =>
            {
                RefreshAllReferences();
            };
        }

        /// <summary>
        /// Handle scene closing event
        /// </summary>
        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            _isSceneChanging = true;
            _logger.LogInfo($"Scene closing: {scene.name}", LogCategory.Reference);
        }

        /// <summary>
        /// Handle scene saved event
        /// </summary>
        private void OnSceneSaved(Scene scene)
        {
            _logger.LogInfo($"Scene saved: {scene.name}. Validating GameObject references.", LogCategory.Reference);
            
            // Validate all cached references after save
            try
            {
                ValidateReferences(_referenceCache.Values);
            }
            catch (ReferenceRestoreException ex)
            {
                _logger.LogWarning($"Some GameObject references may be invalid after save: {ex.Message}", LogCategory.Reference);
            }
        }

        /// <summary>
        /// Get a unique key for a GameObject reference
        /// </summary>
        private string GetReferenceKey(GameObject gameObject)
        {
            if (gameObject == null)
                return "null";

            // Use instance ID for uniqueness within session
            return $"{gameObject.GetInstanceID()}_{gameObject.name}";
        }

        /// <summary>
        /// Dispose of the service and unsubscribe from events
        /// </summary>
        public void Dispose()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            
            ClearCache();
        }
    }

    /// <summary>
    /// Exception thrown when GameObject reference restoration fails
    /// </summary>
    public class ReferenceRestoreException : Exception
    {
        public ReferenceRestoreException(string message) : base(message) { }
        public ReferenceRestoreException(string message, Exception innerException) : base(message, innerException) { }
    }
}