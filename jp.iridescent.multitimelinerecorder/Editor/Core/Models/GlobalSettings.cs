using System;
using UnityEngine;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Log verbosity levels
    /// </summary>
    public enum LogVerbosity
    {
        None,
        Error,
        Warning,
        Info,
        Debug
    }

    /// <summary>
    /// Global settings that apply to all recordings
    /// </summary>
    [Serializable]
    public class GlobalSettings : IGlobalSettings
    {
        [SerializeField]
        private int defaultFrameRate = 24;
        
        [SerializeField]
        private MultiTimelineRecorder.Core.Interfaces.Resolution defaultResolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(1920, 1080);
        
        [SerializeField]
        private OutputPathConfiguration defaultOutputPath = new OutputPathConfiguration();
        
        [SerializeField]
        private bool debugMode = false;
        
        [SerializeField]
        private bool autoSaveBeforeRecording = true;
        
        [SerializeField]
        private bool showPreviewWindow = true;
        
        [SerializeField]
        private bool validateBeforeRecording = true;
        
        [SerializeField]
        private bool openOutputFolderAfterRecording = false;
        
        [SerializeField]
        private int maxConcurrentRecorders = 1;
        
        [SerializeField]
        private bool useAsyncRecording = false;
        
        [SerializeField]
        private bool captureAudio = true;
        
        [SerializeField]
        private LogVerbosity logVerbosity = LogVerbosity.Info;
        
        [SerializeField]
        private bool useSignalEmitterTiming = false;
        
        [SerializeField]
        private string startSignalName = "";
        
        [SerializeField]
        private string endSignalName = "";

        /// <inheritdoc />
        public int DefaultFrameRate
        {
            get => defaultFrameRate;
            set => defaultFrameRate = Mathf.Clamp(value, 1, 120);
        }

        /// <inheritdoc />
        public MultiTimelineRecorder.Core.Interfaces.Resolution DefaultResolution
        {
            get => defaultResolution;
            set => defaultResolution = value;
        }

        /// <inheritdoc />
        public IOutputPathConfiguration DefaultOutputPath
        {
            get => defaultOutputPath;
            set => defaultOutputPath = value as OutputPathConfiguration ?? new OutputPathConfiguration();
        }

        /// <inheritdoc />
        public bool DebugMode
        {
            get => debugMode;
            set => debugMode = value;
        }

        /// <summary>
        /// Whether to auto-save before recording
        /// </summary>
        public bool AutoSaveBeforeRecording
        {
            get => autoSaveBeforeRecording;
            set => autoSaveBeforeRecording = value;
        }

        /// <summary>
        /// Whether to show preview window
        /// </summary>
        public bool ShowPreviewWindow
        {
            get => showPreviewWindow;
            set => showPreviewWindow = value;
        }

        /// <summary>
        /// Whether to validate before recording
        /// </summary>
        public bool ValidateBeforeRecording
        {
            get => validateBeforeRecording;
            set => validateBeforeRecording = value;
        }

        /// <summary>
        /// Whether to open output folder after recording
        /// </summary>
        public bool OpenOutputFolderAfterRecording
        {
            get => openOutputFolderAfterRecording;
            set => openOutputFolderAfterRecording = value;
        }

        /// <summary>
        /// Maximum concurrent recorders
        /// </summary>
        public int MaxConcurrentRecorders
        {
            get => maxConcurrentRecorders;
            set => maxConcurrentRecorders = Mathf.Clamp(value, 1, 4);
        }

        /// <summary>
        /// Whether to use async recording
        /// </summary>
        public bool UseAsyncRecording
        {
            get => useAsyncRecording;
            set => useAsyncRecording = value;
        }

        /// <summary>
        /// Whether to capture audio
        /// </summary>
        public bool CaptureAudio
        {
            get => captureAudio;
            set => captureAudio = value;
        }

        /// <summary>
        /// Log verbosity level
        /// </summary>
        public LogVerbosity LogVerbosity
        {
            get => logVerbosity;
            set => logVerbosity = value;
        }
        
        /// <summary>
        /// Whether to use SignalEmitter timing
        /// </summary>
        public bool UseSignalEmitterTiming
        {
            get => useSignalEmitterTiming;
            set => useSignalEmitterTiming = value;
        }
        
        /// <summary>
        /// Start signal name for SignalEmitter timing
        /// </summary>
        public string StartSignalName
        {
            get => startSignalName;
            set => startSignalName = value;
        }
        
        /// <summary>
        /// End signal name for SignalEmitter timing
        /// </summary>
        public string EndSignalName
        {
            get => endSignalName;
            set => endSignalName = value;
        }

        /// <summary>
        /// Validates the global settings
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate frame rate
            if (defaultFrameRate < 1 || defaultFrameRate > 120)
            {
                result.AddError($"Default frame rate must be between 1 and 120. Current value: {defaultFrameRate}");
            }

            // Validate resolution
            if (defaultResolution.Width < 1 || defaultResolution.Height < 1)
            {
                result.AddError($"Default resolution must be positive. Current value: {defaultResolution}");
            }

            // Validate output path
            if (defaultOutputPath != null)
            {
                var pathResult = defaultOutputPath.Validate();
                if (!pathResult.IsValid)
                {
                    foreach (var issue in pathResult.Issues)
                    {
                        if (issue.Severity == ValidationSeverity.Error)
                        {
                            result.AddError($"Output path: {issue.Message}");
                        }
                        else
                        {
                            result.AddWarning($"Output path: {issue.Message}");
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a clone of the global settings
        /// </summary>
        public IGlobalSettings Clone()
        {
            return new GlobalSettings
            {
                defaultFrameRate = this.defaultFrameRate,
                defaultResolution = this.defaultResolution,
                defaultOutputPath = this.defaultOutputPath?.Clone() as OutputPathConfiguration ?? new OutputPathConfiguration(),
                debugMode = this.debugMode,
                autoSaveBeforeRecording = this.autoSaveBeforeRecording,
                showPreviewWindow = this.showPreviewWindow,
                validateBeforeRecording = this.validateBeforeRecording,
                openOutputFolderAfterRecording = this.openOutputFolderAfterRecording,
                maxConcurrentRecorders = this.maxConcurrentRecorders,
                useAsyncRecording = this.useAsyncRecording,
                captureAudio = this.captureAudio,
                logVerbosity = this.logVerbosity,
                useSignalEmitterTiming = this.useSignalEmitterTiming,
                startSignalName = this.startSignalName,
                endSignalName = this.endSignalName
            };
        }

        /// <summary>
        /// Creates default global settings
        /// </summary>
        public static GlobalSettings CreateDefault()
        {
            return new GlobalSettings
            {
                defaultFrameRate = 24,
                defaultResolution = new MultiTimelineRecorder.Core.Interfaces.Resolution(1920, 1080),
                defaultOutputPath = OutputPathConfiguration.CreateDefault(),
                debugMode = false,
                autoSaveBeforeRecording = true,
                showPreviewWindow = true,
                validateBeforeRecording = true,
                openOutputFolderAfterRecording = false,
                maxConcurrentRecorders = 1,
                useAsyncRecording = false,
                captureAudio = true,
                logVerbosity = LogVerbosity.Info
            };
        }
    }

    /// <summary>
    /// Configuration for output path handling
    /// </summary>
    [Serializable]
    public class OutputPathConfiguration : IOutputPathConfiguration
    {
        [SerializeField]
        private string baseDirectory = "Recordings";
        
        [SerializeField]
        private bool createTimelineSubdirectories = true;
        
        [SerializeField]
        private bool createRecorderTypeSubdirectories = true;
        
        [SerializeField]
        private string filenamePattern = "<Scene>_<Timeline>_<RecorderType>_Take<Take>";

        /// <inheritdoc />
        public string BaseDirectory
        {
            get => baseDirectory;
            set => baseDirectory = value;
        }

        /// <inheritdoc />
        public bool CreateTimelineSubdirectories
        {
            get => createTimelineSubdirectories;
            set => createTimelineSubdirectories = value;
        }

        /// <inheritdoc />
        public bool CreateRecorderTypeSubdirectories
        {
            get => createRecorderTypeSubdirectories;
            set => createRecorderTypeSubdirectories = value;
        }

        /// <inheritdoc />
        public string FilenamePattern
        {
            get => filenamePattern;
            set => filenamePattern = value;
        }

        /// <summary>
        /// Validates the output path configuration
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate base directory
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                result.AddError("Base directory cannot be empty");
            }

            // Validate filename pattern
            if (string.IsNullOrWhiteSpace(filenamePattern))
            {
                result.AddError("Filename pattern cannot be empty");
            }
            else
            {
                // Check for at least one wildcard
                if (!filenamePattern.Contains("<") || !filenamePattern.Contains(">"))
                {
                    result.AddWarning("Filename pattern should contain wildcards (e.g., <Scene>, <Timeline>)");
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a clone of the output path configuration
        /// </summary>
        public IOutputPathConfiguration Clone()
        {
            return new OutputPathConfiguration
            {
                baseDirectory = this.baseDirectory,
                createTimelineSubdirectories = this.createTimelineSubdirectories,
                createRecorderTypeSubdirectories = this.createRecorderTypeSubdirectories,
                filenamePattern = this.filenamePattern
            };
        }

        /// <summary>
        /// Creates default output path configuration
        /// </summary>
        public static OutputPathConfiguration CreateDefault()
        {
            return new OutputPathConfiguration
            {
                baseDirectory = "Recordings",
                createTimelineSubdirectories = true,
                createRecorderTypeSubdirectories = true,
                filenamePattern = "<Scene>_<Timeline>_<RecorderType>_Take<Take>"
            };
        }
    }
}