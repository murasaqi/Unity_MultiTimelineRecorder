# Development Commands

## Unity Editor Operations
- Open project in Unity 6000.0.38f1
- Build: File → Build Settings → Build
- Play Mode: Ctrl+P (Windows/Linux) or Cmd+P (Mac)

## Batch Rendering Tool Access
- **Single Timeline Renderer**: Window → Batch Rendering Tool → Single Timeline Renderer V2
- **Recorder Config Editor**: Window → Batch Rendering Tool → Recorder Config Editor
- **Debug Tools**:
  - AOV Debug Tool: Window → Batch Rendering Tool → Debug → AOV Debug
  - Alembic Debug Tool: Window → Batch Rendering Tool → Debug → Alembic Debug
  - FBX Debug Tool: Window → Batch Rendering Tool → Debug → FBX Debug
  - Recorder Capability Checker: Window → Batch Rendering Tool → Debug → Capability Checker

## Test Automation
- **Run Tests**: Window → Batch Rendering Tool → Test Automation → Test Runner
- **MCP Commands**: Available through MCP integration for automated testing
- **Test Reports**: Generated in `Assets/BatchRenderingTool/Tests/Reports/`

## Common Development Tasks
- Add new recorder types in `Assets/BatchRenderingTool/Scripts/Editor/`
- Create custom editors in `RecorderEditors/` folder
- Write unit tests in `Tests/` folder with appropriate assembly references
- Use BatchRenderingToolLogger for consistent logging across the tool