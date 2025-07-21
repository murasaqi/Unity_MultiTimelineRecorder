using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.UI.Controllers;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Default recorder editor for unsupported recorder types
    /// </summary>
    public class DefaultRecorderEditor : RecorderEditorBase
    {
        public DefaultRecorderEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
            : base(config, controller, eventBus)
        {
        }
        
        protected override void DrawRecorderSpecificSettings()
        {
            EditorGUILayout.HelpBox(
                $"No specific editor available for {_config.Type} recorder type.\n" +
                "Using default settings.",
                MessageType.Info);
        }
        
        public override bool Validate(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
        
        public override void ResetToDefaults()
        {
            // No specific settings to reset
        }
    }
}