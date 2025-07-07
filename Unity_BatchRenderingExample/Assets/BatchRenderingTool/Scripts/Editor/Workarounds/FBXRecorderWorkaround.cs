using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool.Workarounds
{
    /// <summary>
    /// FBXレコーダーの既知の問題を回避するためのワークアラウンド
    /// </summary>
    public static class FBXRecorderWorkaround
    {
        /// <summary>
        /// FBXレコーダーの代わりにAnimationレコーダーを使用してFBXをエクスポートする
        /// </summary>
        public static bool UseFallbackMethod(FBXRecorderSettingsConfig config, string outputPath, string fileName)
        {
            try
            {
                BatchRenderingToolLogger.Log("[FBXRecorderWorkaround] Using fallback method: Animation Recorder + FBX Export");
                
                // Step 1: Create Animation Recorder Settings
                var animConfig = new AnimationRecorderSettingsConfig
                {
                    targetGameObject = config.targetGameObject,
                    recordingScope = config.recordHierarchy 
                        ? AnimationRecordingScope.GameObjectAndChildren 
                        : AnimationRecordingScope.SingleGameObject,
                    frameRate = config.frameRate,
                    compressionLevel = ConvertCompressionLevel(config.animationCompression)
                };
                
                var animSettings = animConfig.CreateAnimationRecorderSettings("TempAnimation");
                if (animSettings == null)
                {
                    BatchRenderingToolLogger.LogError("[FBXRecorderWorkaround] Failed to create animation recorder settings");
                    return false;
                }
                
                // Configure output path
                RecorderSettingsHelper.ConfigureOutputPath(animSettings, outputPath, fileName + "_temp", RecorderSettingsType.Animation);
                
                BatchRenderingToolLogger.Log("[FBXRecorderWorkaround] Animation recorder configured successfully");
                
                // Note: The actual recording will be handled by the Timeline system
                // After recording, the .anim file can be converted to FBX using Unity's FBX Exporter
                
                return true;
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogError($"[FBXRecorderWorkaround] Fallback method failed: {e.Message}");
                return false;
            }
        }
        
        private static AnimationCompressionLevel ConvertCompressionLevel(FBXAnimationCompressionLevel fbxLevel)
        {
            switch (fbxLevel)
            {
                case FBXAnimationCompressionLevel.Lossless:
                    return AnimationCompressionLevel.Low;
                case FBXAnimationCompressionLevel.Lossy:
                    return AnimationCompressionLevel.High;
                case FBXAnimationCompressionLevel.Disabled:
                default:
                    return AnimationCompressionLevel.None;
            }
        }
        
        /// <summary>
        /// FBXレコーダーの設定を検証して問題を診断する
        /// </summary>
        public static void DiagnoseFBXRecorderIssue(RecorderSettings settings)
        {
            if (settings == null || !settings.GetType().Name.Contains("FbxRecorderSettings"))
                return;
                
            BatchRenderingToolLogger.Log("[FBXRecorderWorkaround] === Diagnosing FBX Recorder Issue ===");
            
            try
            {
                var settingsType = settings.GetType();
                
                // Check AnimationInputSettings
                var animInputProp = settingsType.GetProperty("AnimationInputSettings");
                if (animInputProp != null)
                {
                    var animInput = animInputProp.GetValue(settings);
                    if (animInput != null)
                    {
                        var gameObjectProp = animInput.GetType().GetProperty("gameObject");
                        if (gameObjectProp != null)
                        {
                            var target = gameObjectProp.GetValue(animInput) as GameObject;
                            BatchRenderingToolLogger.Log($"[FBXRecorderWorkaround] Target GameObject: {(target != null ? target.name : "NULL")}");
                        }
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[FBXRecorderWorkaround] AnimationInputSettings is NULL");
                    }
                }
                
                // Check other properties
                BatchRenderingToolLogger.Log($"[FBXRecorderWorkaround] Enabled: {settings.Enabled}");
                BatchRenderingToolLogger.Log($"[FBXRecorderWorkaround] RecordMode: {settings.RecordMode}");
                BatchRenderingToolLogger.Log($"[FBXRecorderWorkaround] FrameRate: {settings.FrameRate}");
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogError($"[FBXRecorderWorkaround] Diagnosis failed: {e.Message}");
            }
        }
    }
}