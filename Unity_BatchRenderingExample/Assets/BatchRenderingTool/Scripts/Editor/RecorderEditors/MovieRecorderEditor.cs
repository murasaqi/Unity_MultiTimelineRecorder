using UnityEditor;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;

namespace BatchRenderingTool.RecorderEditors
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
            base.DrawInputSettings();
            
            // Resolution settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            // Preset selection
            host.useMoviePreset = EditorGUILayout.Toggle("Use Preset", host.useMoviePreset);
            
            if (host.useMoviePreset)
            {
                host.moviePreset = (MovieRecorderPreset)EditorGUILayout.EnumPopup("Preset", host.moviePreset);
                
                if (host.moviePreset != MovieRecorderPreset.Custom)
                {
                    var presetConfig = MovieRecorderSettingsConfig.GetPreset(host.moviePreset);
                    
                    // Show preset info
                    EditorGUI.indentLevel++;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.IntField("Width", presetConfig.width);
                        EditorGUILayout.IntField("Height", presetConfig.height);
                        EditorGUILayout.IntField("Frame Rate", presetConfig.frameRate);
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
                }
            }
            
            if (!host.useMoviePreset || host.moviePreset == MovieRecorderPreset.Custom)
            {
                host.width = EditorGUILayout.IntField("Width", host.width);
                host.height = EditorGUILayout.IntField("Height", host.height);
                host.frameRate = EditorGUILayout.IntField("Frame Rate", host.frameRate);
                
                // Common resolution presets
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("HD", GUILayout.Width(40)))
                {
                    host.width = 1920;
                    host.height = 1080;
                }
                if (GUILayout.Button("2K", GUILayout.Width(40)))
                {
                    host.width = 2048;
                    host.height = 1080;
                }
                if (GUILayout.Button("4K", GUILayout.Width(40)))
                {
                    host.width = 3840;
                    host.height = 2160;
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // Video format
            host.movieOutputFormat = (MovieRecorderSettings.VideoRecorderOutputFormat)
                EditorGUILayout.EnumPopup("Format", host.movieOutputFormat);
            
            // Platform-specific warnings
            if (host.movieOutputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MOV)
            {
                #if !UNITY_EDITOR_OSX
                EditorGUILayout.HelpBox("MOV format with ProRes is only available on macOS", MessageType.Warning);
                #endif
            }
            
            // Quality settings
            EditorGUILayout.Space(5);
            host.movieQuality = (VideoBitrateMode)EditorGUILayout.EnumPopup("Quality", host.movieQuality);
            
            // Always show bitrate field for manual control
            host.movieBitrate = EditorGUILayout.IntField("Bitrate (Mbps)", host.movieBitrate);
            
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
            return host.movieOutputFormat switch
            {
                MovieRecorderSettings.VideoRecorderOutputFormat.MP4 => "mp4",
                MovieRecorderSettings.VideoRecorderOutputFormat.MOV => "mov",
                MovieRecorderSettings.VideoRecorderOutputFormat.WebM => "webm",
                _ => "mp4"
            };
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
                bool alphaSupported = host.movieOutputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.MOV ||
                                    host.movieOutputFormat == MovieRecorderSettings.VideoRecorderOutputFormat.WebM;
                
                if (!alphaSupported)
                {
                    errorMessage = "Alpha channel is not supported with the selected format";
                    return false;
                }
            }
            
            errorMessage = null;
            return true;
        }
    }
}