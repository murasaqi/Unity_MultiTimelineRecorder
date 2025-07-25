using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Tests.Helpers
{
    /// <summary>
    /// Builder class for creating test data
    /// </summary>
    public static class TestDataBuilder
    {
        /// <summary>
        /// Creates a test PlayableDirector with Timeline
        /// </summary>
        public static PlayableDirector CreateTestDirector(string name = "TestDirector")
        {
            var go = new GameObject(name);
            var director = go.AddComponent<PlayableDirector>();
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = $"{name}_Timeline";
            director.playableAsset = timeline;
            return director;
        }
        
        /// <summary>
        /// Creates a test RecordingConfiguration
        /// </summary>
        public static RecordingConfiguration CreateTestConfiguration()
        {
            var config = new RecordingConfiguration
            {
                Name = "Test Configuration",
                FrameRate = 30,
                Resolution = new Resolution(1920, 1080),
                OutputPath = "Assets/Recordings/Test",
                GlobalSettings = CreateTestGlobalSettings()
            };
            
            return config;
        }
        
        /// <summary>
        /// Creates test GlobalSettings
        /// </summary>
        public static GlobalSettings CreateTestGlobalSettings()
        {
            return new GlobalSettings
            {
                BaseOutputPath = "Assets/Recordings",
                DefaultFrameRate = 30,
                DefaultWidth = 1920,
                DefaultHeight = 1080,
                UseSceneDirectory = false,
                OrganizeByTimeline = true,
                OrganizeByRecorderType = true,
                AutoCreateDirectories = true,
                ValidateBeforeRecording = true
            };
        }
        
        /// <summary>
        /// Creates a test TimelineRecorderConfig
        /// </summary>
        public static TimelineRecorderConfig CreateTestTimelineConfig(PlayableDirector director = null)
        {
            var config = new TimelineRecorderConfig
            {
                Director = director,
                TimelineName = director != null ? director.name : "TestTimeline",
                IsEnabled = true
            };
            
            return config;
        }
        
        /// <summary>
        /// Creates a test ImageRecorderConfiguration
        /// </summary>
        public static ImageRecorderConfiguration CreateTestImageRecorderConfig()
        {
            return new ImageRecorderConfiguration
            {
                Name = "Test Image Recorder",
                IsEnabled = true,
                Format = ImageFormat.PNG,
                Quality = 95,
                Width = 1920,
                Height = 1080,
                FrameRate = 30,
                FileNamePattern = "<Scene>_<Take>_<Frame>",
                FramePadding = 4,
                IncludeUI = false,
                CaptureAlpha = false,
                ColorSpace = ColorSpace.sRGB
            };
        }
        
        /// <summary>
        /// Creates a test MovieRecorderConfiguration
        /// </summary>
        public static MovieRecorderConfiguration CreateTestMovieRecorderConfig()
        {
            return new MovieRecorderConfiguration
            {
                Name = "Test Movie Recorder",
                IsEnabled = true,
                Format = VideoFormat.MP4,
                Codec = VideoCodec.H264,
                BitrateMode = BitrateMode.Medium,
                Width = 1920,
                Height = 1080,
                AspectRatio = AspectRatioMode.AspectRatio_16_9,
                FrameRate = 30,
                FileName = "<Scene>_<Take>",
                IncludeUI = false,
                CaptureAudio = true,
                AudioCodec = AudioCodec.AAC,
                AudioBitrate = 192,
                UseMotionBlur = false
            };
        }
        
        /// <summary>
        /// Creates a WildcardContext for testing
        /// </summary>
        public static WildcardContext CreateTestWildcardContext()
        {
            return new WildcardContext
            {
                TimelineName = "TestTimeline",
                SceneName = "TestScene",
                TakeNumber = 1,
                RecorderType = "Image",
                RecordingDate = DateTime.Now,
                CustomWildcards = new Dictionary<string, string>
                {
                    { "Width", "1920" },
                    { "Height", "1080" }
                }
            };
        }
        
        /// <summary>
        /// Creates a ValidationResult with errors for testing
        /// </summary>
        public static ValidationResult CreateInvalidResult(params string[] errors)
        {
            var result = new ValidationResult();
            foreach (var error in errors)
            {
                result.AddError(error);
            }
            return result;
        }
        
        /// <summary>
        /// Creates a ValidationResult with warnings for testing
        /// </summary>
        public static ValidationResult CreateWarningResult(params string[] warnings)
        {
            var result = new ValidationResult();
            foreach (var warning in warnings)
            {
                result.AddWarning(warning);
            }
            return result;
        }
    }
}