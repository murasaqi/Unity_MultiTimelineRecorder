using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Services;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.UI
{
    /// <summary>
    /// Editor window for managing scene configurations and settings
    /// </summary>
    public class ConfigurationManagementWindow : EditorWindow
    {
        private SceneConfigurationManager _configManager;
        private ConfigurationValidationService _validationService;
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private string[] _tabNames = { "Scene Settings", "Import/Export", "Validation", "Resource Usage" };
        
        // Scene settings
        private SceneConfiguration _currentConfig;
        private List<SceneConfiguration> _allConfigs;
        private string _selectedConfigName;
        
        // Import/Export
        private string _importExportJson = "";
        private string _exportFileName = "configuration_export";
        
        // Validation
        private ValidationResult _validationResult;
        private List<RepairSuggestion> _repairSuggestions;
        private bool _autoRepairEnabled = true;
        
        // Resource usage
        private ResourceUsagePrediction _resourcePrediction;
        
        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _successStyle;

        [MenuItem("Window/Multi Timeline Recorder/Configuration Management")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConfigurationManagementWindow>("Configuration Management");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            var logger = ServiceLocator.Instance.Get<MultiTimelineRecorder.Core.Interfaces.ILogger>() ?? new UnityConsoleLogger();
            var referenceService = ServiceLocator.Instance.Get<IGameObjectReferenceService>() ?? 
                                 new GameObjectReferenceService(logger, ServiceLocator.Instance.Get<IErrorHandlingService>());
            
            _configManager = new SceneConfigurationManager(logger, referenceService);
            _validationService = new ConfigurationValidationService(logger, referenceService);
            
            _configManager.ConfigurationLoaded += OnConfigurationLoaded;
            _configManager.ConfigurationSaved += OnConfigurationSaved;
            _configManager.ConfigurationError += OnConfigurationError;
            
            InitializeStyles();
            LoadConfigurations();
        }

        private void OnDisable()
        {
            if (_configManager != null)
            {
                _configManager.ConfigurationLoaded -= OnConfigurationLoaded;
                _configManager.ConfigurationSaved -= OnConfigurationSaved;
                _configManager.ConfigurationError -= OnConfigurationError;
                _configManager.Dispose();
            }
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            
            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.8f, 0.2f, 0.2f) }
            };
            
            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.8f, 0.6f, 0.2f) }
            };
            
            _successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Configuration Management", _headerStyle);
            EditorGUILayout.Space();

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0:
                    DrawSceneSettingsTab();
                    break;
                case 1:
                    DrawImportExportTab();
                    break;
                case 2:
                    DrawValidationTab();
                    break;
                case 3:
                    DrawResourceUsageTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSceneSettingsTab()
        {
            // Current scene info
            var currentScene = EditorSceneManager.GetActiveScene();
            EditorGUILayout.LabelField("Current Scene", _sectionStyle);
            EditorGUILayout.LabelField($"Name: {currentScene.name}");
            EditorGUILayout.LabelField($"Path: {currentScene.path}");
            
            if (_configManager.IsDirty)
            {
                EditorGUILayout.LabelField("* Unsaved changes", _warningStyle);
            }
            
            EditorGUILayout.Space();

            // Configuration selection
            EditorGUILayout.LabelField("Configuration", _sectionStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Config:", GUILayout.Width(100));
            
            var configNames = new List<string> { "Current Scene", "Global" };
            if (_allConfigs != null)
            {
                configNames.AddRange(_allConfigs.Where(c => c.SceneName != currentScene.name && c.SceneName != "GlobalConfiguration")
                                              .Select(c => c.SceneName));
            }
            
            var selectedIndex = configNames.IndexOf(_selectedConfigName);
            if (selectedIndex < 0) selectedIndex = 0;
            
            var newIndex = EditorGUILayout.Popup(selectedIndex, configNames.ToArray());
            if (newIndex != selectedIndex)
            {
                _selectedConfigName = configNames[newIndex];
                LoadSelectedConfiguration();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            // Configuration details
            if (_currentConfig != null)
            {
                EditorGUILayout.LabelField("Configuration Details", _sectionStyle);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField($"Created: {_currentConfig.CreatedDate}");
                EditorGUILayout.LabelField($"Modified: {_currentConfig.LastModifiedDate}");
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
                
                // Recording configuration summary
                if (_currentConfig.RecordingConfiguration != null)
                {
                    var recordingConfig = _currentConfig.RecordingConfiguration;
                    
                    EditorGUILayout.LabelField("Recording Settings", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Frame Rate: {recordingConfig.FrameRate} fps");
                    EditorGUILayout.LabelField($"Resolution: {recordingConfig.Resolution}");
                    EditorGUILayout.LabelField($"Output Path: {recordingConfig.OutputPath}");
                    EditorGUILayout.LabelField($"Timeline Count: {recordingConfig.TimelineConfigs.Count}");
                    
                    var totalRecorders = recordingConfig.TimelineConfigs
                        .OfType<TimelineRecorderConfig>()
                        .Sum(t => t.RecorderConfigs?.Count ?? 0);
                    EditorGUILayout.LabelField($"Total Recorders: {totalRecorders}");
                }
                
                EditorGUILayout.Space();
                
                // Actions
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Save", GUILayout.Width(100)))
                {
                    _configManager.SaveCurrentConfiguration();
                }
                
                if (GUILayout.Button("Save As...", GUILayout.Width(100)))
                {
                    SaveConfigurationAs();
                }
                
                if (_selectedConfigName != "Current Scene" && _selectedConfigName != "Global")
                {
                    if (GUILayout.Button("Delete", GUILayout.Width(100)))
                    {
                        if (EditorUtility.DisplayDialog("Delete Configuration", 
                            $"Are you sure you want to delete the configuration for '{_selectedConfigName}'?", 
                            "Delete", "Cancel"))
                        {
                            _configManager.DeleteConfiguration(_selectedConfigName);
                            LoadConfigurations();
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // Merge configurations
            EditorGUILayout.LabelField("Merge Configurations", _sectionStyle);
            DrawMergeSection();
        }

        private void DrawImportExportTab()
        {
            // Export section
            EditorGUILayout.LabelField("Export Configuration", _sectionStyle);
            
            EditorGUILayout.BeginHorizontal();
            _exportFileName = EditorGUILayout.TextField("File Name:", _exportFileName);
            EditorGUILayout.LabelField(".json", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Export Current", GUILayout.Width(120)))
            {
                if (_currentConfig != null)
                {
                    _importExportJson = _configManager.ExportConfiguration(_currentConfig);
                    EditorGUIUtility.systemCopyBuffer = _importExportJson;
                    ShowNotification(new GUIContent("Exported to clipboard"));
                }
            }
            
            if (GUILayout.Button("Export to File", GUILayout.Width(120)))
            {
                ExportToFile();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // Import section
            EditorGUILayout.LabelField("Import Configuration", _sectionStyle);
            
            EditorGUILayout.LabelField("JSON Data:");
            _importExportJson = EditorGUILayout.TextArea(_importExportJson, GUILayout.Height(200));
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Import from Clipboard", GUILayout.Width(150)))
            {
                _importExportJson = EditorGUIUtility.systemCopyBuffer;
            }
            
            if (GUILayout.Button("Import from File", GUILayout.Width(150)))
            {
                ImportFromFile();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Import to Current Scene", GUILayout.Width(150)))
            {
                ImportConfiguration(EditorSceneManager.GetActiveScene().name);
            }
            
            if (GUILayout.Button("Import as New...", GUILayout.Width(150)))
            {
                ImportAsNew();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationTab()
        {
            EditorGUILayout.LabelField("Configuration Validation", _sectionStyle);
            
            // Auto-repair option
            _autoRepairEnabled = EditorGUILayout.Toggle("Enable Auto-Repair", _autoRepairEnabled);
            
            EditorGUILayout.Space();
            
            // Validate button
            if (GUILayout.Button("Validate Current Configuration", GUILayout.Height(30)))
            {
                ValidateCurrentConfiguration();
            }
            
            // Validation results
            if (_validationResult != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation Results", _sectionStyle);
                
                if (_validationResult.IsValid)
                {
                    EditorGUILayout.LabelField("✓ Configuration is valid", _successStyle);
                }
                else
                {
                    var errors = _validationResult.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                    var warnings = _validationResult.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
                    
                    if (errors.Count > 0)
                    {
                        EditorGUILayout.LabelField($"Errors ({errors.Count})", _errorStyle);
                        foreach (var error in errors)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("•", GUILayout.Width(20));
                            EditorGUILayout.LabelField(error.Message, _errorStyle, GUILayout.ExpandWidth(true));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    if (warnings.Count > 0)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField($"Warnings ({warnings.Count})", _warningStyle);
                        foreach (var warning in warnings)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("•", GUILayout.Width(20));
                            EditorGUILayout.LabelField(warning.Message, _warningStyle, GUILayout.ExpandWidth(true));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    // Auto-repair button
                    if (_autoRepairEnabled && errors.Count > 0)
                    {
                        EditorGUILayout.Space();
                        if (GUILayout.Button("Attempt Auto-Repair", GUILayout.Height(25)))
                        {
                            AttemptAutoRepair();
                        }
                    }
                }
                
                // Repair suggestions
                if (_repairSuggestions != null && _repairSuggestions.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Repair Suggestions", _sectionStyle);
                    
                    foreach (var suggestion in _repairSuggestions)
                    {
                        EditorGUILayout.BeginVertical("box");
                        
                        var style = suggestion.Issue.Severity == ValidationSeverity.Error ? _errorStyle : _warningStyle;
                        EditorGUILayout.LabelField(suggestion.Issue.Message, style);
                        EditorGUILayout.LabelField(suggestion.Description, EditorStyles.wordWrappedLabel);
                        
                        if (suggestion.Steps.Count > 0)
                        {
                            EditorGUILayout.LabelField("Steps to fix:");
                            foreach (var step in suggestion.Steps)
                            {
                                EditorGUILayout.LabelField($"  • {step}", EditorStyles.wordWrappedLabel);
                            }
                        }
                        
                        EditorGUILayout.EndVertical();
                    }
                }
            }
        }

        private void DrawResourceUsageTab()
        {
            EditorGUILayout.LabelField("Resource Usage Prediction", _sectionStyle);
            
            if (GUILayout.Button("Calculate Resource Usage", GUILayout.Height(30)))
            {
                CalculateResourceUsage();
            }
            
            if (_resourcePrediction != null)
            {
                EditorGUILayout.Space();
                
                // Memory usage
                EditorGUILayout.LabelField("Memory Usage", EditorStyles.boldLabel);
                var memoryColor = GetColorForImpact(_resourcePrediction.PerformanceImpact);
                GUI.color = memoryColor;
                EditorGUILayout.LabelField($"Estimated: {_resourcePrediction.EstimatedMemoryUsageMB} MB");
                GUI.color = Color.white;
                
                // Disk usage
                EditorGUILayout.LabelField("Disk Usage", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Estimated: {_resourcePrediction.EstimatedDiskUsageMBPerMinute:F1} MB/minute");
                
                var hourlyUsage = _resourcePrediction.EstimatedDiskUsageMBPerMinute * 60;
                EditorGUILayout.LabelField($"Per Hour: {hourlyUsage:F1} MB ({hourlyUsage / 1024:F2} GB)");
                
                // CPU usage
                EditorGUILayout.LabelField("CPU Usage", EditorStyles.boldLabel);
                var cpuColor = _resourcePrediction.EstimatedCPUUsage > 80 ? _errorStyle.normal.textColor : 
                              _resourcePrediction.EstimatedCPUUsage > 50 ? _warningStyle.normal.textColor : 
                              _successStyle.normal.textColor;
                GUI.color = cpuColor;
                EditorGUILayout.LabelField($"Estimated: {_resourcePrediction.EstimatedCPUUsage:F0}%");
                GUI.color = Color.white;
                
                // Performance impact
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Performance Impact", EditorStyles.boldLabel);
                GUI.color = memoryColor;
                EditorGUILayout.LabelField(_resourcePrediction.PerformanceImpact.ToString());
                GUI.color = Color.white;
                
                // Warnings
                if (_resourcePrediction.Warnings.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Warnings", _warningStyle);
                    foreach (var warning in _resourcePrediction.Warnings)
                    {
                        EditorGUILayout.LabelField($"• {warning}", _warningStyle);
                    }
                }
                
                // Recommendations
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Recommendations", EditorStyles.boldLabel);
                DrawRecommendations();
            }
        }

        private void DrawMergeSection()
        {
            // This would be implemented with UI for selecting two configurations to merge
            EditorGUILayout.HelpBox("Configuration merging allows you to combine settings from different scenes or versions.", MessageType.Info);
        }

        private void DrawRecommendations()
        {
            if (_resourcePrediction == null) return;
            
            if (_resourcePrediction.PerformanceImpact == PerformanceImpact.High)
            {
                EditorGUILayout.LabelField("• Consider reducing resolution or frame rate");
                EditorGUILayout.LabelField("• Reduce the number of simultaneous recorders");
                EditorGUILayout.LabelField("• Use more efficient codec settings");
            }
            else if (_resourcePrediction.PerformanceImpact == PerformanceImpact.Medium)
            {
                EditorGUILayout.LabelField("• Monitor system resources during recording");
                EditorGUILayout.LabelField("• Consider closing other applications");
            }
            else
            {
                EditorGUILayout.LabelField("• Configuration should run smoothly");
                EditorGUILayout.LabelField("• System resources are within acceptable limits");
            }
        }

        // Helper methods

        private void LoadConfigurations()
        {
            _allConfigs = _configManager.GetAllConfigurations();
            _currentConfig = _configManager.CurrentConfiguration;
            
            if (_currentConfig != null)
            {
                _selectedConfigName = _currentConfig.SceneName;
            }
        }

        private void LoadSelectedConfiguration()
        {
            if (_selectedConfigName == "Current Scene")
            {
                _currentConfig = _configManager.CurrentConfiguration;
            }
            else if (_selectedConfigName == "Global")
            {
                _currentConfig = _configManager.GlobalConfiguration;
            }
            else
            {
                _currentConfig = _configManager.LoadConfiguration(_selectedConfigName);
            }
            
            // Clear validation results when switching configs
            _validationResult = null;
            _repairSuggestions = null;
            _resourcePrediction = null;
        }

        private void SaveConfigurationAs()
        {
            var sceneName = EditorUtility.SaveFilePanelInProject(
                "Save Configuration As", 
                "SceneConfig", 
                "mtr-config", 
                "Enter a name for the configuration");
            
            if (!string.IsNullOrEmpty(sceneName))
            {
                var configName = Path.GetFileNameWithoutExtension(sceneName);
                _currentConfig.SceneName = configName;
                _configManager.SaveConfiguration(_currentConfig);
                LoadConfigurations();
            }
        }

        private void ExportToFile()
        {
            var path = EditorUtility.SaveFilePanel(
                "Export Configuration", 
                Application.dataPath, 
                _exportFileName + ".json", 
                "json");
            
            if (!string.IsNullOrEmpty(path) && _currentConfig != null)
            {
                var json = _configManager.ExportConfiguration(_currentConfig);
                File.WriteAllText(path, json);
                ShowNotification(new GUIContent("Exported successfully"));
            }
        }

        private void ImportFromFile()
        {
            var path = EditorUtility.OpenFilePanel(
                "Import Configuration", 
                Application.dataPath, 
                "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                _importExportJson = File.ReadAllText(path);
            }
        }

        private void ImportConfiguration(string targetScene)
        {
            if (string.IsNullOrEmpty(_importExportJson))
            {
                ShowNotification(new GUIContent("No data to import"));
                return;
            }
            
            var config = _configManager.ImportConfiguration(_importExportJson, targetScene);
            if (config != null)
            {
                _configManager.SaveConfiguration(config);
                LoadConfigurations();
                ShowNotification(new GUIContent("Import successful"));
            }
            else
            {
                ShowNotification(new GUIContent("Import failed"));
            }
        }

        private void ImportAsNew()
        {
            var sceneName = EditorUtility.SaveFilePanelInProject(
                "Import as New Configuration", 
                "ImportedConfig", 
                "mtr-config", 
                "Enter a name for the imported configuration");
            
            if (!string.IsNullOrEmpty(sceneName))
            {
                var configName = Path.GetFileNameWithoutExtension(sceneName);
                ImportConfiguration(configName);
            }
        }

        private void ValidateCurrentConfiguration()
        {
            if (_currentConfig?.RecordingConfiguration == null)
            {
                ShowNotification(new GUIContent("No configuration to validate"));
                return;
            }
            
            _validationResult = _validationService.ValidateConfiguration(_currentConfig.RecordingConfiguration);
            _repairSuggestions = _validationService.GetRepairSuggestions(_validationResult);
        }

        private void AttemptAutoRepair()
        {
            if (_currentConfig?.RecordingConfiguration == null) return;
            
            var repairResult = _validationService.AutoRepairConfiguration(_currentConfig.RecordingConfiguration);
            
            if (repairResult.Success)
            {
                ShowNotification(new GUIContent("Auto-repair successful"));
                _configManager.MarkDirty();
                
                // Re-validate to show updated results
                ValidateCurrentConfiguration();
            }
            else
            {
                ShowNotification(new GUIContent($"Auto-repair partially successful: {repairResult.Message}"));
            }
        }

        private void CalculateResourceUsage()
        {
            if (_currentConfig?.RecordingConfiguration == null)
            {
                ShowNotification(new GUIContent("No configuration to analyze"));
                return;
            }
            
            _resourcePrediction = _validationService.PredictResourceUsage(_currentConfig.RecordingConfiguration);
        }

        private Color GetColorForImpact(PerformanceImpact impact)
        {
            return impact switch
            {
                PerformanceImpact.High => _errorStyle.normal.textColor,
                PerformanceImpact.Medium => _warningStyle.normal.textColor,
                _ => _successStyle.normal.textColor
            };
        }

        // Event handlers

        private void OnConfigurationLoaded(SceneConfiguration config)
        {
            _currentConfig = config;
            Repaint();
        }

        private void OnConfigurationSaved(SceneConfiguration config)
        {
            ShowNotification(new GUIContent($"Configuration saved: {config.SceneName}"));
            LoadConfigurations();
        }

        private void OnConfigurationError(string error)
        {
            ShowNotification(new GUIContent($"Error: {error}"));
        }
    }
}