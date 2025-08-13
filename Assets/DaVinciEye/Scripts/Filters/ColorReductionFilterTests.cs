using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Comprehensive tests for color reduction filtering system
    /// Tests Requirements: 4.2.1, 4.2.2, 4.2.4, 4.2.5
    /// </summary>
    public class ColorReductionFilterTests
    {
        private GameObject testGameObject;
        private ColorReductionFilter colorReductionFilter;
        private Texture2D testTexture;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject with ColorReductionFilter
            testGameObject = new GameObject("ColorReductionFilterTest");
            colorReductionFilter = testGameObject.AddComponent<ColorReductionFilter>();
            
            // Create test texture with gradient colors
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
        /// Create a test texture with gradient colors for testing
        /// </summary>
        private void CreateTestTexture()
        {
            testTexture = new Texture2D(16, 16, TextureFormat.RGB24, false);
            Color[] pixels = new Color[256];
            
            // Create a gradient from red to blue with many intermediate colors
            for (int i = 0; i < 256; i++)
            {
                float t = i / 255f;
                pixels[i] = Color.Lerp(Color.red, Color.blue, t);
            }
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [Test]
        public void ColorReductionFilter_Initialization_SetsDefaultValues()
        {
            // Test that the filter initializes with proper default values
            Assert.IsNotNull(colorReductionFilter);
            Assert.AreEqual(16, colorReductionFilter.TargetColorCount);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.KMeans, colorReductionFilter.QuantizationMethod);
            Assert.IsTrue(colorReductionFilter.RealTimePreview);
        }
        
        [Test]
        public void Requirement_4_2_1_ColorQuantizationAlgorithm()
        {
            // Requirement 4.2.1: WHEN color reduction filtering is activated THEN the system SHALL provide controls to specify the target number of colors
            
            int targetColors = 8;
            colorReductionFilter.TargetColorCount = targetColors;
            
            var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            
            Assert.IsNotNull(palette, "Color quantization should generate a palette");
            Assert.AreEqual(targetColors, palette.totalColors, "Palette should have the specified number of colors");
            Assert.IsNotNull(palette.colors, "Palette should contain color array");
            Assert.IsNotNull(palette.weights, "Palette should contain weight array");
            
            Debug.Log("✓ Requirement 4.2.1: Color quantization algorithm implemented");
        }
        
        [Test]
        public void Requirement_4_2_2_ColorPreservation()
        {
            // Requirement 4.2.2: WHEN the color count is reduced THEN the system SHALL use color quantization algorithms to preserve the most representative colors
            
            int targetColors = 4;
            
            // Test K-means algorithm for representative color preservation
            var kMeansPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            
            Assert.IsNotNull(kMeansPalette, "K-means should generate a palette");
            Assert.AreEqual(targetColors, kMeansPalette.totalColors, "K-means palette should have correct color count");
            
            // Verify that the palette contains representative colors from the gradient
            bool hasRedish = false, hasBlueish = false;
            foreach (var color in kMeansPalette.colors)
            {
                if (color.r > 0.5f && color.b < 0.5f) hasRedish = true;
                if (color.b > 0.5f && color.r < 0.5f) hasBlueish = true;
            }
            
            Assert.IsTrue(hasRedish || hasBlueish, "Palette should preserve representative colors from the gradient");
            
            Debug.Log("✓ Requirement 4.2.2: Color preservation with quantization algorithms implemented");
        }
        
        [Test]
        public void Requirement_4_2_4_AdjustableColorCount()
        {
            // Requirement 4.2.4: WHEN color reduction is applied THEN the system SHALL allow adjustment of the target color count from 2 to 256 colors
            
            // Test minimum color count
            colorReductionFilter.SetTargetColorCount(2);
            Assert.AreEqual(2, colorReductionFilter.TargetColorCount, "Should accept minimum color count of 2");
            
            // Test maximum color count
            colorReductionFilter.SetTargetColorCount(256);
            Assert.AreEqual(256, colorReductionFilter.TargetColorCount, "Should accept maximum color count of 256");
            
            // Test intermediate values
            colorReductionFilter.SetTargetColorCount(16);
            Assert.AreEqual(16, colorReductionFilter.TargetColorCount, "Should accept intermediate color count");
            
            // Test clamping below minimum
            colorReductionFilter.SetTargetColorCount(1);
            Assert.AreEqual(2, colorReductionFilter.TargetColorCount, "Should clamp to minimum of 2 colors");
            
            // Test clamping above maximum
            colorReductionFilter.SetTargetColorCount(300);
            Assert.AreEqual(256, colorReductionFilter.TargetColorCount, "Should clamp to maximum of 256 colors");
            
            Debug.Log("✓ Requirement 4.2.4: Adjustable color count (2-256) implemented");
        }
        
        [Test]
        public void Requirement_4_2_5_SmoothColorTransitions()
        {
            // Requirement 4.2.5: WHEN color reduction is active THEN the system SHALL maintain smooth color transitions while reducing overall palette complexity
            
            int targetColors = 8;
            Texture2D reducedTexture = colorReductionFilter.ApplyColorReduction(testTexture);
            
            Assert.IsNotNull(reducedTexture, "Color reduction should produce a result texture");
            Assert.AreEqual(testTexture.width, reducedTexture.width, "Result should maintain original dimensions");
            Assert.AreEqual(testTexture.height, reducedTexture.height, "Result should maintain original dimensions");
            
            Color[] originalPixels = testTexture.GetPixels();
            Color[] reducedPixels = reducedTexture.GetPixels();
            
            // Verify that adjacent pixels maintain reasonable color similarity (smooth transitions)
            int smoothTransitions = 0;
            int totalTransitions = 0;
            
            for (int y = 0; y < testTexture.height - 1; y++)
            {
                for (int x = 0; x < testTexture.width - 1; x++)
                {
                    int index = y * testTexture.width + x;
                    int rightIndex = index + 1;
                    int downIndex = (y + 1) * testTexture.width + x;
                    
                    // Check horizontal transition
                    float horizontalDistance = ColorDistance(reducedPixels[index], reducedPixels[rightIndex]);
                    if (horizontalDistance < 0.5f) smoothTransitions++;
                    totalTransitions++;
                    
                    // Check vertical transition
                    float verticalDistance = ColorDistance(reducedPixels[index], reducedPixels[downIndex]);
                    if (verticalDistance < 0.5f) smoothTransitions++;
                    totalTransitions++;
                }
            }
            
            float smoothnessRatio = (float)smoothTransitions / totalTransitions;
            Assert.Greater(smoothnessRatio, 0.3f, "Should maintain reasonable smoothness in color transitions");
            
            Debug.Log($"✓ Requirement 4.2.5: Smooth color transitions maintained ({smoothnessRatio:P1} smooth)");
        }
        
        [Test]
        public void ColorReductionFilter_KMeansAlgorithm_GeneratesValidPalette()
        {
            // Test K-means clustering algorithm
            int targetColors = 6;
            var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            
            Assert.IsNotNull(palette);
            Assert.AreEqual(targetColors, palette.totalColors);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.KMeans, palette.method);
            
            // Verify all colors are valid
            foreach (var color in palette.colors)
            {
                Assert.GreaterOrEqual(color.r, 0f);
                Assert.LessOrEqual(color.r, 1f);
                Assert.GreaterOrEqual(color.g, 0f);
                Assert.LessOrEqual(color.g, 1f);
                Assert.GreaterOrEqual(color.b, 0f);
                Assert.LessOrEqual(color.b, 1f);
            }
            
            // Verify weights sum to approximately 1
            float totalWeight = 0f;
            foreach (var weight in palette.weights)
            {
                totalWeight += weight;
            }
            Assert.AreEqual(1f, totalWeight, 0.1f, "Palette weights should sum to approximately 1");
        }
        
        [Test]
        public void ColorReductionFilter_MedianCutAlgorithm_GeneratesValidPalette()
        {
            // Test median cut algorithm
            int targetColors = 8;
            var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.MedianCut);
            
            Assert.IsNotNull(palette);
            Assert.AreEqual(targetColors, palette.totalColors);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.MedianCut, palette.method);
            
            // Median cut should provide good color space coverage
            float colorSpaceCoverage = CalculateColorSpaceCoverage(palette.colors);
            Assert.Greater(colorSpaceCoverage, 0.2f, "Median cut should provide reasonable color space coverage");
        }
        
        [Test]
        public void ColorReductionFilter_UniformAlgorithm_GeneratesEvenDistribution()
        {
            // Test uniform quantization algorithm
            int targetColors = 8;
            var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.Uniform);
            
            Assert.IsNotNull(palette);
            Assert.AreEqual(targetColors, palette.totalColors);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.Uniform, palette.method);
            
            // Uniform should have evenly distributed colors
            foreach (var weight in palette.weights)
            {
                float expectedWeight = 1f / targetColors;
                Assert.AreEqual(expectedWeight, weight, 0.01f, "Uniform quantization should have equal weights");
            }
        }
        
        [Test]
        public void ColorReductionFilter_PopularityAlgorithm_PreservesCommonColors()
        {
            // Test popularity-based algorithm
            int targetColors = 4;
            var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.Popularity);
            
            Assert.IsNotNull(palette);
            Assert.AreEqual(targetColors, palette.totalColors);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.Popularity, palette.method);
            
            // Popularity should have varying weights based on color frequency
            float maxWeight = 0f, minWeight = 1f;
            foreach (var weight in palette.weights)
            {
                maxWeight = Mathf.Max(maxWeight, weight);
                minWeight = Mathf.Min(minWeight, weight);
            }
            
            Assert.Greater(maxWeight - minWeight, 0.01f, "Popularity algorithm should have varying color weights");
        }
        
        [Test]
        public void ColorReductionFilter_ApplyColorReduction_ProducesValidResult()
        {
            // Test complete color reduction process
            colorReductionFilter.TargetColorCount = 12;
            
            Texture2D result = colorReductionFilter.ApplyColorReduction(testTexture);
            
            Assert.IsNotNull(result, "Color reduction should produce a result");
            Assert.AreEqual(testTexture.width, result.width, "Result should maintain original width");
            Assert.AreEqual(testTexture.height, result.height, "Result should maintain original height");
            
            // Verify that the result has fewer unique colors
            var originalColors = new HashSet<Color>(testTexture.GetPixels());
            var reducedColors = new HashSet<Color>(result.GetPixels());
            
            Assert.Less(reducedColors.Count, originalColors.Count, "Result should have fewer unique colors");
            Assert.LessOrEqual(reducedColors.Count, 12, "Result should not exceed target color count significantly");
        }
        
        [Test]
        public void ColorReductionFilter_GetClosestColor_FindsNearestMatch()
        {
            // Test palette color matching
            var palette = new ColorReductionFilter.QuantizedPalette(3);
            palette.colors[0] = Color.red;
            palette.colors[1] = Color.green;
            palette.colors[2] = Color.blue;
            
            // Test exact matches
            Assert.AreEqual(Color.red, palette.GetClosestColor(Color.red));
            Assert.AreEqual(Color.green, palette.GetClosestColor(Color.green));
            Assert.AreEqual(Color.blue, palette.GetClosestColor(Color.blue));
            
            // Test approximate matches
            Color darkRed = new Color(0.5f, 0f, 0f);
            Color closestToRed = palette.GetClosestColor(darkRed);
            Assert.AreEqual(Color.red, closestToRed, "Dark red should match to red");
            
            Color lightGreen = new Color(0.5f, 1f, 0.5f);
            Color closestToGreen = palette.GetClosestColor(lightGreen);
            Assert.AreEqual(Color.green, closestToGreen, "Light green should match to green");
        }
        
        [Test]
        public void ColorReductionFilter_PerformanceTest_MeetsRequirements()
        {
            // Test performance requirements
            colorReductionFilter.TargetColorCount = 16;
            
            float startTime = Time.realtimeSinceStartup;
            
            Texture2D result = colorReductionFilter.ApplyColorReduction(testTexture);
            
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.IsNotNull(result, "Color reduction should complete successfully");
            Assert.Less(processingTime, 1f, "Color reduction should complete within 1 second for small textures");
            
            // Test statistics
            var stats = colorReductionFilter.GetStatistics();
            Assert.IsNotNull(stats, "Statistics should be available");
            Assert.Greater(stats.originalColorCount, 0, "Should count original colors");
            Assert.Greater(stats.reducedColorCount, 0, "Should count reduced colors");
            Assert.Greater(stats.compressionRatio, 0f, "Should calculate compression ratio");
            
            Debug.Log($"Color reduction performance: {processingTime * 1000f:F2}ms, {stats}");
        }
        
        [Test]
        public void ColorReductionFilter_RealTimePreview_UpdatesCorrectly()
        {
            // Test real-time preview functionality
            colorReductionFilter.RealTimePreview = true;
            
            // Set initial color count
            colorReductionFilter.SetTargetColorCount(8, false);
            Assert.AreEqual(8, colorReductionFilter.TargetColorCount);
            
            // Change color count
            colorReductionFilter.SetTargetColorCount(16, false);
            Assert.AreEqual(16, colorReductionFilter.TargetColorCount);
            
            // Change quantization method
            colorReductionFilter.QuantizationMethod = ColorReductionFilter.ColorQuantizationMethod.MedianCut;
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.MedianCut, colorReductionFilter.QuantizationMethod);
        }
        
        [UnityTest]
        public IEnumerator ColorReductionFilter_EventHandling_FiresCorrectly()
        {
            // Test event firing
            bool paletteGenerated = false;
            bool filterApplied = false;
            bool statisticsUpdated = false;
            
            colorReductionFilter.OnPaletteGenerated += (palette) => paletteGenerated = true;
            colorReductionFilter.OnColorReductionApplied += (texture) => filterApplied = true;
            colorReductionFilter.OnStatisticsUpdated += (stats) => statisticsUpdated = true;
            
            colorReductionFilter.ApplyColorReduction(testTexture);
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.IsTrue(paletteGenerated, "OnPaletteGenerated event should fire");
            Assert.IsTrue(filterApplied, "OnColorReductionApplied event should fire");
            Assert.IsTrue(statisticsUpdated, "OnStatisticsUpdated event should fire");
        }
        
        // Helper methods
        
        private float ColorDistance(Color c1, Color c2)
        {
            float dr = c1.r - c2.r;
            float dg = c1.g - c2.g;
            float db = c1.b - c2.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
        
        private float CalculateColorSpaceCoverage(Color[] colors)
        {
            if (colors.Length == 0) return 0f;
            
            float minR = 1f, maxR = 0f;
            float minG = 1f, maxG = 0f;
            float minB = 1f, maxB = 0f;
            
            foreach (var color in colors)
            {
                minR = Mathf.Min(minR, color.r);
                maxR = Mathf.Max(maxR, color.r);
                minG = Mathf.Min(minG, color.g);
                maxG = Mathf.Max(maxG, color.g);
                minB = Mathf.Min(minB, color.b);
                maxB = Mathf.Max(maxB, color.b);
            }
            
            return (maxR - minR) * (maxG - minG) * (maxB - minB);
        }
    }
}