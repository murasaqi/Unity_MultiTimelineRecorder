using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Unity.MultiTimelineRecorder;
using MultiTimelineRecorder.Core.Models;
using MultiTimelineRecorder.Core.Interfaces;

namespace MultiTimelineRecorder.Tests.Editor
{
    [TestFixture]
    public class WildcardRegistryTests
    {
        private WildcardRegistry _registry;
        private TestLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _registry = new WildcardRegistry(_logger);
        }

        [Test]
        public void RegisterWildcard_WithValidWildcard_AddsToRegistry()
        {
            // Arrange
            var wildcard = new WildcardDefinition
            {
                Key = "<CustomWildcard>",
                Description = "Test wildcard",
                Resolver = context => "TestValue"
            };

            // Act
            _registry.RegisterWildcard(wildcard);

            // Assert
            Assert.IsTrue(_registry.ContainsWildcard("<CustomWildcard>"));
            Assert.AreEqual("TestValue", _registry.ResolveWildcard("<CustomWildcard>", null));
        }

        [Test]
        public void RegisterWildcard_WithDuplicateKey_OverridesExisting()
        {
            // Arrange
            var wildcard1 = new WildcardDefinition
            {
                Key = "<TestKey>",
                Description = "First wildcard",
                Resolver = context => "Value1"
            };
            
            var wildcard2 = new WildcardDefinition
            {
                Key = "<TestKey>",
                Description = "Second wildcard",
                Resolver = context => "Value2"
            };

            // Act
            _registry.RegisterWildcard(wildcard1);
            _registry.RegisterWildcard(wildcard2);

            // Assert
            Assert.AreEqual("Value2", _registry.ResolveWildcard("<TestKey>", null));
            Assert.IsTrue(_logger.HasWarning("Overriding existing wildcard"));
        }

        [Test]
        public void RegisterWildcards_WithMultipleWildcards_AddsAll()
        {
            // Arrange
            var wildcards = new List<WildcardDefinition>
            {
                new WildcardDefinition { Key = "<Wild1>", Resolver = _ => "Value1" },
                new WildcardDefinition { Key = "<Wild2>", Resolver = _ => "Value2" },
                new WildcardDefinition { Key = "<Wild3>", Resolver = _ => "Value3" }
            };

            // Act
            _registry.RegisterWildcards(wildcards);

            // Assert
            Assert.IsTrue(_registry.ContainsWildcard("<Wild1>"));
            Assert.IsTrue(_registry.ContainsWildcard("<Wild2>"));
            Assert.IsTrue(_registry.ContainsWildcard("<Wild3>"));
        }

        [Test]
        public void UnregisterWildcard_WithExistingKey_RemovesFromRegistry()
        {
            // Arrange
            var wildcard = new WildcardDefinition
            {
                Key = "<ToRemove>",
                Resolver = _ => "Value"
            };
            _registry.RegisterWildcard(wildcard);

            // Act
            var result = _registry.UnregisterWildcard("<ToRemove>");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(_registry.ContainsWildcard("<ToRemove>"));
        }

        [Test]
        public void UnregisterWildcard_WithNonExistentKey_ReturnsFalse()
        {
            // Act
            var result = _registry.UnregisterWildcard("<NonExistent>");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetAllWildcards_ReturnsAllRegistered()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition { Key = "<A>", Resolver = _ => "A" });
            _registry.RegisterWildcard(new WildcardDefinition { Key = "<B>", Resolver = _ => "B" });
            _registry.RegisterWildcard(new WildcardDefinition { Key = "<C>", Resolver = _ => "C" });

            // Act
            var wildcards = _registry.GetAllWildcards();

