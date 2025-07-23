using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Base class for all recorder configurations
    /// </summary>
    [Serializable]
    public abstract class RecorderConfigurationBase : IRecorderConfiguration
    {
        [SerializeField]
        private string id;
        
        [SerializeField]
        private string name;
        
        [SerializeField]
        private bool isEnabled = true;
        
        [SerializeField]
        private int takeNumber = 1;
        
        // Non-serialized logger field for subclasses
        [NonSerialized]
        protected ILogger _logger;

        /// <inheritdoc />
        public string Id
        {
            get => string.IsNullOrEmpty(id) ? (id = Guid.NewGuid().ToString()) : id;
            set => id = value;
        }

        /// <inheritdoc />
        public string Name
        {
            get => string.IsNullOrEmpty(name) ? GetDefaultName() : name;
            set => name = value;
        }

        /// <inheritdoc />
        public abstract RecorderSettingsType Type { get; }

        /// <inheritdoc />
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        /// <inheritdoc />
        public int TakeNumber
        {
            get => takeNumber;
            set => takeNumber = Math.Max(1, value);
        }

        /// <inheritdoc />
        public abstract ValidationResult Validate();

        /// <inheritdoc />
        public abstract UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(MultiTimelineRecorder.Core.Interfaces.WildcardContext context);
        
        /// <summary>
        /// Applies global frame rate to the recorder configuration
        /// This is required due to Unity Timeline constraints - all recorders must use the same frame rate
        /// </summary>
        /// <param name="globalFrameRate">The global frame rate to apply</param>
        public virtual void ApplyGlobalFrameRate(int globalFrameRate)
        {
            // Default implementation - can be overridden by specific recorder types if needed
            // The frame rate is typically applied when creating Unity Recorder settings
        }

        /// <summary>
        /// Creates a clone of this configuration
        /// </summary>
        public abstract IRecorderConfiguration Clone();

        /// <summary>
        /// Gets the default name for this recorder type
        /// </summary>
        protected abstract string GetDefaultName();

        /// <summary>
        /// Base validation for common properties
        /// </summary>
        protected ValidationResult ValidateBase()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(name))
            {
                result.AddWarning("Recorder name is empty");
            }

            if (takeNumber < 1)
            {
                result.AddError($"Take number must be positive. Current value: {takeNumber}");
            }

            return result;
        }

        /// <summary>
        /// Copies base properties to another instance
        /// </summary>
        protected void CopyBaseTo(RecorderConfigurationBase target)
        {
            if (target != null)
            {
                target.id = Guid.NewGuid().ToString(); // Generate new ID for clone
                target.name = this.name;
                target.isEnabled = this.isEnabled;
                target.takeNumber = this.takeNumber;
            }
        }
    }
}