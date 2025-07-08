# Unity Configuration

## Unity Version
- **Unity**: 6000.0.38f1
- **Render Pipeline**: HDRP 17.0.3

## Essential Packages
- **Unity Recorder**: Core recording functionality
- **Cinemachine**: Camera management for Timeline
- **Timeline**: Sequencing and playback control
- **Input System**: User input handling
- **Editor Coroutines**: Asynchronous operations in Editor

## Additional Dependencies
- **Alembic**: For Alembic file export support
- **FBX Exporter**: For FBX animation and geometry export
- **Harmony**: Used for runtime patching (FBX compatibility fixes)

## Assembly Definitions
- **BatchRenderingTool**: Runtime assembly
- **BatchRenderingTool.Editor**: Editor-only functionality
- **BatchRenderingTool.Runtime.Tests**: Runtime tests
- **Tests**: Editor test assembly

## Build Settings
- Platform: Standalone (Windows/Mac/Linux)
- Graphics API: DX12/Vulkan/Metal (HDRP compatible)
- Color Space: Linear (required for HDRP)