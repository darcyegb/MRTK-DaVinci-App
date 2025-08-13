using NUnit.Framework;
using UnityEngine;
using DaVinciEye.ColorAnalysis;
using System.Collections.Generic;

namespace DaVinciEye.Tests.ColorAnalysis
{
    /// <summary>
    /// Unit tests for ColorMatcher functionality
    /// Tests color difference algorithms and matching accuracy
    /// </summary>
    public class ColorMatcherTests
    {
        private GameObject testGameObject;
        private ColorMatcher colorMatcher;
        
        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestColorMatcher");
            colorMatcher = testGameObject.AddComponent<ColorMatcher>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }
        
        [Test]
        public void ColorMatcher_CompareIdenticalColors_ReturnsExcellentMatch()
        {
            // Arrange
            Color testColor = Color.red;
            
            // Act
            ColorMatchResult result = colorMatcher.CompareColors(testColor, testColor);
            
            // Assert
            Assert.AreEqual("Excellent", result.matchQuality);
            Assert.AreEqual(1f, result.matchAccuracy, 0.01f);
            Assert.AreEqual(0f, result.deltaE, 0.01f);
        }
        
        [Test]
        public void ColorMatcher_CompareDifferentColors_ReturnsValidResult()
        {
            // Arrange
            Color referenceColor = Color.red;
            Color paintColor = Color.blue;
            
            // Act
            ColorMatchResult result = colorMatcher.CompareColors(referenceColor, paintColor);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(referenceColor, result.referenceColor);
            Assert.AreEqual(paintColor, result.capturedColor);
            Assert.Greater(result.deltaE, 0f);
            Assert.Less(result.matchAccuracy, 1f);
        }
        
        [Test]
        public void ColorMatcher_CompareColors_GeneratesAdjustmentSuggestions()
        {
            // Arrange
            Color referenceColor = new Color(1f, 0f, 0f); // Pure red
            Color paintColor = new Color(0.5f, 0.2f, 0.1f); // Darker, less pure red
            
            // Act
            ColorMatchResult result = colorMatcher.CompareColors(referenceColor, paintColor);
            
            // Assert
            Assert.IsNotNull(result.adjustmentSuggestions);
            Assert.Greater(result.adjustmentSuggestions.Length, 0);
        }
        
        [Test]
        public void ColorMatcher_SaveColorMatch_AddsToHistory()
        {
            // Arrange
            ColorMatchData matchData = new ColorMatchData(Color.red, Color.blue, Vector3.zero);
            int initialCount = colorMatcher.MatchHistory.Count;
            
            // Act
            colorMatcher.SaveColorMatch(matchData);
            
            // Assert
            Assert.AreEqual(initialCount + 1, colorMatcher.MatchHistory.Count);
            Assert.Contains(matchData, colorMatcher.MatchHistory);
        }
        
        [Test]
        public void ColorMatcher_GetColorHistory_ReturnsCorrectHistory()
        {
            // Arrange
            ColorMatchData match1 = new ColorMatchData(Color.red, Color.blue, Vector3.zero);
            ColorMatchData match2 = new ColorMatchData(Color.green, Color.yellow, Vector3.one);
            
            colorMatcher.SaveColorMatch(match1);
            colorMatcher.SaveColorMatch(match2);
            
            // Act
            List<ColorMatchData> history = colorMatcher.GetColorHistory();
            
            // Assert
            Assert.AreEqual(2, history.Count);
            Assert.Contains(match1, history);
            Assert.Contains(match2, history);
        }
        
        [Test]
        public void ColorMatcher_ClearMatchHistory_RemovesAllMatches()
        {
            // Arrange
            colorMatcher.SaveColorMatch(new ColorMatchData(Color.red, Color.blue, Vector3.zero));
            colorMatcher.SaveColorMatch(new ColorMatchData(Color.green, Color.yellow, Vector3.one));
            
            // Act
            colorMatcher.ClearMatchHistory();
            
            // Assert
            Assert.AreEqual(0, colorMatcher.MatchHistory.Count);
        }
        
        [Test]
        public void ColorMatcher_SetMatchingMethod_UpdatesMethod()
        {
            // Act
            colorMatcher.SetMatchingMethod(ColorMatchingMethod.RGB);
            
            // Assert - Test by comparing results with different methods
            Color color1 = Color.red;
            Color color2 = new Color(0.9f, 0f, 0f);
            
            ColorMatchResult rgbResult = colorMatcher.CompareColors(color1, color2);
            
            colorMatcher.SetMatchingMethod(ColorMatchingMethod.HSV);
            ColorMatchResult hsvResult = colorMatcher.CompareColors(color1, color2);
            
            // Results should be different with different methods
            Assert.AreNotEqual(rgbResult.deltaE, hsvResult.deltaE, 0.001f);
        }
        