            // Assert
            Assert.AreEqual(3, wildcards.Count);
            Assert.IsTrue(wildcards.Any(w => w.Key == "<A>"));
            Assert.IsTrue(wildcards.Any(w => w.Key == "<B>"));
            Assert.IsTrue(wildcards.Any(w => w.Key == "<C>"));
        }

        [Test]
        public void GetWildcardDefinition_WithExistingKey_ReturnsDefinition()
        {
            // Arrange
            var wildcard = new WildcardDefinition
            {
                Key = "<TestDef>",
                Description = "Test definition",
                Resolver = _ => "Value"
            };
            _registry.RegisterWildcard(wildcard);

            // Act
            var retrieved = _registry.GetWildcardDefinition("<TestDef>");

            // Assert
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("<TestDef>", retrieved.Key);
            Assert.AreEqual("Test definition", retrieved.Description);
        }

        [Test]
        public void GetWildcardDefinition_WithNonExistentKey_ReturnsNull()
        {
            // Act
            var retrieved = _registry.GetWildcardDefinition("<NonExistent>");

            // Assert
            Assert.IsNull(retrieved);
        }

        [Test]
        public void ResolveWildcard_WithValidContext_ResolvesCorrectly()
        {
            // Arrange
            var wildcard = new WildcardDefinition
            {
                Key = "<ContextBased>",
                Resolver = context => context?.SceneName ?? "DefaultValue"
            };
            _registry.RegisterWildcard(wildcard);
            
            var context = new Unity.MultiTimelineRecorder.WildcardContext
            {
                SceneName = "TestScene"
            };

            // Act
            var result = _registry.ResolveWildcard("<ContextBased>", context);

            // Assert
            Assert.AreEqual("TestScene", result);
        }

        [Test]
        public void ResolveWildcard_WithNullContext_UsesDefault()
        {
            // Arrange
            var wildcard = new WildcardDefinition
            {
                Key = "<NullContext>",
                Resolver = context => context?.TimelineName ?? "DefaultTimeline"
            };
            _registry.RegisterWildcard(wildcard);

            // Act
            var result = _registry.ResolveWildcard("<NullContext>", null);

            // Assert
            Assert.AreEqual("DefaultTimeline", result);
        }

        [Test]
        public void ResolveWildcard_WithNonExistentKey_ReturnsOriginalKey()
        {
            // Act
            var result = _registry.ResolveWildcard("<Unknown>", null);

            // Assert
            Assert.AreEqual("<Unknown>", result);
        }

        [Test]
        public void ResolveWildcard_WithResolverException_ReturnsErrorValue()
        {
            // Arrange
            var wildcard = new WildcardDefinition
            {
                Key = "<Faulty>",
                Resolver = context => throw new System.Exception("Test exception")
            };
            _registry.RegisterWildcard(wildcard);

            // Act
            var result = _registry.ResolveWildcard("<Faulty>", null);

            // Assert
            Assert.AreEqual("<Faulty>", result);
            Assert.IsTrue(_logger.HasError("Failed to resolve wildcard"));
        }

        [Test]
        public void Clear_RemovesAllWildcards()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition { Key = "<A>", Resolver = _ => "A" });
            _registry.RegisterWildcard(new WildcardDefinition { Key = "<B>", Resolver = _ => "B" });

            // Act
            _registry.Clear();

            // Assert
            var wildcards = _registry.GetAllWildcards();
            Assert.AreEqual(0, wildcards.Count);
        }

        [Test]
        public void GetWildcardsByCategory_ReturnsFilteredWildcards()
        {
            // Arrange
            _registry.RegisterWildcard(new WildcardDefinition 
            { 
                Key = "<Time1>", 
                Category = "Time",
                Resolver = _ => "Value" 
            });
            _registry.RegisterWildcard(new WildcardDefinition 
            { 
                Key = "<Time2>", 
                Category = "Time",
                Resolver = _ => "Value" 
            });
            _registry.RegisterWildcard(new WildcardDefinition 
            { 
                Key = "<Scene1>", 
                Category = "Scene",
                Resolver = _ => "Value" 
            });

            // Act
            var timeWildcards = _registry.GetWildcardsByCategory("Time");

            // Assert
            Assert.AreEqual(2, timeWildcards.Count);
            Assert.IsTrue(timeWildcards.All(w => w.Category == "Time"));
        }

        // Test logger implementation
        private class TestLogger : ILogger
        {
            private readonly List<string> _logs = new List<string>();

            public void Log(string message, LogLevel level, LogCategory category)
            {
                _logs.Add($"[{level}] {message}");
            }

            public void LogVerbose(string message, LogCategory category) => Log(message, LogLevel.Verbose, category);
            public void LogInfo(string message, LogCategory category) => Log(message, LogLevel.Info, category);
            public void LogWarning(string message, LogCategory category) => Log(message, LogLevel.Warning, category);
            public void LogError(string message, LogCategory category) => Log(message, LogLevel.Error, category);

            public bool HasWarning(string message) => _logs.Any(log => log.Contains(message) && log.StartsWith($"[{LogLevel.Warning}]"));
            public bool HasError(string message) => _logs.Any(log => log.Contains(message) && log.StartsWith($"[{LogLevel.Error}]"));
        }
    }
}