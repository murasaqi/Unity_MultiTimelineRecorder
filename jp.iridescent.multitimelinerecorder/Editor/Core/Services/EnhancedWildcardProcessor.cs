using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using MultiTimelineRecorder.Core.Interfaces;
using MultiTimelineRecorder.Core.Models;

namespace MultiTimelineRecorder.Core.Services
{
    /// <summary>
    /// Enhanced wildcard processor that handles both Unity Recorder and Multi Timeline Recorder wildcards
    /// </summary>
    public class EnhancedWildcardProcessor : IWildcardProcessor
    {
        private readonly ILogger _logger;
        private readonly WildcardRegistry _wildcardRegistry;
        private readonly Dictionary<string, Func<WildcardContext, string>> _processorMap;
        private static readonly Regex WildcardPattern = new Regex(@"<([^>]+)>", RegexOptions.Compiled);

        public EnhancedWildcardProcessor(ILogger logger, WildcardRegistry wildcardRegistry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wildcardRegistry = wildcardRegistry ?? throw new ArgumentNullException(nameof(wildcardRegistry));
            
            // Initialize processor map for Multi Timeline Recorder wildcards
            _processorMap = new Dictionary<string, Func<WildcardContext, string>>
            {
                { "<Timeline>", ctx => SanitizeFileName(ctx.TimelineName ?? "Timeline") },
                { "<TimelineTake>", ctx => (ctx.TimelineTakeNumber ?? 1).ToString("000") },
                { "<RecorderTake>", ctx => ctx.TakeNumber.ToString("0000") },
                { "<RecorderName>", ctx => SanitizeFileName(ctx.RecorderName ?? "Recorder") }
            };
        }

        /// <inheritdoc />
        public string ProcessWildcards(string input, WildcardContext context)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            _logger.LogVerbose($"Processing wildcards in: {input}", LogCategory.Wildcard);

            // Process wildcards in multiple passes to handle nested wildcards
            string result = input;
            int maxPasses = 3;
            
            for (int pass = 0; pass < maxPasses; pass++)
            {
                string previousResult = result;
                result = ProcessWildcardsPass(result, context);
                
                // If no changes were made, we're done
                if (result == previousResult)
                    break;
            }

            _logger.LogVerbose($"Wildcard processing result: {result}", LogCategory.Wildcard);
            return result;
        }

        /// <inheritdoc />
        public bool ValidateWildcards(string input, out List<string> invalidWildcards)
        {
            invalidWildcards = new List<string>();
            
            if (string.IsNullOrEmpty(input))
                return true;

            var matches = WildcardPattern.Matches(input);
            var allWildcards = _wildcardRegistry.GetAllWildcards();

            foreach (Match match in matches)
            {
                string wildcard = match.Value;
                
                // Check if wildcard exists in any registry
                if (!allWildcards.ContainsKey(wildcard) && !_processorMap.ContainsKey(wildcard))
                {
                    // Check if it's a parameterized wildcard (e.g., <Take:0000>)
                    if (!IsParameterizedWildcard(wildcard))
                    {
                        invalidWildcards.Add(wildcard);
                    }
                }
            }

            return invalidWildcards.Count == 0;
        }

        /// <inheritdoc />
        public string GetUnityRecorderPath(string processedPath, WildcardContext context)
        {
            // Replace Multi Timeline Recorder wildcards with Unity Recorder wildcards or their values
            string result = processedPath;
            
            // Map Multi Timeline Recorder wildcards to Unity Recorder equivalents
            result = result.Replace("<Timeline>", SanitizeFileName(context.TimelineName ?? "Timeline"));
            result = result.Replace("<TimelineTake>", (context.TimelineTakeNumber ?? 1).ToString("000"));
            result = result.Replace("<RecorderTake>", "<Take>"); // Map to Unity Recorder's take wildcard
            result = result.Replace("<RecorderName>", "<Recorder>"); // Map to Unity Recorder's recorder wildcard
            
            _logger.LogVerbose($"Unity Recorder path: {result}", LogCategory.Wildcard);
            return result;
        }

        /// <inheritdoc />
        public void RegisterCustomWildcard(string wildcard, string value)
        {
            _wildcardRegistry.AddCustomWildcard(wildcard, wildcard, value);
            _logger.LogInfo($"Registered custom wildcard: {wildcard} = {value}", LogCategory.Wildcard);
        }

