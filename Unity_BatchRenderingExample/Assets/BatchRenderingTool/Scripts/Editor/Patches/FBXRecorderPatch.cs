using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool.Patches
{
    /// <summary>
    /// FBXレコーダーのTakeSnapshot問題を修正するためのパッチ
    /// </summary>
    public static class FBXRecorderPatch
    {
        /// <summary>
        /// FBXレコーダーの録画前準備を行う
        /// </summary>
        public static void PrepareFBXRecording(RecorderSettings settings)
        {
            if (settings == null || !settings.GetType().Name.Contains("FbxRecorderSettings"))
                return;
                
            BatchRenderingToolLogger.Log("[FBXRecorderPatch] Preparing FBX recording...");
            
            try
            {
                // Get the input settings
                var inputSettingsProperty = settings.GetType().GetProperty("AnimationInputSettings");
                if (inputSettingsProperty != null)
                {
                    var animInputSettings = inputSettingsProperty.GetValue(settings);
                    if (animInputSettings != null)
                    {
                        var gameObjectProp = animInputSettings.GetType().GetProperty("gameObject");
                        if (gameObjectProp != null)
                        {
                            var targetGameObject = gameObjectProp.GetValue(animInputSettings) as GameObject;
                            if (targetGameObject != null)
                            {
                                BatchRenderingToolLogger.Log($"[FBXRecorderPatch] Target GameObject confirmed: {targetGameObject.name}");
                            }
                            else
                            {
                                BatchRenderingToolLogger.LogError("[FBXRecorderPatch] Target GameObject is null!");
                            }
                        }
                    }
                }
                
                // Force enable the recorder
                settings.Enabled = true;
                settings.RecordMode = RecordMode.Manual;
                
                BatchRenderingToolLogger.Log("[FBXRecorderPatch] FBX recording preparation complete");
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogError($"[FBXRecorderPatch] Error preparing FBX recording: {e.Message}");
            }
        }
        
        /// <summary>
        /// RecorderClipのFBX設定を確認して修正する
        /// </summary>
        public static void ValidateFBXRecorderClip(UnityEngine.Timeline.TimelineClip recorderClip)
        {
            if (recorderClip == null || recorderClip.asset == null)
                return;
                
            var clipType = recorderClip.asset.GetType();
            if (!clipType.Name.Contains("RecorderClip"))
                return;
                
            try
            {
                // Get the settings from the RecorderClip
                var settingsField = clipType.GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
                if (settingsField != null)
                {
                    var settings = settingsField.GetValue(recorderClip.asset) as RecorderSettings;
                    if (settings != null && settings.GetType().Name.Contains("FbxRecorderSettings"))
                    {
                        BatchRenderingToolLogger.Log($"[FBXRecorderPatch] Found FBX settings in RecorderClip: {settings.GetType().FullName}");
                        
                        // Apply patch
                        PrepareFBXRecording(settings);
                        
                        // Ensure the clip is properly configured
                        recorderClip.displayName = "FBX Recording";
                    }
                }
            }
            catch (Exception e)
            {
                BatchRenderingToolLogger.LogError($"[FBXRecorderPatch] Error validating FBX recorder clip: {e.Message}");
            }
        }
    }
}