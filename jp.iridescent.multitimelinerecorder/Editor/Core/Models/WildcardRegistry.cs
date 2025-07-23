using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiTimelineRecorder.Core.Models
{
    /// <summary>
    /// Processing type for wildcards
    /// </summary>
    public enum WildcardProcessingType
    {
        UnityRecorder,           // Processed by Unity Recorder (pass-through required)
        MultiTimelineRecorder,   // Processed by Multi Timeline Recorder
        Custom                   // User-defined custom wildcard
    }

    /// <summary>
    /// Wildcard definition
    /// </summary>
    [Serializable]
    public class WildcardDefinition
    {
        public string Wildcard { get; set; }                    // "<Scene>"
        public string DisplayName { get; set; }                 // "Scene Name"
        public string Category { get; set; }                    // "Basic"
        public string Description { get; set; }                 // "Current scene name"
        public bool IsBuiltIn { get; set; }                     // Whether it's a standard wildcard
        public WildcardProcessingType ProcessingType { get; set; } // Processing type
        public string CustomValue { get; set; }                 // Fixed value for custom wildcards

        public WildcardDefinition() { }

        public WildcardDefinition(string wildcard, string displayName, string category, string description, bool isBuiltIn, WildcardProcessingType processingType, string customValue = null)
        {
            Wildcard = wildcard;
            DisplayName = displayName;
            Category = category;
            Description = description;
            IsBuiltIn = isBuiltIn;
            ProcessingType = processingType;
            CustomValue = customValue;
        }
    }

    /// <summary>
    /// Integrated wildcard management
    /// </summary>
    [Serializable]
    public class WildcardRegistry
    {
        // Unity Recorder wildcards (pass-through required)
        // These strings are passed directly to Unity Recorder Clip
        public Dictionary<string, WildcardDefinition> UnityRecorderWildcards { get; set; } = new Dictionary<string, WildcardDefinition>
        {
            { "<Take>", new WildcardDefinition("<Take>", "Take Number", "Unity Recorder", "Unity Recorder standard take number - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Frame>", new WildcardDefinition("<Frame>", "Frame Number", "Unity Recorder", "Frame number (4-digit zero-padded) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Product>", new WildcardDefinition("<Product>", "Product Name", "Unity Recorder", "Application product name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Resolution>", new WildcardDefinition("<Resolution>", "Resolution", "Unity Recorder", "Output resolution (WxH) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Date>", new WildcardDefinition("<Date>", "Date", "Unity Recorder", "Current date (YYYYMMDD) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Time>", new WildcardDefinition("<Time>", "Time", "Unity Recorder", "Current time (HHMMSS) - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Scene>", new WildcardDefinition("<Scene>", "Scene Name", "Unity Recorder", "Current scene name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<Recorder>", new WildcardDefinition("<Recorder>", "Recorder Name", "Unity Recorder", "Recorder configuration name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<AOVType>", new WildcardDefinition("<AOVType>", "AOV Type", "Unity Recorder", "AOV pass type name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) },
            { "<GameObject>", new WildcardDefinition("<GameObject>", "GameObject", "Unity Recorder", "Target GameObject name - passed through to Unity Recorder Clip", true, WildcardProcessingType.UnityRecorder) }
        };

        // Multi Timeline Recorder added wildcards (unique features not in Unity Recorder)
        public Dictionary<string, WildcardDefinition> MultiTimelineRecorderWildcards { get; set; } = new Dictionary<string, WildcardDefinition>
        {
            { "<Timeline>", new WildcardDefinition("<Timeline>", "Timeline Name", "Multi Timeline Recorder", "Timeline asset name - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) },
            { "<TimelineTake>", new WildcardDefinition("<TimelineTake>", "Timeline Take", "Multi Timeline Recorder", "Timeline-specific take number (3-digit) - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) },
            { "<RecorderTake>", new WildcardDefinition("<RecorderTake>", "Recorder Take", "Multi Timeline Recorder", "Recorder-specific take number - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) },
            { "<RecorderName>", new WildcardDefinition("<RecorderName>", "Recorder Display Name", "Multi Timeline Recorder", "Recorder display name - Multi Timeline Recorder extension", true, WildcardProcessingType.MultiTimelineRecorder) }
        };

        // User-defined custom wildcards
        public Dictionary<string, WildcardDefinition> CustomWildcards { get; set; } = new Dictionary<string, WildcardDefinition>();

        /// <summary>
        /// Gets all wildcards
        /// </summary>
        public Dictionary<string, WildcardDefinition> GetAllWildcards()
        {
            var all = new Dictionary<string, WildcardDefinition>();
            
            // Add Unity Recorder wildcards
            foreach (var wildcard in UnityRecorderWildcards)
            {
                all[wildcard.Key] = wildcard.Value;
            }
            
            // Add Multi Timeline Recorder wildcards
            foreach (var wildcard in MultiTimelineRecorderWildcards)
            {
                all[wildcard.Key] = wildcard.Value;
            }
            
            // Add custom wildcards
            foreach (var custom in CustomWildcards)
            {
                all[custom.Key] = custom.Value;
            }
            
            return all;
        }

        /// <summary>
        /// Gets wildcards by category
        /// </summary>
        public Dictionary<string, List<WildcardDefinition>> GetWildcardsByCategory()
        {
            var categorized = new Dictionary<string, List<WildcardDefinition>>();

            foreach (var wildcard in GetAllWildcards().Values)
            {
                if (!categorized.ContainsKey(wildcard.Category))
                {
                    categorized[wildcard.Category] = new List<WildcardDefinition>();
                }
                categorized[wildcard.Category].Add(wildcard);
            }

            return categorized;
        }

        /// <summary>
        /// Adds a custom wildcard
        /// </summary>
        public void AddCustomWildcard(string wildcard, string displayName, string customValue, string description = "")
        {
            if (string.IsNullOrEmpty(wildcard) || string.IsNullOrEmpty(customValue))
                return;

            // Ensure wildcard format
            if (!wildcard.StartsWith("<"))
                wildcard = $"<{wildcard}";
            if (!wildcard.EndsWith(">"))
                wildcard = wildcard.TrimEnd('>') + ">";

            var definition = new WildcardDefinition(
                wildcard,
                displayName ?? wildcard,
                "Custom",
                description ?? $"Custom wildcard: {customValue}",
                false,
                WildcardProcessingType.Custom,
                customValue
            );

            CustomWildcards[wildcard] = definition;
        }

        /// <summary>
        /// Removes a custom wildcard
        /// </summary>
        public bool RemoveCustomWildcard(string wildcard)
        {
            return CustomWildcards.Remove(wildcard);
        }

        /// <summary>
        /// Checks if a wildcard is a Unity Recorder wildcard
        /// </summary>
        public bool IsUnityRecorderWildcard(string wildcard)
        {
            return UnityRecorderWildcards.ContainsKey(wildcard);
        }

        /// <summary>
        /// Checks if a wildcard is a Multi Timeline Recorder wildcard
        /// </summary>
        public bool IsMultiTimelineRecorderWildcard(string wildcard)
        {
            return MultiTimelineRecorderWildcards.ContainsKey(wildcard);
        }

        /// <summary>
        /// Checks if a wildcard is a custom wildcard
        /// </summary>
        public bool IsCustomWildcard(string wildcard)
        {
            return CustomWildcards.ContainsKey(wildcard);
        }
    }
}