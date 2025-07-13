# Unity Multi Timeline Recorder

A comprehensive Unity package for batch recording multiple Timeline assets with various output formats.

## Features

- **Multi-Timeline Batch Recording**: Record multiple Unity Timeline assets in a single batch operation
- **Multiple Output Formats**: 
  - Movie (MP4, MOV, WebM)
  - Image Sequences (PNG, JPG, EXR)
  - Animation Clips
  - Alembic
  - FBX
  - AOV (Arbitrary Output Variables)
- **Flexible Recording Settings**: Configure each recorder independently with specific settings
- **Path Management**: Advanced path management with wildcard support
- **Play Mode Support**: Record timelines in Play Mode with real-time monitoring
- **Progress Tracking**: Visual progress indicators for batch operations

## Installation

### Using Unity Package Manager

1. Open the Package Manager window (Window > Package Manager)
2. Click the + button in the top-left corner
3. Select "Add package from git URL..."
4. Enter: `https://github.com/murasaqi/Unity_MultiTimelineRecorder.git`
5. Click Add

### Manual Installation

1. Download the latest release from the [Releases](https://github.com/murasaqi/Unity_MultiTimelineRecorder/releases) page
2. Extract the package to your project's `Packages` folder
3. Unity will automatically import the package

## Requirements

- Unity 2021.3 or later
- Unity Timeline package (com.unity.timeline) 1.6.0+
- Unity Recorder package (com.unity.recorder) 4.0.0+
- Unity Editor Coroutines package (com.unity.editorcoroutines) 1.0.0+

## Quick Start

1. Open the Multi Timeline Recorder window: **Window > Multi Timeline Recorder**
2. Add Timeline assets to record by clicking the "+" button
3. Configure recording settings for each timeline
4. Click "Start Recording" to begin the batch process

## Usage

### Basic Recording

```csharp
// Example of using the Multi Timeline Recorder API
using Unity.MultiTimelineRecorder.Editor;

// Create a recorder configuration
var config = new MovieRecorderSettingsConfig();
config.Configure(outputPath: "Recordings/MyVideo.mp4", frameRate: 30);

// Add to Multi Timeline Recorder
MultiTimelineRecorder.AddRecording(timelineAsset, config);
```

### Advanced Configuration

The package supports advanced recording configurations including:

- Custom output paths with wildcards
- Frame rate and resolution settings
- Codec selection
- Quality presets
- Audio recording options
- Post-processing effects

## Documentation

For detailed documentation, tutorials, and API reference, visit our [Wiki](https://github.com/murasaqi/Unity_MultiTimelineRecorder/wiki).

## Support

- **Issues**: Report bugs and request features on our [Issue Tracker](https://github.com/murasaqi/Unity_MultiTimelineRecorder/issues)
- **Discussions**: Join our community on [Discussions](https://github.com/murasaqi/Unity_MultiTimelineRecorder/discussions)

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for more information.

## License

This package is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

This package builds upon Unity's Timeline and Recorder systems to provide enhanced batch recording capabilities.