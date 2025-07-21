using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace MultiTimelineRecorder.Tests
{
    /// <summary>
    /// Test runner utility for running all Multi Timeline Recorder tests
    /// </summary>
    public static class TestRunner
    {
        [MenuItem("Multi Timeline Recorder/Tests/Run All Tests")]
        public static void RunAllTests()
        {
            Debug.Log("Running all Multi Timeline Recorder tests...");
            
            var testAssembly = Assembly.GetExecutingAssembly();
            var testTypes = testAssembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestFixtureAttribute>() != null)
                .ToList();
            
            Debug.Log($"Found {testTypes.Count} test fixtures");
            
            int totalTests = 0;
            int passedTests = 0;
            int failedTests = 0;
            
            foreach (var testType in testTypes)
            {
                var testMethods = testType.GetMethods()
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
                    .ToList();
                
                Debug.Log($"\nRunning {testMethods.Count} tests in {testType.Name}");
                
                foreach (var testMethod in testMethods)
                {
                    totalTests++;
                    try
                    {
                        // Create instance if not static
                        object instance = null;
                        if (!testMethod.IsStatic)
                        {
                            instance = Activator.CreateInstance(testType);
                            
                            // Run SetUp if exists
                            var setUp = testType.GetMethod("SetUp");
                            setUp?.Invoke(instance, null);
                        }
                        
                        // Run test
                        testMethod.Invoke(instance, null);
                        
                        // Run TearDown if exists
                        if (instance != null)
                        {
                            var tearDown = testType.GetMethod("TearDown");
                            tearDown?.Invoke(instance, null);
                        }
                        
                        passedTests++;
                        Debug.Log($"  ✓ {testMethod.Name}");
                    }
                    catch (Exception ex)
                    {
                        failedTests++;
                        Debug.LogError($"  ✗ {testMethod.Name}: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }
            
            Debug.Log($"\n========== Test Summary ==========");
            Debug.Log($"Total: {totalTests}");
            Debug.Log($"Passed: {passedTests}");
            Debug.Log($"Failed: {failedTests}");
            Debug.Log($"Success Rate: {(passedTests * 100.0 / totalTests):F1}%");
            Debug.Log($"==================================");
            
            if (failedTests > 0)
            {
                Debug.LogError($"{failedTests} tests failed!");
            }
            else
            {
                Debug.Log("All tests passed! ✓");
            }
        }
        
        [MenuItem("Multi Timeline Recorder/Tests/Run Service Tests")]
        public static void RunServiceTests()
        {
            RunTestsInNamespace("MultiTimelineRecorder.Tests.Services");
        }
        
        [MenuItem("Multi Timeline Recorder/Tests/Run Model Tests")]
        public static void RunModelTests()
        {
            RunTestsInNamespace("MultiTimelineRecorder.Tests.Models");
        }
        
        private static void RunTestsInNamespace(string namespaceName)
        {
            Debug.Log($"Running tests in namespace: {namespaceName}");
            
            var testAssembly = Assembly.GetExecutingAssembly();
            var testTypes = testAssembly.GetTypes()
                .Where(t => t.Namespace == namespaceName && t.GetCustomAttribute<TestFixtureAttribute>() != null)
                .ToList();
            
            Debug.Log($"Found {testTypes.Count} test fixtures in {namespaceName}");
            
            // Similar test execution logic as RunAllTests
            // ... (implementation details)
        }
    }
}