        [Test]
        public void ColorMatcher_SetQualityThresholds_UpdatesThresholds()
        {
            // Arrange
            Color referenceColor = Color.red;
            Color paintColor = new Color(0.95f, 0f, 0f); // Slightly different red
            
            // Act - Set very strict thresholds
            colorMatcher.SetQualityThresholds(0.5f, 1.0f, 2.0f);
            ColorMatchResult strictResult = colorMatcher.CompareColors(referenceColor, paintColor);
            
            // Act - Set very lenient thresholds
            colorMatcher.SetQualityThresholds(10.0f, 20.0f, 30.0f);
            ColorMatchResult lenientResult = colorMatcher.CompareColors(referenceColor, paintColor);
            
            // Assert - Same colors should get different quality ratings with different thresholds
            // (This test assumes the delta E is between 0.5 and 10.0)
            Assert.AreNotEqual(strictResult.matchQuality, lenientResult.matchQuality);
        }
        
        [Test]
        public void ColorMatcher_GetMatchStatistics_ReturnsCorrectStats()
        {
            // Arrange - Add matches with known qualities
            ColorMatchData excellentMatch = new ColorMatchData(Color.red, Color.red, Vector3.zero);
            excellentMatch.matchAccuracy = 0.98f;
            
            ColorMatchData goodMatch = new ColorMatchData(Color.blue, new Color(0.9f, 0f, 0.9f), Vector3.zero);
            goodMatch.matchAccuracy = 0.85f;
            
            colorMatcher.SaveColorMatch(excellentMatch);
            colorMatcher.SaveColorMatch(goodMatch);
            
            // Act
            ColorMatchStatistics stats = colorMatcher.GetMatchStatistics();
            
            // Assert
            Assert.AreEqual(2, stats.totalMatches);
            Assert.AreEqual((0.98f + 0.85f) / 2f, stats.averageAccuracy, 0.01f);
        }
        
        [Test]
        public void ColorMatcher_CompareColors_TriggersEvent()
        {
            // Arrange
            bool eventTriggered = false;
            ColorMatchResult receivedResult = null;
            
            colorMatcher.OnColorMatched += (result) =>
            {
                eventTriggered = true;
                receivedResult = result;
            };
            
            // Act
            ColorMatchResult actualResult = colorMatcher.CompareColors(Color.red, Color.blue);
            
            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(actualResult, receivedResult);
        }
        
        [Test]
        public void ColorMatcher_SaveColorMatch_TriggersEvent()
        {
            // Arrange
            bool eventTriggered = false;
            ColorMatchData receivedData = null;
            
            colorMatcher.OnMatchSaved += (data) =>
            {
                eventTriggered = true;
                receivedData = data;
            };
            
            ColorMatchData testData = new ColorMatchData(Color.red, Color.blue, Vector3.zero);
            
            // Act
            colorMatcher.SaveColorMatch(testData);
            
            // Assert
            Assert.IsTrue(eventTriggered);
            Assert.AreEqual(testData, receivedData);
        }
        
        [Test]
        public void ColorMatcher_CompareGrayscaleColors_HandlesCorrectly()
        {
            // Arrange
            Color lightGray = new Color(0.8f, 0.8f, 0.8f);
            Color darkGray = new Color(0.2f, 0.2f, 0.2f);
            
            // Act
            ColorMatchResult result = colorMatcher.CompareColors(lightGray, darkGray);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.deltaE, 0f);
            Assert.IsNotNull(result.adjustmentSuggestions);
        }
        
        [Test]
        public void ColorMatcher_CompareSaturatedColors_HandlesCorrectly()
        {
            // Arrange
            Color saturatedRed = Color.red;
            Color desaturatedRed = new Color(0.8f, 0.4f, 0.4f);
            
            // Act
            ColorMatchResult result = colorMatcher.CompareColors(saturatedRed, desaturatedRed);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.deltaE, 0f);
            
            // Should suggest increasing saturation
            bool hasSaturationSuggestion = false;
            foreach (string suggestion in result.adjustmentSuggestions)
            {
                if (suggestion.ToLower().Contains("saturation"))
                {
                    hasSaturationSuggestion = true;
                    break;
                }
            }
            Assert.IsTrue(hasSaturationSuggestion);
        }
        
        [Test]
        public void ColorMatcher_HandleNullInput_DoesNotCrash()
        {
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => colorMatcher.SaveColorMatch(null));
            Assert.DoesNotThrow(() => colorMatcher.CompareColors(Color.clear, Color.clear));
        }
        
        [Test]
        public void ColorMatcher_HistoryLimit_MaintainsMaxSize()
        {
            // Arrange - Add more than 100 matches (the limit)
            for (int i = 0; i < 105; i++)
            {
                ColorMatchData match = new ColorMatchData(
                    Color.red, 
                    new Color(i / 105f, 0f, 0f), 
                    Vector3.zero
                );
                colorMatcher.SaveColorMatch(match);
            }
            
            // Assert - Should not exceed 100 matches
            Assert.LessOrEqual(colorMatcher.MatchHistory.Count, 100);
        }
    }
}