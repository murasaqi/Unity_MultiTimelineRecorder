# Multi-Recorder Configuration

The project includes a sophisticated multi-recorder system that allows configuring multiple recorders for a single Timeline:

## MultiRecorderConfig System
- **Purpose**: Configure and manage multiple recorders per Timeline render
- **Features**:
  - Support for all recorder types simultaneously
  - Individual enable/disable for each recorder
  - Per-recorder configuration settings
  - Unified rendering execution

## Supported Configurations per Recorder
- **Common Settings**: Name, enabled state, file naming, take number
- **Type-Specific Settings**:
  - Image: Format (PNG/JPG/EXR), quality settings
  - Movie: Video/audio encoding, bitrate, presets
  - FBX: Animation export settings, transform options
  - Alembic: Export targets, time sampling
  - Animation: Clip settings, keyframe reduction
  - AOV: Pass selection, output format

## RecorderConfig Architecture
- **RecorderConfig**: Base configuration class
- **RecorderConfigEditor**: GUI for editing configurations
- **RecorderConfigEditorWindow**: Popup window for detailed settings
- **RecorderSettingsFactory**: Creates appropriate settings for each type

## Implementation Details
- Each recorder runs independently during Timeline playback
- Output paths are managed per recorder with wildcard support
- Frame-accurate synchronization across all recorders
- Error handling per recorder with detailed logging

## Future Considerations
While the current implementation focuses on multiple recorders per single Timeline, the architecture is designed to be extensible for batch processing multiple Timelines in future versions.