        /// <summary>
        /// Process a single pass of wildcard replacement
        /// </summary>
        private string ProcessWildcardsPass(string input, WildcardContext context)
        {
            return WildcardPattern.Replace(input, match =>
            {
                string wildcard = match.Value;
                string wildcardContent = match.Groups[1].Value;
                
                // Check if it's a Unity Recorder wildcard (pass-through)
                if (_wildcardRegistry.IsUnityRecorderWildcard(wildcard))
                {
                    _logger.LogVerbose($"Unity Recorder wildcard (pass-through): {wildcard}", LogCategory.Wildcard);
                    return wildcard; // Pass through unchanged
                }
                
                // Check if it's a Multi Timeline Recorder wildcard
                if (_processorMap.ContainsKey(wildcard))
                {
                    string value = _processorMap[wildcard](context);
                    _logger.LogVerbose($"Multi Timeline Recorder wildcard: {wildcard} = {value}", LogCategory.Wildcard);
                    return value;
                }
                
                // Check if it's a custom wildcard
                if (_wildcardRegistry.IsCustomWildcard(wildcard))
                {
                    var customDef = _wildcardRegistry.CustomWildcards[wildcard];
                    _logger.LogVerbose($"Custom wildcard: {wildcard} = {customDef.CustomValue}", LogCategory.Wildcard);
                    return customDef.CustomValue;
                }
                
                // Check if it's a parameterized wildcard
                if (IsParameterizedWildcard(wildcard))
                {
                    return ProcessParameterizedWildcard(wildcardContent, context);
                }
                
                // Check custom wildcards in context
                if (context.CustomWildcards != null && context.CustomWildcards.ContainsKey(wildcardContent))
                {
                    string value = context.CustomWildcards[wildcardContent];
                    _logger.LogVerbose($"Context custom wildcard: {wildcard} = {value}", LogCategory.Wildcard);
                    return value;
                }
                
                // Unknown wildcard - return as is
                _logger.LogWarning($"Unknown wildcard: {wildcard}", LogCategory.Wildcard);
                return wildcard;
            });
        }

        /// <summary>
        /// Check if a wildcard is parameterized (e.g., <Take:0000>)
        /// </summary>
        private bool IsParameterizedWildcard(string wildcard)
        {
            return wildcard.Contains(":");
        }

        /// <summary>
        /// Process parameterized wildcards (e.g., <Take:0000>)
        /// </summary>
        private string ProcessParameterizedWildcard(string wildcardContent, WildcardContext context)
        {
            var parts = wildcardContent.Split(':');
            if (parts.Length != 2)
                return $"<{wildcardContent}>";
                
            string wildcardName = parts[0];
            string format = parts[1];
            
            // Handle specific parameterized wildcards
            switch (wildcardName)
            {
                case "Take":
                    return FormatNumber(context.TakeNumber, format);
                    
                case "TimelineTake":
                    return FormatNumber(context.TimelineTakeNumber ?? 1, format);
                    
                case "Frame":
                    // Frame is handled by Unity Recorder, so pass through
                    return $"<{wildcardContent}>";
                    
                default:
                    _logger.LogWarning($"Unknown parameterized wildcard: {wildcardName}", LogCategory.Wildcard);
                    return $"<{wildcardContent}>";
            }
        }

        /// <summary>
        /// Format a number based on the format string (e.g., "0000" for 4-digit padding)
        /// </summary>
        private string FormatNumber(int number, string format)
        {
            if (int.TryParse(format, out int digits))
            {
                return number.ToString($"D{digits}");
            }
            return number.ToString(format);
        }

        /// <summary>
        /// Sanitize a filename by removing invalid characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "Unnamed";
                
            // Remove invalid filename characters
            string invalid = new string(System.IO.Path.GetInvalidFileNameChars());
            string sanitized = fileName;
            
            foreach (char c in invalid)
            {
                sanitized = sanitized.Replace(c.ToString(), "_");
            }
            
            // Trim whitespace and dots
            sanitized = sanitized.Trim(' ', '.');
            
            // Ensure non-empty
            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "Unnamed";
                
            return sanitized;
        }
    }
}