using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for displaying and managing recorder list
    /// </summary>
    public class RecorderListComponent
    {
        private readonly RecorderConfigurationController _controller;
        private readonly IEventBus _eventBus;
        private int _selectedIndex = -1;
        
        public RecorderListComponent(RecorderConfigurationController controller, IEventBus eventBus)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus.Subscribe<RecorderAddedEvent>(OnRecorderAdded);
            _eventBus.Subscribe<RecorderRemovedEvent>(OnRecorderRemoved);
        }
        
        public void Draw(Vector2 scrollPosition)
        {
            var recorders = _controller.GetRecorders();
            
            for (int i = 0; i < recorders.Count; i++)
            {
                DrawRecorderItem(i, recorders[i], scrollPosition);
            }
        }
        
        private void DrawRecorderItem(int index, IRecorderConfiguration recorder, Vector2 scrollPosition)
        {
            var rect = GUILayoutUtility.GetRect(0, UIStyles.ListItemHeight, GUILayout.ExpandWidth(true));
            var isSelected = _controller.SelectedRecorder == recorder;
            var isHovered = rect.Contains(Event.current.mousePosition);
            
            // Draw background
            UIStyles.DrawAlternatingBackground(rect, index);
            UIStyles.DrawSelectionRect(rect, isSelected, isHovered);
            
            // Handle selection and context menu
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedIndex = index;
                _controller.SelectRecorder(recorder);
                GUI.FocusControl(null);
                
                // Right-click context menu
                if (Event.current.button == 1)
                {
                    ShowRecorderContextMenu(recorder);
                }
                
                Event.current.Use();
            }
            
            // Draw content
            using (new EditorGUILayout.HorizontalScope())
            {
                // Enable checkbox
                var checkRect = new Rect(rect.x + 5, rect.y + 2, UIStyles.CheckboxWidth, rect.height - 4);
                var wasEnabled = recorder.IsEnabled;
                recorder.IsEnabled = EditorGUI.Toggle(checkRect, recorder.IsEnabled);
                
                if (wasEnabled != recorder.IsEnabled)
                {
                    _controller.UpdateRecorderConfig(recorder, nameof(recorder.IsEnabled), wasEnabled, recorder.IsEnabled);
                }
                
                // Recorder type icon
                var iconRect = new Rect(rect.x + 30, rect.y + 2, UIStyles.IconWidth, rect.height - 4);
                DrawRecorderIcon(iconRect, recorder.Type);
                
                // Recorder name
                var nameRect = new Rect(rect.x + 60, rect.y, rect.width - 120, rect.height);
                var labelStyle = isSelected ? UIStyles.SelectedLabel : UIStyles.SelectableLabel;
                GUI.Label(nameRect, recorder.Name, labelStyle);
                
                // Action buttons
                var buttonWidth = 20f;
                var buttonX = rect.xMax - buttonWidth - 5;
                
                // Delete button
                var deleteRect = new Rect(buttonX, rect.y + 2, buttonWidth, rect.height - 4);
                if (GUI.Button(deleteRect, "×", EditorStyles.miniButton))
                {
                    _controller.RemoveRecorder(recorder.Id);
                }
                
                // Duplicate button
                buttonX -= buttonWidth + 2;
                var duplicateRect = new Rect(buttonX, rect.y + 2, buttonWidth, rect.height - 4);
                if (GUI.Button(duplicateRect, "⧉", EditorStyles.miniButton))
                {
                    _controller.DuplicateRecorder(recorder.Id);
                }
                
                // Settings button (for quick access)
                buttonX -= buttonWidth + 2;
                var settingsRect = new Rect(buttonX, rect.y + 2, buttonWidth, rect.height - 4);
                if (GUI.Button(settingsRect, "⚙", EditorStyles.miniButton))
                {
                    _controller.SelectRecorder(recorder);
                }
            }
        }
        
        private void DrawRecorderIcon(Rect rect, Unity.MultiTimelineRecorder.RecorderSettingsType type)
        {
            var icon = GetRecorderIcon(type);
            if (icon != null)
            {
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            }
            else
            {
                var label = type.ToString().Substring(0, 1);
                GUI.Label(rect, label, EditorStyles.centeredGreyMiniLabel);
            }
        }
        
        private Texture2D GetRecorderIcon(Unity.MultiTimelineRecorder.RecorderSettingsType type)
        {
            // Try to load built-in icons
            switch (type)
            {
                case Unity.MultiTimelineRecorder.RecorderSettingsType.Image:
                    return EditorGUIUtility.IconContent("RawImage Icon").image as Texture2D;
                    
                case Unity.MultiTimelineRecorder.RecorderSettingsType.Movie:
                    return EditorGUIUtility.IconContent("VideoPlayer Icon").image as Texture2D;
                    
                case Unity.MultiTimelineRecorder.RecorderSettingsType.Animation:
                    return EditorGUIUtility.IconContent("AnimationClip Icon").image as Texture2D;
                    
                case Unity.MultiTimelineRecorder.RecorderSettingsType.Alembic:
                    return EditorGUIUtility.IconContent("Mesh Icon").image as Texture2D;
                    
                case Unity.MultiTimelineRecorder.RecorderSettingsType.FBX:
                    return EditorGUIUtility.IconContent("PrefabModel Icon").image as Texture2D;
                    
                default:
                    return null;
            }
        }
        
        private void OnRecorderAdded(RecorderAddedEvent e)
        {
            // Refresh the list
            _eventBus.Publish(new UIRefreshRequestedEvent 
            { 
                Scope = UIRefreshRequestedEvent.RefreshScope.RecorderList 
            });
        }
        
        private void OnRecorderRemoved(RecorderRemovedEvent e)
        {
            // Clear selection if removed recorder was selected
            if (_controller.SelectedRecorder?.Id == e.RecorderConfigId)
            {
                _controller.SelectRecorder(null);
            }
            
            // Refresh the list
            _eventBus.Publish(new UIRefreshRequestedEvent 
            { 
                Scope = UIRefreshRequestedEvent.RefreshScope.RecorderList 
            });
        }
        
        private void ShowRecorderContextMenu(IRecorderConfiguration recorder)
        {
            var menu = new GenericMenu();
            
            // Duplicate
            menu.AddItem(new GUIContent("Duplicate"), false, () => 
            {
                _controller.DuplicateRecorder(recorder.Id);
            });
            
            // Delete
            menu.AddItem(new GUIContent("Delete"), false, () => 
            {
                _controller.RemoveRecorder(recorder.Id);
            });
            
            menu.AddSeparator("");
            
            // Apply to All Selected Timelines (only show if multiple timelines are selected)
            var selectedTimelineCount = GetSelectedTimelineCount();
            if (selectedTimelineCount > 1)
            {
                menu.AddItem(new GUIContent($"Apply to All Selected Timelines ({selectedTimelineCount})"), false, () => 
                {
                    ApplyRecorderToAllSelectedTimelines(recorder);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Apply to All Selected Timelines (Select multiple timelines)"));
            }
            
            menu.ShowAsContext();
        }
        
        private int GetSelectedTimelineCount()
        {
            // Get selected timeline count from MainController
            var mainController = Core.Services.ServiceLocator.Instance.Get<MainWindowController>();
            return mainController.SelectedTimelines.Count;
        }
        
        private void ApplyRecorderToAllSelectedTimelines(IRecorderConfiguration sourceRecorder)
        {
            var mainController = Core.Services.ServiceLocator.Instance.Get<MainWindowController>();
            var selectedTimelines = mainController.SelectedTimelines;
            
            if (selectedTimelines.Count <= 1)
            {
                Debug.LogWarning("Please select multiple timelines to apply recorder settings");
                return;
            }
            
            // Show confirmation dialog
            bool overwriteExisting = EditorUtility.DisplayDialog(
                "Apply Recorder to All Selected Timelines",
                $"Apply '{sourceRecorder.Name}' to {selectedTimelines.Count} selected timelines?\n\n" +
                "Choose whether to:\n" +
                "• Overwrite: Replace existing recorders of the same type\n" +
                "• Add New: Add as a new recorder to each timeline",
                "Overwrite Existing",
                "Add New");
            
            mainController.ApplyRecorderToSelectedTimelines(sourceRecorder, overwriteExisting);
            
            // Show notification
            EditorWindow.GetWindow<MainWindowView>().ShowNotification(
                new GUIContent($"Applied recorder to {selectedTimelines.Count} timelines"), 
                3f);
            
            // Refresh UI
            _eventBus.Publish(new UIRefreshRequestedEvent 
            { 
                Scope = UIRefreshRequestedEvent.RefreshScope.All 
            });
        }
    }
}