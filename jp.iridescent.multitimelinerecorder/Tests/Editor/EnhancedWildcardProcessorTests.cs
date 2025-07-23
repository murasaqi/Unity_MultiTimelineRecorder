using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Tests.Editor
{
    [TestFixture]
    public class EnhancedWildcardProcessorTests
    {
        private EnhancedWildcardProcessor _processor;
        private WildcardRegistry _registry;
        private TestLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _registry = new WildcardRegistry(_logger);
            _processor = new EnhancedWildcardProcessor(_registry, _logger);

            // Register default wildcards
            RegisterDefaultWildcards();
        }

        [Test]
        public void ProcessPattern_WithSingleWildcard_ResolvesCorrectly()
        {
            // Arrange
            var pattern = "Recording_<Scene>_Final";
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "MainMenu"
            };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("Recording_MainMenu_Final", result);
        }

        [Test]
        public void ProcessPattern_WithMultipleWildcards_ResolvesAll()
        {
            // Arrange
            var pattern = "<Scene>_<Timeline>_<RecorderType>_<Take>";
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "Level1",
                TimelineName = "CutsceneTimeline",
                RecorderType = "Image",
                TakeNumber = 5
            };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("Level1_CutsceneTimeline_Image_005", result);
        }

        [Test]
        public void ProcessPattern_WithNestedWildcards_ResolvesCorrectly()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Nested>",
                Resolver = ctx => "<Scene>_<Take>"
            });

            var pattern = "Output_<Nested>_Final";
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "TestScene",
                TakeNumber = 1
            };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("Output_TestScene_001_Final", result);
        }

        [Test]
        public void ProcessPattern_WithTimeWildcards_FormatsCorrectly()
        {
            // Arrange
            var pattern = "<Date>_<Time>";
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SessionStartTime = new DateTime(2025, 1, 15, 14, 30, 45)
            };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("2025-01-15_14-30-45", result);
        }

        [Test]
        public void ProcessPattern_WithConditionalWildcard_AppliesCondition()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<ConditionalScene>",
                Resolver = ctx => string.IsNullOrEmpty(ctx?.SceneName) ? "UnknownScene" : ctx.SceneName
            });

            var pattern = "Recording_<ConditionalScene>";

            // Act
            var resultWithScene = _processor.ProcessPattern(pattern, new Unity.MultiTimelineRecorder.WildcardContext { SceneName = "Level1" });
            var resultWithoutScene = _processor.ProcessPattern(pattern, new Unity.MultiTimelineRecorder.WildcardContext());

            // Assert
            Assert.AreEqual("Recording_Level1", resultWithScene);
            Assert.AreEqual("Recording_UnknownScene", resultWithoutScene);
        }

        [Test]
        public void ProcessPattern_WithUnknownWildcard_KeepsOriginal()
        {
            // Arrange
            var pattern = "Recording_<UnknownWildcard>_Final";
            var context = new Unity.MultiTimelineRecorder.WildcardContext();

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("Recording_<UnknownWildcard>_Final", result);
        }

        [Test]
        public void ProcessPattern_WithNullPattern_ReturnsEmpty()
        {
            // Act
            var result = _processor.ProcessPattern(null, null);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ProcessPattern_WithEmptyPattern_ReturnsEmpty()
        {
            // Act
            var result = _processor.ProcessPattern("", null);

            // Assert
            Assert.AreEqual("", result);
        }

        [Test]
        public void ProcessPattern_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var pattern = "File_<Scene>_[<Take>].ext";
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "Scene-1",
                TakeNumber = 42
            };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("File_Scene-1_[042].ext", result);
        }

        [Test]
        public void ProcessPattern_WithCustomWildcard_ResolvesCorrectly()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<ProjectName>",
                Resolver = _ => "MyAwesomeProject"
            });

            var pattern = "<ProjectName>_<Scene>_<Date>";
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "MainMenu",
                SessionStartTime = new DateTime(2025, 1, 20, 0, 0, 0)
            };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            Assert.AreEqual("MyAwesomeProject_MainMenu_2025-01-20", result);
        }

        [Test]
        public void ProcessMultiplePatterns_ProcessesAllPatterns()
        {
            // Arrange
            var patterns = new List<string>
            {
                "<Scene>_<Take>",
                "Recording_<Timeline>",
                "<Date>_<Time>"
            };

            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "Level1",
                TimelineName = "Intro",
                TakeNumber = 1,
                SessionStartTime = DateTime.Now
            };

            // Act
            var results = _processor.ProcessMultiplePatterns(patterns, context);

            // Assert
            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results[0].Contains("Level1_001"));
            Assert.IsTrue(results[1].Contains("Recording_Intro"));
            Assert.IsTrue(results[2].Contains("-")); // Date separator
        }

        [Test]
        public void ValidatePattern_WithValidPattern_ReturnsTrue()
        {
            // Arrange
            var pattern = "<Scene>_<Timeline>_<Take>";

            // Act
            var (isValid, errors) = _processor.ValidatePattern(pattern);

            // Assert
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ValidatePattern_WithInvalidCharacters_ReturnsFalse()
        {
            // Arrange
            var pattern = "File:<Scene>|<Take>";

            // Act
            var (isValid, errors) = _processor.ValidatePattern(pattern);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Count > 0);
            Assert.IsTrue(errors[0].Contains("invalid character"));
        }

        [Test]
        public void GetAvailableWildcards_ReturnsAllWildcards()
        {
            // Act
            var wildcards = _processor.GetAvailableWildcards();

            // Assert
            Assert.IsTrue(wildcards.Count > 0);
            Assert.IsTrue(wildcards.Exists(w => w.Key == "<Scene>"));
            Assert.IsTrue(wildcards.Exists(w => w.Key == "<Timeline>"));
            Assert.IsTrue(wildcards.Exists(w => w.Key == "<Take>"));
        }

        [Test]
        public void PreviewPattern_GeneratesPreview()
        {
            // Arrange
            var pattern = "<Scene>_<Timeline>_<RecorderType>_<Take>";

            // Act
            var preview = _processor.PreviewPattern(pattern);

            // Assert
            Assert.IsNotNull(preview);
            Assert.IsTrue(preview.Contains("_")); // Should contain separators
            Assert.IsFalse(preview.Contains("<")); // Should not contain wildcard markers
        }

        [Test]
        public void ExtractWildcardsFromPattern_ExtractsAllWildcards()
        {
            // Arrange
            var pattern = "Output_<Scene>_<Timeline>_<Take>_<Date>_<Time>.mp4";

            // Act
            var wildcards = _processor.ExtractWildcardsFromPattern(pattern);

            // Assert
            Assert.AreEqual(5, wildcards.Count);
            Assert.Contains("<Scene>", wildcards);
            Assert.Contains("<Timeline>", wildcards);
            Assert.Contains("<Take>", wildcards);
            Assert.Contains("<Date>", wildcards);
            Assert.Contains("<Time>", wildcards);
        }

        [Test]
        public void ProcessPattern_WithRecursiveWildcard_HandlesGracefully()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Recursive>",
                Resolver = ctx => _processor.ProcessPattern("<Scene>_<Recursive>", ctx)
            });

            var pattern = "<Recursive>";
            var context = new Unity.MultiTimelineRecorder.WildcardContext { SceneName = "Test" };

            // Act
            var result = _processor.ProcessPattern(pattern, context);

            // Assert
            // Should handle recursion gracefully
            Assert.IsNotNull(result);
        }

        private void RegisterDefaultWildcards()
        {
            // Scene wildcard
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Scene>",
                Category = "Scene",
                Description = "Current scene name",
                Resolver = ctx => ctx?.SceneName ?? "UnknownScene"
            });

            // Timeline wildcard
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Timeline>",
                Category = "Timeline",
                Description = "Timeline name",
                Resolver = ctx => ctx?.TimelineName ?? "Timeline"
            });

            // Take wildcard
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Take>",
                Category = "Recording",
                Description = "Take number",
                Resolver = ctx => (ctx?.TakeNumber ?? 1).ToString("D3")
            });

            // RecorderType wildcard
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<RecorderType>",
                Category = "Recording",
                Description = "Type of recorder",
                Resolver = ctx => ctx?.RecorderType ?? "Unknown"
            });

            // Date wildcard
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Date>",
                Category = "Time",
                Description = "Current date",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("yyyy-MM-dd")
            });

            // Time wildcard
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Time>",
                Category = "Time",
                Description = "Current time",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("HH-mm-ss")
            });
        }

        // Test logger implementation
        private class TestLogger : ILogger
        {
            private readonly List<string> _logs = new List<string>();

            public void Log(string message, LogLevel level, LogCategory category)
            {
                _logs.Add($"[{level}] {message}");
            }

            public void LogVerbose(string message, LogCategory category) => Log(message, LogLevel.Verbose, category);
            public void LogInfo(string message, LogCategory category) => Log(message, LogLevel.Info, category);
            public void LogWarning(string message, LogCategory category) => Log(message, LogLevel.Warning, category);
            public void LogError(string message, LogCategory category) => Log(message, LogLevel.Error, category);

            public bool HasWarning(string message) => _logs.Any(log => log.Contains(message) && log.StartsWith($"[{LogLevel.Warning}]"));
            public bool HasError(string message) => _logs.Any(log => log.Contains(message) && log.StartsWith($"[{LogLevel.Error}]"));
        }
    }
}