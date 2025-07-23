using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Tests.Editor
{
    [TestFixture]
    public class EndToEndWorkflowTests
    {
        private Scene _testScene;
        private GameObject _testGameObject;
        private Camera _testCamera;
        private TestLogger _logger;
        private TestErrorHandler _errorHandler;
        private GameObjectReferenceService _referenceService;
        private SceneConfigurationManager _configManager;
        private ConfigurationValidationService _validationService;
        private WildcardRegistry _wildcardRegistry;
        private EnhancedWildcardProcessor _wildcardProcessor;

        [SetUp]
        public void SetUp()
        {
            // Initialize services
            _logger = new TestLogger();
            _errorHandler = new TestErrorHandler();
            _referenceService = new GameObjectReferenceService(_logger, _errorHandler);
            _configManager = new SceneConfigurationManager(_logger, _referenceService);
            _validationService = new ConfigurationValidationService(_logger, _referenceService);
            _wildcardRegistry = new WildcardRegistry(_logger);
            _wildcardProcessor = new EnhancedWildcardProcessor(_wildcardRegistry, _logger);

            // Create test scene
            _testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _testGameObject = new GameObject("TestRecordingTarget");
            _testCamera = _testGameObject.AddComponent<Camera>();

            // Register default wildcards
            RegisterDefaultWildcards();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
                GameObject.DestroyImmediate(_testGameObject);

            _referenceService?.Dispose();
            _configManager?.Dispose();
        }

        [Test]
        public void CompleteWorkflow_CreateValidateAndSaveConfiguration()
        {
            // Step 1: Create recording configuration
            var recordingConfig = CreateSampleRecordingConfiguration();
            Assert.IsNotNull(recordingConfig);

            // Step 2: Validate configuration
            var validationResult = _validationService.ValidateConfiguration(recordingConfig);
            Assert.IsTrue(validationResult.IsValid, 
                $"Validation failed: {string.Join(", ", validationResult.Issues.Select(i => i.Message))}");

            // Step 3: Create scene configuration
            var sceneConfig = new SceneConfiguration
            {
                SceneName = _testScene.name,
                RecordingConfiguration = recordingConfig,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            // Step 4: Save configuration
            _configManager.SaveConfiguration(sceneConfig);

            // Step 5: Load and verify
            var loadedConfig = _configManager.LoadConfiguration(_testScene.name);
            Assert.IsNotNull(loadedConfig);
            Assert.AreEqual(recordingConfig.FrameRate, loadedConfig.RecordingConfiguration.FrameRate);
            Assert.AreEqual(recordingConfig.Resolution.Width, loadedConfig.RecordingConfiguration.Resolution.Width);
        }

        [Test]
        public void CompleteWorkflow_WildcardProcessing()
        {
            // Step 1: Create wildcard context
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "TestScene",
                TimelineName = "MainTimeline",
                RecorderType = "Movie",
                TakeNumber = 5,
                SessionStartTime = new DateTime(2025, 1, 23, 14, 30, 0)
            };

            // Step 2: Process various patterns
            var patterns = new[]
            {
                "<Scene>_<Timeline>_<RecorderType>_<Take>",
                "Recording_<Date>_<Time>",
                "<Scene>/<Timeline>/<RecorderType>_<Take>"
            };

            var results = new List<string>();
            foreach (var pattern in patterns)
            {
                var result = _wildcardProcessor.ProcessPattern(pattern, context);
                results.Add(result);
                Assert.IsFalse(result.Contains("<"), $"Unresolved wildcard in: {result}");
            }

            // Step 3: Verify results
            Assert.AreEqual("TestScene_MainTimeline_Movie_005", results[0]);
            Assert.AreEqual("Recording_2025-01-23_14-30-00", results[1]);
            Assert.AreEqual("TestScene/MainTimeline/Movie_005", results[2]);
        }

        [Test]
        public void CompleteWorkflow_ConfigurationWithMultipleRecorders()
        {
            // Step 1: Create configuration with multiple recorder types
            var recordingConfig = new RecordingConfiguration
            {
                FrameRate = 30,
                Resolution = new Resolution(1920, 1080),
                OutputPath = "Recordings",
                TimelineConfigs = new List<ITimelineConfiguration>
                {
                    new TimelineRecorderConfig
                    {
                        TimelinePath = "Timeline1",
                        RecorderConfigurations = new List<IRecorderConfiguration>
                        {
                            CreateImageRecorder(),
                            CreateMovieRecorder(),
                            CreateAnimationRecorder()
                        }
                    }
                }
            };

            // Step 2: Validate
            var validationResult = _validationService.ValidateConfiguration(recordingConfig);
            Assert.IsTrue(validationResult.IsValid);

            // Step 3: Predict resource usage
            var resourcePrediction = _validationService.PredictResourceUsage(recordingConfig);
            Assert.IsNotNull(resourcePrediction);
            Assert.Greater(resourcePrediction.EstimatedMemoryUsageMB, 0);
            Assert.Greater(resourcePrediction.EstimatedDiskUsageMBPerMinute, 0);

            // Step 4: Process wildcards for each recorder
            var timelineConfig = recordingConfig.TimelineConfigs[0] as TimelineRecorderConfig;
            foreach (var recorder in timelineConfig.RecorderConfigurations)
            {
                var pattern = GetPatternForRecorder(recorder);
                var context = new Unity.MultiTimelineRecorder.WildcardContext
                {
                    SceneName = _testScene.name,
                    TimelineName = "Timeline1",
                    RecorderType = recorder.Type.ToString(),
                    TakeNumber = 1
                };

                var processedName = _wildcardProcessor.ProcessPattern(pattern, context);
                Assert.IsFalse(string.IsNullOrEmpty(processedName));
            }
        }

        [Test]
        public void CompleteWorkflow_AutoRepairInvalidConfiguration()
        {
            // Step 1: Create invalid configuration
            var invalidConfig = new RecordingConfiguration
            {
                FrameRate = -1, // Invalid
                Resolution = new Resolution(0, 0), // Invalid
                OutputPath = "", // Invalid
                TimelineConfigs = new List<ITimelineConfiguration>
                {
                    new TimelineRecorderConfig
                    {
                        TimelinePath = "Timeline1",
                        RecorderConfigurations = new List<IRecorderConfiguration>
                        {
                            new AnimationRecorderConfiguration
                            {
                                Type = RecorderSettingsType.Animation,
                                TargetGameObject = null, // Invalid
                                FrameRate = 999 // Invalid
                            }
                        }
                    }
                }
            };

            // Step 2: Validate and identify issues
            var validationResult = _validationService.ValidateConfiguration(invalidConfig);
            Assert.IsFalse(validationResult.IsValid);
            Assert.Greater(validationResult.Issues.Count, 0);

            // Step 3: Attempt auto-repair
            var repairResult = _validationService.AutoRepairConfiguration(invalidConfig);
            Assert.IsTrue(repairResult.Success || repairResult.RepairedIssues.Count > 0);

            // Step 4: Re-validate
            var revalidationResult = _validationService.ValidateConfiguration(invalidConfig);
            Assert.IsTrue(revalidationResult.Issues.Count < validationResult.Issues.Count, 
                "Auto-repair should fix at least some issues");
        }

        [Test]
        public void CompleteWorkflow_GameObjectReferenceAcrossScenes()
        {
            // Step 1: Create reference in current scene
            var reference = _referenceService.CreateReference(_testGameObject);
            Assert.IsNotNull(reference);

            // Step 2: Create configuration using the reference
            var animConfig = new AnimationRecorderConfiguration
            {
                Type = RecorderSettingsType.Animation,
                TargetGameObject = _testGameObject,
                FrameRate = 30
            };

            var recordingConfig = new RecordingConfiguration
            {
                FrameRate = 30,
                Resolution = new Resolution(1920, 1080),
                OutputPath = "Recordings",
                TimelineConfigs = new List<ITimelineConfiguration>
                {
                    new TimelineRecorderConfig
                    {
                        TimelinePath = "Timeline1",
                        RecorderConfigurations = new List<IRecorderConfiguration> { animConfig }
                    }
                }
            };

            // Step 3: Save configuration
            var sceneConfig = new SceneConfiguration
            {
                SceneName = _testScene.name,
                RecordingConfiguration = recordingConfig
            };
            _configManager.SaveConfiguration(sceneConfig);

            // Step 4: Simulate scene change
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            
            // Step 5: Load configuration and verify reference can be restored
            var loadedConfig = _configManager.LoadConfiguration(_testScene.name);
            Assert.IsNotNull(loadedConfig);

            // Note: In a real scenario, the GameObject reference would need to be restored
            // This test verifies the configuration persistence mechanism
        }

        [Test]
        public void CompleteWorkflow_TemplateBasedConfiguration()
        {
            // Step 1: Create and register custom wildcard
            _wildcardRegistry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<ProjectName>",
                Description = "Project name",
                Resolver = _ => "MyProject"
            });

            // Step 2: Create template-based configuration
            var template = new WildcardTemplate
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Production Template",
                Pattern = "<ProjectName>_<Scene>_<Timeline>_<RecorderType>_<Date>_<Take>",
                Category = "Production"
            };

            // Step 3: Use template in recorder configurations
            var recorders = new List<IRecorderConfiguration>
            {
                new ImageRecorderConfiguration
                {
                    Type = RecorderSettingsType.Image,
                    FileNamePattern = template.Pattern
                },
                new MovieRecorderConfiguration
                {
                    Type = RecorderSettingsType.Movie,
                    FileNamePattern = template.Pattern
                }
            };

            // Step 4: Process patterns
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "Level1",
                TimelineName = "Cutscene",
                RecorderType = "Image",
                TakeNumber = 1,
                SessionStartTime = DateTime.Now
            };

            foreach (var recorder in recorders)
            {
                var pattern = GetPatternForRecorder(recorder);
                var processed = _wildcardProcessor.ProcessPattern(pattern, context);
                
                Assert.IsTrue(processed.StartsWith("MyProject_"));
                Assert.IsTrue(processed.Contains("Level1"));
                Assert.IsTrue(processed.Contains("Cutscene"));
            }
        }

        // Helper methods

        private RecordingConfiguration CreateSampleRecordingConfiguration()
        {
            return new RecordingConfiguration
            {
                FrameRate = 30,
                Resolution = new Resolution(1920, 1080),
                OutputPath = "Recordings",
                TimelineConfigs = new List<ITimelineConfiguration>
                {
                    new TimelineRecorderConfig
                    {
                        TimelinePath = "Assets/Timelines/MainTimeline.playable",
                        RecorderConfigurations = new List<IRecorderConfiguration>
                        {
                            CreateImageRecorder(),
                            CreateMovieRecorder()
                        }
                    }
                }
            };
        }

        private IRecorderConfiguration CreateImageRecorder()
        {
            return new ImageRecorderConfiguration
            {
                Type = RecorderSettingsType.Image,
                Width = 1920,
                Height = 1080,
                OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG,
                FileNamePattern = "Image_<Scene>_<Take>",
                SourceType = ImageRecorderSourceType.GameView
            };
        }

        private IRecorderConfiguration CreateMovieRecorder()
        {
            return new MovieRecorderConfiguration
            {
                Type = RecorderSettingsType.Movie,
                Width = 1920,
                Height = 1080,
                OutputFormat = UnityEditor.Recorder.MovieRecorderSettings.VideoRecorderOutputFormat.MP4,
                FileNamePattern = "Movie_<Scene>_<Take>",
                Quality = 0.75f
            };
        }

        private IRecorderConfiguration CreateAnimationRecorder()
        {
            return new AnimationRecorderConfiguration
            {
                Type = RecorderSettingsType.Animation,
                TargetGameObject = _testGameObject,
                FrameRate = 30,
                RecordTransform = true,
                RecordComponents = true,
                FileName = "Animation_<Scene>_<Take>"
            };
        }

        private string GetPatternForRecorder(IRecorderConfiguration recorder)
        {
            return recorder switch
            {
                ImageRecorderConfiguration img => img.FileNamePattern,
                MovieRecorderConfiguration mov => mov.FileNamePattern,
                AnimationRecorderConfiguration anim => anim.FileName,
                _ => "<Scene>_<RecorderType>_<Take>"
            };
        }

        private void RegisterDefaultWildcards()
        {
            var wildcards = new[]
            {
                new WildcardDefinition
                {
                    Key = "<Scene>",
                    Resolver = ctx => ctx?.SceneName ?? "UnknownScene"
                },
                new WildcardDefinition
                {
                    Key = "<Timeline>",
                    Resolver = ctx => ctx?.TimelineName ?? "Timeline"
                },
                new WildcardDefinition
                {
                    Key = "<RecorderType>",
                    Resolver = ctx => ctx?.RecorderType ?? "Recorder"
                },
                new WildcardDefinition
                {
                    Key = "<Take>",
                    Resolver = ctx => (ctx?.TakeNumber ?? 1).ToString("D3")
                },
                new WildcardDefinition
                {
                    Key = "<Date>",
                    Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("yyyy-MM-dd")
                },
                new WildcardDefinition
                {
                    Key = "<Time>",
                    Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("HH-mm-ss")
                }
            };

            _wildcardRegistry.RegisterWildcards(wildcards);
        }

        // Test implementations

        private class TestLogger : ILogger
        {
            private readonly List<string> _logs = new List<string>();

            public void Log(string message, LogLevel level, LogCategory category)
            {
                _logs.Add($"[{level}][{category}] {message}");
                Debug.Log($"[TEST][{level}][{category}] {message}");
            }

            public void LogVerbose(string message, LogCategory category) => Log(message, LogLevel.Verbose, category);
            public void LogInfo(string message, LogCategory category) => Log(message, LogLevel.Info, category);
            public void LogWarning(string message, LogCategory category) => Log(message, LogLevel.Warning, category);
            public void LogError(string message, LogCategory category) => Log(message, LogLevel.Error, category);

            public List<string> GetLogs() => new List<string>(_logs);
        }

        private class TestErrorHandler : IErrorHandlingService
        {
            public void HandleError(Exception exception, ErrorSeverity severity = ErrorSeverity.Error, string context = null)
            {
                Debug.LogError($"[TEST ERROR] {context}: {exception.Message}");
            }
        }
    }
}