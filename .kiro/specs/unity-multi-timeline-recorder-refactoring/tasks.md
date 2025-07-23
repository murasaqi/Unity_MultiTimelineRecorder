# Implementation Plan - Development Restart ToDo

## ✅ 実装済み (Completed)

- [x] 1. プロジェクト構造の準備
  - [x] 新しいディレクトリ構造を作成
    - `Editor/Core/` - コアサービス ✅
    - `Editor/Core/Services/` - ビジネスロジックサービス ✅
    - `Editor/Core/Models/` - データモデル ✅
    - `Editor/UI/Controllers/` - UIコントローラー ✅
    - `Editor/UI/` - UIビュー ✅
    - `Editor/Utilities/` - ユーティリティ（既存活用） ✅
  - [x] Assembly Definition Fileの更新 ✅
  - [x] 名前空間の整理 ✅

- [x] 2. コアデータモデルの実装
  - [x] 2.1 基本データモデルの作成
    - [x] `GlobalRecordingSettings`クラス ✅
    - [x] `RecordingConfiguration`クラス ✅
    - [x] `TimelineRecorderConfig`クラス ✅
    - [x] `ValidationResult`クラス ✅
    - [x] `MultiTimelineRecorderSettings`クラス（既存拡張） ✅

  - [x] 2.2 GameObject参照管理モデル
    - [x] `GameObjectReference`クラス ✅
    - [x] 階層パス生成・解決機能 ✅
    - [ ] `GameObjectReferenceService`クラス（部分実装）

  - [x] 2.3 レコーダー設定インターフェース
    - [x] `IRecorderConfiguration`インターフェース ✅
    - [x] `RecorderConfigurationBase`基底クラス ✅
    - [ ] フレームレート統一制約の実装

- [x] 3. コアサービスの基本実装
  - [x] 3.1 ConfigurationServiceの基本実装 ✅
  - [x] 3.2 RecordingServiceの基本実装 ✅
  - [x] 3.3 TimelineServiceの基本実装 ✅
  - [x] 3.4 ErrorHandlingServiceの実装 ✅
  - [x] 3.5 UnityConsoleLoggerの実装 ✅
  - [x] 3.6 ServiceLocatorの実装 ✅

- [x] 4. UIコントローラーの基本実装
  - [x] 4.1 MainWindowControllerの作成 ✅
  - [x] 4.2 RecorderConfigurationControllerの作成 ✅

## 🔄 進行中 (In Progress)

- [ ] 5. 既存機能の新アーキテクチャ統合

  - [ ] 5.1 既存MultiTimelineRecorderの新アーキテクチャ統合
    - [ ] 既存UIロジックのコントローラーへの移行
    - [ ] 3カラムレイアウトの新アーキテクチャ対応
    - [ ] 既存レコーダーエディターの統合
    - _Requirements: 4.1, 4.2, 4.4_

  - [ ] 5.2 フレームレート統一制約の実装
    - [ ] グローバルフレームレート管理の実装
    - [ ] レコーダー設定でのフレームレート統一適用
    - [ ] 検証機能の実装
    - _Requirements: 2.2, 2.3_

## 📋 次の優先タスク (Next Priority)

- [ ] 6. ワイルドカード管理システムの新アーキテクチャ統合
  - [ ] 6.1 既存WildcardProcessorの新システム統合
    - [ ] `WildcardRegistry`クラスの実装
    - [ ] `EnhancedWildcardProcessor`クラスの実装
    - [ ] Unity Recorderワイルドカードのパススルー機能
    - [ ] Multi Timeline Recorderワイルドカード処理（Timeline, TimelineTake, RecorderTake, RecorderName）
    - _Requirements: 8.1, 8.2, 8.3_

  - [ ] 6.2 ユーザーカスタマイズ機能の実装
    - [ ] `WildcardManagementSettings`クラス
    - [ ] `TemplateRegistry`クラス
    - [ ] カスタムワイルドカード定義機能
    - [ ] テンプレートプリセット管理
    - _Requirements: 8.1, 8.2, 8.3_

- [ ] 7. GameObject参照管理の完成
  - [ ] 7.1 GameObjectReferenceServiceの完全実装
    - [ ] 参照作成・復元機能の完成
    - [ ] シーン変更時の自動復元
    - [ ] 参照復元失敗時の警告システム
    - _Requirements: 10.1, 10.2_

  - [ ] 7.2 レコーダー設定での参照管理統合
    - [ ] 各レコーダー設定クラスでの参照保持機能
    - [ ] 参照復元メソッドの実装
    - [ ] 参照の検証とエラーハンドリング
    - _Requirements: 11.1, 11.2, 11.3_

- [ ] 8. UI機能の完成
  - [ ] 8.1 "Apply to All Selected Timelines"機能の実装
    - [ ] 右クリックメニューの実装
    - [ ] 同名レコーダー上書き・新規追加ロジック
    - [ ] 複数タイムライン選択時のみ表示制御
    - _Requirements: 4.1, 4.3_

  - [ ] 8.2 改善されたエラー表示システム
    - [ ] ユーザーフレンドリーなエラーメッセージ
    - [ ] 解決策の提示機能
    - [ ] GameObject参照エラーの詳細表示
    - _Requirements: 5.1, 5.3, 12.3_

