using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.Recorder;

namespace BatchRenderingTool.Tests.Editor
{
    /// <summary>
    /// Comprehensive tests for Animation Recorder functionality
    /// </summary>
    public class AnimationRecorderTests
    {
        private GameObject testGameObject;
        private AnimationRecorderSettingsConfig config;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject
            testGameObject = new GameObject("TestAnimationTarget");
            testGameObject.AddComponent<Animator>();
            
            // Initialize config
            config = new AnimationRecorderSettingsConfig();
            config.targetGameObject = testGameObject;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void Config_Validation_RequiresRecordingProperties()
        {
            // Arrange
            config.recordingProperties = AnimationRecordingProperties.None;
            
            // Act
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.AreEqual("At least one recording property must be selected", errorMessage);
        }
        
        [Test]
        public void Config_Validation_ValidatesFrameRate()
        {
            // Test invalid frame rates
            config.recordingProperties = AnimationRecordingProperties.TransformOnly;
            
            // Test zero frame rate
            config.frameRate = 0;
            string errorMessage;
            Assert.IsFalse(config.Validate(out errorMessage));
            Assert.IsTrue(errorMessage.Contains("Frame rate"));
            
            // Test negative frame rate
            config.frameRate = -10;
            Assert.IsFalse(config.Validate(out errorMessage));
            
            // Test too high frame rate
            config.frameRate = 150;
            Assert.IsFalse(config.Validate(out errorMessage));
            
            // Test valid frame rate
            config.frameRate = 30;
            Assert.IsTrue(config.Validate(out errorMessage));
        }
        
        [Test]
        public void Config_Validation_RequiresTargetForSingleGameObject()
        {
            // Arrange
            config.recordingProperties = AnimationRecordingProperties.TransformOnly;
            config.recordingScope = AnimationRecordingScope.SingleGameObject;
            config.targetGameObject = null;
            
            // Act
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.AreEqual("Target GameObject is required", errorMessage);
        }
        
        [Test]
        public void Config_CreateAnimationRecorderSettings_CreatesValidSettings()
        {
            // Arrange
            config.recordingProperties = AnimationRecordingProperties.TransformOnly;
            config.frameRate = 60;
            config.compressionLevel = AnimationCompressionLevel.Medium;
            
            // Act
            var settings = config.CreateAnimationRecorderSettings("TestAnimation");
            
            // Assert
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings is AnimationRecorderSettings);
            Assert.AreEqual("TestAnimation", settings.name);
            Assert.IsTrue(settings.Enabled);
            Assert.AreEqual(RecordMode.Manual, settings.RecordMode);
            Assert.AreEqual(60, settings.FrameRate);
        }
        
        [Test]
        public void Config_GetRecordingObjects_SingleGameObject()
        {
            // Arrange
            config.recordingScope = AnimationRecordingScope.SingleGameObject;
            
            // Act
            var objects = config.GetRecordingObjects();
            
            // Assert
            Assert.AreEqual(1, objects.Count);
            Assert.AreEqual(testGameObject, objects[0]);
        }
        
        [Test]
        public void Config_GetRecordingObjects_GameObjectAndChildren()
        {
            // Arrange
            var child1 = new GameObject("Child1");
            var child2 = new GameObject("Child2");
            child1.transform.parent = testGameObject.transform;
            child2.transform.parent = child1.transform;
            
            config.recordingScope = AnimationRecordingScope.GameObjectAndChildren;
            
            // Act
            var objects = config.GetRecordingObjects();
            
            // Assert
            Assert.AreEqual(3, objects.Count);
            Assert.Contains(testGameObject, objects);
            Assert.Contains(child1, objects);
            Assert.Contains(child2, objects);
            
            // Cleanup
            Object.DestroyImmediate(child1);
        }
        
        [Test]
        public void Config_Clone_CreatesDeepCopy()
        {
            // Arrange
            config.recordingProperties = AnimationRecordingProperties.AllProperties;
            config.frameRate = 45;
            config.compressionLevel = AnimationCompressionLevel.High;
            config.positionError = 0.05f;
            
            // Act
            var clone = config.Clone();
            
            // Assert
            Assert.AreNotSame(config, clone);
            Assert.AreEqual(config.recordingProperties, clone.recordingProperties);
            Assert.AreEqual(config.frameRate, clone.frameRate);
            Assert.AreEqual(config.compressionLevel, clone.compressionLevel);
            Assert.AreEqual(config.positionError, clone.positionError);
        }
        
