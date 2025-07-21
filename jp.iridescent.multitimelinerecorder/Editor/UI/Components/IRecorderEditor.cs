namespace MultiTimelineRecorder.UI.Components
{
    /// <summary>
    /// Interface for recorder-specific editor components
    /// </summary>
    public interface IRecorderEditor
    {
        /// <summary>
        /// Draws the editor UI for the recorder configuration
        /// </summary>
        void Draw();
        
        /// <summary>
        /// Validates the current configuration
        /// </summary>
        bool Validate(out string errorMessage);
        
        /// <summary>
        /// Resets the configuration to default values
        /// </summary>
        void ResetToDefaults();
    }
}