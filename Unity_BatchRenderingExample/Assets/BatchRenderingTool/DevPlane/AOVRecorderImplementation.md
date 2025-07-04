# AOV Recorder Implementation Progress

## 概要
Unity Batch Rendering ToolにAOV（Arbitrary Output Variables）レコーダー機能を実装中。
HDRPプロジェクトでコンポジット用の各種レンダリングパスを個別に書き出せるようにする。

## 完了したタスク ✅

### STU-23: AOVRecorderSettings基盤構築
- AOVRecorderTypes.cs: AOVタイプ列挙型とヘルパークラスを作成
  - 18種類のAOVタイプを定義（Depth、Normal、Motion Vectors、Albedo等）
  - カテゴリ別グループ化（Geometry、Material、Lighting、Additional）
  - HDRPパッケージ依存性チェック機能
- AOVRecorderSettingsConfig.cs: AOV設定クラスを作成
  - 複数AOV選択対応（Flags enum）
  - 出力フォーマット設定（EXR16/32、PNG16、TGA）
  - プリセット機能（Compositing、GeometryOnly、LightingOnly、MaterialProperties）
- RecorderSettingsFactory.cs: AOVレコーダー作成メソッドを追加
  - CreateAOVRecorderSettings メソッド群
  - HDRPチェックとエラーハンドリング
- RecorderSettingsHelper.cs: AOV用ヘルパーメソッドを追加
  - AOV出力パス設定
  - AOVRecorderSettings検証

### STU-24: AOVタイプ選択UI実装
- RecorderSettingsDebugWindow.cs: AOV選択UIを実装
  - カテゴリ別チェックボックスリスト
  - プリセット選択機能
  - 選択数表示とSelect All/Clear Allボタン
  - 各AOVタイプの説明とツールチップ
  - 推奨フォーマット表示

## 現在の課題 🚧

### Unity Recorder API制限
- 実際のAOVRecorderSettingsクラスがUnity Recorder APIで公開されているか不明
- 現在はImageRecorderSettingsをプレースホルダーとして使用
- 実際のHDRP AOV APIとの統合が必要

### 実装上の注意点
- AOVレコーダーは1つのTimelineに対して複数のRecorderClipが必要になる可能性
- 各AOVタイプごとに別々のレコーダーインスタンスが必要
- メモリ使用量とパフォーマンスの考慮が必要

## 次のステップ 📋

### STU-25: SingleTimelineRenderer AOV統合（未実装）
- RecorderTypeにAOVを追加
- AOV選択時の専用UI表示を統合
- 複数RecorderClip作成対応
- AOVタイプ別の出力パス管理

### STU-26: AOVテスト・デバッグ機能（未実装）
- HDRPプロジェクト判定機能
- 利用可能なAOVタイプの自動検出
- AOVプレビュー機能
- テストレンダリング機能

### STU-27: MultiTimelineRenderer AOV統合（未実装）
- 複数Timeline×複数AOVの管理
- バッチ処理最適化
- メモリ管理とエラーハンドリング

## 技術的決定事項

1. **Flags enum使用**: 複数AOV選択のためにビットフラグを使用
2. **カテゴリ別UI**: ユーザビリティ向上のためAOVをカテゴリ別に表示
3. **プリセットシステム**: よく使うAOV組み合わせをプリセット化
4. **条件コンパイル**: HDRPパッケージ有無を`UNITY_PIPELINE_HDRP`で判定

## 今後の検討事項

1. Unity Recorder APIの実際のAOV実装確認
2. HDRP Custom Passとの統合方法
3. 大量EXRファイル生成時のディスク容量管理
4. レンダリング時間見積もり機能
5. 部分的な再レンダリング機能