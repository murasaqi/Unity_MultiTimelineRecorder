using System;
using System.Collections.Generic;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Controllers;
using Unity.MultiTimelineRecorder;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Factory for creating recorder-specific editor components
    /// </summary>
    public static class RecorderEditorFactory
    {
        private static readonly Dictionary<RecorderSettingsType, Type> _editorTypes = new Dictionary<RecorderSettingsType, Type>
        {
            { RecorderSettingsType.Image, typeof(ImageRecorderEditor) },
            { RecorderSettingsType.Movie, typeof(MovieRecorderEditor) },
            { RecorderSettingsType.Animation, typeof(AnimationRecorderEditor) },
            { RecorderSettingsType.Alembic, typeof(AlembicRecorderEditor) },
            { RecorderSettingsType.FBX, typeof(FBXRecorderEditor) }
        };
        
        /// <summary>
        /// Creates an editor component for the specified recorder configuration
        /// </summary>
        public static IRecorderEditor CreateEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            
            if (_editorTypes.TryGetValue(config.Type, out var editorType))
            {
                var editor = Activator.CreateInstance(editorType, config, controller, eventBus) as IRecorderEditor;
                return editor;
            }
            
            // Return default editor if no specific editor is found
            return new DefaultRecorderEditor(config, controller, eventBus);
        }
        
        /// <summary>
        /// Registers a custom editor type for a recorder type
        /// </summary>
        public static void RegisterEditorType(RecorderSettingsType recorderType, Type editorType)
        {
            if (!typeof(IRecorderEditor).IsAssignableFrom(editorType))
            {
                throw new ArgumentException($"Editor type must implement {nameof(IRecorderEditor)}", nameof(editorType));
            }
            
            _editorTypes[recorderType] = editorType;
        }
        
        /// <summary>
        /// Checks if an editor is registered for a recorder type
        /// </summary>
        public static bool HasEditor(RecorderSettingsType recorderType)
        {
            return _editorTypes.ContainsKey(recorderType);
        }
    }
}