<!-- # 開発メモ

## プロジェクト状態
SingleTimelineRendererが完全に動作することを確認。複雑な実装をすべて削除し、シンプルで確実な単一Timelineレンダリング機能のみを提供。

## アーキテクチャ

### BatchRenderingController（更新版）
- **RecorderTrackを使用した新しい実装方式**
  - 動的にレンダリング用TimelineAssetを作成
  - ControlTrackで元のTimelineを制御
  - RecorderTrackとRecorderClipで録画を管理
  - 一時的なPlayableDirectorで再生制御
- **選択的レンダリング機能**
  - StartSelectiveRenderingメソッドで選択されたTimelineのみレンダリング
  - Timeline毎のカスタム設定をサポート
  - エラーハンドリングとイベント通知

#### レンダリングフロー
1. ユーザーがレンダリングしたいTimelineを選択
2. 選択されたTimelineに対してレンダリング用Timelineを動的生成
3. カスタム設定がある場合は、その設定を適用
4. ControlTrackを追加し、元のTimelineを制御対象に設定
5. RecorderTrackを追加し、RecorderClipで録画設定を適用
6. 一時的なPlayableDirectorでレンダリング用Timelineを再生
7. 再生完了後、自動的に録画ファイルが出力される
8. 使用した一時アセットをクリーンアップ

### 現在のコンポーネント

#### 1. SingleTimelineRenderer
- 単一Timelineのレンダリング機能（完全動作確認済み）
- RecorderTrackを使用した正確なレンダリング
- Play Mode移行の正しい処理
- 完全に独立したレンダリング機能

#### 2. RecorderClipUtility
- RecorderClipの初期化ヘルパー
- ImageRecorderSettingsの正しい作成
- リフレクションを使用した内部設定

#### 3. BatchRenderingDebugHelper
- 一時アセットのクリア
- EditorPrefsの確認とクリア
- デバッグ用ユーティリティ

## 実装の特徴

### RecorderTrack方式の利点
- Unity標準のワークフローに準拠
- Timeline上でのフレーム同期が正確
- Recorder Clipの詳細設定が可能
- 再生と録画の完全な同期

### 選択的レンダリングの利点
- 必要なTimelineのみレンダリング可能
- Timeline毎に異なる設定を適用可能
- レンダリング時間の短縮
- エラー発生時の影響範囲を限定

### 技術的な詳細
```csharp
// Timeline選択的レンダリング
var selectedTimelines = timelineSettings.Where(t => t.isSelected).ToList();
controller.StartSelectiveRendering(selectedTimelines);

// カスタム設定の適用
if (settings.useCustomSettings)
{
    renderSettings = settings.ToRenderSettings(renderSettings.outputPath);
}

// エラーハンドリング
controller.OnError += (timelineName, errorMessage) =>
{
    // エラー処理
};
```

## 使い方ガイド

### 単一Timelineのレンダリング
1. **Window > Batch Rendering Tool > Single Timeline Renderer** を開く
2. Timelineを選択
3. レンダリング設定を調整
4. 「Start Rendering」ボタンをクリック

### 複数Timelineのレンダリング
複数のTimelineをレンダリングする場合は、SingleTimelineRendererを使用して一つずつレンダリングしてください。

### デバッグ機能
**Window > Batch Rendering Tool > Debug** メニューから：
- Clear Temp Assets: 一時ファイルを削除
- Force Exit Play Mode: Play Modeを強制終了
- Check Editor Prefs: エディタ設定を確認

## 実装済み機能
- [x] SingleTimelineRenderer: 単一Timelineのレンダリング
- [x] RecorderTrackを使用した正確なレンダリング
- [x] Play Mode移行の正しい処理
- [x] デバッグヘルパー機能

## トラブルシューティング

### 録画が開始されない場合
- Unity EditorがPlayモードでないことを確認
- RecorderTrackが正しく設定されているか確認
- Timeline上のRecorderClipの長さが適切か確認

### 出力ファイルが見つからない場合
- 出力パスが正しいか確認
- ファイル名のテンプレートが適切か確認
- ディスク容量が十分か確認


## 参考リンク

- [Unity Recorder Documentation](https://docs.unity3d.com/Packages/com.unity.recorder@latest)
- [Timeline Documentation](https://docs.unity3d.com/Packages/com.unity.timeline@latest)
- [Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@latest) -->