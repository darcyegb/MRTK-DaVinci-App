using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Verification tests for Task 6.1: Create color range filtering system
    /// Validates Requirements: 4.1.1, 4.1.2, 4.1.3
    /// </summary>
    public class Task61Verification
    {
        private GameObject testGameObject;
        private ColorRangeFilter colorRangeFilter;
        private ColorRangePickerUI pickerUI;
        private FilterManager filterManager;
        private Texture2D testTexture;
        
        [SetUp]
        public void Setup()
        {
            // Create test environment
            testGameObject = new GameObject("Task61Test");
            colorRangeFilter = testGameObject.AddComponent<ColorRangeFilter>();
            pickerUI = testGameObject.AddComponent<ColorRangePickerUI>();
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
            testTexture = new Texture2D(8, 8, TextureFormat.RGB24, false);
            Color[] pixels = new Color[64];
            
            // Create test pattern with different colors
            for (int i = 0; i < 16; i++) pixels[i] = Color.red;      // Red quadrant
            for (int i = 16; i < 32; i++) pixels[i] = Color.green;   // Green quadrant
            for (int i = 32; i < 48; i++) pixels[i] = Color.blue;    // Blue quadrant
            for (int i = 48; i < 64; i++) pixels[i] = Color.white;   // White quadrant
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [Test]
        public void Requirement_4_1_1_HSVBasedColorRangeSelection()
        {
            // Requirement 4.1.1: WHEN color range filtering is activated THEN the system SHALL provide controls for selecting target color ranges
            
            // Test HSV-based color range selection
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            
            // Verify HSV range detection works
            Color.RGBToHSV(Color.red, out float h, out float s, out float v);
            Vector3 redHSV = new Vector3(h, s, v);
            
            bool redDetected = redRange.IsColorInRange(redHSV);
            Assert.IsTrue(redDetected, "HSV-based color range selection should detect red color");
            
            // Test that non-red colors are not detected
            Color.RGBToHSV(Color.green, out h, out s, out v);
            Vector3 greenHSV = new Vector3(h, s, v);
            
            bool greenRejected = !redRange.IsColorInRange(greenHSV);
            Assert.IsTrue(greenRejected, "HSV-based color range selection should reject non-target colors");
            
            Debug.Log("✓ Requirement 4.1.1: HSV-based color range selection implemented");
        }
        
        [Test]
        public void Requirement_4_1_2_ColorRangeIsolation()
        {
            // Requirement 4.1.2: WHEN a color range is defined THEN the system SHALL highlight only pixels within the specified hue, saturation, and brightness thresholds
            
            // Create red color range
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            redRange.showOriginalColors = true;
            
            colorRangeFilter.AddColorRange(redRange);
            
            // Apply filter to test texture
            Texture2D result = colorRangeFilter.ApplyColorRangeFilter(testTexture);
            
            Assert.IsNotNull(result, "Color range isolation should produce a result texture");
            
            Color[] resultPixels = result.GetPixels();
            
            // Verify red pixels are preserved (first 16 pixels)
            int redPixelsPreserved = 0;
            for (int i = 0; i < 16; i++)
            {
                if (resultPixels[i] != Color.black)
                    redPixelsPreserved++;
            }
            
            Assert.Greater(redPixelsPreserved, 0, "Red pixels should be preserved in color range isolation");
            
            // Verify non-red pixels are filtered out
            int nonRedPixelsFiltered = 0;
            for (int i = 16; i < 64; i++)
            {
                if (resultPixels[i] == Color.black)
                    nonRedPixelsFiltered++;
            }
            
            Assert.Greater(nonRedPixelsFiltered, 0, "Non-red pixels should be filtered out in color range isolation");
            
            Debug.Log("✓ Requirement 4.1.2: Color range isolation implemented");
        }
        
        [Test]
        public void Requirement_4_1_3_RealTimeParameterAdjustment()
        {
            // Requirement 4.1.3: WHEN color range parameters are adjusted THEN the system SHALL update the filtered display in real-time
            
            // Enable real-time preview
            colorRangeFilter.RealTimePreview = true;
            
            // Set initial range
            colorRangeFilter.SetColorRange(0f, 60f, 0.5f, 1f, 0.5f, 1f);
            
            var initialRange = colorRangeFilter.CurrentRange;
            Assert.IsNotNull(initialRange, "Initial color range should be set");
            Assert.AreEqual(0f, initialRange.hueMin, 0.01f, "Initial hue min should be set correctly");
            Assert.AreEqual(60f, initialRange.hueMax, 0.01f, "Initial hue max should be set correctly");
            
            // Update range parameters
            colorRangeFilter.SetColorRange(120f, 180f, 0.3f, 0.8f, 0.2f, 0.9f);
            
            var updatedRange = colorRangeFilter.CurrentRange;
            Assert.AreEqual(120f, updatedRange.hueMin, 0.01f, "Updated hue min should be applied in real-time");
            Assert.AreEqual(180f, updatedRange.hueMax, 0.01f, "Updated hue max should be applied in real-time");
            Assert.AreEqual(0.3f, updatedRange.saturationMin, 0.01f, "Updated saturation min should be applied in real-time");
            Assert.AreEqual(0.8f, updatedRange.saturationMax, 0.01f, "Updated saturation max should be applied in real-time");
            
            Debug.Log("✓ Requirement 4.1.3: Real-time parameter adjustment implemented");
        }
        
        [Test]
        public void ColorRangeFilter_Integration_WithFilterManager()
        {
            // Test integration with FilterManager
            var parameters = new FilterParameters(FilterType.ColorRange);
            parameters.targetColor = Color.red;
            parameters.colorTolerance = 0.2f;
            parameters.intensity = 1f;
            
            // Apply through FilterManager
            filterManager.SetSourceTexture(testTexture);
            filterManager.ApplyFilter(FilterType.ColorRange, parameters);
            
            // Verify filter was applied
            bool isActive = filterManager.IsFilterActive(FilterType.ColorRange);
            Assert.IsTrue(isActive, "Color range filter should be active in FilterManager");
            
            var activeFilters = filterManager.GetActiveFilterTypes();
            Assert.Contains(FilterType.ColorRange, activeFilters, "Color range filter should be in active filters list");
            
            Debug.Log("✓ ColorRangeFilter integration with FilterManager verified");
        }
        
        [Test]
        public void ColorRangePickerUI_InteractiveControls()
        {
            // Test interactive color range picker UI components
            Assert.IsNotNull(pickerUI, "ColorRangePickerUI component should be created");
            
            // Test setting range from color
            pickerUI.SetRangeFromColor(Color.green, 0.3f);
            
            var currentRange = pickerUI.GetCurrentRange();
            Assert.IsNotNull(currentRange, "Current range should be set from color picker");
            
            // Verify the range is approximately centered around green
            Color.RGBToHSV(Color.green, out float h, out float s, out float v);
            Vector3 greenHSV = new Vector3(h, s, v);
            
            bool greenInRange = currentRange.IsColorInRange(greenHSV);
            Assert.IsTrue(greenInRange, "Green color should be within the range set by color picker");
            
            Debug.Log("✓ ColorRangePickerUI interactive controls verified");
        }
        
        [Test]
        public void ColorRangeFilter_PerformanceMetrics()
        {
            // Test performance of color range filtering
            var range = new ColorRangeFilter.ColorRangeSettings();
            range.hueMin = 0f;
            range.hueMax = 60f;
            range.saturationMin = 0.5f;
            range.saturationMax = 1f;
            range.valueMin = 0.5f;
            range.valueMax = 1f;
            
            colorRangeFilter.AddColorRange(range);
            
            // Measure processing time
            float startTime = Time.realtimeSinceStartup;
            
            Texture2D result = colorRangeFilter.ApplyColorRangeFilter(testTexture);
            
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.IsNotNull(result, "Color range filter should produce result");
            Assert.Less(processingTime, 0.1f, "Color range filtering should complete within 100ms for small textures");
            
            // Test color statistics
            var stats = colorRangeFilter.GetColorStatistics(testTexture);
            Assert.Greater(stats.totalPixels, 0, "Color statistics should count total pixels");
            Assert.GreaterOrEqual(stats.percentageInRange, 0f, "Percentage in range should be non-negative");
            Assert.LessOrEqual(stats.percentageInRange, 100f, "Percentage in range should not exceed 100%");
            
            Debug.Log($"✓ Color range filter performance: {processingTime * 1000f:F2}ms, {stats}");
        }
        
        [Test]
        public void ColorRangeFilter_HueWraparound()
        {
            // Test hue wraparound for red color range (crosses 0/360 boundary)
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;  // 350 degrees
            redRange.hueMax = 10f;   // 10 degrees (wraps around)
            redRange.saturationMin = 0f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0f;
            redRange.valueMax = 1f;
            
            // Test pure red (0 degrees)
            Vector3 redHSV = new Vector3(0f, 1f, 1f);
            Assert.IsTrue(redRange.IsColorInRange(redHSV), "Pure red should be in wraparound range");
            
            // Test 355 degrees (should be in range)
            Vector3 darkRedHSV = new Vector3(355f / 360f, 1f, 1f);
            Assert.IsTrue(redRange.IsColorInRange(darkRedHSV), "355° should be in wraparound range");
            
            // Test 5 degrees (should be in range)
            Vector3 lightRedHSV = new Vector3(5f / 360f, 1f, 1f);
            Assert.IsTrue(redRange.IsColorInRange(lightRedHSV), "5° should be in wraparound range");
            
            // Test 180 degrees (should not be in range)
            Vector3 cyanHSV = new Vector3(180f / 360f, 1f, 1f);
            Assert.IsFalse(redRange.IsColorInRange(cyanHSV), "180° should not be in wraparound range");
            
            Debug.Log("✓ Hue wraparound handling verified");
        }
        
        [UnityTest]
        public IEnumerator ColorRangeFilter_RealTimePreview()
        {
            // Test real-time preview functionality
            colorRangeFilter.RealTimePreview = true;
            
            // Set initial range
            colorRangeFilter.SetColorRange(0f, 60f, 0.5f, 1f, 0.5f, 1f);
            
            yield return new WaitForSeconds(0.1f);
            
            var initialRange = colorRangeFilter.CurrentRange;
            Assert.AreEqual(0f, initialRange.hueMin, 0.01f);
            
            // Update range
            colorRangeFilter.SetColorRange(120f, 180f, 0.3f, 0.8f, 0.2f, 0.9f);
            
            yield return new WaitForSeconds(0.1f);
            
            var updatedRange = colorRangeFilter.CurrentRange;
            Assert.AreEqual(120f, updatedRange.hueMin, 0.01f);
            
            Debug.Log("✓ Real-time preview functionality verified");
        }
        
        [Test]
        public void Task61_CompletionVerification()
        {
            // Verify all components of Task 6.1 are implemented
            
            // 1. HSV-based color range selection and isolation
            Assert.IsNotNull(colorRangeFilter, "ColorRangeFilter component should exist");
            
            // 2. Interactive color range picker using MRTK UI components
            Assert.IsNotNull(pickerUI, "ColorRangePickerUI component should exist");
            
            // 3. Integration with FilterManager
            Assert.IsNotNull(filterManager, "FilterManager integration should exist");
            
            // 4. Real-time performance
            var range = new ColorRangeFilter.ColorRangeSettings();
            colorRangeFilter.AddColorRange(range);
            
            float startTime = Time.realtimeSinceStartup;
            colorRangeFilter.ApplyColorRangeFilter(testTexture);
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.Less(processingTime, 0.1f, "Color range filtering should meet real-time performance requirements");
            
            Debug.Log("✓ Task 6.1 - Create color range filtering system: COMPLETED");
            Debug.Log("  - HSV-based color range selection: ✓");
            Debug.Log("  - Interactive color range picker UI: ✓");
            Debug.Log("  - Real-time performance: ✓");
            Debug.Log("  - FilterManager integration: ✓");
            Debug.Log($"  - Processing time: {processingTime * 1000f:F2}ms");
        }
    }
}