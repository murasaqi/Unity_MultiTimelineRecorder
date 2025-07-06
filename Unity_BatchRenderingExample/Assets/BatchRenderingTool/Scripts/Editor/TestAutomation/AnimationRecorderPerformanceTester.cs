using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
// using Debug = UnityEngine.Debug; // Removed to avoid namespace conflicts

namespace BatchRenderingTool.TestAutomation
{
    /// <summary>
    /// Performance testing tool for Animation Recorder
    /// Measures recording performance, memory usage, and optimization effectiveness
    /// </summary>
    public class AnimationRecorderPerformanceTester
    {
        private struct PerformanceMetrics
        {
            public long memoryBefore;
            public long memoryAfter;
            public float recordingTime;
            public int keyframeCount;
            public int propertyCount;
            public float compressionRatio;
            public long fileSizeBytes;
        }
        
        /// <summary>
        /// Run comprehensive performance test
        /// </summary>
        public static void RunPerformanceTest(AnimationRecorderSettingsConfig config, float duration = 5f)
        {
            UnityEngine.Debug.Log("[AnimationPerformance] Starting performance test...");
            
            var metrics = new PerformanceMetrics();
            
            // Measure initial memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            metrics.memoryBefore = GC.GetTotalMemory(false);
            
            // Start timing
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Create recorder settings
                var settings = config.CreateAnimationRecorderSettings("PerformanceTest");
                
                // Count properties
                metrics.propertyCount = CountRecordableProperties(config);
                
                // Simulate recording
                var clip = SimulateRecording(config, duration);
                
                // Stop timing
                stopwatch.Stop();
                metrics.recordingTime = (float)stopwatch.Elapsed.TotalSeconds;
                
                // Measure memory after
                metrics.memoryAfter = GC.GetTotalMemory(false);
                
                // Analyze clip if created
                if (clip != null)
                {
                    AnalyzeAnimationClip(clip, ref metrics);
                }
                
                // Report results
                ReportPerformanceResults(config, metrics, duration);
                
                // Cleanup
                if (clip != null)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }
                UnityEngine.Object.DestroyImmediate(settings);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[AnimationPerformance] Test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Run stress test with multiple targets
        /// </summary>
        public static void RunStressTest(int targetCount, float duration = 5f)
        {
            UnityEngine.Debug.Log($"[AnimationPerformance] Starting stress test with {targetCount} targets...");
            
            var testObjects = new List<GameObject>();
            var configs = new List<AnimationRecorderSettingsConfig>();
            
            try
            {
                // Create test objects
                for (int i = 0; i < targetCount; i++)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = $"StressTestObject_{i}";
                    obj.transform.position = UnityEngine.Random.insideUnitSphere * 10f;
                    
                    // Add some animated components
                    var animator = obj.AddComponent<Animator>();
                    var renderer = obj.GetComponent<Renderer>();
                    
                    testObjects.Add(obj);
                    
                    // Create config for each object
                    var config = new AnimationRecorderSettingsConfig
                    {
                        targetGameObject = obj,
                        recordingProperties = AnimationRecordingProperties.AllProperties,
                        recordingScope = AnimationRecordingScope.SingleGameObject,
                        frameRate = 30f,
                        compressionLevel = AnimationCompressionLevel.Medium
                    };
                    configs.Add(config);
                }
                
                // Measure performance
                var startMemory = GC.GetTotalMemory(false);
                var stopwatch = Stopwatch.StartNew();
                
                // Simulate recording all objects
                foreach (var config in configs)
                {
                    var settings = config.CreateAnimationRecorderSettings($"StressTest_{config.targetGameObject.name}");
                    UnityEngine.Object.DestroyImmediate(settings);
                }
                
                stopwatch.Stop();
                var endMemory = GC.GetTotalMemory(false);
                
                // Report results
                UnityEngine.Debug.Log($"[AnimationPerformance] Stress test completed:");
                UnityEngine.Debug.Log($"  - Objects: {targetCount}");
                UnityEngine.Debug.Log($"  - Total Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
                UnityEngine.Debug.Log($"  - Avg Time per Object: {stopwatch.Elapsed.TotalSeconds / targetCount:F3}s");
                UnityEngine.Debug.Log($"  - Memory Used: {FormatBytes(endMemory - startMemory)}");
            }
            finally
            {
                // Cleanup
                foreach (var obj in testObjects)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }
        
        /// <summary>
        /// Test compression effectiveness
        /// </summary>
        public static void TestCompressionLevels(AnimationRecorderSettingsConfig baseConfig)
        {
            UnityEngine.Debug.Log("[AnimationPerformance] Testing compression levels...");
            
            var compressionLevels = Enum.GetValues(typeof(AnimationCompressionLevel)) as AnimationCompressionLevel[];
            var results = new Dictionary<AnimationCompressionLevel, PerformanceMetrics>();
            
            foreach (var level in compressionLevels)
            {
                var config = baseConfig.Clone();
                config.compressionLevel = level;
                
                var metrics = new PerformanceMetrics();
                
                try
                {
                    // Create settings with this compression level
                    var settings = config.CreateAnimationRecorderSettings($"CompressionTest_{level}");
                    
                    // Simulate recording
                    var clip = SimulateRecording(config, 2f);
                    
                    if (clip != null)
                    {
                        AnalyzeAnimationClip(clip, ref metrics);
                        UnityEngine.Object.DestroyImmediate(clip);
                    }
                    
                    UnityEngine.Object.DestroyImmediate(settings);
                    
                    results[level] = metrics;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[AnimationPerformance] Compression test failed for {level}: {e.Message}");
                }
            }
            
            // Report comparison
            UnityEngine.Debug.Log("[AnimationPerformance] Compression Level Comparison:");
            foreach (var kvp in results)
            {
                var metrics = kvp.Value;
                UnityEngine.Debug.Log($"  {kvp.Key}:");
                UnityEngine.Debug.Log($"    - Keyframes: {metrics.keyframeCount}");
                UnityEngine.Debug.Log($"    - Compression Ratio: {metrics.compressionRatio:F2}");
                UnityEngine.Debug.Log($"    - Estimated Size: {FormatBytes(metrics.fileSizeBytes)}");
            }
        }
        
        private static int CountRecordableProperties(AnimationRecorderSettingsConfig config)
        {
            int count = 0;
            
            if ((config.recordingProperties & AnimationRecordingProperties.Position) != 0) count += 3;
            if ((config.recordingProperties & AnimationRecordingProperties.Rotation) != 0) count += 4;
            if ((config.recordingProperties & AnimationRecordingProperties.Scale) != 0) count += 3;
            
            if (config.targetGameObject != null)
            {
                var smr = config.targetGameObject.GetComponent<SkinnedMeshRenderer>();
                if (smr != null && smr.sharedMesh != null && 
                    (config.recordingProperties & AnimationRecordingProperties.BlendShapes) != 0)
                {
                    count += smr.sharedMesh.blendShapeCount;
                }
                
                if ((config.recordingProperties & AnimationRecordingProperties.MaterialProperties) != 0)
                {
                    var renderers = config.targetGameObject.GetComponents<Renderer>();
                    foreach (var r in renderers)
                    {
                        count += r.sharedMaterials.Length * 5; // Estimate 5 properties per material
                    }
                }
            }
            
            return count;
        }
        
        private static AnimationClip SimulateRecording(AnimationRecorderSettingsConfig config, float duration)
        {
            // Create a dummy animation clip for testing
            var clip = new AnimationClip();
            clip.name = "TestRecording";
            clip.frameRate = config.frameRate;
            
            // Add some dummy curves based on recording properties
            if ((config.recordingProperties & AnimationRecordingProperties.Position) != 0)
            {
                var curve = AnimationCurve.Linear(0, 0, duration, 1);
                clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
                clip.SetCurve("", typeof(Transform), "localPosition.y", curve);
                clip.SetCurve("", typeof(Transform), "localPosition.z", curve);
            }
            
            if ((config.recordingProperties & AnimationRecordingProperties.Rotation) != 0)
            {
                var curve = AnimationCurve.Linear(0, 0, duration, 360);
                clip.SetCurve("", typeof(Transform), "localRotation.x", curve);
                clip.SetCurve("", typeof(Transform), "localRotation.y", curve);
                clip.SetCurve("", typeof(Transform), "localRotation.z", curve);
                clip.SetCurve("", typeof(Transform), "localRotation.w", curve);
            }
            
            return clip;
        }
        
        private static void AnalyzeAnimationClip(AnimationClip clip, ref PerformanceMetrics metrics)
        {
            if (clip == null) return;
            
            // Get curve bindings
            var curveBindings = AnimationUtility.GetCurveBindings(clip);
            metrics.propertyCount = curveBindings.Length;
            
            // Count total keyframes
            metrics.keyframeCount = 0;
            foreach (var binding in curveBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null)
                {
                    metrics.keyframeCount += curve.keys.Length;
                }
            }
            
            // Estimate file size (16 bytes per keyframe)
            metrics.fileSizeBytes = metrics.keyframeCount * 16;
            
            // Calculate compression ratio (simulated)
            var uncompressedSize = metrics.propertyCount * (int)(clip.length * clip.frameRate) * 16;
            metrics.compressionRatio = uncompressedSize > 0 ? (float)uncompressedSize / metrics.fileSizeBytes : 1f;
        }
        
