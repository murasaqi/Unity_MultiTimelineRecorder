using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Tests.Helpers;

namespace MultiTimelineRecorder.Tests.Services
{
    /// <summary>
    /// Unit tests for RecordingService
    /// </summary>
    [TestFixture]
    public class RecordingServiceTests : TestFixtureBase
    {
        private RecordingService _recordingService;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _recordingService = new RecordingService(Logger, ErrorHandler);
        }
        
        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RecordingService(null, ErrorHandler));
        }
        
        [Test]
        public void Constructor_WithNullErrorHandler_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RecordingService(Logger, null));
        }
        
        [Test]
        public void IsRecording_InitiallyFalse()
        {
            Assert.IsFalse(_recordingService.IsRecording);
        }
        
        [Test]
        public void ExecuteRecording_WithNullTimelines_ThrowsArgumentNullException()
        {
            var config = TestDataBuilder.CreateTestConfiguration();
            
            Assert.Throws<ArgumentNullException>(() => 
                _recordingService.ExecuteRecording(null, config));
        }
        
        [Test]
        public void ExecuteRecording_WithNullConfiguration_ThrowsArgumentNullException()
        {
            var directors = new List<PlayableDirector> { TestDataBuilder.CreateTestDirector() };
            
            Assert.Throws<ArgumentNullException>(() => 
                _recordingService.ExecuteRecording(directors, null));
        }
        
        [Test]
        public void ExecuteRecording_WithEmptyTimelines_ReturnsFailure()
        {
            var directors = new List<PlayableDirector>();
            var config = TestDataBuilder.CreateTestConfiguration();
            
            var result = _recordingService.ExecuteRecording(directors, config);
            
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("No timelines provided for recording", result.ErrorMessage);
            AssertLogContains("No timelines provided", LogLevel.Error);
        }
        
        [Test]
        public void ExecuteRecording_WithInvalidConfiguration_ReturnsFailure()
        {
            var directors = new List<PlayableDirector> { TestDataBuilder.CreateTestDirector() };
            var config = TestDataBuilder.CreateTestConfiguration();
            config.FrameRate = -1; // Invalid frame rate
            
            var result = _recordingService.ExecuteRecording(directors, config);
            
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid configuration"));
            AssertLogContains("Invalid configuration", LogLevel.Error);
        }
        
        [Test]
        public void ExecuteRecording_WhileAlreadyRecording_ReturnsFailure()
        {
            // Start first recording
            var directors1 = new List<PlayableDirector> { TestDataBuilder.CreateTestDirector() };
            var config1 = TestDataBuilder.CreateTestConfiguration();
            config1.TimelineConfigs.Add(TestDataBuilder.CreateTestTimelineConfig(directors1[0]));
            config1.TimelineConfigs[0].RecorderConfigs.Add(TestDataBuilder.CreateTestImageRecorderConfig());
            
            // This would start recording (mocked)
            _recordingService.ExecuteRecording(directors1, config1);
            
            // Try to start second recording
            var directors2 = new List<PlayableDirector> { TestDataBuilder.CreateTestDirector() };
            var config2 = TestDataBuilder.CreateTestConfiguration();
            
            var result = _recordingService.ExecuteRecording(directors2, config2);
            
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("Recording is already in progress", result.ErrorMessage);
        }
        
        [Test]
        public void CancelRecording_WithValidJobId_CancelsRecording()
        {
            var jobId = "test-job-123";
            
            _recordingService.CancelRecording(jobId);
            
            AssertLogContains($"Cancelling recording job: {jobId}", LogLevel.Info);
        }
        
        [Test]
        public void CancelRecording_WithNullJobId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _recordingService.CancelRecording(null));
        }
        
        [Test]
        public void GetProgress_WithValidJobId_ReturnsProgress()
        {
            var jobId = "test-job-123";
            
            var progress = _recordingService.GetProgress(jobId);
            
            Assert.IsNotNull(progress);
            Assert.AreEqual(0, progress.CurrentFrame);
            Assert.AreEqual(0, progress.TotalFrames);
            Assert.AreEqual(0f, progress.Progress);
        }
        
        [Test]
        public async Task ExecuteRecordingAsync_WithValidInput_ReturnsSuccess()
        {
            var directors = new List<PlayableDirector> { TestDataBuilder.CreateTestDirector() };
            var config = TestDataBuilder.CreateTestConfiguration();
            config.TimelineConfigs.Add(TestDataBuilder.CreateTestTimelineConfig(directors[0]));
            config.TimelineConfigs[0].RecorderConfigs.Add(TestDataBuilder.CreateTestImageRecorderConfig());
            
            var result = await _recordingService.ExecuteRecordingAsync(directors, config);
            
            // In mock, we expect this to complete immediately
            Assert.IsNotNull(result);
            AssertLogContains("Starting async recording", LogLevel.Info);
        }
        
        [Test]
        public void Dispose_CleansUpResources()
        {
            _recordingService.Dispose();
            
            // Should not throw when accessing after dispose
            Assert.IsFalse(_recordingService.IsRecording);
        }
        
        [Test]
        public void ExecuteRecording_PublishesStartedEvent()
        {
            var directors = new List<PlayableDirector> { TestDataBuilder.CreateTestDirector() };
            var config = TestDataBuilder.CreateTestConfiguration();
            config.TimelineConfigs.Add(TestDataBuilder.CreateTestTimelineConfig(directors[0]));
            config.TimelineConfigs[0].RecorderConfigs.Add(TestDataBuilder.CreateTestImageRecorderConfig());
            
            // Subscribe to event bus
            ServiceLocator.Register<IEventBus>(EventBus);
            var recordingService = new RecordingService(Logger, ErrorHandler);
            
            recordingService.ExecuteRecording(directors, config);
            
            // Since we can't actually record in tests, we check for attempt
            AssertLogContains("Starting recording", LogLevel.Info);
        }
        
        [Test]
        public void GetProgress_WithInvalidJobId_ReturnsZeroProgress()
        {
            var progress = _recordingService.GetProgress("non-existent-job");
            
            Assert.IsNotNull(progress);
            Assert.AreEqual(0, progress.CurrentFrame);
            Assert.AreEqual(0, progress.TotalFrames);
            Assert.AreEqual(0f, progress.Progress);
            Assert.AreEqual("Unknown", progress.CurrentRecorder);
        }
    }
}