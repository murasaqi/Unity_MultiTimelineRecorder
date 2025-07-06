using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BatchRenderingTool
{
    /// <summary>
    /// AOV録画機能の実装方法に関するドキュメント
    /// Unity Recorder 5.1.2にはAOVRecorderSettingsが存在しないため、
    /// 代替実装方法を提供します
    /// </summary>
    public static class AOVRecorderImplementation
    {
        /// <summary>
        /// Unity Recorder 5.1.2でのAOV実装アプローチ
        /// </summary>
        public static class ImplementationApproach
        {
            public const string Info = @"
Unity Recorder 5.1.2でのAOV実装方法:

1. HDRPのCustom Passを使用
   - Custom Passでレンダーターゲットを切り替え
   - 各AOVタイプごとにレンダリング

2. ImageRecorderSettingsを複数使用
   - 各AOVタイプごとにImageRecorderSettingsを作成
   - カメラのターゲットテクスチャを切り替えながら録画

3. 実装の流れ:
   a) HDRPのCustom Pass Volumeを作成
   b) 各AOVタイプ用のRenderTextureを準備
   c) Custom Passでレンダリング内容を制御
   d) ImageRecorderSettingsでRenderTextureを録画
";
        }
        
        /// <summary>
        /// AOV録画のための暫定的な実装
        /// </summary>
        public static class AOVRecorderFallback
        {
            /// <summary>
            /// AOV録画設定を作成（ImageRecorderSettingsを使用）
            /// </summary>
            public static List<UnityEditor.Recorder.RecorderSettings> CreateAOVRecorderSettingsFallback(
                string baseName, 
                AOVRecorderSettingsConfig config)
            {
                var settingsList = new List<UnityEditor.Recorder.RecorderSettings>();
                
                // HDRPチェック
                if (!AOVTypeInfo.IsHDRPAvailable())
                {
                    Debug.LogWarning("[AOVRecorder] HDRP is not available. AOV recording requires HDRP.");
                    return settingsList;
                }
                
                var selectedAOVs = config.GetSelectedAOVsList();
                
                foreach (var aovType in selectedAOVs)
                {
                    // 各AOVタイプごとにImageRecorderSettingsを作成
                    var settings = ScriptableObject.CreateInstance<UnityEditor.Recorder.ImageRecorderSettings>();
                    settings.name = $"{baseName}_AOV_{aovType}";
                    
                    // 基本設定
                    settings.Enabled = true;
                    settings.RecordMode = UnityEditor.Recorder.RecordMode.Manual;
                    settings.FrameRatePlayback = UnityEditor.Recorder.FrameRatePlayback.Constant;
                    settings.FrameRate = config.frameRate;
                    settings.CapFrameRate = config.capFrameRate;
                    
                    // 出力フォーマット設定
                    switch (config.outputFormat)
                    {
                        case AOVOutputFormat.EXR16:
                        case AOVOutputFormat.EXR32:
                            settings.OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                            break;
                        default:
                            settings.OutputFormat = UnityEditor.Recorder.ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                            break;
                    }
                    
                    // 解像度設定
                    var inputSettings = new UnityEditor.Recorder.Input.GameViewInputSettings
                    {
                        OutputWidth = config.width,
                        OutputHeight = config.height,
                        FlipFinalOutput = config.flipVertical
                    };
                    settings.imageInputSettings = inputSettings;
                    
                    // 出力パス設定（AOVタイプごとにサブフォルダ）
                    settings.OutputFile = $"{baseName}/AOV_{aovType}/<Frame>";
                    
                    settingsList.Add(settings);
                    
                    Debug.Log($"[AOVRecorder] Created fallback settings for AOV type: {aovType}");
                }
                
                return settingsList;
            }
            
            /// <summary>
            /// AOV録画の実装ステータスを取得
            /// </summary>
            public static string GetImplementationStatus()
            {
                return @"
AOV Recorder実装ステータス:
- Unity Recorder 5.1.2には専用のAOVRecorderSettingsが存在しません
- 現在はImageRecorderSettingsを使用した暫定実装を提供しています
- 完全なAOV機能にはHDRPのCustom Pass統合が必要です

推奨事項:
1. HDRPのCustom Pass Volumeを使用してAOVレンダリングを実装
2. 各AOVタイプごとにRenderTextureを準備
3. ImageRecorderSettingsでRenderTextureを録画

将来的な改善:
- Unity RecorderのAPIが更新されたら専用のAOVRecorderSettingsを使用
- HDRPとの深い統合によるより効率的なAOVワークフロー
";
            }
        }
    }
}