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
        // Multiple timeline mode support
        private TimelineAsset CreateRenderTimelineMultiple(List<PlayableDirector> directors)
        {
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === CreateRenderTimelineMultiple started - {directors.Count} directors ===");
            
            try
            {
                // Create timeline
                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                if (timeline == null)
                {
                    BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create TimelineAsset instance");
                    return null;
                }
                timeline.name = $"MultiTimeline_RenderTimeline_{directors.Count}";
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
                
                // Calculate timeline positions
                float currentStartTime = 0f;
                float marginTime = timelineMarginFrames / (float)frameRate;
                float oneFrameDuration = 1.0f / frameRate;
                
                // Calculate pre-roll time (only for the first timeline)
                float preRollTime = preRollFrames > 0 ? preRollFrames / (float)frameRate : 0f;
                if (preRollFrames > 0)
                {
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Pre-roll enabled: {preRollFrames} frames ({preRollTime:F2} seconds) ===");
                }
                
                // Add pre-roll for the first timeline if needed
                if (preRollFrames > 0 && directors.Count > 0)
                {
                    var firstDirector = directors[0];
                    var firstTimeline = firstDirector.playableAsset as TimelineAsset;
                    
                    if (firstTimeline != null)
                    {
                        BatchRenderingToolLogger.LogVerbose($"[SingleTimelineRenderer] Creating pre-roll clip for {preRollFrames} frames ({preRollTime:F2} seconds)");
                        
                        // Create pre-roll clip (holds at frame 0)
                        var preRollClip = controlTrack.CreateClip<ControlPlayableAsset>();
                        if (preRollClip == null)
                        {
                            BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create pre-roll ControlClip");
                            return null;
                        }
                        
                        preRollClip.displayName = $"{firstDirector.gameObject.name} (Pre-roll)";
                        preRollClip.start = 0;
                        preRollClip.duration = preRollTime;
                        
                        var preRollAsset = preRollClip.asset as ControlPlayableAsset;
                        preRollAsset.sourceGameObject.defaultValue = firstDirector.gameObject;
                        preRollAsset.updateDirector = true;
                        preRollAsset.updateParticle = true;
                        preRollAsset.updateITimeControl = true;
                        preRollAsset.searchHierarchy = false;
                        preRollAsset.active = true;
                        preRollAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Active;
                        
                        // Set the clip to hold at frame 0
                        preRollClip.clipIn = 0;
                        preRollClip.timeScale = 0.0001; // Virtually freeze time
                        
                        currentStartTime = preRollTime;
                        BatchRenderingToolLogger.LogVerbose("[SingleTimelineRenderer] Pre-roll ControlClip created successfully");
                    }
                }
                
                // Create control clips for each timeline
                for (int i = 0; i < directors.Count; i++)
                {
                    var director = directors[i];
                    var originalTimeline = director.playableAsset as TimelineAsset;
                    
                    if (originalTimeline == null)
                    {
                        BatchRenderingToolLogger.LogWarning($"[SingleTimelineRenderer] Director {director.gameObject.name} has no timeline, skipping");
                        continue;
                    }
                    
                    // Create control clip
                    var controlClip = controlTrack.CreateClip<ControlPlayableAsset>();
                    if (controlClip == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create ControlClip for {director.gameObject.name}");
                        return null;
                    }
                    
                    controlClip.displayName = director.gameObject.name;
                    controlClip.start = currentStartTime;
                    controlClip.duration = originalTimeline.duration + oneFrameDuration;
                    
                    var controlAsset = controlClip.asset as ControlPlayableAsset;
                    controlAsset.sourceGameObject.defaultValue = director.gameObject;
                    controlAsset.updateDirector = true;
                    controlAsset.updateParticle = true;
                    controlAsset.updateITimeControl = true;
                    controlAsset.searchHierarchy = false;
                    controlAsset.active = true;
                    
                    // Set postPlayback based on recorder type
                    if (recorderType == RecorderSettingsType.FBX)
                    {
                        controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Active;
                    }
                    else
                    {
                        controlAsset.postPlayback = ActivationControlPlayable.PostPlaybackState.Revert;
                    }
                    
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Created ControlClip for {director.gameObject.name}: start={currentStartTime:F2}s, duration={controlClip.duration:F2}s");
                    
                    // Update start time for next timeline
                    currentStartTime += (float)controlClip.duration;
                    
                    // Add margin if not the last timeline
                    if (i < directors.Count - 1 && timelineMarginFrames > 0)
                    {
                        currentStartTime += marginTime;
                        BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Added margin: {marginTime:F2}s");
                    }
                }
                
                // Save asset
                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Store last generated asset path for debugging
                if (debugMode)
                {
                    lastGeneratedAssetPath = tempAssetPath;
                }
                
                // Calculate total duration (currentStartTime is the end time after all clips)
                float totalDuration = currentStartTime - preRollTime;
                
                // Create recorder tracks based on mode
                if (useMultiRecorder)
                {
                    // Multi-recorder mode with per-timeline configurations
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Multi-Recorder Mode for Multiple Timelines ===");
                    return CreateRecorderTracksForMultipleTimelines(timeline, directors, preRollTime, oneFrameDuration);
                }
                else
                {
                    // Single recorder mode for all timelines
                    BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Single Recorder Mode for Multiple Timelines ===");
                    return CreateRecorderTrackForMultipleTimelinesSingleRecorder(timeline, directors, preRollTime, totalDuration);
                }
            }
            catch (System.Exception e)
            {
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Exception in CreateRenderTimelineMultiple: {e.Message}");
                BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Stack trace: {e.StackTrace}");
                return null;
            }
        }
        
        private TimelineAsset CreateRecorderTracksForMultipleTimelines(TimelineAsset timeline, List<PlayableDirector> directors, float preRollTime, float oneFrameDuration)
        {
            // Create recorder tracks for each timeline segment
            float currentStartTime = preRollTime;
            float marginTime = timelineMarginFrames / (float)frameRate;
            
            // Keep track of all recorder types needed across all timelines
            Dictionary<RecorderSettingsType, RecorderTrack> recorderTracks = new Dictionary<RecorderSettingsType, RecorderTrack>();
            
            // Create recorder clips for each timeline
            for (int i = 0; i < directors.Count; i++)
            {
                var director = directors[i];
                var originalTimeline = director.playableAsset as TimelineAsset;
                if (originalTimeline == null) continue;
                
                float timelineDuration = (float)originalTimeline.duration + oneFrameDuration;
                
                // Find the timeline index in the available directors list
                int timelineIndex = availableDirectors.IndexOf(director);
                if (timelineIndex < 0) continue;
                
                // Get the recorder config for this timeline
                var timelineRecorderConfig = GetTimelineRecorderConfig(timelineIndex);
                var enabledRecorders = timelineRecorderConfig.GetEnabledRecorders();
                
                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Timeline {director.gameObject.name} has {enabledRecorders.Count} enabled recorders");
                
                // Create recorder clips for each enabled recorder
                foreach (var recorderItem in enabledRecorders)
                {
                    // Get or create the recorder track for this type
                    if (!recorderTracks.ContainsKey(recorderItem.recorderType))
                    {
                        var trackName = $"{recorderItem.recorderType} Recorder Track";
                        var track = timeline.CreateTrack<RecorderTrack>(null, trackName);
                        if (track == null)
                        {
                            BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create {trackName}");
                            continue;
                        }
                        recorderTracks[recorderItem.recorderType] = track;
                    }
                    
                    var recorderTrack = recorderTracks[recorderItem.recorderType];
                    
                    // Create recorder settings for this specific timeline and recorder
                    RecorderSettings recorderSettings = CreateRecorderSettingsForItem(recorderItem, director, timelineIndex);
                    if (recorderSettings == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create recorder settings for {recorderItem.recorderType}");
                        continue;
                    }
                    
                    // Save settings as sub-asset
                    AssetDatabase.AddObjectToAsset(recorderSettings, timeline);
                    
                    // Create recorder clip
                    var recorderClip = recorderTrack.CreateClip<RecorderClip>();
                    if (recorderClip == null)
                    {
                        BatchRenderingToolLogger.LogError($"[SingleTimelineRenderer] Failed to create RecorderClip for {recorderItem.recorderType}");
                        continue;
                    }
                    
                    recorderClip.displayName = $"{director.gameObject.name} - {recorderItem.recorderType}";
                    recorderClip.start = currentStartTime;
                    recorderClip.duration = timelineDuration;
                    
                    var recorderAsset = recorderClip.asset as RecorderClip;
                    recorderAsset.settings = recorderSettings;
                    
                    // Apply type-specific settings
                    if (recorderItem.recorderType == RecorderSettingsType.FBX)
                    {
                        GameObject fbxTarget = recorderItem.fbxConfig?.targetGameObject;
                        if (fbxTarget != null)
                        {
                            ApplyFBXRecorderPatchForMultiRecorder(recorderAsset, recorderClip, fbxTarget);
                        }
                    }
                    else if (recorderItem.recorderType == RecorderSettingsType.Alembic)
                    {
                        ApplyAlembicSettingsToRecorderClip(recorderAsset, recorderSettings);
                    }
                    
                    RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
                }
                
                // Update start time for next timeline
                currentStartTime += timelineDuration;
                if (i < directors.Count - 1 && timelineMarginFrames > 0)
                {
                    currentStartTime += marginTime;
                }
            }
            
            // Save asset after all tracks are created
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Multi-Timeline with individual recorder settings created successfully ===");
            
            return timeline;
        }
        
        private TimelineAsset CreateRecorderTrackForMultipleTimelinesSingleRecorder(TimelineAsset timeline, List<PlayableDirector> directors, float preRollTime, float totalDuration)
        {
            // Use first director name for context
            var firstDirector = directors[0];
            var context = new WildcardContext(takeNumber, width, height);
            context.TimelineName = directors.Count > 1 ? $"MultiTimeline_{directors.Count}" : firstDirector.gameObject.name;
            
            // Set GameObject name for specific recorder types
            if (recorderType == RecorderSettingsType.Alembic && alembicExportScope == AlembicExportScope.TargetGameObject && alembicTargetGameObject != null)
            {
                context.GameObjectName = alembicTargetGameObject.name;
            }
            
            var processedFileName = WildcardProcessor.ProcessWildcards(fileName, context);
            var processedFilePath = filePath;
            List<RecorderSettings> recorderSettingsList = new List<RecorderSettings>();
            
            // Create recorder settings (same as single timeline mode)
            switch (recorderType)
            {
                case RecorderSettingsType.Image:
                    var imageSettings = CreateImageRecorderSettings(processedFilePath, processedFileName);
                    if (imageSettings != null) recorderSettingsList.Add(imageSettings);
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
                    var alembicSettings = CreateAlembicRecorderSettings(processedFilePath, processedFileName);
                    if (alembicSettings != null) recorderSettingsList.Add(alembicSettings);
                    break;
                    
                case RecorderSettingsType.Animation:
                    var animationSettings = CreateAnimationRecorderSettings(processedFilePath, processedFileName);
                    if (animationSettings != null) recorderSettingsList.Add(animationSettings);
                    break;
                    
                case RecorderSettingsType.FBX:
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
            
            RecorderSettings recorderSettings = recorderSettingsList[0];
            
            // Save all recorder settings as sub-assets
            foreach (var settings in recorderSettingsList)
            {
                AssetDatabase.AddObjectToAsset(settings, timeline);
            }
            
            // Create recorder track and clip
            var recorderTrack = timeline.CreateTrack<UnityEditor.Recorder.Timeline.RecorderTrack>(null, "Recorder Track");
            if (recorderTrack == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create RecorderTrack");
                return null;
            }
            
            var recorderClip = recorderTrack.CreateClip<UnityEditor.Recorder.Timeline.RecorderClip>();
            if (recorderClip == null)
            {
                BatchRenderingToolLogger.LogError("[SingleTimelineRenderer] Failed to create RecorderClip");
                return null;
            }
            
            recorderClip.displayName = directors.Count > 1 ? $"Record Multiple ({directors.Count})" : $"Record {firstDirector.gameObject.name}";
            recorderClip.start = preRollTime;
            recorderClip.duration = totalDuration;
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] RecorderClip timing: start={recorderClip.start:F3}s, duration={recorderClip.duration:F3}s");
            
            var recorderAsset = recorderClip.asset as UnityEditor.Recorder.Timeline.RecorderClip;
            recorderAsset.settings = recorderSettings;
            
            // Apply type-specific patches
            if (recorderType == RecorderSettingsType.FBX)
            {
                ApplyFBXRecorderPatch(recorderAsset, recorderClip);
            }
            else if (recorderType == RecorderSettingsType.Alembic)
            {
                ApplyAlembicSettingsToRecorderClip(recorderAsset, recorderSettings);
            }
            
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            // Save asset
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] === Multiple timeline render timeline created successfully at: {tempAssetPath} ===");
            
            return timeline;
        }
        
        private void CreateRecorderTrackForMultipleTimelines(TimelineAsset timeline, MultiRecorderConfig.RecorderConfigItem recorderItem,
            List<PlayableDirector> directors, float preRollTime, float totalDuration)
        {
            // Similar to CreateRecorderTrack but with multiple timeline support
            var firstDirector = directors[0];
            var context = new WildcardContext(recorderItem.takeNumber,
                multiRecorderConfig.useGlobalResolution ? multiRecorderConfig.globalWidth : recorderItem.width,
                multiRecorderConfig.useGlobalResolution ? multiRecorderConfig.globalHeight : recorderItem.height);
            context.TimelineName = directors.Count > 1 ? $"MultiTimeline_{directors.Count}" : firstDirector.gameObject.name;
            context.RecorderName = recorderItem.recorderType.ToString();
            
            // Set GameObject name based on recorder type
            if (recorderItem.recorderType == RecorderSettingsType.Alembic && recorderItem.alembicConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.alembicConfig.targetGameObject.name;
            }
            else if (recorderItem.recorderType == RecorderSettingsType.Animation && recorderItem.animationConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.animationConfig.targetGameObject.name;
            }
            else if (recorderItem.recorderType == RecorderSettingsType.FBX && recorderItem.fbxConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.fbxConfig.targetGameObject.name;
            }
            
            var processedFileName = WildcardProcessor.ProcessWildcards(recorderItem.fileName, context);
            var processedFilePath = multiRecorderConfig.globalOutputPath;
            
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
            recorderClip.duration = totalDuration;
            
            var recorderAsset = recorderClip.asset as RecorderClip;
            recorderAsset.settings = recorderSettings;
            
            // Apply type-specific settings
            if (recorderItem.recorderType == RecorderSettingsType.FBX)
            {
                GameObject fbxTarget = recorderItem.fbxConfig?.targetGameObject;
                if (fbxTarget != null)
                {
                    ApplyFBXRecorderPatchForMultiRecorder(recorderAsset, recorderClip, fbxTarget);
                }
            }
            else if (recorderItem.recorderType == RecorderSettingsType.Alembic)
            {
                ApplyAlembicSettingsToRecorderClip(recorderAsset, recorderSettings);
            }
            
            RecorderClipUtility.EnsureRecorderTypeIsSet(recorderAsset, recorderSettings);
            
            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Created {recorderItem.recorderType} recorder track for multiple timelines");
        }
        
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
            var context = new WildcardContext(recorderItem.takeNumber, 
                multiRecorderConfig.useGlobalResolution ? multiRecorderConfig.globalWidth : recorderItem.width,
                multiRecorderConfig.useGlobalResolution ? multiRecorderConfig.globalHeight : recorderItem.height);
            context.TimelineName = originalDirector.gameObject.name;
            context.RecorderName = recorderItem.recorderType.ToString();
            
            // Set GameObject name based on recorder type
            if (recorderItem.recorderType == RecorderSettingsType.Alembic && recorderItem.alembicConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.alembicConfig.targetGameObject.name;
            }
            else if (recorderItem.recorderType == RecorderSettingsType.Animation && recorderItem.animationConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.animationConfig.targetGameObject.name;
            }
            else if (recorderItem.recorderType == RecorderSettingsType.FBX && recorderItem.fbxConfig?.targetGameObject != null)
            {
                context.GameObjectName = recorderItem.fbxConfig.targetGameObject.name;
            }
            
            // Use each recorder's individual file name
            var processedFileName = WildcardProcessor.ProcessWildcards(recorderItem.fileName, context);
            var processedFilePath = multiRecorderConfig.globalOutputPath;
            
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
                // Multi Recorder Mode用にtargetGameObjectを取得
                GameObject fbxTarget = recorderItem.fbxConfig?.targetGameObject;
                if (fbxTarget != null)
                {
                    ApplyFBXRecorderPatchForMultiRecorder(recorderAsset, recorderClip, fbxTarget);
                }
                else
                {
                    BatchRenderingToolLogger.LogWarning("[SingleTimelineRenderer] FBX target GameObject is not set for multi-recorder mode");
                }
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
        
        private void ApplyFBXRecorderPatchForMultiRecorder(RecorderClip recorderAsset, TimelineClip recorderClip, GameObject targetGameObject)
        {
            BatchRenderingToolLogger.Log("[SingleTimelineRenderer] === Applying FBX recorder special configuration (Multi-Recorder) ===");
            
            // FBX configuration
            recorderClip.displayName = $"Record FBX {targetGameObject?.name ?? "Unknown"}";
            
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
                            var currentTargetGO = gameObjectProp.GetValue(animInput) as GameObject;
                            BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Current FBX Target GameObject: {(currentTargetGO != null ? currentTargetGO.name : "NULL")}");
                            
                            if (currentTargetGO == null && targetGameObject != null)
                            {
                                // ターゲットが設定されていない場合は再設定
                                gameObjectProp.SetValue(animInput, targetGameObject);
                                BatchRenderingToolLogger.Log($"[SingleTimelineRenderer] Set FBX Target GameObject to: {targetGameObject.name}");
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