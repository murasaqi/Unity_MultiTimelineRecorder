using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Public API for programmatic control of Multi Timeline Recorder
    /// Provides UI-independent access to recording functionality
    /// </summary>
    public static class MultiTimelineRecorderAPI
    {
        private static bool _isInitialized = false;
        
        /// <summary>
        /// Ensures the API is initialized
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                ServiceLocator.Instance.Initialize();
                _isInitialized = true;
            }
        }
        
        #region Recording Execution
        
        /// <summary>
        /// Executes recording with the specified configuration
        /// </summary>
        /// <param name="config">Recording configuration</param>
        /// <returns>Recording result with job ID</returns>
        public static RecordingResult ExecuteRecording(RecordingConfiguration config)
        {
            EnsureInitialized();
            
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            
            // Extract timelines from configuration
            var timelines = new List<PlayableDirector>();
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                if (timelineConfig is TimelineRecorderConfig trc && trc.Director != null)
                {
                    timelines.Add(trc.Director);
                }
            }
            
            if (timelines.Count == 0)
            {
                throw new InvalidOperationException("No valid timelines found in configuration");
            }
            
            var recordingService = ServiceLocator.Instance.Get<IRecordingService>();
            return recordingService.ExecuteRecording(timelines, config);
        }
        
        /// <summary>
        /// Executes recording asynchronously with the specified configuration
        /// </summary>
        /// <param name="config">Recording configuration</param>
        /// <returns>Task with recording result</returns>
        public static async Task<RecordingResult> ExecuteRecordingAsync(RecordingConfiguration config)
        {
            EnsureInitialized();
            
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            
            // Extract timelines from configuration
            var timelines = new List<PlayableDirector>();
            foreach (var timelineConfig in config.TimelineConfigs)
            {
                if (timelineConfig is TimelineRecorderConfig trc && trc.Director != null)
                {
                    timelines.Add(trc.Director);
                }
            }
            
            if (timelines.Count == 0)
            {
                throw new InvalidOperationException("No valid timelines found in configuration");
            }
            
            var recordingService = ServiceLocator.Instance.Get<IRecordingService>();
            return await recordingService.ExecuteRecordingAsync(timelines, config);
        }
        
        /// <summary>
        /// Executes recording with quick settings
        /// </summary>
        /// <param name="timelines">List of timelines to record</param>
        /// <param name="outputPath">Output directory path</param>
        /// <param name="recorderType">Type of recorder to use</param>
        /// <param name="frameRate">Frame rate for recording</param>
        /// <returns>Recording result</returns>
        public static RecordingResult ExecuteRecording(
            List<PlayableDirector> timelines,
            string outputPath,
            RecorderType recorderType,
            int frameRate = 30)
        {
            EnsureInitialized();
            
            // Create configuration from parameters
            var config = CreateConfiguration();
            config.OutputPath = outputPath;
            config.FrameRate = frameRate;
            
            foreach (var timeline in timelines)
            {
                AddTimeline(config, timeline);
                var timelineConfig = config.TimelineConfigs.Last();
                AddRecorder(timelineConfig as TimelineRecorderConfig, recorderType);
            }
            
            return ExecuteRecording(config);
        }
        
        /// <summary>
        /// Cancels an active recording
        /// </summary>
        /// <param name="jobId">Recording job ID to cancel</param>
        public static void CancelRecording(string jobId)
        {
            EnsureInitialized();
            var recordingService = ServiceLocator.Instance.Get<IRecordingService>();
            recordingService.CancelRecording(jobId);
        }
        
        /// <summary>
        /// Gets the progress of an active recording
        /// </summary>
        /// <param name="jobId">Recording job ID</param>
        /// <returns>Recording progress information</returns>
        public static RecordingProgress GetProgress(string jobId)
        {
            EnsureInitialized();
            var recordingService = ServiceLocator.Instance.Get<IRecordingService>();
            return recordingService.GetProgress(jobId);
        }
        
        /// <summary>
        /// Checks if any recording is currently active
        /// </summary>
        public static bool IsRecording
        {
            get
            {
                EnsureInitialized();
                var recordingService = ServiceLocator.Instance.Get<IRecordingService>();
                return recordingService.IsRecording;
            }
        }
        
        #endregion
        
        #region Configuration Management
        
        /// <summary>
        /// Creates a new recording configuration with default settings
        /// </summary>
        /// <returns>New recording configuration</returns>
        public static RecordingConfiguration CreateConfiguration()
        {
            EnsureInitialized();
            var configService = ServiceLocator.Instance.Get<IConfigurationService>();
            return configService.GetDefaultConfiguration() as RecordingConfiguration ?? RecordingConfiguration.CreateDefault();
        }
        
        /// <summary>
        /// Saves a recording configuration to disk
        /// </summary>
        /// <param name="config">Configuration to save</param>
        /// <param name="path">Optional path (uses default if null)</param>
        public static void SaveConfiguration(RecordingConfiguration config, string path = null)
        {
            EnsureInitialized();
            var configService = ServiceLocator.Instance.Get<IConfigurationService>();
            configService.SaveConfiguration(config, path);
        }
        
        /// <summary>
        /// Loads a recording configuration from disk
        /// </summary>
        /// <param name="path">Optional path (uses default if null)</param>
        /// <returns>Loaded configuration</returns>
        public static RecordingConfiguration LoadConfiguration(string path = null)
        {
            EnsureInitialized();
            var configService = ServiceLocator.Instance.Get<IConfigurationService>();
            return configService.LoadConfiguration(path) as RecordingConfiguration;
        }
        
        /// <summary>
        /// Lists all saved configurations
        /// </summary>
        /// <returns>List of configuration information</returns>
        public static List<ConfigurationInfo> ListConfigurations()
        {
            EnsureInitialized();
            var configService = ServiceLocator.Instance.Get<IConfigurationService>();
            return configService.ListSavedConfigurations();
        }
        
        /// <summary>
        /// Exports configuration to JSON
        /// </summary>
        /// <param name="config">Configuration to export</param>
        /// <returns>JSON string</returns>
        public static string ExportConfiguration(RecordingConfiguration config)
        {
            EnsureInitialized();
            var configService = ServiceLocator.Instance.Get<IConfigurationService>();
            return configService.ExportConfiguration(config);
        }
        
        /// <summary>
        /// Imports configuration from JSON
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Imported configuration</returns>
        public static RecordingConfiguration ImportConfiguration(string json)
        {
            EnsureInitialized();
            var configService = ServiceLocator.Instance.Get<IConfigurationService>();
            return configService.ImportConfiguration(json) as RecordingConfiguration;
        }
        
        #endregion
        
        #region Timeline Management
        
        /// <summary>
        /// Scans for all available timelines in the project
        /// </summary>
        /// <param name="includeInactive">Include inactive GameObjects</param>
        /// <returns>List of PlayableDirectors with timelines</returns>
        public static List<PlayableDirector> ScanTimelines(bool includeInactive = false)
        {
            EnsureInitialized();
            var timelineService = ServiceLocator.Instance.Get<ITimelineService>();
            return timelineService.ScanAvailableTimelines(includeInactive);
        }
        
        /// <summary>
        /// Scans for timelines in a specific scene
        /// </summary>
        /// <param name="scenePath">Scene path (null for active scene)</param>
        /// <param name="includeInactive">Include inactive GameObjects</param>
        /// <returns>List of PlayableDirectors with timelines</returns>
        public static List<PlayableDirector> ScanSceneTimelines(string scenePath = null, bool includeInactive = false)
        {
            EnsureInitialized();
            var timelineService = ServiceLocator.Instance.Get<ITimelineService>();
            return timelineService.ScanSceneTimelines(scenePath, includeInactive);
        }
        
        /// <summary>
        /// Adds a timeline to the configuration
        /// </summary>
        /// <param name="config">Recording configuration</param>
        /// <param name="director">PlayableDirector to add</param>
        /// <returns>The created timeline configuration</returns>
        public static TimelineRecorderConfig AddTimeline(RecordingConfiguration config, PlayableDirector director)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            
            if (director == null)
            {
                throw new ArgumentNullException(nameof(director));
            }
            
            var timelineConfig = new TimelineRecorderConfig
            {
                Director = director,
                TimelineName = director.name,
                IsEnabled = true
            };
            
            config.AddTimelineConfig(timelineConfig);
            return timelineConfig;
        }
        
        /// <summary>
        /// Removes a timeline from the configuration
        /// </summary>
        /// <param name="config">Recording configuration</param>
        /// <param name="director">PlayableDirector to remove</param>
        public static void RemoveTimeline(RecordingConfiguration config, PlayableDirector director)
        {
            if (config == null || director == null) return;
            
            var timelineConfig = config.TimelineConfigs
                .OfType<TimelineRecorderConfig>()
                .FirstOrDefault(t => t.Director == director);
                
            if (timelineConfig != null)
            {
                // Remove from the list directly since RemoveTimelineConfig method may not exist
                config.TimelineConfigs.Remove(timelineConfig);
            }
        }
        
        #endregion
        
        #region Recorder Management
        
        /// <summary>
        /// Adds a recorder to a timeline configuration
        /// </summary>
        /// <param name="timeline">Timeline configuration</param>
        /// <param name="type">Type of recorder to add</param>
        /// <returns>The created recorder configuration</returns>
        public static IRecorderConfiguration AddRecorder(TimelineRecorderConfig timeline, RecorderType type)
        {
            if (timeline == null)
            {
                throw new ArgumentNullException(nameof(timeline));
            }
            
            IRecorderConfiguration recorder = type switch
            {
                RecorderType.Movie => new MovieRecorderConfiguration
                {
                    Name = "Movie Recorder",
                    IsEnabled = true,
                    Format = VideoFormat.MP4,
                    FrameRate = 30,
                    Width = 1920,
                    Height = 1080
                },
                RecorderType.Image => new ImageRecorderConfiguration
                {
                    Name = "Image Sequence",
                    IsEnabled = true,
                    Format = ImageFormat.PNG,
                    FrameRate = 30,
                    Width = 1920,
                    Height = 1080
                },
                RecorderType.Animation => new AnimationRecorderConfiguration
                {
                    Name = "Animation Recorder",
                    IsEnabled = true,
                    RecordTransform = true,
                    RecordComponents = true,
                    FrameRate = 30
                },
                // Audio recorder not yet implemented
                RecorderType.Audio => throw new NotImplementedException("Audio recorder not yet implemented"),
                RecorderType.AOV => new AOVRecorderConfiguration
                {
                    Name = "AOV Recorder",
                    IsEnabled = true,
                    AOVType = AOVType.Beauty,
                    OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR
                },
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            
            timeline.RecorderConfigs.Add(recorder);
            return recorder;
        }
        
        /// <summary>
        /// Removes a recorder from a timeline configuration
        /// </summary>
        /// <param name="timeline">Timeline configuration</param>
        /// <param name="recorderId">Recorder ID to remove</param>
        public static void RemoveRecorder(TimelineRecorderConfig timeline, string recorderId)
        {
            if (timeline == null || string.IsNullOrEmpty(recorderId)) return;
            
            var recorder = timeline.RecorderConfigs.FirstOrDefault(r => r.Id == recorderId);
            if (recorder != null)
            {
                timeline.RecorderConfigs.Remove(recorder);
            }
        }
        
        /// <summary>
        /// Removes a recorder from a timeline configuration
        /// </summary>
        /// <param name="timeline">Timeline configuration</param>
        /// <param name="recorder">Recorder to remove</param>
        public static void RemoveRecorder(TimelineRecorderConfig timeline, IRecorderConfiguration recorder)
        {
            if (timeline == null || recorder == null) return;
            timeline.RecorderConfigs.Remove(recorder);
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates a recording configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateConfiguration(RecordingConfiguration config)
        {
            if (config == null)
            {
                var result = new ValidationResult();
                result.AddError("Configuration is null");
                return result;
            }
            
            return config.Validate();
        }
        
        /// <summary>
        /// Validates a timeline
        /// </summary>
        /// <param name="director">PlayableDirector to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateTimeline(PlayableDirector director)
        {
            EnsureInitialized();
            var timelineService = ServiceLocator.Instance.Get<ITimelineService>();
            return timelineService.ValidateTimeline(director);
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Gets information about a timeline
        /// </summary>
        /// <param name="director">PlayableDirector</param>
        /// <returns>Timeline information</returns>
        public static TimelineInfo GetTimelineInfo(PlayableDirector director)
        {
            EnsureInitialized();
            var timelineService = ServiceLocator.Instance.Get<ITimelineService>();
            return timelineService.GetTimelineInfo(director);
        }
        
        /// <summary>
        /// Resets the API and clears all services
        /// </summary>
        public static void Reset()
        {
            ServiceLocator.ResetInstance();
            _isInitialized = false;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Enumeration of supported recorder types
    /// </summary>
    public enum RecorderType
    {
        Movie,
        Image,
        Animation,
        Audio,
        AOV
    }
}