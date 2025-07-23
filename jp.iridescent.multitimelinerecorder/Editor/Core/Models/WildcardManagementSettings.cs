using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Settings for managing wildcards and templates
    /// </summary>
    [CreateAssetMenu(fileName = "WildcardManagementSettings", menuName = "Multi Timeline Recorder/Wildcard Management Settings")]
    public class WildcardManagementSettings : ScriptableObject
    {
        private const string DefaultSettingsPath = "Assets/MultiTimelineRecorder/Settings/WildcardManagementSettings.asset";
        private const string EditorPrefsKey = "MultiTimelineRecorder.WildcardSettings";
        
        [SerializeField]
        private WildcardRegistry wildcardRegistry = new WildcardRegistry();
        
        [SerializeField]
        private List<WildcardTemplate> templates = new List<WildcardTemplate>();
        
        [SerializeField]
        private bool autoSaveChanges = true;
        
        [SerializeField]
        private bool showBuiltInWildcards = true;
        
        [SerializeField]
        private bool allowDuplicateWildcards = false;

        /// <summary>
        /// The wildcard registry containing all wildcard definitions
        /// </summary>
        public WildcardRegistry WildcardRegistry
        {
            get => wildcardRegistry;
            set => wildcardRegistry = value ?? new WildcardRegistry();
        }

        /// <summary>
        /// List of wildcard templates
        /// </summary>
        public List<WildcardTemplate> Templates
        {
            get => templates;
            set => templates = value ?? new List<WildcardTemplate>();
        }

        /// <summary>
        /// Whether to automatically save changes
        /// </summary>
        public bool AutoSaveChanges
        {
            get => autoSaveChanges;
            set => autoSaveChanges = value;
        }

        /// <summary>
        /// Whether to show built-in wildcards in UI
        /// </summary>
        public bool ShowBuiltInWildcards
        {
            get => showBuiltInWildcards;
            set => showBuiltInWildcards = value;
        }

        /// <summary>
        /// Whether to allow duplicate wildcard names
        /// </summary>
        public bool AllowDuplicateWildcards
        {
            get => allowDuplicateWildcards;
            set => allowDuplicateWildcards = value;
        }

        /// <summary>
        /// Gets or creates the singleton instance
        /// </summary>
        public static WildcardManagementSettings Instance
        {
            get
            {
                // Try to load from assets
                var settings = AssetDatabase.LoadAssetAtPath<WildcardManagementSettings>(DefaultSettingsPath);
                
                if (settings == null)
                {
                    // Try to find in project
                    var guids = AssetDatabase.FindAssets("t:WildcardManagementSettings");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        settings = AssetDatabase.LoadAssetAtPath<WildcardManagementSettings>(path);
                    }
                }
                
                if (settings == null)
                {
                    // Create new instance
                    settings = CreateInstance<WildcardManagementSettings>();
                    settings.InitializeDefaults();
                    
                    // Save to default location
                    var directory = System.IO.Path.GetDirectoryName(DefaultSettingsPath);
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    
                    AssetDatabase.CreateAsset(settings, DefaultSettingsPath);
                    AssetDatabase.SaveAssets();
                }
                
                return settings;
            }
        }

        /// <summary>
        /// Initialize with default values
        /// </summary>
        private void InitializeDefaults()
        {
            // Add some default templates
            templates.Add(new WildcardTemplate
            {
                Name = "Standard Output",
                Description = "Standard naming pattern for general recordings",
                Pattern = "<Scene>_<Timeline>_<RecorderType>_Take<Take:0000>",
                IsBuiltIn = true
            });
            
            templates.Add(new WildcardTemplate
            {
                Name = "Date-based Output",
                Description = "Naming pattern with date and time",
                Pattern = "<Date>_<Time>_<Scene>_<Timeline>_<RecorderType>",
                IsBuiltIn = true
            });
            
            templates.Add(new WildcardTemplate
            {
                Name = "Production Output",
                Description = "Professional naming pattern for production use",
                Pattern = "<Product>_<Scene>_<Timeline>_<Resolution>_<RecorderType>_v<Take:000>",
                IsBuiltIn = true
            });
        }

        /// <summary>
        /// Adds a custom wildcard
        /// </summary>
        public bool AddCustomWildcard(string wildcard, string displayName, string value, string description = "")
        {
            if (!ValidateWildcard(wildcard, value))
                return false;
            
            wildcardRegistry.AddCustomWildcard(wildcard, displayName, value, description);
            
            if (autoSaveChanges)
                Save();
            
            return true;
        }

        /// <summary>
        /// Updates a custom wildcard
        /// </summary>
        public bool UpdateCustomWildcard(string wildcard, string newDisplayName, string newValue, string newDescription = "")
        {
            if (!wildcardRegistry.IsCustomWildcard(wildcard))
                return false;
            
            wildcardRegistry.RemoveCustomWildcard(wildcard);
            return AddCustomWildcard(wildcard, newDisplayName, newValue, newDescription);
        }

        /// <summary>
        /// Removes a custom wildcard
        /// </summary>
        public bool RemoveCustomWildcard(string wildcard)
        {
            var result = wildcardRegistry.RemoveCustomWildcard(wildcard);
            
            if (result && autoSaveChanges)
                Save();
            
            return result;
        }

        /// <summary>
        /// Adds a template
        /// </summary>
        public void AddTemplate(WildcardTemplate template)
        {
            if (template == null || templates.Any(t => t.Name == template.Name))
                return;
            
            templates.Add(template);
            
            if (autoSaveChanges)
                Save();
        }

        /// <summary>
        /// Updates a template
        /// </summary>
        public bool UpdateTemplate(string templateName, WildcardTemplate newTemplate)
        {
            var index = templates.FindIndex(t => t.Name == templateName);
            if (index < 0)
                return false;
            
            templates[index] = newTemplate;
            
            if (autoSaveChanges)
                Save();
            
            return true;
        }

        /// <summary>
        /// Removes a template
        /// </summary>
        public bool RemoveTemplate(string templateName)
        {
            var removed = templates.RemoveAll(t => t.Name == templateName && !t.IsBuiltIn) > 0;
            
            if (removed && autoSaveChanges)
                Save();
            
            return removed;
        }

        /// <summary>
        /// Gets a template by name
        /// </summary>
        public WildcardTemplate GetTemplate(string name)
        {
            return templates.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// Validates a wildcard
        /// </summary>
        private bool ValidateWildcard(string wildcard, string value)
        {
            if (string.IsNullOrEmpty(wildcard) || string.IsNullOrEmpty(value))
                return false;
            
            // Check for duplicates
            if (!allowDuplicateWildcards)
            {
                var allWildcards = wildcardRegistry.GetAllWildcards();
                if (allWildcards.ContainsKey(wildcard))
                    return false;
            }
            
            // Validate format
            if (!wildcard.StartsWith("<") || !wildcard.EndsWith(">"))
                return false;
            
            // Check for reserved characters
            var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '|' };
            if (value.Any(c => invalidChars.Contains(c)))
                return false;
            
            return true;
        }

        /// <summary>
        /// Exports settings to JSON
        /// </summary>
        public string ExportToJson()
        {
            var exportData = new WildcardSettingsExportData
            {
                CustomWildcards = wildcardRegistry.CustomWildcards.Values.ToList(),
                Templates = templates.Where(t => !t.IsBuiltIn).ToList(),
                ExportDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            return JsonUtility.ToJson(exportData, true);
        }

        /// <summary>
        /// Imports settings from JSON
        /// </summary>
        public bool ImportFromJson(string json, bool overwrite = false)
        {
            try
            {
                var importData = JsonUtility.FromJson<WildcardSettingsExportData>(json);
                if (importData == null)
                    return false;
                
                // Import custom wildcards
                if (overwrite)
                    wildcardRegistry.CustomWildcards.Clear();
                
                foreach (var wildcard in importData.CustomWildcards)
                {
                    if (!wildcardRegistry.CustomWildcards.ContainsKey(wildcard.Wildcard))
                    {
                        wildcardRegistry.CustomWildcards[wildcard.Wildcard] = wildcard;
                    }
                }
                
                // Import templates
                if (overwrite)
                    templates.RemoveAll(t => !t.IsBuiltIn);
                
                foreach (var template in importData.Templates)
                {
                    if (!templates.Any(t => t.Name == template.Name))
                    {
                        templates.Add(template);
                    }
                }
                
                if (autoSaveChanges)
                    Save();
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the settings
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Resets to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            wildcardRegistry.CustomWildcards.Clear();
            templates.RemoveAll(t => !t.IsBuiltIn);
            
            if (autoSaveChanges)
                Save();
        }
    }

    /// <summary>
    /// Wildcard template definition
    /// </summary>
    [Serializable]
    public class WildcardTemplate
    {
        [SerializeField]
        private string name;
        
        [SerializeField]
        private string description;
        
        [SerializeField]
        private string pattern;
        
        [SerializeField]
        private bool isBuiltIn;
        
        [SerializeField]
        private List<string> tags = new List<string>();

        public string Name
        {
            get => name;
            set => name = value;
        }

        public string Description
        {
            get => description;
            set => description = value;
        }

        public string Pattern
        {
            get => pattern;
            set => pattern = value;
        }

        public bool IsBuiltIn
        {
            get => isBuiltIn;
            set => isBuiltIn = value;
        }

        public List<string> Tags
        {
            get => tags;
            set => tags = value ?? new List<string>();
        }
    }

    /// <summary>
    /// Data structure for import/export
    /// </summary>
    [Serializable]
    public class WildcardSettingsExportData
    {
        public List<WildcardDefinition> CustomWildcards;
        public List<WildcardTemplate> Templates;
        public string ExportDate;
    }
}