        [Test]
        public void Config_Presets_CharacterAnimation()
        {
            // Act
            var preset = AnimationRecorderSettingsConfig.GetPreset(AnimationExportPreset.CharacterAnimation);
            
            // Assert
            Assert.AreEqual(AnimationRecordingProperties.TransformAndBlendShapes, preset.recordingProperties);
            Assert.AreEqual(AnimationRecordingScope.GameObjectAndChildren, preset.recordingScope);
            Assert.IsTrue(preset.treatAsHumanoid);
            Assert.IsTrue(preset.recordRootMotion);
            Assert.AreEqual(AnimationCompressionLevel.Medium, preset.compressionLevel);
        }
        
        [Test]
        public void Config_Presets_CameraAnimation()
        {
            // Act
            var preset = AnimationRecorderSettingsConfig.GetPreset(AnimationExportPreset.CameraAnimation);
            
            // Assert
            var expectedProps = AnimationRecordingProperties.Position | 
                              AnimationRecordingProperties.Rotation | 
                              AnimationRecordingProperties.CameraProperties;
            Assert.AreEqual(expectedProps, preset.recordingProperties);
            Assert.AreEqual(AnimationRecordingScope.SingleGameObject, preset.recordingScope);
            Assert.AreEqual(AnimationCompressionLevel.Low, preset.compressionLevel);
        }
        
        [Test]
        public void Config_HumanoidValidation_RequiresHumanoidAnimator()
        {
            // Arrange
            config.recordingProperties = AnimationRecordingProperties.TransformOnly;
            config.treatAsHumanoid = true;
            // testGameObject has Animator but not configured as humanoid
            
            // Act
            string errorMessage;
            bool isValid = config.Validate(out errorMessage);
            
            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errorMessage.Contains("Humanoid Animator"));
        }
        
        [Test]
        public void Config_CompressionSettings_ValidatesErrorTolerances()
        {
            // Arrange
            config.recordingProperties = AnimationRecordingProperties.TransformOnly;
            
            // Test negative position error
            config.positionError = -0.1f;
            string errorMessage;
            Assert.IsFalse(config.Validate(out errorMessage));
            Assert.IsTrue(errorMessage.Contains("Error tolerances"));
            
            // Test negative rotation error
            config.positionError = 0.01f;
            config.rotationError = -1f;
            Assert.IsFalse(config.Validate(out errorMessage));
            
            // Test valid errors
            config.rotationError = 0.5f;
            config.scaleError = 0.01f;
            Assert.IsTrue(config.Validate(out errorMessage));
        }
        
        [Test]
        public void AnimationRecordingProperties_EnumValues()
        {
            // Test that enum flags work correctly
            var combined = AnimationRecordingProperties.Position | AnimationRecordingProperties.Rotation;
            
            Assert.IsTrue((combined & AnimationRecordingProperties.Position) != 0);
            Assert.IsTrue((combined & AnimationRecordingProperties.Rotation) != 0);
            Assert.IsFalse((combined & AnimationRecordingProperties.Scale) != 0);
        }
        
        [Test]
        public void CompressionPresets_ReturnsValidValues()
        {
            // Test all compression levels return valid error tolerances
            var levels = new[] {
                AnimationCompressionLevel.None,
                AnimationCompressionLevel.Low,
                AnimationCompressionLevel.Medium,
                AnimationCompressionLevel.High,
                AnimationCompressionLevel.Optimal
            };
            
            foreach (var level in levels)
            {
                var posError = AnimationRecordingInfo.CompressionPresets.GetPositionErrorTolerance(level);
                var rotError = AnimationRecordingInfo.CompressionPresets.GetRotationErrorTolerance(level);
                var scaleError = AnimationRecordingInfo.CompressionPresets.GetScaleErrorTolerance(level);
                
                Assert.GreaterOrEqual(posError, 0f);
                Assert.GreaterOrEqual(rotError, 0f);
                Assert.GreaterOrEqual(scaleError, 0f);
            }
        }
    }
}