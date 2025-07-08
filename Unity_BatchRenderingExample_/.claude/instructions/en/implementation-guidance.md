# Implementation Guidance

## Adding New Recorder Types
1. Create a new RecorderSettingsConfig class inheriting from base classes
2. Implement the corresponding RecorderEditor in `RecorderEditors/`
3. Add the new type to RecorderSettingsType enum
4. Update RecorderSettingsFactory to handle the new type
5. Create unit tests in the Tests directory

## Working with the Recording System
- Use `SingleTimelineRendererV2` for single Timeline rendering
- Implement `IRecorderSettingsHost` interface for custom recorder containers
- Utilize `RecorderClipUtility` for RecorderClip creation
- Use `WildcardProcessor` for dynamic path generation with wildcards

## Key APIs and Patterns
- **Unity Recorder Timeline Integration**: Use RecorderTrack and RecorderClip
- **Async Operations**: Use EditorCoroutines for long-running operations
- **Path Management**: Use PathUtility for cross-platform path handling
- **Logging**: Use BatchRenderingToolLogger for consistent logging

## Testing Guidelines
1. Write unit tests for all new recorder configurations
2. Use TestHelpers for common test operations
3. Run MCP test automation for integration testing
4. Export test results using TestResultExporter

## Debug Tools
- Use the Debug folder tools for validation and troubleshooting
- AOVIntegrationValidator for AOV recorder issues
- AlembicIntegrationValidator for Alembic export validation
- FBXRecorderAnalyzer for FBX recording analysis