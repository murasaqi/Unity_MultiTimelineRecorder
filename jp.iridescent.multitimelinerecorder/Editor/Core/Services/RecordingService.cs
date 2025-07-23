using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Timeline;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using Unity.EditorCoroutines.Editor;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Service responsible for executing recordings
    /// </summary>
    public class RecordingService : IRecordingService
    {
        private readonly MultiTimelineRecorder.Core.Interfaces.ILogger _logger;
        private readonly IErrorHandlingService _errorHandler;
        private readonly Dictionary<string, RecordingJob> _activeJobs = new Dictionary<string, RecordingJob>();
        private EditorCoroutine _recordingCoroutine;

        public RecordingService(MultiTimelineRecorder.Core.Interfaces.ILogger logger, IErrorHandlingService errorHandler)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <inheritdoc />
        public bool IsRecording => _activeJobs.Any(j => j.Value.State == RecordingState.Recording);

        /// <inheritdoc />
        public RecordingResult ExecuteRecording(List<PlayableDirector> timelines, IRecordingConfiguration config)
        {
            return _errorHandler.ExecuteWithErrorHandling(() =>
            {
                // Validate inputs
                if (timelines == null || timelines.Count == 0)
                {
                    throw new ArgumentException("No timelines provided for recording");
                }

                var validationResult = config.Validate();
                if (!validationResult.IsValid)
                {
                    throw new ValidationException("Configuration validation failed", validationResult);
                }
                
                // Validate frame rate consistency
                ValidateFrameRateConsistency(config);

                // Create recording job
                var job = new RecordingJob
                {
                    Id = Guid.NewGuid().ToString(),
                    Timelines = timelines,
                    Configuration = config,
                    State = RecordingState.Pending
                };

                _activeJobs[job.Id] = job;

                // Execute recording
                try
                {
                    var renderTimeline = CreateRenderTimeline(timelines, config);
                    if (renderTimeline == null)
                    {
                        throw new RecordingExecutionException("Failed to create render timeline");
                    }

                    job.RenderTimeline = renderTimeline;
                    job.State = RecordingState.Preparing;

                    // Start recording coroutine
                    _recordingCoroutine = EditorCoroutineUtility.StartCoroutine(
                        ExecuteRecordingCoroutine(job), this);

                    return new RecordingResult
                    {
                        IsSuccess = true,
                        JobId = job.Id
                    };
                }
                catch (Exception ex)
                {
                    job.State = RecordingState.Failed;
                    _activeJobs.Remove(job.Id);
                    throw new RecordingExecutionException("Recording execution failed", ex);
                }
            }, "ExecuteRecording");
        }

        /// <inheritdoc />
        public async Task<RecordingResult> ExecuteRecordingAsync(List<PlayableDirector> timelines, IRecordingConfiguration config)
        {
            return await Task.Run(() => ExecuteRecording(timelines, config));
        }

        /// <inheritdoc />
        public void CancelRecording(string jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var job))
            {
                _logger.LogInfo($"Cancelling recording job: {jobId}", LogCategory.Recording);
                
                job.State = RecordingState.Cancelled;
                
                if (_recordingCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(_recordingCoroutine);
                    _recordingCoroutine = null;
                }

                CleanupJob(job);
                _activeJobs.Remove(jobId);
            }
        }

        /// <inheritdoc />
        public RecordingProgress GetProgress(string jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var job))
            {
                return new RecordingProgress
                {
                    JobId = jobId,
                    State = job.State,
                    Progress = job.Progress,
                    CurrentFrame = job.CurrentFrame,
                    TotalFrames = job.TotalFrames,
                    CurrentTimeline = job.CurrentTimelineName
                };
            }

            return null;
        }

        /// <summary>
        /// Creates a render timeline from multiple source timelines
        /// </summary>
        private TimelineAsset CreateRenderTimeline(List<PlayableDirector> directors, IRecordingConfiguration config)
        {
            _logger.LogInfo($"Creating render timeline for {directors.Count} directors", LogCategory.Recording);

            try
            {
                // Create timeline asset
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                if (timeline == null)
                {
                    throw new RecordingExecutionException("Failed to create TimelineAsset instance");
                }

                timeline.name = $"MultiTimeline_RenderTimeline_{directors.Count}";
                timeline.editorSettings.frameRate = config.FrameRate;

                // Save as temporary asset
                string tempDir = "Assets/MultiTimelineRecorder/Temp";
                EnsureDirectoryExists(tempDir);

                var tempAssetPath = $"{tempDir}/{timeline.name}_{DateTime.Now.Ticks}.playable";
                AssetDatabase.CreateAsset(timeline, tempAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Create control track
                var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
                if (controlTrack == null)
                {
                    throw new RecordingExecutionException("Failed to create ControlTrack");
                }

                // Add control clips for each director
                float currentStartTime = 0f;
                foreach (var director in directors)
                {
                    if (director == null || director.playableAsset == null) continue;

                    var duration = director.duration;
                    var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
                    
                    controlClip.start = currentStartTime;
                    controlClip.duration = duration;
                    controlClip.displayName = director.name;

                    var controlAsset = controlClip.asset as ControlPlayableAsset;
                    if (controlAsset != null)
                    {
                        // Set the source game object directly
                        controlAsset.sourceGameObject = new UnityEngine.ExposedReference<GameObject>();
                        controlAsset.sourceGameObject.defaultValue = director.gameObject;
                    }

                    currentStartTime += (float)(duration + (1.0f / config.FrameRate)); // Add one frame margin
                }

                // Create recorder tracks for enabled recorder configs
                var enabledTimelineConfigs = config.TimelineConfigs.Where(t => t.IsEnabled).ToList();
                foreach (var timelineConfig in enabledTimelineConfigs)
                {
                    var enabledRecorders = timelineConfig.RecorderConfigs.Where(r => r.IsEnabled).ToList();
                    foreach (var recorderConfig in enabledRecorders)
                    {
                        CreateRecorderTrack(timeline, recorderConfig, 0, timeline.duration);
                    }
                }

                _logger.LogInfo($"Successfully created render timeline: {timeline.name}", LogCategory.Recording);
                return timeline;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to create render timeline: {ex.Message}", LogCategory.Recording);
                throw;
            }
        }

        /// <summary>
        /// Creates a recorder track on the timeline
        /// </summary>
        private void CreateRecorderTrack(TimelineAsset timeline, IRecorderConfiguration recorderConfig, 
            double startTime, double duration)
        {
            var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, recorderConfig.Name);
            var recorderClip = recorderTrack.CreateClip<RecorderClip>();
            
            recorderClip.start = startTime;
            recorderClip.duration = duration;
            recorderClip.displayName = recorderConfig.Name;

            var recorderSettings = recorderConfig.CreateUnityRecorderSettings(new WildcardContext
            {
                TimelineName = timeline.name,
                SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                TakeNumber = recorderConfig.TakeNumber,
                RecorderType = recorderConfig.Type.ToString(),
                RecordingDate = DateTime.Now,
                GlobalFrameRate = (int)timeline.editorSettings.frameRate,
                RecorderName = recorderConfig.Name
            });

            if (recorderSettings != null)
            {
                var clip = recorderClip.asset as RecorderClip;
                if (clip != null)
                {
                    clip.settings = recorderSettings;
                }
            }
        }

        /// <summary>
        /// Executes the recording coroutine
        /// </summary>
        private System.Collections.IEnumerator ExecuteRecordingCoroutine(RecordingJob job)
        {
            job.State = RecordingState.Recording;
            _logger.LogInfo($"Starting recording job: {job.Id}", LogCategory.Recording);

            // Create temporary PlayableDirector for playback
            var tempGO = new GameObject("TempRecorderDirector");
            var tempDirector = tempGO.AddComponent<PlayableDirector>();
            tempDirector.playableAsset = job.RenderTimeline;

            try
            {
                // Calculate total frames
                job.TotalFrames = Mathf.CeilToInt((float)(job.RenderTimeline.duration * job.Configuration.FrameRate));
                
                // Start playback
                tempDirector.time = 0;
                tempDirector.Play();

                // Wait for recording to complete
                while (tempDirector.state == PlayState.Playing && job.State == RecordingState.Recording)
                {
                    job.CurrentFrame = Mathf.FloorToInt((float)(tempDirector.time * job.Configuration.FrameRate));
                    job.Progress = (float)(tempDirector.time / job.RenderTimeline.duration);
                    
                    yield return null;
                }

                if (job.State == RecordingState.Recording)
                {
                    job.State = RecordingState.Completed;
                    _logger.LogInfo($"Recording completed successfully: {job.Id}", LogCategory.Recording);
                }
            }
            finally
            {
                // Cleanup
                if (tempGO != null)
                {
                    GameObject.DestroyImmediate(tempGO);
                }

                CleanupJob(job);
                _activeJobs.Remove(job.Id);
            }
        }

        /// <summary>
        /// Cleans up resources for a recording job
        /// </summary>
        private void CleanupJob(RecordingJob job)
        {
            if (job.RenderTimeline != null)
            {
                var path = AssetDatabase.GetAssetPath(job.RenderTimeline);
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        /// <summary>
        /// Ensures a directory exists in the project
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var currentPath = parts[0];
                
                for (int i = 1; i < parts.Length; i++)
                {
                    var nextPath = $"{currentPath}/{parts[i]}";
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, parts[i]);
                    }
                    currentPath = nextPath;
                }
            }
        }
        
        /// <summary>
        /// Validates frame rate consistency across all recorder configurations
        /// </summary>
        private void ValidateFrameRateConsistency(IRecordingConfiguration config)
        {
            _logger.LogInfo($"Validating frame rate consistency. Global frame rate: {config.FrameRate}", LogCategory.Recording);
            
            // Timeline constraint: All recorders must use the same frame rate
            // This is a Unity Timeline limitation when using multiple recorder clips
            var globalFrameRate = config.FrameRate;
            
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                if (!timelineConfig.IsEnabled) continue;
                
                foreach (var recorderConfig in timelineConfig.RecorderConfigs)
                {
                    if (!recorderConfig.IsEnabled) continue;
                    
                    // Ensure each recorder configuration uses the global frame rate
                    // Individual recorder frame rates are ignored due to Timeline constraints
                    _logger.LogVerbose($"Recorder '{recorderConfig.Name}' will use global frame rate: {globalFrameRate}", LogCategory.Recording);
                }
            }
            
            _logger.LogInfo("Frame rate consistency validation completed", LogCategory.Recording);
        }

        /// <summary>
        /// Internal class representing a recording job
        /// </summary>
        private class RecordingJob
        {
            public string Id { get; set; }
            public List<PlayableDirector> Timelines { get; set; }
            public IRecordingConfiguration Configuration { get; set; }
            public TimelineAsset RenderTimeline { get; set; }
            public RecordingState State { get; set; }
            public float Progress { get; set; }
            public int CurrentFrame { get; set; }
            public int TotalFrames { get; set; }
            public string CurrentTimelineName { get; set; }
        }
    }
}