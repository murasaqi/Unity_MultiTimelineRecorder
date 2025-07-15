using UnityEditor;
using UnityEngine;
using UnityEditor.Recorder;

namespace Unity.MultiTimelineRecorder.RecorderEditors
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
            
            // Frame Rate (image sequences still need frame rate for timing)
            EditorGUILayout.Space(5);
            host.frameRate = EditorGUILayout.IntField("Frame Rate", host.frameRate);
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
                    host.jpegQuality = EditorGUILayout.IntSlider("Quality", host.jpegQuality, 1, 100);
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
        
        protected override string GetRecorderName()
        {
            return "Image";
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
        
        protected override RecorderSettingsType GetRecorderType()
        {
            return RecorderSettingsType.Image;
        }
    }
}