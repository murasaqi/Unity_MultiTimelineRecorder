using UnityEditor;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;

namespace Unity.MultiTimelineRecorder.RecorderEditors
{
    /// <summary>
    /// Editor for Movie Recorder settings following Unity Recorder's standard UI
    /// </summary>
    public class MovieRecorderEditor : RecorderSettingsEditorBase
    {
        public MovieRecorderEditor(IRecorderSettingsHost host)
        {
            this.host = host;
        }
        
        protected override void DrawInputSettings()
        {
            // Source Type selection
            host.imageSourceType = (ImageRecorderSourceType)EditorGUILayout.EnumPopup("Source", host.imageSourceType);
            
            // Source-specific settings
            switch (host.imageSourceType)
            {
                case ImageRecorderSourceType.GameView:
                    EditorGUILayout.LabelField("Capture", "Game View");
                    break;
                    
                case ImageRecorderSourceType.TargetCamera:
                    EditorGUILayout.Space(3);
                    host.imageTargetCamera = (Camera)EditorGUILayout.ObjectField("Target Camera", host.imageTargetCamera, typeof(Camera), true);
                    if (host.imageTargetCamera == null)
                    {
                        EditorGUILayout.HelpBox("Please assign a target camera.", MessageType.Warning);
                    }
                    break;
                    
                case ImageRecorderSourceType.RenderTexture:
                    EditorGUILayout.Space(3);
                    host.imageRenderTexture = (RenderTexture)EditorGUILayout.ObjectField("Render Texture", host.imageRenderTexture, typeof(RenderTexture), false);
                    if (host.imageRenderTexture == null)
                    {
                        EditorGUILayout.HelpBox("Please assign a render texture.", MessageType.Warning);
                    }
                    break;
            }
            
            // Call base to draw resolution settings
            base.DrawInputSettings();
            
            // Movie-specific presets
            EditorGUILayout.Space(5);
            DrawSubsectionHeader("Movie Presets");
            
            // Preset selection
            host.useMoviePreset = EditorGUILayout.Toggle("Use Preset", host.useMoviePreset);
            
            if (host.useMoviePreset)
            {
                EditorGUI.indentLevel++;
                host.moviePreset = (MovieRecorderPreset)EditorGUILayout.EnumPopup("Preset", host.moviePreset);
                
                if (host.moviePreset != MovieRecorderPreset.Custom)
                {
                    var presetConfig = MovieRecorderSettingsConfig.GetPreset(host.moviePreset);
                    
                    // Show preset info
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.IntField("Preset Width", presetConfig.width);
                        EditorGUILayout.IntField("Preset Height", presetConfig.height);
                        EditorGUILayout.IntField("Preset Frame Rate", presetConfig.frameRate);
                    }
                    EditorGUI.indentLevel--;
                    
                    // Apply preset values
                    host.width = presetConfig.width;
                    host.height = presetConfig.height;
                    host.frameRate = presetConfig.frameRate;
                    host.movieOutputFormat = presetConfig.outputFormat;
                    host.movieQuality = presetConfig.videoBitrateMode;
                    host.movieCaptureAudio = presetConfig.captureAudio;
                    host.movieCaptureAlpha = presetConfig.captureAlpha;
                    
                    // Override useGlobalResolution when using preset
                    host.useGlobalResolution = false;
                }
                EditorGUI.indentLevel--;
            }
            
            // Frame Rate (always show, not part of resolution)
            EditorGUILayout.Space(5);
            host.frameRate = EditorGUILayout.IntField("Frame Rate", host.frameRate);
        }
        
