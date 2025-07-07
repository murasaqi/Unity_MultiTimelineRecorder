namespace BatchRenderingTool
{
    /// <summary>
    /// FBX export preset options
    /// </summary>
    public enum FBXExportPreset
    {
        Custom,
        AnimationExport,
        ModelExport,
        ModelAndAnimation
    }
    
    /// <summary>
    /// FBX include options
    /// </summary>
    public enum FBXIncludeOptions
    {
        Model,
        Animation,
        ModelAndAnimation
    }
    
    /// <summary>
    /// FBX Animation Compression Level (matches Unity Recorder's CurveSimplificationOptions)
    /// </summary>
    public enum FBXAnimationCompressionLevel
    {
        Lossy,       // Reduces keyframes to save space, with 0.5% tolerance
        Lossless,    // Only removes keyframes from constant curves
        Disabled     // No compression, keeps all keyframes
    }
    
    /// <summary>
    /// FBX Recorded Component type
    /// </summary>
    public enum FBXRecordedComponent
    {
        Camera,
        Transform
    }
}