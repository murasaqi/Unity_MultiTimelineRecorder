using NUnit.Framework;
using UnityEngine;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class FBXRecorderSettingsConfigTests
    {
        [Test]
        public void Validate_WithValidConfig_ReturnsTrue()
        {
            Debug.Log("Validate_WithValidConfig_ReturnsTrue - テスト開始");
            
            var config = new FBXRecorderSettingsConfig
            {
                exportGeometry = true,
                frameRate = 24f
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsTrue(isValid, "Valid config should return true");
            Assert.IsTrue(string.IsNullOrEmpty(errorMessage), "Error message should be empty for valid config");
            
            Debug.Log("Validate_WithValidConfig_ReturnsTrue - テスト完了");
        }
        
        [Test]
        public void Validate_WithInvalidFrameRate_ReturnsFalse()
        {
            Debug.Log("Validate_WithInvalidFrameRate_ReturnsFalse - テスト開始");
            
            var config = new FBXRecorderSettingsConfig
            {
                exportGeometry = true,
                frameRate = 0f // Invalid
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Config with zero frame rate should be invalid");
            Assert.IsFalse(string.IsNullOrEmpty(errorMessage), "Error message should not be empty");
            Assert.IsTrue(errorMessage.Contains("Frame rate"), "Error message should mention frame rate");
            
            Debug.Log($"Validate_WithInvalidFrameRate_ReturnsFalse - エラーメッセージ: {errorMessage}");
            Debug.Log("Validate_WithInvalidFrameRate_ReturnsFalse - テスト完了");
        }
        
        [Test]
        public void Validate_WithSourceButNoDestination_ReturnsFalse()
        {
            Debug.Log("Validate_WithSourceButNoDestination_ReturnsFalse - テスト開始");
            
            var testGameObject = new GameObject("TestObject");
            try
            {
                var config = new FBXRecorderSettingsConfig
                {
                    exportGeometry = true,
                    frameRate = 24f,
                    transferAnimationSource = testGameObject.transform,
                    transferAnimationDest = null // Invalid combination
                };
                
                string errorMessage;
                bool isValid = config.Validate(out errorMessage);
                
                Assert.IsFalse(isValid, "Config with source but no destination should be invalid");
                Assert.IsFalse(string.IsNullOrEmpty(errorMessage), "Error message should not be empty");
                Assert.IsTrue(errorMessage.Contains("destination"), "Error message should mention destination");
                
                Debug.Log($"Validate_WithSourceButNoDestination_ReturnsFalse - エラーメッセージ: {errorMessage}");
            }
            finally
            {
                GameObject.DestroyImmediate(testGameObject);
            }
            
            Debug.Log("Validate_WithSourceButNoDestination_ReturnsFalse - テスト完了");
        }
        
        [Test]
        public void Validate_WithSameSourceAndDestination_ReturnsFalse()
        {
            Debug.Log("Validate_WithSameSourceAndDestination_ReturnsFalse - テスト開始");
            
            var testGameObject = new GameObject("TestObject");
            try
            {
                var config = new FBXRecorderSettingsConfig
                {
                    exportGeometry = true,
                    frameRate = 24f,
                    transferAnimationSource = testGameObject.transform,
                    transferAnimationDest = testGameObject.transform // Same object - invalid
                };
                
                string errorMessage;
                bool isValid = config.Validate(out errorMessage);
                
                Assert.IsFalse(isValid, "Config with same source and destination should be invalid");
                Assert.IsFalse(string.IsNullOrEmpty(errorMessage), "Error message should not be empty");
                Assert.IsTrue(errorMessage.Contains("same"), "Error message should mention they are the same");
                
                Debug.Log($"Validate_WithSameSourceAndDestination_ReturnsFalse - エラーメッセージ: {errorMessage}");
            }
            finally
            {
                GameObject.DestroyImmediate(testGameObject);
            }
            
            Debug.Log("Validate_WithSameSourceAndDestination_ReturnsFalse - テスト完了");
        }
        
        [Test]
        public void GetPreset_AnimationExport_ReturnsCorrectConfig()
        {
            Debug.Log("GetPreset_AnimationExport_ReturnsCorrectConfig - テスト開始");
            
            var config = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.AnimationExport);
            
            Assert.IsNotNull(config, "Animation export preset should not be null");
            Assert.IsFalse(config.exportGeometry, "Animation export should not export geometry");
            Assert.AreEqual(24f, config.frameRate, "Default frame rate should be 24");
            
            Debug.Log("GetPreset_AnimationExport_ReturnsCorrectConfig - テスト完了");
        }
        
        [Test]
        public void GetPreset_ModelExport_ReturnsCorrectConfig()
        {
            Debug.Log("GetPreset_ModelExport_ReturnsCorrectConfig - テスト開始");
            
            var config = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.ModelExport);
            
            Assert.IsNotNull(config, "Model export preset should not be null");
            Assert.IsTrue(config.exportGeometry, "Model export should export geometry");
            Assert.AreEqual(24f, config.frameRate, "Default frame rate should be 24");
            
            Debug.Log("GetPreset_ModelExport_ReturnsCorrectConfig - テスト完了");
        }
        
        [Test]
        public void GetPreset_ModelAndAnimation_ReturnsCorrectConfig()
        {
            Debug.Log("GetPreset_ModelAndAnimation_ReturnsCorrectConfig - テスト開始");
            
            var config = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.ModelAndAnimation);
            
            Assert.IsNotNull(config, "Model and animation export preset should not be null");
            Assert.IsTrue(config.exportGeometry, "Model and animation export should export geometry");
            Assert.AreEqual(24f, config.frameRate, "Default frame rate should be 24");
            
            Debug.Log("GetPreset_ModelAndAnimation_ReturnsCorrectConfig - テスト完了");
        }
        
        [Test]
        public void CreateFBXRecorderSettings_CreatesValidSettings()
        {
            Debug.Log("CreateFBXRecorderSettings_CreatesValidSettings - テスト開始");
            
            // Skip test if FBX package is not available
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Ignore("FBX package is not installed");
                return;
            }
            
            var config = new FBXRecorderSettingsConfig
            {
                exportGeometry = true,
                frameRate = 30f
            };
            
            var settings = config.CreateFBXRecorderSettings("TestFBX");
            
            Assert.IsNotNull(settings, "Created settings should not be null");
            Assert.AreEqual("TestFBX", settings.name, "Settings name should match");
            Assert.AreEqual(30f, settings.FrameRate, "Frame rate should match config");
            
            Debug.Log("CreateFBXRecorderSettings_CreatesValidSettings - テスト完了");
        }
    }
}