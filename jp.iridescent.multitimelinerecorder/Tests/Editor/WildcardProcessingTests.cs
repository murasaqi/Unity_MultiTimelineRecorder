using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests
{
    /// <summary>
    /// Unit tests for wildcard processing functionality
    /// </summary>
    [TestFixture]
    public class WildcardProcessingTests : TestFixtureBase
    {
        private WildcardRegistry _registry;
        private EnhancedWildcardProcessor _processor;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _registry = new WildcardRegistry(Logger);
            _processor = new EnhancedWildcardProcessor(_registry, Logger);
            
            // Register default Multi Timeline Recorder wildcards
            RegisterMTRWildcards();
        }
        
        [Test]
        public void ProcessPattern_MTRWildcards_ResolvesCorrectly()
        {
            // Arrange
            var pattern = "<Timeline>_<TimelineTake>_<RecorderTake>_<RecorderName>";
            var context = new WildcardContext
            {
                TimelineName = "CutsceneTimeline",
                TimelineTakeNumber = 3,
                RecorderTakeNumber = 5,
                RecorderName = "MainCamera"
            };
            
            // Act
            var result = _processor.ProcessPattern(pattern, context);
            
            // Assert
            Assert.AreEqual("CutsceneTimeline_003_005_MainCamera", result);
            AssertLogContains("Processing Multi Timeline Recorder wildcard", LogLevel.Verbose);
        }
        
        [Test]
        public void ProcessPattern_UnityRecorderWildcards_PassesThrough()
        {
            // Arrange
            var pattern = "Recording_<Project>_<Resolution>_<Product>"; // Unity Recorder wildcards
            var context = new WildcardContext();
            
            // Act
            var result = _processor.ProcessPattern(pattern, context);
            
            // Assert
            Assert.AreEqual("Recording_<Project>_<Resolution>_<Product>", result); // Should pass through
            AssertLogContains("Passing through Unity Recorder wildcard", LogLevel.Verbose);
        }
        
        [Test]
        public void ProcessPattern_MixedWildcards_ProcessesOnlyMTR()
        {
            // Arrange
            var pattern = "<Timeline>_<Project>_<RecorderTake>_<Resolution>";
            var context = new WildcardContext
            {
                TimelineName = "Intro",
                RecorderTakeNumber = 1
            };
            
            // Act
            var result = _processor.ProcessPattern(pattern, context);
            
            // Assert
            Assert.AreEqual("Intro_<Project>_001_<Resolution>", result);
            // MTR wildcards processed, Unity wildcards passed through
        }
        
        [Test]
        public void ValidatePattern_WithCustomWildcard_AcceptsValid()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<CustomProject>",
                Category = "Custom",
                Description = "Custom project name",
                Resolver = _ => "MyProject"
            });
            
            var pattern = "<CustomProject>_<Timeline>_<Date>";
            
            // Act
            var (isValid, errors) = _processor.ValidatePattern(pattern);
            
            // Assert
            Assert.IsTrue(isValid);
            Assert.AreEqual(0, errors.Count);
        }
        
        [Test]
        public void ProcessPattern_WithTemplatePreset_AppliesTemplate()
        {
            // Arrange
            var templateRegistry = new TemplateRegistry(Logger);
            templateRegistry.RegisterTemplate(new WildcardTemplate
            {
                Name = "StandardNaming",
                Pattern = "<Scene>_<Timeline>_<RecorderType>_<Take>",
                Description = "Standard naming convention"
            });
            
            var pattern = templateRegistry.GetTemplate("StandardNaming").Pattern;
            var context = new WildcardContext
            {
                SceneName = "Level1",
                TimelineName = "BossFight",
                RecorderType = "Movie",
                TakeNumber = 2
            };
            
            // Act
            var result = _processor.ProcessPattern(pattern, context);
            
            // Assert
            Assert.AreEqual("Level1_BossFight_Movie_002", result);
        }
        
        [Test]
        public void WildcardManagementSettings_SaveAndLoad_PreservesCustomWildcards()
        {
            // Arrange
            var settings = new WildcardManagementSettings();
            settings.CustomWildcards.Add(new WildcardDefinition
            {
                Key = "<BuildNumber>",
                Category = "Build",
                Description = "Current build number",
                Resolver = _ => "1234"
            });
            
            // Act - Save
            settings.SaveSettings();
            
            // Act - Load
            var loaded = WildcardManagementSettings.LoadSettings();
            
            // Assert
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.CustomWildcards.Count);
            Assert.AreEqual("<BuildNumber>", loaded.CustomWildcards[0].Key);
        }
        
        [Test]
        public void ProcessPattern_WithDateTimeFormats_FormatsCorrectly()
        {
            // Arrange
            var testDate = new DateTime(2025, 1, 25, 14, 30, 45);
            var context = new WildcardContext
            {
                SessionStartTime = testDate
            };
            
            // Test various date/time patterns
            var patterns = new Dictionary<string, string>
            {
                { "<Date>", "2025-01-25" },
                { "<Time>", "14-30-45" },
                { "<DateTime>", "2025-01-25_14-30-45" },
                { "<Year>", "2025" },
                { "<Month>", "01" },
                { "<Day>", "25" }
            };
            
            foreach (var kvp in patterns)
            {
                // Act
                var result = _processor.ProcessPattern(kvp.Key, context);
                
                // Assert
                Assert.AreEqual(kvp.Value, result, $"Pattern {kvp.Key} failed");
            }
        }
        
        [Test]
        public void ProcessPattern_WithPaddedNumbers_PadsCorrectly()
        {
            // Test take number padding
            var contexts = new[]
            {
                (1, "001"),
                (10, "010"),
                (100, "100"),
                (1000, "1000") // Should not truncate
            };
            
            foreach (var (takeNumber, expected) in contexts)
            {
                // Arrange
                var context = new WildcardContext { TakeNumber = takeNumber };
                
                // Act
                var result = _processor.ProcessPattern("<Take>", context);
                
                // Assert
                Assert.AreEqual(expected, result, $"Take {takeNumber} padding failed");
            }
        }
        
        [Test]
        public void ProcessPattern_WithInvalidCharactersInResult_SanitizesOutput()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<BadPath>",
                Resolver = _ => "Path/With:Invalid*Characters?"
            });
            
            var pattern = "Output_<BadPath>_Final";
            
            // Act
            var result = _processor.ProcessPattern(pattern, new WildcardContext());
            
            // Assert
            Assert.IsFalse(result.Contains(":"));
            Assert.IsFalse(result.Contains("*"));
            Assert.IsFalse(result.Contains("?"));
            AssertLogContains("Sanitized wildcard output", LogLevel.Warning);
        }
        
        [Test]
        public void WildcardRegistry_GetWildcardsByCategory_ReturnsCorrectWildcards()
        {
            // Act
            var timelineWildcards = _registry.GetWildcardsByCategory("Timeline");
            var recordingWildcards = _registry.GetWildcardsByCategory("Recording");
            
            // Assert
            Assert.IsTrue(timelineWildcards.Count > 0);
            Assert.IsTrue(timelineWildcards.Exists(w => w.Key == "<Timeline>"));
            Assert.IsTrue(timelineWildcards.Exists(w => w.Key == "<TimelineTake>"));
            
            Assert.IsTrue(recordingWildcards.Count > 0);
            Assert.IsTrue(recordingWildcards.Exists(w => w.Key == "<RecorderTake>"));
        }
        
        [Test]
        public void ProcessPattern_WithNullContext_UsesDefaults()
        {
            // Arrange
            var pattern = "<Timeline>_<RecorderTake>";
            
            // Act
            var result = _processor.ProcessPattern(pattern, null);
            
            // Assert
            Assert.AreEqual("Timeline_001", result); // Should use default values
            AssertLogContains("Using default values for null context", LogLevel.Warning);
        }
        
        [Test]
        public void TemplateRegistry_ImportExport_PreservesTemplates()
        {
            // Arrange
            var registry = new TemplateRegistry(Logger);
            registry.RegisterTemplate(new WildcardTemplate
            {
                Name = "MovieTemplate",
                Pattern = "<Scene>_<Timeline>_Movie_<Take>",
                Description = "Template for movie recordings"
            });
            
            var exportPath = "Assets/Tests/templates_export.json";
            
            // Act - Export
            var exportResult = registry.ExportTemplates(exportPath);
            Assert.IsTrue(exportResult);
            
            // Act - Import
            var newRegistry = new TemplateRegistry(Logger);
            var importResult = newRegistry.ImportTemplates(exportPath);
            
            // Assert
            Assert.IsTrue(importResult);
            var imported = newRegistry.GetTemplate("MovieTemplate");
            Assert.IsNotNull(imported);
            Assert.AreEqual("<Scene>_<Timeline>_Movie_<Take>", imported.Pattern);
            
            // Cleanup
            if (System.IO.File.Exists(exportPath))
            {
                System.IO.File.Delete(exportPath);
            }
        }
        
        private void RegisterMTRWildcards()
        {
            // Multi Timeline Recorder specific wildcards
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Timeline>",
                Category = "Timeline",
                Description = "Timeline name",
                Resolver = ctx => ctx?.TimelineName ?? "Timeline"
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<TimelineTake>",
                Category = "Timeline",
                Description = "Timeline take number",
                Resolver = ctx => (ctx?.TimelineTakeNumber ?? 1).ToString("D3")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<RecorderTake>",
                Category = "Recording",
                Description = "Recorder take number",
                Resolver = ctx => (ctx?.RecorderTakeNumber ?? 1).ToString("D3")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<RecorderName>",
                Category = "Recording",
                Description = "Recorder name",
                Resolver = ctx => ctx?.RecorderName ?? "Recorder"
            });
            
            // Common wildcards
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Scene>",
                Category = "Scene",
                Description = "Current scene name",
                Resolver = ctx => ctx?.SceneName ?? "UnknownScene"
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Take>",
                Category = "Recording",
                Description = "Take number",
                Resolver = ctx => (ctx?.TakeNumber ?? 1).ToString("D3")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<RecorderType>",
                Category = "Recording",
                Description = "Type of recorder",
                Resolver = ctx => ctx?.RecorderType ?? "Unknown"
            });
            
            // Date/Time wildcards
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Date>",
                Category = "Time",
                Description = "Current date",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("yyyy-MM-dd")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Time>",
                Category = "Time",
                Description = "Current time",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("HH-mm-ss")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<DateTime>",
                Category = "Time",
                Description = "Current date and time",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("yyyy-MM-dd_HH-mm-ss")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Year>",
                Category = "Time",
                Description = "Current year",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("yyyy")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Month>",
                Category = "Time",
                Description = "Current month",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("MM")
            });
            
            _registry.RegisterWildcard(new WildcardDefinition
            {
                Key = "<Day>",
                Category = "Time",
                Description = "Current day",
                Resolver = ctx => (ctx?.SessionStartTime ?? DateTime.Now).ToString("dd")
            });
        }
    }
}