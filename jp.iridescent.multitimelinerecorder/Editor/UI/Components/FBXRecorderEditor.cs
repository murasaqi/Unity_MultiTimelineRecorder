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
    /// Editor component for FBX recorder configuration
    /// </summary>
    public class FBXRecorderEditor : RecorderEditorBase
    {
        private FBXRecorderConfiguration _fbxConfig;
        
        public FBXRecorderEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
            : base(config, controller, eventBus)
        {
            _fbxConfig = config as FBXRecorderConfiguration;
            if (_fbxConfig == null)
            {
                throw new ArgumentException("Configuration must be FBXRecorderConfiguration", nameof(config));
            }
        }
        
        protected override void DrawRecorderSpecificSettings()
        {
            GUILayout.Label("FBX Settings", UIStyles.SectionHeader);
            
            // GameObject to record
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target GameObject", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newTarget = EditorGUILayout.ObjectField(_fbxConfig.TargetGameObject, typeof(GameObject), true) as GameObject;
            if (newTarget != _fbxConfig.TargetGameObject)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.TargetGameObject), _fbxConfig.TargetGameObject, newTarget);
                _fbxConfig.TargetGameObject = newTarget;
            }
            EditorGUILayout.EndHorizontal();
            
            // Record hierarchy
            var recordHierarchy = EditorGUILayout.Toggle("Record Hierarchy", _fbxConfig.RecordHierarchy);
            if (recordHierarchy != _fbxConfig.RecordHierarchy)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.RecordHierarchy), _fbxConfig.RecordHierarchy, recordHierarchy);
                _fbxConfig.RecordHierarchy = recordHierarchy;
            }
            
            // Export options
            GUILayout.Space(10);
            GUILayout.Label("Export Options", UIStyles.SectionHeader);
            
            // Export format
            DrawEnumField("Format", _fbxConfig.ExportFormat, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportFormat), _fbxConfig.ExportFormat, value);
                _fbxConfig.ExportFormat = value;
            });
            
            // Model settings
            var exportMeshes = EditorGUILayout.Toggle("Export Meshes", _fbxConfig.ExportMeshes);
            if (exportMeshes != _fbxConfig.ExportMeshes)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportMeshes), _fbxConfig.ExportMeshes, exportMeshes);
                _fbxConfig.ExportMeshes = exportMeshes;
            }
            
            var exportSkinnedMesh = EditorGUILayout.Toggle("Export Skinned Mesh", _fbxConfig.ExportSkinnedMesh);
            if (exportSkinnedMesh != _fbxConfig.ExportSkinnedMesh)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportSkinnedMesh), _fbxConfig.ExportSkinnedMesh, exportSkinnedMesh);
                _fbxConfig.ExportSkinnedMesh = exportSkinnedMesh;
            }
            
            var exportAnimation = EditorGUILayout.Toggle("Export Animation", _fbxConfig.ExportAnimation);
            if (exportAnimation != _fbxConfig.ExportAnimation)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportAnimation), _fbxConfig.ExportAnimation, exportAnimation);
                _fbxConfig.ExportAnimation = exportAnimation;
            }
            
            // Animation settings
            if (_fbxConfig.ExportAnimation)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    // Bake animation
                    var bakeAnimation = EditorGUILayout.Toggle("Bake Animation", _fbxConfig.BakeAnimation);
                    if (bakeAnimation != _fbxConfig.BakeAnimation)
                    {
                        _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.BakeAnimation), _fbxConfig.BakeAnimation, bakeAnimation);
                        _fbxConfig.BakeAnimation = bakeAnimation;
                    }
                    
                    // Resample curves
                    var resampleCurves = EditorGUILayout.Toggle("Resample Curves", _fbxConfig.ResampleCurves);
                    if (resampleCurves != _fbxConfig.ResampleCurves)
                    {
                        _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ResampleCurves), _fbxConfig.ResampleCurves, resampleCurves);
                        _fbxConfig.ResampleCurves = resampleCurves;
                    }
                    
                    // Constrain props
                    var constrainProps = EditorGUILayout.Toggle("Apply Constraints", _fbxConfig.ApplyConstantKeyReducer);
                    if (constrainProps != _fbxConfig.ApplyConstantKeyReducer)
                    {
                        _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ApplyConstantKeyReducer), _fbxConfig.ApplyConstantKeyReducer, constrainProps);
                        _fbxConfig.ApplyConstantKeyReducer = constrainProps;
                    }
                }
            }
            
            // Geometry settings
            GUILayout.Space(10);
            GUILayout.Label("Geometry", UIStyles.SectionHeader);
            
            // LOD
            DrawEnumField("LOD Level", _fbxConfig.LODLevel, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.LODLevel), _fbxConfig.LODLevel, value);
                _fbxConfig.LODLevel = value;
            });
            
            // Keep quads
            var keepQuads = EditorGUILayout.Toggle("Preserve Quads", _fbxConfig.PreserveQuads);
            if (keepQuads != _fbxConfig.PreserveQuads)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.PreserveQuads), _fbxConfig.PreserveQuads, keepQuads);
                _fbxConfig.PreserveQuads = keepQuads;
            }
            
            // Export colors
            var exportColors = EditorGUILayout.Toggle("Export Vertex Colors", _fbxConfig.ExportVertexColors);
            if (exportColors != _fbxConfig.ExportVertexColors)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportVertexColors), _fbxConfig.ExportVertexColors, exportColors);
                _fbxConfig.ExportVertexColors = exportColors;
            }
            
            // Export cameras
            var exportCameras = EditorGUILayout.Toggle("Export Cameras", _fbxConfig.ExportCameras);
            if (exportCameras != _fbxConfig.ExportCameras)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportCameras), _fbxConfig.ExportCameras, exportCameras);
                _fbxConfig.ExportCameras = exportCameras;
            }
            
            // Export lights
            var exportLights = EditorGUILayout.Toggle("Export Lights", _fbxConfig.ExportLights);
            if (exportLights != _fbxConfig.ExportLights)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.ExportLights), _fbxConfig.ExportLights, exportLights);
                _fbxConfig.ExportLights = exportLights;
            }
            
            // Frame rate
            GUILayout.Space(10);
            GUILayout.Label("Timing", UIStyles.SectionHeader);
            
            DrawIntSlider("Frame Rate", _fbxConfig.FrameRate, 24, 120, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.FrameRate), _fbxConfig.FrameRate, value);
                _fbxConfig.FrameRate = value;
            });
            
            // Advanced
            GUILayout.Space(10);
            GUILayout.Label("Advanced", UIStyles.SectionHeader);
            
            // Axis conversion
            DrawEnumField("Up Axis", _fbxConfig.UpAxis, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.UpAxis), _fbxConfig.UpAxis, value);
                _fbxConfig.UpAxis = value;
            });
            
            // Unit scale
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Unit Scale", GUILayout.Width(UIStyles.FieldLabelWidth));
            var unitScale = EditorGUILayout.FloatField(_fbxConfig.UnitScale);
            if (Math.Abs(unitScale - _fbxConfig.UnitScale) > 0.0001f)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.UnitScale), _fbxConfig.UnitScale, unitScale);
                _fbxConfig.UnitScale = unitScale;
            }
            EditorGUILayout.EndHorizontal();
            
            // Include invisible
            var includeInvisible = EditorGUILayout.Toggle("Include Invisible Objects", _fbxConfig.IncludeInvisible);
            if (includeInvisible != _fbxConfig.IncludeInvisible)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.IncludeInvisible), _fbxConfig.IncludeInvisible, includeInvisible);
                _fbxConfig.IncludeInvisible = includeInvisible;
            }
            
            // Output
            GUILayout.Space(10);
            GUILayout.Label("Output", UIStyles.SectionHeader);
            
            // File name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Name", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newFileName = EditorGUILayout.TextField(_fbxConfig.FileName);
            if (newFileName != _fbxConfig.FileName)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_fbxConfig.FileName), _fbxConfig.FileName, newFileName);
                _fbxConfig.FileName = newFileName;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "FBX export captures 3D models and animations for use in other applications.\n" +
                "Supports meshes, animations, cameras, and lights.",
                MessageType.Info);
        }
        
        public override bool Validate(out string errorMessage)
        {
            if (_fbxConfig.TargetGameObject == null)
            {
                errorMessage = "Target GameObject is not set.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(_fbxConfig.FileName))
            {
                errorMessage = "File name cannot be empty.";
                return false;
            }
            
            if (_fbxConfig.FrameRate <= 0)
            {
                errorMessage = "Frame rate must be greater than 0.";
                return false;
            }
            
            if (_fbxConfig.UnitScale <= 0)
            {
                errorMessage = "Unit scale must be greater than 0.";
                return false;
            }
            
            if (!_fbxConfig.ExportMeshes && 
                !_fbxConfig.ExportSkinnedMesh && 
                !_fbxConfig.ExportAnimation && 
                !_fbxConfig.ExportCameras && 
                !_fbxConfig.ExportLights)
            {
                errorMessage = "At least one export option must be enabled.";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        public override void ResetToDefaults()
        {
            _fbxConfig.TargetGameObject = null;
            _fbxConfig.RecordHierarchy = true;
            _fbxConfig.ExportFormat = FBXFormat.Binary;
            _fbxConfig.ExportMeshes = true;
            _fbxConfig.ExportSkinnedMesh = true;
            _fbxConfig.ExportAnimation = true;
            _fbxConfig.BakeAnimation = true;
            _fbxConfig.ResampleCurves = true;
            _fbxConfig.ApplyConstantKeyReducer = true;
            _fbxConfig.LODLevel = LODLevel.LOD0;
            _fbxConfig.PreserveQuads = false;
            _fbxConfig.ExportVertexColors = true;
            _fbxConfig.ExportCameras = true;
            _fbxConfig.ExportLights = true;
            _fbxConfig.FrameRate = 30;
            _fbxConfig.UpAxis = UpAxis.Y;
            _fbxConfig.UnitScale = 1.0f;
            _fbxConfig.IncludeInvisible = false;
            _fbxConfig.FileName = "<Scene>_<Take>";
            
            OnSettingsChanged();
        }
    }
}