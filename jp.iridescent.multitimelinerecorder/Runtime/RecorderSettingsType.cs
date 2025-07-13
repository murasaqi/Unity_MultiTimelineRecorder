using System;

namespace Unity.MultiTimelineRecorder
{
    /// <summary>
    /// Supported recorder types for batch rendering
    /// </summary>
    public enum RecorderSettingsType
    {
        Image,
        Movie,
        Animation,
        Alembic,
        AOV,
        FBX
    }
    
    /// <summary>
    /// Output resolution presets
    /// </summary>
    public enum OutputResolution
    {
        HD720p,
        HD1080p,
        UHD4K,
        UHD8K,
        Custom
    }
}