# Batch Rendering Tool Implementation

The project includes a comprehensive batch rendering system with multiple recorder support:

## Core Renderers
- **SingleTimelineRendererV2**: Enhanced Timeline rendering with full recorder support
  - Dynamic Timeline generation with ControlTrack and RecorderTrack
  - Support for all recorder types (Image, Movie, FBX, Alembic, Animation, AOV)
  - Automatic cleanup and resource management
  - Robust Play Mode transition handling

## Supported Recorder Types
1. **Image Sequence Recorder**
   - PNG, JPG, EXR output formats
   - Customizable resolution and frame rate
   - Alpha channel support

2. **Movie Recorder**
   - H.264/H.265 video encoding
   - Audio recording support with configurable bitrate
   - Multiple quality presets

3. **FBX Recorder**
   - Animation and geometry export
   - Transform recording options
   - Harmony patches for compatibility

4. **Alembic Recorder**
   - Point cloud and mesh export
   - Uniform/Acyclic time sampling
   - Material property export

5. **Animation Recorder**
   - Animation clip generation
   - Selective object recording
   - Keyframe optimization

6. **AOV (Arbitrary Output Variables) Recorder**
   - Multi-pass rendering support
   - Custom shader variable capture
   - HDRP integration

## Key Features
- **RecorderConfig System**: Centralized configuration management
- **Wildcard Path Processing**: Dynamic file naming with wildcards
- **Multi-Recorder Support**: Configure multiple recorders per Timeline
- **Error Recovery**: Robust error handling and recovery mechanisms
- **Test Automation**: MCP integration for automated testing