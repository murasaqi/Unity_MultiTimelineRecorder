# Multi Timeline Recorder Programmatic API

The Multi Timeline Recorder API provides UI-independent control over the recording functionality, enabling automation, batch processing, and integration with external tools.

## Quick Start

```csharp
using MultiTimelineRecorder.API;
using UnityEngine.Playables;

// Simple recording
var timelines = MultiTimelineRecorderAPI.ScanTimelines();
var result = MultiTimelineRecorderAPI.ExecuteRecording(
    timelines,
    "Assets/Recordings",
    RecorderType.Movie,
    frameRate: 30
);
```

## Configuration Builder

The fluent API makes it easy to create complex recording configurations:

```csharp
var config = new RecordingConfigurationBuilder()
    .WithFrameRate(30)
    .WithResolution(1920, 1080)
    .WithOutputPath("Assets/Recordings")
    .AddTimeline(timeline1)
        .WithMovieRecorder("Main Camera")
            .SetOutputFormat(VideoFormat.MP4)
            .SetQuality(BitrateMode.High)
            .SetFileName("<Scene>_<Timeline>_<Take>")
        .WithImageRecorder("Screenshots")
            .SetOutputFormat(ImageFormat.PNG)
            .SetCaptureAlpha(true)
    .AddTimeline(timeline2)
        .WithAnimationRecorder()
            .SetTargetGameObject(targetObject)
            .SetRecordingScope(includeChildren: true)
    .Build();

var result = MultiTimelineRecorderAPI.ExecuteRecording(config);
```

## Main API Methods

### Recording Execution

```csharp
// Execute recording synchronously
RecordingResult ExecuteRecording(RecordingConfiguration config)

// Execute recording asynchronously
Task<RecordingResult> ExecuteRecordingAsync(RecordingConfiguration config)

// Cancel active recording
void CancelRecording(string jobId)

// Get recording progress
RecordingProgress GetProgress(string jobId)

// Check if recording is active
bool IsRecording { get; }
```

### Configuration Management

```csharp
// Create new configuration
RecordingConfiguration CreateConfiguration()

// Save/Load configurations
void SaveConfiguration(RecordingConfiguration config, string path = null)
RecordingConfiguration LoadConfiguration(string path = null)

// Import/Export JSON
string ExportConfiguration(RecordingConfiguration config)
RecordingConfiguration ImportConfiguration(string json)
```

### Timeline Management

```csharp
// Scan for timelines
List<PlayableDirector> ScanTimelines(bool includeInactive = false)
List<PlayableDirector> ScanSceneTimelines(string scenePath = null, bool includeInactive = false)

// Add/Remove timelines
TimelineRecorderConfig AddTimeline(RecordingConfiguration config, PlayableDirector director)
void RemoveTimeline(RecordingConfiguration config, PlayableDirector director)
```

### Recorder Management

```csharp
// Add recorders
IRecorderConfiguration AddRecorder(TimelineRecorderConfig timeline, RecorderType type)

// Remove recorders
void RemoveRecorder(TimelineRecorderConfig timeline, string recorderId)
void RemoveRecorder(TimelineRecorderConfig timeline, IRecorderConfiguration recorder)
```

## Advanced Examples

### Batch Recording with Different Settings

```csharp
var directors = MultiTimelineRecorderAPI.ScanTimelines();

foreach (var director in directors)
{
    var config = new RecordingConfigurationBuilder()
        .WithName($"Batch_{director.name}")
        .WithFrameRate(60)
        .WithResolution(3840, 2160)
        .WithOutputPath($"Assets/Recordings/{director.name}")
        .AddTimeline(director)
            .WithMovieRecorder()
                .SetOutputFormat(VideoFormat.MP4)
                .SetQuality(BitrateMode.High)
        .Build();
    
    var result = await MultiTimelineRecorderAPI.ExecuteRecordingAsync(config);
    Debug.Log($"Recording {director.name}: {result.JobId}");
}
```

### Using SignalEmitter Timing

```csharp
var config = new RecordingConfigurationBuilder()
    .WithSignalEmitterTiming(true, "[MTR] Start", "[MTR] End")
    .AddTimeline(timelineWithSignals)
        .WithMovieRecorder()
    .Build();

MultiTimelineRecorderAPI.ExecuteRecording(config);
```

### Custom Recorder Configuration

```csharp
var config = new RecordingConfigurationBuilder()
    .AddTimeline(director)
        .WithRecorder<CustomRecorderConfiguration>(recorder =>
        {
            recorder.Name = "Custom Recorder";
            recorder.CustomProperty = "Value";
            recorder.FrameRate = 24;
        })
    .Build();
```

### Monitoring Recording Progress

```csharp
var result = MultiTimelineRecorderAPI.ExecuteRecording(config);

// Monitor progress
EditorApplication.update += () =>
{
    if (MultiTimelineRecorderAPI.IsRecording)
    {
        var progress = MultiTimelineRecorderAPI.GetProgress(result.JobId);
        EditorUtility.DisplayProgressBar(
            "Recording", 
            $"Frame {progress.CurrentFrame}/{progress.TotalFrames}", 
            progress.Progress
        );
    }
    else
    {
        EditorUtility.ClearProgressBar();
    }
};
```

## Command Line Integration

The API can be used for command line automation:

```csharp
public static class BatchRecorder
{
    [MenuItem("Tools/Batch Record All Timelines")]
    public static void BatchRecordAll()
    {
        var timelines = MultiTimelineRecorderAPI.ScanTimelines();
        var config = MultiTimelineRecorderAPI.CreateConfiguration();
        
        foreach (var timeline in timelines)
        {
            MultiTimelineRecorderAPI.AddTimeline(config, timeline);
        }
        
        MultiTimelineRecorderAPI.ExecuteRecording(config);
    }
}
```

## Error Handling

```csharp
try
{
    var config = new RecordingConfigurationBuilder()
        .WithFrameRate(30)
        .Build();
        
    var validationResult = MultiTimelineRecorderAPI.ValidateConfiguration(config);
    if (!validationResult.IsValid)
    {
        Debug.LogError($"Configuration errors: {string.Join(", ", validationResult.Errors)}");
        return;
    }
    
    var result = MultiTimelineRecorderAPI.ExecuteRecording(config);
}
catch (Exception ex)
{
    Debug.LogError($"Recording failed: {ex.Message}");
}
```

## Best Practices

1. **Always validate configurations** before executing recordings
2. **Use the builder pattern** for complex configurations
3. **Handle errors gracefully** with try-catch blocks
4. **Monitor progress** for long recordings
5. **Clean up resources** by calling `Reset()` when done
6. **Save configurations** for reuse in production workflows

## Integration with CI/CD

The API is designed to work with continuous integration systems:

```bash
Unity -batchmode -quit -projectPath . -executeMethod MyRecorder.RecordAll
```

```csharp
public class MyRecorder
{
    public static void RecordAll()
    {
        var config = MultiTimelineRecorderAPI.LoadConfiguration("Assets/CI/RecordingConfig.asset");
        var result = MultiTimelineRecorderAPI.ExecuteRecording(config);
        
        // Exit with appropriate code
        EditorApplication.Exit(result.IsSuccess ? 0 : 1);
    }
}
```