using System;
using UnityEngine;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using UnityEditor.Recorder;

namespace MultiTimelineRecorder.Core.Models.RecorderSettings
{
    /// <summary>
    /// Configuration for Alembic recording
    /// </summary>
    [Serializable]
    public class AlembicRecorderConfiguration : RecorderConfigurationBase
    {
        [SerializeField]
        private GameObject targetGameObject;
        
        [SerializeField]
        private bool recordHierarchy = true;
        
        [SerializeField]
        private bool captureTransform = true;
        
        [SerializeField]
        private bool captureMeshRenderer = true;
        
        [SerializeField]
        private bool captureSkinnedMeshRenderer = true;
        
        [SerializeField]
        private bool captureCamera = true;
        
        [SerializeField]
        private bool captureVertexColor = true;
        
        [SerializeField]
        private bool captureFaceSets = true;
        
        [SerializeField]
        private float scaleFactor = 1.0f;
        
        [SerializeField]
        private Handedness handedness = Handedness.Right;
        
        [SerializeField]
        private int frameRate = 30;
        
        [SerializeField]
        private TransformType xformType = TransformType.TRS;
        
        [SerializeField]
        private bool assumeNonUniformScale = false;
        
        [SerializeField]
        private bool swapFaces = false;
        
        [SerializeField]
        private string fileName = "<Scene>_<Take>";

        /// <inheritdoc />
        public override RecorderSettingsType Type => RecorderSettingsType.Alembic;

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
        /// Whether to capture transform
        /// </summary>
        public bool CaptureTransform
        {
            get => captureTransform;
            set => captureTransform = value;
        }

        /// <summary>
        /// Whether to capture mesh renderer
        /// </summary>
        public bool CaptureMeshRenderer
        {
            get => captureMeshRenderer;
            set => captureMeshRenderer = value;
        }

        /// <summary>
        /// Whether to capture skinned mesh renderer
        /// </summary>
        public bool CaptureSkinnedMeshRenderer
        {
            get => captureSkinnedMeshRenderer;
            set => captureSkinnedMeshRenderer = value;
        }

        /// <summary>
        /// Whether to capture camera
        /// </summary>
        public bool CaptureCamera
        {
            get => captureCamera;
            set => captureCamera = value;
        }

        /// <summary>
        /// Whether to capture vertex color
        /// </summary>
        public bool CaptureVertexColor
        {
            get => captureVertexColor;
            set => captureVertexColor = value;
        }

        /// <summary>
        /// Whether to capture face sets
        /// </summary>
        public bool CaptureFaceSets
        {
            get => captureFaceSets;
            set => captureFaceSets = value;
        }

        /// <summary>
        /// Scale factor
        /// </summary>
        public float ScaleFactor
        {
            get => scaleFactor;
            set => scaleFactor = Mathf.Max(0.001f, value);
        }

        /// <summary>
        /// Handedness configuration
        /// </summary>
        public Handedness Handedness
        {
            get => handedness;
            set => handedness = value;
        }

        /// <summary>
        /// Frame rate
        /// </summary>
        public int FrameRate
        {
            get => frameRate;
            set => frameRate = Mathf.Clamp(value, 1, 120);
        }

        /// <summary>
        /// Transform type
        /// </summary>
        public TransformType XformType
        {
            get => xformType;
            set => xformType = value;
        }

        /// <summary>
        /// Whether to assume non-uniform scale
        /// </summary>
        public bool AssumeNonUniformScale
        {
            get => assumeNonUniformScale;
            set => assumeNonUniformScale = value;
        }

        /// <summary>
        /// Whether to swap faces
        /// </summary>
        public bool SwapFaces
        {
            get => swapFaces;
            set => swapFaces = value;
        }

        /// <summary>
        /// Output filename pattern
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = base.ValidateBase();

            // Validate target GameObject
            if (targetGameObject == null)
            {
                result.AddError("Target GameObject is required for Alembic recording");
            }

            // Validate filename
            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddError("Filename pattern cannot be empty");
            }

            // Validate scale factor
            if (scaleFactor <= 0)
            {
                result.AddError($"Scale factor must be positive. Current value: {scaleFactor}");
            }

            // Validate frame rate
            if (frameRate < 1 || frameRate > 120)
            {
                result.AddError($"Frame rate must be between 1 and 120. Current value: {frameRate}");
            }

            return result;
        }

        /// <inheritdoc />
        public override UnityEditor.Recorder.RecorderSettings CreateUnityRecorderSettings(MultiTimelineRecorder.Core.Interfaces.WildcardContext context)
        {
            // Note: Unity Recorder doesn't have built-in Alembic recorder
            // This would need Alembic for Unity package integration
            throw new NotImplementedException("Alembic recording requires Alembic for Unity package");
        }

        /// <inheritdoc />
        public override IRecorderConfiguration Clone()
        {
            var clone = new AlembicRecorderConfiguration
            {
                targetGameObject = this.targetGameObject,
                recordHierarchy = this.recordHierarchy,
                captureTransform = this.captureTransform,
                captureMeshRenderer = this.captureMeshRenderer,
                captureSkinnedMeshRenderer = this.captureSkinnedMeshRenderer,
                captureCamera = this.captureCamera,
                captureVertexColor = this.captureVertexColor,
                captureFaceSets = this.captureFaceSets,
                scaleFactor = this.scaleFactor,
                handedness = this.handedness,
                frameRate = this.frameRate,
                xformType = this.xformType,
                assumeNonUniformScale = this.assumeNonUniformScale,
                swapFaces = this.swapFaces,
                fileName = this.fileName
            };
            
            CopyBaseTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected override string GetDefaultName()
        {
            return "Alembic Export";
        }
    }

    /// <summary>
    /// Handedness options for Alembic export
    /// </summary>
    public enum Handedness
    {
        Left,
        Right
    }

    /// <summary>
    /// Transform type options
    /// </summary>
    public enum TransformType
    {
        TRS,
        Matrix
    }
}