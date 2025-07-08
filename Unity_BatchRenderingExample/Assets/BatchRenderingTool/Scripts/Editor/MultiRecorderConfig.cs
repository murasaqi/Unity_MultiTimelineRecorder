using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

namespace BatchRenderingTool
{
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
                    imageFormat = this.imageFormat,
                    imageQuality = this.imageQuality,
                    width = this.width,
                    height = this.height,
                    frameRate = this.frameRate,
                    capFrameRate = this.capFrameRate
                };
                
                // 各設定のクローン
                clone.movieConfig = this.movieConfig.Clone();
                clone.aovConfig = this.aovConfig.Clone();
                clone.alembicConfig = this.alembicConfig.Clone();
                clone.animationConfig = this.animationConfig.Clone();
                clone.fbxConfig = this.fbxConfig.Clone();
                
                return clone;
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
        public int globalWidth = 1920;
        public int globalHeight = 1080;
        public bool useGlobalFrameRate = true;
        public int globalFrameRate = 24;
        
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
                    item.fileName = "Recordings/<Scene>_<Take>/image_<Frame>";
                    break;
                    
                case RecorderSettingsType.Movie:
                    item.name = "Movie";
                    item.fileName = "Recordings/<Scene>_<Take>";
                    break;
                    
                case RecorderSettingsType.Animation:
                    item.name = "Animation";
                    item.fileName = "Assets/Animations/<Scene>_<Take>";
                    break;
                    
                case RecorderSettingsType.Alembic:
                    item.name = "Alembic";
                    item.fileName = "Recordings/<Scene>_<Take>/<Scene>";
                    break;
                    
                case RecorderSettingsType.AOV:
                    item.name = "AOV";
                    item.fileName = "Recordings/<Scene>_<Take>/AOV/<AOVType>_<Frame>";
                    break;
                    
                case RecorderSettingsType.FBX:
                    item.name = "FBX Animation";
                    item.fileName = "Assets/FBX/<Scene>_<Take>";
                    break;
            }
            
            return item;
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
                beautyItem.fileName = "Recordings/<Scene>_<Take>/Beauty/beauty_<Frame>";
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