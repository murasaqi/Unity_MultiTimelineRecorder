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
    /// Editor component for movie recorder configuration
    /// </summary>
    public class MovieRecorderEditor : RecorderEditorBase
    {
        private MovieRecorderConfiguration _movieConfig;
        
        public MovieRecorderEditor(IRecorderConfiguration config, RecorderConfigurationController controller, IEventBus eventBus)
            : base(config, controller, eventBus)
        {
            _movieConfig = config as MovieRecorderConfiguration;
            if (_movieConfig == null)
            {
                throw new ArgumentException("Configuration must be MovieRecorderConfiguration", nameof(config));
            }
        }
        
        protected override void DrawRecorderSpecificSettings()
        {
            GUILayout.Label("Video Settings", UIStyles.SectionHeader);
            
            // Format
            DrawEnumField("Format", _movieConfig.Format, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.Format), _movieConfig.Format, value);
                _movieConfig.Format = value;
            });
            
            // Codec
            DrawEnumField("Codec", _movieConfig.Codec, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.Codec), _movieConfig.Codec, value);
                _movieConfig.Codec = value;
            });
            
            // Bitrate
            DrawIntSlider("Bitrate (Mbps)", _movieConfig.BitrateMode == BitrateMode.High ? 50 : 20, 1, 100, (value) => 
            {
                // Convert to BitrateMode
                var mode = value >= 40 ? BitrateMode.High : value >= 20 ? BitrateMode.Medium : BitrateMode.Low;
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.BitrateMode), _movieConfig.BitrateMode, mode);
                _movieConfig.BitrateMode = mode;
            });
            
            // Resolution
            GUILayout.Space(10);
            GUILayout.Label("Resolution", UIStyles.SectionHeader);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Resolution", GUILayout.Width(UIStyles.FieldLabelWidth));
            
            var newWidth = EditorGUILayout.IntField(_movieConfig.Width, GUILayout.Width(60));
            EditorGUILayout.LabelField("x", GUILayout.Width(15));
            var newHeight = EditorGUILayout.IntField(_movieConfig.Height, GUILayout.Width(60));
            
            if (newWidth != _movieConfig.Width)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.Width), _movieConfig.Width, newWidth);
                _movieConfig.Width = newWidth;
            }
            
            if (newHeight != _movieConfig.Height)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.Height), _movieConfig.Height, newHeight);
                _movieConfig.Height = newHeight;
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
            
            // Aspect ratio
            DrawEnumField("Aspect Ratio", _movieConfig.AspectRatio, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.AspectRatio), _movieConfig.AspectRatio, value);
                _movieConfig.AspectRatio = value;
                UpdateResolutionFromAspectRatio();
            });
            
            // Frame Rate
            GUILayout.Space(10);
            GUILayout.Label("Timing", UIStyles.SectionHeader);
            
            DrawIntSlider("Frame Rate", _movieConfig.FrameRate, 24, 120, (value) => 
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.FrameRate), _movieConfig.FrameRate, value);
                _movieConfig.FrameRate = value;
            });
            
            // Audio
            GUILayout.Space(10);
            GUILayout.Label("Audio", UIStyles.SectionHeader);
            
            var captureAudio = EditorGUILayout.Toggle("Capture Audio", _movieConfig.CaptureAudio);
            if (captureAudio != _movieConfig.CaptureAudio)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.CaptureAudio), _movieConfig.CaptureAudio, captureAudio);
                _movieConfig.CaptureAudio = captureAudio;
            }
            
            if (_movieConfig.CaptureAudio)
            {
                DrawEnumField("Audio Codec", _movieConfig.AudioCodec, (value) => 
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.AudioCodec), _movieConfig.AudioCodec, value);
                    _movieConfig.AudioCodec = value;
                });
                
                DrawIntSlider("Audio Bitrate (Kbps)", _movieConfig.AudioBitrate, 64, 320, (value) => 
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.AudioBitrate), _movieConfig.AudioBitrate, value);
                    _movieConfig.AudioBitrate = value;
                });
            }
            
            // Output
            GUILayout.Space(10);
            GUILayout.Label("Output", UIStyles.SectionHeader);
            
            // File name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Name", GUILayout.Width(UIStyles.FieldLabelWidth));
            var newFileName = EditorGUILayout.TextField(_movieConfig.FileName);
            if (newFileName != _movieConfig.FileName)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.FileName), _movieConfig.FileName, newFileName);
                _movieConfig.FileName = newFileName;
            }
            EditorGUILayout.EndHorizontal();
            
            // Advanced
            GUILayout.Space(10);
            GUILayout.Label("Advanced", UIStyles.SectionHeader);
            
            // Include UI
            var includeUI = EditorGUILayout.Toggle("Include UI", _movieConfig.IncludeUI);
            if (includeUI != _movieConfig.IncludeUI)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.IncludeUI), _movieConfig.IncludeUI, includeUI);
                _movieConfig.IncludeUI = includeUI;
            }
            
            // Motion blur
            var motionBlur = EditorGUILayout.Toggle("Motion Blur", _movieConfig.UseMotionBlur);
            if (motionBlur != _movieConfig.UseMotionBlur)
            {
                _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.UseMotionBlur), _movieConfig.UseMotionBlur, motionBlur);
                _movieConfig.UseMotionBlur = motionBlur;
            }
            
            if (_movieConfig.UseMotionBlur)
            {
                DrawFloatSlider("Shutter Angle", _movieConfig.ShutterAngle, 0f, 360f, (value) => 
                {
                    _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.ShutterAngle), _movieConfig.ShutterAngle, value);
                    _movieConfig.ShutterAngle = value;
                });
            }
            
            // Quality preset info
            EditorGUILayout.HelpBox(GetQualityInfo(), MessageType.Info);
        }
        
        private void SetResolution(int width, int height)
        {
            _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.Width), _movieConfig.Width, width);
            _controller.UpdateRecorderConfig(_config, nameof(_movieConfig.Height), _movieConfig.Height, height);
            _movieConfig.Width = width;
            _movieConfig.Height = height;
        }
        
        private void UpdateResolutionFromAspectRatio()
        {
            switch (_movieConfig.AspectRatio)
            {
                case AspectRatioMode.AspectRatio_16_9:
                    if (_movieConfig.Height > 0)
                    {
                        _movieConfig.Width = (_movieConfig.Height * 16) / 9;
                    }
                    break;
                    
                case AspectRatioMode.AspectRatio_4_3:
                    if (_movieConfig.Height > 0)
                    {
                        _movieConfig.Width = (_movieConfig.Height * 4) / 3;
                    }
                    break;
                    
                case AspectRatioMode.AspectRatio_21_9:
                    if (_movieConfig.Height > 0)
                    {
                        _movieConfig.Width = (_movieConfig.Height * 21) / 9;
                    }
                    break;
            }
        }
        
        private string GetQualityInfo()
        {
            var bitrate = _movieConfig.BitrateMode == BitrateMode.High ? "50 Mbps" : 
                         _movieConfig.BitrateMode == BitrateMode.Medium ? "20 Mbps" : "10 Mbps";
            
            return $"Current settings:\n" +
                   $"Format: {_movieConfig.Format}\n" +
                   $"Codec: {_movieConfig.Codec}\n" +
                   $"Bitrate: {bitrate}\n" +
                   $"Resolution: {_movieConfig.Width}x{_movieConfig.Height}\n" +
                   $"Frame Rate: {_movieConfig.FrameRate} fps";
        }
        
        public override bool Validate(out string errorMessage)
        {
            if (_movieConfig.Width <= 0 || _movieConfig.Height <= 0)
            {
                errorMessage = "Invalid resolution. Width and height must be greater than 0.";
                return false;
            }
            
            if (_movieConfig.Width > 8192 || _movieConfig.Height > 4320)
            {
                errorMessage = "Resolution too high. Maximum supported resolution is 8192x4320 (8K).";
                return false;
            }
            
            if (_movieConfig.FrameRate <= 0 || _movieConfig.FrameRate > 120)
            {
                errorMessage = "Invalid frame rate. Frame rate must be between 1 and 120.";
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(_movieConfig.FileName))
            {
                errorMessage = "File name cannot be empty.";
                return false;
            }
            
            // Check codec compatibility
            if (_movieConfig.Format == VideoFormat.MP4 && _movieConfig.Codec == MultiTimelineRecorder.Core.Models.RecorderSettings.VideoCodec.ProRes)
            {
                errorMessage = "ProRes codec is not compatible with MP4 format. Use MOV format instead.";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
        
        public override void ResetToDefaults()
        {
            _movieConfig.Format = VideoFormat.MP4;
            _movieConfig.Codec = MultiTimelineRecorder.Core.Models.RecorderSettings.VideoCodec.H264;
            _movieConfig.BitrateMode = BitrateMode.Medium;
            _movieConfig.Width = 1920;
            _movieConfig.Height = 1080;
            _movieConfig.AspectRatio = AspectRatioMode.AspectRatio_16_9;
            _movieConfig.FrameRate = 30;
            _movieConfig.FileName = "<Scene>_<Take>";
            _movieConfig.IncludeUI = false;
            _movieConfig.CaptureAudio = true;
            _movieConfig.AudioCodec = AudioCodec.AAC;
            _movieConfig.AudioBitrate = 192;
            _movieConfig.UseMotionBlur = false;
            _movieConfig.ShutterAngle = 180;
            
            OnSettingsChanged();
        }
    }
}