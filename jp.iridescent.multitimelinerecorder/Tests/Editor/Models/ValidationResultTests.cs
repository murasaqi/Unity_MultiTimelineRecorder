using System;
using NUnit.Framework;
using MultiTimelineRecorder.Core.Models;

namespace MultiTimelineRecorder.Tests.Models
{
    /// <summary>
    /// Unit tests for ValidationResult
    /// </summary>
    [TestFixture]
    public class ValidationResultTests
    {
        [Test]
        public void Constructor_InitializesAsValid()
        {
            var result = new ValidationResult();
            
            Assert.IsTrue(result.IsValid);
            Assert.IsNotNull(result.Errors);
            Assert.IsEmpty(result.Errors);
            Assert.IsNotNull(result.Warnings);
            Assert.IsEmpty(result.Warnings);
        }
        
        [Test]
        public void AddError_WithNullMessage_DoesNotAdd()
        {
            var result = new ValidationResult();
            
            result.AddError(null);
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }
        
        [Test]
        public void AddError_WithEmptyMessage_DoesNotAdd()
        {
            var result = new ValidationResult();
            
            result.AddError("");
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }
        
        [Test]
        public void AddError_WithValidMessage_AddsError()
        {
            var result = new ValidationResult();
            
            result.AddError("Test error message");
            
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.Contains("Test error message", result.Errors);
        }
        
        [Test]
        public void AddError_MultipleTimes_AddsAllErrors()
        {
            var result = new ValidationResult();
            
            result.AddError("Error 1");
            result.AddError("Error 2");
            result.AddError("Error 3");
            
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(3, result.Errors.Count);
            Assert.Contains("Error 1", result.Errors);
            Assert.Contains("Error 2", result.Errors);
            Assert.Contains("Error 3", result.Errors);
        }
        
        [Test]
        public void AddWarning_WithNullMessage_DoesNotAdd()
        {
            var result = new ValidationResult();
            
            result.AddWarning(null);
            
            Assert.IsEmpty(result.Warnings);
        }
        
        [Test]
        public void AddWarning_WithEmptyMessage_DoesNotAdd()
        {
            var result = new ValidationResult();
            
            result.AddWarning("");
            
            Assert.IsEmpty(result.Warnings);
        }
        
        [Test]
        public void AddWarning_WithValidMessage_AddsWarning()
        {
            var result = new ValidationResult();
            
            result.AddWarning("Test warning message");
            
            Assert.IsTrue(result.IsValid); // Warnings don't affect validity
            Assert.AreEqual(1, result.Warnings.Count);
            Assert.Contains("Test warning message", result.Warnings);
        }
        
        [Test]
        public void AddWarning_DoesNotAffectValidity()
        {
            var result = new ValidationResult();
            
            result.AddWarning("Warning 1");
            result.AddWarning("Warning 2");
            result.AddWarning("Warning 3");
            
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(3, result.Warnings.Count);
        }
        
        [Test]
        public void Merge_WithNullOther_DoesNothing()
        {
            var result = new ValidationResult();
            result.AddError("Original error");
            
            result.Merge(null);
            
            Assert.AreEqual(1, result.Errors.Count);
            Assert.Contains("Original error", result.Errors);
        }
        
        [Test]
        public void Merge_WithOtherErrors_CombinesErrors()
        {
            var result1 = new ValidationResult();
            result1.AddError("Error 1");
            
            var result2 = new ValidationResult();
            result2.AddError("Error 2");
            result2.AddError("Error 3");
            
            result1.Merge(result2);
            
            Assert.IsFalse(result1.IsValid);
            Assert.AreEqual(3, result1.Errors.Count);
            Assert.Contains("Error 1", result1.Errors);
            Assert.Contains("Error 2", result1.Errors);
            Assert.Contains("Error 3", result1.Errors);
        }
        
        [Test]
        public void Merge_WithOtherWarnings_CombinesWarnings()
        {
            var result1 = new ValidationResult();
            result1.AddWarning("Warning 1");
            
            var result2 = new ValidationResult();
            result2.AddWarning("Warning 2");
            
            result1.Merge(result2);
            
            Assert.IsTrue(result1.IsValid);
            Assert.AreEqual(2, result1.Warnings.Count);
            Assert.Contains("Warning 1", result1.Warnings);
            Assert.Contains("Warning 2", result1.Warnings);
        }
        
        [Test]
        public void Merge_WithMixedResults_CombinesAll()
        {
            var result1 = new ValidationResult();
            result1.AddError("Error 1");
            result1.AddWarning("Warning 1");
            
            var result2 = new ValidationResult();
            result2.AddError("Error 2");
            result2.AddWarning("Warning 2");
            
            result1.Merge(result2);
            
            Assert.IsFalse(result1.IsValid);
            Assert.AreEqual(2, result1.Errors.Count);
            Assert.AreEqual(2, result1.Warnings.Count);
        }
        
        [Test]
        public void Clear_RemovesAllErrorsAndWarnings()
        {
            var result = new ValidationResult();
            result.AddError("Error 1");
            result.AddError("Error 2");
            result.AddWarning("Warning 1");
            
            result.Clear();
            
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
            Assert.IsEmpty(result.Warnings);
        }
        
        [Test]
        public void HasWarnings_ReturnsCorrectValue()
        {
            var result = new ValidationResult();
            
            Assert.IsFalse(result.HasWarnings);
            
            result.AddWarning("Test warning");
            
            Assert.IsTrue(result.HasWarnings);
        }
        
        [Test]
        public void ToString_WithErrors_IncludesErrors()
        {
            var result = new ValidationResult();
            result.AddError("Error 1");
            result.AddError("Error 2");
            
            var str = result.ToString();
            
            Assert.IsTrue(str.Contains("Errors:"));
            Assert.IsTrue(str.Contains("Error 1"));
            Assert.IsTrue(str.Contains("Error 2"));
        }
        
        [Test]
        public void ToString_WithWarnings_IncludesWarnings()
        {
            var result = new ValidationResult();
            result.AddWarning("Warning 1");
            
            var str = result.ToString();
            
            Assert.IsTrue(str.Contains("Warnings:"));
            Assert.IsTrue(str.Contains("Warning 1"));
        }
        
        [Test]
        public void ToString_WithNoIssues_ReturnsValidMessage()
        {
            var result = new ValidationResult();
            
            var str = result.ToString();
            
            Assert.AreEqual("Validation passed", str);
        }
    }
}