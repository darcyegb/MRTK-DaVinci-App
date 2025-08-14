using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace DaVinciEye.SessionManagement.Tests
{
    /// <summary>
    /// Unit tests for ColorHistoryManager
    /// Tests color match history storage, persistence, and data integrity
    /// </summary>
    public class ColorHistoryManagerTests
    {
        private GameObject testGameObject;
        private ColorHistoryManager historyManager;
        private string testHistoryPath;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with ColorHistoryManager
            testGameObject = new GameObject("TestColorHistoryManager");
            historyManager = testGameObject.AddComponent<ColorHistoryManager>();
            
            // Set up test file path
            testHistoryPath = Path.Combine(Application.temporaryCachePath, "test_color_history.json");
            
            // Use reflection to set the private historyFilePath field for testing
            var field = typeof(ColorHistoryManager).GetField("historyFilePath", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(historyManager, testHistoryPath);
            
            // Clean up any existing test files
            if (File.Exists(testHistoryPath))
            {
                File.Delete(testHistoryPath);
            }
            
            if (File.Exists(testHistoryPath + ".backup"))
            {
                File.Delete(testHistoryPath + ".backup");
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            if (File.Exists(testHistoryPath))
            {
                File.Delete(testHistoryPath);
            }
            
            if (File.Exists(testHistoryPath + ".backup"))
            {
                File.Delete(testHistoryPath + ".backup");
            }
            
            // Destroy test GameObject
            if (testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void ColorHistoryManager_Initialization_SetsUpCorrectly()
        {
            // Act - Awake is called automatically
            
            // Assert
            Assert.IsNotNull(historyManager.CurrentSessionHistory);
            Assert.IsNotNull(historyManager.AllTimeHistory);
            Assert.AreEqual(0, historyManager.TotalMatches);
            Assert.AreEqual(0, historyManager.SessionMatches);
        }
        
        [Test]
        public void StartNewSession_CreatesNewSession()
        {
            // Act
            historyManager.StartNewSession();
            
            // Assert
            Assert.IsNotNull(historyManager.CurrentSession);
            Assert.IsNotEmpty(historyManager.CurrentSession.sessionId);
            Assert.AreEqual(0, historyManager.CurrentSession.GetMatchCount());
            Assert.AreEqual(0, historyManager.SessionMatches);
        }
        
        [Test]
        public void AddColorMatch_AddsToCurrentSessionAndAllTime()
        {
            // Arrange
            historyManager.StartNewSession();
            var matchData = CreateTestColorMatchData();
            
            // Act
            historyManager.AddColorMatch(matchData);
            
            // Assert
            Assert.AreEqual(1, historyManager.SessionMatches);
            Assert.AreEqual(1, historyManager.TotalMatches);
            Assert.AreEqual(historyManager.CurrentSession.sessionId, matchData.sessionId);
            Assert.Contains(matchData, historyManager.CurrentSessionHistory);
            Assert.Contains(matchData, historyManager.AllTimeHistory);
        }
        
        [Test]
        public void AddColorMatch_WithNullData_DoesNotAdd()
        {
            // Arrange
            historyManager.StartNewSession();
            
            // Act
            historyManager.AddColorMatch(null);
            
            // Assert
            Assert.AreEqual(0, historyManager.SessionMatches);
            Assert.AreEqual(0, historyManager.TotalMatches);
        }
        
        [Test]
        public void AddColorMatch_SetsTimestampIfNotSet()
        {
            // Arrange
            historyManager.StartNewSession();
            var matchData = CreateTestColorMatchData();
            matchData.timestamp = default(DateTime);
            var beforeAdd = DateTime.Now;
            
            // Act
            historyManager.AddColorMatch(matchData);
            
            // Assert
            Assert.Greater(matchData.timestamp, beforeAdd.AddSeconds(-1));
            Assert.Less(matchData.timestamp, DateTime.Now.AddSeconds(1));
        }
        
        [Test]
        public void GetCurrentSessionMatches_ReturnsCorrectMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            var match1 = CreateTestColorMatchData();
            var match2 = CreateTestColorMatchData();
            historyManager.AddColorMatch(match1);
            historyManager.AddColorMatch(match2);
            
            // Act
            var sessionMatches = historyManager.GetCurrentSessionMatches();
            
            // Assert
            Assert.AreEqual(2, sessionMatches.Count);
            Assert.Contains(match1, sessionMatches);
            Assert.Contains(match2, sessionMatches);
        }
        
        [Test]
        public void GetAllMatches_ReturnsAllHistoryMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            var match1 = CreateTestColorMatchData();
            historyManager.AddColorMatch(match1);
            
            historyManager.StartNewSession(); // New session
            var match2 = CreateTestColorMatchData();
            historyManager.AddColorMatch(match2);
            
            // Act
            var allMatches = historyManager.GetAllMatches();
            
            // Assert
            Assert.AreEqual(2, allMatches.Count);
            Assert.Contains(match1, allMatches);
            Assert.Contains(match2, allMatches);
        }
        
        [Test]
        public void GetMatchesFiltered_ByAccuracy_ReturnsCorrectMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            var highAccuracyMatch = CreateTestColorMatchData();
            highAccuracyMatch.matchAccuracy = 0.9f;
            var lowAccuracyMatch = CreateTestColorMatchData();
            lowAccuracyMatch.matchAccuracy = 0.3f;
            
            historyManager.AddColorMatch(highAccuracyMatch);
            historyManager.AddColorMatch(lowAccuracyMatch);
            
            var filter = new ColorMatchFilter
            {
                minAccuracy = 0.8f
            };
            
            // Act
            var filteredMatches = historyManager.GetMatchesFiltered(filter);
            
            // Assert
            Assert.AreEqual(1, filteredMatches.Count);
            Assert.Contains(highAccuracyMatch, filteredMatches);
            Assert.IsFalse(filteredMatches.Contains(lowAccuracyMatch));
        }
        
        [Test]
        public void GetMatchesFiltered_ByDateRange_ReturnsCorrectMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            var oldMatch = CreateTestColorMatchData();
            oldMatch.timestamp = DateTime.Now.AddDays(-10);
            var recentMatch = CreateTestColorMatchData();
            recentMatch.timestamp = DateTime.Now.AddDays(-1);
            
            historyManager.AddColorMatch(oldMatch);
            historyManager.AddColorMatch(recentMatch);
            
            var filter = new ColorMatchFilter
            {
                startDate = DateTime.Now.AddDays(-5)
            };
            
            // Act
            var filteredMatches = historyManager.GetMatchesFiltered(filter);
            
            // Assert
            Assert.AreEqual(1, filteredMatches.Count);
            Assert.Contains(recentMatch, filteredMatches);
            Assert.IsFalse(filteredMatches.Contains(oldMatch));
        }
        
        [Test]
        public void GetMatchesFiltered_BySessionId_ReturnsCorrectMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            string firstSessionId = historyManager.CurrentSession.sessionId;
            var match1 = CreateTestColorMatchData();
            historyManager.AddColorMatch(match1);
            
            historyManager.StartNewSession();
            var match2 = CreateTestColorMatchData();
            historyManager.AddColorMatch(match2);
            
            var filter = new ColorMatchFilter
            {
                sessionId = firstSessionId
            };
            
            // Act
            var filteredMatches = historyManager.GetMatchesFiltered(filter);
            
            // Assert
            Assert.AreEqual(1, filteredMatches.Count);
            Assert.Contains(match1, filteredMatches);
            Assert.IsFalse(filteredMatches.Contains(match2));
        }
        
        [Test]
        public void GetStatistics_WithMatches_ReturnsCorrectStatistics()
        {
            // Arrange
            historyManager.StartNewSession();
            var match1 = CreateTestColorMatchData();
            match1.matchAccuracy = 0.9f;
            var match2 = CreateTestColorMatchData();
            match2.matchAccuracy = 0.7f;
            var match3 = CreateTestColorMatchData();
            match3.matchAccuracy = 0.4f;
            
            historyManager.AddColorMatch(match1);
            historyManager.AddColorMatch(match2);
            historyManager.AddColorMatch(match3);
            
            // Act
            var stats = historyManager.GetStatistics();
            
            // Assert
            Assert.AreEqual(3, stats.totalMatches);
            Assert.AreEqual(3, stats.sessionMatches);
            Assert.AreEqual(0.67f, stats.averageAccuracy, 0.01f);
            Assert.AreEqual(1, stats.excellentMatches); // >= 0.9
            Assert.AreEqual(1, stats.goodMatches); // >= 0.7 && < 0.9
            Assert.AreEqual(0, stats.fairMatches); // >= 0.5 && < 0.7
            Assert.AreEqual(1, stats.poorMatches); // < 0.5
            Assert.AreEqual(match1, stats.bestMatch);
            Assert.AreEqual(match3, stats.worstMatch);
        }
        
        [Test]
        public void GetStatistics_WithNoMatches_ReturnsEmptyStatistics()
        {
            // Act
            var stats = historyManager.GetStatistics();
            
            // Assert
            Assert.AreEqual(0, stats.totalMatches);
            Assert.AreEqual(0, stats.sessionMatches);
            Assert.AreEqual(0f, stats.averageAccuracy);
            Assert.IsNull(stats.bestMatch);
            Assert.IsNull(stats.worstMatch);
        }
        
        [Test]
        public void ClearHistory_RemovesAllMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            historyManager.AddColorMatch(CreateTestColorMatchData());
            historyManager.AddColorMatch(CreateTestColorMatchData());
            
            // Act
            historyManager.ClearHistory();
            
            // Assert
            Assert.AreEqual(0, historyManager.TotalMatches);
            Assert.AreEqual(0, historyManager.SessionMatches);
            Assert.AreEqual(0, historyManager.CurrentSessionHistory.Count);
            Assert.AreEqual(0, historyManager.AllTimeHistory.Count);
        }
        
        [Test]
        public void ClearCurrentSession_RemovesOnlySessionMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            historyManager.AddColorMatch(CreateTestColorMatchData());
            int totalBeforeClear = historyManager.TotalMatches;
            
            // Act
            historyManager.ClearCurrentSession();
            
            // Assert
            Assert.AreEqual(0, historyManager.SessionMatches);
            Assert.AreEqual(totalBeforeClear, historyManager.TotalMatches); // All-time history unchanged
        }
        
        [Test]
        public void ExportHistoryToJson_WithMatches_ReturnsValidJson()
        {
            // Arrange
            historyManager.StartNewSession();
            historyManager.AddColorMatch(CreateTestColorMatchData());
            
            // Act
            string json = historyManager.ExportHistoryToJson();
            
            // Assert
            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            
            // Verify JSON can be parsed
            var exportData = JsonUtility.FromJson<ColorHistoryExportData>(json);
            Assert.AreEqual(1, exportData.totalMatches);
            Assert.AreEqual(1, exportData.matches.Length);
        }
        
        [Test]
        public void ImportHistoryFromJson_WithValidJson_ImportsCorrectly()
        {
            // Arrange
            var testMatch = CreateTestColorMatchData();
            var exportData = new ColorHistoryExportData
            {
                totalMatches = 1,
                matches = new[] { testMatch }
            };
            string json = JsonUtility.ToJson(exportData);
            
            // Act
            bool success = historyManager.ImportHistoryFromJson(json, false);
            
            // Assert
            Assert.IsTrue(success);
            Assert.AreEqual(1, historyManager.TotalMatches);
            
            var importedMatch = historyManager.AllTimeHistory[0];
            Assert.AreEqual(testMatch.referenceColor, importedMatch.referenceColor);
            Assert.AreEqual(testMatch.capturedColor, importedMatch.capturedColor);
            Assert.AreEqual(testMatch.matchAccuracy, importedMatch.matchAccuracy);
        }
        
        [Test]
        public void ImportHistoryFromJson_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            string invalidJson = "{ invalid json }";
            
            // Act
            bool success = historyManager.ImportHistoryFromJson(invalidJson);
            
            // Assert
            Assert.IsFalse(success);
            Assert.AreEqual(0, historyManager.TotalMatches);
        }
        
        [Test]
        public void EndCurrentSession_SetsEndTimeAndSavesMatches()
        {
            // Arrange
            historyManager.StartNewSession();
            var sessionId = historyManager.CurrentSession.sessionId;
            historyManager.AddColorMatch(CreateTestColorMatchData());
            
            // Act
            historyManager.EndCurrentSession();
            
            // Assert
            Assert.IsNull(historyManager.CurrentSession);
            // Session should be ended but matches remain in all-time history
            Assert.AreEqual(1, historyManager.TotalMatches);
        }
        
        [UnityTest]
        public IEnumerator SaveAndLoadHistoryToDisk_PreservesData()
        {
            // Arrange
            historyManager.StartNewSession();
            var testMatch = CreateTestColorMatchData();
            historyManager.AddColorMatch(testMatch);
            
            // Act - Save
            historyManager.SaveHistoryToDisk();
            yield return null; // Wait a frame for file operations
            
            // Clear memory and load
            historyManager.ClearHistory();
            Assert.AreEqual(0, historyManager.TotalMatches);
            
            historyManager.LoadHistoryFromDisk();
            yield return null; // Wait a frame for file operations
            
            // Assert
            Assert.AreEqual(1, historyManager.TotalMatches);
            var loadedMatch = historyManager.AllTimeHistory[0];
            Assert.AreEqual(testMatch.referenceColor, loadedMatch.referenceColor);
            Assert.AreEqual(testMatch.capturedColor, loadedMatch.capturedColor);
            Assert.AreEqual(testMatch.matchAccuracy, loadedMatch.matchAccuracy);
        }
        
        [Test]
        public void GetUniqueSessions_ReturnsCorrectSessionIds()
        {
            // Arrange
            historyManager.StartNewSession();
            string session1Id = historyManager.CurrentSession.sessionId;
            historyManager.AddColorMatch(CreateTestColorMatchData());
            
            historyManager.StartNewSession();
            string session2Id = historyManager.CurrentSession.sessionId;
            historyManager.AddColorMatch(CreateTestColorMatchData());
            
            // Act
            var uniqueSessions = historyManager.GetUniqueSessions();
            
            // Assert
            Assert.AreEqual(2, uniqueSessions.Count);
            Assert.Contains(session1Id, uniqueSessions);
            Assert.Contains(session2Id, uniqueSessions);
        }
        
        [Test]
        public void ColorMatchData_Constructor_SetsDefaultValues()
        {
            // Act
            var matchData = new ColorMatchData();
            
            // Assert
            Assert.AreEqual(Color.white, matchData.referenceColor);
            Assert.AreEqual(Color.white, matchData.capturedColor);
            Assert.AreEqual(0f, matchData.matchAccuracy);
            Assert.AreEqual(Vector3.zero, matchData.capturePosition);
            Assert.AreEqual(Vector2.zero, matchData.imageCoordinate);
            Assert.AreEqual("", matchData.sessionId);
            Assert.AreEqual("", matchData.notes);
            Assert.AreEqual(0f, matchData.deltaE);
            Assert.AreEqual("Unknown", matchData.matchQuality);
            Assert.IsNotNull(matchData.adjustmentSuggestions);
            Assert.AreEqual(ColorMatchingMethod.DeltaE, matchData.matchingMethod);
        }
        
        [Test]
        public void ColorMatchData_GetRGBDifference_CalculatesCorrectly()
        {
            // Arrange
            var matchData = new ColorMatchData
            {
                referenceColor = new Color(1f, 0.5f, 0f),
                capturedColor = new Color(0.8f, 0.7f, 0.2f)
            };
            
            // Act
            var rgbDiff = matchData.GetRGBDifference();
            
            // Assert
            Assert.AreEqual(0.2f, rgbDiff.x, 0.001f);
            Assert.AreEqual(0.2f, rgbDiff.y, 0.001f);
            Assert.AreEqual(0.2f, rgbDiff.z, 0.001f);
        }
        
        [Test]
        public void ColorMatchData_IsGoodMatch_ReturnsCorrectResult()
        {
            // Arrange
            var goodMatch = new ColorMatchData { matchAccuracy = 0.8f };
            var poorMatch = new ColorMatchData { matchAccuracy = 0.5f };
            
            // Act & Assert
            Assert.IsTrue(goodMatch.IsGoodMatch(0.7f));
            Assert.IsFalse(poorMatch.IsGoodMatch(0.7f));
        }
        
        [Test]
        public void ColorMatchData_Clone_CreatesExactCopy()
        {
            // Arrange
            var original = CreateTestColorMatchData();
            original.notes = "Test notes";
            original.adjustmentSuggestions = new[] { "Add red", "Reduce blue" };
            
            // Act
            var clone = original.Clone();
            
            // Assert
            Assert.AreEqual(original.referenceColor, clone.referenceColor);
            Assert.AreEqual(original.capturedColor, clone.capturedColor);
            Assert.AreEqual(original.matchAccuracy, clone.matchAccuracy);
            Assert.AreEqual(original.notes, clone.notes);
            Assert.AreEqual(original.adjustmentSuggestions.Length, clone.adjustmentSuggestions.Length);
            
            // Ensure it's a deep copy
            Assert.AreNotSame(original, clone);
            Assert.AreNotSame(original.adjustmentSuggestions, clone.adjustmentSuggestions);
        }
        
        [Test]
        public void ColorMatchData_ToJsonAndFromJson_PreservesData()
        {
            // Arrange
            var original = CreateTestColorMatchData();
            original.notes = "Test serialization";
            
            // Act
            string json = original.ToJson();
            var deserialized = ColorMatchData.FromJson(json);
            
            // Assert
            Assert.AreEqual(original.referenceColor, deserialized.referenceColor);
            Assert.AreEqual(original.capturedColor, deserialized.capturedColor);
            Assert.AreEqual(original.matchAccuracy, deserialized.matchAccuracy);
            Assert.AreEqual(original.notes, deserialized.notes);
        }
        
        // Helper method to create test color match data
        private ColorMatchData CreateTestColorMatchData()
        {
            return new ColorMatchData
            {
                referenceColor = new Color(0.8f, 0.2f, 0.4f),
                capturedColor = new Color(0.7f, 0.3f, 0.5f),
                matchAccuracy = 0.85f,
                capturePosition = new Vector3(1f, 2f, 3f),
                imageCoordinate = new Vector2(0.5f, 0.7f),
                timestamp = DateTime.Now,
                deltaE = 5.2f,
                matchQuality = "Good",
                adjustmentSuggestions = new[] { "Add more red" },
                matchingMethod = ColorMatchingMethod.DeltaE
            };
        }
    }
}