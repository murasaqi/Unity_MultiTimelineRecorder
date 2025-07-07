# Architecture Notes

The project uses advanced Unity patterns and custom implementations:

## Core Architecture
- **RecorderSettingsConfig System**: Modular architecture for different recorder types
  - ImageRecorderSettingsConfig
  - MovieRecorderSettingsConfig  
  - FBXRecorderSettingsConfig
  - AlembicRecorderSettingsConfig
  - AnimationRecorderSettingsConfig
  - AOVRecorderSettingsConfig

## Key Components
- **SingleTimelineRendererV2**: Main rendering engine with RecorderTrack integration
- **RecorderClipUtility**: Manages RecorderClip creation and configuration
- **PathUtility & WildcardProcessor**: Advanced path handling with wildcard support
- **RecorderSettingsFactory**: Factory pattern for creating appropriate recorder settings
- **BatchRenderingToolLogger**: Comprehensive logging system for debugging

## Integration Features
- Unity Recorder API integration through Timeline
- MCP (Model Control Protocol) test automation support
- Editor Coroutines for asynchronous operations
- Harmony patches for FBX recorder compatibility

## Testing Infrastructure
- Comprehensive unit test suite
- Test automation tools with MCP integration
- Performance testing capabilities
- XML test result export functionality