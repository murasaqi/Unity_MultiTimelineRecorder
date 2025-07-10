using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
    /// <summary>
    /// Take番号の管理モード
    /// </summary>
    public enum RecorderTakeMode
    {
        /// <summary>
        /// RecordersカラムのTimeline固有のTake番号を使用
        /// </summary>
        RecordersTake,
        
        /// <summary>
        /// 各ClipごとのTake番号を使用（従来の動作）
        /// </summary>
        ClipTake
    }
    
    /// <summary>
    /// 複数のレコーダー設定を管理するためのコンフィグクラス
    /// </summary>
    [Serializable]
    public class MultiRecorderConfig
    {
        /// <summary>
        /// 個別のレコーダー設定項目
        /// </summary>
        [Serializable]
        public class RecorderConfigItem
        {
            public string name = "New Recorder";
            public bool enabled = true;
            public RecorderSettingsType recorderType = RecorderSettingsType.Image;
            
            // 各レコーダータイプ固有の設定
            public string fileName = "Recording_<Take>";
            public int takeNumber = 1;
            public RecorderTakeMode takeMode = RecorderTakeMode.ClipTake;
            
            // Output path settings
            public OutputPathSettings outputPath = new OutputPathSettings() { pathMode = RecorderPathMode.UseGlobal };
            
            // Image Recorder
            public ImageRecorderSettings.ImageRecorderOutputFormat imageFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
            public int imageQuality = 75;
            public bool captureAlpha = false;
            public int jpegQuality = 75;
            public CompressionUtility.EXRCompressionType exrCompression = CompressionUtility.EXRCompressionType.None;
            
            // Movie Recorder
            public MovieRecorderSettingsConfig movieConfig = new MovieRecorderSettingsConfig();
            
            // AOV Recorder
            public AOVRecorderSettingsConfig aovConfig = new AOVRecorderSettingsConfig();
            
            // Alembic Recorder
            public AlembicRecorderSettingsConfig alembicConfig = new AlembicRecorderSettingsConfig();
            
            // Animation Recorder
            public AnimationRecorderSettingsConfig animationConfig = new AnimationRecorderSettingsConfig();
            
            // FBX Recorder
            public FBXRecorderSettingsConfig fbxConfig = new FBXRecorderSettingsConfig();
            
            // 共通設定
            public int width = 1920;
            public int height = 1080;
            public int frameRate = 24;
            public bool capFrameRate = true;
            
            /// <summary>
            /// 設定をクローン
            /// </summary>
            public RecorderConfigItem Clone()
            {
                var clone = new RecorderConfigItem
                {
                    name = this.name,
                    enabled = this.enabled,
                    recorderType = this.recorderType,
                    fileName = this.fileName,
                    takeNumber = this.takeNumber,
                    takeMode = this.takeMode,
                    imageFormat = this.imageFormat,
                    imageQuality = this.imageQuality,
                    captureAlpha = this.captureAlpha,
                    jpegQuality = this.jpegQuality,
                    exrCompression = this.exrCompression,
                    width = this.width,
                    height = this.height,
                    frameRate = this.frameRate,
                    capFrameRate = this.capFrameRate
                };
                
                // 各設定のクローン
                clone.outputPath = this.outputPath?.Clone();
                clone.movieConfig = this.movieConfig?.Clone();
                clone.aovConfig = this.aovConfig?.Clone();
                clone.alembicConfig = this.alembicConfig?.Clone();
                clone.animationConfig = this.animationConfig?.Clone();
                clone.fbxConfig = this.fbxConfig?.Clone();
                
                return clone;
            }
            
            /// <summary>
            /// DeepCopyメソッド（エイリアス）
            /// </summary>
            public RecorderConfigItem DeepCopy()
            {
                return Clone();
            }
            
            /// <summary>
            /// 設定の検証
            /// </summary>
            public bool Validate(out string errorMessage)
            {
                if (string.IsNullOrEmpty(name))
                {
                    errorMessage = "Recorder name cannot be empty";
                    return false;
                }
                
                if (string.IsNullOrEmpty(fileName))
                {
                    errorMessage = "File name cannot be empty";
                    return false;
                }
                
                if (width <= 0 || height <= 0)
                {
                    errorMessage = "Invalid resolution";
                    return false;
                }
                
                if (frameRate <= 0 || frameRate > 120)
                {
                    errorMessage = "Frame rate must be between 1 and 120";
                    return false;
                }
                
                // レコーダータイプ固有の検証
                switch (recorderType)
                {
                    case RecorderSettingsType.Movie:
                        return movieConfig.Validate(out errorMessage);
                        
                    case RecorderSettingsType.AOV:
                        return aovConfig.Validate(out errorMessage);
                        
                    case RecorderSettingsType.Alembic:
                        return alembicConfig.Validate(out errorMessage);
                        
                    case RecorderSettingsType.Animation:
                        return animationConfig.Validate(out errorMessage);
                        
                    case RecorderSettingsType.FBX:
                        return fbxConfig.Validate(out errorMessage);
                        
                    default:
                        errorMessage = null;
                        return true;
                }
            }
        }
        
        /// <summary>
        /// レコーダー設定のリスト
        /// </summary>
        [SerializeField]
        private List<RecorderConfigItem> recorderItems = new List<RecorderConfigItem>();
        
        /// <summary>
        /// グローバル設定（全レコーダー共通）
        /// </summary>
        public string globalOutputPath = "Recordings";
        public bool useGlobalResolution = true;
        
        /// <summary>
        /// レコーダー設定のリストを取得
        /// </summary>
        public List<RecorderConfigItem> RecorderItems => recorderItems;
        
        /// <summary>
        /// 有効なレコーダー設定のみを取得
        /// </summary>
        public List<RecorderConfigItem> GetEnabledRecorders()
        {
            return recorderItems.FindAll(item => item.enabled);
        }
        
        /// <summary>
        /// レコーダー設定を追加
        /// </summary>
        public void AddRecorder(RecorderConfigItem item)
        {
            if (item != null)
            {
                recorderItems.Add(item);
            }
        }
        
        /// <summary>
        /// レコーダー設定を削除
        /// </summary>
        public void RemoveRecorder(int index)
        {
            if (index >= 0 && index < recorderItems.Count)
            {
                recorderItems.RemoveAt(index);
            }
        }
        
        /// <summary>
        /// レコーダーの順序を変更
        /// </summary>
        public void MoveRecorder(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < recorderItems.Count &&
                toIndex >= 0 && toIndex < recorderItems.Count &&
                fromIndex != toIndex)
            {
                var item = recorderItems[fromIndex];
                recorderItems.RemoveAt(fromIndex);
                recorderItems.Insert(toIndex, item);
            }
        }
        
        /// <summary>
        /// デフォルトのレコーダー設定を作成
        /// </summary>
        public static RecorderConfigItem CreateDefaultRecorder(RecorderSettingsType type)
        {
            var item = new RecorderConfigItem
            {
                recorderType = type,
                enabled = true
            };
            
            // タイプ別のデフォルト設定
            switch (type)
            {
                case RecorderSettingsType.Image:
                    item.name = "Image Sequence";
                    item.fileName = "<Scene>_<Take>_image_<Frame>";
                    break;
                    
                case RecorderSettingsType.Movie:
                    item.name = "Movie";
                    item.fileName = "<Scene>_<Take>";
                    // Movie configを初期化
                    item.movieConfig = new MovieRecorderSettingsConfig();
                    break;
                    
                case RecorderSettingsType.Animation:
                    item.name = "Animation";
                    item.fileName = "<Scene>_<Take>_animation";
                    // Animation configを初期化
                    item.animationConfig = new AnimationRecorderSettingsConfig();
                    break;
                    
                case RecorderSettingsType.Alembic:
                    item.name = "Alembic";
                    item.fileName = "<Scene>_<Take>_alembic";
                    // Alembic configを初期化
                    item.alembicConfig = new AlembicRecorderSettingsConfig();
                    break;
                    
                case RecorderSettingsType.AOV:
                    item.name = "AOV";
                    item.fileName = "<Scene>_<Take>_<AOVType>_<Frame>";
                    // AOV configを初期化
                    item.aovConfig = new AOVRecorderSettingsConfig();
                    break;
                    
                case RecorderSettingsType.FBX:
                    item.name = "FBX Animation";
                    item.fileName = "<Scene>_<Take>_fbx";
                    // FBX configを初期化して、GameObject参照が保持されるようにする
                    item.fbxConfig = new FBXRecorderSettingsConfig();
                    break;
            }
            
            return item;
        }
        
        /// <summary>
        /// レコーダー設定項目をクローン
        /// </summary>
        public static RecorderConfigItem CloneRecorderItem(RecorderConfigItem source)
        {
            var clone = new RecorderConfigItem
            {
                name = source.name,
                enabled = source.enabled,
                recorderType = source.recorderType,
                fileName = source.fileName,
                takeNumber = source.takeNumber,
                
                // Image settings
                imageFormat = source.imageFormat,
                imageQuality = source.imageQuality,
                captureAlpha = source.captureAlpha,
                jpegQuality = source.jpegQuality,
                exrCompression = source.exrCompression,
                
                // Resolution
                width = source.width,
                height = source.height
            };
            
            // Clone config objects
            if (source.movieConfig != null)
            {
                clone.movieConfig = new MovieRecorderSettingsConfig
                {
                    outputFormat = source.movieConfig.outputFormat,
                    videoBitrateMode = source.movieConfig.videoBitrateMode,
                    captureAudio = source.movieConfig.captureAudio,
                    captureAlpha = source.movieConfig.captureAlpha,
                    customBitrate = source.movieConfig.customBitrate,
                    audioBitrate = source.movieConfig.audioBitrate
                };
            }
            
            if (source.aovConfig != null)
            {
                clone.aovConfig = new AOVRecorderSettingsConfig
                {
                    selectedAOVs = source.aovConfig.selectedAOVs,
                    outputFormat = source.aovConfig.outputFormat,
                    useMultiPartEXR = source.aovConfig.useMultiPartEXR,
                    colorSpace = source.aovConfig.colorSpace,
                    compression = source.aovConfig.compression
                };
            }
            
            if (source.alembicConfig != null)
            {
                clone.alembicConfig = new AlembicRecorderSettingsConfig
                {
                    exportTargets = source.alembicConfig.exportTargets,
                    exportScope = source.alembicConfig.exportScope,
                    handedness = source.alembicConfig.handedness,
                    scaleFactor = source.alembicConfig.scaleFactor,
                    frameRate = source.alembicConfig.frameRate,
                    timeSamplingType = source.alembicConfig.timeSamplingType,
                    includeChildren = source.alembicConfig.includeChildren,
                    flattenHierarchy = source.alembicConfig.flattenHierarchy
                };
                // GameObject参照もコピー
                clone.alembicConfig.targetGameObject = source.alembicConfig.targetGameObject;
                clone.alembicConfig.customSelection = new List<GameObject>(source.alembicConfig.customSelection);
            }
            
            if (source.animationConfig != null)
            {
                clone.animationConfig = new AnimationRecorderSettingsConfig
                {
                    recordingProperties = source.animationConfig.recordingProperties,
                    recordingScope = source.animationConfig.recordingScope,
                    interpolationMode = source.animationConfig.interpolationMode,
                    compressionLevel = source.animationConfig.compressionLevel,
                    includeChildren = source.animationConfig.includeChildren,
                    clampedTangents = source.animationConfig.clampedTangents,
                    recordBlendShapes = source.animationConfig.recordBlendShapes
                };
                // GameObject参照もコピー
                clone.animationConfig.targetGameObject = source.animationConfig.targetGameObject;
                clone.animationConfig.customSelection = new List<GameObject>(source.animationConfig.customSelection);
            }
            
            if (source.fbxConfig != null)
            {
                clone.fbxConfig = new FBXRecorderSettingsConfig
                {
                    recordedComponent = source.fbxConfig.recordedComponent,
                    recordHierarchy = source.fbxConfig.recordHierarchy,
                    clampedTangents = source.fbxConfig.clampedTangents,
                    animationCompression = source.fbxConfig.animationCompression,
                    exportGeometry = source.fbxConfig.exportGeometry
                };
                // GameObject参照もコピー
                clone.fbxConfig.targetGameObject = source.fbxConfig.targetGameObject;
                clone.fbxConfig.transferAnimationSource = source.fbxConfig.transferAnimationSource;
                clone.fbxConfig.transferAnimationDest = source.fbxConfig.transferAnimationDest;
            }
            
            return clone;
        }
        
        /// <summary>
        /// プリセット設定を作成
        /// </summary>
        public static class Presets
        {
            /// <summary>
            /// 基本的な動画とイメージシーケンス
            /// </summary>
            public static MultiRecorderConfig CreateBasicPreset()
            {
                var config = new MultiRecorderConfig();
                
                // Movie Recorder
                var movieItem = CreateDefaultRecorder(RecorderSettingsType.Movie);
                movieItem.movieConfig = MovieRecorderSettingsConfig.GetPreset(MovieRecorderPreset.HighQuality1080p);
                config.AddRecorder(movieItem);
                
                // Image Sequence
                var imageItem = CreateDefaultRecorder(RecorderSettingsType.Image);
                imageItem.imageFormat = ImageRecorderSettings.ImageRecorderOutputFormat.PNG;
                config.AddRecorder(imageItem);
                
                return config;
            }
            
            /// <summary>
            /// アニメーション制作向け
            /// </summary>
            public static MultiRecorderConfig CreateAnimationPreset()
            {
                var config = new MultiRecorderConfig();
                
                // Animation Clip
                var animItem = CreateDefaultRecorder(RecorderSettingsType.Animation);
                config.AddRecorder(animItem);
                
                // Alembic Export
                var alembicItem = CreateDefaultRecorder(RecorderSettingsType.Alembic);
                config.AddRecorder(alembicItem);
                
                // Preview Movie
                var movieItem = CreateDefaultRecorder(RecorderSettingsType.Movie);
                movieItem.name = "Preview Movie";
                movieItem.movieConfig = MovieRecorderSettingsConfig.GetPreset(MovieRecorderPreset.HighQuality1080p);
                config.AddRecorder(movieItem);
                
                return config;
            }
            
            /// <summary>
            /// コンポジット向け
            /// </summary>
            public static MultiRecorderConfig CreateCompositingPreset()
            {
                var config = new MultiRecorderConfig();
                
                // Beauty Pass (EXR)
                var beautyItem = CreateDefaultRecorder(RecorderSettingsType.Image);
                beautyItem.name = "Beauty Pass";
                beautyItem.imageFormat = ImageRecorderSettings.ImageRecorderOutputFormat.EXR;
                beautyItem.fileName = "<Scene>_<Take>_beauty_<Frame>";
                config.AddRecorder(beautyItem);
                
                // AOV Passes
                var aovItem = CreateDefaultRecorder(RecorderSettingsType.AOV);
                aovItem.aovConfig = AOVRecorderSettingsConfig.Presets.GetCompositing();
                config.AddRecorder(aovItem);
                
                // Alembic Geometry
                var alembicItem = CreateDefaultRecorder(RecorderSettingsType.Alembic);
                alembicItem.name = "Geometry Cache";
                config.AddRecorder(alembicItem);
                
                return config;
            }
        }
    }
}