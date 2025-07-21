using System;
using NUnit.Framework;
using MultiTimelineRecorder.Core.Models;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.Tests.Models
{
    /// <summary>
    /// Unit tests for GlobalSettings
    /// </summary>
    [TestFixture]
    public class GlobalSettingsTests
    {
        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            var settings = new GlobalSettings();
            
            Assert.AreEqual("Assets/Recordings", settings.BaseOutputPath);
            Assert.AreEqual(30, settings.DefaultFrameRate);
            Assert.AreEqual(1920, settings.DefaultWidth);
            Assert.AreEqual(1080, settings.DefaultHeight);
            Assert.AreEqual(95, settings.DefaultQuality);
            Assert.IsFalse(settings.UseSceneDirectory);
            Assert.IsTrue(settings.OrganizeByTimeline);
            Assert.IsTrue(settings.OrganizeByRecorderType);
            Assert.IsTrue(settings.AutoCreateDirectories);
            Assert.IsTrue(settings.AutoSaveBeforeRecording);
            Assert.IsFalse(settings.ShowPreviewWindow);
            Assert.IsTrue(settings.ValidateBeforeRecording);
            Assert.IsFalse(settings.OpenOutputFolderAfterRecording);
            Assert.AreEqual(1, settings.MaxConcurrentRecorders);
            Assert.IsFalse(settings.UseAsyncRecording);
            Assert.IsTrue(settings.CaptureAudio);
            Assert.IsFalse(settings.UseMotionBlur);
            Assert.AreEqual(LogVerbosity.Normal, settings.LogVerbosity);
        }
        
        [Test]
        public void BaseOutputPath_SetNull_DoesNotThrow()
        {
            var settings = new GlobalSettings();
            
            Assert.DoesNotThrow(() => settings.BaseOutputPath = null);
            Assert.IsNull(settings.BaseOutputPath);
        }
        
        [Test]
        public void BaseOutputPath_SetEmpty_StoresEmpty()
        {
            var settings = new GlobalSettings();
            
            settings.BaseOutputPath = "";
            
            Assert.AreEqual("", settings.BaseOutputPath);
        }
        
        [Test]
        public void DefaultFrameRate_SetNegative_StoresNegative()
        {
            var settings = new GlobalSettings();
            
            settings.DefaultFrameRate = -1;
            
            Assert.AreEqual(-1, settings.DefaultFrameRate);
        }
        
        [Test]
        public void DefaultWidth_SetZero_StoresZero()
        {
            var settings = new GlobalSettings();
            
            settings.DefaultWidth = 0;
            
            Assert.AreEqual(0, settings.DefaultWidth);
        }
        
        [Test]
        public void DefaultHeight_SetZero_StoresZero()
        {
            var settings = new GlobalSettings();
            
            settings.DefaultHeight = 0;
            
            Assert.AreEqual(0, settings.DefaultHeight);
        }
        
        [Test]
        public void DefaultQuality_SetOutOfRange_StoresValue()
        {
            var settings = new GlobalSettings();
            
            settings.DefaultQuality = 200; // Over 100
            Assert.AreEqual(200, settings.DefaultQuality);
            
            settings.DefaultQuality = -50; // Below 0
            Assert.AreEqual(-50, settings.DefaultQuality);
        }
        
        [Test]
        public void MaxConcurrentRecorders_SetNegative_StoresNegative()
        {
            var settings = new GlobalSettings();
            
            settings.MaxConcurrentRecorders = -1;
            
            Assert.AreEqual(-1, settings.MaxConcurrentRecorders);
        }
        
        [Test]
        public void OrganizationSettings_Toggle_WorksCorrectly()
        {
            var settings = new GlobalSettings();
            
            // Test timeline organization
            Assert.IsTrue(settings.OrganizeByTimeline);
            settings.OrganizeByTimeline = false;
            Assert.IsFalse(settings.OrganizeByTimeline);
            
            // Test recorder type organization
            Assert.IsTrue(settings.OrganizeByRecorderType);
            settings.OrganizeByRecorderType = false;
            Assert.IsFalse(settings.OrganizeByRecorderType);
        }
        
        [Test]
        public void WorkflowSettings_Toggle_WorksCorrectly()
        {
            var settings = new GlobalSettings();
            
            // Test auto-save
            Assert.IsTrue(settings.AutoSaveBeforeRecording);
            settings.AutoSaveBeforeRecording = false;
            Assert.IsFalse(settings.AutoSaveBeforeRecording);
            
            // Test preview window
            Assert.IsFalse(settings.ShowPreviewWindow);
            settings.ShowPreviewWindow = true;
            Assert.IsTrue(settings.ShowPreviewWindow);
            
            // Test validation
            Assert.IsTrue(settings.ValidateBeforeRecording);
            settings.ValidateBeforeRecording = false;
            Assert.IsFalse(settings.ValidateBeforeRecording);
            
            // Test open folder
            Assert.IsFalse(settings.OpenOutputFolderAfterRecording);
            settings.OpenOutputFolderAfterRecording = true;
            Assert.IsTrue(settings.OpenOutputFolderAfterRecording);
        }
        
        [Test]
        public void RecordingSettings_Toggle_WorksCorrectly()
        {
            var settings = new GlobalSettings();
            
            // Test async recording
            Assert.IsFalse(settings.UseAsyncRecording);
            settings.UseAsyncRecording = true;
            Assert.IsTrue(settings.UseAsyncRecording);
            
            // Test audio capture
            Assert.IsTrue(settings.CaptureAudio);
            settings.CaptureAudio = false;
            Assert.IsFalse(settings.CaptureAudio);
            
            // Test motion blur
            Assert.IsFalse(settings.UseMotionBlur);
            settings.UseMotionBlur = true;
            Assert.IsTrue(settings.UseMotionBlur);
        }
        
        [Test]
        public void LogVerbosity_SetAllValues_WorksCorrectly()
        {
            var settings = new GlobalSettings();
            
            settings.LogVerbosity = LogVerbosity.Quiet;
            Assert.AreEqual(LogVerbosity.Quiet, settings.LogVerbosity);
            
            settings.LogVerbosity = LogVerbosity.Normal;
            Assert.AreEqual(LogVerbosity.Normal, settings.LogVerbosity);
            
            settings.LogVerbosity = LogVerbosity.Verbose;
            Assert.AreEqual(LogVerbosity.Verbose, settings.LogVerbosity);
        }
        
        [Test]
        public void Clone_CreatesDeepCopy()
        {
            var original = new GlobalSettings
            {
                BaseOutputPath = "Custom/Path",
                DefaultFrameRate = 60,
                DefaultWidth = 3840,
                DefaultHeight = 2160,
                DefaultQuality = 100,
                UseSceneDirectory = true,
                OrganizeByTimeline = false,
                MaxConcurrentRecorders = 4,
                LogVerbosity = LogVerbosity.Verbose
            };
            
            var clone = original.Clone();
            
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.BaseOutputPath, clone.BaseOutputPath);
            Assert.AreEqual(original.DefaultFrameRate, clone.DefaultFrameRate);
            Assert.AreEqual(original.DefaultWidth, clone.DefaultWidth);
            Assert.AreEqual(original.DefaultHeight, clone.DefaultHeight);
            Assert.AreEqual(original.DefaultQuality, clone.DefaultQuality);
            Assert.AreEqual(original.UseSceneDirectory, clone.UseSceneDirectory);
            Assert.AreEqual(original.OrganizeByTimeline, clone.OrganizeByTimeline);
            Assert.AreEqual(original.MaxConcurrentRecorders, clone.MaxConcurrentRecorders);
            Assert.AreEqual(original.LogVerbosity, clone.LogVerbosity);
        }
        
        [Test]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            var original = new GlobalSettings();
            var clone = original.Clone();
            
            clone.BaseOutputPath = "Modified/Path";
            clone.DefaultFrameRate = 120;
            clone.LogVerbosity = LogVerbosity.Quiet;
            
            Assert.AreEqual("Assets/Recordings", original.BaseOutputPath);
            Assert.AreEqual(30, original.DefaultFrameRate);
            Assert.AreEqual(LogVerbosity.Normal, original.LogVerbosity);
        }
    }
}