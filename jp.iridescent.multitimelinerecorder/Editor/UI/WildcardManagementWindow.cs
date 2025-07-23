using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using MultiTimelineRecorder.Core.Models;

namespace MultiTimelineRecorder.UI
{
    /// <summary>
    /// Editor window for managing wildcards and templates
    /// </summary>
    public class WildcardManagementWindow : EditorWindow
    {
        private WildcardManagementSettings settings;
        private TemplateRegistry templateRegistry;
        
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "Wildcards", "Templates", "Import/Export" };
        
        // Wildcard editing
        private string newWildcardName = "";
        private string newWildcardDisplayName = "";
        private string newWildcardValue = "";
        private string newWildcardDescription = "";
        private string wildcardSearchFilter = "";
        
        // Template editing
        private string newTemplateName = "";
        private string newTemplateDescription = "";
        private string newTemplatePattern = "";
        private string newTemplateTag = "";
        private List<string> newTemplateTags = new List<string>();
        private string templateSearchFilter = "";
        private WildcardTemplate selectedTemplate;
        
        // Import/Export
        private string importExportJson = "";
        
        // Styles
        private GUIStyle wildcardLabelStyle;
        private GUIStyle wildcardValueStyle;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;

        [MenuItem("Window/Multi Timeline Recorder/Wildcard Management")]
        public static void ShowWindow()
        {
            var window = GetWindow<WildcardManagementWindow>("Wildcard Management");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            settings = WildcardManagementSettings.Instance;
            templateRegistry = new TemplateRegistry();
            
            // Load templates into registry
            if (settings.Templates != null)
            {
                foreach (var template in settings.Templates)
                {
                    templateRegistry.RegisterTemplate(template);
                }
            }
            
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            wildcardLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };
            
            wildcardValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Normal,
                fontSize = 10
            };
            
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14
            };
            
            boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
        }

        private void OnGUI()
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Failed to load wildcard settings.", MessageType.Error);
                return;
            }

            // Header
            EditorGUILayout.LabelField("Wildcard and Template Management", headerStyle);
            EditorGUILayout.Space();

            // Settings
            EditorGUILayout.BeginHorizontal();
            settings.AutoSaveChanges = EditorGUILayout.Toggle("Auto Save", settings.AutoSaveChanges);
            settings.ShowBuiltInWildcards = EditorGUILayout.Toggle("Show Built-in", settings.ShowBuiltInWildcards);
            settings.AllowDuplicateWildcards = EditorGUILayout.Toggle("Allow Duplicates", settings.AllowDuplicateWildcards);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();

            // Tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawWildcardsTab();
                    break;
                case 1:
                    DrawTemplatesTab();
                    break;
                case 2:
                    DrawImportExportTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            // Footer
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save", GUILayout.Width(100)))
            {
                settings.Save();
                ShowNotification(new GUIContent("Settings saved"));
            }
            
            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Reset to Defaults", 
                    "This will remove all custom wildcards and templates. Are you sure?", 
                    "Yes", "No"))
                {
                    settings.ResetToDefaults();
                    ShowNotification(new GUIContent("Reset to defaults"));
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWildcardsTab()
        {
            // Add new wildcard section
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Add Custom Wildcard", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Wildcard:", GUILayout.Width(80));
            newWildcardName = EditorGUILayout.TextField(newWildcardName);
            if (!newWildcardName.StartsWith("<") && !string.IsNullOrEmpty(newWildcardName))
            {
                EditorGUILayout.LabelField("(will be wrapped in < >)", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            newWildcardDisplayName = EditorGUILayout.TextField("Display Name:", newWildcardDisplayName);
            newWildcardValue = EditorGUILayout.TextField("Value:", newWildcardValue);
            newWildcardDescription = EditorGUILayout.TextField("Description:", newWildcardDescription);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Wildcard", GUILayout.Width(100)))
            {
                if (!string.IsNullOrEmpty(newWildcardName) && !string.IsNullOrEmpty(newWildcardValue))
                {
                    var wildcard = newWildcardName;
                    if (!wildcard.StartsWith("<")) wildcard = "<" + wildcard;
                    if (!wildcard.EndsWith(">")) wildcard = wildcard.TrimEnd('>') + ">";
                    
                    if (settings.AddCustomWildcard(wildcard, newWildcardDisplayName, newWildcardValue, newWildcardDescription))
                    {
                        newWildcardName = "";
                        newWildcardDisplayName = "";
                        newWildcardValue = "";
                        newWildcardDescription = "";
                        ShowNotification(new GUIContent("Wildcard added"));
                    }
                    else
                    {
                        ShowNotification(new GUIContent("Failed to add wildcard"));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Search
            wildcardSearchFilter = EditorGUILayout.TextField("Search:", wildcardSearchFilter);
            
            // Display wildcards
            var categorizedWildcards = settings.WildcardRegistry.GetWildcardsByCategory();
            
            foreach (var category in categorizedWildcards)
            {
                if (!settings.ShowBuiltInWildcards && category.Key != "Custom")
                    continue;
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(category.Key, EditorStyles.boldLabel);
                
                foreach (var wildcard in category.Value)
                {
                    if (!string.IsNullOrEmpty(wildcardSearchFilter) && 
                        !wildcard.Wildcard.ToLower().Contains(wildcardSearchFilter.ToLower()) &&
                        !wildcard.DisplayName.ToLower().Contains(wildcardSearchFilter.ToLower()))
                        continue;
                    
                    DrawWildcardEntry(wildcard);
                }
            }
        }

        private void DrawWildcardEntry(WildcardDefinition wildcard)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(wildcard.Wildcard, wildcardLabelStyle, GUILayout.Width(150));
            EditorGUILayout.LabelField(wildcard.DisplayName, GUILayout.Width(150));
            
            if (wildcard.ProcessingType == WildcardProcessingType.Custom)
            {
                EditorGUILayout.LabelField($"→ {wildcard.CustomValue}", wildcardValueStyle);
                
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    // TODO: Implement edit dialog
                }
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Remove Wildcard", 
                        $"Remove wildcard {wildcard.Wildcard}?", 
                        "Yes", "No"))
                    {
                        settings.RemoveCustomWildcard(wildcard.Wildcard);
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField($"[{wildcard.ProcessingType}]", wildcardValueStyle);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (!string.IsNullOrEmpty(wildcard.Description))
            {
                EditorGUILayout.LabelField(wildcard.Description, EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawTemplatesTab()
        {
            // Add new template section
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Add Template", EditorStyles.boldLabel);
            
            newTemplateName = EditorGUILayout.TextField("Name:", newTemplateName);
            newTemplateDescription = EditorGUILayout.TextField("Description:", newTemplateDescription);
            
            EditorGUILayout.LabelField("Pattern:");
            newTemplatePattern = EditorGUILayout.TextArea(newTemplatePattern, GUILayout.Height(40));
            
            // Tags
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tags:", GUILayout.Width(50));
            newTemplateTag = EditorGUILayout.TextField(newTemplateTag);
            if (GUILayout.Button("Add Tag", GUILayout.Width(70)))
            {
                if (!string.IsNullOrEmpty(newTemplateTag) && !newTemplateTags.Contains(newTemplateTag))
                {
                    newTemplateTags.Add(newTemplateTag);
                    newTemplateTag = "";
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (newTemplateTags.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(50));
                foreach (var tag in newTemplateTags.ToList())
                {
                    if (GUILayout.Button($"{tag} ×", GUILayout.Width(80)))
                    {
                        newTemplateTags.Remove(tag);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Available wildcards helper
            if (GUILayout.Button("Show Available Wildcards", GUILayout.Width(150)))
            {
                ShowWildcardHelperMenu();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Template", GUILayout.Width(100)))
            {
                if (!string.IsNullOrEmpty(newTemplateName) && !string.IsNullOrEmpty(newTemplatePattern))
                {
                    var template = new WildcardTemplate
                    {
                        Name = newTemplateName,
                        Description = newTemplateDescription,
                        Pattern = newTemplatePattern,
                        IsBuiltIn = false,
                        Tags = new List<string>(newTemplateTags)
                    };
                    
                    settings.AddTemplate(template);
                    templateRegistry.RegisterTemplate(template);
                    
                    newTemplateName = "";
                    newTemplateDescription = "";
                    newTemplatePattern = "";
                    newTemplateTags.Clear();
                    
                    ShowNotification(new GUIContent("Template added"));
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Search
            templateSearchFilter = EditorGUILayout.TextField("Search:", templateSearchFilter);
            
            // Display templates
            var categorizedTemplates = templateRegistry.GetTemplatesByCategory();
            
            foreach (var category in categorizedTemplates)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(category.Key, EditorStyles.boldLabel);
                
                foreach (var template in category.Value)
                {
                    if (!string.IsNullOrEmpty(templateSearchFilter) && 
                        !template.Name.ToLower().Contains(templateSearchFilter.ToLower()) &&
                        !template.Pattern.ToLower().Contains(templateSearchFilter.ToLower()))
                        continue;
                    
                    DrawTemplateEntry(template);
                }
            }
        }

        private void DrawTemplateEntry(WildcardTemplate template)
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(template.Name, wildcardLabelStyle);
            
            if (!template.IsBuiltIn)
            {
                if (GUILayout.Button("Use", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = template.Pattern;
                    ShowNotification(new GUIContent("Pattern copied to clipboard"));
                }
                
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    // TODO: Implement edit dialog
                    selectedTemplate = template;
                }
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Remove Template", 
                        $"Remove template '{template.Name}'?", 
                        "Yes", "No"))
                    {
                        settings.RemoveTemplate(template.Name);
                        templateRegistry.UnregisterTemplate(template.Name);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = template.Pattern;
                    ShowNotification(new GUIContent("Pattern copied to clipboard"));
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField("Pattern: " + template.Pattern, EditorStyles.wordWrappedLabel);
            
            if (!string.IsNullOrEmpty(template.Description))
            {
                EditorGUILayout.LabelField(template.Description, EditorStyles.miniLabel);
            }
            
            if (template.Tags != null && template.Tags.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Tags:", EditorStyles.miniLabel, GUILayout.Width(40));
                foreach (var tag in template.Tags)
                {
                    EditorGUILayout.LabelField(tag, EditorStyles.miniLabel, GUILayout.Width(60));
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawImportExportTab()
        {
            // Export section
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Export to JSON", GUILayout.Width(150)))
            {
                importExportJson = settings.ExportToJson();
                EditorGUIUtility.systemCopyBuffer = importExportJson;
                ShowNotification(new GUIContent("Exported to clipboard"));
            }
            
            if (GUILayout.Button("Save to File", GUILayout.Width(150)))
            {
                var path = EditorUtility.SaveFilePanel("Save Wildcard Settings", "", "wildcard_settings.json", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    System.IO.File.WriteAllText(path, settings.ExportToJson());
                    ShowNotification(new GUIContent("Saved to file"));
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space();
            
            // Import section
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Import Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("JSON Data:");
            importExportJson = EditorGUILayout.TextArea(importExportJson, GUILayout.Height(200));
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Import from Clipboard", GUILayout.Width(150)))
            {
                importExportJson = EditorGUIUtility.systemCopyBuffer;
            }
            
            if (GUILayout.Button("Load from File", GUILayout.Width(150)))
            {
                var path = EditorUtility.OpenFilePanel("Load Wildcard Settings", "", "json");
                if (!string.IsNullOrEmpty(path))
                {
                    importExportJson = System.IO.File.ReadAllText(path);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Import (Merge)", GUILayout.Width(100)))
            {
                if (settings.ImportFromJson(importExportJson, false))
                {
                    ShowNotification(new GUIContent("Import successful"));
                    OnEnable(); // Reload
                }
                else
                {
                    ShowNotification(new GUIContent("Import failed"));
                }
            }
            
            if (GUILayout.Button("Import (Overwrite)", GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Import and Overwrite", 
                    "This will replace all custom wildcards and templates. Are you sure?", 
                    "Yes", "No"))
                {
                    if (settings.ImportFromJson(importExportJson, true))
                    {
                        ShowNotification(new GUIContent("Import successful"));
                        OnEnable(); // Reload
                    }
                    else
                    {
                        ShowNotification(new GUIContent("Import failed"));
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void ShowWildcardHelperMenu()
        {
            var menu = new GenericMenu();
            var allWildcards = settings.WildcardRegistry.GetAllWildcards();
            
            foreach (var category in settings.WildcardRegistry.GetWildcardsByCategory())
            {
                foreach (var wildcard in category.Value)
                {
                    var w = wildcard;
                    menu.AddItem(new GUIContent($"{category.Key}/{w.Wildcard} - {w.DisplayName}"), 
                        false, 
                        () => {
                            newTemplatePattern += w.Wildcard;
                        });
                }
            }
            
            menu.ShowAsContext();
        }
    }
}