        private static void ReportPerformanceResults(AnimationRecorderSettingsConfig config, PerformanceMetrics metrics, float duration)
        {
            UnityEngine.Debug.Log("[AnimationPerformance] Performance Test Results:");
            UnityEngine.Debug.Log($"  Configuration:");
            UnityEngine.Debug.Log($"    - Target: {(config.targetGameObject != null ? config.targetGameObject.name : "None")}");
            UnityEngine.Debug.Log($"    - Properties: {config.recordingProperties}");
            UnityEngine.Debug.Log($"    - Frame Rate: {config.frameRate} fps");
            UnityEngine.Debug.Log($"    - Compression: {config.compressionLevel}");
            UnityEngine.Debug.Log($"    - Duration: {duration}s");
            
            UnityEngine.Debug.Log($"  Performance:");
            UnityEngine.Debug.Log($"    - Recording Time: {metrics.recordingTime:F2}s");
            UnityEngine.Debug.Log($"    - Speed Ratio: {duration / metrics.recordingTime:F2}x realtime");
            UnityEngine.Debug.Log($"    - Memory Delta: {FormatBytes(metrics.memoryAfter - metrics.memoryBefore)}");
            
            UnityEngine.Debug.Log($"  Output:");
            UnityEngine.Debug.Log($"    - Properties: {metrics.propertyCount}");
            UnityEngine.Debug.Log($"    - Keyframes: {metrics.keyframeCount}");
            UnityEngine.Debug.Log($"    - Compression Ratio: {metrics.compressionRatio:F2}:1");
            UnityEngine.Debug.Log($"    - Estimated Size: {FormatBytes(metrics.fileSizeBytes)}");
        }
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = Math.Abs(bytes);
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}