using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;

namespace BatchRenderingTool.Editor.Tests
{
    [TestFixture]
    public class FBXRecorderCreationTests
    {
        [Test]
        public void FBXPackage_IsInstalled()
        {
            bool isInstalled = FBXExportInfo.IsFBXPackageAvailable();
            Assert.IsTrue(isInstalled, "Unity FBX Exporter package (com.unity.formats.fbx) is not installed");
        }
        
        [Test]
        public void FBXRecorderSettings_CanBeCreated_UsingRecorderClipUtility()
        {
            var settings = RecorderClipUtility.CreateProperRecorderSettings("FbxRecorderSettings");
            Assert.IsNotNull(settings, "Failed to create FBX recorder settings using RecorderClipUtility");
            
            if (settings != null)
            {
                Debug.Log($"[FBXRecorderCreationTests] Created FBX recorder settings of type: {settings.GetType().FullName}");
            }
        }
        
        [Test]
        public void FBXRecorderSettings_CanBeCreated_UsingCreateProperFBXRecorderSettings()
        {
            var settings = RecorderClipUtility.CreateProperFBXRecorderSettings("TestFBX");
            Assert.IsNotNull(settings, "Failed to create FBX recorder settings using CreateProperFBXRecorderSettings");
            
            if (settings != null)
            {
                Debug.Log($"[FBXRecorderCreationTests] Created FBX recorder settings of type: {settings.GetType().FullName}");
            }
        }
        
        [Test]
        public void FBXRecorderSettingsConfig_CanCreateSettings()
        {
            var config = new FBXRecorderSettingsConfig
            {
                targetGameObject = new GameObject("TestObject"),
                recordHierarchy = true,
                clampedTangents = true,
                animationCompression = FBXAnimationCompressionLevel.Lossy,
                exportGeometry = true,
                frameRate = 24f
            };
            
            try
            {
                var settings = config.CreateFBXRecorderSettings("TestFBX");
                Assert.IsNotNull(settings, "FBXRecorderSettingsConfig.CreateFBXRecorderSettings returned null");
                
                if (settings != null)
                {
                    Debug.Log($"[FBXRecorderCreationTests] Config created settings of type: {settings.GetType().FullName}");
                }
            }
            finally
            {
                // Clean up
                if (config.targetGameObject != null)
                {
                    Object.DestroyImmediate(config.targetGameObject);
                }
            }
        }
        
        [Test]
        public void FBXRecorderSettings_HasExpectedProperties()
        {
            var settings = RecorderClipUtility.CreateProperFBXRecorderSettings("TestFBX");
            
            if (settings == null)
            {
                Assert.Inconclusive("Cannot test properties - FBX recorder settings creation failed");
                return;
            }
            
            var settingsType = settings.GetType();
            
            // Check for expected properties
            var exportGeometryProp = settingsType.GetProperty("ExportGeometry");
            Assert.IsNotNull(exportGeometryProp, "ExportGeometry property not found");
            
            var animInputField = settingsType.GetField("m_AnimationInputSettings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(animInputField, "m_AnimationInputSettings field not found");
            
            Debug.Log($"[FBXRecorderCreationTests] FBX recorder settings type: {settingsType.FullName}");
            Debug.Log($"[FBXRecorderCreationTests] Properties found: ExportGeometry={exportGeometryProp != null}, AnimationInputSettings={animInputField != null}");
        }
    }
}