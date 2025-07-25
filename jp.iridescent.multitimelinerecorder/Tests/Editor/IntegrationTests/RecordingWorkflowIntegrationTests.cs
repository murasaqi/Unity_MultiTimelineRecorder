using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for complete recording workflows
    /// </summary>
    [TestFixture]
    public class RecordingWorkflowIntegrationTests : TestFixtureBase
    {
        private Scene _testScene;
        private List<GameObject> _testObjects;
        private List<PlayableDirector> _testDirectors;
        
        // Services
        private ConfigurationService _configService;
        private TimelineService _timelineService;
        private RecordingService _recordingService;
        private GameObjectReferenceService _referenceService;
        private SignalEmitterService _signalEmitterService;
        private ConfigurationValidationService _validationService;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Initialize services
            _configService = new ConfigurationService(Logger);
            _timelineService = new TimelineService(Logger);
            _recordingService = new RecordingService(Logger, ErrorHandler);
            _referenceService = new GameObjectReferenceService(Logger, ErrorHandler);
            _signalEmitterService = new SignalEmitterService(Logger);
            _validationService = new ConfigurationValidationService(Logger, _referenceService);
            
            // Create test scene and objects
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _testScene.name = "IntegrationTestScene";
            _testObjects = new List<GameObject>();
            _testDirectors = new List<PlayableDirector>();
            
            // Create test timelines
            CreateTestTimelines();
        }
        
        [TearDown]
        public override void TearDown()
        {
            // Clean up test objects
            foreach (var obj in _testObjects)
            {
                if (obj != null)
                    GameObject.DestroyImmediate(obj);
            }
            
            // Clean up services
            _configService?.Dispose();
            _timelineService?.Dispose();
            _recordingService?.Dispose();
            _referenceService?.Dispose();
            _signalEmitterService?.Dispose();
            
            base.TearDown();
        }
        
        [Test]
        public void CompleteRecordingWorkflow_WithMultipleTimelines_ExecutesSuccessfully()
        {
            // Arrange - Create configuration with multiple timelines
            var config = CreateMultiTimelineConfiguration();
            
            // Act & Assert - Validate configuration
            var validationResult = _configService.ValidateConfiguration(config);
            Assert.IsTrue(validationResult.IsValid, 
                $"Validation failed: {string.Join(", ", validationResult.Errors)}");
            
            // Act - Apply global frame rate
            _configService.ApplyGlobalFrameRate(config);
            
            // Assert - All recorders should have unified frame rate
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                foreach (var recorder in timelineConfig.RecorderConfigs)
                {
                    Assert.AreEqual(config.FrameRate, recorder.FrameRate, 
                        "Frame rate not unified across all recorders");
                }
            }
            
            // Act - Execute recording (mock)
            var recordingResult = _recordingService.ExecuteRecording(_testDirectors, config);
            
            // Assert
            Assert.IsNotNull(recordingResult);
            AssertLogContains("Starting recording", LogLevel.Info);
        }
        
        [Test]
        public void ThreeColumnUIWorkflow_SelectTimelineAddRecordersConfigure_WorksCorrectly()
        {
            // Simulate the 3-column UI workflow
            
            // Column 1: Select timelines
            var selectedDirectors = new List<PlayableDirector> { _testDirectors[0], _testDirectors[1] };
            
            // Column 2: Add recorders to each timeline
            var config = _configService.CreateDefaultConfiguration();
            
            foreach (var director in selectedDirectors)
            {
                var timelineConfig = new TimelineRecorderConfig
                {
                    Director = director,
                    TimelineName = director.name,
                    IsEnabled = true
                };
                
                // Add multiple recorder types
                timelineConfig.RecorderConfigs.Add(CreateImageRecorder());
                timelineConfig.RecorderConfigs.Add(CreateMovieRecorder());
                
                config.TimelineConfigs.Add(timelineConfig);
            }
            
            // Column 3: Configure recorder settings
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                foreach (var recorder in timelineConfig.RecorderConfigs)
                {
                    // Apply timeline-specific settings
                    recorder.OutputPath = $"Recordings/{timelineConfig.TimelineName}/{recorder.Name}";
                    recorder.FrameRate = config.FrameRate; // Ensure frame rate consistency
                }
            }
            
            // Validate the complete configuration
            var validationResult = _validationService.ValidateConfiguration(config);
            Assert.IsTrue(validationResult.IsValid);
            
            // Save configuration
            var saveResult = _configService.SaveConfiguration(config, "Assets/Tests/3ColumnUITest.asset");
            Assert.IsTrue(saveResult);
        }
        
        [Test]
        public void GameObjectReferenceWorkflow_SaveAndRestoreAfterSceneReload_PreservesReferences()
        {
            // Arrange - Create GameObjects and references
            var targetGO = CreateTestGameObject("RecordingTarget");
            var cameraGO = CreateTestGameObject("MainCamera");
            cameraGO.AddComponent<Camera>();
            
            // Create references
            var targetRef = _referenceService.CreateReference(targetGO);
            var cameraRef = _referenceService.CreateReference(cameraGO);
            
            // Create recorder configuration with references
            var movieRecorder = new MovieRecorderConfiguration
            {
                Name = "Test Movie Recorder",
                TargetCamera = cameraRef,
                RecordingTarget = targetRef
            };
            
            // Save configuration
            var config = _configService.CreateDefaultConfiguration();
            var timelineConfig = new TimelineRecorderConfig();
            timelineConfig.RecorderConfigs.Add(movieRecorder);
            config.TimelineConfigs.Add(timelineConfig);
            
            var savePath = "Assets/Tests/ReferenceTest.asset";
            _configService.SaveConfiguration(config, savePath);
            
            // Act - Simulate scene reload
            // Destroy original objects
            GameObject.DestroyImmediate(targetGO);
            GameObject.DestroyImmediate(cameraGO);
            
            // Recreate with same names
            var newTargetGO = CreateTestGameObject("RecordingTarget");
            var newCameraGO = CreateTestGameObject("MainCamera");
            newCameraGO.AddComponent<Camera>();
            
            // Load configuration
            var loadedConfig = _configService.LoadConfiguration(savePath);
            
            // Restore references
            var loadedRecorder = loadedConfig.TimelineConfigs[0].RecorderConfigs[0] as MovieRecorderConfiguration;
            
            _referenceService.TryRestoreReference(loadedRecorder.TargetCamera, out var restoredCamera);
            _referenceService.TryRestoreReference(loadedRecorder.RecordingTarget, out var restoredTarget);
            
            // Assert
            Assert.IsNotNull(restoredCamera);
            Assert.IsNotNull(restoredTarget);
            Assert.AreEqual("MainCamera", restoredCamera.name);
            Assert.AreEqual("RecordingTarget", restoredTarget.name);
            
            // Cleanup
            if (System.IO.File.Exists(savePath))
            {
                AssetDatabase.DeleteAsset(savePath);
            }
        }
        
        [Test]
        public void SignalEmitterIntegration_RecordingWithSignals_UsesCorrectTimeRange()
        {
            // Arrange - Create timeline with signal emitters
            var director = _testDirectors[0];
            var timeline = director.playableAsset as TimelineAsset;
            
            // Note: In unit tests, we can't actually create SignalEmitters
            // but we can test the service logic
            
            var signalSettings = new SignalEmitterSettings
            {
                UseSignalEmitters = true,
                RecordOnlyBetweenSignals = true,
                StartSignalName = "RecordingStart",
                EndSignalName = "RecordingEnd",
                StartMarginFrames = 10,
                EndMarginFrames = 5
            };
            
            // Apply signal settings
            _signalEmitterService.ApplySignalEmitterSettings(director, signalSettings);
            
            // Get recording range
            var range = _signalEmitterService.GetRecordingRangeFromSignals(director, signalSettings);
            
            // Assert
            Assert.IsNotNull(range);
            AssertLogContains("Calculating recording range from signals", LogLevel.Info);
            
            // Create configuration with signal-based recording
            var config = _configService.CreateDefaultConfiguration();
            config.UseSignalEmitters = true;
            config.SignalEmitterSettings = signalSettings;
            
            var timelineConfig = new TimelineRecorderConfig
            {
                Director = director,
                TimelineName = director.name,
                RecordingStartTime = range.StartTime,
                RecordingEndTime = range.EndTime
            };
            
            config.TimelineConfigs.Add(timelineConfig);
            
            // Validate
            var validationResult = _configService.ValidateConfiguration(config);
            Assert.IsTrue(validationResult.IsValid);
        }
        
        [Test]
        public void ApplyToAllSelectedTimelines_WithMultipleSelections_AppliesCorrectly()
        {
            // Arrange - Create base recorder configuration
            var baseRecorder = new MovieRecorderConfiguration
            {
                Name = "Template Movie Recorder",
                Format = VideoFormat.MP4,
                FrameRate = 60,
                Width = 3840,
                Height = 2160,
                Codec = VideoCodec.H264,
                BitrateMode = BitrateMode.High
            };
            
            // Select multiple timelines
            var selectedIndices = new List<int> { 0, 1, 2 };
            var config = _configService.CreateDefaultConfiguration();
            
            // Add timeline configs
            foreach (int idx in selectedIndices)
            {
                if (idx < _testDirectors.Count)
                {
                    var timelineConfig = new TimelineRecorderConfig
                    {
                        Director = _testDirectors[idx],
                        TimelineName = _testDirectors[idx].name,
                        IsEnabled = true
                    };
                    config.TimelineConfigs.Add(timelineConfig);
                }
            }
            
            // Act - Apply recorder to all selected timelines
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                // Clone the base recorder for each timeline
                var clonedRecorder = baseRecorder.Clone() as MovieRecorderConfiguration;
                clonedRecorder.Name = $"{baseRecorder.Name} - {timelineConfig.TimelineName}";
                timelineConfig.RecorderConfigs.Add(clonedRecorder);
            }
            
            // Assert
            Assert.AreEqual(3, config.TimelineConfigs.Count);
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                Assert.AreEqual(1, timelineConfig.RecorderConfigs.Count);
                var recorder = timelineConfig.RecorderConfigs[0] as MovieRecorderConfiguration;
                Assert.IsNotNull(recorder);
                Assert.AreEqual(VideoFormat.MP4, recorder.Format);
                Assert.AreEqual(60, recorder.FrameRate);
                Assert.AreEqual(3840, recorder.Width);
                Assert.AreEqual(2160, recorder.Height);
            }
            
            // Validate frame rate consistency
            var validationResult = _validationService.ValidateConfiguration(config);
            Assert.IsTrue(validationResult.IsValid);
        }
        
        [Test]
        public void ErrorHandlingIntegration_WithInvalidConfiguration_ProducesUserFriendlyErrors()
        {
            // Arrange - Create intentionally invalid configuration
            var config = _configService.CreateDefaultConfiguration();
            config.FrameRate = -1; // Invalid
            config.Resolution = new Resolution(0, 0); // Invalid
            
            // Add timeline with mismatched frame rates
            var timeline1 = new TimelineRecorderConfig { TimelineName = "Timeline1" };
            var recorder1 = new MovieRecorderConfiguration { FrameRate = 30 };
            timeline1.RecorderConfigs.Add(recorder1);
            
            var timeline2 = new TimelineRecorderConfig { TimelineName = "Timeline2" };
            var recorder2 = new MovieRecorderConfiguration { FrameRate = 60 }; // Different!
            timeline2.RecorderConfigs.Add(recorder2);
            
            config.TimelineConfigs.Add(timeline1);
            config.TimelineConfigs.Add(timeline2);
            
            // Act - Validate with enhanced error handling
            var validationResult = _validationService.ValidateConfigurationWithSuggestions(config);
            
            // Assert
            Assert.IsFalse(validationResult.IsValid);
            Assert.IsTrue(validationResult.Issues.Count > 0);
            
            // Check for user-friendly error messages
            var frameRateIssue = validationResult.Issues.FirstOrDefault(
                i => i.Message.Contains("Frame rate"));
            Assert.IsNotNull(frameRateIssue);
            Assert.IsNotNull(frameRateIssue.SuggestedFix);
            Assert.IsTrue(frameRateIssue.SuggestedFix.Contains("unified frame rate"));
            
            var resolutionIssue = validationResult.Issues.FirstOrDefault(
                i => i.Message.Contains("Resolution"));
            Assert.IsNotNull(resolutionIssue);
            Assert.IsNotNull(resolutionIssue.SuggestedFix);
        }
        
        // Helper methods
        
        private void CreateTestTimelines()
        {
            for (int i = 0; i < 3; i++)
            {
                var go = CreateTestGameObject($"Timeline_{i}");
                var director = go.AddComponent<PlayableDirector>();
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = $"TestTimeline_{i}";
                director.playableAsset = timeline;
                _testDirectors.Add(director);
            }
        }
        
        private GameObject CreateTestGameObject(string name)
        {
            var go = new GameObject(name);
            _testObjects.Add(go);
            return go;
        }
        
        private RecordingConfiguration CreateMultiTimelineConfiguration()
        {
            var config = _configService.CreateDefaultConfiguration();
            config.Name = "Multi-Timeline Test Config";
            config.FrameRate = 30;
            config.Resolution = new Resolution(1920, 1080);
            
            // Add multiple timelines with different recorder types
            foreach (var director in _testDirectors)
            {
                var timelineConfig = new TimelineRecorderConfig
                {
                    Director = director,
                    TimelineName = director.name,
                    IsEnabled = true
                };
                
                // Mix of recorder types
                timelineConfig.RecorderConfigs.Add(CreateImageRecorder());
                timelineConfig.RecorderConfigs.Add(CreateMovieRecorder());
                
                config.TimelineConfigs.Add(timelineConfig);
            }
            
            return config;
        }
        
        private ImageRecorderConfiguration CreateImageRecorder()
        {
            return new ImageRecorderConfiguration
            {
                Name = "Test Image Sequence",
                IsEnabled = true,
                Format = ImageFormat.PNG,
                FrameRate = 30,
                Width = 1920,
                Height = 1080
            };
        }
        
        private MovieRecorderConfiguration CreateMovieRecorder()
        {
            return new MovieRecorderConfiguration
            {
                Name = "Test Movie",
                IsEnabled = true,
                Format = VideoFormat.MP4,
                FrameRate = 30,
                Width = 1920,
                Height = 1080
            };
        }
    }
}