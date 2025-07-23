using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder.Input;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Editor component for animation recorder configuration
    /// </summary>
    public class AnimationRecorderEditor : RecorderEditorBase
    {
        private AnimationRecorderConfiguration _animConfig;
        
        public AnimationRecorderEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
            : base(config, controller, eventBus)
        {
            _animConfig = config as AnimationRecorderConfiguration;
            if (_animConfig == null)
            {
                throw new ArgumentException("Configuration must be AnimationRecorderConfiguration", nameof(config));
            }
        }
        
        protected override void DrawRecorderSpecificSettings()
        {
            GUILayout.Label("Animation Settings", UIStyles.SectionHeader);
            
            // GameObject to record
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target GameObject", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newTarget = EditorGUILayout.ObjectField(_animConfig.TargetGameObject, typeof(GameObject), true) as GameObject;
            if (newTarget != _animConfig.TargetGameObject)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.TargetGameObject), _animConfig.TargetGameObject, newTarget);
                _animConfig.TargetGameObject = newTarget;
            }
            EditorGUILayout.EndHorizontal();
            
            // Record hierarchy
            var recordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", _animConfig.RecordHierarchy);
            if (recordHierarchy != _animConfig.RecordHierarchy)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.RecordHierarchy), _animConfig.RecordHierarchy, recordHierarchy);
                _animConfig.RecordHierarchy = recordHierarchy;
            }
            
            // Recording options
            GUILayout.Space(10);
            GUILayout.Label("Recording Options", UIStyles.SectionHeader);
            
            // Record transform
            var recordTransform = EditorGUILayout.Toggle("Record Transform", _animConfig.RecordTransform);
            if (recordTransform != _animConfig.RecordTransform)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.RecordTransform), _animConfig.RecordTransform, recordTransform);
                _animConfig.RecordTransform = recordTransform;
            }
            
            if (_animConfig.RecordTransform)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _animConfig.RecordPosition = EditorGUILayout.Toggle("Position", _animConfig.RecordPosition);
                    _animConfig.RecordRotation = EditorGUILayout.Toggle("Rotation", _animConfig.RecordRotation);
                    _animConfig.RecordScale = EditorGUILayout.Toggle("Scale", _animConfig.RecordScale);
                }
            }
            
            // Record components
            var recordComponents = EditorGUILayout.Toggle("Record Components", _animConfig.RecordComponents);
            if (recordComponents != _animConfig.RecordComponents)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.RecordComponents), _animConfig.RecordComponents, recordComponents);
                _animConfig.RecordComponents = recordComponents;
            }
            
            if (_animConfig.RecordComponents)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _animConfig.RecordBlendShapes = EditorGUILayout.Toggle("Blend Shapes", _animConfig.RecordBlendShapes);
                    _animConfig.RecordActiveState = EditorGUILayout.Toggle("Active State", _animConfig.RecordActiveState);
                    _animConfig.RecordMaterialProperties = EditorGUILayout.Toggle("Material Properties", _animConfig.RecordMaterialProperties);
                }
            }
            
            // Frame rate
            GUILayout.Space(10);
            GUILayout.Label("Timing", UIStyles.SectionHeader);
            
            DrawIntSlider("Frame Rate", _animConfig.FrameRate, 15, 120, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.FrameRate), _animConfig.FrameRate, value);
                _animConfig.FrameRate = value;
            });
            
            // Compression
            GUILayout.Space(10);
            GUILayout.Label("Optimization", UIStyles.SectionHeader);
            
            DrawEnumField("Compression", _animConfig.CompressionMode, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.CompressionMode), _animConfig.CompressionMode, value);
                _animConfig.CompressionMode = value;
            });
            
            if (_animConfig.CompressionMode != AnimationCompressionMode.Off)
            {
                // Position error
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Position Error", GUILayout.Width(UIStyles.FieldLabelWidth));
                var posError = EditorGUILayout.FloatField(_animConfig.PositionError);
                if (Math.Abs(posError - _animConfig.PositionError) > 0.0001f)
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_animConfig.PositionError), _animConfig.PositionError, posError);
                    _animConfig.PositionError = posError;
                }
                EditorGUILayout.EndHorizontal();
                
                // Rotation error
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rotation Error", GUILayout.Width(UIStyles.FieldLabelWidth));
                var rotError = EditorGUILayout.FloatField(_animConfig.RotationError);
                if (Math.Abs(rotError - _animConfig.RotationError) > 0.0001f)
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_animConfig.RotationError), _animConfig.RotationError, rotError);
                    _animConfig.RotationError = rotError;
                }
                EditorGUILayout.EndHorizontal();
                
                // Scale error
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scale Error", GUILayout.Width(UIStyles.FieldLabelWidth));
                var scaleError = EditorGUILayout.FloatField(_animConfig.ScaleError);
                if (Math.Abs(scaleError - _animConfig.ScaleError) > 0.0001f)
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_animConfig.ScaleError), _animConfig.ScaleError, scaleError);
                    _animConfig.ScaleError = scaleError;
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Keyframe reduction
            var keyframeReduction = EditorGUILayout.Toggle("Keyframe Reduction", _animConfig.KeyframeReduction);
            if (keyframeReduction != _animConfig.KeyframeReduction)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.KeyframeReduction), _animConfig.KeyframeReduction, keyframeReduction);
                _animConfig.KeyframeReduction = keyframeReduction;
            }
            
            // Output
            GUILayout.Space(10);
            GUILayout.Label("Output", UIStyles.SectionHeader);
            
            // Clip name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Clip Name", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newClipName = EditorGUILayout.TextField(_animConfig.ClipName);
            if (newClipName != _animConfig.ClipName)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.ClipName), _animConfig.ClipName, newClipName);
                _animConfig.ClipName = newClipName;
            }
            EditorGUILayout.EndHorizontal();
            
            // Take name
            var takeName = EditorGUILayout.Toggle("Take Name in File", _animConfig.TakeNameInFile);
            if (takeName != _animConfig.TakeNameInFile)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_animConfig.TakeNameInFile), _animConfig.TakeNameInFile, takeName);
                _animConfig.TakeNameInFile = takeName;
            }
            
            // Preview
            EditorGUILayout.HelpBox(
                $"Animation will be saved as:\n" +
                $"{(_animConfig.TakeNameInFile ? "<Scene>_<Take>_" : "")}{_animConfig.ClipName}.anim",
                MessageType.Info);
        }
        
        public override bool Validate(out string errorMessage)
        {
            if (_animConfig.TargetGameObject == null)
            {
                errorMessage = "Target GameObject is not set.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(_animConfig.ClipName))
            {
                errorMessage = "Clip name cannot be empty.";
                return false;
            }
            
            if (_animConfig.FrameRate <= 0)
            {
                errorMessage = "Frame rate must be greater than 0.";
                return false;
            }
            
            if (!_animConfig.RecordTransform && !_animConfig.RecordComponents)
            {
                errorMessage = "At least one recording option must be enabled.";
                return false;
            }
            
            if (_animConfig.RecordTransform && 
                !_animConfig.RecordPosition && 
                !_animConfig.RecordRotation && 
                !_animConfig.RecordScale)
            {
                errorMessage = "At least one transform property must be enabled.";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        public override void ResetToDefaults()
        {
            _animConfig.TargetGameObject = null;
            _animConfig.RecordHierarchy = true;
            _animConfig.RecordTransform = true;
            _animConfig.RecordPosition = true;
            _animConfig.RecordRotation = true;
            _animConfig.RecordScale = true;
            _animConfig.RecordComponents = false;
            _animConfig.RecordBlendShapes = false;
            _animConfig.RecordActiveState = false;
            _animConfig.RecordMaterialProperties = false;
            _animConfig.FrameRate = 30;
            _animConfig.CompressionMode = AnimationCompressionMode.Optimal;
            _animConfig.PositionError = 0.5f;
            _animConfig.RotationError = 0.5f;
            _animConfig.ScaleError = 0.00025f;
            _animConfig.KeyframeReduction = true;
            _animConfig.ClipName = "RecordedAnimation";
            _animConfig.TakeNameInFile = true;
            
            OnSettingsChanged();
        }
    }
}