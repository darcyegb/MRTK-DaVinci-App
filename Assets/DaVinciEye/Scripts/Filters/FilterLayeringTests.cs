using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DaVinciEye.Filters;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Integration tests for filter layering and management functionality
    /// Tests requirements 4.7 and 4.9 - multiple filter combination and session persistence
    /// </summary>
    public class FilterLayeringTests
    {
        private GameObject testGameObject;
        private FilterManager filterManager;
        private Texture2D testTexture;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with FilterManager
            testGameObject = new GameObject("TestFilterManager");
            filterManager = testGameObject.AddComponent<FilterManager>();
            
            // Create test texture
            testTexture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            Color[] pixels = new Color[256 * 256];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            testTexture.SetPixels(pixels);
            testTexture.Apply();
            
            filterManager.SetSourceTexture(testTexture);
            
            // Wait for initialization
            filterManager.Start();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testTexture != null)
            {
                Object.DestroyImmediate(testTexture);
            }
        }
        
        /// <summary>
        /// Test multiple filter combination system (Requirement 4.7)
        /// </summary>
        [Test]
        public void TestMultipleFilterCombination()
        {
            // Arrange
            var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.6f };
            var edgeParams = new FilterParameters(FilterType.EdgeDetection) { intensity = 0.4f };
            
            // Act - Apply multiple filters
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
            filterManager.ApplyFilter(FilterType.EdgeDetection, edgeParams);
            
            // Assert
            Assert.AreEqual(3, filterManager.ActiveFilters.Count, "Should have 3 active filters");
            
            var activeTypes = filterManager.GetActiveFilterTypes();
            Assert.Contains(FilterType.Grayscale, activeTypes, "Grayscale filter should be active");
            Assert.Contains(FilterType.ContrastEnhancement, activeTypes, "Contrast filter should be active");
            Assert.Contains(FilterType.EdgeDetection, activeTypes, "Edge detection filter should be active");
            
            // Verify layer ordering
            var grayscaleFilter = filterManager.ActiveFilters.First(f => f.type == FilterType.Grayscale);
            var contrastFilter = filterManager.ActiveFilters.First(f => f.type == FilterType.ContrastEnhancement);
            var edgeFilter = filterManager.ActiveFilters.First(f => f.type == FilterType.EdgeDetection);
            
            Assert.Less(grayscaleFilter.layerOrder, contrastFilter.layerOrder, "Grayscale should be applied before contrast");
            Assert.Less(contrastFilter.layerOrder, edgeFilter.layerOrder, "Contrast should be applied before edge detection");
        }
        
        /// <summary>
        /// Test filter layer reordering functionality
        /// </summary>
        [Test]
        public void TestFilterLayerReordering()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            var initialGrayscaleOrder = filterManager.GetFilterLayerOrder(FilterType.Grayscale);
            var initialContrastOrder = filterManager.GetFilterLayerOrder(FilterType.ContrastEnhancement);
            
            // Act - Move grayscale filter up (should swap with contrast)
            filterManager.MoveFilterUp(FilterType.Grayscale);
            
            // Assert
            var newGrayscaleOrder = filterManager.GetFilterLayerOrder(FilterType.Grayscale);
            var newContrastOrder = filterManager.GetFilterLayerOrder(FilterType.ContrastEnhancement);
            
            Assert.AreEqual(initialContrastOrder, newGrayscaleOrder, "Grayscale should now have contrast's original order");
            Assert.AreEqual(initialGrayscaleOrder, newContrastOrder, "Contrast should now have grayscale's original order");
        }
        
        /// <summary>
        /// Test individual filter removal while maintaining other filters
        /// </summary>
        [Test]
        public void TestIndividualFilterRemoval()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            
            Assert.AreEqual(3, filterManager.ActiveFilters.Count, "Should start with 3 filters");
            
            // Act - Remove middle filter
            filterManager.RemoveFilter(FilterType.ContrastEnhancement);
            
            // Assert
            Assert.AreEqual(2, filterManager.ActiveFilters.Count, "Should have 2 filters after removal");
            Assert.IsFalse(filterManager.IsFilterActive(FilterType.ContrastEnhancement), "Contrast filter should not be active");
            Assert.IsTrue(filterManager.IsFilterActive(FilterType.Grayscale), "Grayscale filter should still be active");
            Assert.IsTrue(filterManager.IsFilterActive(FilterType.EdgeDetection), "Edge detection filter should still be active");
        }
        
        /// <summary>
        /// Test filter toggle functionality
        /// </summary>
        [Test]
        public void TestFilterToggle()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            Assert.IsTrue(filterManager.IsFilterActive(FilterType.Grayscale), "Filter should be active initially");
            
            // Act - Toggle off
            filterManager.ToggleFilter(FilterType.Grayscale, false);
            
            // Assert
            Assert.IsFalse(filterManager.IsFilterActive(FilterType.Grayscale), "Filter should be inactive after toggle off");
            Assert.AreEqual(1, filterManager.ActiveFilters.Count, "Filter should still exist in the list");
            
            // Act - Toggle back on
            filterManager.ToggleFilter(FilterType.Grayscale, true);
            
            // Assert
            Assert.IsTrue(filterManager.IsFilterActive(FilterType.Grayscale), "Filter should be active after toggle on");
        }
        
        /// <summary>
        /// Test filter reset functionality
        /// </summary>
        [Test]
        public void TestFilterReset()
        {
            // Arrange
            var customParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.5f };
            filterManager.ApplyFilter(FilterType.Grayscale, customParams);
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            // Act - Reset all filter parameters
            filterManager.ResetAllFilterParameters();
            
            // Assert
            var grayscaleParams = filterManager.GetFilterParameters(FilterType.Grayscale);
            Assert.AreEqual(1f, grayscaleParams.intensity, "Grayscale intensity should be reset to default");
            
            var contrastParams = filterManager.GetFilterParameters(FilterType.ContrastEnhancement);
            Assert.AreEqual(0.5f, contrastParams.intensity, "Contrast intensity should be reset to default");
        }
        
        /// <summary>
        /// Test clear all filters with backup functionality
        /// </summary>
        [Test]
        public void TestClearAllFiltersWithBackup()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            Assert.AreEqual(2, filterManager.ActiveFilters.Count, "Should start with 2 filters");
            
            // Act - Clear all filters (with backup)
            filterManager.ClearAllFilters(true);
            
            // Assert
            Assert.AreEqual(0, filterManager.ActiveFilters.Count, "Should have no active filters after clear");
            
            // Act - Restore from backup
            filterManager.RestoreFromBackup();
            
            // Assert
            Assert.AreEqual(2, filterManager.ActiveFilters.Count, "Should restore 2 filters from backup");
            Assert.IsTrue(filterManager.IsFilterActive(FilterType.Grayscale), "Grayscale should be restored");
            Assert.IsTrue(filterManager.IsFilterActive(FilterType.ContrastEnhancement), "Contrast should be restored");
        }
        
        /// <summary>
        /// Test session persistence (Requirement 4.9)
        /// </summary>
        [Test]
        public void TestSessionPersistence()
        {
            // Arrange
            var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.7f };
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.3f };
            
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
            
            // Act - Save session
            filterManager.SaveSession();
            
            // Clear current filters
            filterManager.ClearAllFilters(false);
            Assert.AreEqual(0, filterManager.ActiveFilters.Count, "Should have no filters after clear");
            
            // Load session
            filterManager.LoadSession();
            
            // Assert
            Assert.AreEqual(2, filterManager.ActiveFilters.Count, "Should restore 2 filters from session");
            
            var restoredGrayscale = filterManager.GetFilterParameters(FilterType.Grayscale);
            var restoredContrast = filterManager.GetFilterParameters(FilterType.ContrastEnhancement);
            
            Assert.AreEqual(0.7f, restoredGrayscale.intensity, 0.01f, "Grayscale intensity should be restored");
            Assert.AreEqual(0.3f, restoredContrast.intensity, 0.01f, "Contrast intensity should be restored");
        }
        
        /// <summary>
        /// Test filter settings JSON serialization and deserialization
        /// </summary>
        [Test]
        public void TestFilterSettingsSerialization()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale) { intensity = 0.8f });
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.6f });
            
            // Act - Save to JSON
            string json = filterManager.SaveFilterSettings();
            Assert.IsNotEmpty(json, "JSON should not be empty");
            
            // Clear filters
            filterManager.ClearAllFilters(false);
            Assert.AreEqual(0, filterManager.ActiveFilters.Count, "Should have no filters");
            
            // Load from JSON
            filterManager.LoadFilterSettings(json);
            
            // Assert
            Assert.AreEqual(2, filterManager.ActiveFilters.Count, "Should restore 2 filters from JSON");
            
            var grayscaleParams = filterManager.GetFilterParameters(FilterType.Grayscale);
            var contrastParams = filterManager.GetFilterParameters(FilterType.ContrastEnhancement);
            
            Assert.AreEqual(0.8f, grayscaleParams.intensity, 0.01f, "Grayscale intensity should match");
            Assert.AreEqual(0.6f, contrastParams.intensity, 0.01f, "Contrast intensity should match");
        }
        
        /// <summary>
        /// Test performance with multiple filters active
        /// </summary>
        [UnityTest]
        public IEnumerator TestMultipleFilterPerformance()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            
            // Act - Enable real-time preview
            filterManager.EnableRealTimePreview(true);
            
            // Wait for several frames to test performance
            float startTime = Time.realtimeSinceStartup;
            int frameCount = 0;
            
            while (frameCount < 60) // Test for 60 frames
            {
                yield return null;
                frameCount++;
            }
            
            float endTime = Time.realtimeSinceStartup;
            float averageFrameTime = (endTime - startTime) / frameCount;
            float fps = 1f / averageFrameTime;
            
            // Disable real-time preview
            filterManager.EnableRealTimePreview(false);
            
            // Assert
            Assert.Greater(fps, 30f, "Should maintain at least 30 FPS with multiple filters");
            
            var performanceMetrics = filterManager.GetPerformanceMetrics();
            Assert.AreEqual(3, performanceMetrics.activeFilterCount, "Should report 3 active filters");
            Assert.Less(performanceMetrics.memoryUsage, 100f, "Memory usage should be reasonable");
        }
        
        /// <summary>
        /// Test filter compatibility system
        /// </summary>
        [Test]
        public void TestFilterCompatibility()
        {
            // This test verifies that the compatibility system works
            // Note: Current implementation has EdgeDetection incompatible with Grayscale
            
            // Arrange & Act
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            
            // Assert - Both filters should be in the list, but compatibility rules should be applied
            Assert.AreEqual(2, filterManager.ActiveFilters.Count, "Both filters should be added to the list");
            
            // The actual compatibility behavior depends on the implementation
            // This test ensures the system doesn't crash when incompatible filters are applied
        }
        
        /// <summary>
        /// Test error handling for invalid filter operations
        /// </summary>
        [Test]
        public void TestErrorHandling()
        {
            // Test removing non-existent filter
            Assert.DoesNotThrow(() => filterManager.RemoveFilter(FilterType.ColorRange), 
                "Should not throw when removing non-existent filter");
            
            // Test toggling non-existent filter
            Assert.DoesNotThrow(() => filterManager.ToggleFilter(FilterType.ColorRange, true), 
                "Should not throw when toggling non-existent filter");
            
            // Test getting parameters for non-existent filter
            var params = filterManager.GetFilterParameters(FilterType.ColorRange);
            Assert.IsNull(params, "Should return null for non-existent filter parameters");
            
            // Test loading invalid JSON
            Assert.DoesNotThrow(() => filterManager.LoadFilterSettings("invalid json"), 
                "Should not throw when loading invalid JSON");
        }
    }
}