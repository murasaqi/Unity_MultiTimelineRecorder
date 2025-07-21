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
    }
}