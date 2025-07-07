using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using System.Collections.Generic;

namespace BatchRenderingTool.Tests
{
    /// <summary>
    /// Tests for SingleTimelineRendererV2 multi-recorder functionality
    /// </summary>
    [TestFixture]
    public class SingleTimelineRendererV2Tests
    {
        private SingleTimelineRendererV2 window;
        private GameObject testGameObject;
        private PlayableDirector testDirector;
        private TimelineAsset testTimeline;
        
        [SetUp]
        public void Setup()
        {
            // Create test timeline
            testGameObject = new GameObject("TestTimeline");
            testDirector = testGameObject.AddComponent<PlayableDirector>();
            testTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
            testTimeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            testTimeline.fixedDuration = 5.0;
            testDirector.playableAsset = testTimeline;
            
            // Create window instance
            window = SingleTimelineRendererV2.ShowWindow();
            Assert.IsNotNull(window, "Failed to create SingleTimelineRendererV2 window");
        }
        
        [TearDown]
        public void TearDown()
        {
            if (window != null)
            {
                window.Close();
            }
            
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testTimeline != null)
            {
                Object.DestroyImmediate(testTimeline);
            }
        }
        
        [Test]
        public void TestRecorderConfigCreation()
        {
            // Test creating RecorderConfig
            var config = RecorderConfig.CreateDefault(RecorderSettingsType.Image);
            
            Assert.IsNotNull(config);
            Assert.AreEqual(RecorderSettingsType.Image, config.recorderType);
            Assert.AreEqual("Image Sequence", config.configName);
            Assert.IsTrue(config.fileName.Contains("<Frame>"), "Image sequence should have <Frame> wildcard");
        }
        
        [Test]
        public void TestRecorderConfigValidation()
        {
            // Test validation
            var config = RecorderConfig.CreateDefault(RecorderSettingsType.Image);
            string error;
            
            // Should be valid by default
            Assert.IsTrue(config.Validate(out error), "Default config should be valid");
            
            // Test invalid frame rate
            config.frameRate = -1;
            Assert.IsFalse(config.Validate(out error));
            Assert.IsTrue(error.Contains("frame rate"));
            
            // Test missing file name
            config.frameRate = 24;
            config.fileName = "";
            Assert.IsFalse(config.Validate(out error));
            Assert.IsTrue(error.Contains("File name"));
            
            // Test image sequence without <Frame>
            config.fileName = "test";
            Assert.IsFalse(config.Validate(out error));
            Assert.IsTrue(error.Contains("<Frame>"));
        }
        
        [Test]
        public void TestRecorderConfigClone()
        {
            var original = RecorderConfig.CreateDefault(RecorderSettingsType.Movie);
            original.configName = "Test Movie";
            original.width = 1280;
            original.height = 720;
            original.frameRate = 30;
            
            var clone = original.Clone();
            
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.configName, clone.configName);
            Assert.AreEqual(original.width, clone.width);
            Assert.AreEqual(original.height, clone.height);
            Assert.AreEqual(original.frameRate, clone.frameRate);
            Assert.AreEqual(original.recorderType, clone.recorderType);
        }
        
        [Test]
        public void TestMultipleRecorderTypes()
        {
            // Test that different recorder types have appropriate defaults
            var imageConfig = RecorderConfig.CreateDefault(RecorderSettingsType.Image);
            var movieConfig = RecorderConfig.CreateDefault(RecorderSettingsType.Movie);
            var aovConfig = RecorderConfig.CreateDefault(RecorderSettingsType.AOV);
            
            Assert.IsTrue(imageConfig.fileName.Contains("<Frame>"));
            Assert.IsFalse(movieConfig.fileName.Contains("<Frame>"));
            Assert.IsTrue(aovConfig.fileName.Contains("<Frame>"));
        }
        
        [Test]
        public void TestRecorderConfigEditor()
        {
            var config = RecorderConfig.CreateDefault(RecorderSettingsType.FBX);
            var editor = new RecorderConfigEditor(config, window);
            
            Assert.IsNotNull(editor);
            // Note: We can't easily test the UI drawing without a proper Unity Editor context
        }
        
        [Test]
        public void TestFBXConfigRequiresTarget()
        {
            var config = RecorderConfig.CreateDefault(RecorderSettingsType.FBX);
            string error;
            
            // FBX should require target GameObject
            config.fbxTargetGameObject = null;
            Assert.IsFalse(config.Validate(out error));
            Assert.IsTrue(error.Contains("FBX target GameObject"));
            
            // With target should be valid
            config.fbxTargetGameObject = testGameObject;
            Assert.IsTrue(config.Validate(out error));
        }
        
        [Test]
        public void TestAlembicConfigWithTargetScope()
        {
            var config = RecorderConfig.CreateDefault(RecorderSettingsType.Alembic);
            config.alembicExportScope = AlembicExportScope.TargetGameObject;
            string error;
            
            // Should require target when scope is TargetGameObject
            config.alembicTargetGameObject = null;
            Assert.IsFalse(config.Validate(out error));
            Assert.IsTrue(error.Contains("Alembic target GameObject"));
            
            // With target should be valid
            config.alembicTargetGameObject = testGameObject;
            Assert.IsTrue(config.Validate(out error));
            
            // EntireScene should not require target
            config.alembicExportScope = AlembicExportScope.EntireScene;
            config.alembicTargetGameObject = null;
            Assert.IsTrue(config.Validate(out error));
        }
        
        [Test]
        public void TestAnimationConfigWithScope()
        {
            var config = RecorderConfig.CreateDefault(RecorderSettingsType.Animation);
            config.animationRecordingScope = AnimationRecordingScope.SingleGameObject;
            string error;
            
            // Should require target for SingleGameObject scope
            config.animationTargetGameObject = null;
            Assert.IsFalse(config.Validate(out error));
            Assert.IsTrue(error.Contains("Animation target GameObject"));
            
            // With target should be valid
            config.animationTargetGameObject = testGameObject;
            Assert.IsTrue(config.Validate(out error));
        }
        
        [Test]
        public void TestUniqueConfigNames()
        {
            var config1 = RecorderConfig.CreateDefault(RecorderSettingsType.Image);
            var config2 = RecorderConfig.CreateDefault(RecorderSettingsType.Image);
            
            Assert.AreEqual(config1.configName, config2.configName);
            
            // Window should generate unique names when adding
            // This would be tested in the actual window implementation
        }
    }
}