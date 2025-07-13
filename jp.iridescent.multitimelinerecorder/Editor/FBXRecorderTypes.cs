namespace Unity.MultiTimelineRecorder
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
    /// FBX Recorded Component type (Flags for multiple selection)
    /// </summary>
    [System.Flags]
    public enum FBXRecordedComponent
    {
        None = 0,
        Transform = 1 << 0,
        Camera = 1 << 1,
        Light = 1 << 2,
        MeshRenderer = 1 << 3,
        SkinnedMeshRenderer = 1 << 4,
        Animator = 1 << 5,
        All = Transform | Camera | Light | MeshRenderer | SkinnedMeshRenderer | Animator
    }
}