# Technology Stack

## Unity Package Structure
- **Unity Version**: 2021.3 or later
- **Package Type**: Unity Package Manager (UPM) package
- **License**: MIT License

## Dependencies
- `com.unity.recorder` (5.1.2+) - Unity's built-in recording system
- `com.unity.timeline` (1.6.0+) - Unity Timeline system
- `com.unity.editorcoroutines` (1.0.0+) - Editor coroutines for async operations

## Architecture
- **Runtime Assembly**: `Unity.MultiTimelineRecorder.Runtime` - Core runtime functionality
- **Editor Assembly**: `Unity.MultiTimelineRecorder.Editor` - Editor tools and UI
- **Namespace Convention**: `Unity.MultiTimelineRecorder.{Runtime|Editor}`

## Code Organization
- **Editor-Only Package**: All main functionality runs in Unity Editor
- **Assembly Definitions**: Separate runtime and editor assemblies with proper references
- **Modular Design**: Separate modules for different recorder types (Movie, Image, Animation, etc.)

## Build & Development
- **No Build Process**: Unity package, no external build system required
- **Testing**: Unity Test Framework (Tests/Editor folder structure)
- **Development**: Standard Unity package development workflow

## Common Commands
```bash
# Package installation via UPM
# Add via Unity Package Manager: Window > Package Manager > + > Add package from git URL
# URL: https://github.com/murasaqi/Unity_MultiTimelineRecorder.git?path=jp.iridescent.multitimelinerecorder

# Development workflow
# 1. Open Unity project
# 2. Modify package files in jp.iridescent.multitimelinerecorder/
# 3. Test in Unity Editor
# 4. Use demo project in Unity_MultiTimelineRecorder_Demo~/ for testing
```

## Platform Support
- **Editor Only**: Windows, macOS, Linux (Unity Editor platforms)
- **Output Formats**: Cross-platform video/image formats