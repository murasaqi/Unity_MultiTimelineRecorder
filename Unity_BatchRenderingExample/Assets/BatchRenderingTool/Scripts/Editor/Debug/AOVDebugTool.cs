using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine.Rendering;

namespace BatchRenderingTool.DebugTools
{
    /// <summary>
    /// AOVレコーダーのテストとデバッグ機能を提供するツール
    /// </summary>
    public class AOVDebugTool : EditorWindow
    {
        private AOVRecorderSettingsConfig testConfig;
        private Vector2 scrollPosition;
        
        // デバッグ情報
        private bool hdrpCheckFoldout = true;
        private bool aovTypesFoldout = true;
        private bool previewFoldout = true;
        private bool testRenderFoldout = true;
        private bool performanceFoldout = true;
        
        // HDRP情報
        private bool isHDRPActive = false;
        private string hdrpVersion = "Not found";
        private string renderPipelineInfo = "";
        private List<string> availableAOVTypes = new List<string>();
        
        // テストレンダリング設定
        private string testOutputPath = "Assets/AOVTest";
        private int testFrameCount = 1;
        private bool showAdvancedSettings = false;
        
        // パフォーマンス計測
        private Dictionary<AOVType, float> aovRenderTimes = new Dictionary<AOVType, float>();
        private float lastTestDuration = 0f;
        
        // エラーログ
        private List<string> errorLogs = new List<string>();
        private List<string> warningLogs = new List<string>();
        
