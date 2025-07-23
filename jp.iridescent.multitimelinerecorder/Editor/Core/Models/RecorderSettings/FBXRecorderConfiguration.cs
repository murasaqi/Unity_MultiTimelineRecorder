using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// Configuration for FBX recording
    /// </summary>
    [Serializable]
    public class FBXRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private GameObject targetGameObject;
        
        [SerializeField]
        private int frameRate = 30;
        
        [SerializeField]
        private bool recordHierarchy = true;
        
        [SerializeField]
        private FBXFormat exportFormat = FBXFormat.Binary;
        
        [SerializeField]
        private bool exportMeshes = true;
        
        [SerializeField]
        private bool exportSkinnedMesh = true;
        
        [SerializeField]
        private bool exportAnimation = true;
        
        [SerializeField]
        private bool bakeAnimation = true;
        
        [SerializeField]
        private bool resampleCurves = true;
        
        [SerializeField]
        private bool applyConstantKeyReducer = true;
        
        [SerializeField]
        private LODLevel lodLevel = LODLevel.LOD0;
        
        [SerializeField]
        private bool preserveQuads = false;
        
        [SerializeField]
        private bool exportVertexColors = true;
        
        [SerializeField]
        private bool exportCameras = true;
        
        [SerializeField]
        private bool exportLights = true;
        
        [SerializeField]
        private UpAxis upAxis = UpAxis.Y;
        
        [SerializeField]
        private float unitScale = 1.0f;
        
        [SerializeField]
        private bool includeInvisible = false;
        
        [SerializeField]
        private string fileName = "<Scene>_<Take>";

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.FBX;

        /// <summary>
        /// Target GameObject to record
        /// </summary>
        public GameObject TargetGameObject
        {
            get => targetGameObject;
            set => targetGameObject = value;
        }

        /// <summary>
        /// Whether to record the entire hierarchy
        /// </summary>
        public bool RecordHierarchy
        {
            get => recordHierarchy;
            set => recordHierarchy = value;
        }

        /// <summary>
        /// Export format
        /// </summary>
        public FBXFormat ExportFormat
        {
            get => exportFormat;
            set => exportFormat = value;
        }

        /// <summary>
        /// Whether to export meshes
        /// </summary>
        public bool ExportMeshes
        {
            get => exportMeshes;
            set => exportMeshes = value;
        }

        /// <summary>
        /// Whether to export skinned meshes
        /// </summary>
        public bool ExportSkinnedMesh
        {
            get => exportSkinnedMesh;
            set => exportSkinnedMesh = value;
        }

        /// <summary>
        /// Whether to export animation
        /// </summary>
        public bool ExportAnimation
        {
            get => exportAnimation;
            set => exportAnimation = value;
        }

        /// <summary>
        /// Whether to bake animation
        /// </summary>
        public bool BakeAnimation
        {
            get => bakeAnimation;
            set => bakeAnimation = value;
        }

        /// <summary>
        /// Whether to resample curves
        /// </summary>
        public bool ResampleCurves
        {
            get => resampleCurves;
            set => resampleCurves = value;
        }

        /// <summary>
        /// Whether to apply constant key reducer
        /// </summary>
        public bool ApplyConstantKeyReducer
        {
            get => applyConstantKeyReducer;
            set => applyConstantKeyReducer = value;
        }

        /// <summary>
        /// LOD level to export
        /// </summary>
        public LODLevel LODLevel
        {
            get => lodLevel;
            set => lodLevel = value;
        }

        /// <summary>
        /// Whether to preserve quads
        /// </summary>
        public bool PreserveQuads
        {
            get => preserveQuads;
            set => preserveQuads = value;
        }

        /// <summary>
        /// Whether to export vertex colors
        /// </summary>
        public bool ExportVertexColors
        {
            get => exportVertexColors;
            set => exportVertexColors = value;
        }

        /// <summary>
        /// Whether to export cameras
        /// </summary>
        public bool ExportCameras
        {
            get => exportCameras;
            set => exportCameras = value;
        }

        /// <summary>
        /// Whether to export lights
        /// </summary>
        public bool ExportLights
        {
            get => exportLights;
            set => exportLights = value;
        }

        /// <summary>
        /// Up axis configuration
        /// </summary>
        public UpAxis UpAxis
        {
            get => upAxis;
            set => upAxis = value;
        }

        /// <summary>
        /// Unit scale
        /// </summary>
        public float UnitScale
        {
            get => unitScale;
            set => unitScale = Mathf.Max(0.001f, value);
        }

        /// <summary>
        /// Whether to include invisible objects
        /// </summary>
        public bool IncludeInvisible
        {
            get => includeInvisible;
            set => includeInvisible = value;
        }

        /// <summary>
        /// Output filename pattern
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        /// <summary>
        /// Frame rate for FBX recording
        /// </summary>
        public int FrameRate
        {
            get => frameRate;
            set => frameRate = Mathf.Clamp(value, 1, 120);
        }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = base.ValidateBase();

            // Validate target GameObject
            if (targetGameObject == null)
            {
                result.AddError("Target GameObject is required for FBX recording");
            }

            // Validate filename
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddError("Filename pattern cannot be empty");
            }

            // Validate unit scale
            if (unitScale <= 0)
            {
                result.AddError($"Unit scale must be positive. Current value: {unitScale}");
            }

            return result;
        }

        /// <inheritdoc />
        public override UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
        {
            // Note: Unity Recorder doesn't have built-in FBX recorder
            // This would need custom implementation or third-party plugin
            throw new NotImplementedException("FBX recording requires custom implementation or third-party plugin");
        }

        /// <inheritdoc />
        public override IRecorderConfiguration Clone()
        {
            var clone = new FBXRecorderConfiguration
            {
                targetGameObject = this.targetGameObject,
                recordHierarchy = this.recordHierarchy,
                exportFormat = this.exportFormat,
                exportMeshes = this.exportMeshes,
                exportSkinnedMesh = this.exportSkinnedMesh,
                exportAnimation = this.exportAnimation,
                bakeAnimation = this.bakeAnimation,
                resampleCurves = this.resampleCurves,
                applyConstantKeyReducer = this.applyConstantKeyReducer,
                lodLevel = this.lodLevel,
                preserveQuads = this.preserveQuads,
                exportVertexColors = this.exportVertexColors,
                exportCameras = this.exportCameras,
                exportLights = this.exportLights,
                upAxis = this.upAxis,
                unitScale = this.unitScale,
                includeInvisible = this.includeInvisible,
                fileName = this.fileName
            };
            
            CopyBaseTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected override string GetDefaultName()
        {
            return "FBX Export";
        }
    }

    /// <summary>
    /// FBX format options
    /// </summary>
    public enum FBXFormat
    {
        Binary,
        ASCII
    }

    /// <summary>
    /// LOD level options
    /// </summary>
    public enum LODLevel
    {
        LOD0,
        LOD1,
        LOD2,
        LOD3
    }

    /// <summary>
    /// Up axis options
    /// </summary>
    public enum UpAxis
    {
        Y,
        Z
    }
}