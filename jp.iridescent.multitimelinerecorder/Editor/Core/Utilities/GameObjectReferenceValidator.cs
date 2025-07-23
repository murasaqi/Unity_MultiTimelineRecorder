using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Services;

namespace MultiTimelineRecorder.Core.Utilities
{
    /// <summary>
    /// Utility class for validating GameObject references in recorder configurations
    /// </summary>
    public static class GameObjectReferenceValidator
    {
        /// <summary>
        /// Validate all GameObject references in a recorder configuration
        /// </summary>
        public static ValidationResult ValidateRecorderReferences(object recorderConfig)
        {
            var result = new ValidationResult();
            
            if (recorderConfig == null)
            {
                result.AddError("Recorder configuration is null");
                return result;
            }
            
            // Get reference service if available
            IGameObjectReferenceService referenceService = null;
            if (ServiceLocator.Instance.TryGet<IGameObjectReferenceService>(out var service))
            {
                referenceService = service;
            }
            
            // Check specific recorder types
            switch (recorderConfig)
            {
                case AnimationRecorderSettingsConfig animConfig:
                    ValidateAnimationRecorderReferences(animConfig, result, referenceService);
                    break;
                    
                case FBXRecorderSettingsConfig fbxConfig:
                    ValidateFBXRecorderReferences(fbxConfig, result, referenceService);
                    break;
                    
                case AlembicRecorderSettingsConfig alembicConfig:
                    ValidateAlembicRecorderReferences(alembicConfig, result, referenceService);
                    break;
            }
            
            return result;
        }
        
        /// <summary>
        /// Validate references for animation recorder
        /// </summary>
        private static void ValidateAnimationRecorderReferences(
            AnimationRecorderSettingsConfig config, 
            ValidationResult result,
            IGameObjectReferenceService referenceService)
        {
            // Validate target GameObject
            if (config.targetGameObject == null)
            {
                if (config.recordingScope == AnimationRecordingScope.SingleGameObject ||
                    config.recordingScope == AnimationRecordingScope.GameObjectAndChildren)
                {
                    result.AddError("Target GameObject reference could not be restored");
                    
                    // Show dialog to help user fix the issue
                    if (EditorUtility.DisplayDialog(
                        "Missing GameObject Reference",
                        "The target GameObject for animation recording could not be found. Would you like to select a new GameObject?",
                        "Select GameObject",
                        "Cancel"))
                    {
                        GameObject newTarget = SelectGameObject("Select Target GameObject for Animation Recording");
                        if (newTarget != null)
                        {
                            config.targetGameObject = newTarget;
                            result.AddWarning($"Target GameObject updated to: {newTarget.name}");
                        }
                    }
                }
            }
            else
            {
                // Validate the reference is still valid
                if (referenceService != null)
                {
                    try
                    {
                        var reference = new GameObjectReference { GameObject = config.targetGameObject };
                        referenceService.ValidateReference(reference);
                    }
                    catch (ReferenceRestoreException ex)
                    {
                        result.AddWarning($"Target GameObject reference may be unstable: {ex.Message}");
                    }
                }
            }
            
            // Validate custom selection
            if (config.recordingScope == AnimationRecordingScope.CustomSelection)
            {
                var invalidObjects = new List<int>();
                var customSelection = config.customSelection;
                
                for (int i = 0; i < customSelection.Count; i++)
                {
                    if (customSelection[i] == null)
                    {
                        invalidObjects.Add(i);
                    }
                }
                
                if (invalidObjects.Count > 0)
                {
                    result.AddWarning($"{invalidObjects.Count} GameObject(s) in custom selection could not be restored");
                    
                    // Offer to remove invalid references
                    if (EditorUtility.DisplayDialog(
                        "Invalid GameObject References",
                        $"{invalidObjects.Count} GameObject(s) in the custom selection are missing. Would you like to remove them?",
                        "Remove Invalid",
                        "Keep"))
                    {
                        // Remove invalid objects in reverse order to maintain indices
                        for (int i = invalidObjects.Count - 1; i >= 0; i--)
                        {
                            customSelection.RemoveAt(invalidObjects[i]);
                        }
                        config.customSelection = customSelection;
                    }
                }
            }
        }
        
