using System;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Editor component for Alembic recorder configuration
    /// </summary>
    public class AlembicRecorderEditor : RecorderEditorBase
    {
        private AlembicRecorderConfiguration _alembicConfig;
        
        public AlembicRecorderEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
            : base(config, controller, eventBus)
        {
            _alembicConfig = config as AlembicRecorderConfiguration;
            if (_alembicConfig == null)
            {
                throw new ArgumentException("Configuration must be AlembicRecorderConfiguration", nameof(config));
            }
        }
        
        protected override void DrawRecorderSpecificSettings()
        {
            GUILayout.Label("Alembic Settings", UIStyles.SectionHeader);
            
            // GameObject to record
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target GameObject", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newTarget = EditorGUILayout.ObjectField(_alembicConfig.TargetGameObject, typeof(GameObject), true) as GameObject;
            if (newTarget != _alembicConfig.TargetGameObject)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.TargetGameObject), _alembicConfig.TargetGameObject, newTarget);
                _alembicConfig.TargetGameObject = newTarget;
            }
            EditorGUILayout.EndHorizontal();
            
            // Record hierarchy
            var recordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", _alembicConfig.RecordHierarchy);
            if (recordHierarchy != _alembicConfig.RecordHierarchy)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.RecordHierarchy), _alembicConfig.RecordHierarchy, recordHierarchy);
                _alembicConfig.RecordHierarchy = recordHierarchy;
            }
            
            // Capture options
            GUILayout.Space(10);
            GUILayout.Label("Capture Options", UIStyles.SectionHeader);
            
            // Transform
            var captureTransform = EditorGUILayout.Toggle("Transform", _alembicConfig.CaptureTransform);
            if (captureTransform != _alembicConfig.CaptureTransform)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.CaptureTransform), _alembicConfig.CaptureTransform, captureTransform);
                _alembicConfig.CaptureTransform = captureTransform;
            }
            
            // Mesh renderer
            var captureMeshRenderer = EditorGUILayout.Toggle("Mesh Renderer", _alembicConfig.CaptureMeshRenderer);
            if (captureMeshRenderer != _alembicConfig.CaptureMeshRenderer)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.CaptureMeshRenderer), _alembicConfig.CaptureMeshRenderer, captureMeshRenderer);
                _alembicConfig.CaptureMeshRenderer = captureMeshRenderer;
            }
            
            // Skinned mesh renderer
            var captureSkinnedMesh = EditorGUILayout.Toggle("Skinned Mesh Renderer", _alembicConfig.CaptureSkinnedMeshRenderer);
            if (captureSkinnedMesh != _alembicConfig.CaptureSkinnedMeshRenderer)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.CaptureSkinnedMeshRenderer), _alembicConfig.CaptureSkinnedMeshRenderer, captureSkinnedMesh);
                _alembicConfig.CaptureSkinnedMeshRenderer = captureSkinnedMesh;
            }
            
            // Camera
            var captureCamera = EditorGUILayout.Toggle("Camera", _alembicConfig.CaptureCamera);
            if (captureCamera != _alembicConfig.CaptureCamera)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.CaptureCamera), _alembicConfig.CaptureCamera, captureCamera);
                _alembicConfig.CaptureCamera = captureCamera;
            }
            
            // Data settings
            GUILayout.Space(10);
            GUILayout.Label("Data Settings", UIStyles.SectionHeader);
            
            // Vertex color
            var captureVertexColor = EditorGUILayout.Toggle("Vertex Color", _alembicConfig.CaptureVertexColor);
            if (captureVertexColor != _alembicConfig.CaptureVertexColor)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.CaptureVertexColor), _alembicConfig.CaptureVertexColor, captureVertexColor);
                _alembicConfig.CaptureVertexColor = captureVertexColor;
            }
            
            // Face sets
            var captureFaceSets = EditorGUILayout.Toggle("Face Sets", _alembicConfig.CaptureFaceSets);
            if (captureFaceSets != _alembicConfig.CaptureFaceSets)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.CaptureFaceSets), _alembicConfig.CaptureFaceSets, captureFaceSets);
                _alembicConfig.CaptureFaceSets = captureFaceSets;
            }
            
            // Scale factor
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scale Factor", GUILayout.Width(UIStyles.FieldLabelWidth));
            var scaleFactor = EditorGUILayout.FloatField(_alembicConfig.ScaleFactor);
            if (Math.Abs(scaleFactor - _alembicConfig.ScaleFactor) > 0.0001f)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.ScaleFactor), _alembicConfig.ScaleFactor, scaleFactor);
                _alembicConfig.ScaleFactor = scaleFactor;
            }
            EditorGUILayout.EndHorizontal();
            
            // Handedness
            DrawEnumField("Handedness", _alembicConfig.Handedness, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.Handedness), _alembicConfig.Handedness, value);
                _alembicConfig.Handedness = value;
            });
            
            // Frame rate
            GUILayout.Space(10);
            GUILayout.Label("Timing", UIStyles.SectionHeader);
            
            DrawIntSlider("Frame Rate", _alembicConfig.FrameRate, 24, 120, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.FrameRate), _alembicConfig.FrameRate, value);
                _alembicConfig.FrameRate = value;
            });
            
            // X-form type
            DrawEnumField("Transform Type", _alembicConfig.XformType, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.XformType), _alembicConfig.XformType, value);
                _alembicConfig.XformType = value;
            });
            
            // Advanced
            GUILayout.Space(10);
            GUILayout.Label("Advanced", UIStyles.SectionHeader);
            
            // Assume non-uniform scale
            var assumeNonUniform = EditorGUILayout.Toggle("Assume Non-Uniform Scale", _alembicConfig.AssumeNonUniformScale);
            if (assumeNonUniform != _alembicConfig.AssumeNonUniformScale)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.AssumeNonUniformScale), _alembicConfig.AssumeNonUniformScale, assumeNonUniform);
                _alembicConfig.AssumeNonUniformScale = assumeNonUniform;
            }
            
            // Swap faces
            var swapFaces = EditorGUILayout.Toggle("Swap Faces", _alembicConfig.SwapFaces);
            if (swapFaces != _alembicConfig.SwapFaces)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.SwapFaces), _alembicConfig.SwapFaces, swapFaces);
                _alembicConfig.SwapFaces = swapFaces;
            }
            
            // Output
            GUILayout.Space(10);
            GUILayout.Label("Output", UIStyles.SectionHeader);
            
            // File name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Name", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newFileName = EditorGUILayout.TextField(_alembicConfig.FileName);
            if (newFileName != _alembicConfig.FileName)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_alembicConfig.FileName), _alembicConfig.FileName, newFileName);
                _alembicConfig.FileName = newFileName;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Alembic export captures geometry cache for use in other 3D applications.\n" +
                "Supports transform, mesh, and camera data.",
                MessageType.Info);
        }
        
        public override bool Validate(out string errorMessage)
        {
            if (_alembicConfig.TargetGameObject == null)
            {
                errorMessage = "Target GameObject is not set.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(_alembicConfig.FileName))
            {
                errorMessage = "File name cannot be empty.";
                return false;
            }
            
            if (_alembicConfig.FrameRate <= 0)
            {
                errorMessage = "Frame rate must be greater than 0.";
                return false;
            }
            
            if (_alembicConfig.ScaleFactor <= 0)
            {
                errorMessage = "Scale factor must be greater than 0.";
                return false;
            }
            
            if (!_alembicConfig.CaptureTransform && 
                !_alembicConfig.CaptureMeshRenderer && 
                !_alembicConfig.CaptureSkinnedMeshRenderer && 
                !_alembicConfig.CaptureCamera)
            {
                errorMessage = "At least one capture option must be enabled.";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        public override void ResetToDefaults()
        {
            _alembicConfig.TargetGameObject = null;
            _alembicConfig.RecordHierarchy = true;
            _alembicConfig.CaptureTransform = true;
            _alembicConfig.CaptureMeshRenderer = true;
            _alembicConfig.CaptureSkinnedMeshRenderer = true;
            _alembicConfig.CaptureCamera = true;
            _alembicConfig.CaptureVertexColor = true;
            _alembicConfig.CaptureFaceSets = true;
            _alembicConfig.ScaleFactor = 1.0f;
            _alembicConfig.Handedness = Handedness.Right;
            _alembicConfig.FrameRate = 30;
            _alembicConfig.XformType = TransformType.TRS;
            _alembicConfig.AssumeNonUniformScale = false;
            _alembicConfig.SwapFaces = false;
            _alembicConfig.FileName = "<Scene>_<Take>";
            
            OnSettingsChanged();
        }
    }
}