# Unity Multi Timeline Recorder

**English** | [日本語](README.ja.md)

---

## Overview

Unity Multi Timeline Recorder is a comprehensive tool for batch recording multiple Unity Timeline assets with various output formats. This package extends Unity's built-in Recorder functionality to support simultaneous recording of multiple timelines with different configurations.

## Features

- **Multi-Timeline Batch Recording**: Record multiple Unity Timeline assets in a single batch operation
- **Multiple Output Formats**: 
  - Movie (MP4, MOV, WebM)
  - Image Sequences (PNG, JPG, EXR)
  - Animation Clips
  - Alembic
  - FBX
  - AOV (Arbitrary Output Variables)
- **Signal Emitter Timing Control**: Use Timeline Signal Emitters to precisely control recording start and end times
- **Flexible Recording Settings**: Configure each recorder independently with specific settings
- **Path Management**: Advanced path management with wildcard support
- **Play Mode Support**: Record timelines in Play Mode with real-time monitoring
- **Progress Tracking**: Visual progress indicators for batch operations

## Repository Structure

```
Unity_MultiTimelineRecorder/
└── jp.iridescent.multitimelinerecorder/    # Unity Package
    ├── Runtime/                             # Runtime scripts
    ├── Editor/                              # Editor scripts
    ├── Documentation~/                      # Documentation
    ├── Samples~/                            # Sample assets
    ├── package.json                         # Package manifest
    ├── README.md                            # Package documentation
    ├── LICENSE                              # MIT License
    └── CHANGELOG.md                         # Version history
```

## Installation

### Package Installation via Unity Package Manager

1. Open the Package Manager window (Window > Package Manager)
2. Click the + button in the top-left corner
3. Select "Add package from git URL..."
4. Enter: `https://github.com/murasaqi/Unity_MultiTimelineRecorder.git?path=jp.iridescent.multitimelinerecorder`
5. Click Add


## Requirements

- Unity 2021.3 or later
- Unity Timeline package (com.unity.timeline) 1.6.0+
- Unity Recorder package (com.unity.recorder) 4.0.0+
- Unity Editor Coroutines package (com.unity.editorcoroutines) 1.0.0+

## Quick Start

1. Open the Multi Timeline Recorder window: **Window > Multi Timeline Recorder**
2. Add Timeline assets to record by clicking the "+" button
3. Configure recording settings for each timeline
4. **Optional**: Enable Signal Emitter timing control for precise recording ranges:
   - Check "Enable" in the Signal Emitter Timing section
   - Set start and end timing names (e.g., "pre" and "post")
   - Add Signal Emitters to your Timeline tracks with matching names
   - Use the Frame/Seconds display toggle to view timing in your preferred format
5. Click "Start Recording" to begin the batch process

## License

This project is licensed under the MIT License - see the [LICENSE](jp.iridescent.multitimelinerecorder/LICENSE) file for details.

## Author

**Murasaqi**
- GitHub: [@murasaqi](https://github.com/murasaqi)

---

## Support

- **Issues**: [GitHub Issues](https://github.com/murasaqi/Unity_MultiTimelineRecorder/issues)
- **Discussions**: [GitHub Discussions](https://github.com/murasaqi/Unity_MultiTimelineRecorder/discussions)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.