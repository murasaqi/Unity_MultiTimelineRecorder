using UnityEditor;
using UnityEngine;
using UnityEditor.Recorder;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Editor for Image Recorder settings following Unity Recorder's standard UI
    /// </summary>
    public class ImageRecorderEditor : RecorderSettingsEditorBase
    {
        public ImageRecorderEditor(IRecorderSettingsHost host)
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
            host.width = EditorGUILayout.IntField("Width", host.width);
            host.height = EditorGUILayout.IntField("Height", host.height);
            
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
            EditorGUI.indentLevel--;
        }
        
        protected override void DrawOutputFormatSettings()
        {
            // Image format
            host.imageOutputFormat = (ImageRecorderSettings.ImageRecorderOutputFormat)
                EditorGUILayout.EnumPopup("Format", host.imageOutputFormat);
            
            // Format-specific settings
            switch (host.imageOutputFormat)
            {
                case ImageRecorderSettings.ImageRecorderOutputFormat.JPEG:
                    RecorderUIHelper.DrawPropertyWithHelp(
                        "JPEG Quality",
                        "Lower values produce smaller files but lower quality",
                        MessageType.None,
                        () => host.jpegQuality = EditorGUILayout.IntSlider("Quality", host.jpegQuality, 1, 100)
                    );
                    break;
                    
                case ImageRecorderSettings.ImageRecorderOutputFormat.EXR:
                    host.exrCompression = (CompressionUtility.EXRCompressionType)
                        EditorGUILayout.EnumPopup("Compression", host.exrCompression);
                    break;
            }
            
            // Alpha channel
            EditorGUILayout.Space(5);
            host.imageCaptureAlpha = EditorGUILayout.Toggle("Capture Alpha", host.imageCaptureAlpha);
            
            if (host.imageCaptureAlpha)
            {
                if (host.imageOutputFormat == ImageRecorderSettings.ImageRecorderOutputFormat.JPEG)
                {
                    EditorGUILayout.HelpBox("JPEG format does not support alpha channel. Consider using PNG or EXR.", MessageType.Warning);
                }
                else if (host.imageOutputFormat != ImageRecorderSettings.ImageRecorderOutputFormat.EXR)
                {
                    EditorGUILayout.HelpBox("For best alpha channel quality, consider using EXR format.", MessageType.Info);
                }
            }
        }
        
        protected override string GetFileExtension()
        {
            return host.imageOutputFormat switch
            {
                ImageRecorderSettings.ImageRecorderOutputFormat.PNG => "png",
                ImageRecorderSettings.ImageRecorderOutputFormat.JPEG => "jpg",
                ImageRecorderSettings.ImageRecorderOutputFormat.EXR => "exr",
                _ => "png"
            };
        }
        
        public override bool ValidateSettings(out string errorMessage)
        {
            if (host.width <= 0 || host.height <= 0)
            {
                errorMessage = "Width and height must be greater than 0";
                return false;
            }
            
            if (string.IsNullOrEmpty(host.fileName))
            {
                errorMessage = "File name cannot be empty";
                return false;
            }
            
            errorMessage = null;
            return true;
        }
    }
}