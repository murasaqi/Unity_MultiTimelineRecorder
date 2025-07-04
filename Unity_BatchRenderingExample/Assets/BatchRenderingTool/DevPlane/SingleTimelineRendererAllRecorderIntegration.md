# SingleTimelineRenderer 全レコーダータイプ統合

## 概要
SingleTimelineRendererにすべてのレコーダータイプ（Image、Movie、AOV、Alembic、Animation）を統合しました。

## 完了した実装内容 ✅

### 1. UI更新
- **レコーダータイプ選択**
  - すべてのレコーダータイプを選択可能に
  - パッケージ依存チェック（AOVはHDRP、AlembicはAlembicパッケージ必須）
  - エラーメッセージ表示

- **各レコーダータイプ専用設定UI**
  - **AOV設定**: プリセット選択、AOVタイプチェックボックス（2列グリッド表示）、出力フォーマット
  - **Alembic設定**: プリセット選択、エクスポートスコープ、ターゲット選択、エクスポートタイプ、座標系設定
  - **Animation設定**: プリセット選択、記録スコープ、ターゲット選択、プロパティ選択、圧縮設定

### 2. 設定変数追加
```csharp
// AOV recorder settings
private AOVType selectedAOVTypes = AOVType.Depth | AOVType.Normal | AOVType.Albedo;
private AOVOutputFormat aovOutputFormat = AOVOutputFormat.EXR16;
private AOVPreset aovPreset = AOVPreset.Compositing;
private bool useAOVPreset = false;

// Alembic recorder settings
private AlembicExportTargets alembicExportTargets = AlembicExportTargets.MeshRenderer | AlembicExportTargets.Transform;
private AlembicExportScope alembicExportScope = AlembicExportScope.EntireScene;
private GameObject alembicTargetGameObject = null;
private AlembicHandedness alembicHandedness = AlembicHandedness.Left;
private float alembicScaleFactor = 1f;
private AlembicExportPreset alembicPreset = AlembicExportPreset.AnimationExport;
private bool useAlembicPreset = false;

// Animation recorder settings
private AnimationRecordingProperties animationRecordingProperties = AnimationRecordingProperties.TransformOnly;
private AnimationRecordingScope animationRecordingScope = AnimationRecordingScope.SingleGameObject;
private GameObject animationTargetGameObject = null;
private AnimationInterpolationMode animationInterpolationMode = AnimationInterpolationMode.Linear;
private AnimationCompressionLevel animationCompressionLevel = AnimationCompressionLevel.Medium;
private AnimationExportPreset animationPreset = AnimationExportPreset.CharacterAnimation;
private bool useAnimationPreset = false;
```

### 3. レンダリングロジック更新
- **CreateRenderTimelineメソッド**
  - 全レコーダータイプ対応
  - AOVの複数設定対応（List<RecorderSettings>）
  - 各タイプに応じたCreateメソッドの呼び出し

- **各レコーダータイプ用Createメソッド追加**
  - `CreateAOVRecorderSettings`: AOV設定作成（複数設定を返す）
  - `CreateAlembicRecorderSettings`: Alembic設定作成
  - `CreateAnimationRecorderSettings`: Animation設定作成

### 4. 出力プレビュー更新
- 各レコーダータイプに適した出力パス表示
  - Image: `{outputPath}/{sanitized}/{sanitized}_<Frame>.{format}`
  - Movie: `{outputPath}/{sanitized}/{sanitized}.{format}`
  - AOV: `{outputPath}/{sanitized}/ (N AOV sequences as .{format})`
  - Alembic: `{outputPath}/{sanitized}/{sanitized}.abc`
  - Animation: `{outputPath}/{sanitized}/{sanitized}.anim`

### 5. 完了メッセージ更新
- 各レコーダータイプに応じた完了メッセージ
- AOVは記録したAOVの数を表示

## 実装の特徴

### プリセットシステム
- 各レコーダータイプでプリセット選択可能
- カスタム設定との切り替えが容易

### UIの一貫性
- すべてのレコーダータイプで統一されたUI構造
- プリセット選択 → 詳細設定の流れ

### エラーハンドリング
- パッケージ依存チェック
- 設定検証（Validate）
- 適切なエラーメッセージ表示

## 使用方法

1. **レコーダータイプ選択**
   - Recorder Type ドロップダウンから選択
   - AOV/Alembicの場合、必要なパッケージがインストールされているか確認

2. **設定**
   - プリセットを使用するか、カスタム設定を行う
   - 各レコーダータイプ固有の設定を調整

3. **レンダリング実行**
   - Start Renderingボタンをクリック
   - PlayModeに入り、自動的にレンダリングが実行される

## 今後の改善点

1. **AOV複数トラック対応**
   - 現在は最初の設定のみRecorderTrackに設定
   - 複数のAOVを同時に記録する場合の最適化

2. **プログレス表示の改善**
   - 各レコーダータイプに応じた詳細なプログレス情報

3. **検証とテスト**
   - 各レコーダータイプの実際の動作確認
   - Unity Recorder APIとの互換性確認

## 関連ファイル
- SingleTimelineRenderer.cs（更新）
- RecorderSettingsFactory.cs（活用）
- RecorderSettingsHelper.cs（活用）
- 各RecorderSettingsConfigクラス（活用）