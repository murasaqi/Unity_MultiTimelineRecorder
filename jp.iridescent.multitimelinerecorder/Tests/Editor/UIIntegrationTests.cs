using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.UI;

namespace MultiTimelineRecorder.Tests.Editor
{
    [TestFixture]
    public class UIIntegrationTests
    {
        private WildcardManagementWindow _wildcardWindow;
        private ConfigurationManagementWindow _configWindow;
        private WildcardManagementSettings _settings;
        private TestLogger _logger;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            
            // Clean up any existing windows
            CloseAllTestWindows();
            
            // Create settings
            _settings = ScriptableObject.CreateInstance<WildcardManagementSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            CloseAllTestWindows();
            
            if (_settings != null)
                ScriptableObject.DestroyImmediate(_settings);
        }

        [Test]
        public void WildcardManagementWindow_CanOpenAndClose()
        {
            // Act
            _wildcardWindow = EditorWindow.GetWindow<WildcardManagementWindow>();
            
            // Assert
            Assert.IsNotNull(_wildcardWindow);
            Assert.IsTrue(_wildcardWindow.titleContent.text.Contains("Wildcard"));
            
            // Close
            _wildcardWindow.Close();
        }

        [Test]
        public void ConfigurationManagementWindow_CanOpenAndClose()
        {
            // Act
            _configWindow = EditorWindow.GetWindow<ConfigurationManagementWindow>();
            
            // Assert
            Assert.IsNotNull(_configWindow);
            Assert.IsTrue(_configWindow.titleContent.text.Contains("Configuration"));
            
            // Close
            _configWindow.Close();
        }

        [Test]
        public void WildcardManagementWindow_CanAddCustomWildcard()
        {
            // Arrange
            _wildcardWindow = EditorWindow.GetWindow<WildcardManagementWindow>();
            var windowType = typeof(WildcardManagementWindow);
            
            // Set private fields via reflection
            SetPrivateField(_wildcardWindow, "_settings", _settings);
            SetPrivateField(_wildcardWindow, "_selectedTab", 0); // Wildcards tab
            
            // Simulate adding a custom wildcard
            var customWildcards = new List<CustomWildcard>
            {
                new CustomWildcard
                {
                    Key = "<TestWildcard>",
                    Value = "TestValue",
                    Description = "Test wildcard"
                }
            };
            _settings.CustomWildcards = customWildcards;
            
            // Act
            _wildcardWindow.Repaint();
            
            // Assert
            Assert.AreEqual(1, _settings.CustomWildcards.Count);
            Assert.AreEqual("<TestWildcard>", _settings.CustomWildcards[0].Key);
            
            // Close
            _wildcardWindow.Close();
        }

        [Test]
        public void WildcardManagementWindow_CanCreateTemplate()
        {
            // Arrange
            _wildcardWindow = EditorWindow.GetWindow<WildcardManagementWindow>();
            SetPrivateField(_wildcardWindow, "_settings", _settings);
            
            // Create template via settings
            var template = new WildcardTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Template",
                Description = "Test template description",
                Pattern = "<Scene>_<Timeline>_<Take>",
                Category = "Testing",
                Tags = new List<string> { "test", "example" }
            };
            
            _settings.Templates.Add(template);
            
            // Act
            _wildcardWindow.Repaint();
            
            // Assert
            Assert.AreEqual(1, _settings.Templates.Count);
            Assert.AreEqual("Test Template", _settings.Templates[0].Name);
            