- [ ] 9. SignalEmitter機能の新アーキテクチャ統合
  - [ ] 9.1 既存SignalEmitterRecordControlの統合
    - [ ] 新サービス層での実装
    - [ ] タイムライン期間制御機能
    - [ ] [MTR]トラック優先検索機能
    - _Requirements: 3.1, 3.3_

  - [ ] 9.2 SignalEmitter UI機能の統合
    - [ ] タイムライン一覧でのマーカー表示
    - [ ] 録画期間表示（秒数/フレーム数切り替え）
    - [ ] SignalEmitter設定パネル
    - _Requirements: 4.1, 4.3_

## 🔧 中期タスク (Medium Priority)

- [ ] 10. レコーダー設定ファクトリーの拡張
  - [ ] 10.1 既存RecorderSettingsFactoryの新アーキテクチャ統合
    - [ ] `RecorderConfigurationFactory`クラスの完成
    - [ ] GameObject参照の自動設定
    - [ ] フレームレート統一適用
    - [ ] プラグイン対応アーキテクチャ
    - _Requirements: 8.1, 8.2, 8.3_

- [ ] 11. 設定の永続化とシーン管理の強化
  - [ ] 11.1 シーン固有設定の完全実装
    - [ ] シーン変更時の自動保存・復元
    - [ ] 設定の競合解決
    - [ ] 設定のインポート/エクスポート
    - _Requirements: 2.1, 2.2, 2.3_

  - [ ] 11.2 設定検証システムの強化
    - [ ] 詳細な検証ルール
    - [ ] 自動修復機能
    - [ ] 設定の互換性チェック
    - _Requirements: 2.2, 2.3_

## 🧪 テストと検証 (Testing Phase)

- [ ] 12. 基本機能テスト
  - [ ] 12.1 サービス層の単体テスト
    - [ ] ConfigurationService、TimelineService、RecordingServiceのテスト
    - [ ] GameObject参照管理のテスト
    - [ ] ワイルドカード処理のテスト
    - _Requirements: 6.1, 6.2_

  - [ ] 12.2 統合テスト
    - [ ] 3カラムUI操作の統合テスト
    - [ ] 録画ワークフローのエンドツーエンドテスト
    - [ ] GameObject参照の保持・復元テスト
    - _Requirements: 6.2, 6.4_

## 🚀 高度な機能とAPI (Advanced Features)

- [ ] 13. プログラマティックAPI
  - [ ] 13.1 MultiTimelineRecorderAPIの実装
    - [ ] UIに依存しないAPI設計
    - [ ] 既存サービス層の活用
    - [ ] 外部制御インターフェース
    - _Requirements: 9.1, 9.2_

  - [ ] 13.2 設定ビルダーAPI
    - [ ] RecordingConfigurationBuilderの実装
    - [ ] 流暢なAPI設計
    - [ ] 設定の検証とエラーハンドリング
    - _Requirements: 9.1, 9.3, 9.4_

## 🎯 最終調整と品質向上 (Final Polish)

- [ ] 14. パフォーマンス最適化
  - [ ] 14.1 大量タイムライン処理の最適化
    - [ ] メモリ使用量の最適化
    - [ ] UI応答性の改善
    - [ ] バックグラウンド処理の実装
    - _Requirements: 7.3, 7.4_

- [ ] 15. 最終検証とドキュメント
  - [ ] 15.1 機能完全性の検証
    - [ ] 旧システムとの機能パリティ確認
    - [ ] 全レコーダータイプでの動作確認
    - [ ] SignalEmitter機能の動作確認
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [ ] 15.2 ユーザードキュメント作成
    - [ ] 新機能の使用方法ガイド
    - [ ] GameObject参照管理の説明
    - [ ] トラブルシューティングガイド
    - _Requirements: 11.1, 11.2, 11.3_

---

## 🎯 開発再開の推奨順序

### 即座に開始可能 (Ready to Start)
1. **Task 5**: 既存機能の新アーキテクチャ統合 - 最優先
2. **Task 6**: ワイルドカード管理システム - 重要　機能
3. **Task 7**: GameObject参照管理の完成 - 安定性向上

### 次のステップ (Next Steps)
4. **Task 8**: UI機能の完成 - ユーザビリティ向上
5. **Task 9**: SignalEmitter機能統合 - 高度な機能
6. **Task 10-11**: 中期タスク - 機能拡張

### 最終段階 (Final Phase)
7. **Task 12-15**: テスト・API・最終調整

## 📊 進捗状況サマリー
- ✅ **完了**: 基盤アーキテクチャ、コアサービス、UIコントローラー
- 🔄 **進行中**: 既存機能の統合
- 📋 **次の優先**: ワイルドカード管理、GameObject参照管理
- 🎯 **全体進捗**: 約40%完了

新アーキテクチャの基盤が既に構築されているため、既存機能の統合から開始することを強く推奨します。