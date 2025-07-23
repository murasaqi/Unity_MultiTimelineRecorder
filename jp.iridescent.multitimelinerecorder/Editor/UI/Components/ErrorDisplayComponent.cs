using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for displaying user-friendly error messages with solutions
    /// </summary>
    public class ErrorDisplayComponent
    {
        private ValidationResult _currentValidation;
        private bool _isExpanded = true;
        private Vector2 _scrollPosition;
        private readonly Dictionary<string, bool> _expandedSections = new Dictionary<string, bool>();
        
        /// <summary>
        /// Sets the validation result to display
        /// </summary>
        public void SetValidationResult(ValidationResult validation)
        {
            _currentValidation = validation;
        }
        
        /// <summary>
        /// Draws the error display component
        /// </summary>
        public void Draw()
        {
            if (_currentValidation == null || _currentValidation.IsValid)
            {
                return;
            }
            
            var errors = _currentValidation.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
            var warnings = _currentValidation.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
            
            if (errors.Count == 0 && warnings.Count == 0)
            {
                return;
            }
            
            using (new EditorGUILayout.VerticalScope(UIStyles.ValidationBox))
            {
                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    _isExpanded = EditorGUILayout.Foldout(_isExpanded, 
                        $"Validation Issues ({errors.Count} errors, {warnings.Count} warnings)", 
                        true);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        _currentValidation = null;
                        return;
                    }
                }
                
                if (_isExpanded)
                {
                    EditorGUILayout.Space(5);
                    
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.MaxHeight(200)))
                    {
                        _scrollPosition = scrollView.scrollPosition;
                        
                        // Display errors
                        if (errors.Count > 0)
                        {
                            DrawIssueSection("Errors", errors, EditorGUIUtility.IconContent("console.erroricon"));
                        }
                        
                        // Display warnings
                        if (warnings.Count > 0)
                        {
                            if (errors.Count > 0)
                            {
                                EditorGUILayout.Space(10);
                            }
                            DrawIssueSection("Warnings", warnings, EditorGUIUtility.IconContent("console.warnicon"));
                        }
                    }
                }
            }
        }
        
        private void DrawIssueSection(string title, List<ValidationIssue> issues, GUIContent icon)
        {
            // Section header
            using (new EditorGUILayout.HorizontalScope())
            {
                var isExpanded = GetSectionExpanded(title);
                isExpanded = EditorGUILayout.Foldout(isExpanded, title, true);
                SetSectionExpanded(title, isExpanded);
                
                GUILayout.Label($"({issues.Count})", EditorStyles.miniLabel);
            }
            
            if (GetSectionExpanded(title))
            {
                EditorGUI.indentLevel++;
                
                foreach (var issue in issues)
                {
                    DrawIssue(issue, icon);
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawIssue(ValidationIssue issue, GUIContent icon)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Issue message with icon
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                    GUILayout.Label(GetUserFriendlyMessage(issue), EditorStyles.wordWrappedLabel);
                }
                
                // Solution if available
                var solution = GetSolutionForIssue(issue);
                if (!string.IsNullOrEmpty(solution))
                {
                    EditorGUILayout.Space(2);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(25);
                        GUILayout.Label("Solution:", EditorStyles.boldLabel, GUILayout.Width(60));
                        GUILayout.Label(solution, EditorStyles.wordWrappedMiniLabel);
                    }
                    
                    // Quick fix button if applicable
                    var quickFix = GetQuickFixForIssue(issue);
                    if (quickFix != null)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(25);
                            if (GUILayout.Button(quickFix.Label, EditorStyles.miniButton, GUILayout.Width(100)))
                            {
                                quickFix.Action?.Invoke();
                            }
                        }
                    }
                }
                
                // Details (category, etc.)
                if (!string.IsNullOrEmpty(issue.Category))
                {
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(25);
                        GUILayout.Label($"Category: {issue.Category}", EditorStyles.miniLabel);
                    }
                }
            }
            
            EditorGUILayout.Space(2);
        }
        
        private string GetUserFriendlyMessage(ValidationIssue issue)
        {
            // Convert technical messages to user-friendly ones
            var message = issue.Message;
            
            // GameObject reference errors
            if (message.Contains("GameObject reference could not be restored"))
            {
                return "A GameObject that was previously recorded is missing from the scene. The recorder needs a valid GameObject to function properly.";
            }
            
            if (message.Contains("Target GameObject is required"))
            {
                return "Please select a GameObject to record. You can drag and drop a GameObject from the Hierarchy window.";
            }
            
            // Frame rate errors
            if (message.Contains("Frame rate must be"))
            {
                return "The frame rate is outside the valid range. Please use a frame rate between 1 and 120 FPS.";
            }
            
            // Resolution errors
            if (message.Contains("Invalid resolution"))
            {
                return "The recording resolution is invalid. Please ensure both width and height are positive numbers.";
            }
            
            if (message.Contains("Resolution exceeds maximum"))
            {
                return "The resolution is too large. Maximum supported resolution is 8192x8192 pixels.";
            }
            
            // Timeline errors
            if (message.Contains("No timelines selected"))
            {
                return "Please select at least one Timeline to record. Click the '+' button to add a Timeline.";
            }
            
            // Default: return original message
            return message;
        }
        
        private string GetSolutionForIssue(ValidationIssue issue)
        {
            var message = issue.Message;
            
            // GameObject reference solutions
            if (message.Contains("GameObject reference"))
            {
                return "1. Check if the GameObject exists in the scene\n" +
                       "2. Re-select the GameObject in the recorder settings\n" +
                       "3. Or create a new GameObject with the expected name";
            }
            
            if (message.Contains("Target GameObject is required"))
            {
                return "1. Select a GameObject in the Hierarchy\n" +
                       "2. Drag it to the Target GameObject field\n" +
                       "3. Or use the object picker (circle icon) to browse";
            }
            
            // Frame rate solutions
            if (message.Contains("Frame rate"))
            {
                return "Enter a frame rate between 1 and 120 FPS. Common values:\n" +
                       "• 24 FPS (Film)\n" +
                       "• 30 FPS (Video)\n" +
                       "• 60 FPS (Games)";
            }
            
            // Resolution solutions
            if (message.Contains("resolution"))
            {
                return "Common resolutions:\n" +
                       "• 1920x1080 (Full HD)\n" +
                       "• 3840x2160 (4K)\n" +
                       "• 1280x720 (HD)";
            }
            
            // Timeline solutions
            if (message.Contains("No timelines selected"))
            {
                return "1. Click the '+' button in the Timeline list\n" +
                       "2. Select a Timeline from your project\n" +
                       "3. Or drag a Timeline asset to the window";
            }
            
            return null;
        }
        
        private QuickFix GetQuickFixForIssue(ValidationIssue issue)
        {
            var message = issue.Message;
            
            // Frame rate quick fixes
            if (message.Contains("Frame rate") && message.Contains("0"))
            {
                return new QuickFix
                {
                    Label = "Set to 30 FPS",
                    Action = () => Debug.Log("TODO: Set frame rate to 30")
                };
            }
            
            // Resolution quick fixes
            if (message.Contains("Invalid resolution"))
            {
                return new QuickFix
                {
                    Label = "Set to 1920x1080",
                    Action = () => Debug.Log("TODO: Set resolution to 1920x1080")
                };
            }
            
            return null;
        }
        
        private bool GetSectionExpanded(string section)
        {
            if (!_expandedSections.ContainsKey(section))
            {
                _expandedSections[section] = true;
            }
            return _expandedSections[section];
        }
        
        private void SetSectionExpanded(string section, bool expanded)
        {
            _expandedSections[section] = expanded;
        }
        
        private class QuickFix
        {
            public string Label { get; set; }
            public Action Action { get; set; }
        }
    }
}