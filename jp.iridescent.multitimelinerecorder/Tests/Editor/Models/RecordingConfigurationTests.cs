using System;
using System.Linq;
using NUnit.Framework;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Models
{
    /// <summary>
    /// Unit tests for RecordingConfiguration
    /// </summary>
    [TestFixture]
    public class RecordingConfigurationTests
    {
        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            var config = new RecordingConfiguration();
            
            Assert.IsNotNull(config.Id);
            Assert.AreEqual("New Recording Configuration", config.Name);
            Assert.AreEqual(30, config.FrameRate);
            Assert.AreEqual(1920, config.Resolution.Width);
            Assert.AreEqual(1080, config.Resolution.Height);
            Assert.IsNotNull(config.TimelineConfigs);
            Assert.IsEmpty(config.TimelineConfigs);
            Assert.IsNotNull(config.GlobalSettings);
        }
        
        [Test]
        public void Validate_WithValidConfiguration_ReturnsSuccess()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            
            var result = config.Validate();
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }
        
        [Test]
        public void Validate_WithInvalidFrameRate_ReturnsError()
        {
            var config = new RecordingConfiguration { FrameRate = 0 };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Frame rate must be positive")));
        }
        
        [Test]
        public void Validate_WithNegativeFrameRate_ReturnsError()
        {
            var config = new RecordingConfiguration { FrameRate = -30 };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Frame rate must be positive")));
        }
        
        [Test]
        public void Validate_WithInvalidResolution_ReturnsError()
        {
            var config = new RecordingConfiguration 
            { 
                Resolution = new Resolution(0, 1080) 
            };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Resolution width and height must be positive")));
        }
        
        [Test]
        public void Validate_WithEmptyOutputPath_ReturnsError()
        {
            var config = new RecordingConfiguration { OutputPath = "" };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Output path cannot be empty")));
        }
        
        [Test]
        public void Validate_WithNullGlobalSettings_ReturnsError()
        {
            var config = new RecordingConfiguration { GlobalSettings = null };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Global settings cannot be null")));
        }
        
        [Test]
        public void Clone_CreatesDeepCopy()
        {
            var original = TestDataBuilder.CreateTestConfiguration();
            original.Name = "Original Config";
            original.FrameRate = 60;
            
            var timeline = TestDataBuilder.CreateTestTimelineConfig(null);
            timeline.TimelineName = "Test Timeline";
            original.TimelineConfigs.Add(timeline);
            
            var clone = original.Clone() as RecordingConfiguration;
            
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.Name, clone.Name);
            Assert.AreEqual(original.FrameRate, clone.FrameRate);
            Assert.AreEqual(original.TimelineConfigs.Count, clone.TimelineConfigs.Count);
            Assert.AreNotSame(original.TimelineConfigs[0], clone.TimelineConfigs[0]);
            Assert.AreNotSame(original.GlobalSettings, clone.GlobalSettings);
        }
        
        [Test]
        public void Clone_ModifyingClone_DoesNotAffectOriginal()
        {
            var original = TestDataBuilder.CreateTestConfiguration();
            original.Name = "Original";
            
            var clone = original.Clone() as RecordingConfiguration;
            clone.Name = "Modified Clone";
            clone.FrameRate = 120;
            
            Assert.AreEqual("Original", original.Name);
            Assert.AreEqual(30, original.FrameRate);
        }
        
        [Test]
        public void AddTimelineConfig_AddsToList()
        {
            var config = new RecordingConfiguration();
            var timeline = TestDataBuilder.CreateTestTimelineConfig(null);
            
            config.TimelineConfigs.Add(timeline);
            
            Assert.AreEqual(1, config.TimelineConfigs.Count);
            Assert.Contains(timeline, config.TimelineConfigs);
        }
        
        [Test]
        public void RemoveTimelineConfig_RemovesFromList()
        {
            var config = new RecordingConfiguration();
            var timeline = TestDataBuilder.CreateTestTimelineConfig(null);
            config.TimelineConfigs.Add(timeline);
            
            config.TimelineConfigs.Remove(timeline);
            
            Assert.IsEmpty(config.TimelineConfigs);
        }
        
        [Test]
        public void GetActiveTimelineConfigs_ReturnsOnlyEnabled()
        {
            var config = new RecordingConfiguration();
            
            var timeline1 = TestDataBuilder.CreateTestTimelineConfig(null);
            timeline1.IsEnabled = true;
            
            var timeline2 = TestDataBuilder.CreateTestTimelineConfig(null);
            timeline2.IsEnabled = false;
            
            var timeline3 = TestDataBuilder.CreateTestTimelineConfig(null);
            timeline3.IsEnabled = true;
            
            config.TimelineConfigs.Add(timeline1);
            config.TimelineConfigs.Add(timeline2);
            config.TimelineConfigs.Add(timeline3);
            
            var activeConfigs = config.TimelineConfigs.Where(t => t.IsEnabled).ToList();
            
            Assert.AreEqual(2, activeConfigs.Count);
            Assert.Contains(timeline1, activeConfigs);
            Assert.Contains(timeline3, activeConfigs);
            Assert.IsFalse(activeConfigs.Contains(timeline2));
        }
        
        [Test]
        public void Validate_WithInvalidTimelineConfig_ReturnsError()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            var timeline = TestDataBuilder.CreateTestTimelineConfig(null);
            timeline.TimelineName = ""; // Invalid
            config.TimelineConfigs.Add(timeline);
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Timeline configuration validation failed")));
        }
        
        [Test]
        public void Resolution_ToString_ReturnsFormattedString()
        {
            var resolution = new Resolution(1920, 1080);
            
            Assert.AreEqual("1920x1080", resolution.ToString());
        }
    }
}