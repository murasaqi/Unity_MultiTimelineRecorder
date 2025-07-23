using System.Collections.Generic;

namespace MultiTimelineRecorder.Core.Interfaces
{
    /// <summary>
    /// Interface for wildcard processing service
    /// </summary>
    public interface IWildcardProcessor
    {
        /// <summary>
        /// Process wildcards in the input string
        /// </summary>
        /// <param name="input">Input string containing wildcards</param>
        /// <param name="context">Context for wildcard replacement</param>
        /// <returns>Processed string with wildcards replaced</returns>
        string ProcessWildcards(string input, WildcardContext context);
        
        /// <summary>
        /// Validate wildcards in the input string
        /// </summary>
        /// <param name="input">Input string to validate</param>
        /// <param name="invalidWildcards">List of invalid wildcards found</param>
        /// <returns>True if all wildcards are valid</returns>
        bool ValidateWildcards(string input, out List<string> invalidWildcards);
        
        /// <summary>
        /// Get the path with Unity Recorder wildcards preserved
        /// Converts Multi Timeline Recorder wildcards to Unity Recorder format
        /// </summary>
        /// <param name="processedPath">Path with processed wildcards</param>
        /// <param name="context">Context for wildcard replacement</param>
        /// <returns>Path suitable for Unity Recorder</returns>
        string GetUnityRecorderPath(string processedPath, WildcardContext context);
        
        /// <summary>
        /// Register a custom wildcard
        /// </summary>
        /// <param name="wildcard">Wildcard name (without angle brackets)</param>
        /// <param name="value">Value to replace the wildcard with</param>
        void RegisterCustomWildcard(string wildcard, string value);
    }
}