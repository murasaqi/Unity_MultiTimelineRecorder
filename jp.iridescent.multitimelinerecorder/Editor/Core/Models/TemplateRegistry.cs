using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Registry for managing wildcard templates
    /// </summary>
    [Serializable]
    public class TemplateRegistry
    {
        [SerializeField]
        private List<WildcardTemplate> templates = new List<WildcardTemplate>();
        
        [SerializeField]
        private Dictionary<string, List<string>> tagIndex = new Dictionary<string, List<string>>();
        
        /// <summary>
        /// All registered templates
        /// </summary>
        public List<WildcardTemplate> Templates
        {
            get => templates;
            private set => templates = value ?? new List<WildcardTemplate>();
        }

        /// <summary>
        /// Registers a new template
        /// </summary>
        public bool RegisterTemplate(WildcardTemplate template)
        {
            if (template == null || string.IsNullOrEmpty(template.Name))
                return false;
            
            // Check for duplicates
            if (templates.Any(t => t.Name == template.Name))
                return false;
            
            templates.Add(template);
            UpdateTagIndex(template);
            
            return true;
        }

        /// <summary>
        /// Unregisters a template
        /// </summary>
        public bool UnregisterTemplate(string templateName)
        {
            var template = templates.FirstOrDefault(t => t.Name == templateName);
            if (template == null || template.IsBuiltIn)
                return false;
            
            templates.Remove(template);
            RebuildTagIndex();
            
            return true;
        }

        /// <summary>
        /// Gets a template by name
        /// </summary>
        public WildcardTemplate GetTemplate(string name)
        {
            return templates.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// Gets templates by tag
        /// </summary>
        public List<WildcardTemplate> GetTemplatesByTag(string tag)
        {
            if (!tagIndex.ContainsKey(tag))
                return new List<WildcardTemplate>();
            
            return tagIndex[tag]
                .Select(name => GetTemplate(name))
                .Where(t => t != null)
                .ToList();
        }

        /// <summary>
        /// Gets all unique tags
        /// </summary>
        public List<string> GetAllTags()
        {
            return tagIndex.Keys.ToList();
        }

        /// <summary>
        /// Searches templates by pattern content
        /// </summary>
        public List<WildcardTemplate> SearchByPattern(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return templates;
            
            var lowerSearch = searchTerm.ToLower();
            return templates.Where(t => 
                t.Pattern.ToLower().Contains(lowerSearch) ||
                t.Name.ToLower().Contains(lowerSearch) ||
                t.Description.ToLower().Contains(lowerSearch)
            ).ToList();
        }

        /// <summary>
        /// Gets template categories based on tags
        /// </summary>
        public Dictionary<string, List<WildcardTemplate>> GetTemplatesByCategory()
        {
            var categories = new Dictionary<string, List<WildcardTemplate>>();
            
            // Built-in templates
            var builtIn = templates.Where(t => t.IsBuiltIn).ToList();
            if (builtIn.Any())
                categories["Built-in"] = builtIn;
            
            // Custom templates by tag
            foreach (var tag in tagIndex.Keys)
            {
                var tagged = GetTemplatesByTag(tag);
                if (tagged.Any())
                    categories[tag] = tagged;
            }
            
            // Untagged custom templates
            var untagged = templates.Where(t => !t.IsBuiltIn && (t.Tags == null || t.Tags.Count == 0)).ToList();
            if (untagged.Any())
                categories["Custom"] = untagged;
            
            return categories;
        }

        /// <summary>
        /// Validates a template pattern
        /// </summary>
        public ValidationResult ValidateTemplate(WildcardTemplate template)
        {
            var result = new ValidationResult();
            
            if (template == null)
            {
                result.AddError("Template cannot be null");
                return result;
            }
            
            if (string.IsNullOrWhiteSpace(template.Name))
            {
                result.AddError("Template name cannot be empty");
            }
            
            if (string.IsNullOrWhiteSpace(template.Pattern))
            {
                result.AddError("Template pattern cannot be empty");
            }
            else
            {
                // Check for at least one wildcard
                if (!template.Pattern.Contains("<") || !template.Pattern.Contains(">"))
                {
                    result.AddWarning("Template pattern should contain at least one wildcard");
                }
                
                // Check for invalid path characters
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                var patternWithoutWildcards = System.Text.RegularExpressions.Regex.Replace(template.Pattern, @"<[^>]+>", "");
                if (patternWithoutWildcards.Any(c => invalidChars.Contains(c)))
                {
                    result.AddError("Template pattern contains invalid filename characters");
                }
            }
            
            return result;
        }

        /// <summary>
        /// Creates a copy of a template
        /// </summary>
        public WildcardTemplate CloneTemplate(string templateName, string newName)
        {
            var original = GetTemplate(templateName);
            if (original == null)
                return null;
            
            var clone = new WildcardTemplate
            {
                Name = newName,
                Description = original.Description,
                Pattern = original.Pattern,
                IsBuiltIn = false,
                Tags = new List<string>(original.Tags ?? new List<string>())
            };
            
            return clone;
        }

        /// <summary>
        /// Imports templates from another registry
        /// </summary>
        public int ImportTemplates(TemplateRegistry other, bool overwriteExisting = false)
        {
            if (other == null)
                return 0;
            
            int imported = 0;
            
            foreach (var template in other.Templates)
            {
                if (template.IsBuiltIn)
                    continue;
                
                var existing = GetTemplate(template.Name);
                if (existing != null)
                {
                    if (overwriteExisting)
                    {
                        templates.Remove(existing);
                        templates.Add(template);
                        imported++;
                    }
                }
                else
                {
                    templates.Add(template);
                    imported++;
                }
            }
            
            RebuildTagIndex();
            return imported;
        }

        /// <summary>
        /// Updates the tag index for a template
        /// </summary>
        private void UpdateTagIndex(WildcardTemplate template)
        {
            if (template.Tags == null || template.Tags.Count == 0)
                return;
            
            foreach (var tag in template.Tags)
            {
                if (!tagIndex.ContainsKey(tag))
                    tagIndex[tag] = new List<string>();
                
                if (!tagIndex[tag].Contains(template.Name))
                    tagIndex[tag].Add(template.Name);
            }
        }

        /// <summary>
        /// Rebuilds the entire tag index
        /// </summary>
        private void RebuildTagIndex()
        {
            tagIndex.Clear();
            
            foreach (var template in templates)
            {
                UpdateTagIndex(template);
            }
        }

        /// <summary>
        /// Gets statistics about the registry
        /// </summary>
        public TemplateRegistryStats GetStatistics()
        {
            return new TemplateRegistryStats
            {
                TotalTemplates = templates.Count,
                BuiltInTemplates = templates.Count(t => t.IsBuiltIn),
                CustomTemplates = templates.Count(t => !t.IsBuiltIn),
                TotalTags = tagIndex.Keys.Count,
                AverageTagsPerTemplate = templates.Where(t => t.Tags != null).DefaultIfEmpty().Average(t => t?.Tags?.Count ?? 0)
            };
        }

        /// <summary>
        /// Creates default templates
        /// </summary>
        public static TemplateRegistry CreateWithDefaults()
        {
            var registry = new TemplateRegistry();
            
            // Add default templates
            registry.RegisterTemplate(new WildcardTemplate
            {
                Name = "Simple Output",
                Description = "Basic naming pattern",
                Pattern = "<Scene>_<Timeline>_<Take>",
                IsBuiltIn = true,
                Tags = new List<string> { "Basic", "Simple" }
            });
            
            registry.RegisterTemplate(new WildcardTemplate
            {
                Name = "Full Detail",
                Description = "Detailed naming with all information",
                Pattern = "<Product>_<Scene>_<Timeline>_<RecorderType>_<Resolution>_<Date>_<Time>_v<Take:000>",
                IsBuiltIn = true,
                Tags = new List<string> { "Detailed", "Production" }
            });
            
            registry.RegisterTemplate(new WildcardTemplate
            {
                Name = "Animation Export",
                Description = "Optimized for animation exports",
                Pattern = "<GameObject>_<Timeline>_Anim_<Frame>",
                IsBuiltIn = true,
                Tags = new List<string> { "Animation", "Export" }
            });
            
            registry.RegisterTemplate(new WildcardTemplate
            {
                Name = "AOV Pass",
                Description = "For AOV render passes",
                Pattern = "<Scene>_<AOVType>_<Resolution>_<Take>",
                IsBuiltIn = true,
                Tags = new List<string> { "Rendering", "AOV" }
            });
            
            return registry;
        }
    }

    /// <summary>
    /// Statistics about the template registry
    /// </summary>
    public struct TemplateRegistryStats
    {
        public int TotalTemplates;
        public int BuiltInTemplates;
        public int CustomTemplates;
        public int TotalTags;
        public double AverageTagsPerTemplate;
    }
}