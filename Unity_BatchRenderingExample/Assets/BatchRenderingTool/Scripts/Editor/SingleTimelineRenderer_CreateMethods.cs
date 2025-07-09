using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEditor.Recorder.Timeline;
using UnityEditor.Recorder.Encoder;
using Unity.EditorCoroutines.Editor;
using System.IO;

namespace BatchRenderingTool
{
    // SingleTimelineRendererクラスのpartial実装
    // CreateRenderTimelineとCreateRecorderTrackメソッドを含む
    public partial class SingleTimelineRenderer
    {
        private TimelineAsset CreateRenderTimeline(PlayableDirector originalDirector, TimelineAsset originalTimeline)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateRenderTimeline started - Director: {originalDirector.gameObject.name}, Timeline: {originalTimeline.name} ===");
            
            try
            {
                // Create timeline
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                if (timeline == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create TimelineAsset instance");
                    return null;
                }
                timeline.name = $"{originalDirector.gameObject.name}_RenderTimeline";
                timeline.editorSettings.frameRate = frameRate;
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Created TimelineAsset: {timeline.name}, frameRate: {frameRate} ===");
                
                // Save as temporary asset
                string tempDir = "Assets/BatchRenderingTool/Temp";
                if (!AssetDatabase.IsValidFolder(tempDir))
                {
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating temp directory...");
                    if (!AssetDatabase.IsValidFolder("Assets/BatchRenderingTool"))
                    {
                        AssetDatabase.CreateFolder("Assets", "BatchRenderingTool");
                        BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Created BatchRenderingTool folder");
                    }
                    AssetDatabase.CreateFolder("Assets/BatchRenderingTool", "Temp");
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Created Temp folder");
                }
                
                tempAssetPath = $"{tempDir}/{timeline.name}_{System.DateTime.Now.Ticks}.playable";
                BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating asset at: {tempAssetPath}");
                try
                {
                    AssetDatabase.CreateAsset(timeline, tempAssetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // アセットが正しく作成されたか確認
                    var createdAsset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(tempAssetPath);
                    if (createdAsset == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Asset created but could not be loaded back: {tempAssetPath}");
                        return null;
                    }
                    
                    BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Successfully created and verified asset at: {tempAssetPath}");
                }
                catch (System.Exception e)
                {
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create asset: {e.Message}");
                    return null;
                }
                
                // Create control track
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating ControlTrack...");
                var controlTrack = timeline.CreateTrack<ControlTrack>(null, "Control Track");
                if (controlTrack == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create ControlTrack");
                    return null;
                }
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] ControlTrack created successfully");
                
                // Calculate pre-roll time
                float preRollTime = preRollFrames > 0 ? preRollFrames / (float)frameRate : 0f;
                
                if (preRollFrames > 0)
                {
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Pre-roll enabled: {preRollFrames} frames ({preRollTime:F2} seconds) ===");
                }
                
                if (preRollFrames > 0)
                {
                    BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating pre-roll clip for {preRollFrames} frames ({preRollTime:F2} seconds)");
                    
                    // Create pre-roll clip (holds at frame 0)
                    var preRollClip = controlTrack.CreateClip<ControlPlayableAsset>();
                    if (preRollClip == null)
                    {
                        BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create pre-roll ControlClip");
                        return null;
                    }
                    
                    preRollClip.displayName = $"{originalDirector.gameObject.name} (Pre-roll)";
                    preRollClip.start = 0;
                    preRollClip.duration = preRollTime;
                    
                    var preRollAsset = preRollClip.asset as ControlPlayableAsset;
                    // ExposedReferenceは使わず、実行時にGameObject名で解決
                    preRollAsset.sourceGameObject.defaultValue = originalDirector.gameObject;
                    preRollAsset.updateDirector = true;
                    preRollAsset.updateParticle = true;
                    preRollAsset.updateITimeControl = true;
                    preRollAsset.searchHierarchy = false;
                    preRollAsset.active = true;
                    preRollAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Active;
                    
                    // IMPORTANT: Set the clip to hold at frame 0
                    // The pre-roll clip will play the director at the beginning (0-0 range)
                    preRollClip.clipIn = 0;
                    preRollClip.timeScale = 0.0001; // Virtually freeze time
                    
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Pre-roll ControlClip created successfully");
                }
                
                // Create main playback clip
                var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
                if (controlClip == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create main ControlClip");
                    return null;
                }
                BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Main ControlClip created successfully");
                controlClip.displayName = originalDirector.gameObject.name;
                controlClip.start = preRollTime;
                // Add one frame to ensure the last frame is included
                float oneFrameDuration = 1.0f / frameRate;
                controlClip.duration = originalTimeline.duration + oneFrameDuration;
                
                var controlAsset = controlClip.asset as ControlPlayableAsset;
                
                // ExposedReferenceは使わず、実行時にGameObject名で解決
                controlAsset.sourceGameObject.defaultValue = originalDirector.gameObject;
                
                // Configure control asset properties
                controlAsset.updateDirector = true;
                controlAsset.updateParticle = true;
                controlAsset.updateITimeControl = true;
                controlAsset.searchHierarchy = false;
                controlAsset.active = true;
                
                // FBXレコーダーの場合、postPlaybackをActiveに設定して
                // Timeline終了時もGameObjectがアクティブなままにする
                if (recorderType == RecorderSettingsType.FBX)
                {
                    controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Active;
                    BatchRenderingToolLogger.Log("[SingleTimelineRenderer] FBX: Set ControlClip postPlayback to Active");
                }
                else
                {
                    controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
                }
                
                // Important: We'll set the bindings on the PlayableDirector after creating it
                
                // Multi-recorder mode or single recorder mode
                if (useMultiRecorder)
                {
                    // Create multiple recorder tracks
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Multi-Recorder Mode: Creating {multiRecorderConfig.GetEnabledRecorders().Count} recorder tracks ===");
                    
                    var enabledRecorders = multiRecorderConfig.GetEnabledRecorders();
                    if (enabledRecorders.Count == 0)
                    {
                        BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] No enabled recorders in multi-recorder config");
                        return null;
                    }
                    
                    foreach (var recorderItem in enabledRecorders)
                    {
                        CreateRecorderTrack(timeline, recorderItem, originalDirector, originalTimeline, preRollTime, oneFrameDuration);
                    }
                    
                    // Save asset after all tracks are created
                    EditorUtility.SetDirty(timeline);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    
                    // Store last generated asset path for debugging
                    if (debugMode)
                    {
                        lastGeneratedAssetPath = tempAssetPath;
                    }
                    
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Multi-Recorder Timeline created successfully at: {tempAssetPath} ===");
                    
                    return timeline;
                }
                else
                {
                    // Single recorder mode - continue with rest of method
                    // This part will be in the next section due to length
                    return CreateRenderTimelineSingleRecorder(timeline, originalDirector, originalTimeline, preRollTime, oneFrameDuration);
                }
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Exception in CreateRenderTimeline: {e.Message}");
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Stack trace: {e.StackTrace}");
                return null;
            }
        }
        
        private TimelineAsset CreateRenderTimelineSingleRecorder(TimelineAsset timeline, PlayableDirector originalDirector, TimelineAsset originalTimeline, float preRollTime, float oneFrameDuration)
        {
            // Single recorder mode - existing code
            var context = new WildcardContext(takeNumber, width, height);
            context.TimelineName = originalDirector.gameObject.name;
            
            // Set GameObject name for Alembic export
            if (recorderType == RecorderSettingsType.Alembic && alembicExportScope == AlembicExportScope.TargetGameObject && alembicTargetGameObject != null)
            {
                context.GameObjectName = alembicTargetGameObject.name;
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Setting GameObject wildcard to: {alembicTargetGameObject.name} ===");
            }
            
            var processedFileName = WildcardProcessor.ProcessWildcards(fileName, context);
            var processedFilePath = filePath; // Path doesn't need wildcard processing
            List<RecorderSettings> recorderSettingsList = new List<RecorderSettings>();
            
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating recorder settings for type: {recorderType}");
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Creating ImageRecorderSettings...");
                    var imageSettings = CreateImageRecorderSettings(processedFilePath, processedFileName);
                    if (imageSettings != null)
                    {
                        recorderSettingsList.Add(imageSettings);
                        BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] ImageRecorderSettings created: {imageSettings.GetType().Name}");
                    }
                    else
                    {
                        BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] CreateImageRecorderSettings returned null");
                    }
                    break;
                    
                case RecorderSettingsType.Movie:
                    var movieSettings = CreateMovieRecorderSettings(processedFilePath, processedFileName);
                    if (movieSettings != null) recorderSettingsList.Add(movieSettings);
                    break;
                    
                case RecorderSettingsType.AOV:
                    var aovSettingsList = CreateAOVRecorderSettings(processedFilePath, processedFileName);
                    if (aovSettingsList != null) recorderSettingsList.AddRange(aovSettingsList);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Creating Alembic settings with file: {processedFileName} ===");
                    var alembicSettings = CreateAlembicRecorderSettings(processedFilePath, processedFileName);
                    if (alembicSettings != null) recorderSettingsList.Add(alembicSettings);
                    break;
                    
                case RecorderSettingsType.Animation:
                    var animationSettings = CreateAnimationRecorderSettings(processedFilePath, processedFileName);
                    if (animationSettings != null) recorderSettingsList.Add(animationSettings);
                    break;
                    
                case RecorderSettingsType.FBX:
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Creating FBX settings with file: {processedFileName} ===");
                    var fbxSettings = CreateFBXRecorderSettings(processedFilePath, processedFileName);
                    if (fbxSettings != null) recorderSettingsList.Add(fbxSettings);
                    break;
                    
                default:
                    BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Unsupported recorder type: {recorderType}");
                    return null;
            }
            
            if (recorderSettingsList.Count == 0)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create recorder settings for type: {recorderType}");
                return null;
            }
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Created {recorderSettingsList.Count} recorder settings");
            
            // For AOV, we might have multiple settings, but for now use the first one for the main recorder track
            RecorderSettings recorderSettings = recorderSettingsList[0];
            
            // Save all recorder settings as sub-assets
            foreach (var settings in recorderSettingsList)
            {
                AssetDatabase.AddObjectToAsset(settings, timeline);
            }
            
            // Create recorder track and clip
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === Creating RecorderTrack... ===");
            var recorderTrack = timeline.CreateTrack<UnityEditor.Recorder.Timeline.RecorderTrack>(null, "Recorder Track");
            if (recorderTrack == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create RecorderTrack");
                return null;
            }
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === RecorderTrack created successfully ===");
            var recorderClip = recorderTrack.CreateClip<UnityEditor.Recorder.Timeline.RecorderClip>();
            if (recorderClip == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create RecorderClip");
                return null;
            }
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === RecorderClip created successfully ===");
            
            recorderClip.displayName = $"Record {originalDirector.gameObject.name}";
            
            // すべてのレコーダーで同じタイミングを使用（TODO-101: FBX Recorder ClipがTimelineの尺と違う問題の修正）
            recorderClip.start = preRollTime;
            // Add one frame to ensure the last frame is included
            recorderClip.duration = originalTimeline.duration + oneFrameDuration;
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] RecorderClip timing: start={recorderClip.start:F3}s, duration={recorderClip.duration:F3}s (includes +1 frame = {oneFrameDuration:F3}s)");
            
            var recorderAsset = recorderClip.asset as UnityEditor.Recorder.Timeline.RecorderClip;
            if (recorderAsset == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to get RecorderClip asset");
                return null;
            }
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] RecorderClip asset type: {recorderAsset.GetType().FullName}");
            
            recorderAsset.settings = recorderSettings;
            BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Assigned RecorderSettings of type: {recorderSettings.GetType().FullName}");
            
            // Apply FBX patch if needed
            if (recorderType == RecorderSettingsType.FBX)
            {
                ApplyFBXRecorderPatch(recorderAsset, recorderClip);
            }
            
            // For Alembic Recorder, ensure the UI reflects the correct settings
            if (recorderType == RecorderSettingsType.Alembic)
            {
                ApplyAlembicSettingsToRecorderClip(recorderAsset, recorderSettings);
            }
            
            // Use RecorderClipUtility to ensure proper initialization
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            // Save asset
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Store last generated asset path for debugging
            if (debugMode)
            {
                lastGeneratedAssetPath = tempAssetPath;
            }
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Timeline created successfully at: {tempAssetPath} ===");
            
            return timeline;
        }
        
        private void CreateRecorderTrack(TimelineAsset timeline, MultiRecorderConfig.RecorderConfigItem recorderItem, 
            PlayableDirector originalDirector, TimelineAsset originalTimeline, float preRollTime, float oneFrameDuration)
        {
            var context = new WildcardContext(takeNumber, width, height);
            context.TimelineName = originalDirector.gameObject.name;
            context.RecorderName = recorderItem.recorderType.ToString();
            
            var processedFileName = WildcardProcessor.ProcessWildcards(fileName, context);
            var processedFilePath = filePath;
            
            // Create recorder settings based on type
            RecorderSettings recorderSettings = null;
            List<RecorderSettings> settingsList = new List<RecorderSettings>();
            
            switch (recorderItem.recorderType)
            {
                case RecorderSettingsType.Image:
                    recorderSettings = CreateImageRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (recorderSettings != null) settingsList.Add(recorderSettings);
                    break;
                    
                case RecorderSettingsType.Movie:
                    recorderSettings = CreateMovieRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (recorderSettings != null) settingsList.Add(recorderSettings);
                    break;
                    
                case RecorderSettingsType.AOV:
                    var aovSettingsList = CreateAOVRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (aovSettingsList != null && aovSettingsList.Count > 0)
                    {
                        settingsList.AddRange(aovSettingsList);
                        // AOVは複数の設定を返すが、CreateRecorderTrackでは1つだけ使用
                        recorderSettings = aovSettingsList[0];
                    }
                    break;
                    
                case RecorderSettingsType.Animation:
                    recorderSettings = CreateAnimationRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (recorderSettings != null) settingsList.Add(recorderSettings);
                    break;
                    
                case RecorderSettingsType.FBX:
                    recorderSettings = CreateFBXRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (recorderSettings != null) settingsList.Add(recorderSettings);
                    break;
                    
                case RecorderSettingsType.Alembic:
                    recorderSettings = CreateAlembicRecorderSettingsFromConfig(processedFilePath, processedFileName, recorderItem);
                    if (recorderSettings != null) settingsList.Add(recorderSettings);
                    break;
            }
            
            if (recorderSettings == null)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create recorder settings for {recorderItem.recorderType}");
                return;
            }
            
            // Save all settings as sub-assets
            foreach (var settings in settingsList)
            {
                AssetDatabase.AddObjectToAsset(settings, timeline);
            }
            
            // Create recorder track
            var trackName = $"{recorderItem.recorderType} Recorder Track";
            var recorderTrack = timeline.CreateTrack<RecorderTrack>(null, trackName);
            if (recorderTrack == null)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create {trackName}");
                return;
            }
            
            // Create recorder clip
            var recorderClip = recorderTrack.CreateClip<RecorderClip>();
            if (recorderClip == null)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create RecorderClip for {recorderItem.recorderType}");
                return;
            }
            
            recorderClip.displayName = $"Record {recorderItem.recorderType}";
            recorderClip.start = preRollTime;
            recorderClip.duration = originalTimeline.duration + oneFrameDuration;
            
            var recorderAsset = recorderClip.asset as RecorderClip;
            recorderAsset.settings = recorderSettings;
            
            // Apply type-specific settings
            if (recorderItem.recorderType == RecorderSettingsType.FBX)
            {
                ApplyFBXRecorderPatch(recorderAsset, recorderClip);
            }
            else if (recorderItem.recorderType == RecorderSettingsType.Alembic)
            {
                ApplyAlembicSettingsToRecorderClip(recorderAsset, recorderSettings);
            }
            
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Created {recorderItem.recorderType} recorder track successfully");
        }
        
        private void ApplyFBXRecorderPatch(RecorderClip recorderAsset, TimelineClip recorderClip)
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === Applying FBX recorder special configuration ===");
            
            // FBX configuration
            recorderClip.displayName = $"Record FBX {fbxTargetGameObject?.name ?? "Unknown"}";
            
            // RecorderAssetの設定を再確認
            if (recorderAsset.settings != null)
            {
                var fbxSettings = recorderAsset.settings;
                var settingsType = fbxSettings.GetType();
                
                // FBXレコーダーを手動で有効化
                fbxSettings.Enabled = true;
                fbxSettings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                
                // AnimationInputSettingsを確認
                var animInputProp = settingsType.GetProperty("AnimationInputSettings");
                if (animInputProp != null)
                {
                    var animInput = animInputProp.GetValue(fbxSettings);
                    if (animInput != null)
                    {
                        var animType = animInput.GetType();
                        var gameObjectProp = animType.GetProperty("gameObject");
                        if (gameObjectProp != null)
                        {
                            var targetGO = gameObjectProp.GetValue(animInput) as GameObject;
                            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] FBX Target GameObject: {(targetGO != null ? targetGO.name : "NULL")}");
                            
                            if (targetGO == null && fbxTargetGameObject != null)
                            {
                                // ターゲットが設定されていない場合は再設定
                                gameObjectProp.SetValue(animInput, fbxTargetGameObject);
                                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Set FBX Target GameObject to: {fbxTargetGameObject.name}");
                                
                                // Also add component to record
                                var addComponentMethod = animType.GetMethod("AddComponentToRecord");
                                if (addComponentMethod != null)
                                {
                                    Type componentType = typeof(Transform);
                                    if (fbxRecordedComponent == FBXRecordedComponent.Camera)
                                    {
                                        var camera = fbxTargetGameObject.GetComponent<Camera>();
                                        if (camera != null)
                                        {
                                            componentType = typeof(Camera);
                                        }
                                    }
                                    
                                    try
                                    {
                                        addComponentMethod.Invoke(animInput, new object[] { componentType });
                                        BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Added {componentType.Name} to FBX recorded components");
                                    }
                                    catch (Exception ex)
                                    {
                                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to add component: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] FBX configuration complete");
        }
        
        private void ApplyAlembicSettingsToRecorderClip(RecorderClip recorderAsset, RecorderSettings recorderSettings)
        {
            // Ensure the timeline asset has the correct settings before saving
            if (recorderAsset.settings != null)
            {
                // Force refresh the RecorderClip's internal state
                var settingsField = recorderAsset.GetType().GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
                if (settingsField != null)
                {
                    settingsField.SetValue(recorderAsset, recorderSettings);
                    BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Force set m_Settings field on RecorderClip");
                }
            }
        }
    }
}