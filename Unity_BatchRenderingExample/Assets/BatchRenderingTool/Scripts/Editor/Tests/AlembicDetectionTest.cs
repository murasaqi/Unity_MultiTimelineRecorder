using UnityEngine;
using UnityEditor;

namespace BatchRenderingTool.Tests
{
    public static class AlembicDetectionTest
    {
        [MenuItem("Window/Batch Rendering Tool/Test/Test Alembic Detection")]
        public static void TestAlembicDetection()
        {
            UnityEngine.Debug.Log("=== Alembic Detection Test ===");
            
            bool isAvailable = AlembicExportInfo.IsAlembicPackageAvailable();
            UnityEngine.Debug.Log($"[AlembicDetectionTest] IsAlembicPackageAvailable: {isAvailable}");
            
            // Also test individual types
            var testTypes = new string[]
            {
                "UnityEditor.Recorder.AlembicRecorderSettings, Unity.Recorder.Editor",
                "UnityEngine.Formats.Alembic.Importer.AlembicStreamPlayer, Unity.Formats.Alembic.Runtime",
                "UnityEditor.Formats.Alembic.Recorder.AlembicRecorderSettings, Unity.Formats.Alembic.Editor",
                "UnityEditor.Recorder.Formats.Alembic.AlembicRecorderSettings, Unity.Recorder.Editor",
                "UnityEditor.Formats.Alembic.Exporter.AlembicExporter, Unity.Formats.Alembic.Editor",
                "AlembicRecorderSettings, Unity.Formats.Alembic.Editor"
            };
            
            UnityEngine.Debug.Log("\n[AlembicDetectionTest] Testing individual types:");
            foreach (var typeName in testTypes)
            {
                var type = System.Type.GetType(typeName);
                UnityEngine.Debug.Log($"  {typeName}: {(type != null ? "Found" : "Not found")}");
            }
            
            UnityEngine.Debug.Log("\n[AlembicDetectionTest] Checking assemblies for Alembic:");
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Alembic"))
                {
                    UnityEngine.Debug.Log($"  Found assembly: {assembly.FullName}");
                }
            }
            
            UnityEngine.Debug.Log("\n=== Test Complete ===");
        }
    }
}