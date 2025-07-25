using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Events;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.UI.Controllers;
using MultiTimelineRecorder.UI.Components;
using MultiTimelineRecorder.UI.Styles;

namespace MultiTimelineRecorder.UI.Windows
{
    /// <summary>
    /// Main window view for Multi Timeline Recorder
    /// </summary>
    public class MainWindowView : EditorWindow
    {
        // Controllers
        private MainWindowController _mainController;
        private RecorderConfigurationController _recorderController;
        
        // UI State
        private Vector2 _timelineScrollPos;
        private Vector2 _recorderScrollPos;
        private Vector2 _settingsScrollPos;
        private int _selectedTimelineIndex = -1;
        private bool _showGlobalSettings = false;
        
        // UI Components
        private TimelineListComponent _timelineList;
        private RecorderListComponent _recorderList;
        private GlobalSettingsComponent _globalSettings;
        private ValidationPanelComponent _validationPanel;
        
        // Layout settings
        private float _leftColumnWidth = 300f;
        private float _middleColumnWidth = 350f;
        private bool _isResizingLeft = false;
        private bool _isResizingMiddle = false;
        
        // Services
        private IEventBus _eventBus;
        private MultiTimelineRecorder.Core.Interfaces.ILogger _logger;

        [MenuItem("Window/Multi Timeline Recorder (New)")]
        public static void ShowWindow()
        {
            var window = GetWindow<MainWindowView>("Multi Timeline Recorder");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeServices();
            InitializeComponents();
            SubscribeToEvents();
            RefreshUI();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            ServiceLocator.ResetInstance();
        }

        private void InitializeServices()
        {
            // Initialize service locator
            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.Initialize();
            
            // Get services
            _mainController = serviceLocator.GetMainController();
            _recorderController = serviceLocator.GetRecorderController();
            _eventBus = serviceLocator.GetEventBus();
            _logger = serviceLocator.GetLogger();
            
            _logger.LogInfo("MainWindowView initialized", LogCategory.UI);
        }

        private void InitializeComponents()
        {
            _timelineList = new TimelineListComponent(_mainController, _eventBus);
            _recorderList = new RecorderListComponent(_recorderController, _eventBus);
            _globalSettings = new GlobalSettingsComponent(_mainController, _eventBus);
            _validationPanel = new ValidationPanelComponent(_mainController, _eventBus);
        }

        private void SubscribeToEvents()
        {
            _eventBus.Subscribe<UIRefreshRequestedEvent>(OnUIRefreshRequested);
            _eventBus.Subscribe<RecordingStartedEvent>(OnRecordingStarted);
            _eventBus.Subscribe<RecordingCompletedEvent>(OnRecordingCompleted);
            _eventBus.Subscribe<RecordingProgressEvent>(OnRecordingProgress);
            _eventBus.Subscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
        }

        private void UnsubscribeFromEvents()
        {
            _eventBus.Unsubscribe<UIRefreshRequestedEvent>(OnUIRefreshRequested);
            _eventBus.Unsubscribe<RecordingStartedEvent>(OnRecordingStarted);
            _eventBus.Unsubscribe<RecordingCompletedEvent>(OnRecordingCompleted);
            _eventBus.Unsubscribe<RecordingProgressEvent>(OnRecordingProgress);
            _eventBus.Unsubscribe<TimelineSelectionChangedEvent>(OnTimelineSelectionChanged);
        }

