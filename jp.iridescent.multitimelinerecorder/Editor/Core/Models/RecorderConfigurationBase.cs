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