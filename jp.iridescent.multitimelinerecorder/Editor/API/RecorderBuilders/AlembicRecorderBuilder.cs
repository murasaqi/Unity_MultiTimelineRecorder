using System;
using UnityEngine;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for Alembic recorder configuration
    /// </summary>
    public class AlembicRecorderBuilder : RecorderBuilderBase<AlembicRecorderBuilder>
    {
        private AlembicRecorderConfiguration AlembicConfig => (AlembicRecorderConfiguration)_configuration;
        
        /// <summary>
        /// Creates a new Alembic recorder builder
        /// </summary>
        public AlembicRecorderBuilder()
        {
            _configuration = new AlembicRecorderConfiguration
            {
                Name = "Alembic Recorder",
                IsEnabled = true,
                CaptureMeshRenderer = true,
                CaptureSkinnedMeshRenderer = true,
                CaptureCamera = true,
                FrameRate = 30
            };
        }
        
        /// <summary>
        /// Creates an Alembic recorder builder with parent timeline
        /// </summary>
        /// <param name="timelineBuilder">Parent timeline builder</param>
        internal AlembicRecorderBuilder(TimelineConfigurationBuilder timelineBuilder) : this()
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the root GameObject to capture
        /// </summary>
        /// <param name="root">Root GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithRoot(GameObject root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            
            AlembicConfig.TargetGameObject = root;
            
            return this;
        }
        
        /// <summary>
        /// Sets whether to capture mesh renderers
        /// </summary>
        /// <param name="capture">Capture mesh renderers</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithMeshRenderers(bool capture = true)
        {
            AlembicConfig.CaptureMeshRenderer = capture;
            return this;
        }
        
        /// <summary>
        /// Sets whether to capture skinned mesh renderers
        /// </summary>
        /// <param name="capture">Capture skinned mesh renderers</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithSkinnedMeshRenderers(bool capture = true)
        {
            AlembicConfig.CaptureSkinnedMeshRenderer = capture;
            return this;
        }
        
        /// <summary>
        /// Sets whether to capture cameras
        /// </summary>
        /// <param name="capture">Capture cameras</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithCameras(bool capture = true)
        {
            AlembicConfig.CaptureCamera = capture;
            return this;
        }
        
        /// <summary>
        /// Sets whether to capture transform data
        /// </summary>
        /// <param name="capture">Capture transform</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithTransform(bool capture = true)
        {
            AlembicConfig.CaptureTransform = capture;
            return this;
        }
        
        /// <summary>
        /// Sets the handedness for the Alembic file
        /// </summary>
        /// <param name="handedness">Handedness</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithHandedness(Handedness handedness)
        {
            AlembicConfig.Handedness = handedness;
            return this;
        }
        
        /// <summary>
        /// Sets the file name pattern
        /// </summary>
        /// <param name="fileName">File name pattern</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            
            AlembicConfig.FileName = fileName;
            return this;
        }
        
        /// <summary>
        /// Sets whether to capture vertex colors
        /// </summary>
        /// <param name="capture">Capture vertex colors</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithVertexColors(bool capture = true)
        {
            AlembicConfig.CaptureVertexColor = capture;
            return this;
        }
        
        /// <summary>
        /// Sets whether to capture face sets
        /// </summary>
        /// <param name="capture">Capture face sets</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithFaceSets(bool capture = true)
        {
            AlembicConfig.CaptureFaceSets = capture;
            return this;
        }
        
        /// <summary>
        /// Sets the scale factor
        /// </summary>
        /// <param name="scale">Scale factor</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithScale(float scale)
        {
            if (scale <= 0f)
                throw new ArgumentException("Scale must be positive", nameof(scale));
            
            AlembicConfig.ScaleFactor = scale;
            return this;
        }
        
        /// <summary>
        /// Sets whether to record hierarchy
        /// </summary>
        /// <param name="recordHierarchy">Record hierarchy</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithHierarchy(bool recordHierarchy = true)
        {
            AlembicConfig.RecordHierarchy = recordHierarchy;
            return this;
        }
        
        /// <summary>
        /// Sets whether to swap faces
        /// </summary>
        /// <param name="swap">Swap faces</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithSwapFaces(bool swap = true)
        {
            AlembicConfig.SwapFaces = swap;
            return this;
        }
        
        /// <summary>
        /// Applies a capture preset
        /// </summary>
        /// <param name="preset">Capture preset</param>
        /// <returns>Builder instance for chaining</returns>
        public AlembicRecorderBuilder WithPreset(AlembicCapturePreset preset)
        {
            switch (preset)
            {
                case AlembicCapturePreset.Basic:
                    WithMeshRenderers(true)
                        .WithSkinnedMeshRenderers(false)
                        .WithCameras(false)
                        .WithVertexColors(false)
                        .WithFaceSets(false);
                    break;
                    
                case AlembicCapturePreset.Animation:
                    WithMeshRenderers(true)
                        .WithSkinnedMeshRenderers(true)
                        .WithCameras(false)
                        .WithVertexColors(false)
                        .WithFaceSets(true);
                    break;
                    
                case AlembicCapturePreset.Complete:
                    WithMeshRenderers(true)
                        .WithSkinnedMeshRenderers(true)
                        .WithCameras(true)
                        .WithTransform(true)
                        .WithVertexColors(true)
                        .WithFaceSets(true);
                    break;
                    
                case AlembicCapturePreset.CameraOnly:
                    WithMeshRenderers(false)
                        .WithSkinnedMeshRenderers(false)
                        .WithCameras(true);
                    break;
            }
            
            return this;
        }
        
        /// <summary>
        /// Validates the Alembic recorder configuration
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();
            
            if (AlembicConfig.TargetGameObject == null)
                throw new InvalidOperationException("Target GameObject is required for Alembic capture");
            
            if (!AlembicConfig.CaptureMeshRenderer && 
                !AlembicConfig.CaptureSkinnedMeshRenderer && 
                !AlembicConfig.CaptureCamera)
                throw new InvalidOperationException("At least one capture option must be enabled");
            
            if (string.IsNullOrWhiteSpace(AlembicConfig.FileName))
                AlembicConfig.FileName = "Alembic_<Scene>_<Take>";
        }
    }
    
    /// <summary>
    /// Alembic capture presets
    /// </summary>
    public enum AlembicCapturePreset
    {
        Basic,          // Static meshes only
        Animation,      // Meshes with animation
        Complete,       // Everything
        CameraOnly      // Only camera motion
    }
}