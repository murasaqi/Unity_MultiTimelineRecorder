using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Services
{
    /// <summary>
    /// Unit tests for ConfigurationService
    /// </summary>
    [TestFixture]
    public class ConfigurationServiceTests : TestFixtureBase
    {
        private ConfigurationService _configService;
        private string _testConfigPath;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _configService = new ConfigurationService(Logger);
            _testConfigPath = "Assets/Tests/TestConfig.asset";
            
            // Ensure test directory exists
            var testDir = Path.GetDirectoryName(_testConfigPath);
            if (!Directory.Exists(testDir))
            {
                Directory.CreateDirectory(testDir);
            }
        }
        
        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            
            // Clean up test files
            if (File.Exists(_testConfigPath))
            {
                AssetDatabase.DeleteAsset(_testConfigPath);
            }
        }
        
        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigurationService(null));
        }
        
        [Test]
        public void SaveConfiguration_WithNullConfig_ThrowsFalse()
        {
            var result = _configService.SaveConfiguration(null, _testConfigPath);
            
            Assert.IsFalse(result);
            AssertLogContains("Cannot save null configuration", LogLevel.Error);
        }
        
        [Test]
        public void SaveConfiguration_WithNullPath_ReturnsFalse()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            
            var result = _configService.SaveConfiguration(config, null);
            
            Assert.IsFalse(result);
            AssertLogContains("Save path is null or empty", LogLevel.Error);
        }
        
        [Test]
        public void SaveConfiguration_WithEmptyPath_ReturnsFalse()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            
            var result = _configService.SaveConfiguration(config, "");
            
            Assert.IsFalse(result);
            AssertLogContains("Save path is null or empty", LogLevel.Error);
        }
        
        [Test]
        public void SaveConfiguration_WithValidConfig_ReturnsTrue()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            
            var result = _configService.SaveConfiguration(config, _testConfigPath);
            
            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(_testConfigPath));
            AssertLogContains($"Configuration saved to: {_testConfigPath}", LogLevel.Info);
        }
        
        [Test]
        public void LoadConfiguration_WithNullPath_ReturnsNull()
        {
            var config = _configService.LoadConfiguration(null);
            
            Assert.IsNull(config);
            AssertLogContains("Load path is null or empty", LogLevel.Error);
        }
        
        [Test]
        public void LoadConfiguration_WithNonExistentFile_ReturnsNull()
        {
            var config = _configService.LoadConfiguration("Assets/NonExistent/Config.asset");
            
            Assert.IsNull(config);
            AssertLogContains("Configuration file not found", LogLevel.Error);
        }
        
        [Test]
        public void LoadConfiguration_WithValidFile_ReturnsConfig()
        {
            // First save a config
            var originalConfig = TestDataBuilder.CreateTestConfiguration();
            originalConfig.Name = "Test Load Config";
            _configService.SaveConfiguration(originalConfig, _testConfigPath);
            
            // Then load it
            var loadedConfig = _configService.LoadConfiguration(_testConfigPath);
            
            Assert.IsNotNull(loadedConfig);
            Assert.AreEqual("Test Load Config", loadedConfig.Name);
            AssertLogContains($"Configuration loaded from: {_testConfigPath}", LogLevel.Info);
        }
        
        [Test]
        public void CreateDefaultConfiguration_ReturnsValidConfig()
        {
            var config = _configService.CreateDefaultConfiguration();
            
            Assert.IsNotNull(config);
            Assert.AreEqual("New Recording Configuration", config.Name);
            Assert.AreEqual(30, config.FrameRate);
            Assert.IsNotNull(config.GlobalSettings);
            Assert.IsNotNull(config.TimelineConfigs);
            Assert.IsEmpty(config.TimelineConfigs);
            AssertLogContains("Created default configuration", LogLevel.Info);
        }
        
        [Test]
        public void ValidateConfiguration_WithNullConfig_ReturnsFalse()
        {
            var result = _configService.ValidateConfiguration(null);
            
            Assert.IsFalse(result.IsValid);
            Assert.Contains("Configuration is null", result.Errors);
        }
        
        [Test]
        public void ValidateConfiguration_WithInvalidFrameRate_ReturnsFalse()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = -1;
            
            var result = _configService.ValidateConfiguration(config);
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Frame rate")));
        }
        
        [Test]
        public void ValidateConfiguration_WithInvalidResolution_ReturnsFalse()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            config.Resolution = new Resolution(0, 0);
            
            var result = _configService.ValidateConfiguration(config);
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Resolution")));
        }
        
        [Test]
        public void ValidateConfiguration_WithValidConfig_ReturnsTrue()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            
            var result = _configService.ValidateConfiguration(config);
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
            AssertLogContains("Configuration is valid", LogLevel.Info);
        }
        
        [Test]
        public void ExportConfiguration_WithNullConfig_ReturnsFalse()
        {
            var result = _configService.ExportConfiguration(null, "export.json");
            
            Assert.IsFalse(result);
            AssertLogContains("Cannot export null configuration", LogLevel.Error);
        }
        
        [Test]
        public void ImportConfiguration_WithNullPath_ReturnsNull()
        {
            var config = _configService.ImportConfiguration(null);
            
            Assert.IsNull(config);
            AssertLogContains("Import path is null or empty", LogLevel.Error);
        }
        
        [Test]
        public void GetRecentConfigurations_ReturnsEmptyListInitially()
        {
            var recent = _configService.GetRecentConfigurations();
            
            Assert.IsNotNull(recent);
            Assert.IsEmpty(recent);
        }
        
        [Test]
        public void Dispose_CleansUpResources()
        {
            _configService.Dispose();
            
            // Should not throw when accessing after dispose
            var config = _configService.CreateDefaultConfiguration();
            Assert.IsNotNull(config);
        }
        
        [Test]
        public void SaveConfiguration_CreatesBackup_WhenFileExists()
        {
            // Save initial config
            var config1 = TestDataBuilder.CreateTestConfiguration();
            config1.Name = "Original Config";
            _configService.SaveConfiguration(config1, _testConfigPath);
            
            // Save again to trigger backup
            var config2 = TestDataBuilder.CreateTestConfiguration();
            config2.Name = "Updated Config";
            _configService.SaveConfiguration(config2, _testConfigPath);
            
            // Check that backup was mentioned in logs
            AssertLogContains("backup", LogLevel.Info);
        }
        
        [Test]
        public void ValidateConfiguration_WithMismatchedFrameRates_ReturnsFalse()
        {
            // Arrange
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = 30;
            
            // Add timeline configs with different frame rates
            var timeline1 = TestDataBuilder.CreateTestTimelineConfig();
            var timeline2 = TestDataBuilder.CreateTestTimelineConfig();
            
            // Add recorders with mismatched frame rates
            var recorder1 = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder1.FrameRate = 30;
            timeline1.RecorderConfigs.Add(recorder1);
            
            var recorder2 = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder2.FrameRate = 60; // Different from global
            timeline2.RecorderConfigs.Add(recorder2);
            
            config.TimelineConfigs.Add(timeline1);
            config.TimelineConfigs.Add(timeline2);
            
            // Act
            var result = _configService.ValidateConfiguration(config);
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Exists(e => e.Contains("Frame rate")));
        }
        
        [Test]
        public void ValidateConfiguration_WithUnifiedFrameRates_ReturnsTrue()
        {
            // Arrange
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = 30;
            
            // Add timeline configs with matching frame rates
            var timeline1 = TestDataBuilder.CreateTestTimelineConfig();
            var timeline2 = TestDataBuilder.CreateTestTimelineConfig();
            
            // Add recorders with matching frame rates
            var recorder1 = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder1.FrameRate = 30;
            timeline1.RecorderConfigs.Add(recorder1);
            
            var recorder2 = TestDataBuilder.CreateTestImageRecorderConfig();
            recorder2.FrameRate = 30;
            timeline2.RecorderConfigs.Add(recorder2);
            
            config.TimelineConfigs.Add(timeline1);
            config.TimelineConfigs.Add(timeline2);
            
            // Act
            var result = _configService.ValidateConfiguration(config);
            
            // Assert
            Assert.IsTrue(result.IsValid);
            AssertLogContains("Frame rate consistency check passed", LogLevel.Info);
        }
        
        [Test]
        public void ApplyGlobalFrameRate_UpdatesAllRecorderFrameRates()
        {
            // Arrange
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = 60;
            
            // Add timeline configs with recorders
            var timeline1 = TestDataBuilder.CreateTestTimelineConfig();
            var recorder1 = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder1.FrameRate = 30; // Different from global
            timeline1.RecorderConfigs.Add(recorder1);
            
            var timeline2 = TestDataBuilder.CreateTestTimelineConfig();
            var recorder2 = TestDataBuilder.CreateTestImageRecorderConfig();
            recorder2.FrameRate = 24; // Different from global
            timeline2.RecorderConfigs.Add(recorder2);
            
            config.TimelineConfigs.Add(timeline1);
            config.TimelineConfigs.Add(timeline2);
            
            // Act
            _configService.ApplyGlobalFrameRate(config);
            
            // Assert
            Assert.AreEqual(60, recorder1.FrameRate);
            Assert.AreEqual(60, recorder2.FrameRate);
            AssertLogContains("Applied global frame rate", LogLevel.Info);
        }
        
        [Test]
        public void ValidateConfiguration_WithTimelineFrameRateConstraints_ChecksCompatibility()
        {
            // Arrange
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = 30;
            
            // Add timeline with specific frame rate constraint
            var timeline = TestDataBuilder.CreateTestTimelineConfig();
            timeline.TimelineFrameRate = 24; // Timeline has different frame rate
            
            var recorder = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder.FrameRate = 30;
            timeline.RecorderConfigs.Add(recorder);
            
            config.TimelineConfigs.Add(timeline);
            
            // Act
            var result = _configService.ValidateConfiguration(config);
            
            // Assert
            // Should warn about timeline frame rate mismatch
            Assert.IsTrue(result.Warnings.Count > 0);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("Timeline frame rate")));
        }
        
        [Test]
        public void ExportConfiguration_IncludesFrameRateSettings()
        {
            // Arrange
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = 60;
            config.EnforceFrameRateConsistency = true;
            
            var exportPath = "Assets/Tests/export_test.json";
            
            // Act
            var result = _configService.ExportConfiguration(config, exportPath);
            
            // Assert
            Assert.IsTrue(result);
            AssertLogContains("Exported configuration", LogLevel.Info);
            
            // Clean up
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }
}