            // Close
            _wildcardWindow.Close();
        }

        [Test]
        public void ConfigurationManagementWindow_InitializesCorrectly()
        {
            // Arrange
            _configWindow = EditorWindow.GetWindow<ConfigurationManagementWindow>();
            
            // Act
            var windowType = typeof(ConfigurationManagementWindow);
            var tabNames = GetPrivateField<string[]>(_configWindow, "_tabNames");
            
            // Assert
            Assert.IsNotNull(tabNames);
            Assert.AreEqual(4, tabNames.Length);
            Assert.Contains("Scene Settings", tabNames);
            Assert.Contains("Import/Export", tabNames);
            Assert.Contains("Validation", tabNames);
            Assert.Contains("Resource Usage", tabNames);
            
            // Close
            _configWindow.Close();
        }

        [Test]
        public void ConfigurationValidation_CanValidateConfiguration()
        {
            // Arrange
            var referenceService = new MockGameObjectReferenceService();
            var validationService = new ConfigurationValidationService(_logger, referenceService);
            
            var config = new RecordingConfiguration
            {
                FrameRate = 30,
                Resolution = new Resolution(1920, 1080),
                OutputPath = "Recordings",
                TimelineConfigs = new List<ITimelineConfiguration>()
            };
            
            // Act
            var result = validationService.ValidateConfiguration(config);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ConfigurationValidation_DetectsInvalidConfiguration()
        {
            // Arrange
            var referenceService = new MockGameObjectReferenceService();
            var validationService = new ConfigurationValidationService(_logger, referenceService);
            
            var config = new RecordingConfiguration
            {
                FrameRate = -1, // Invalid
                Resolution = new Resolution(0, 0), // Invalid
                OutputPath = "", // Invalid
                TimelineConfigs = new List<ITimelineConfiguration>()
            };
            
            // Act
            var result = validationService.ValidateConfiguration(config);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Issues.Count > 0);
        }

        [Test]
        public void SceneConfigurationManager_CanSaveAndLoadConfiguration()
        {
            // Arrange
            var referenceService = new MockGameObjectReferenceService();
            var configManager = new SceneConfigurationManager(_logger, referenceService);
            
            var config = new SceneConfiguration
            {
                SceneName = "TestScene",
                RecordingConfiguration = new RecordingConfiguration
                {
                    FrameRate = 60,
                    Resolution = new Resolution(2560, 1440),
                    OutputPath = "TestOutput"
                }
            };
            
            // Act
            configManager.SaveConfiguration(config);
            var loaded = configManager.LoadConfiguration("TestScene");
            
            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual("TestScene", loaded.SceneName);
            Assert.AreEqual(60, loaded.RecordingConfiguration.FrameRate);
            
            // Cleanup
            configManager.DeleteConfiguration("TestScene");
            configManager.Dispose();
        }

        [Test]
        public void ResourceUsagePrediction_CalculatesCorrectly()
        {
            // Arrange
            var referenceService = new MockGameObjectReferenceService();
            var validationService = new ConfigurationValidationService(_logger, referenceService);
            
            var config = new RecordingConfiguration
            {
                FrameRate = 60,
                Resolution = new Resolution(3840, 2160), // 4K
                OutputPath = "Recordings",
                TimelineConfigs = new List<ITimelineConfiguration>
                {
                    new TimelineRecorderConfig
                    {
                        TimelinePath = "TestTimeline",
                        RecorderConfigurations = new List<IRecorderConfiguration>
                        {
                            new ImageRecorderConfiguration { Type = RecorderSettingsType.Image },
                            new MovieRecorderConfiguration { Type = RecorderSettingsType.Movie }
                        }
                    }
                }
            };
            
            // Act
            var prediction = validationService.PredictResourceUsage(config);
            
            // Assert
            Assert.IsNotNull(prediction);
            Assert.Greater(prediction.EstimatedMemoryUsageMB, 0);
            Assert.Greater(prediction.EstimatedDiskUsageMBPerMinute, 0);
            Assert.Greater(prediction.EstimatedCPUUsage, 0);
        }

        [Test]
        public void WildcardTemplate_ImportExport_WorksCorrectly()
        {
            // Arrange
            var originalTemplate = new WildcardTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Export Test",
                Description = "Template for export test",
                Pattern = "<Project>_<Scene>_<Date>_<Time>",
                Category = "Export",
                Tags = new List<string> { "export", "test" }
            };
            
            // Act - Export
            var json = JsonUtility.ToJson(originalTemplate, true);
            
            // Act - Import
            var importedTemplate = JsonUtility.FromJson<WildcardTemplate>(json);
            
            // Assert
            Assert.IsNotNull(importedTemplate);
            Assert.AreEqual(originalTemplate.Name, importedTemplate.Name);
            Assert.AreEqual(originalTemplate.Pattern, importedTemplate.Pattern);
            Assert.AreEqual(originalTemplate.Category, importedTemplate.Category);
        }

        // Helper methods

        private void CloseAllTestWindows()
        {
            if (_wildcardWindow != null)
                _wildcardWindow.Close();
            if (_configWindow != null)
                _configWindow.Close();
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null ? (T)field.GetValue(obj) : default(T);
        }

        // Mock implementations

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

            public bool HasLog(string message) => _logs.Any(log => log.Contains(message));
        }

        private class MockGameObjectReferenceService : IGameObjectReferenceService
        {
            public GameObjectReference CreateReference(GameObject gameObject)
            {
                return new GameObjectReference { GameObject = gameObject };
            }

            public GameObjectReference CreateReference(Transform transform)
            {
                return new GameObjectReference { GameObject = transform?.gameObject };
            }

            public bool TryRestoreReference(GameObjectReference reference, out GameObject gameObject)
            {
                gameObject = reference?.GameObject;
                return gameObject != null;
            }

            public bool TryRestoreTransform(GameObjectReference reference, out Transform transform)
            {
                transform = reference?.GameObject?.transform;
                return transform != null;
            }

            public void ValidateReference(GameObjectReference reference) { }

            public void ValidateReferences(IEnumerable<GameObjectReference> references) { }

            public void RefreshAllReferences() { }

            public void ClearCache() { }

            public GameObject ResolveGameObjectByName(string scenePath, string objectName)
            {
                return null;
            }

            public void Dispose() { }
        }
    }
}