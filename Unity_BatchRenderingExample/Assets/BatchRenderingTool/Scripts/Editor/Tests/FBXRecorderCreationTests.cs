using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using System;

namespace BatchRenderingTool.Tests
{
    /// <summary>
    /// Tests for FBX Recorder creation and configuration
    /// </summary>
    public class FBXRecorderCreationTests
    {
        [Test]
        public void CanDetectFBXPackage()
        {
            // Clear cache to ensure fresh check
            FBXExportInfo.ClearCache();
            
            bool isAvailable = FBXExportInfo.IsFBXPackageAvailable();
            
            // This test will pass or fail based on whether the package is installed
            if (!isAvailable)
            {
                Assert.Inconclusive("FBX Exporter package is not installed. Install com.unity.formats.fbx to enable FBX recording.");
            }
            else
            {
                Assert.IsTrue(isAvailable, "FBX package should be detected when installed");
            }
        }
        
        [Test]
        public void CanCreateFBXRecorderSettings_WithUtility()
        {
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Inconclusive("FBX Exporter package is not installed");
                return;
            }
            
            var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
            
            Assert.IsNotNull(settings, "Should create FBX recorder settings");
            Assert.IsTrue(settings.GetType().Name.Contains("FbxRecorderSettings"), 
                $"Settings should be FBX type, but was {settings.GetType().Name}");
        }
        
        [Test]
        public void CanCreateFBXRecorderSettings_WithFactory()
        {
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Inconclusive("FBX Exporter package is not installed");
                return;
            }
            
            var settings = RecorderSettingsFactory.CreateFBXRecorderSettings("TestFBX");
            
            Assert.IsNotNull(settings, "Should create FBX recorder settings via factory");
            Assert.AreEqual("TestFBX", settings.name);
            Assert.AreEqual(RecordMode.Manual, settings.RecordMode);
            Assert.AreEqual(24, settings.FrameRate);
        }
        
        [Test]
        public void CanConfigureFBXRecorderSettings_WithGameObject()
        {
            if (!FBXExportInfo.IsFBXPackageAvailable())
            {
                Assert.Inconclusive("FBX Exporter package is not installed");
                return;
            }
            
            // Create a test GameObject
            var testGO = new GameObject("TestFBXTarget");
            
            try
            {
                var config = new FBXRecorderSettingsConfig
                {
                    targetGameObject = testGO,
                    recordHierarchy = true,
                    clampedTangents = true,
                    animationCompression = FBXAnimationCompressionLevel.Lossy,
                    exportGeometry = true,
                    frameRate = 30f
                };
                
                string errorMessage;
                Assert.IsTrue(config.Validate(out errorMessage), $"Config should be valid: {errorMessage}");
                
                var settings = config.CreateFBXRecorderSettings("TestFBXWithGO");
                
                Assert.IsNotNull(settings, "Should create configured FBX recorder settings");
                Assert.AreEqual("TestFBXWithGO", settings.name);
                
                // Verify frame rate was set
                Assert.AreEqual(30f, settings.FrameRate, "Frame rate should be set from config");
            }
            finally
            {
                // Clean up
                if (testGO != null)
                    GameObject.DestroyImmediate(testGO);
            }
        }
        
        [Test]
        public void FBXConfig_RequiresTargetGameObject()
        {
            var config = new FBXRecorderSettingsConfig
            {
                targetGameObject = null, // This should cause validation to fail
                recordHierarchy = true,
                frameRate = 24f
            };
            
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            Assert.IsFalse(isValid, "Config should be invalid without target GameObject");
            Assert.IsTrue(errorMessage.Contains("Target GameObject"), 
                $"Error message should mention target GameObject, but was: {errorMessage}");
        }
        
        [Test]
        public void FBXConfig_ValidatesAnimationTransfer()
        {
            var source = new GameObject("Source");
            var dest = new GameObject("Dest");
            
            try
            {
                // Test invalid config - source without dest
                var config1 = new FBXRecorderSettingsConfig
                {
                    targetGameObject = source,
                    transferAnimationSource = source.transform,
                    transferAnimationDest = null,
                    frameRate = 24f
                };
                
                string errorMessage;
                Assert.IsFalse(config1.Validate(out errorMessage), 
                    "Config should be invalid with source but no dest");
                
                // Test invalid config - same source and dest
                var config2 = new FBXRecorderSettingsConfig
                {
                    targetGameObject = source,
                    transferAnimationSource = source.transform,
                    transferAnimationDest = source.transform,
                    frameRate = 24f
                };
                
                Assert.IsFalse(config2.Validate(out errorMessage), 
                    "Config should be invalid with same source and dest");
                
                // Test valid config
                var config3 = new FBXRecorderSettingsConfig
                {
                    targetGameObject = source,
                    transferAnimationSource = source.transform,
                    transferAnimationDest = dest.transform,
                    frameRate = 24f
                };
                
                Assert.IsTrue(config3.Validate(out errorMessage), 
                    $"Config should be valid with different source and dest: {errorMessage}");
            }
            finally
            {
                // Clean up
                if (source != null) GameObject.DestroyImmediate(source);
                if (dest != null) GameObject.DestroyImmediate(dest);
            }
        }
        
        [Test]
        public void CanGetFBXPresetConfigurations()
        {
            // Test AnimationExport preset
            var animConfig = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.AnimationExport);
            Assert.IsNotNull(animConfig);
            Assert.IsFalse(animConfig.exportGeometry, "Animation preset should not export geometry");
            
            // Test ModelExport preset
            var modelConfig = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.ModelExport);
            Assert.IsNotNull(modelConfig);
            Assert.IsTrue(modelConfig.exportGeometry, "Model preset should export geometry");
            
            // Test ModelAndAnimation preset
            var bothConfig = FBXRecorderSettingsConfig.GetPreset(FBXExportPreset.ModelAndAnimation);
            Assert.IsNotNull(bothConfig);
            Assert.IsTrue(bothConfig.exportGeometry, "Model+Animation preset should export geometry");
            Assert.IsTrue(bothConfig.recordHierarchy, "Model+Animation preset should record hierarchy");
        }
    }
}