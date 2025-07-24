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
    - [x] `GameObjectReferenceService`クラス ✅

  - [x] 2.3 レコーダー設定インターフェース
    - [x] `IRecorderConfiguration`インターフェース ✅
    - [x] `RecorderConfigurationBase`基底クラス ✅
    - [x] フレームレート統一制約の実装 ✅

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

- [x] 5. 既存機能の新アーキテクチャ統合

  - [x] 5.1 既存MultiTimelineRecorderの新アーキテクチャ統合 (TODO-352) ✅
    - [x] 既存UIロジックのコントローラーへの移行 ✅
    - [x] 3カラムレイアウトの新アーキテクチャ対応 ✅
    - [x] 既存レコーダーエディターの統合 ✅
    - _Requirements: 4.1, 4.2, 4.4_

  - [x] 5.2 フレームレート統一制約の実装 (TODO-352) ✅
    - [x] グローバルフレームレート管理の実装 ✅
    - [x] レコーダー設定でのフレームレート統一適用 ✅
    - [x] 検証機能の実装 ✅
    - _Requirements: 2.2, 2.3_

- [x] 6. ワイルドカード管理システムの新アーキテクチャ統合 (TODO-353) ✅
  - [x] 6.1 既存WildcardProcessorの新システム統合 ✅
    - [x] `WildcardRegistry`クラスの実装 ✅
    - [x] `EnhancedWildcardProcessor`クラスの実装 ✅
    - [x] Unity Recorderワイルドカードのパススルー機能 ✅
    - [x] Multi Timeline Recorderワイルドカード処理（Timeline, TimelineTake, RecorderTake, RecorderName） ✅
    - _Requirements: 8.1, 8.2, 8.3_

  - [x] 6.2 ユーザーカスタマイズ機能の実装 (TODO-359) ✅
    - [x] `WildcardManagementSettings`クラス ✅
    - [x] `TemplateRegistry`クラス ✅
    - [x] カスタムワイルドカード定義機能 ✅
    - [x] テンプレートプリセット管理 ✅
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 7. GameObject参照管理の完成 (TODO-354) ✅
  - [x] 7.1 GameObjectReferenceServiceの完全実装 ✅
    - [x] 参照作成・復元機能の完成 ✅
    - [x] シーン変更時の自動復元 ✅
    - [x] 参照復元失敗時の警告システム ✅
    - _Requirements: 10.1, 10.2_

  - [x] 7.2 レコーダー設定での参照管理統合 ✅
    - [x] 各レコーダー設定クラスでの参照保持機能 ✅
    - [x] 参照復元メソッドの実装 ✅
    - [x] 参照の検証とエラーハンドリング ✅
    - _Requirements: 11.1, 11.2, 11.3_

- [x] 8. UI機能の完成 (TODO-355) ✅
  - [x] 8.1 "Apply to All Selected Timelines"機能の実装 ✅
    - [x] 右クリックメニューの実装 ✅
    - [x] 同名レコーダー上書き・新規追加ロジック ✅
    - [x] 複数タイムライン選択時のみ表示制御 ✅
    - _Requirements: 4.1, 4.3_

  - [x] 8.2 改善されたエラー表示システム ✅
    - [x] ユーザーフレンドリーなエラーメッセージ ✅
    - [x] 解決策の提示機能 ✅
    - [x] GameObject参照エラーの詳細表示 ✅
    - _Requirements: 5.1, 5.3, 12.3_

- [x] 9. SignalEmitter機能の新アーキテクチャ統合 (TODO-356) ✅
  - [x] 9.1 既存SignalEmitterRecordControlの統合 ✅
    - [x] 新サービス層での実装 ✅
    - [x] タイムライン期間制御機能 ✅
    - [x] [MTR]トラック優先検索機能 ✅
    - _Requirements: 3.1, 3.3_

  - [x] 9.2 SignalEmitter UI機能の統合 ✅
    - [x] タイムライン一覧でのマーカー表示 ✅
    - [x] 録画期間表示（秒数/フレーム数切り替え） ✅
    - [x] SignalEmitter設定パネル ✅
    - _Requirements: 4.1, 4.3_

- [x] 10. コンパイルエラーの修正 ✅
  - [x] IRecorderConfiguration.Cloneメソッドの実装 ✅
  - [x] IEventBusインターフェースの追加 ✅
  - [x] ValidationResultのMergeメソッド追加 ✅
  - [x] 各種using文の修正 ✅
  - [x] SignalTimingInfoのプロパティ名修正 ✅
  - [x] AlembicRecorderSettingsConfigのプロパティ名修正 ✅

## 📋 次の優先タスク (Next Priority)

- [ ] 13. 基本機能テスト
- [ ] 14. プログラマティックAPI
- [ ] 15. パフォーマンス最適化
- [ ] 16. 最終検証とドキュメント

## 🔧 中期タスク (Medium Priority)

