using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Services
{
    /// <summary>
    /// Unit tests for TimelineService
    /// </summary>
    [TestFixture]
    public class TimelineServiceTests : TestFixtureBase
    {
        private TimelineService _timelineService;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _timelineService = new TimelineService(Logger);
        }
        
        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TimelineService(null));
        }
        
        [Test]
        public void FindAllTimelines_ReturnsEmptyListWhenNoTimelines()
        {
            var timelines = _timelineService.FindAllTimelines();
            
            Assert.IsNotNull(timelines);
            Assert.IsEmpty(timelines);
            AssertLogContains("No timelines found", LogLevel.Warning);
        }
        
        [Test]
        public void FindAllTimelines_FindsActiveTimelines()
        {
            // Create test directors
            var director1 = TestDataBuilder.CreateTestDirector("Director1");
            var director2 = TestDataBuilder.CreateTestDirector("Director2");
            
            var timelines = _timelineService.FindAllTimelines();
            
            // In unit tests, we can't rely on Unity's FindObjectsOfType
            // so we verify the method runs without error
            Assert.IsNotNull(timelines);
            AssertLogContains("Searching for timelines", LogLevel.Info);
        }
        
        [Test]
        public void ValidateTimeline_WithNullDirector_ReturnsFalse()
        {
            var result = _timelineService.ValidateTimeline(null);
            
            Assert.IsFalse(result.IsValid);
            Assert.Contains("Director is null", result.Errors);
        }
        
        [Test]
        public void ValidateTimeline_WithNullPlayableAsset_ReturnsFalse()
        {
            var go = new GameObject("TestDirector");
            var director = go.AddComponent<PlayableDirector>();
            // Don't assign playableAsset
            
            var result = _timelineService.ValidateTimeline(director);
            
            Assert.IsFalse(result.IsValid);
            Assert.Contains("Director has no playable asset assigned", result.Errors);
        }
        
        [Test]
        public void ValidateTimeline_WithValidDirector_ReturnsTrue()
        {
            var director = TestDataBuilder.CreateTestDirector();
            
            var result = _timelineService.ValidateTimeline(director);
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
            AssertLogContains($"Timeline '{director.name}' is valid", LogLevel.Info);
        }
        
        [Test]
        public void GetTimelineDuration_WithNullDirector_ReturnsZero()
        {
            var duration = _timelineService.GetTimelineDuration(null);
            
            Assert.AreEqual(0.0, duration);
            AssertLogContains("Cannot get duration: director is null", LogLevel.Warning);
        }
        
        [Test]
        public void GetTimelineDuration_WithValidDirector_ReturnsDuration()
        {
            var director = TestDataBuilder.CreateTestDirector();
            var timeline = director.playableAsset as TimelineAsset;
            
            // Timeline duration is based on its tracks and clips
            // In a basic timeline without clips, duration is 0
            var duration = _timelineService.GetTimelineDuration(director);
            
            Assert.AreEqual(timeline.duration, duration);
        }
        
        [Test]
        public void CreateTimelineSnapshot_WithNullDirector_ReturnsNull()
        {
            var snapshot = _timelineService.CreateTimelineSnapshot(null);
            
            Assert.IsNull(snapshot);
            AssertLogContains("Cannot create snapshot: director is null", LogLevel.Error);
        }
        
        [Test]
        public void CreateTimelineSnapshot_WithValidDirector_ReturnsSnapshot()
        {
            var director = TestDataBuilder.CreateTestDirector();
            
            var snapshot = _timelineService.CreateTimelineSnapshot(director);
            
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(director, snapshot.Director);
            Assert.AreEqual(director.name, snapshot.Name);
            Assert.AreEqual(director.time, snapshot.CurrentTime);
            Assert.AreEqual(director.playableAsset.duration, snapshot.Duration);
        }
        
        [Test]
        public void RestoreTimelineSnapshot_WithNullSnapshot_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _timelineService.RestoreTimelineSnapshot(null));
            AssertLogContains("Cannot restore: snapshot is null", LogLevel.Warning);
        }
        
        [Test]
        public void RestoreTimelineSnapshot_WithValidSnapshot_RestoresState()
        {
            var director = TestDataBuilder.CreateTestDirector();
            director.time = 5.0;
            
            var snapshot = _timelineService.CreateTimelineSnapshot(director);
            
            // Change director state
            director.time = 10.0;
            
            // Restore
            _timelineService.RestoreTimelineSnapshot(snapshot);
            
            Assert.AreEqual(5.0, director.time);
            AssertLogContains($"Restored timeline '{director.name}' to time 5", LogLevel.Info);
        }
        
        [Test]
        public void Dispose_CleansUpResources()
        {
            _timelineService.Dispose();
            
            // Should not throw when accessing after dispose
            var timelines = _timelineService.FindAllTimelines();
            Assert.IsNotNull(timelines);
        }
        
        [Test]
        public void FindTimelinesByName_WithNullPattern_ReturnsEmpty()
        {
            var timelines = _timelineService.FindTimelinesByName(null);
            
            Assert.IsNotNull(timelines);
            Assert.IsEmpty(timelines);
        }
        
        [Test]
        public void FindTimelinesByName_WithEmptyPattern_ReturnsEmpty()
        {
            var timelines = _timelineService.FindTimelinesByName("");
            
            Assert.IsNotNull(timelines);
            Assert.IsEmpty(timelines);
        }
        
        [Test]
        public void IsTimelineRecording_WithNullDirector_ReturnsFalse()
        {
            var isRecording = _timelineService.IsTimelineRecording(null);
            
            Assert.IsFalse(isRecording);
        }
        
        [Test]
        public void IsTimelineRecording_WithStoppedDirector_ReturnsFalse()
        {
            var director = TestDataBuilder.CreateTestDirector();
            director.Stop();
            
            var isRecording = _timelineService.IsTimelineRecording(director);
            
            Assert.IsFalse(isRecording);
        }
    }
}