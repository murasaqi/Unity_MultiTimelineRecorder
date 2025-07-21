using System;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Editor component for image sequence recorder configuration
    /// </summary>
    public class ImageRecorderEditor : RecorderEditorBase
    {
        private ImageRecorderConfiguration _imageConfig;
        
        public ImageRecorderEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
            : base(config, controller, eventBus)
        {
            _imageConfig = config as ImageRecorderConfiguration;
            if (_imageConfig == null)
            {
                throw new ArgumentException("Configuration must be ImageRecorderConfiguration", nameof(config));
            }
        }
        
        protected override void DrawRecorderSpecificSettings()
        {
            GUILayout.Label("Image Sequence Settings", UIStyles.SectionHeader);
            
            // Format
            DrawEnumField("Format", _imageConfig.Format, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.Format), _imageConfig.Format, value);
                _imageConfig.Format = value;
            });
            
            // Quality (for JPEG)
            if (_imageConfig.Format == ImageFormat.JPEG)
            {
                DrawIntSlider("Quality", _imageConfig.Quality, 1, 100, (value) => 
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.Quality), _imageConfig.Quality, value);
                    _imageConfig.Quality = value;
                });
            }
            
            // Resolution
            GUILayout.Space(10);
            GUILayout.Label("Resolution", UIStyles.SectionHeader);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Resolution", GUILayout.Width(UIStyles.FieldLabelWidth));
            
            var newWidth = EditorGUILayout.IntField(_imageConfig.Width, GUILayout.Width(60));
            EditorGUILayout.LabelField("x", GUILayout.Width(15));
            var newHeight = EditorGUILayout.IntField(_imageConfig.Height, GUILayout.Width(60));
            
            if (newWidth != _imageConfig.Width)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.Width), _imageConfig.Width, newWidth);
                _imageConfig.Width = newWidth;
            }
            
            if (newHeight != _imageConfig.Height)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.Height), _imageConfig.Height, newHeight);
                _imageConfig.Height = newHeight;
            }
            
            // Resolution presets
            if (GUILayout.Button("HD", EditorStyles.miniButtonLeft, GUILayout.Width(30)))
            {
                SetResolution(1920, 1080);
            }
            if (GUILayout.Button("4K", EditorStyles.miniButtonMid, GUILayout.Width(30)))
            {
                SetResolution(3840, 2160);
            }
            if (GUILayout.Button("8K", EditorStyles.miniButtonRight, GUILayout.Width(30)))
            {
                SetResolution(7680, 4320);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Aspect ratio lock
            _imageConfig.MaintainAspectRatio = EditorGUILayout.Toggle("Maintain Aspect Ratio", _imageConfig.MaintainAspectRatio);
            
            // Frame Rate
            GUILayout.Space(10);
            GUILayout.Label("Timing", UIStyles.SectionHeader);
            
            DrawIntSlider("Frame Rate", _imageConfig.FrameRate, 1, 120, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.FrameRate), _imageConfig.FrameRate, value);
                _imageConfig.FrameRate = value;
            });
            
            // Output
            GUILayout.Space(10);
            GUILayout.Label("Output", UIStyles.SectionHeader);
            
            // File name pattern
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Pattern", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newPattern = EditorGUILayout.TextField(_imageConfig.FileNamePattern);
            if (newPattern != _imageConfig.FileNamePattern)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.FileNamePattern), _imageConfig.FileNamePattern, newPattern);
                _imageConfig.FileNamePattern = newPattern;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Use wildcards in file pattern:\n" +
                "<Frame> - Frame number\n" +
                "<Take> - Take number\n" +
                "<Scene> - Scene name\n" +
                "<Time> - Timestamp",
                MessageType.Info);
            
            // Padding
            DrawIntSlider("Frame Padding", _imageConfig.FramePadding, 1, 10, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.FramePadding), _imageConfig.FramePadding, value);
                _imageConfig.FramePadding = value;
            });
            
            // Advanced
            GUILayout.Space(10);
            GUILayout.Label("Advanced", UIStyles.SectionHeader);
            
            // Include UI
            var includeUI = EditorGUILayout.Toggle("Include UI", _imageConfig.IncludeUI);
            if (includeUI != _imageConfig.IncludeUI)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.IncludeUI), _imageConfig.IncludeUI, includeUI);
                _imageConfig.IncludeUI = includeUI;
            }
            
            // Transparency
            var transparency = EditorGUILayout.Toggle("Capture Alpha", _imageConfig.CaptureAlpha);
            if (transparency != _imageConfig.CaptureAlpha)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.CaptureAlpha), _imageConfig.CaptureAlpha, transparency);
                _imageConfig.CaptureAlpha = transparency;
            }
            
            // Color space
            DrawEnumField("Color Space", _imageConfig.ColorSpace, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.ColorSpace), _imageConfig.ColorSpace, value);
                _imageConfig.ColorSpace = value;
            });
        }
        
        private void SetResolution(int width, int height)
        {
            _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.Width), _imageConfig.Width, width);
            _controller.UpdateRecorderConfig(_config, nameof(_imageConfig.Height), _imageConfig.Height, height);
            _imageConfig.Width = width;
            _imageConfig.Height = height;
        }
        
        public override bool Validate(out string errorMessage)
        {
            if (_imageConfig.Width <= 0 || _imageConfig.Height <= 0)
            {
                errorMessage = "Invalid resolution. Width and height must be greater than 0.";
                return false;
            }
            
            if (_imageConfig.Width > 16384 || _imageConfig.Height > 16384)
            {
                errorMessage = "Resolution too high. Maximum supported resolution is 16384x16384.";
                return false;
            }
            
            if (_imageConfig.FrameRate <= 0)
            {
                errorMessage = "Invalid frame rate. Frame rate must be greater than 0.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(_imageConfig.FileNamePattern))
            {
                errorMessage = "File name pattern cannot be empty.";
                return false;
            }
            
            if (_imageConfig.Quality < 1 || _imageConfig.Quality > 100)
            {
                errorMessage = "Quality must be between 1 and 100.";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        public override void ResetToDefaults()
        {
            _imageConfig.Format = ImageFormat.PNG;
            _imageConfig.Quality = 95;
            _imageConfig.Width = 1920;
            _imageConfig.Height = 1080;
            _imageConfig.FrameRate = 30;
            _imageConfig.FileNamePattern = "<Scene>_<Take>_<Frame>";
            _imageConfig.FramePadding = 4;
            _imageConfig.IncludeUI = false;
            _imageConfig.CaptureAlpha = false;
            _imageConfig.ColorSpace = ColorSpace.sRGB;
            _imageConfig.MaintainAspectRatio = true;
            
            OnSettingsChanged();
        }
    }
    
    /// <summary>
    /// Image format options
    /// </summary>
    public enum ImageFormat
    {
        PNG,
        JPEG,
        EXR,
        TGA
    }
    
    /// <summary>
    /// Color space options
    /// </summary>
    public enum ColorSpace
    {
        sRGB,
        Linear
    }
}