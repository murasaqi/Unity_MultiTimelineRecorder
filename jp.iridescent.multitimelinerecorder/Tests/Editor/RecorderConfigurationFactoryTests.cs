using System;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Services;

namespace MultiTimelineRecorder.Tests.Editor
{
    [TestFixture]
    public class RecorderConfigurationFactoryTests
    {
        private GameObject _testGameObject;
        private Camera _testCamera;
        private RenderTexture _testRenderTexture;
        private TestLogger _logger;
        private TestGameObjectReferenceService _referenceService;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _referenceService = new TestGameObjectReferenceService();
            
            // Set up test objects
            _testGameObject = new GameObject("TestObject");
            _testCamera = _testGameObject.AddComponent<Camera>();
            _testRenderTexture = new RenderTexture(1920, 1080, 24);
            
            // Configure factory
            RecorderConfigurationFactory.SetGameObjectReferenceService(_referenceService);
            RecorderConfigurationFactory.SetGlobalFrameRate(30);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
                GameObject.DestroyImmediate(_testGameObject);
            if (_testRenderTexture != null)
                RenderTexture.DestroyImmediate(_testRenderTexture);
        }

        [Test]
        public void CreateConfiguration_ImageRecorder_CreatesValidConfig()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfiguration(RecorderSettingsType.Image);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config is ImageRecorderConfiguration);
            var imageConfig = config as ImageRecorderConfiguration;
            Assert.AreEqual(1920, imageConfig.Width);
            Assert.AreEqual(1080, imageConfig.Height);
        }

        [Test]
        public void CreateConfiguration_MovieRecorder_CreatesValidConfig()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfiguration(RecorderSettingsType.Movie);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config is MovieRecorderConfiguration);
            var movieConfig = config as MovieRecorderConfiguration;
            Assert.AreEqual(1920, movieConfig.Width);
            Assert.AreEqual(1080, movieConfig.Height);
        }

        [Test]
        public void CreateConfiguration_AnimationRecorder_CreatesValidConfig()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfiguration(RecorderSettingsType.Animation);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config is AnimationRecorderConfiguration);
            var animConfig = config as AnimationRecorderConfiguration;
            Assert.AreEqual(30, animConfig.FrameRate);
        }

        [Test]
        public void CreateConfiguration_AlembicRecorder_CreatesValidConfig()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfiguration(RecorderSettingsType.Alembic);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config is AlembicRecorderConfiguration);
            var alembicConfig = config as AlembicRecorderConfiguration;
            Assert.AreEqual("Alembic_<Scene>_<Take>", alembicConfig.FileName);
        }

        [Test]
        public void CreateConfiguration_FBXRecorder_CreatesValidConfig()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfiguration(RecorderSettingsType.FBX);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config is FBXRecorderConfiguration);
            var fbxConfig = config as FBXRecorderConfiguration;
            Assert.AreEqual("FBX_<Scene>_<Take>", fbxConfig.FileName);
        }

        [Test]
        public void CreateConfiguration_AOVRecorder_CreatesValidConfig()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfiguration(RecorderSettingsType.AOV);

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config is AOVRecorderConfiguration);
            var aovConfig = config as AOVRecorderConfiguration;
            Assert.AreEqual(AOVType.Beauty, aovConfig.AOVType);
        }

        [Test]
        public void CreateConfiguration_WithInvalidType_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                RecorderConfigurationFactory.CreateConfiguration((RecorderSettingsType)999));
        }

        [Test]
        public void CreateDefaultImageRecorderConfiguration_CreatesExpectedDefaults()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateDefaultImageRecorderConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1920, config.Width);
            Assert.AreEqual(1080, config.Height);
            Assert.AreEqual(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG, config.OutputFormat);
            Assert.AreEqual("Recording_<Scene>_<Take>", config.FileNamePattern);
        }

        [Test]
        public void CreateDefaultMovieRecorderConfiguration_CreatesExpectedDefaults()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateDefaultMovieRecorderConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1920, config.Width);
            Assert.AreEqual(1080, config.Height);
            Assert.AreEqual(UnityEditor.Recorder.MovieRecorderSettings.VideoRecorderOutputFormat.MP4, config.OutputFormat);
            Assert.AreEqual(0.75f, config.Quality);
        }

        [Test]
        public void CreateDefaultAnimationRecorderConfiguration_CreatesExpectedDefaults()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateDefaultAnimationRecorderConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(30, config.FrameRate);
            Assert.IsTrue(config.RecordTransform);
            Assert.IsTrue(config.RecordComponents);
            Assert.AreEqual("Animation_<Scene>_<Take>", config.FileName);
        }

        [Test]
        public void CreateDefaultAlembicRecorderConfiguration_CreatesExpectedDefaults()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateDefaultAlembicRecorderConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(1.0f, config.ScaleFactor);
            Assert.IsTrue(config.CaptureTransform);
            Assert.IsTrue(config.CaptureMeshRenderer);
            Assert.AreEqual(UnityEditor.Recorder.AlembicRecorder.AlembicCompressionType.Ogawa, config.CompressionType);
        }

        [Test]
        public void CreateDefaultFBXRecorderConfiguration_CreatesExpectedDefaults()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateDefaultFBXRecorderConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.IsTrue(config.ExportMeshes);
            Assert.IsTrue(config.ExportAnimation);
            Assert.IsTrue(config.ExportSkinnedMesh);
            Assert.AreEqual(1.0f, config.UnitScale);
        }

        [Test]
        public void CreateDefaultAOVRecorderConfiguration_CreatesExpectedDefaults()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateDefaultAOVRecorderConfiguration();

            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(AOVType.Beauty, config.AOVType);
            Assert.AreEqual(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR, config.OutputFormat);
            Assert.AreEqual("AOV_<AOVType>_<Scene>_<Take>", config.FileName);
        }

        [Test]
        public void SetGlobalFrameRate_UpdatesAllNewConfigurations()
        {
            // Arrange
            RecorderConfigurationFactory.SetGlobalFrameRate(60);

            // Act
            var animConfig = RecorderConfigurationFactory.CreateDefaultAnimationRecorderConfiguration();

            // Assert
            Assert.AreEqual(60, animConfig.FrameRate);
        }

        [Test]
        public void CreateConfigurationWithCamera_SetsTargetCamera()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfigurationWithCamera(
                RecorderSettingsType.Image, _testCamera);

            // Assert
            Assert.IsTrue(config is ImageRecorderConfiguration);
            var imageConfig = config as ImageRecorderConfiguration;
            Assert.AreEqual(_testCamera, imageConfig.TargetCamera);
            Assert.AreEqual(ImageRecorderSourceType.TargetCamera, imageConfig.SourceType);
        }

        [Test]
        public void CreateConfigurationWithRenderTexture_SetsRenderTexture()
        {
            // Act
            var config = RecorderConfigurationFactory.CreateConfigurationWithRenderTexture(
                RecorderSettingsType.Image, _testRenderTexture);

            // Assert
            Assert.IsTrue(config is ImageRecorderConfiguration);
            var imageConfig = config as ImageRecorderConfiguration;
            Assert.AreEqual(_testRenderTexture, imageConfig.RenderTexture);
            Assert.AreEqual(ImageRecorderSourceType.RenderTexture, imageConfig.SourceType);
        }

        [Test]
        public void CreateConfigurationWithAutoResolve_ResolvesGameObject()
        {
            // Arrange
            _referenceService.SetResolveResult(_testGameObject);

            // Act
            var config = RecorderConfigurationFactory.CreateConfigurationWithAutoResolve(
                RecorderSettingsType.Animation, "TestScene", "TestObject");

            // Assert
            Assert.IsTrue(config is AnimationRecorderConfiguration);
            var animConfig = config as AnimationRecorderConfiguration;
            Assert.AreEqual(_testGameObject, animConfig.TargetGameObject);
        }

        [Test]
        public void CreateConfigurationWithAutoResolve_HandlesNullResult()
        {
            // Arrange
            _referenceService.SetResolveResult(null);

            // Act
            var config = RecorderConfigurationFactory.CreateConfigurationWithAutoResolve(
                RecorderSettingsType.Animation, "TestScene", "NonExistent");

            // Assert
            Assert.IsTrue(config is AnimationRecorderConfiguration);
            var animConfig = config as AnimationRecorderConfiguration;
            Assert.IsNull(animConfig.TargetGameObject);
        }

        [Test]
        public void CreateConfigurationFromTemplate_CopiesAllProperties()
        {
            // Arrange
            var template = new ImageRecorderConfiguration
            {
                Width = 2560,
                Height = 1440,
                OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.JPEG,
                Quality = 0.85f,
                FileNamePattern = "Custom_<Scene>",
                CaptureAlpha = true
            };

            // Act
            var copy = RecorderConfigurationFactory.CreateConfigurationFromTemplate(template);

            // Assert
            Assert.IsTrue(copy is ImageRecorderConfiguration);
            var imageCopy = copy as ImageRecorderConfiguration;
            Assert.AreEqual(2560, imageCopy.Width);
            Assert.AreEqual(1440, imageCopy.Height);
            Assert.AreEqual(UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.JPEG, imageCopy.OutputFormat);
            Assert.AreEqual(0.85f, imageCopy.Quality, 0.001f);
            Assert.AreEqual("Custom_<Scene>", imageCopy.FileNamePattern);
            Assert.IsTrue(imageCopy.CaptureAlpha);
        }

        // Test implementations

        private class TestLogger : ILogger
        {
            public void Log(string message, LogLevel level, LogCategory category) { }
            public void LogVerbose(string message, LogCategory category) { }
            public void LogInfo(string message, LogCategory category) { }
            public void LogWarning(string message, LogCategory category) { }
            public void LogError(string message, LogCategory category) { }
        }

        private class TestGameObjectReferenceService : IGameObjectReferenceService
        {
            private GameObject _resolveResult;

            public void SetResolveResult(GameObject result)
            {
                _resolveResult = result;
            }

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

            public void ValidateReferences(System.Collections.Generic.IEnumerable<GameObjectReference> references) { }

            public void RefreshAllReferences() { }

            public void ClearCache() { }

            public GameObject ResolveGameObjectByName(string scenePath, string objectName)
            {
                return _resolveResult;
            }

            public void Dispose() { }
        }
    }
}