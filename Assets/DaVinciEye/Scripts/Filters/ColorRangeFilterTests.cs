using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Comprehensive tests for color range filtering system
    /// Tests Requirements: 4.1.1, 4.1.2, 4.1.3
    /// </summary>
    public class ColorRangeFilterTests
    {
        private GameObject testGameObject;
        private ColorRangeFilter colorRangeFilter;
        private Texture2D testTexture;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject with ColorRangeFilter
            testGameObject = new GameObject("ColorRangeFilterTest");
            colorRangeFilter = testGameObject.AddComponent<ColorRangeFilter>();
            
            // Create test texture with known colors
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
        
        /// <summary>
        /// Create a test texture with specific colors for testing
        /// </summary>
        private void CreateTestTexture()
        {
            testTexture = new Texture2D(4, 4, TextureFormat.RGB24, false);
            
            // Create a 4x4 texture with different colors
            Color[] pixels = new Color[16];
            
            // Red pixels (H=0, S=1, V=1)
            pixels[0] = Color.red;
            pixels[1] = Color.red;
            pixels[2] = Color.red;
            pixels[3] = Color.red;
            
            // Green pixels (H=120, S=1, V=1)
            pixels[4] = Color.green;
            pixels[5] = Color.green;
            pixels[6] = Color.green;
            pixels[7] = Color.green;
            
            // Blue pixels (H=240, S=1, V=1)
            pixels[8] = Color.blue;
            pixels[9] = Color.blue;
            pixels[10] = Color.blue;
            pixels[11] = Color.blue;
            
            // White pixels (H=0, S=0, V=1)
            pixels[12] = Color.white;
            pixels[13] = Color.white;
            pixels[14] = Color.white;
            pixels[15] = Color.white;
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [Test]
        public void ColorRangeFilter_Initialization_SetsDefaultValues()
        {
            // Test that the filter initializes with proper default values
            Assert.IsNotNull(colorRangeFilter);
            Assert.IsNotNull(colorRangeFilter.ActiveRanges);
            Assert.AreEqual(0, colorRangeFilter.ActiveRanges.Count);
            Assert.IsTrue(colorRangeFilter.RealTimePreview);
        }
        
        [Test]
        public void ColorRangeSettings_IsColorInRange_DetectsRedCorrectly()
        {
            // Test HSV-based color range detection for red color
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;  // Red hue range (wraps around 0)
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            
            // Convert red color to HSV
            Color.RGBToHSV(Color.red, out float h, out float s, out float v);
            Vector3 redHSV = new Vector3(h, s, v);
            
            bool isInRange = redRange.IsColorInRange(redHSV);
            Assert.IsTrue(isInRange, "Red color should be detected in red range");
        }
        
        [Test]
        public void ColorRangeSettings_IsColorInRange_RejectsWrongColor()
        {
            // Test that blue color is not detected in red range
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            
            // Convert blue color to HSV
            Color.RGBToHSV(Color.blue, out float h, out float s, out float v);
            Vector3 blueHSV = new Vector3(h, s, v);
            
            bool isInRange = redRange.IsColorInRange(blueHSV);
            Assert.IsFalse(isInRange, "Blue color should not be detected in red range");
        }
        
        [Test]
        public void ColorRangeFilter_SetColorRange_UpdatesCurrentRange()
        {
            // Test setting color range parameters
            float hueMin = 100f, hueMax = 140f;
            float satMin = 0.5f, satMax = 1f;
            float valueMin = 0.3f, valueMax = 0.9f;
            
            colorRangeFilter.SetColorRange(hueMin, hueMax, satMin, satMax, valueMin, valueMax);
            
            var currentRange = colorRangeFilter.CurrentRange;
            Assert.IsNotNull(currentRange);
            Assert.AreEqual(hueMin, currentRange.hueMin, 0.01f);
            Assert.AreEqual(hueMax, currentRange.hueMax, 0.01f);
            Assert.AreEqual(satMin, currentRange.saturationMin, 0.01f);
            Assert.AreEqual(satMax, currentRange.saturationMax, 0.01f);
            Assert.AreEqual(valueMin, currentRange.valueMin, 0.01f);
            Assert.AreEqual(valueMax, currentRange.valueMax, 0.01f);
        }
        
        [Test]
        public void ColorRangeFilter_SetColorRangeFromColor_CreatesCorrectRange()
        {
            // Test creating range from target color with tolerance
            Color targetColor = Color.green;
            float tolerance = 0.2f;
            
            colorRangeFilter.SetColorRangeFromColor(targetColor, tolerance);
            
            var currentRange = colorRangeFilter.CurrentRange;
            Assert.IsNotNull(currentRange);
            
            // Verify that the range is centered around green color
            Color.RGBToHSV(targetColor, out float h, out float s, out float v);
            float expectedHue = h * 360f;
            
            // Check that the range includes the target color
            Vector3 targetHSV = new Vector3(h, s, v);
            bool targetInRange = currentRange.IsColorInRange(targetHSV);
            Assert.IsTrue(targetInRange, "Target color should be within created range");
        }
        
        [Test]
        public void ColorRangeFilter_AddColorRange_IncreasesActiveRanges()
        {
            // Test adding color ranges to active list
            var range1 = new ColorRangeFilter.ColorRangeSettings();
            range1.rangeName = "Test Range 1";
            
            var range2 = new ColorRangeFilter.ColorRangeSettings();
            range2.rangeName = "Test Range 2";
            
            Assert.AreEqual(0, colorRangeFilter.ActiveRanges.Count);
            
            colorRangeFilter.AddColorRange(range1);
            Assert.AreEqual(1, colorRangeFilter.ActiveRanges.Count);
            
            colorRangeFilter.AddColorRange(range2);
            Assert.AreEqual(2, colorRangeFilter.ActiveRanges.Count);
            
            // Verify ranges have unique IDs
            Assert.AreNotEqual(range1.rangeId, range2.rangeId);
        }
        
        [Test]
        public void ColorRangeFilter_RemoveColorRange_DecreasesActiveRanges()
        {
            // Test removing color ranges
            var range = new ColorRangeFilter.ColorRangeSettings();
            range.rangeName = "Test Range";
            
            colorRangeFilter.AddColorRange(range);
            Assert.AreEqual(1, colorRangeFilter.ActiveRanges.Count);
            
            bool removed = colorRangeFilter.RemoveColorRange(range.rangeId);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, colorRangeFilter.ActiveRanges.Count);
            
            // Test removing non-existent range
            bool removedAgain = colorRangeFilter.RemoveColorRange(range.rangeId);
            Assert.IsFalse(removedAgain);
        }
        
        [Test]
        public void ColorRangeFilter_ClearAllRanges_RemovesAllRanges()
        {
            // Add multiple ranges
            for (int i = 0; i < 3; i++)
            {
                var range = new ColorRangeFilter.ColorRangeSettings();
                range.rangeName = $"Test Range {i}";
                colorRangeFilter.AddColorRange(range);
            }
            
            Assert.AreEqual(3, colorRangeFilter.ActiveRanges.Count);
            
            colorRangeFilter.ClearAllRanges();
            Assert.AreEqual(0, colorRangeFilter.ActiveRanges.Count);
        }
        
        [Test]
        public void ColorRangeFilter_ToggleColorRange_ChangesActiveState()
        {
            // Test toggling range active state
            var range = new ColorRangeFilter.ColorRangeSettings();
            range.rangeName = "Test Range";
            range.isActive = true;
            
            colorRangeFilter.AddColorRange(range);
            
            // Toggle off
            colorRangeFilter.ToggleColorRange(range.rangeId, false);
            Assert.IsFalse(colorRangeFilter.ActiveRanges[0].isActive);
            
            // Toggle on
            colorRangeFilter.ToggleColorRange(range.rangeId, true);
            Assert.IsTrue(colorRangeFilter.ActiveRanges[0].isActive);
        }
        
        [Test]
        public void ColorRangeFilter_ApplyColorRangeFilter_ProcessesTextureCorrectly()
        {
            // Test applying color range filter to texture
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            redRange.showOriginalColors = true;
            
            colorRangeFilter.AddColorRange(redRange);
            
            Texture2D result = colorRangeFilter.ApplyColorRangeFilter(testTexture);
            
            Assert.IsNotNull(result);
            Assert.AreEqual(testTexture.width, result.width);
            Assert.AreEqual(testTexture.height, result.height);
            
            // Check that red pixels are preserved and others are black
            Color[] resultPixels = result.GetPixels();
            
            // First 4 pixels should be red (preserved)
            for (int i = 0; i < 4; i++)
            {
                Assert.AreNotEqual(Color.black, resultPixels[i], $"Red pixel {i} should be preserved");
            }
            
            // Other pixels should be black (filtered out)
            for (int i = 4; i < 16; i++)
            {
                Assert.AreEqual(Color.black, resultPixels[i], $"Non-red pixel {i} should be filtered to black");
            }
        }
        
        [Test]
        public void ColorRangeFilter_GetColorStatistics_ReturnsCorrectStats()
        {
            // Test color statistics calculation
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            
            colorRangeFilter.AddColorRange(redRange);
            
            var stats = colorRangeFilter.GetColorStatistics(testTexture);
            
            Assert.AreEqual(16, stats.totalPixels);
            Assert.AreEqual(4, stats.pixelsInRange); // 4 red pixels out of 16 total
            Assert.AreEqual(25f, stats.percentageInRange, 0.1f); // 25% of pixels are red
        }
        
        [UnityTest]
        public IEnumerator ColorRangeFilter_RealTimePreview_UpdatesInRealTime()
        {
            // Test real-time preview functionality
            colorRangeFilter.RealTimePreview = true;
            
            // Set initial range
            colorRangeFilter.SetColorRange(0f, 60f, 0.5f, 1f, 0.5f, 1f);
            
            yield return new WaitForSeconds(0.1f);
            
            // Change range parameters
            colorRangeFilter.SetColorRange(120f, 180f, 0.3f, 0.8f, 0.2f, 0.9f);
            
            yield return new WaitForSeconds(0.1f);
            
            // Verify that current range was updated
            var currentRange = colorRangeFilter.CurrentRange;
            Assert.AreEqual(120f, currentRange.hueMin, 0.01f);
            Assert.AreEqual(180f, currentRange.hueMax, 0.01f);
        }
        
        [Test]
        public void ColorRangeSettings_HueWraparound_HandlesCorrectly()
        {
            // Test hue wraparound for red color range (350-10 degrees)
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0f;
            redRange.valueMax = 1f;
            
            // Test colors that should be in range
            Vector3 redHSV = new Vector3(0f, 1f, 1f); // Pure red (0 degrees)
            Vector3 darkRedHSV = new Vector3(355f / 360f, 1f, 1f); // Dark red (355 degrees)
            Vector3 lightRedHSV = new Vector3(5f / 360f, 1f, 1f); // Light red (5 degrees)
            
            Assert.IsTrue(redRange.IsColorInRange(redHSV), "Pure red should be in wraparound range");
            Assert.IsTrue(redRange.IsColorInRange(darkRedHSV), "Dark red should be in wraparound range");
            Assert.IsTrue(redRange.IsColorInRange(lightRedHSV), "Light red should be in wraparound range");
            
            // Test color that should not be in range
            Vector3 greenHSV = new Vector3(120f / 360f, 1f, 1f); // Green (120 degrees)
            Assert.IsFalse(redRange.IsColorInRange(greenHSV), "Green should not be in red wraparound range");
        }
        
        [Test]
        public void ColorRangeFilter_MultipleRanges_CombinesCorrectly()
        {
            // Test combining multiple color ranges
            var redRange = new ColorRangeFilter.ColorRangeSettings();
            redRange.hueMin = 350f;
            redRange.hueMax = 10f;
            redRange.saturationMin = 0.8f;
            redRange.saturationMax = 1f;
            redRange.valueMin = 0.8f;
            redRange.valueMax = 1f;
            redRange.rangeName = "Red Range";
            
            var greenRange = new ColorRangeFilter.ColorRangeSettings();
            greenRange.hueMin = 100f;
            greenRange.hueMax = 140f;
            greenRange.saturationMin = 0.8f;
            greenRange.saturationMax = 1f;
            greenRange.valueMin = 0.8f;
            greenRange.valueMax = 1f;
            greenRange.rangeName = "Green Range";
            
            colorRangeFilter.AddColorRange(redRange);
            colorRangeFilter.AddColorRange(greenRange);
            
            Texture2D result = colorRangeFilter.ApplyColorRangeFilter(testTexture);
            
            Assert.IsNotNull(result);
            
            Color[] resultPixels = result.GetPixels();
            
            // Both red and green pixels should be preserved
            // Red pixels (0-3) should not be black
            for (int i = 0; i < 4; i++)
            {
                Assert.AreNotEqual(Color.black, resultPixels[i], $"Red pixel {i} should be preserved");
            }
            
            // Green pixels (4-7) should not be black
            for (int i = 4; i < 8; i++)
            {
                Assert.AreNotEqual(Color.black, resultPixels[i], $"Green pixel {i} should be preserved");
            }
            
            // Blue and white pixels should be black
            for (int i = 8; i < 16; i++)
            {
                Assert.AreEqual(Color.black, resultPixels[i], $"Non-target pixel {i} should be filtered to black");
            }
        }
        
        [Test]
        public void ColorRangeFilter_HighlightMode_AppliesHighlightCorrectly()
        {
            // Test highlight mode functionality
            var range = new ColorRangeFilter.ColorRangeSettings();
            range.hueMin = 350f;
            range.hueMax = 10f;
            range.saturationMin = 0.8f;
            range.saturationMax = 1f;
            range.valueMin = 0.8f;
            range.valueMax = 1f;
            range.showAsHighlight = true;
            range.showOriginalColors = false;
            range.highlightColor = Color.yellow;
            range.highlightIntensity = 0.5f;
            
            colorRangeFilter.AddColorRange(range);
            
            Texture2D result = colorRangeFilter.ApplyColorRangeFilter(testTexture);
            
            Assert.IsNotNull(result);
            
            Color[] resultPixels = result.GetPixels();
            
            // Red pixels should be highlighted (mixed with yellow)
            for (int i = 0; i < 4; i++)
            {
                Color pixel = resultPixels[i];
                Assert.AreNotEqual(Color.red, pixel, $"Red pixel {i} should be highlighted, not original red");
                Assert.AreNotEqual(Color.black, pixel, $"Red pixel {i} should not be black in highlight mode");
                
                // Should have some yellow component
                Assert.Greater(pixel.g, 0.1f, $"Highlighted pixel {i} should have yellow component");
            }
        }
        
        [Test]
        public void ColorRangeSettings_GetHueSaturationRange_ReturnsCorrectVector()
        {
            // Test vector conversion for shader use
            var range = new ColorRangeFilter.ColorRangeSettings();
            range.hueMin = 120f;
            range.hueMax = 180f;
            range.saturationMin = 0.3f;
            range.saturationMax = 0.8f;
            
            Vector4 hsRange = range.GetHueSaturationRange();
            
            Assert.AreEqual(120f / 360f, hsRange.x, 0.001f);
            Assert.AreEqual(180f / 360f, hsRange.y, 0.001f);
            Assert.AreEqual(0.3f, hsRange.z, 0.001f);
            Assert.AreEqual(0.8f, hsRange.w, 0.001f);
        }
        
        [Test]
        public void ColorRangeSettings_GetValueRange_ReturnsCorrectVector()
        {
            // Test value range vector conversion
            var range = new ColorRangeFilter.ColorRangeSettings();
            range.valueMin = 0.2f;
            range.valueMax = 0.9f;
            
            Vector2 valueRange = range.GetValueRange();
            
            Assert.AreEqual(0.2f, valueRange.x, 0.001f);
            Assert.AreEqual(0.9f, valueRange.y, 0.001f);
        }
    }
}