        private void OnGUI()
        {
            // Handle keyboard shortcuts
            HandleKeyboardShortcuts();
            
            DrawToolbar();
            DrawRecordingHeader();
            DrawMainContent();
            DrawStatusBar();
            
            HandleResizing();
            
            // Process any deferred actions
            if (Event.current.type == EventType.Layout)
            {
                ProcessDeferredActions();
            }
        }
        
        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Ctrl+R or Cmd+R for recording toggle
                if ((e.control || e.command) && e.keyCode == KeyCode.R)
                {
                    if (_mainController.IsRecording)
                    {
                        _mainController.StopRecording();
                    }
                    else
                    {
                        _mainController.StartRecording();
                    }
                    e.Use();
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Refresh button
                if (GUILayout.Button(new GUIContent("Refresh", "Refresh timeline list"), EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    _mainController.RefreshTimelines(true);
                }
                
                // Settings button
                _showGlobalSettings = GUILayout.Toggle(_showGlobalSettings, 
                    new GUIContent("Settings", "Show global settings"), 
                    EditorStyles.toolbarButton, GUILayout.Width(60));
                
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawRecordingHeader()
        {
            // Create a prominent recording control area
            using (new EditorGUILayout.VerticalScope(GUILayout.Height(80)))
            {
                GUILayout.Space(10);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    
                    // Recording controls with large, prominent buttons
                    if (!_mainController.IsRecording)
                    {
                        // Start Recording Button
                        var originalColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f); // Red color
                        
                        var recordButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 16,
                            fontStyle = FontStyle.Bold,
                            fixedHeight = 40,
                            fixedWidth = 200
                        };
                        
                        var recordContent = new GUIContent("● START RECORDING", "Start recording selected timelines (Ctrl+R)");
                        if (GUILayout.Button(recordContent, recordButtonStyle))
                        {
                            _mainController.StartRecording();
                        }
                        
                        GUI.backgroundColor = originalColor;
                    }
                    else
                    {
                        // Recording in progress - show animated indicator
                        var originalColor = GUI.backgroundColor;
                        
                        // Blinking effect
                        var blinkColor = Mathf.PingPong((float)EditorApplication.timeSinceStartup * 2f, 1f);
                        GUI.backgroundColor = Color.Lerp(Color.red, new Color(0.5f, 0f, 0f), blinkColor);
                        
                        var stopButtonStyle = new GUIStyle(GUI.skin.button)
                        {
                            fontSize = 16,
                            fontStyle = FontStyle.Bold,
                            fixedHeight = 40,
                            fixedWidth = 200
                        };
                        
                        var stopContent = new GUIContent("■ STOP RECORDING", "Stop recording (Ctrl+R)");
                        if (GUILayout.Button(stopContent, stopButtonStyle))
                        {
                            _mainController.StopRecording();
                        }
                        
                        GUI.backgroundColor = originalColor;
                        
                        // Show recording progress
                        GUILayout.Space(20);
                        var progress = _mainController.GetRecordingProgress();
                        if (progress != null)
                        {
                            using (new EditorGUILayout.VerticalScope(GUILayout.Width(200)))
                            {
                                var progressText = $"Recording: {progress.CurrentFrame}/{progress.TotalFrames}";
                                EditorGUILayout.LabelField(progressText, EditorStyles.boldLabel);
                                
                                var rect = GUILayoutUtility.GetRect(200, 20);
                                EditorGUI.ProgressBar(rect, progress.Progress, $"{progress.Progress:P0}");
                            }
                        }
                    }
                    
                    GUILayout.FlexibleSpace();
                }
                
                GUILayout.Space(10);
                
                // Add separator line
                var separatorRect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }
        }

        private void DrawMainContent()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Left column - Timeline list
                DrawTimelineColumn();
                
                // Left resizer
                DrawColumnResizer(ref _leftColumnWidth, ref _isResizingLeft);
                
                // Middle column - Recorder list
                DrawRecorderColumn();
                
                // Right resizer
                DrawColumnResizer(ref _middleColumnWidth, ref _isResizingMiddle);
                
