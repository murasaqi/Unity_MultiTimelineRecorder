# Unity Test Runner Results - 2025-07-06

## Summary
✅ All tests passed successfully

## Edit Mode Tests
- **Total**: 64
- **Passed**: 64 ✅
- **Failed**: 0
- **Skipped**: 0
- **Inconclusive**: 0

## Play Mode Tests  
- **Total**: 4
- **Passed**: 4 ✅
- **Failed**: 0
- **Skipped**: 0
- **Inconclusive**: 0

## Fixed Issues
### Compiler Warnings Fixed:
1. Removed unused field `SingleTimelineRenderer.isRenderingInProgress`
2. Removed unused field `SingleTimelineRenderer.outputFile`
3. Updated deprecated `FindObjectsOfType` to `FindObjectsByType` (2 occurrences)
4. Changed FBX warning log level from Warning to Verbose

### Remaining Warnings:
- MovieRecorderSettings.VideoRecorderOutputFormat deprecation warnings - These are in the Unity Recorder API and will need to be addressed when updating to newer API

## Test Categories Covered:
- AOV Recorder Settings Configuration
- Alembic Recorder Settings Configuration
- Animation Recorder Settings Configuration  
- FBX Recorder Settings Configuration
- Image Recorder Settings Configuration
- Movie Recorder Settings Configuration
- Recorder Settings Factory
- Recorder Settings Helper
- Batch Rendering Tool Logger
- Recorder Clip Utility
- Preset Manager
- Config Migration

All systems tested and functioning correctly.