        /// <summary>
        /// Validate references for FBX recorder
        /// </summary>
        private static void ValidateFBXRecorderReferences(
            FBXRecorderSettingsConfig config,
            ValidationResult result,
            IGameObjectReferenceService referenceService)
        {
            // Validate target GameObject
            if (config.targetGameObject == null)
            {
                result.AddError("Target GameObject reference could not be restored for FBX recording");
                
                if (EditorUtility.DisplayDialog(
                    "Missing GameObject Reference",
                    "The target GameObject for FBX recording could not be found. Would you like to select a new GameObject?",
                    "Select GameObject",
                    "Cancel"))
                {
                    GameObject newTarget = SelectGameObject("Select Target GameObject for FBX Recording");
                    if (newTarget != null)
                    {
                        config.targetGameObject = newTarget;
                        result.AddWarning($"Target GameObject updated to: {newTarget.name}");
                    }
                }
            }
            
            // Validate animation transfer references
            if (config.transferAnimationSource != null && config.transferAnimationDest != null)
            {
                bool sourceValid = config.transferAnimationSource != null;
                bool destValid = config.transferAnimationDest != null;
                
                if (!sourceValid || !destValid)
                {
                    result.AddWarning("Animation transfer references could not be fully restored");
                    
                    if (EditorUtility.DisplayDialog(
                        "Invalid Animation Transfer References",
                        "The animation transfer source or destination could not be found. Would you like to clear these references?",
                        "Clear References",
                        "Keep"))
                    {
                        config.transferAnimationSource = null;
                        config.transferAnimationDest = null;
                    }
                }
            }
        }
        
        /// <summary>
        /// Validate references for Alembic recorder
        /// </summary>
        private static void ValidateAlembicRecorderReferences(
            AlembicRecorderSettingsConfig config,
            ValidationResult result,
            IGameObjectReferenceService referenceService)
        {
            // Validate target GameObject
            if (config.targetGameObject == null && config.exportScope == AlembicExportScope.TargetGameObject)
            {
                result.AddError("Target GameObject reference could not be restored for Alembic recording");
                
                if (EditorUtility.DisplayDialog(
                    "Missing GameObject Reference", 
                    "The target GameObject for Alembic recording could not be found. Would you like to select a new GameObject?",
                    "Select GameObject",
                    "Cancel"))
                {
                    GameObject newTarget = SelectGameObject("Select Target GameObject for Alembic Recording");
                    if (newTarget != null)
                    {
                        config.targetGameObject = newTarget;
                        result.AddWarning($"Target GameObject updated to: {newTarget.name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Show dialog to select a GameObject
        /// </summary>
        private static GameObject SelectGameObject(string title)
        {
            // Get current selection
            GameObject currentSelection = Selection.activeGameObject;
            
            if (currentSelection != null)
            {
                if (EditorUtility.DisplayDialog(
                    title,
                    $"Use currently selected GameObject: {currentSelection.name}?",
                    "Use Selected",
                    "Browse"))
                {
                    return currentSelection;
                }
            }
            
            // Open object picker
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, true, "", 0);
            
            // Note: Object picker is asynchronous, so we can't get the result immediately
            // In a real implementation, this would need to be handled differently
            EditorUtility.DisplayDialog(
                "Select GameObject",
                "Please select a GameObject from the Object Picker window, then re-validate the configuration.",
                "OK");
                
            return null;
        }
        
        /// <summary>
        /// Validate all recorder configurations in a timeline configuration
        /// </summary>
        public static ValidationResult ValidateAllReferences(ITimelineRecorderConfig timelineConfig)
        {
            var result = new ValidationResult();
            
            if (timelineConfig == null)
            {
                result.AddError("Timeline configuration is null");
                return result;
            }
            
            foreach (var recorderConfig in timelineConfig.RecorderConfigs)
            {
                if (recorderConfig != null && recorderConfig.IsEnabled)
                {
                    var recorderResult = ValidateRecorderReferences(recorderConfig);
                    result.Merge(recorderResult);
                }
            }
            
            return result;
        }
    }
}