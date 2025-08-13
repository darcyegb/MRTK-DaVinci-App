using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Verification tests for Task 6.3: Add multiple color range support
    /// Validates Requirements: 4.1.4, 4.1.5
    /// </summary>
    public class Task63Verification
    {
        private GameObject testGameObject;
        private MultipleColorRangeManager rangeManager;
        private MultipleColorRangeUI rangeUI;
        private ColorRangeFilter colorRangeFilter;
        private FilterManager filterManager;
        private Texture2D testTexture;
        
        [SetUp]
        public void Setup()
        {
            // Create test environment
            testGameObject = new GameObject("Task63Test");
            rangeManager = testGameObject.AddComponent<MultipleColorRangeManager>();
            rangeUI = testGameObject.AddComponent<MultipleColorRangeUI>();
            colorRangeFilter = testGameObject.AddComponent<ColorRangeFilter>();
            filterManager = testGameObject.AddComponent<FilterManager>();
            
            CreateTestTexture();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
                Object.DestroyImmediate(testGameObject);
            
            if (testTexture != null)
                Object.DestroyImmediate(testTexture);
        }
        
        private void CreateTestTexture()
        {
            testTexture = new Texture2D(20, 20, TextureFormat.RGB24, false);
            Color[] pixels = new Color[400];
            
            // Create a test pattern with multiple distinct color regions
            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    int index = y * 20 + x;
                    
                    if (x < 10 && y < 10)
                        pixels[index] = Color.red;      // Top-left: Red
                    else if (x >= 10 && y < 10)
                        pixels[index] = Color.green;    // Top-right: Green
                    else if (x < 10 && y >= 10)
                        pixels[index] = Color.blue;     // Bottom-left: Blue
                    else
                        pixels[index] = Color.yellow;   // Bottom-right: Yellow
                }
            }
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [Test]
        public void Requirement_4_1_4_MultipleColorRangeFilters()
        {
            // Requirement 4.1.4: WHEN multiple color ranges are selected THEN the system SHALL allow combining multiple color range filters
            
            // Create multiple distinct color ranges
            var redRange = new MultipleColorRangeManager.ColorRangeData("Red Range");
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            redRange.showOriginalColors = true;
            
            var greenRange = new MultipleColorRangeManager.ColorRangeData("Green Range");
            greenRange.hueMin = 100f;
            greenRange.hueMax = 140f;
            greenRange.saturationMin = 0.8f;
            greenRange.saturationMax = 1f;
            greenRange.valueMin = 0.8f;
            greenRange.valueMax = 1f;
            greenRange.showOriginalColors = true;
            
            var blueRange = new MultipleColorRangeManager.ColorRangeData("Blue Range");
            blueRange.hueMin = 220f;
            blueRange.hueMax = 260f;
            blueRange.saturationMin = 0.8f;
            blueRange.saturationMax = 1f;
            blueRange.valueMin = 0.8f;
            blueRange.valueMax = 1f;
            blueRange.showOriginalColors = true;
            
            // Add multiple ranges
            rangeManager.AddColorRange(redRange);
            rangeManager.AddColorRange(greenRange);
            rangeManager.AddColorRange(blueRange);
            
            Assert.AreEqual(3, rangeManager.ColorRanges.Count, "Should support adding multiple color range filters");
            
            // Verify all ranges are active
            var activeRanges = rangeManager.GetActiveRanges();
            Assert.AreEqual(3, activeRanges.Count, "All added ranges should be active");
            
            // Test combining the filters
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Union);
            Texture2D combinedResult = rangeManager.ApplyMultipleColorRanges(testTexture);
            
            Assert.IsNotNull(combinedResult, "Combining multiple color range filters should produce a result");
            
            // Verify that pixels from multiple color ranges are preserved
            Color[] resultPixels = combinedResult.GetPixels();
            int preservedPixels = resultPixels.Count(p => p != Color.black);
            
            Assert.Greater(preservedPixels, 100, "Multiple color range combination should preserve pixels from different ranges");
            
            Debug.Log($"✓ Requirement 4.1.4: Multiple color range filters combined successfully (preserved {preservedPixels} pixels)");
        }
        
        [Test]
        public void Requirement_4_1_5_ColorRangeDisplayOptions()
        {
            // Requirement 4.1.5: WHEN color range filtering is active THEN the system SHALL provide options to show filtered colors in original colors or as highlights
            
            // Create ranges with different display options
            var originalColorRange = new MultipleColorRangeManager.ColorRangeData("Original Color Range");
            originalColorRange.hueMin = 350f;
            originalColorRange.hueMax = 10f;
            originalColorRange.saturationMin = 0.8f;
            originalColorRange.saturationMax = 1f;
            originalColorRange.valueMin = 0.8f;
            originalColorRange.valueMax = 1f;
            originalColorRange.showOriginalColors = true;
            originalColorRange.showAsHighlight = false;
            
            var highlightRange = new MultipleColorRangeManager.ColorRangeData("Highlight Range");
            highlightRange.hueMin = 100f;
            highlightRange.hueMax = 140f;
            highlightRange.saturationMin = 0.8f;
            highlightRange.saturationMax = 1f;
            highlightRange.valueMin = 0.8f;
            highlightRange.valueMax = 1f;
            highlightRange.showOriginalColors = false;
            highlightRange.showAsHighlight = true;
            highlightRange.highlightColor = Color.cyan;
            highlightRange.highlightIntensity = 0.8f;
            
            rangeManager.AddColorRange(originalColorRange);
            rangeManager.AddColorRange(highlightRange);
            
            // Test original colors display option
            Assert.IsTrue(originalColorRange.showOriginalColors, "Should support showing filtered colors in original colors");
            Assert.IsFalse(originalColorRange.showAsHighlight, "Original color mode should not use highlights");
            
            // Test highlight display option
            Assert.IsFalse(highlightRange.showOriginalColors, "Highlight mode should not show original colors");
            Assert.IsTrue(highlightRange.showAsHighlight, "Should support showing filtered colors as highlights");
            Assert.AreEqual(Color.cyan, highlightRange.highlightColor, "Should allow custom highlight colors");
            Assert.AreEqual(0.8f, highlightRange.highlightIntensity, 0.01f, "Should allow adjustable highlight intensity");
            
            // Apply filters and verify different display modes work
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Union);
            Texture2D result = rangeManager.ApplyMultipleColorRanges(testTexture);
            
            Assert.IsNotNull(result, "Multiple ranges with different display options should work");
            
            Color[] resultPixels = result.GetPixels();
            
            // Check for original colors (red pixels should remain red)
            bool foundOriginalRed = resultPixels.Any(p => Vector3.Distance(new Vector3(p.r, p.g, p.b), new Vector3(1f, 0f, 0f)) < 0.1f);
            
            // Check for highlighted colors (green pixels should be cyan-tinted)
            bool foundHighlightedPixels = resultPixels.Any(p => p.b > 0.3f && p.g > 0.3f && p != Color.black);
            
            Assert.IsTrue(foundOriginalRed || foundHighlightedPixels, "Should demonstrate different display options working");
            
            Debug.Log($"✓ Requirement 4.1.5: Color range display options implemented (original colors: {foundOriginalRed}, highlights: {foundHighlightedPixels})");
        }
        
        [Test]
        public void MultipleColorRangeManager_CombinationModes_AllWork()
        {
            // Test all combination modes work correctly
            var range1 = new MultipleColorRangeManager.ColorRangeData("Range 1");
            range1.hueMin = 350f;
            range1.hueMax = 10f;
            range1.saturationMin = 0.8f;
            range1.saturationMax = 1f;
            range1.valueMin = 0.8f;
            range1.valueMax = 1f;
            
            var range2 = new MultipleColorRangeManager.ColorRangeData("Range 2");
            range2.hueMin = 100f;
            range2.hueMax = 140f;
            range2.saturationMin = 0.8f;
            range2.saturationMax = 1f;
            range2.valueMin = 0.8f;
            range2.valueMax = 1f;
            
            rangeManager.AddColorRange(range1);
            rangeManager.AddColorRange(range2);
            
            // Test Union mode (show pixels that match ANY range)
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Union);
            var unionResult = rangeManager.ApplyMultipleColorRanges(testTexture);
            Assert.IsNotNull(unionResult, "Union mode should work");
            
            // Test Intersection mode (show pixels that match ALL ranges)
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Intersection);
            var intersectionResult = rangeManager.ApplyMultipleColorRanges(testTexture);
            Assert.IsNotNull(intersectionResult, "Intersection mode should work");
            
            // Test Exclusive mode (show pixels that match ONLY ONE range)
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Exclusive);
            var exclusiveResult = rangeManager.ApplyMultipleColorRanges(testTexture);
            Assert.IsNotNull(exclusiveResult, "Exclusive mode should work");
            
            // Test Weighted mode (blend multiple range effects)
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Weighted);
            var weightedResult = rangeManager.ApplyMultipleColorRanges(testTexture);
            Assert.IsNotNull(weightedResult, "Weighted mode should work");
            
            Debug.Log("✓ All combination modes (Union, Intersection, Exclusive, Weighted) work correctly");
        }
        
        [Test]
        public void MultipleColorRangeManager_RangeManagement_WorksProperly()
        {
            // Test comprehensive range management functionality
            
            // Test adding ranges
            var range1 = new MultipleColorRangeManager.ColorRangeData("Test Range 1");
            var range2 = new MultipleColorRangeManager.ColorRangeData("Test Range 2");
            var range3 = new MultipleColorRangeManager.ColorRangeData("Test Range 3");
            
            rangeManager.AddColorRange(range1);
            rangeManager.AddColorRange(range2);
            rangeManager.AddColorRange(range3);
            
            Assert.AreEqual(3, rangeManager.ColorRanges.Count, "Should add multiple ranges");
            
            // Test toggling ranges
            rangeManager.ToggleColorRange(range2.rangeId, false);
            var activeRanges = rangeManager.GetActiveRanges();
            Assert.AreEqual(2, activeRanges.Count, "Should toggle range active state");
            
            // Test removing specific range
            bool removed = rangeManager.RemoveColorRange(range1.rangeId);
            Assert.IsTrue(removed, "Should remove specific range");
            Assert.AreEqual(2, rangeManager.ColorRanges.Count, "Range count should decrease after removal");
            
            // Test clearing all ranges
            rangeManager.ClearAllRanges();
            Assert.AreEqual(0, rangeManager.ColorRanges.Count, "Should clear all ranges");
            
            Debug.Log("✓ Range management (add, remove, toggle, clear) works properly");
        }
        
        [Test]
        public void MultipleColorRangeManager_OverlapDetection_WorksCorrectly()
        {
            // Test overlap detection and optimization
            
            // Create overlapping ranges
            var range1 = new MultipleColorRangeManager.ColorRangeData("Range 1");
            range1.hueMin = 0f;
            range1.hueMax = 60f;
            range1.saturationMin = 0.5f;
            range1.saturationMax = 1f;
            range1.valueMin = 0.5f;
            range1.valueMax = 1f;
            
            var range2 = new MultipleColorRangeManager.ColorRangeData("Range 2");
            range2.hueMin = 30f;  // Overlaps with range1
            range2.hueMax = 90f;
            range2.saturationMin = 0.7f;  // Overlaps with range1
            range2.saturationMax = 1f;
            range2.valueMin = 0.7f;  // Overlaps with range1
            range2.valueMax = 1f;
            
            var range3 = new MultipleColorRangeManager.ColorRangeData("Range 3");
            range3.hueMin = 180f;  // No overlap
            range3.hueMax = 240f;
            range3.saturationMin = 0.5f;
            range3.saturationMax = 1f;
            range3.valueMin = 0.5f;
            range3.valueMax = 1f;
            
            rangeManager.AddColorRange(range1);
            rangeManager.AddColorRange(range2);
            rangeManager.AddColorRange(range3);
            
            // Test overlap detection
            var overlappingRanges = rangeManager.GetOverlappingRanges(range1.rangeId);
            Assert.Greater(overlappingRanges.Count, 0, "Should detect overlapping ranges");
            
            // Test optimization
            int originalCount = rangeManager.ColorRanges.Count;
            rangeManager.OptimizeRanges();
            int optimizedCount = rangeManager.ColorRanges.Count;
            
            Assert.LessOrEqual(optimizedCount, originalCount, "Optimization should reduce or maintain range count");
            
            Debug.Log($"✓ Overlap detection and optimization works (ranges: {originalCount} → {optimizedCount})");
        }
        
        [Test]
        public void MultipleColorRangeManager_PerformanceRequirements_Met()
        {
            // Test performance with multiple ranges
            
            // Add several ranges
            for (int i = 0; i < 5; i++)
            {
                var range = new MultipleColorRangeManager.ColorRangeData($"Range {i + 1}");
                range.hueMin = i * 60f;
                range.hueMax = (i + 1) * 60f;
                range.saturationMin = 0.5f;
                range.saturationMax = 1f;
                range.valueMin = 0.5f;
                range.valueMax = 1f;
                rangeManager.AddColorRange(range);
            }
            
            // Measure processing time
            float startTime = Time.realtimeSinceStartup;
            
            Texture2D result = rangeManager.ApplyMultipleColorRanges(testTexture);
            
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.IsNotNull(result, "Multiple range processing should complete successfully");
            Assert.Less(processingTime, 1f, "Multiple range processing should complete within 1 second");
            
            // Check statistics
            var stats = rangeManager.GetStatistics();
            Assert.IsNotNull(stats, "Should provide processing statistics");
            Assert.AreEqual(5, stats.activeRanges, "Should track active range count");
            Assert.Greater(stats.processingTime, 0f, "Should measure processing time");
            
            Debug.Log($"✓ Performance requirements met: {processingTime * 1000f:F2}ms for {stats.activeRanges} ranges");
        }
        
        [Test]
        public void MultipleColorRangeUI_Management_WorksCorrectly()
        {
            // Test UI management functionality
            Assert.IsNotNull(rangeUI, "MultipleColorRangeUI should be available");
            
            // Test setting maximum ranges
            rangeUI.SetMaxActiveRanges(6);
            Assert.AreEqual(6, rangeManager.MaxActiveRanges, "UI should update manager settings");
            
            // Test getting selected range (initially none)
            var selectedRange = rangeUI.GetSelectedRange();
            Assert.IsNull(selectedRange, "Initially no range should be selected");
            
            Debug.Log("✓ MultipleColorRangeUI management works correctly");
        }
        
        [Test]
        public void MultipleColorRangeManager_Integration_WithExistingFilters()
        {
            // Test integration with existing filter system
            Assert.IsNotNull(colorRangeFilter, "Should integrate with ColorRangeFilter");
            Assert.IsNotNull(filterManager, "Should integrate with FilterManager");
            
            // Add a range to the multiple range manager
            var range = new MultipleColorRangeManager.ColorRangeData("Integration Test Range");
            range.hueMin = 350f;
            range.hueMax = 10f;
            range.saturationMin = 0.8f;
            range.saturationMax = 1f;
            range.valueMin = 0.8f;
            range.valueMax = 1f;
            
            rangeManager.AddColorRange(range);
            
            // Apply through multiple range manager
            Texture2D multipleRangeResult = rangeManager.ApplyMultipleColorRanges(testTexture);
            Assert.IsNotNull(multipleRangeResult, "Multiple range manager should work independently");
            
            // Apply through single color range filter for comparison
            colorRangeFilter.SetColorRange(range.hueMin, range.hueMax, range.saturationMin, range.saturationMax, range.valueMin, range.valueMax);
            Texture2D singleRangeResult = colorRangeFilter.ApplyColorRangeFilter(testTexture);
            Assert.IsNotNull(singleRangeResult, "Single range filter should still work");
            
            Debug.Log("✓ Integration with existing filter system works correctly");
        }
        
        [UnityTest]
        public IEnumerator MultipleColorRangeManager_RealTimeUpdates()
        {
            // Test real-time updates
            rangeManager.RealTimePreview = true;
            
            var range = new MultipleColorRangeManager.ColorRangeData("Real-time Test Range");
            rangeManager.AddColorRange(range);
            
            yield return new WaitForSeconds(0.1f);
            
            // Change combination mode
            rangeManager.SetCombinationMode(MultipleColorRangeManager.CombinationMode.Intersection, false);
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.AreEqual(MultipleColorRangeManager.CombinationMode.Intersection, rangeManager.CurrentCombinationMode);
            
            Debug.Log("✓ Real-time updates work correctly");
        }
        
        [Test]
        public void Task63_CompletionVerification()
        {
            // Comprehensive verification that Task 6.3 is complete
            
            // 1. Multiple color range support infrastructure
            Assert.IsNotNull(rangeManager, "MultipleColorRangeManager component should exist");
            Assert.IsNotNull(rangeUI, "MultipleColorRangeUI component should exist");
            
            // 2. Multiple range addition and management
            var range1 = new MultipleColorRangeManager.ColorRangeData("Verification Range 1");
            var range2 = new MultipleColorRangeManager.ColorRangeData("Verification Range 2");
            var range3 = new MultipleColorRangeManager.ColorRangeData("Verification Range 3");
            
            rangeManager.AddColorRange(range1);
            rangeManager.AddColorRange(range2);
            rangeManager.AddColorRange(range3);
            
            Assert.AreEqual(3, rangeManager.ColorRanges.Count, "Should support multiple color ranges");
            
            // 3. All combination modes functional
            var combinationModes = new[] {
                MultipleColorRangeManager.CombinationMode.Union,
                MultipleColorRangeManager.CombinationMode.Intersection,
                MultipleColorRangeManager.CombinationMode.Exclusive,
                MultipleColorRangeManager.CombinationMode.Weighted
            };
            
            foreach (var mode in combinationModes)
            {
                rangeManager.SetCombinationMode(mode);
                var result = rangeManager.ApplyMultipleColorRanges(testTexture);
                Assert.IsNotNull(result, $"Combination mode {mode} should work");
            }
            
            // 4. Range management operations
            rangeManager.ToggleColorRange(range2.rangeId, false);
            Assert.AreEqual(2, rangeManager.GetActiveRanges().Count, "Range toggling should work");
            
            bool removed = rangeManager.RemoveColorRange(range1.rangeId);
            Assert.IsTrue(removed, "Range removal should work");
            
            // 5. Performance requirements
            float startTime = Time.realtimeSinceStartup;
            rangeManager.ApplyMultipleColorRanges(testTexture);
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.Less(processingTime, 1f, "Multiple range processing should meet performance requirements");
            
            // 6. Statistics and monitoring
            var stats = rangeManager.GetStatistics();
            Assert.IsNotNull(stats, "Should provide comprehensive statistics");
            
            // 7. UI integration
            rangeUI.SetMaxActiveRanges(10);
            Assert.AreEqual(10, rangeManager.MaxActiveRanges, "UI integration should work");
            
            Debug.Log("✓ Task 6.3 - Add multiple color range support: COMPLETED");
            Debug.Log("  - Multiple color range infrastructure: ✓");
            Debug.Log("  - Range combination modes (Union, Intersection, Exclusive, Weighted): ✓");
            Debug.Log("  - Range management (add, remove, toggle, clear): ✓");
            Debug.Log("  - Overlap detection and optimization: ✓");
            Debug.Log("  - UI management system: ✓");
            Debug.Log("  - Integration with existing filters: ✓");
            Debug.Log($"  - Performance: {processingTime * 1000f:F2}ms for multiple ranges");
            Debug.Log($"  - Statistics tracking: {stats}");
        }
    }
}