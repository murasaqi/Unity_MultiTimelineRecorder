using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.Tests.Editor
{
    [TestFixture]
    public class GameObjectReferenceServiceTests
    {
        private GameObjectReferenceService _service;
        private TestLogger _logger;
        private TestErrorHandler _errorHandler;
        private Scene _testScene;
        private List<GameObject> _testObjects;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _errorHandler = new TestErrorHandler();
            _service = new GameObjectReferenceService(_logger, _errorHandler);
            _testObjects = new List<GameObject>();
            
            // Create a test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test objects
            foreach (var obj in _testObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
            _testObjects.Clear();
            
            _service?.Dispose();
        }

        [Test]
        public void CreateReference_WithValidGameObject_ReturnsValidReference()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");

            // Act
            var reference = _service.CreateReference(gameObject);

            // Assert
            Assert.IsNotNull(reference);
            Assert.AreEqual(gameObject, reference.GameObject);
        }

        [Test]
        public void CreateReference_WithNullGameObject_ReturnsEmptyReference()
        {
            // Act
            var reference = _service.CreateReference(null);

            // Assert
            Assert.IsNotNull(reference);
            Assert.IsNull(reference.GameObject);
            Assert.IsTrue(_logger.HasWarning("Cannot create reference for null GameObject"));
        }

        [Test]
        public void CreateReference_WithTransform_ReturnsValidReference()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            var transform = gameObject.transform;

            // Act
            var reference = _service.CreateReference(transform);

            // Assert
            Assert.IsNotNull(reference);
            Assert.AreEqual(gameObject, reference.GameObject);
        }

        [Test]
        public void TryRestoreReference_WithValidReference_RestoresGameObject()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            var reference = _service.CreateReference(gameObject);

            // Act
            var result = _service.TryRestoreReference(reference, out var restoredObject);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(gameObject, restoredObject);
        }

        [Test]
        public void TryRestoreReference_WithNullReference_ReturnsFalse()
        {
            // Act
            var result = _service.TryRestoreReference(null, out var restoredObject);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(restoredObject);
            Assert.IsTrue(_logger.HasWarning("Cannot restore null reference"));
        }

        [Test]
        public void TryRestoreReference_WithDestroyedGameObject_ReturnsFalse()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            var reference = _service.CreateReference(gameObject);
            Object.DestroyImmediate(gameObject);
            _testObjects.Remove(gameObject);

            // Act
            var result = _service.TryRestoreReference(reference, out var restoredObject);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(restoredObject);
            Assert.IsTrue(_logger.HasWarning("Failed to restore GameObject reference"));
        }

        [Test]
        public void TryRestoreTransform_WithValidReference_RestoresTransform()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            var reference = _service.CreateReference(gameObject);

            // Act
            var result = _service.TryRestoreTransform(reference, out var restoredTransform);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(gameObject.transform, restoredTransform);
        }

        [Test]
        public void ValidateReference_WithValidReference_DoesNotThrow()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            var reference = _service.CreateReference(gameObject);

            // Act & Assert
            Assert.DoesNotThrow(() => _service.ValidateReference(reference));
        }

        [Test]
        public void ValidateReference_WithNullReference_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ValidateReference(null));
        }

        [Test]
        public void ValidateReference_WithDestroyedGameObject_ThrowsReferenceRestoreException()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            var reference = _service.CreateReference(gameObject);
            Object.DestroyImmediate(gameObject);
            _testObjects.Remove(gameObject);

            // Act & Assert
            Assert.Throws<ReferenceRestoreException>(() => _service.ValidateReference(reference));
        }

        [Test]
        public void ValidateReferences_WithAllValidReferences_DoesNotThrow()
        {
            // Arrange
            var references = new List<GameObjectReference>();
            for (int i = 0; i < 3; i++)
            {
                var gameObject = CreateTestGameObject($"TestObject{i}");
                references.Add(_service.CreateReference(gameObject));
            }

            // Act & Assert
            Assert.DoesNotThrow(() => _service.ValidateReferences(references));
        }

        [Test]
        public void ValidateReferences_WithSomeInvalidReferences_ThrowsReferenceRestoreException()
        {
            // Arrange
            var references = new List<GameObjectReference>();
            
            // Add valid reference
            var validObject = CreateTestGameObject("ValidObject");
            references.Add(_service.CreateReference(validObject));
            
            // Add invalid reference
            var invalidObject = CreateTestGameObject("InvalidObject");
            var invalidRef = _service.CreateReference(invalidObject);
            Object.DestroyImmediate(invalidObject);
            _testObjects.Remove(invalidObject);
            references.Add(invalidRef);

            // Act & Assert
            var ex = Assert.Throws<ReferenceRestoreException>(() => _service.ValidateReferences(references));
            Assert.IsTrue(ex.Message.Contains("Failed to restore 1 GameObject references"));
        }

        [Test]
        public void RefreshAllReferences_UpdatesAllCachedReferences()
        {
            // Arrange
            var gameObjects = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                var obj = CreateTestGameObject($"TestObject{i}");
                gameObjects.Add(obj);
                _service.CreateReference(obj);
            }

            // Act
            _service.RefreshAllReferences();

            // Assert
            Assert.IsTrue(_logger.HasInfo("Reference refresh complete"));
            Assert.IsTrue(_logger.HasInfo("Refreshed: 3"));
        }

        [Test]
        public void ClearCache_RemovesAllCachedReferences()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                var obj = CreateTestGameObject($"TestObject{i}");
                _service.CreateReference(obj);
            }

            // Act
            _service.ClearCache();

            // Assert
            Assert.IsTrue(_logger.HasInfo("GameObject reference cache cleared"));
        }

        [Test]
        public void SceneChange_TriggersReferenceRefresh()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            _service.CreateReference(gameObject);
            
            // Act - Simulate scene change
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            
            // Wait for delayed call
            EditorApplication.delayCall?.Invoke();
            
            // Assert
            Assert.IsTrue(_logger.HasInfo("Scene opened"));
        }

        [Test]
        public void HierarchicalReference_PreservesHierarchy()
        {
            // Arrange
            var parent = CreateTestGameObject("Parent");
            var child = CreateTestGameObject("Child");
            child.transform.SetParent(parent.transform);
            
            var parentRef = _service.CreateReference(parent);
            var childRef = _service.CreateReference(child);

            // Act
            _service.TryRestoreReference(parentRef, out var restoredParent);
            _service.TryRestoreReference(childRef, out var restoredChild);

            // Assert
            Assert.AreEqual(restoredParent.transform, restoredChild.transform.parent);
        }

        [Test]
        public void ReferenceWithComponents_PreservesComponents()
        {
            // Arrange
            var gameObject = CreateTestGameObject("TestObject");
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<BoxCollider>();
            
            var reference = _service.CreateReference(gameObject);

            // Act
            _service.TryRestoreReference(reference, out var restoredObject);

            // Assert
            Assert.IsNotNull(restoredObject.GetComponent<MeshRenderer>());
            Assert.IsNotNull(restoredObject.GetComponent<BoxCollider>());
        }

        // Helper methods

        private GameObject CreateTestGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _testObjects.Add(gameObject);
            return gameObject;
        }

        // Test implementations

        private class TestLogger : ILogger
        {
            private readonly List<string> _logs = new List<string>();

            public void Log(string message, LogLevel level, LogCategory category)
            {
                _logs.Add($"[{level}] {message}");
            }

            public void LogVerbose(string message, LogCategory category) => Log(message, LogLevel.Verbose, category);
            public void LogInfo(string message, LogCategory category) => Log(message, LogLevel.Info, category);
            public void LogWarning(string message, LogCategory category) => Log(message, LogLevel.Warning, category);
            public void LogError(string message, LogCategory category) => Log(message, LogLevel.Error, category);

            public bool HasWarning(string message) => _logs.Contains($"[{LogLevel.Warning}] {message}");
            public bool HasInfo(string message) => _logs.Any(log => log.Contains(message) && log.StartsWith($"[{LogLevel.Info}]"));
        }

        private class TestErrorHandler : IErrorHandlingService
        {
            public void HandleError(System.Exception exception, ErrorSeverity severity = ErrorSeverity.Error, string context = null)
            {
                // Test implementation
            }
        }
    }
}