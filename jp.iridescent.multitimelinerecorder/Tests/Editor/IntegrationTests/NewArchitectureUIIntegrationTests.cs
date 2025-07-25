using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Windows;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests for new architecture's 3-column UI and recording workflow
    /// </summary>
    [TestFixture]
    public class NewArchitectureUIIntegrationTests : TestFixtureBase
    {
        private MainWindowView _mainWindow;
        private MainWindowController _mainController;
        private RecorderConfigurationController _recorderController;
        private IEventBus _eventBus;
        
        private Scene _testScene;
        private List<GameObject> _testObjects;
        private List<PlayableDirector> _testDirectors;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            
            // Initialize service locator
            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.Initialize();
            
            // Get services
            _mainController = serviceLocator.GetMainController();
            _recorderController = serviceLocator.GetRecorderController();
            _eventBus = serviceLocator.GetEventBus();
            
            // Create test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _testScene.name = "NewArchUITestScene";
            _testObjects = new List<GameObject>();
            _testDirectors = new List<PlayableDirector>();
            
            // Create test timelines
            CreateTestTimelines();
            
            // Open main window
            _mainWindow = EditorWindow.GetWindow<MainWindowView>("Multi Timeline Recorder Test");
        }
        
        [TearDown]
        public override void TearDown()
        {
            // Close window
            if (_mainWindow != null)
                _mainWindow.Close();
            
            // Clean up test objects
            foreach (var obj in _testObjects)
            {
                if (obj != null)
                    GameObject.DestroyImmediate(obj);
            }
            
            // Reset service locator
            ServiceLocator.ResetInstance();
            
            base.TearDown();
        }
        
        [Test]
        public void ThreeColumnUI_TimelineSelection_UpdatesRecorderList()
        {
            // Arrange
            _mainController.RefreshTimelines(true);
            var availableTimelines = _mainController.AvailableTimelines;
            Assert.Greater(availableTimelines.Count, 0, "No timelines found");
            
            // Act - Select first timeline
            _mainController.AddTimeline(availableTimelines[0]);
            
            // Publish selection event
            _eventBus.Publish(new TimelineSelectionChangedEvent
            {
                SelectedTimelines = _mainController.SelectedTimelines,
                SelectedIndex = 0
            });
            
            // Assert
            Assert.AreEqual(1, _mainController.SelectedTimelines.Count);
            Assert.AreEqual(availableTimelines[0], _mainController.SelectedTimelines[0]);
            AssertLogContains($"Added timeline: {availableTimelines[0].name}", LogLevel.Info);
            
            // Verify recorder controller received the timeline config
            Assert.IsNotNull(_recorderController.CurrentTimelineConfig);
        }
        
        [Test]
        public void ThreeColumnUI_AddMultipleRecorders_ConfiguresCorrectly()
        {
            // Arrange - Select timeline
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            // Get timeline config
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            var timelineConfig = config.TimelineConfigs[0] as TimelineRecorderConfig;
            _recorderController.SetTimelineConfig(timelineConfig);
            
            // Act - Add multiple recorder types
            _recorderController.AddRecorder(RecorderType.Image);
            _recorderController.AddRecorder(RecorderType.Movie);
            _recorderController.AddRecorder(RecorderType.Animation);
            
            // Assert
            Assert.AreEqual(3, timelineConfig.RecorderConfigs.Count);
            
            // Verify recorder types
            Assert.IsTrue(timelineConfig.RecorderConfigs.Any(r => r is ImageRecorderConfiguration));
            Assert.IsTrue(timelineConfig.RecorderConfigs.Any(r => r is MovieRecorderConfiguration));
            Assert.IsTrue(timelineConfig.RecorderConfigs.Any(r => r is AnimationRecorderConfiguration));
            
            // Verify all recorders have consistent frame rate
            foreach (var recorder in timelineConfig.RecorderConfigs)
            {
                Assert.AreEqual(config.GlobalSettings.DefaultFrameRate, recorder.FrameRate);
            }
        }
        
        [Test]
        public void ThreeColumnUI_RecorderSelection_UpdatesSettingsPanel()
        {
            // Arrange - Setup timeline and recorder
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            var timelineConfig = config.TimelineConfigs[0] as TimelineRecorderConfig;
            _recorderController.SetTimelineConfig(timelineConfig);
            _recorderController.AddRecorder(RecorderType.Movie);
            
            // Act - Select the recorder
            var recorder = timelineConfig.RecorderConfigs[0];
            _recorderController.SelectRecorder(recorder);
            
            // Assert
            Assert.AreEqual(recorder, _recorderController.SelectedRecorder);
            AssertLogContains($"Selected recorder: {recorder.Name}", LogLevel.Info);
            
            // Verify settings can be modified
            var movieRecorder = recorder as MovieRecorderConfiguration;
            Assert.IsNotNull(movieRecorder);
            
            // Modify settings
            movieRecorder.Width = 3840;
            movieRecorder.Height = 2160;
            movieRecorder.FrameRate = 60;
            
            // Verify changes persisted
            Assert.AreEqual(3840, movieRecorder.Width);
            Assert.AreEqual(2160, movieRecorder.Height);
            Assert.AreEqual(60, movieRecorder.FrameRate);
        }
        
        [Test]
        public async Task RecordingWorkflow_StartStopRecording_CompletesSuccessfully()
        {
            // Arrange - Setup timeline with recorder
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            var timelineConfig = config.TimelineConfigs[0] as TimelineRecorderConfig;
            _recorderController.SetTimelineConfig(timelineConfig);
            _recorderController.AddRecorder(RecorderType.Image);
            
            // Subscribe to recording events
            bool recordingStarted = false;
            bool recordingCompleted = false;
            RecordingProgress lastProgress = null;
            
            _eventBus.Subscribe<RecordingStartedEvent>(e => recordingStarted = true);
            _eventBus.Subscribe<RecordingCompletedEvent>(e => recordingCompleted = true);
            _eventBus.Subscribe<RecordingProgressEvent>(e => lastProgress = e.Progress);
            
            // Act - Start recording
            var canStart = _mainController.CanStartRecording();
            Assert.IsTrue(canStart, "Cannot start recording");
            
            _mainController.StartRecording();
            
            // Wait a bit for async operations
            await Task.Delay(100);
            
            // Stop recording
            _mainController.StopRecording();
            
            // Assert
            Assert.IsTrue(recordingStarted, "Recording started event not received");
            Assert.IsFalse(_mainController.IsRecording, "Recording should be stopped");
            
            // Note: In unit tests, actual recording won't complete, 
            // but we verify the workflow executes without errors
            AssertLogContains("Starting recording", LogLevel.Info);
            AssertLogContains("Stopping recording", LogLevel.Info);
        }
        
        [Test]
        public void RecordingWorkflow_ValidationBeforeRecording_ValidatesCorrectly()
        {
            // Arrange - Setup invalid configuration
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            config.GlobalSettings.DefaultFrameRate = -1; // Invalid
            
            // Act - Try to validate
            var validationResult = _mainController.ValidateConfiguration();
            
            // Assert
            Assert.IsFalse(validationResult.IsValid);
            Assert.Greater(validationResult.Errors.Count, 0);
            Assert.IsTrue(validationResult.Errors.Any(e => e.Contains("Frame rate")));
            
            // Verify cannot start recording with invalid config
            var canStart = _mainController.CanStartRecording();
            Assert.IsFalse(canStart, "Should not be able to start with invalid config");
        }
        
        [Test]
        public void ConfigurationPersistence_SaveAndReload_PreservesSettings()
        {
            // Arrange - Create configuration
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            var timelineConfig = config.TimelineConfigs[0] as TimelineRecorderConfig;
            _recorderController.SetTimelineConfig(timelineConfig);
            
            // Add and configure recorders
            _recorderController.AddRecorder(RecorderType.Movie);
            _recorderController.AddRecorder(RecorderType.Image);
            
            var movieRecorder = timelineConfig.RecorderConfigs[0] as MovieRecorderConfiguration;
            movieRecorder.Width = 2560;
            movieRecorder.Height = 1440;
            movieRecorder.BitrateMode = BitrateMode.High;
            
            // Set global settings
            config.GlobalSettings.DefaultFrameRate = 60;
            config.GlobalSettings.DefaultOutputPath.BaseDirectory = "TestOutput";
            
            // Act - Save configuration
            var savePath = "Assets/Tests/UIIntegrationTest.asset";
            _mainController.SaveConfiguration(savePath);
            
            // Clear and reload
            _mainController.CreateNewConfiguration();
            _mainController.LoadConfiguration(savePath);
            
            // Assert
            var loadedConfig = _mainController.CurrentConfiguration as RecordingConfiguration;
            Assert.IsNotNull(loadedConfig);
            Assert.AreEqual(60, loadedConfig.GlobalSettings.DefaultFrameRate);
            Assert.AreEqual("TestOutput", loadedConfig.GlobalSettings.DefaultOutputPath.BaseDirectory);
            
            Assert.AreEqual(1, loadedConfig.TimelineConfigs.Count);
            var loadedTimelineConfig = loadedConfig.TimelineConfigs[0] as TimelineRecorderConfig;
            Assert.AreEqual(2, loadedTimelineConfig.RecorderConfigs.Count);
            
            var loadedMovieRecorder = loadedTimelineConfig.RecorderConfigs[0] as MovieRecorderConfiguration;
            Assert.AreEqual(2560, loadedMovieRecorder.Width);
            Assert.AreEqual(1440, loadedMovieRecorder.Height);
            Assert.AreEqual(BitrateMode.High, loadedMovieRecorder.BitrateMode);
            
            // Cleanup
            if (System.IO.File.Exists(savePath))
            {
                AssetDatabase.DeleteAsset(savePath);
            }
        }
        
        [Test]
        public void UIStateSync_TimelineRemoval_UpdatesAllComponents()
        {
            // Arrange - Add multiple timelines
            _mainController.RefreshTimelines(true);
            var timelines = _mainController.AvailableTimelines.Take(2).ToList();
            foreach (var timeline in timelines)
            {
                _mainController.AddTimeline(timeline);
            }
            
            Assert.AreEqual(2, _mainController.SelectedTimelines.Count);
            
            // Act - Remove first timeline
            _mainController.RemoveTimeline(0);
            
            // Assert
            Assert.AreEqual(1, _mainController.SelectedTimelines.Count);
            Assert.AreEqual(timelines[1], _mainController.SelectedTimelines[0]);
            
            // Verify configuration updated
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            Assert.AreEqual(1, config.TimelineConfigs.Count);
            
            // Verify UI refresh event was published
            AssertLogContains("Publishing UI refresh event", LogLevel.Verbose);
        }
        
        [Test]
        public void GameObjectReference_SceneReload_RestoresReferences()
        {
            // Arrange - Create recorder with GameObject references
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            // Create camera for recorder
            var cameraGO = CreateTestGameObject("TestCamera");
            var camera = cameraGO.AddComponent<Camera>();
            
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            var timelineConfig = config.TimelineConfigs[0] as TimelineRecorderConfig;
            _recorderController.SetTimelineConfig(timelineConfig);
            _recorderController.AddRecorder(RecorderType.Movie);
            
            var movieRecorder = timelineConfig.RecorderConfigs[0] as MovieRecorderConfiguration;
            
            // Create reference using the service
            var referenceService = ServiceLocator.Instance.Get<IGameObjectReferenceService>();
            movieRecorder.TargetCamera = referenceService.CreateReference(cameraGO);
            
            // Save configuration
            var savePath = "Assets/Tests/ReferenceTest.asset";
            _mainController.SaveConfiguration(savePath);
            
            // Act - Simulate scene reload by destroying and recreating
            GameObject.DestroyImmediate(cameraGO);
            _testObjects.Remove(cameraGO);
            
            // Recreate with same name
            var newCameraGO = CreateTestGameObject("TestCamera");
            newCameraGO.AddComponent<Camera>();
            
            // Load configuration
            _mainController.LoadConfiguration(savePath);
            
            // Restore references
            var loadedConfig = _mainController.CurrentConfiguration as RecordingConfiguration;
            var loadedRecorder = loadedConfig.TimelineConfigs[0].RecorderConfigs[0] as MovieRecorderConfiguration;
            
            GameObject restoredCamera;
            var restored = referenceService.TryRestoreReference(loadedRecorder.TargetCamera, out restoredCamera);
            
            // Assert
            Assert.IsTrue(restored, "Failed to restore camera reference");
            Assert.IsNotNull(restoredCamera);
            Assert.AreEqual("TestCamera", restoredCamera.name);
            Assert.IsNotNull(restoredCamera.GetComponent<Camera>());
            
            // Cleanup
            if (System.IO.File.Exists(savePath))
            {
                AssetDatabase.DeleteAsset(savePath);
            }
        }
        
        [Test]
        public void ErrorRecovery_InvalidRecorderSettings_ShowsUserFriendlyError()
        {
            // Arrange
            _mainController.RefreshTimelines(true);
            var timeline = _mainController.AvailableTimelines[0];
            _mainController.AddTimeline(timeline);
            
            var config = _mainController.CurrentConfiguration as RecordingConfiguration;
            var timelineConfig = config.TimelineConfigs[0] as TimelineRecorderConfig;
            _recorderController.SetTimelineConfig(timelineConfig);
            _recorderController.AddRecorder(RecorderType.Movie);
            
            var movieRecorder = timelineConfig.RecorderConfigs[0] as MovieRecorderConfiguration;
            
            // Act - Set invalid settings
            movieRecorder.Width = 0; // Invalid
            movieRecorder.Height = 0; // Invalid
            movieRecorder.FrameRate = -1; // Invalid
            
            // Validate
            var validationResult = _mainController.ValidateConfiguration();
            
            // Assert
            Assert.IsFalse(validationResult.IsValid);
            
            // Check for user-friendly error messages
            Assert.IsTrue(validationResult.Errors.Any(e => e.Contains("resolution") || e.Contains("Resolution")));
            Assert.IsTrue(validationResult.Errors.Any(e => e.Contains("frame rate") || e.Contains("Frame rate")));
            
            // Verify suggestions are provided
            if (validationResult is ValidationResultWithSuggestions enhanced)
            {
                Assert.Greater(enhanced.Issues.Count, 0);
                var resolutionIssue = enhanced.Issues.FirstOrDefault(i => i.Message.Contains("resolution"));
                Assert.IsNotNull(resolutionIssue?.SuggestedFix);
            }
        }
        
        // Helper methods
        
        private void CreateTestTimelines()
        {
            for (int i = 0; i < 3; i++)
            {
                var go = CreateTestGameObject($"TestTimeline_{i}");
                var director = go.AddComponent<PlayableDirector>();
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = $"Timeline_{i}";
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
    }
}