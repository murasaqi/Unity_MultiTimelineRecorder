using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Models
{
    /// <summary>
    /// Unit tests for TimelineRecorderConfig
    /// </summary>
    [TestFixture]
    public class TimelineRecorderConfigTests
    {
        private PlayableDirector _testDirector;
        
        [SetUp]
        public void SetUp()
        {
            _testDirector = TestDataBuilder.CreateTestDirector();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testDirector != null)
            {
                GameObject.DestroyImmediate(_testDirector.gameObject);
            }
        }
        
        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            var config = new TimelineRecorderConfig();
            
            Assert.IsNotNull(config.Id);
            Assert.IsTrue(config.IsEnabled);
            Assert.IsNotNull(config.RecorderConfigs);
            Assert.IsEmpty(config.RecorderConfigs);
        }
        
        [Test]
        public void Constructor_WithDirector_SetsTimelineName()
        {
            var config = new TimelineRecorderConfig { Director = _testDirector };
            
            Assert.AreEqual(_testDirector.name, config.TimelineName);
        }
        
        [Test]
        public void Validate_WithValidConfig_ReturnsSuccess()
        {
            var config = TestDataBuilder.CreateTestTimelineConfig(_testDirector);
            config.RecorderConfigs.Add(TestDataBuilder.CreateTestImageRecorderConfig());
            
            var result = config.Validate();
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }
        
        [Test]
        public void Validate_WithNullDirector_ReturnsError()
        {
            var config = new TimelineRecorderConfig { Director = null };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Director is not assigned")));
        }
        
        [Test]
        public void Validate_WithEmptyTimelineName_ReturnsError()
        {
            var config = new TimelineRecorderConfig 
            { 
                Director = _testDirector,
                TimelineName = ""
            };
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Timeline name cannot be empty")));
        }
        
        [Test]
        public void Validate_WithNoRecorders_ReturnsWarning()
        {
            var config = new TimelineRecorderConfig 
            { 
                Director = _testDirector 
            };
            
            var result = config.Validate();
            
            Assert.IsTrue(result.IsValid); // Still valid, just with warning
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("No recorder configurations")));
        }
        
        [Test]
        public void Validate_WithAllRecordersDisabled_ReturnsWarning()
        {
            var config = TestDataBuilder.CreateTestTimelineConfig(_testDirector);
            
            var recorder1 = TestDataBuilder.CreateTestImageRecorderConfig();
            recorder1.IsEnabled = false;
            
            var recorder2 = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder2.IsEnabled = false;
            
            config.RecorderConfigs.Add(recorder1);
            config.RecorderConfigs.Add(recorder2);
            
            var result = config.Validate();
            
            Assert.IsTrue(result.IsValid);
            Assert.IsTrue(result.Warnings.Any(w => w.Contains("All recorders are disabled")));
        }
        
        [Test]
        public void Validate_WithInvalidRecorder_ReturnsError()
        {
            var config = TestDataBuilder.CreateTestTimelineConfig(_testDirector);
            
            var invalidRecorder = TestDataBuilder.CreateTestImageRecorderConfig();
            invalidRecorder.FrameRate = -1; // Invalid
            config.RecorderConfigs.Add(invalidRecorder);
            
            var result = config.Validate();
            
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("Recorder configuration validation failed")));
        }
        
        [Test]
        public void AddRecorderConfig_AddsToList()
        {
            var config = new TimelineRecorderConfig();
            var recorder = TestDataBuilder.CreateTestImageRecorderConfig();
            
            config.RecorderConfigs.Add(recorder);
            
            Assert.AreEqual(1, config.RecorderConfigs.Count);
            Assert.Contains(recorder, config.RecorderConfigs);
        }
        
        [Test]
        public void RemoveRecorderConfig_RemovesFromList()
        {
            var config = new TimelineRecorderConfig();
            var recorder = TestDataBuilder.CreateTestImageRecorderConfig();
            config.RecorderConfigs.Add(recorder);
            
            config.RecorderConfigs.Remove(recorder);
            
            Assert.IsEmpty(config.RecorderConfigs);
        }
        
        [Test]
        public void GetActiveRecorders_ReturnsOnlyEnabled()
        {
            var config = new TimelineRecorderConfig();
            
            var recorder1 = TestDataBuilder.CreateTestImageRecorderConfig();
            recorder1.IsEnabled = true;
            
            var recorder2 = TestDataBuilder.CreateTestMovieRecorderConfig();
            recorder2.IsEnabled = false;
            
            var recorder3 = TestDataBuilder.CreateTestImageRecorderConfig();
            recorder3.IsEnabled = true;
            
            config.RecorderConfigs.Add(recorder1);
            config.RecorderConfigs.Add(recorder2);
            config.RecorderConfigs.Add(recorder3);
            
            var activeRecorders = config.RecorderConfigs.Where(r => r.IsEnabled).ToList();
            
            Assert.AreEqual(2, activeRecorders.Count);
            Assert.Contains(recorder1, activeRecorders);
            Assert.Contains(recorder3, activeRecorders);
            Assert.IsFalse(activeRecorders.Contains(recorder2));
        }
        
        [Test]
        public void FindRecorderById_WithValidId_ReturnsRecorder()
        {
            var config = new TimelineRecorderConfig();
            var recorder = TestDataBuilder.CreateTestImageRecorderConfig();
            config.RecorderConfigs.Add(recorder);
            
            var found = config.RecorderConfigs.FirstOrDefault(r => r.Id == recorder.Id);
            
            Assert.IsNotNull(found);
            Assert.AreEqual(recorder, found);
        }
        
        [Test]
        public void FindRecorderById_WithInvalidId_ReturnsNull()
        {
            var config = new TimelineRecorderConfig();
            var recorder = TestDataBuilder.CreateTestImageRecorderConfig();
            config.RecorderConfigs.Add(recorder);
            
            var found = config.RecorderConfigs.FirstOrDefault(r => r.Id == "invalid-id");
            
            Assert.IsNull(found);
        }
        
        [Test]
        public void DirectorPlayableAsset_WithNullDirector_DoesNotThrow()
        {
            var config = new TimelineRecorderConfig { Director = null };
            
            Assert.DoesNotThrow(() => 
            {
                var asset = config.Director?.playableAsset;
            });
        }
        
        [Test]
        public void HasMultipleEnabledRecorders_ReturnsCorrectResult()
        {
            var config = new TimelineRecorderConfig();
            
            // No recorders
            Assert.IsFalse(config.RecorderConfigs.Count(r => r.IsEnabled) > 1);
            
            // One enabled recorder
            config.RecorderConfigs.Add(TestDataBuilder.CreateTestImageRecorderConfig());
            Assert.IsFalse(config.RecorderConfigs.Count(r => r.IsEnabled) > 1);
            
            // Two enabled recorders
            config.RecorderConfigs.Add(TestDataBuilder.CreateTestMovieRecorderConfig());
            Assert.IsTrue(config.RecorderConfigs.Count(r => r.IsEnabled) > 1);
        }
    }
}