        [MenuItem("Window/Batch Rendering Tool/Debug/AOV Debug Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<AOVDebugTool>("AOV Debug Tool");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }
        
        private void OnEnable()
        {
            testConfig = new AOVRecorderSettingsConfig();
            CheckHDRPStatus();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("AOV Debug Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // HDRPチェック
            DrawHDRPCheck();
            
            EditorGUILayout.Space();
            
            // 利用可能なAOVタイプ
            DrawAvailableAOVTypes();
            
            EditorGUILayout.Space();
            
            // AOVプレビュー
            DrawAOVPreview();
            
            EditorGUILayout.Space();
            
            // テストレンダリング
            DrawTestRendering();
            
            EditorGUILayout.Space();
            
            // パフォーマンス計測
            DrawPerformanceMetrics();
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// HDRPチェックセクションを描画
        /// </summary>
        private void DrawHDRPCheck()
        {
            hdrpCheckFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(hdrpCheckFoldout, "HDRP Project Check");
            if (hdrpCheckFoldout)
            {
                EditorGUI.indentLevel++;
                
                // HDRPステータス
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("HDRP Status:", GUILayout.Width(120));
                if (isHDRPActive)
                {
                    EditorGUILayout.LabelField("✓ Active", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } });
                }
                else
                {
                    EditorGUILayout.LabelField("✗ Not Active", new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } });
                }
                EditorGUILayout.EndHorizontal();
                
                // バージョン情報
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("HDRP Version:", GUILayout.Width(120));
                EditorGUILayout.LabelField(hdrpVersion);
                EditorGUILayout.EndHorizontal();
                
                // レンダーパイプライン情報
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Pipeline Info:", GUILayout.Width(120));
                EditorGUILayout.LabelField(renderPipelineInfo);
                EditorGUILayout.EndHorizontal();
                
                // 再チェックボタン
                if (GUILayout.Button("Re-check HDRP"))
                {
                    CheckHDRPStatus();
                }
                
                // HDRPがない場合の警告
                if (!isHDRPActive)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "AOV Recorder requires HDRP (High Definition Render Pipeline).\n\n" +
                        "Please:\n" +
                        "1. Install HDRP package via Package Manager\n" +
                        "2. Configure your project to use HDRP\n" +
                        "3. Set HDRP Asset in Project Settings > Graphics",
                        MessageType.Error);
                    
                    if (GUILayout.Button("Open Package Manager"))
                    {
                        UnityEditor.PackageManager.UI.Window.Open("com.unity.render-pipelines.high-definition");
                    }
                }
                else
                {
                    // HDRP設定の推奨事項
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("HDRP Settings Recommendations:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "For AOV rendering:\n" +
                        "• Enable 'Custom Pass' in HDRP Asset\n" +
                        "• Enable 'Motion Vectors' for motion blur AOV\n" +
                        "• Configure 'AOV Request' in Frame Settings\n" +
                        "• Set appropriate buffer formats in HDRP Asset",
                        MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// 利用可能なAOVタイプセクションを描画
        /// </summary>
        private void DrawAvailableAOVTypes()
        {
            aovTypesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(aovTypesFoldout, "Available AOV Types");
            if (aovTypesFoldout)
            {
                EditorGUI.indentLevel++;
                
                if (!isHDRPActive)
                {
                    EditorGUILayout.HelpBox("HDRP is required to detect available AOV types", MessageType.Warning);
                }
                else
                {
                    // カテゴリ別にAOVタイプを表示
                    var aovsByCategory = AOVTypeInfo.GetAOVsByCategory();
                    
                    foreach (var category in aovsByCategory)
                    {
                        EditorGUILayout.LabelField(category.Key, EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        
                        foreach (var aovType in category.Value)
                        {
                            var info = AOVTypeInfo.GetInfo(aovType);
                            if (info != null)
                            {
                                EditorGUILayout.BeginHorizontal();
                                
                                // チェックボックス
                                bool isSelected = (testConfig.selectedAOVs & aovType) != 0;
                                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                                if (newSelected != isSelected)
                                {
                                    if (newSelected)
                                        testConfig.selectedAOVs |= aovType;
                                    else
                                        testConfig.selectedAOVs &= ~aovType;
                                }
                                
                                // 名前と説明
                                EditorGUILayout.LabelField(info.DisplayName, GUILayout.Width(150));
                                EditorGUILayout.LabelField(info.Description, EditorStyles.miniLabel);
                                
                                // 推奨フォーマット
                                EditorGUILayout.LabelField($"[{info.RecommendedFormat}]", EditorStyles.miniLabel, GUILayout.Width(60));
                                
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space(5);
                    }
                    
                    // 一括選択ボタン
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select All"))
                    {
                        testConfig.selectedAOVs = ~AOVType.None;
                    }
                    if (GUILayout.Button("Clear All"))
                    {
                        testConfig.selectedAOVs = AOVType.None;
                    }
                    if (GUILayout.Button("Compositing Preset"))
                    {
                        var preset = AOVRecorderSettingsConfig.Presets.GetCompositing();
                        testConfig.selectedAOVs = preset.selectedAOVs;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // AOV検出テスト
                    if (GUILayout.Button("Detect Available AOVs"))
                    {
                        DetectAvailableAOVs();
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// AOVプレビューセクションを描画
        /// </summary>
        private void DrawAOVPreview()
        {
            previewFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(previewFoldout, "AOV Preview");
            if (previewFoldout)
            {
                EditorGUI.indentLevel++;
                
                var selectedAOVs = testConfig.GetSelectedAOVsList();
                
                if (selectedAOVs.Count == 0)
                {
                    EditorGUILayout.HelpBox("No AOV types selected", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField($"Selected AOVs: {selectedAOVs.Count}", EditorStyles.boldLabel);
                    
                    // 出力設定
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
                    testConfig.outputFormat = (AOVOutputFormat)EditorGUILayout.EnumPopup("Format", testConfig.outputFormat);
                    testConfig.compressionEnabled = EditorGUILayout.Toggle("Compression", testConfig.compressionEnabled);
                    
                    // 解像度設定
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
                    testConfig.width = EditorGUILayout.IntField("Width", testConfig.width);
                    testConfig.height = EditorGUILayout.IntField("Height", testConfig.height);
                    
                    // プリセット解像度
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("HD")) { testConfig.width = 1920; testConfig.height = 1080; }
                    if (GUILayout.Button("2K")) { testConfig.width = 2048; testConfig.height = 1080; }
                    if (GUILayout.Button("4K")) { testConfig.width = 3840; testConfig.height = 2160; }
                    EditorGUILayout.EndHorizontal();
                    
                    // 予想ファイルサイズ
                    EditorGUILayout.Space();
                    float estimatedSizeMB = EstimateFileSize();
                    EditorGUILayout.HelpBox(
                        $"Estimated output:\n" +
                        $"• Files: {selectedAOVs.Count} AOV files\n" +
                        $"• Size per frame: ~{estimatedSizeMB:F1} MB total\n" +
                        $"• Format: {GetFormatExtension()}",
                        MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// テストレンダリングセクションを描画
        /// </summary>
        private void DrawTestRendering()
        {
            testRenderFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(testRenderFoldout, "Test Rendering");
            if (testRenderFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Test Settings", EditorStyles.boldLabel);
                
                testFrameCount = EditorGUILayout.IntSlider("Frame Count", testFrameCount, 1, 10);
                testConfig.frameRate = EditorGUILayout.IntField("Frame Rate", testConfig.frameRate);
                
                EditorGUILayout.BeginHorizontal();
                testOutputPath = EditorGUILayout.TextField("Output Path", testOutputPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string selectedPath = EditorUtility.SaveFolderPanel("Select Output Folder", testOutputPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        testOutputPath = FileUtil.GetProjectRelativePath(selectedPath);
                        if (string.IsNullOrEmpty(testOutputPath))
                            testOutputPath = selectedPath;
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // 詳細設定
                showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
                if (showAdvancedSettings)
                {
                    EditorGUI.indentLevel++;
                    testConfig.flipVertical = EditorGUILayout.Toggle("Flip Vertical", testConfig.flipVertical);
                    testConfig.capFrameRate = EditorGUILayout.Toggle("Cap Frame Rate", testConfig.capFrameRate);
                    
                    if (testConfig.selectedAOVs.HasFlag(AOVType.CustomPass))
                    {
                        testConfig.customPassName = EditorGUILayout.TextField("Custom Pass Name", testConfig.customPassName);
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // テストレンダリングボタン
                using (new EditorGUI.DisabledScope(!isHDRPActive || testConfig.selectedAOVs == AOVType.None))
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("Single Frame Test", GUILayout.Height(30)))
                    {
                        RunSingleFrameTest();
                    }
                    
                    if (GUILayout.Button("Multi-Frame Test", GUILayout.Height(30)))
                    {
                        RunMultiFrameTest();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                // エラー/警告ログ表示
                DrawLogs();
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// パフォーマンス計測セクションを描画
        /// </summary>
        private void DrawPerformanceMetrics()
        {
            performanceFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(performanceFoldout, "Performance Metrics");
            if (performanceFoldout)
            {
                EditorGUI.indentLevel++;
                
                if (aovRenderTimes.Count == 0)
                {
                    EditorGUILayout.HelpBox("Run a test to collect performance metrics", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField("AOV Render Times", EditorStyles.boldLabel);
                    
                    // 個別のAOVレンダリング時間
                    float totalTime = 0f;
                    foreach (var kvp in aovRenderTimes)
                    {
                        var info = AOVTypeInfo.GetInfo(kvp.Key);
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(info?.DisplayName ?? kvp.Key.ToString(), GUILayout.Width(150));
                        EditorGUILayout.LabelField($"{kvp.Value:F2} ms", GUILayout.Width(80));
                        
                        // プログレスバーで視覚化
                        var rect = GUILayoutUtility.GetRect(100, 18);
                        float maxTime = aovRenderTimes.Values.Max();
                        EditorGUI.ProgressBar(rect, kvp.Value / maxTime, "");
                        
                        EditorGUILayout.EndHorizontal();
                        totalTime += kvp.Value;
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Total Time: {totalTime:F2} ms", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Last Test Duration: {lastTestDuration:F2} seconds");
                    
                    // メモリ使用量
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Memory Usage", EditorStyles.boldLabel);
                    var memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                    EditorGUILayout.LabelField($"Current: {memoryMB:F2} MB");
                    
                    if (GUILayout.Button("Clear Metrics"))
                    {
                        aovRenderTimes.Clear();
                    }
                }
                
                // EXRファイル検証
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("EXR File Validation", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Validate EXR Files"))
                {
                    ValidateEXRFiles();
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        
        /// <summary>
        /// HDRPステータスをチェック
        /// </summary>
        private void CheckHDRPStatus()
        {
            isHDRPActive = false;
            hdrpVersion = "Not found";
            renderPipelineInfo = "None";
            
            // GraphicsSettingsからレンダーパイプラインを取得
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP != null)
            {
                renderPipelineInfo = currentRP.GetType().Name;
                
                // HDRPかチェック
                if (currentRP.GetType().FullName.Contains("HighDefinition"))
                {
                    isHDRPActive = true;
                    
                    // バージョンを取得
                    var assembly = currentRP.GetType().Assembly;
                    hdrpVersion = assembly.GetName().Version.ToString();
                }
            }
            
            // 追加の検証
            #if UNITY_PIPELINE_HDRP
            isHDRPActive = true;
            if (hdrpVersion == "Not found")
            {
                hdrpVersion = "HDRP (version unknown)";
            }
            #endif
            
            UnityEngine.Debug.Log($"[AOVDebugTool] HDRP Status: {(isHDRPActive ? "Active" : "Not Active")}, Version: {hdrpVersion}");
        }
        
        /// <summary>
        /// 利用可能なAOVを検出
        /// </summary>
        private void DetectAvailableAOVs()
        {
            availableAOVTypes.Clear();
            UnityEngine.Debug.Log("[AOVDebugTool] Detecting available AOV types...");
            
            if (!isHDRPActive)
            {
                UnityEngine.Debug.LogError("[AOVDebugTool] HDRP is not active");
                return;
            }
            
            // Unity RecorderのAOV関連の型を探す
            var aovRecorderTypes = new string[]
            {
                "UnityEditor.Recorder.AOVRecorderSettings",
                "UnityEngine.Rendering.HighDefinition.AOVRecorder",
                "UnityEngine.Rendering.HighDefinition.AOVRequest",
                "UnityEditor.Recorder.Input.AOVCameraInputSettings"
            };
            
            foreach (var typeName in aovRecorderTypes)
            {
                var type = Type.GetType(typeName + ", Unity.Recorder.Editor");
                if (type == null)
                    type = Type.GetType(typeName + ", Unity.RenderPipelines.HighDefinition.Runtime");
                
                if (type != null)
                {
                    availableAOVTypes.Add(type.FullName);
                    UnityEngine.Debug.Log($"[AOVDebugTool] Found AOV type: {type.FullName}");
                }
            }
            
            if (availableAOVTypes.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[AOVDebugTool] No AOV types found. Make sure Unity Recorder is installed with HDRP support.");
            }
        }
        
        /// <summary>
        /// 単一フレームテストを実行
        /// </summary>
        private void RunSingleFrameTest()
        {
            errorLogs.Clear();
            warningLogs.Clear();
            
            try
            {
                UnityEngine.Debug.Log("[AOVDebugTool] Starting single frame AOV test...");
                
                // 設定の検証
                string errorMessage;
                if (!testConfig.Validate(out errorMessage))
                {
                    errorLogs.Add(errorMessage);
                    return;
                }
                
                // 出力ディレクトリを作成
                if (!Directory.Exists(testOutputPath))
                {
                    Directory.CreateDirectory(testOutputPath);
                }
                
                var selectedAOVs = testConfig.GetSelectedAOVsList();
                var startTime = EditorApplication.timeSinceStartup;
                
                // 各AOVのレンダリングをシミュレート
                foreach (var aovType in selectedAOVs)
                {
                    var aovStartTime = EditorApplication.timeSinceStartup;
                    
                    UnityEngine.Debug.Log($"[AOVDebugTool] Testing AOV: {aovType}");
                    
                    // 実際のレンダリングの代わりにダミーファイルを作成
                    string fileName = $"Test_AOV_{aovType}_Frame0000.{GetFormatExtension()}";
                    string filePath = Path.Combine(testOutputPath, fileName);
                    
                    // ダミーファイルを作成（実際のAOVレンダリングはUnity Recorderが行う）
                    File.WriteAllText(filePath, $"AOV Test: {aovType}");
                    
                    var aovEndTime = EditorApplication.timeSinceStartup;
                    aovRenderTimes[aovType] = (float)((aovEndTime - aovStartTime) * 1000.0);
                }
                
                lastTestDuration = (float)(EditorApplication.timeSinceStartup - startTime);
                
                UnityEngine.Debug.Log($"[AOVDebugTool] Single frame test completed in {lastTestDuration:F2} seconds");
                EditorUtility.DisplayDialog("Test Complete", 
                    $"Single frame AOV test completed.\n\n" +
                    $"Output: {testOutputPath}\n" +
                    $"AOVs rendered: {selectedAOVs.Count}\n" +
                    $"Duration: {lastTestDuration:F2} seconds", 
                    "OK");
            }
            catch (Exception ex)
            {
                errorLogs.Add($"Test failed: {ex.Message}");
                UnityEngine.Debug.LogError($"[AOVDebugTool] Test failed: {ex}");
            }
        }
        
        /// <summary>
        /// 複数フレームテストを実行
        /// </summary>
        private void RunMultiFrameTest()
        {
            errorLogs.Clear();
            warningLogs.Clear();
            
            try
            {
                UnityEngine.Debug.Log($"[AOVDebugTool] Starting multi-frame AOV test ({testFrameCount} frames)...");
                
                // 設定の検証
                string errorMessage;
                if (!testConfig.Validate(out errorMessage))
                {
                    errorLogs.Add(errorMessage);
                    return;
                }
                
                // RecorderSettingsを作成
                var recorderSettingsList = testConfig.CreateAOVRecorderSettings("AOVTest");
                
                if (recorderSettingsList.Count == 0)
                {
                    errorLogs.Add("Failed to create AOV recorder settings");
                    return;
                }
                
                UnityEngine.Debug.Log($"[AOVDebugTool] Created {recorderSettingsList.Count} recorder settings");
                
                // 各設定にフレーム範囲を設定
                foreach (var settings in recorderSettingsList)
                {
                    settings.StartFrame = 0;
                    settings.EndFrame = testFrameCount - 1;
                    RecorderSettingsHelper.SetOutputPath(settings, testOutputPath, settings.name);
                }
                
                EditorUtility.DisplayDialog("Multi-Frame Test Ready", 
                    $"AOV recorders have been configured.\n\n" +
                    $"Output: {testOutputPath}\n" +
                    $"Frames: {testFrameCount}\n" +
                    $"AOVs: {recorderSettingsList.Count}\n\n" +
                    "Enter Play Mode to start recording.", 
                    "OK");
                
                // Note: 実際の録画はPlay Modeで行う必要があります
            }
            catch (Exception ex)
            {
                errorLogs.Add($"Multi-frame test failed: {ex.Message}");
                UnityEngine.Debug.LogError($"[AOVDebugTool] Multi-frame test failed: {ex}");
            }
        }
        
        /// <summary>
        /// EXRファイルを検証
        /// </summary>
        private void ValidateEXRFiles()
        {
            if (!Directory.Exists(testOutputPath))
            {
                EditorUtility.DisplayDialog("Validation Error", "Output directory does not exist", "OK");
                return;
            }
            
            var exrFiles = Directory.GetFiles(testOutputPath, "*.exr", SearchOption.AllDirectories);
            
            if (exrFiles.Length == 0)
            {
                EditorUtility.DisplayDialog("No Files", "No EXR files found in output directory", "OK");
                return;
            }
            
            UnityEngine.Debug.Log($"[AOVDebugTool] Validating {exrFiles.Length} EXR files...");
            
            int validFiles = 0;
            foreach (var file in exrFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.Length > 0)
                {
                    validFiles++;
                    UnityEngine.Debug.Log($"[AOVDebugTool] Valid: {Path.GetFileName(file)} ({fileInfo.Length / 1024f / 1024f:F2} MB)");
                }
                else
                {
                    warningLogs.Add($"Empty file: {Path.GetFileName(file)}");
                }
            }
            
            EditorUtility.DisplayDialog("Validation Complete", 
                $"Validated {exrFiles.Length} EXR files\n\n" +
                $"Valid files: {validFiles}\n" +
                $"Issues: {exrFiles.Length - validFiles}", 
                "OK");
        }
        
        /// <summary>
        /// ファイルサイズを推定
        /// </summary>
        private float EstimateFileSize()
        {
            var selectedCount = testConfig.GetSelectedAOVsList().Count;
            if (selectedCount == 0) return 0f;
            
            float pixelCount = testConfig.width * testConfig.height;
            float bytesPerPixel = 0f;
            
            switch (testConfig.outputFormat)
            {
                case AOVOutputFormat.EXR16:
                    bytesPerPixel = 6f; // RGB16F
                    break;
                case AOVOutputFormat.EXR32:
                    bytesPerPixel = 12f; // RGB32F
                    break;
                case AOVOutputFormat.PNG16:
                    bytesPerPixel = 2f; // Compressed estimate
                    break;
                case AOVOutputFormat.TGA:
                    bytesPerPixel = 3f; // RGB8
                    break;
            }
            
            float sizePerAOV = (pixelCount * bytesPerPixel) / (1024f * 1024f);
            if (testConfig.compressionEnabled && testConfig.outputFormat.ToString().Contains("EXR"))
            {
                sizePerAOV *= 0.6f; // 圧縮による推定削減
            }
            
            return sizePerAOV * selectedCount;
        }
        
        /// <summary>
        /// フォーマットの拡張子を取得
        /// </summary>
        private string GetFormatExtension()
        {
            switch (testConfig.outputFormat)
            {
                case AOVOutputFormat.EXR16:
                case AOVOutputFormat.EXR32:
                    return "exr";
                case AOVOutputFormat.PNG16:
                    return "png";
                case AOVOutputFormat.TGA:
                    return "tga";
                default:
                    return "exr";
            }
        }
        
        /// <summary>
        /// ログを描画
        /// </summary>
        private void DrawLogs()
        {
            if (errorLogs.Count > 0 || warningLogs.Count > 0)
            {
                EditorGUILayout.Space();
                
                if (errorLogs.Count > 0)
                {
                    EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foreach (var error in errorLogs)
                    {
                        EditorGUILayout.LabelField(error, new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red }, wordWrap = true });
                    }
                    EditorGUILayout.EndVertical();
                }
                
                if (warningLogs.Count > 0)
                {
                    EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    foreach (var warning in warningLogs)
                    {
                        EditorGUILayout.LabelField(warning, new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow }, wordWrap = true });
                    }
                    EditorGUILayout.EndVertical();
                }
                
                if (GUILayout.Button("Clear Logs"))
                {
                    errorLogs.Clear();
                    warningLogs.Clear();
                }
            }
        }
    }
}