using System;
using UnityEngine;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Models.RecorderSettings;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.API
{
    /// <summary>
    /// Fluent API builder for FBX recorder configuration
    /// </summary>
    public class FBXRecorderBuilder : RecorderBuilderBase<FBXRecorderBuilder>
    {
        private FBXRecorderConfiguration FBXConfig => (FBXRecorderConfiguration)_configuration;
        
        /// <summary>
        /// Creates a new FBX recorder builder
        /// </summary>
        public FBXRecorderBuilder()
        {
            _configuration = new FBXRecorderConfiguration
            {
                Name = "FBX Recorder",
                IsEnabled = true,
                ExportCameras = true,
                ExportLights = true,
                FrameRate = 30
            };
        }
        
        /// <summary>
        /// Creates an FBX recorder builder with parent timeline
        /// </summary>
        /// <param name="timelineBuilder">Parent timeline builder</param>
        internal FBXRecorderBuilder(TimelineConfigurationBuilder timelineBuilder) : this()
        {
            _timelineBuilder = timelineBuilder;
        }
        
        /// <summary>
        /// Sets the root GameObject to export
        /// </summary>
        /// <param name="root">Root GameObject</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithRoot(GameObject root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            
            FBXConfig.TargetGameObject = root;
            
            return this;
        }
        
        /// <summary>
        /// Sets whether to export meshes
        /// </summary>
        /// <param name="exportMeshes">Export meshes</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithMeshes(bool exportMeshes = true)
        {
            FBXConfig.ExportMeshes = exportMeshes;
            return this;
        }
        
        /// <summary>
        /// Sets whether to export cameras
        /// </summary>
        /// <param name="exportCameras">Export cameras</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithCameras(bool exportCameras = true)
        {
            FBXConfig.ExportCameras = exportCameras;
            return this;
        }
        
        /// <summary>
        /// Sets whether to export lights
        /// </summary>
        /// <param name="exportLights">Export lights</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithLights(bool exportLights = true)
        {
            FBXConfig.ExportLights = exportLights;
            return this;
        }
        
        /// <summary>
        /// Sets whether to export animation
        /// </summary>
        /// <param name="exportAnimation">Export animation</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithAnimation(bool exportAnimation = true)
        {
            FBXConfig.ExportAnimation = exportAnimation;
            return this;
        }
        
        /// <summary>
        /// Sets whether to bake animation
        /// </summary>
        /// <param name="bakeAnimation">Bake animation</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithBakedAnimation(bool bakeAnimation = true)
        {
            FBXConfig.BakeAnimation = bakeAnimation;
            FBXConfig.ExportAnimation = true;
            return this;
        }
        
        /// <summary>
        /// Sets the file name pattern
        /// </summary>
        /// <param name="fileName">File name pattern</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));
            
            FBXConfig.FileName = fileName;
            return this;
        }
        
        /// <summary>
        /// Sets whether to include invisible objects
        /// </summary>
        /// <param name="includeInvisible">Include invisible objects</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithInvisibleObjects(bool includeInvisible = true)
        {
            FBXConfig.IncludeInvisible = includeInvisible;
            return this;
        }
        
        /// <summary>
        /// Sets the up axis for the FBX file
        /// </summary>
        /// <param name="axis">Up axis</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithUpAxis(UpAxis axis)
        {
            FBXConfig.UpAxis = axis;
            return this;
        }
        
        /// <summary>
        /// Sets the export format
        /// </summary>
        /// <param name="format">FBX format</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithFormat(FBXFormat format)
        {
            FBXConfig.ExportFormat = format;
            return this;
        }
        
        /// <summary>
        /// Applies an export preset
        /// </summary>
        /// <param name="preset">Export preset</param>
        /// <returns>Builder instance for chaining</returns>
        public FBXRecorderBuilder WithPreset(FBXExportPreset preset)
        {
            switch (preset)
            {
                case FBXExportPreset.GeometryOnly:
                    WithMeshes(true)
                        .WithCameras(false)
                        .WithLights(false)
                        .WithAnimation(false);
                    break;
                    
                case FBXExportPreset.Complete:
                    WithMeshes(true)
                        .WithCameras(true)
                        .WithLights(true)
                        .WithAnimation(true)
                        .WithBakedAnimation(true);
                    break;
                    
                case FBXExportPreset.Animation:
                    WithMeshes(true)
                        .WithCameras(false)
                        .WithLights(false)
                        .WithAnimation(true);
                    break;
                    
                case FBXExportPreset.LightweightAnimation:
                    WithMeshes(false)
                        .WithCameras(false)
                        .WithLights(false)
                        .WithAnimation(true);
                    break;
            }
            
            return this;
        }
        
        /// <summary>
        /// Validates the FBX recorder configuration
        /// </summary>
        protected override void ValidateConfiguration()
        {
            base.ValidateConfiguration();
            
            if (FBXConfig.TargetGameObject == null)
                throw new InvalidOperationException("Target GameObject is required for FBX export");
            
            if (string.IsNullOrWhiteSpace(FBXConfig.FileName))
                FBXConfig.FileName = "Export_<Scene>_<Take>";
        }
    }
    
    /// <summary>
    /// FBX export presets
    /// </summary>
    public enum FBXExportPreset
    {
        GeometryOnly,           // Export only geometry
        Complete,               // Export everything
        Animation,              // Geometry + animation
        LightweightAnimation    // Animation only
    }
}