        private void DrawEncoderSettings()
        {
            // Encoder type selection
            EditorGUILayout.LabelField("Encoder", EditorStyles.boldLabel);
            
            #if UNITY_EDITOR_OSX
            // On macOS, both encoders are available
            host.useProResEncoder = EditorGUILayout.Toggle("Use ProRes Encoder", host.useProResEncoder);
            #elif UNITY_EDITOR_WIN
            // On Windows, show ProRes option with warning
            var previousValue = host.useProResEncoder;
            host.useProResEncoder = EditorGUILayout.Toggle("Use ProRes Encoder", host.useProResEncoder);
            if (host.useProResEncoder)
            {
                EditorGUILayout.HelpBox("ProRes encoding is supported on Windows through Unity Recorder.", MessageType.Info);
            }
            #else
            // On other platforms, force Core Encoder
            host.useProResEncoder = false;
            EditorGUILayout.HelpBox("ProRes encoder is not supported on this platform.", MessageType.Warning);
            #endif
            
            EditorGUI.indentLevel++;
            
            if (host.useProResEncoder)
            {
                // ProRes encoder settings
                host.proResFormat = (ProResEncoderSettings.OutputFormat)EditorGUILayout.EnumPopup("ProRes Format", host.proResFormat);
                
                // Show alpha support info
                bool supportsAlpha = host.proResFormat == ProResEncoderSettings.OutputFormat.ProRes4444 || 
                                   host.proResFormat == ProResEncoderSettings.OutputFormat.ProRes4444XQ;
                if (supportsAlpha)
                {
                    EditorGUILayout.HelpBox("This ProRes format supports alpha channel.", MessageType.Info);
                }
            }
            else
            {
                // Core encoder settings
                host.coreCodec = (CoreEncoderSettings.OutputCodec)EditorGUILayout.EnumPopup("Codec", host.coreCodec);
                
                #if UNITY_EDITOR_LINUX
                if (host.coreCodec == CoreEncoderSettings.OutputCodec.MP4)
                {
                    EditorGUILayout.HelpBox("H.264 MP4 is not supported on Linux. WebM will be used instead.", MessageType.Warning);
                    host.coreCodec = CoreEncoderSettings.OutputCodec.WEBM;
                }
                #endif
                
                // Encoding quality
                host.coreEncodingQuality = (CoreEncoderSettings.VideoEncodingQuality)EditorGUILayout.EnumPopup("Quality", host.coreEncodingQuality);
                
                // Show bitrate for custom quality
                if (host.coreEncodingQuality == CoreEncoderSettings.VideoEncodingQuality.Custom)
                {
                    host.movieBitrate = EditorGUILayout.IntSlider("Bitrate (Mbps)", host.movieBitrate, 1, 150);
                }
                
                // Show alpha support info
                if (host.coreCodec == CoreEncoderSettings.OutputCodec.WEBM)
                {
                    EditorGUILayout.HelpBox("WebM format supports alpha channel.", MessageType.Info);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // Encoder selection
            DrawEncoderSettings();
            
            EditorGUILayout.Space(5);
            
            // Audio settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            host.movieCaptureAudio = EditorGUILayout.Toggle("Capture Audio", host.movieCaptureAudio);
            
            if (host.movieCaptureAudio)
            {
                EditorGUI.indentLevel++;
                host.audioBitrate = (AudioBitRateMode)EditorGUILayout.EnumPopup("Audio Quality", host.audioBitrate);
                EditorGUI.indentLevel--;
            }
            
            // Alpha channel
            EditorGUILayout.Space(5);
            host.movieCaptureAlpha = EditorGUILayout.Toggle("Capture Alpha", host.movieCaptureAlpha);
            
            if (host.movieCaptureAlpha)
            {
                bool alphaSupported = host.movieOutputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MOV ||
                                    host.movieOutputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.WebM;
                
                if (!alphaSupported)
                {
                    EditorGUILayout.HelpBox("Alpha channel is only supported with MOV (ProRes) or WebM formats", MessageType.Error);
                }
            }
        }
        
        protected override string GetFileExtension()
        {
            if (host.useProResEncoder)
            {
                return "mov";
            }
            else
            {
                return host.coreCodec switch
                {
                    CoreEncoderSettings.OutputCodec.MP4 => "mp4",
                    CoreEncoderSettings.OutputCodec.WEBM => "webm",
                    _ => "mp4"
                };
            }
        }
        
        protected override string GetRecorderName()
        {
            return "Movie";
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (host.width <= 0 || host.height <= 0)
            {
                errorMessage = "Width and height must be greater than 0";
                return false;
            }
            
            if (host.frameRate <= 0)
            {
                errorMessage = "Frame rate must be greater than 0";
                return false;
            }
            
            if (string.IsNullOrEmpty(host.fileName))
            {
                errorMessage = "File name cannot be empty";
                return false;
            }
            
            // Check alpha support
            if (host.movieCaptureAlpha)
            {
                bool alphaSupported = false;
                
                if (host.useProResEncoder)
                {
                    alphaSupported = host.proResFormat == ProResEncoderSettings.OutputFormat.ProRes4444 || 
                                   host.proResFormat == ProResEncoderSettings.OutputFormat.ProRes4444XQ;
                }
                else
                {
                    alphaSupported = host.coreCodec == CoreEncoderSettings.OutputCodec.WEBM;
                }
                
                if (!alphaSupported)
                {
                    errorMessage = "Alpha channel is not supported with the selected encoder settings";
                    return false;
                }
            }
            
            errorMessage = null;
            return true;
        }
        
        protected override RecorderSettingsType GetRecorderType()
        {
            return RecorderSettingsType.Movie;
        }
    }
}