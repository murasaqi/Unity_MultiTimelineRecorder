using System;

namespace BatchRenderingTool
{
    /// <summary>
    /// Alembic time sampling type
    /// </summary>
    public enum AlembicTimeSamplingType
    {
        Uniform,      // Regular frame intervals
        Acyclic      // Irregular time samples
    }
    
    /// <summary>
    /// Audio bit rate mode for movie recording
    /// </summary>
    public enum AudioBitRateMode
    {
        Low = 64,        // 64 kbps
        Medium = 128,    // 128 kbps
        High = 192,      // 192 kbps
        VeryHigh = 320   // 320 kbps
    }
    
}