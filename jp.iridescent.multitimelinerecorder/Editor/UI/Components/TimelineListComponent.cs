using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Component for displaying and managing timeline list
    /// </summary>
    public class TimelineListComponent
    {
        private readonly MainWindowController _controller;
        private readonly IEventBus _eventBus;
        private List<TimelineListItem> _items = new List<TimelineListItem>();
        private int _selectedIndex = -1;
        private bool _isDragging = false;
        private int _draggedIndex = -1;
        
        public TimelineListComponent(MainWindowController controller, IEventBus eventBus)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            _eventBus.Subscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
        }
        
        public void Draw(Vector2 scrollPosition)
        {
            UpdateItems();
            
            for (int i = 0; i < _items.Count; i++)
            {
                DrawTimelineItem(i, scrollPosition);
            }
            
            // Handle drag and drop
            HandleDragAndDrop();
        }
        
        private void UpdateItems()
        {
            var selectedTimelines = _controller.SelectedTimelines;
            
            // Update or create items
            _items.Clear();
            for (int i = 0; i < selectedTimelines.Count; i++)
            {
                var timeline = selectedTimelines[i];
                _items.Add(new TimelineListItem
                {
                    Director = timeline,
                    IsEnabled = true,
                    Index = i
                });
            }
        }
        
        private void DrawTimelineItem(int index, Vector2 scrollPosition)
        {
            var item = _items[index];
            var rect = GUILayoutUtility.GetRect(0, UIStyles.ListItemHeight, GUILayout.ExpandWidth(true));
            var isSelected = index == _selectedIndex;
            var isHovered = rect.Contains(Event.current.mousePosition);
            
            // Draw background
            UIStyles.DrawAlternatingBackground(rect, index);
            UIStyles.DrawSelectionRect(rect, isSelected, isHovered);
            
            // Handle selection
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedIndex = index;
                GUI.FocusControl(null);
                Event.current.Use();
                
                // Notify selection change
                _eventBus.Publish(new UIRefreshRequestedEvent 
                { 
                    Scope = UIRefreshRequestedEvent.RefreshScope.RecorderList 
                });
            }
            
            // Draw content
            // Calculate rects
            var dragRect = new Rect(rect.x, rect.y, 20, rect.height);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // Drag handle
                GUI.Label(dragRect, "≡", EditorStyles.centeredGreyMiniLabel);
                
                // Enable checkbox
                var checkRect = new Rect(rect.x + 25, rect.y + 2, UIStyles.CheckboxWidth, rect.height - 4);
                item.IsEnabled = EditorGUI.Toggle(checkRect, item.IsEnabled);
                
                // Timeline name
                var nameRect = new Rect(rect.x + 50, rect.y, rect.width - 100, rect.height);
                var labelStyle = isSelected ? UIStyles.SelectedLabel : UIStyles.SelectableLabel;
                GUI.Label(nameRect, item.Director.name, labelStyle);
                
                // Delete button
                var deleteRect = new Rect(rect.xMax - 25, rect.y + 2, 20, rect.height - 4);
                if (GUI.Button(deleteRect, "×", EditorStyles.miniButton))
                {
                    _controller.RemoveTimeline(item.Director);
                }
            }
            
            // Handle drag start
            if (Event.current.type == EventType.MouseDrag && dragRect.Contains(Event.current.mousePosition))
            {
                _isDragging = true;
                _draggedIndex = index;
                Event.current.Use();
            }
        }
        
        private void HandleDragAndDrop()
        {
            if (!_isDragging) return;
            
            if (Event.current.type == EventType.MouseUp)
            {
                _isDragging = false;
                _draggedIndex = -1;
            }
            
            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
            {
                // Handle timeline drag and drop
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                
                if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    // Implement reordering logic here
                }
                
                Event.current.Use();
            }
        }
        
        private void OnTimelineSelectionChanged(TimelineSelectionChangedEvent e)
        {
            _selectedIndex = -1;
        }
        
        private class TimelineListItem
        {
            public PlayableDirector Director { get; set; }
            public bool IsEnabled { get; set; }
            public int Index { get; set; }
        }
    }
}