                // Right column - Settings or global settings
                DrawSettingsColumn();
            }
        }

        private void DrawTimelineColumn()
        {
            using (new EditorGUILayout.VerticalScope(UIStyles.ColumnBackground, GUILayout.Width(_leftColumnWidth)))
            {
                DrawColumnHeader("Timelines");
                
                _timelineScrollPos = EditorGUILayout.BeginScrollView(_timelineScrollPos);
                _timelineList.Draw(_timelineScrollPos);
                EditorGUILayout.EndScrollView();
                
                // Add timeline button
                if (GUILayout.Button("Add Timeline", GUILayout.Height(25)))
                {
                    ShowTimelineSelectionMenu();
                }
            }
        }

        private void DrawRecorderColumn()
        {
            using (new EditorGUILayout.VerticalScope(UIStyles.ColumnBackground, GUILayout.Width(_middleColumnWidth)))
            {
                DrawColumnHeader("Recorders");
                
                if (_selectedTimelineIndex >= 0 && _selectedTimelineIndex < _mainController.SelectedTimelines.Count)
                {
                    // Show selected timeline name
                    var selectedTimeline = _mainController.SelectedTimelines[_selectedTimelineIndex];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Timeline:", GUILayout.Width(60));
                    GUI.enabled = false;
                    GUILayout.Label(selectedTimeline.name, EditorStyles.miniLabel);
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                    
                    _recorderScrollPos = EditorGUILayout.BeginScrollView(_recorderScrollPos);
                    _recorderList.Draw(_recorderScrollPos);
                    EditorGUILayout.EndScrollView();
                    
                    // Add recorder button
                    if (GUILayout.Button("Add Recorder", GUILayout.Height(25)))
                    {
                        ShowRecorderTypeMenu();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a timeline to configure recorders", MessageType.Info);
                }
            }
        }

        private void DrawSettingsColumn()
        {
            using (new EditorGUILayout.VerticalScope(UIStyles.ColumnBackground, GUILayout.ExpandWidth(true)))
            {
                if (_showGlobalSettings)
                {
                    DrawColumnHeader("Global Settings");
                    _settingsScrollPos = EditorGUILayout.BeginScrollView(_settingsScrollPos);
                    _globalSettings.Draw();
                    EditorGUILayout.EndScrollView();
                }
                else if (_recorderController.SelectedRecorder != null)
                {
                    DrawColumnHeader("Recorder Settings");
                    _settingsScrollPos = EditorGUILayout.BeginScrollView(_settingsScrollPos);
                    DrawRecorderSettings();
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    DrawColumnHeader("Validation");
                    _validationPanel.Draw();
                }
            }
        }

        private void DrawColumnHeader(string title)
        {
            using (new EditorGUILayout.HorizontalScope(UIStyles.HeaderBackground, GUILayout.Height(25)))
            {
                GUILayout.Label(title, UIStyles.HeaderLabel);
            }
        }

        private void DrawColumnResizer(ref float columnWidth, ref bool isResizing)
        {
            var resizerRect = GUILayoutUtility.GetRect(5f, 5f, GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(resizerRect, MouseCursor.ResizeHorizontal);
            
            if (Event.current.type == EventType.MouseDown && resizerRect.Contains(Event.current.mousePosition))
            {
                isResizing = true;
                Event.current.Use();
            }
            
            if (isResizing && Event.current.type == EventType.MouseDrag)
            {
                columnWidth += Event.current.delta.x;
                columnWidth = Mathf.Clamp(columnWidth, 200f, 500f);
                Repaint();
                Event.current.Use();
            }
            
            if (Event.current.type == EventType.MouseUp)
            {
                isResizing = false;
            }
        }

        private void DrawRecorderSettings()
        {
            var recorder = _recorderController.SelectedRecorder;
            if (recorder == null) return;
            
            // Use the appropriate editor component based on recorder type
            var editorComponent = RecorderEditorFactory.CreateEditor(recorder, _recorderController, _eventBus);
            if (editorComponent != null)
            {
                editorComponent.Draw();
            }
            else
            {
                EditorGUILayout.HelpBox($"No editor available for {recorder.Type} recorder", MessageType.Warning);
            }
        }

        private void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(UIStyles.StatusBarBackground, GUILayout.Height(20)))
            {
                if (_mainController.IsRecording)
                {
                    var progress = _mainController.GetRecordingProgress();
                    if (progress != null)
                    {
                        var progressText = $"Recording: {progress.CurrentFrame}/{progress.TotalFrames} ({progress.Progress:P0})";
                        EditorGUI.ProgressBar(
                            GUILayoutUtility.GetRect(200, 16), 
                            progress.Progress, 
                            progressText);
                    }
                }
                else
                {
                    var timelineCount = _mainController.SelectedTimelines.Count;
                    var recorderCount = GetTotalRecorderCount();
                    GUILayout.Label($"Timelines: {timelineCount} | Recorders: {recorderCount}", UIStyles.StatusLabel);
                }
                
                GUILayout.FlexibleSpace();
            }
        }

        private void ShowTimelineSelectionMenu()
        {
            var menu = new GenericMenu();
            var availableTimelines = _mainController.AvailableTimelines;
            
            foreach (var timeline in availableTimelines)
            {
                if (!_mainController.SelectedTimelines.Contains(timeline))
                {
                    menu.AddItem(new GUIContent(timeline.name), false, () => 
                    {
                        _mainController.AddTimeline(timeline);
                    });
                }
            }
            
            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No available timelines"));
            }
            
            menu.ShowAsContext();
        }

        private void ShowRecorderTypeMenu()
        {
            var menu = new GenericMenu();
            var availableTypes = _recorderController.AvailableRecorderTypes;
            
            foreach (var type in availableTypes)
            {
                menu.AddItem(new GUIContent(type.ToString()), false, () => 
                {
                    _recorderController.AddRecorder(type);
                });
            }
            
            menu.ShowAsContext();
        }

        private void RefreshUI()
        {
            _mainController.RefreshTimelines();
            Repaint();
        }

        private int GetTotalRecorderCount()
        {
            var config = _mainController.CurrentConfiguration;
            if (config == null) return 0;
            
            return config.TimelineConfigs
                .Where(t => t.IsEnabled)
                .Sum(t => t.RecorderConfigs.Count(r => r.IsEnabled));
        }

        private void HandleResizing()
        {
            if (_isResizingLeft || _isResizingMiddle)
            {
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), MouseCursor.ResizeHorizontal);
            }
        }

        private void ProcessDeferredActions()
        {
            // Process any deferred UI actions here
        }

        // Event handlers
        private void OnUIRefreshRequested(UIRefreshRequestedEvent e)
        {
            Repaint();
        }

        private void OnRecordingStarted(RecordingStartedEvent e)
        {
            Repaint();
        }

        private void OnRecordingCompleted(RecordingCompletedEvent e)
        {
            ShowNotification(new GUIContent("Recording completed successfully"), 3f);
            Repaint();
        }

        private void OnRecordingProgress(RecordingProgressEvent e)
        {
            Repaint();
        }

        private void OnTimelineSelectionChanged(TimelineSelectionChangedEvent e)
        {
            if (e.SelectedTimelines.Count > 0 && e.SelectedIndex >= 0)
            {
                _selectedTimelineIndex = e.SelectedIndex;
                
                // Update recorder controller with the selected timeline's config
                var config = _mainController.CurrentConfiguration as RecordingConfiguration;
                if (config != null && e.SelectedIndex < e.SelectedTimelines.Count)
                {
                    var selectedTimeline = e.SelectedTimelines[e.SelectedIndex];
                    var timelineConfig = config.TimelineConfigs
                        .FirstOrDefault(t => t is TimelineRecorderConfig trc && 
                                           trc.Director == selectedTimeline) as TimelineRecorderConfig;
                    
                    if (timelineConfig != null)
                    {
                        _recorderController.SetTimelineConfig(timelineConfig);
                    }
                }
            }
            else
            {
                _selectedTimelineIndex = -1;
            }
            
            Repaint();
        }
    }
}