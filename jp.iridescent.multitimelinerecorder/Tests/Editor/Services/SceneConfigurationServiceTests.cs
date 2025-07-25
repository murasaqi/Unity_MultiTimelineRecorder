using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Services
{
    /// <summary>
    /// Unit tests for scene-specific configuration management
    /// </summary>
    [TestFixture]
    public class SceneConfigurationServiceTests : TestFixtureBase
    {
        private ConfigurationService _configService;
        private SceneConfigurationManager _sceneConfigManager;
        private GameObjectReferenceService _referenceService;
        private Scene _testScene1;
        private Scene _testScene2;
        private string _testConfigPath;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _configService = new ConfigurationService(Logger);
            _referenceService = new GameObjectReferenceService(Logger, ErrorHandler);
            _sceneConfigManager = new SceneConfigurationManager(Logger, _referenceService);
            
            _testConfigPath = "Assets/Tests/SceneConfigs";
            
            // Create test scenes
            _testScene1 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _testScene1.name = "TestScene1";
            
            // Ensure test directory exists
            if (!Directory.Exists(_testConfigPath))
            {
                Directory.CreateDirectory(_testConfigPath);
            }
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            
            // Clean up test files
            if (Directory.Exists(_testConfigPath))
            {
                Directory.Delete(_testConfigPath, true);
            }
        }
        
        [Test]
        public void SaveSceneConfiguration_WithValidConfig_SavesSuccessfully()
        {
            // Arrange
            var sceneConfig = new SceneConfiguration
            {
                SceneName = _testScene1.name,
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration(),
                LastModified = DateTime.Now
            };
            
            // Act
            var result = _sceneConfigManager.SaveSceneConfiguration(sceneConfig);
            
            // Assert
            Assert.IsTrue(result);
            AssertLogContains($"Saved scene configuration for: {_testScene1.name}", LogLevel.Info);
        }
        
        [Test]
        public void LoadSceneConfiguration_WithExistingConfig_LoadsSuccessfully()
        {
            // Arrange
            var sceneConfig = new SceneConfiguration
            {
                SceneName = _testScene1.name,
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration()
            };
            sceneConfig.RecordingConfiguration.Name = "Scene1 Config";
            
            _sceneConfigManager.SaveSceneConfiguration(sceneConfig);
            
            // Act
            var loaded = _sceneConfigManager.LoadSceneConfiguration(_testScene1.name);
            
            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Scene1 Config", loaded.RecordingConfiguration.Name);
            Assert.AreEqual(_testScene1.name, loaded.SceneName);
        }
        
        [Test]
        public void LoadSceneConfiguration_WithNonExistentConfig_ReturnsNull()
        {
            // Act
            var loaded = _sceneConfigManager.LoadSceneConfiguration("NonExistentScene");
            
            // Assert
            Assert.IsNull(loaded);
            AssertLogContains("Scene configuration not found", LogLevel.Info);
        }
        
        [Test]
        public void AutoSaveOnSceneChange_SavesCurrentSceneConfig()
        {
            // Arrange
            var sceneConfig = new SceneConfiguration
            {
                SceneName = _testScene1.name,
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration()
            };
            
            _sceneConfigManager.SetCurrentConfiguration(sceneConfig);
            
            // Act - Simulate scene change
            _testScene2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _testScene2.name = "TestScene2";
            
            // Trigger scene opened event
            _sceneConfigManager.OnSceneOpened(_testScene2, OpenSceneMode.Additive);
            
            // Assert
            AssertLogContains("Auto-saving configuration for scene", LogLevel.Info);
        }
        
        [Test]
        public void AutoRestoreOnSceneOpen_RestoresPreviousConfig()
        {
            // Arrange
            var sceneConfig = new SceneConfiguration
            {
                SceneName = "TestScene2",
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration()
            };
            sceneConfig.RecordingConfiguration.Name = "Scene2 Specific Config";
            
            _sceneConfigManager.SaveSceneConfiguration(sceneConfig);
            
            // Act
            _testScene2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            _testScene2.name = "TestScene2";
            _sceneConfigManager.OnSceneOpened(_testScene2, OpenSceneMode.Additive);
            
            // Assert
            var current = _sceneConfigManager.GetCurrentConfiguration();
            Assert.IsNotNull(current);
            Assert.AreEqual("Scene2 Specific Config", current.RecordingConfiguration.Name);
            AssertLogContains("Restored configuration for scene: TestScene2", LogLevel.Info);
        }
        
        [Test]
        public void MergeSceneConfigurations_WithConflicts_ResolvesCorrectly()
        {
            // Arrange
            var config1 = new SceneConfiguration
            {
                SceneName = "SharedScene",
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration(),
                LastModified = DateTime.Now.AddMinutes(-5)
            };
            config1.RecordingConfiguration.FrameRate = 30;
            
            var config2 = new SceneConfiguration
            {
                SceneName = "SharedScene",
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration(),
                LastModified = DateTime.Now
            };
            config2.RecordingConfiguration.FrameRate = 60;
            
            // Act
            var merged = _sceneConfigManager.MergeConfigurations(config1, config2, MergeStrategy.UseNewest);
            
            // Assert
            Assert.AreEqual(60, merged.RecordingConfiguration.FrameRate); // Should use newer config
            AssertLogContains("Merged configurations using strategy: UseNewest", LogLevel.Info);
        }
        
        [Test]
        public void ValidateSceneConfiguration_WithInvalidGameObjectRefs_ReportsErrors()
        {
            // Arrange
            var sceneConfig = new SceneConfiguration
            {
                SceneName = _testScene1.name,
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration()
            };
            
            // Add timeline config with GameObject reference
            var timeline = TestDataBuilder.CreateTestTimelineConfig();
            var go = new GameObject("TempObject");
            timeline.TargetGameObject = _referenceService.CreateReference(go);
            GameObject.DestroyImmediate(go); // Destroy to make reference invalid
            
            sceneConfig.RecordingConfiguration.TimelineConfigs.Add(timeline);
            
            // Act
            var result = _sceneConfigManager.ValidateSceneConfiguration(sceneConfig);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("GameObject reference")));
        }
        
        [Test]
        public void ImportExportSceneConfiguration_RoundTrip_PreservesData()
        {
            // Arrange
            var sceneConfig = new SceneConfiguration
            {
                SceneName = "ExportTestScene",
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration()
            };
            sceneConfig.RecordingConfiguration.Name = "Export Test Config";
            sceneConfig.RecordingConfiguration.FrameRate = 60;
            
            var exportPath = Path.Combine(_testConfigPath, "export_test.json");
            
            // Act - Export
            var exportResult = _sceneConfigManager.ExportSceneConfiguration(sceneConfig, exportPath);
            Assert.IsTrue(exportResult);
            
            // Act - Import
            var imported = _sceneConfigManager.ImportSceneConfiguration(exportPath);
            
            // Assert
            Assert.IsNotNull(imported);
            Assert.AreEqual("ExportTestScene", imported.SceneName);
            Assert.AreEqual("Export Test Config", imported.RecordingConfiguration.Name);
            Assert.AreEqual(60, imported.RecordingConfiguration.FrameRate);
        }
        
        [Test]
        public void GetSceneConfigurationHistory_ReturnsRecentConfigs()
        {
            // Arrange
            for (int i = 0; i < 5; i++)
            {
                var config = new SceneConfiguration
                {
                    SceneName = $"HistoryScene{i}",
                    RecordingConfiguration = TestDataBuilder.CreateTestConfiguration(),
                    LastModified = DateTime.Now.AddMinutes(-i)
                };
                _sceneConfigManager.SaveSceneConfiguration(config);
            }
            
            // Act
            var history = _sceneConfigManager.GetSceneConfigurationHistory(3);
            
            // Assert
            Assert.AreEqual(3, history.Count);
            Assert.AreEqual("HistoryScene0", history[0].SceneName); // Most recent first
        }
        
        [Test]
        public void CleanupOldConfigurations_RemovesOutdatedConfigs()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                var config = new SceneConfiguration
                {
                    SceneName = $"OldScene{i}",
                    RecordingConfiguration = TestDataBuilder.CreateTestConfiguration(),
                    LastModified = DateTime.Now.AddDays(-i - 30) // Old configs
                };
                _sceneConfigManager.SaveSceneConfiguration(config);
            }
            
            // Act
            var removed = _sceneConfigManager.CleanupOldConfigurations(TimeSpan.FromDays(30));
            
            // Assert
            Assert.IsTrue(removed > 0);
            AssertLogContains($"Cleaned up {removed} old scene configurations", LogLevel.Info);
        }
        
        [Test]
        public void SceneConfigurationScope_GlobalVsSceneSpecific_HandledCorrectly()
        {
            // Arrange
            var globalConfig = TestDataBuilder.CreateTestConfiguration();
            globalConfig.Name = "Global Config";
            globalConfig.IsGlobalConfiguration = true;
            
            var sceneSpecificConfig = new SceneConfiguration
            {
                SceneName = _testScene1.name,
                RecordingConfiguration = TestDataBuilder.CreateTestConfiguration(),
                OverridesGlobalSettings = true
            };
            sceneSpecificConfig.RecordingConfiguration.Name = "Scene Specific";
            
            // Act
            _sceneConfigManager.SetGlobalConfiguration(globalConfig);
            _sceneConfigManager.SaveSceneConfiguration(sceneSpecificConfig);
            
            var effective = _sceneConfigManager.GetEffectiveConfiguration(_testScene1.name);
            
            // Assert
            Assert.IsNotNull(effective);
            Assert.AreEqual("Scene Specific", effective.Name); // Scene-specific should override
            AssertLogContains("Using scene-specific configuration", LogLevel.Info);
        }
    }
}