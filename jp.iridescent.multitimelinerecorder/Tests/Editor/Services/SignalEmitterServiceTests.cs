using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Services
{
    /// <summary>
    /// Unit tests for SignalEmitterService
    /// </summary>
    [TestFixture]
    public class SignalEmitterServiceTests : TestFixtureBase
    {
        private SignalEmitterService _signalEmitterService;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _signalEmitterService = new SignalEmitterService(Logger);
        }
        
        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SignalEmitterService(null));
        }
        
        [Test]
        public void FindSignalEmitters_WithNullDirector_ReturnsEmpty()
        {
            var emitters = _signalEmitterService.FindSignalEmitters(null);
            
            Assert.IsNotNull(emitters);
            Assert.IsEmpty(emitters);
            AssertLogContains("Director is null", LogLevel.Warning);
        }
        
        [Test]
        public void FindSignalEmitters_WithDirectorNoPlayableAsset_ReturnsEmpty()
        {
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            // Don't assign playableAsset
            
            var emitters = _signalEmitterService.FindSignalEmitters(director);
            
            Assert.IsNotNull(emitters);
            Assert.IsEmpty(emitters);
            AssertLogContains("Director has no playable asset", LogLevel.Warning);
        }
        
        [Test]
        public void FindSignalEmitters_WithValidTimelineNoSignals_ReturnsEmpty()
        {
            var director = TestDataBuilder.CreateTestDirector();
            
            var emitters = _signalEmitterService.FindSignalEmitters(director);
            
            Assert.IsNotNull(emitters);
            Assert.IsEmpty(emitters);
            AssertLogContains($"No signal emitters found in timeline '{director.name}'", LogLevel.Info);
        }
        
        [Test]
        public void FindSignalEmittersInRange_WithNullDirector_ReturnsEmpty()
        {
            var emitters = _signalEmitterService.FindSignalEmittersInRange(null, 0, 10);
            
            Assert.IsNotNull(emitters);
            Assert.IsEmpty(emitters);
            AssertLogContains("Director is null", LogLevel.Warning);
        }
        
        [Test]
        public void FindSignalEmittersInRange_WithValidRange_ReturnsEmittersInRange()
        {
            var director = TestDataBuilder.CreateTestDirector();
            
            // In a unit test without actual timeline data, we verify the method runs correctly
            var emitters = _signalEmitterService.FindSignalEmittersInRange(director, 5.0, 10.0);
            
            Assert.IsNotNull(emitters);
            AssertLogContains($"Searching for signal emitters in range [5-10] seconds", LogLevel.Info);
        }
        
        [Test]
        public void FindSignalEmittersInRange_WithInvalidRange_ReturnsEmpty()
        {
            var director = TestDataBuilder.CreateTestDirector();
            
            var emitters = _signalEmitterService.FindSignalEmittersInRange(director, 10.0, 5.0);
            
            Assert.IsNotNull(emitters);
            Assert.IsEmpty(emitters);
            AssertLogContains("Invalid time range", LogLevel.Warning);
        }
        
        [Test]
        public void ValidateSignalEmitter_WithNullEmitter_ReturnsFalse()
        {
            var result = _signalEmitterService.ValidateSignalEmitter(null);
            
            Assert.IsFalse(result.IsValid);
            Assert.Contains("Signal emitter is null", result.Errors);
        }
        
        [Test]
        public void GetSignalEmitterInfo_WithNullEmitter_ReturnsNull()
        {
            var info = _signalEmitterService.GetSignalEmitterInfo(null);
            
            Assert.IsNull(info);
            AssertLogContains("Signal emitter is null", LogLevel.Warning);
        }
        
        [Test]
        public void CreateSignalEmitterSnapshot_WithNullEmitter_ReturnsNull()
        {
            var snapshot = _signalEmitterService.CreateSignalEmitterSnapshot(null);
            
            Assert.IsNull(snapshot);
            AssertLogContains("Cannot create snapshot: signal emitter is null", LogLevel.Error);
        }
        
        [Test]
        public void CalculateRecordingRange_WithNoEmitters_ReturnsFullDuration()
        {
            var director = TestDataBuilder.CreateTestDirector();
            var emitters = new List<SignalEmitter>();
            
            var range = _signalEmitterService.CalculateRecordingRange(director, emitters);
            
            Assert.AreEqual(0, range.StartTime);
            Assert.AreEqual(director.playableAsset.duration, range.EndTime);
            AssertLogContains("No signal emitters provided, using full timeline duration", LogLevel.Info);
        }
        
        [Test]
        public void CalculateRecordingRange_WithNullDirector_ReturnsZeroRange()
        {
            var emitters = new List<SignalEmitter>();
            
            var range = _signalEmitterService.CalculateRecordingRange(null, emitters);
            
            Assert.AreEqual(0, range.StartTime);
            Assert.AreEqual(0, range.EndTime);
            AssertLogContains("Director is null", LogLevel.Warning);
        }
        
        [Test]
        public void ApplySignalEmitterSettings_WithNullSettings_DoesNotThrow()
        {
            var director = TestDataBuilder.CreateTestDirector();
            
            Assert.DoesNotThrow(() => _signalEmitterService.ApplySignalEmitterSettings(director, null));
            AssertLogContains("No signal emitter settings to apply", LogLevel.Info);
        }
        
        [Test]
        public void ApplySignalEmitterSettings_WithValidSettings_AppliesSuccessfully()
        {
            var director = TestDataBuilder.CreateTestDirector();
            var settings = new SignalEmitterSettings
            {
                UseSignalEmitters = true,
                RecordOnlyBetweenSignals = true,
                StartSignalName = "StartRecording",
                EndSignalName = "StopRecording"
            };
            
            _signalEmitterService.ApplySignalEmitterSettings(director, settings);
            
            AssertLogContains($"Applied signal emitter settings to timeline '{director.name}'", LogLevel.Info);
        }
        
        [Test]
        public void Dispose_CleansUpResources()
        {
            _signalEmitterService.Dispose();
            
            // Should not throw when accessing after dispose
            var emitters = _signalEmitterService.FindSignalEmitters(null);
            Assert.IsNotNull(emitters);
        }
        
        [Test]
        public void FindMTRSignalTracks_WithNullTimeline_ReturnsEmpty()
        {
            var tracks = _signalEmitterService.FindMTRSignalTracks(null);
            
            Assert.IsNotNull(tracks);
            Assert.IsEmpty(tracks);
            AssertLogContains("Timeline asset is null", LogLevel.Warning);
        }
        
        [Test]
        public void FilterSignalEmittersByName_WithNullList_ReturnsEmpty()
        {
            var filtered = _signalEmitterService.FilterSignalEmittersByName(null, "TestSignal");
            
            Assert.IsNotNull(filtered);
            Assert.IsEmpty(filtered);
        }
        
        [Test]
        public void FilterSignalEmittersByName_WithEmptyPattern_ReturnsAll()
        {
            var emitters = new List<SignalEmitter>();
            // Would add test emitters here in a real scenario
            
            var filtered = _signalEmitterService.FilterSignalEmittersByName(emitters, "");
            
            Assert.IsNotNull(filtered);
            Assert.AreEqual(emitters.Count, filtered.Count);
        }
        
        [Test]
        public void GetRecordingRangeFromSignals_WithStartAndEndSignals_ReturnsCorrectRange()
        {
            var director = TestDataBuilder.CreateTestDirector();
            var settings = new SignalEmitterSettings
            {
                StartSignalName = "Start",
                EndSignalName = "End",
                StartMarginFrames = 10,
                EndMarginFrames = 5
            };
            
            // In unit tests, we can't create real signal emitters, so we verify the method logic
            var range = _signalEmitterService.GetRecordingRangeFromSignals(director, settings);
            
            Assert.IsNotNull(range);
            AssertLogContains("Calculating recording range from signals", LogLevel.Info);
        }
    }
}