- [x] 11. レコーダー設定ファクトリーの拡張 (TODO-360) ✅
  - [x] 11.1 既存RecorderSettingsFactoryの新アーキテクチャ統合 ✅
    - [x] `RecorderConfigurationFactory`クラスの完成 ✅
    - [x] GameObject参照の自動設定 ✅
    - [x] フレームレート統一適用 ✅
    - [x] プラグイン対応アーキテクチャ ✅
    - _Requirements: 8.1, 8.2, 8.3_

- [x] 12. 設定の永続化とシーン管理の強化 (TODO-361) ✅
  - [x] 12.1 シーン固有設定の完全実装 ✅
    - [x] シーン変更時の自動保存・復元 ✅
    - [x] 設定の競合解決 ✅
    - [x] 設定のインポート/エクスポート ✅
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 12.2 設定検証システムの強化 ✅
    - [x] 詳細な検証ルール ✅
    - [x] 自動修復機能 ✅
    - [x] 設定の互換性チェック ✅
    - _Requirements: 2.2, 2.3_

## 🧪 テストと検証 (Testing Phase)

- [ ] 13. 基本機能テスト
  - [ ] 13.1 サービス層の単体テスト
    - [ ] ConfigurationService、TimelineService、RecordingServiceのテスト
    - [ ] GameObject参照管理のテスト
    - [ ] ワイルドカード処理のテスト
    - _Requirements: 6.1, 6.2_

  - [ ] 13.2 統合テスト
    - [ ] 3カラムUI操作の統合テスト
    - [ ] 録画ワークフローのエンドツーエンドテスト
    - [ ] GameObject参照の保持・復元テスト
    - _Requirements: 6.2, 6.4_

## 🚀 高度な機能とAPI (Advanced Features)

- [ ] 14. プログラマティックAPI
  - [ ] 14.1 MultiTimelineRecorderAPIの実装
    - [ ] UIに依存しないAPI設計
    - [ ] 既存サービス層の活用
    - [ ] 外部制御インターフェース
    - _Requirements: 9.1, 9.2_

  - [ ] 14.2 設定ビルダーAPI
    - [ ] RecordingConfigurationBuilderの実装
    - [ ] 流暢なAPI設計
    - [ ] 設定の検証とエラーハンドリング
    - _Requirements: 9.1, 9.3, 9.4_

## 🎯 最終調整と品質向上 (Final Polish)

- [ ] 15. パフォーマンス最適化
  - [ ] 15.1 大量タイムライン処理の最適化
    - [ ] メモリ使用量の最適化
    - [ ] UI応答性の改善
    - [ ] バックグラウンド処理の実装
    - _Requirements: 7.3, 7.4_

- [ ] 16. 最終検証とドキュメント
  - [ ] 16.1 機能完全性の検証
    - [ ] 旧システムとの機能パリティ確認
    - [ ] 全レコーダータイプでの動作確認
    - [ ] SignalEmitter機能の動作確認
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [ ] 16.2 ユーザードキュメント作成
    - [ ] 新機能の使用方法ガイド
    - [ ] GameObject参照管理の説明
    - [ ] トラブルシューティングガイド
    - _Requirements: 11.1, 11.2, 11.3_

---

## 🎯 開発再開の推奨順序

### 完了済み (Completed) - 2025年1月
1. **Task 5**: 既存機能の新アーキテクチャ統合 (TODO-352) ✅
2. **Task 6**: ワイルドカード管理システム (TODO-353) ✅
3. **Task 7**: GameObject参照管理の完成 (TODO-354) ✅
4. **Task 8**: UI機能の完成 (TODO-355) ✅
5. **Task 9**: SignalEmitter機能統合 (TODO-356) ✅
6. **Task 10**: コンパイルエラー修正 ✅

### 次のステップ (Next Steps)
7. **Task 11-12**: 中期タスク - 機能拡張 ✅ (完了済み)

### 最終段階 (Final Phase)
8. **Task 13-16**: テスト・API・最終調整

## 📊 進捗状況サマリー
- ✅ **完了**: 
  - 基盤アーキテクチャ、コアサービス、UIコントローラー
  - 既存機能統合（TODO-352）
  - ワイルドカード管理システム（TODO-353）
  - GameObject参照管理（TODO-354）
  - UI機能完成（TODO-355）
  - SignalEmitter機能統合（TODO-356）
  - コンパイルエラー修正
- 🔄 **進行中**: なし（Linear上の全タスク完了）
- 📋 **次の優先**: テストフェーズ（Task 13: 基本機能テスト）
- 🎯 **全体進捗**: 約80%完了（中期タスクも完了）

Linear上の全タスクが完了しました。新アーキテクチャへの移行が成功し、主要機能が実装されました。

## 🎉 主な成果
- サービス指向アーキテクチャへの移行完了
- Unity Timeline制約に対応したフレームレート統一機能
- Unity RecorderとMulti Timeline Recorderのワイルドカード分離
- GameObject参照の自動復元機能
- ユーザーフレンドリーなエラー表示と解決策提示
- SignalEmitterによる録画範囲制御