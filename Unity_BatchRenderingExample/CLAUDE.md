# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Unity開発自動化指示書

これは、Claude Codeを用いてUnityの開発を自動化するための命令である。
Linearでのタスク管理、デバッグ、問題があればタスクに追加、適切なGitへのコミットを自動化し、自主的に改善をつづけることを目的としています。

## 基本方針

### DO MUST
- 対話は日本語で行ってください
- Unity Natural MCPを積極的に活用し、Unity Editorの操作を自動化してください
  - Consoleの確認
  - テストの実行とログの出力
- なるべく疎結合な開発を心がけてください
- Unity Test Runnerでの自動テストを前提とした設計をしてください
- 単体テスト可能なメソッド構造を心がけてください
- 細かくDebug.Logを実装し、デバッグしやすくなるような開発を心がけてください
- Unity Batch Rendering Tool DevのProjectがLinearに存在しているので、そちらでタスクを管理してください。
- すべてのToDoは、Linearに追加してから作業を開始してください。
- LinearでBacklogやToDoやIn Progressになっているタスクを優先度が高い順に実装していってください。
- タスクの説明やコメント欄に詳細が書いてある事があるので必ず確認してください。
- 作業中はステータスをIn Progressにし、作業が完了したものはDoneにし、GitにLinearのIssueIDを含めてコミットしてください。
- 実装の際に発生した問題や特記事項などは、そのタスク内のコメント欄に残しておいてください。
- 定期的にConsoleのログを確認し、エラーが出ていたら確実に対処してください。
- すべてのサブタスクが完了したら親のタスクも完了にしてください。
- 途中で修正タスクが発生した場合は、Linearにタスクを追加し、必要に応じてサブタスクや説明欄、コメントなども追加してください。
- すべてのタスクが終わったらMCPでTest Runnerを実行し、そのあとログを出力してください。
- Test Runnerのログを出力したら、そのログを確認してください。問題があれば修正してください。
- タスクが終わったら、追加のタスクがないか確認し、ある場合は同様に進めてください。
- Unity Editorでのスクリプトのコンパイルや、Error確認はMCPを介して自動で行ってください。
- Assets/BatchRenderingTool/Tests/logにTest Runnerのログを保存しています。最新のログが見たい場合はこちらのディレクトリの最新の日付のファイルを確認してください。

## 完了基準

以下の条件をすべて満たした場合に作業完了とする：

- 全タスクが適切に完了している
- Test Runnerの結果でエラーが出ていない
- Console にエラーが存在しない
- すべての単体テストが成功している
- 各実装に対応するテストケースが作成されている
- Editor Windowツールの場合、テスト用ウィンドウが正常に動作している
- 適切なGitコミット履歴が作成されている
- Linear上のタスク管理が正確に更新されている


## Project Overview

This is a Unity 6000.0.38f1 project configured with the High Definition Render Pipeline (HDRP). Despite the name "UnityBatchRenderingTool", the project currently contains only Unity's HDRP template assets and sample controllers.

## Unity Configuration

- **Unity Version**: 6000.0.38f1
- **Render Pipeline**: HDRP 17.0.3
- **Key Packages**: Unity Recorder, Cinemachine, Timeline, Input System

## Development Commands

### Unity Editor Operations
- Open project in Unity 6000.0.38f1
- Build: File → Build Settings → Build
- Play Mode: Ctrl+P (Windows/Linux) or Cmd+P (Mac)

### Common Unity Tasks
- To create batch rendering functionality, implement scripts in `Assets/BatchRenderingTool/`
- Use Unity's BatchRendererGroup API for custom batch rendering
- Place Editor tools in `Assets/BatchRenderingTool/Editor/`

## Project Structure

```
Assets/
├── BatchRenderingTool/     # Intended location for batch rendering tools (currently empty)
├── HDRPDefaultResources/   # HDRP configuration and assets
├── SampleSceneAssets/      # Demo assets (controllers, materials, textures)
└── Scenes/                 # Contains SampleScene with HDRP setup
```

## Architecture Notes

The project uses standard Unity patterns:
- Component-based architecture with GameObjects
- HDRP for high-quality rendering
- No custom batch rendering implementation exists yet

## Implementation Guidance

When implementing batch rendering features:
1. Create scripts in `Assets/BatchRenderingTool/Scripts/`
2. Place Editor windows/tools in `Assets/BatchRenderingTool/Editor/`
3. Consider using:
   - BatchRendererGroup API for custom GPU-driven rendering
   - Unity Recorder package for automated capture
   - Timeline for sequencing renders
4. Test with the existing SampleScene HDRP setup

## Batch Rendering Tool Implementation

The project now includes a Single Timeline Renderer tool with RecorderTrack integration:
- **SingleTimelineRenderer**: Complete standalone Timeline rendering tool
  - Dynamically creates render Timelines with ControlTrack and RecorderTrack
  - Uses temporary PlayableDirector for isolated rendering
  - Automatic cleanup of temporary assets
  - Handles Play Mode transitions correctly
- **Editor UI**: Accessible via Window → Batch Rendering Tool → Single Timeline Renderer
- Uses Unity Recorder API's Timeline integration for frame-accurate recording
- Detects all PlayableDirectors in the scene for easy selection
- Supports PNG/JPG/EXR output formats with customizable resolution and frame rate

### Key Implementation Details
- Creates a temporary Timeline for each render with proper track bindings
- ControlTrack controls the original Timeline playback
- RecorderTrack manages the recording process with RecorderClip settings
- Ensures perfect synchronization between Timeline playback and recording
- Properly handles Unity Editor Play Mode transitions during rendering

## Multi Timeline Renderer Implementation

The project now includes a Multi Timeline Renderer tool for batch rendering multiple Timelines:
- **MultiTimelineRenderer**: Advanced batch rendering tool for multiple Timelines
  - Select multiple PlayableDirectors with checkboxes
  - Reorder rendering sequence with up/down buttons
  - Batch render all selected Timelines in a single Play Mode session
  - Individual progress tracking for each Timeline
  - Error handling options (stop on error or continue)
- **Editor UI**: Accessible via Window → Batch Rendering Tool → Multi Timeline Renderer
- **Key Features**:
  - Select All/Deselect All buttons for quick selection
  - Visual status indicators for each Timeline (Idle, Rendering, Complete, Error)
  - Individual and overall progress bars
  - Configurable error handling strategy
  - Each Timeline is rendered to its own output folder

### Multi Timeline Renderer Details
- Optimized Play Mode transitions - enters Play Mode once for all Timelines
- Preserves original PlayOnAwake settings and restores them after rendering
- Uses EditorPrefs to maintain state across Play Mode transitions
- Automatic cleanup of temporary assets after each render
- Detailed error reporting with per-Timeline status tracking