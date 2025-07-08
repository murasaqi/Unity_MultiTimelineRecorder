# Alembic Recorder Implementation Progress

## 概要
Unity Batch Rendering ToolにAlembic（.abc）レコーダー機能を実装中。
3DアニメーションをAlembic形式で書き出し、Maya、Houdini、Blender等との連携を可能にする。

## 完了したタスク ✅

### STU-30: AlembicRecorderSettings基盤構築
- AlembicExportSettings.cs: Alembicエクスポート設定の定義
  - 6種類のエクスポートターゲット（Mesh、SkinnedMesh、Camera、Transform、Particle、Light）
  - 座標系設定（左手系/右手系）
  - タイムサンプリングモード
  - エクスポートスコープ（全シーン、選択階層、特定GameObject等）
- AlembicRecorderSettingsConfig.cs: Alembic設定クラス
  - エクスポート設定の管理
  - プリセット機能（Animation、Camera、FullScene、Effects）
  - バリデーション機能
- RecorderSettingsFactory.cs: Alembicレコーダー作成メソッドを追加
  - CreateAlembicRecorderSettings メソッド群
  - Alembicパッケージチェック
- RecorderSettingsHelper.cs: Alembic用ヘルパーメソッドを追加
  - Alembic出力パス設定（.abc拡張子）
  - AlembicRecorderSettings検証

### STU-31: Alembicエクスポート設定UI実装
- RecorderSettingsDebugWindow.cs: Alembic設定UIを実装
  - プリセット選択機能
  - エクスポートスコープ選択
  - エクスポートターゲットのチェックボックス
  - 座標系・スケール設定
  - サンプリング設定（1-5 samples/frame）
  - ジオメトリ設定（UV、Normal）

## 現在の課題 🚧

### Unity Recorder API制限
- 実際のAlembicRecorderSettingsクラスがUnity Recorder APIで公開されているか不明
- 現在はImageRecorderSettingsをプレースホルダーとして使用
- Unity Alembicパッケージとの統合方法の確認が必要

### 実装上の注意点
- 大規模シーンでのメモリ使用量
- アニメーション中のトポロジー変更への対応
- パーティクルシステムのエクスポート制限

## 次のステップ 📋

### STU-32: SingleTimelineRenderer Alembic統合（未実装）
- RecorderTypeにAlembicを追加済み（デバッグウィンドウ）
- SingleTimelineRendererへの統合
- エクスポート対象の自動検出
- プログレス表示

### STU-33: Alembicテスト・デバッグ機能（未実装）
- エクスポート対象のプレビュー
- ジオメトリカウント表示
- テストエクスポート機能
- ファイルサイズ見積もり

### STU-34: MultiTimelineRenderer Alembic統合（未実装）
- 複数TimelineのAlembicエクスポート
- バッチ処理最適化
- メモリ管理

## 技術的決定事項

1. **Flags enum使用**: 複数エクスポートターゲット選択
2. **プリセットシステム**: よく使うエクスポート設定をプリセット化
3. **座標系変換**: 他ソフトとの互換性のため座標系変換オプション
4. **スケールファクター**: 単位系の違いに対応

## Alembic設定の詳細

### エクスポートターゲット
- **MeshRenderer**: 静的メッシュジオメトリ
- **SkinnedMeshRenderer**: アニメーション付きメッシュ
- **Camera**: カメラパラメータとアニメーション
- **Transform**: トランスフォーム階層
- **ParticleSystem**: パーティクル位置（ポイントクラウド）
- **Light**: ライトパラメータ（限定的サポート）

### プリセット
1. **AnimationExport**: キャラクターアニメーション向け
2. **CameraExport**: カメラモーション向け
3. **FullSceneExport**: シーン全体のエクスポート
4. **EffectsExport**: エフェクト・パーティクル向け

## 今後の検討事項

1. Unity Recorder APIの実際のAlembic実装確認
2. カスタムアトリビュートのエクスポート
3. マテリアル情報の保存方法
4. インクリメンタルエクスポート機能
5. 他ソフトでの読み込みテスト手順の文書化