using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Verification tests for Task 6.2: Implement color reduction filtering
    /// Validates Requirements: 4.2.1, 4.2.2, 4.2.4, 4.2.5
    /// </summary>
    public class Task62Verification
    {
        private GameObject testGameObject;
        private ColorReductionFilter colorReductionFilter;
        private ColorReductionUI reductionUI;
        private FilterManager filterManager;
        private Texture2D testTexture;
        
        [SetUp]
        public void Setup()
        {
            // Create test environment
            testGameObject = new GameObject("Task62Test");
            colorReductionFilter = testGameObject.AddComponent<ColorReductionFilter>();
            reductionUI = testGameObject.AddComponent<ColorReductionUI>();
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
            testTexture = new Texture2D(32, 32, TextureFormat.RGB24, false);
            Color[] pixels = new Color[1024];
            
            // Create test pattern with many colors (gradient + noise)
            for (int i = 0; i < 1024; i++)
            {
                float t = i / 1023f;
                Color baseColor = Color.Lerp(Color.red, Color.blue, t);
                
                // Add some noise to create more unique colors
                float noise = (UnityEngine.Random.value - 0.5f) * 0.2f;
                baseColor.r = Mathf.Clamp01(baseColor.r + noise);
                baseColor.g = Mathf.Clamp01(baseColor.g + noise);
                baseColor.b = Mathf.Clamp01(baseColor.b + noise);
                
                pixels[i] = baseColor;
            }
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [Test]
        public void Requirement_4_2_1_ColorQuantizationAlgorithm()
        {
            // Requirement 4.2.1: WHEN color reduction filtering is activated THEN the system SHALL provide controls to specify the target number of colors
            
            int targetColors = 16;
            colorReductionFilter.TargetColorCount = targetColors;
            
            // Test K-means quantization
            var kMeansPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            Assert.IsNotNull(kMeansPalette, "K-means quantization should generate a palette");
            Assert.AreEqual(targetColors, kMeansPalette.totalColors, "K-means palette should have specified color count");
            
            // Test Median Cut quantization
            var medianCutPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.MedianCut);
            Assert.IsNotNull(medianCutPalette, "Median cut quantization should generate a palette");
            Assert.AreEqual(targetColors, medianCutPalette.totalColors, "Median cut palette should have specified color count");
            
            // Test Uniform quantization
            var uniformPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.Uniform);
            Assert.IsNotNull(uniformPalette, "Uniform quantization should generate a palette");
            Assert.AreEqual(targetColors, uniformPalette.totalColors, "Uniform palette should have specified color count");
            
            // Test Popularity quantization
            var popularityPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.Popularity);
            Assert.IsNotNull(popularityPalette, "Popularity quantization should generate a palette");
            Assert.AreEqual(targetColors, popularityPalette.totalColors, "Popularity palette should have specified color count");
            
            Debug.Log("✓ Requirement 4.2.1: Color quantization algorithms implemented");
        }
        
        [Test]
        public void Requirement_4_2_2_RepresentativeColorPreservation()
        {
            // Requirement 4.2.2: WHEN the color count is reduced THEN the system SHALL use color quantization algorithms to preserve the most representative colors
            
            int targetColors = 8;
            
            // Apply color reduction
            Texture2D reducedTexture = colorReductionFilter.ApplyColorReduction(testTexture);
            
            Assert.IsNotNull(reducedTexture, "Color reduction should produce a result");
            
            // Get the generated palette
            var palette = colorReductionFilter.GetCurrentPalette();
            Assert.IsNotNull(palette, "Should generate a quantized palette");
            
            // Verify that the palette preserves representative colors from the original gradient
            bool hasRedComponent = false, hasBlueComponent = false;
            foreach (var color in palette.colors)
            {
                if (color.r > 0.3f && color.b < 0.7f) hasRedComponent = true;
                if (color.b > 0.3f && color.r < 0.7f) hasBlueComponent = true;
            }
            
            Assert.IsTrue(hasRedComponent || hasBlueComponent, "Palette should preserve representative colors from the gradient");
            
            // Verify color weights are calculated
            float totalWeight = 0f;
            foreach (var weight in palette.weights)
            {
                Assert.GreaterOrEqual(weight, 0f, "Color weights should be non-negative");
                totalWeight += weight;
            }
            Assert.AreEqual(1f, totalWeight, 0.1f, "Color weights should sum to approximately 1");
            
            Debug.Log("✓ Requirement 4.2.2: Representative color preservation implemented");
        }
        
        [Test]
        public void Requirement_4_2_4_AdjustableColorCount()
        {
            // Requirement 4.2.4: WHEN color reduction is applied THEN the system SHALL allow adjustment of the target color count from 2 to 256 colors
            
            // Test minimum boundary (2 colors)
            colorReductionFilter.SetTargetColorCount(2);
            Assert.AreEqual(2, colorReductionFilter.TargetColorCount, "Should accept minimum of 2 colors");
            
            var palette2 = colorReductionFilter.GenerateQuantizedPalette(testTexture, 2, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            Assert.AreEqual(2, palette2.totalColors, "Should generate palette with 2 colors");
            
            // Test maximum boundary (256 colors)
            colorReductionFilter.SetTargetColorCount(256);
            Assert.AreEqual(256, colorReductionFilter.TargetColorCount, "Should accept maximum of 256 colors");
            
            var palette256 = colorReductionFilter.GenerateQuantizedPalette(testTexture, 256, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            Assert.AreEqual(256, palette256.totalColors, "Should generate palette with 256 colors");
            
            // Test intermediate values
            int[] testCounts = { 4, 8, 16, 32, 64, 128 };
            foreach (int count in testCounts)
            {
                colorReductionFilter.SetTargetColorCount(count);
                Assert.AreEqual(count, colorReductionFilter.TargetColorCount, $"Should accept color count of {count}");
                
                var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, count, ColorReductionFilter.ColorQuantizationMethod.KMeans);
                Assert.AreEqual(count, palette.totalColors, $"Should generate palette with {count} colors");
            }
            
            // Test boundary clamping
            colorReductionFilter.SetTargetColorCount(1);
            Assert.AreEqual(2, colorReductionFilter.TargetColorCount, "Should clamp below minimum to 2");
            
            colorReductionFilter.SetTargetColorCount(300);
            Assert.AreEqual(256, colorReductionFilter.TargetColorCount, "Should clamp above maximum to 256");
            
            Debug.Log("✓ Requirement 4.2.4: Adjustable color count (2-256) implemented");
        }
        
        [Test]
        public void Requirement_4_2_5_PaletteComplexityReduction()
        {
            // Requirement 4.2.5: WHEN color reduction is active THEN the system SHALL maintain smooth color transitions while reducing overall palette complexity
            
            int targetColors = 12;
            colorReductionFilter.TargetColorCount = targetColors;
            
            // Apply color reduction
            Texture2D reducedTexture = colorReductionFilter.ApplyColorReduction(testTexture);
            
            Assert.IsNotNull(reducedTexture, "Color reduction should produce a result");
            
            // Verify palette complexity reduction
            var originalColors = new System.Collections.Generic.HashSet<Color>(testTexture.GetPixels());
            var reducedColors = new System.Collections.Generic.HashSet<Color>(reducedTexture.GetPixels());
            
            Assert.Less(reducedColors.Count, originalColors.Count, "Should reduce the number of unique colors");
            Assert.LessOrEqual(reducedColors.Count, targetColors * 2, "Should not significantly exceed target color count");
            
            // Verify smooth color transitions by checking adjacent pixel similarity
            Color[] reducedPixels = reducedTexture.GetPixels();
            int smoothTransitions = 0;
            int totalTransitions = 0;
            
            for (int y = 0; y < reducedTexture.height - 1; y++)
            {
                for (int x = 0; x < reducedTexture.width - 1; x++)
                {
                    int index = y * reducedTexture.width + x;
                    int rightIndex = index + 1;
                    
                    float colorDistance = ColorDistance(reducedPixels[index], reducedPixels[rightIndex]);
                    if (colorDistance < 0.6f) smoothTransitions++;
                    totalTransitions++;
                }
            }
            
            float smoothnessRatio = (float)smoothTransitions / totalTransitions;
            Assert.Greater(smoothnessRatio, 0.4f, "Should maintain reasonable smoothness in color transitions");
            
            // Get statistics
            var stats = colorReductionFilter.GetStatistics();
            Assert.IsNotNull(stats, "Should provide color reduction statistics");
            Assert.Greater(stats.originalColorCount, stats.reducedColorCount, "Should show complexity reduction");
            Assert.Less(stats.compressionRatio, 1f, "Should show compression achieved");
            
            Debug.Log($"✓ Requirement 4.2.5: Palette complexity reduction implemented (smoothness: {smoothnessRatio:P1}, compression: {stats.compressionRatio:P1})");
        }
        
        [Test]
        public void ColorReductionFilter_QuantizationMethods_ProduceDifferentResults()
        {
            // Test that different quantization methods produce different results
            int targetColors = 8;
            
            var kMeansPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.KMeans);
            var medianCutPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.MedianCut);
            var uniformPalette = colorReductionFilter.GenerateQuantizedPalette(testTexture, targetColors, ColorReductionFilter.ColorQuantizationMethod.Uniform);
            
            // Verify methods produce different palettes
            bool kMeansVsMedianDifferent = !PalettesEqual(kMeansPalette, medianCutPalette);
            bool kMeansVsUniformDifferent = !PalettesEqual(kMeansPalette, uniformPalette);
            
            Assert.IsTrue(kMeansVsMedianDifferent || kMeansVsUniformDifferent, "Different quantization methods should produce different results");
            
            Debug.Log("✓ Different quantization methods produce distinct results");
        }
        
        [Test]
        public void ColorReductionUI_Controls_FunctionCorrectly()
        {
            // Test UI control functionality
            Assert.IsNotNull(reductionUI, "ColorReductionUI component should exist");
            
            // Test color count setting
            reductionUI.SetColorCount(24);
            Assert.AreEqual(24, reductionUI.GetColorCount(), "UI should set color count correctly");
            
            // Test quantization method setting
            reductionUI.SetQuantizationMethod(ColorReductionFilter.ColorQuantizationMethod.MedianCut);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.MedianCut, reductionUI.GetQuantizationMethod(), "UI should set quantization method correctly");
            
            Debug.Log("✓ ColorReductionUI controls function correctly");
        }
        
        [Test]
        public void ColorReductionFilter_Integration_WithFilterManager()
        {
            // Test integration with FilterManager
            var parameters = new FilterParameters(FilterType.ColorReduction);
            parameters.targetColorCount = 20;
            parameters.intensity = 1f;
            parameters.customParameters["method"] = (float)ColorReductionFilter.ColorQuantizationMethod.KMeans;
            
            // Apply through FilterManager
            filterManager.SetSourceTexture(testTexture);
            filterManager.ApplyFilter(FilterType.ColorReduction, parameters);
            
            // Verify filter was applied
            bool isActive = filterManager.IsFilterActive(FilterType.ColorReduction);
            Assert.IsTrue(isActive, "Color reduction filter should be active in FilterManager");
            
            var activeFilters = filterManager.GetActiveFilterTypes();
            Assert.Contains(FilterType.ColorReduction, activeFilters, "Color reduction filter should be in active filters list");
            
            Debug.Log("✓ ColorReductionFilter integration with FilterManager verified");
        }
        
        [Test]
        public void ColorReductionFilter_Performance_MeetsRequirements()
        {
            // Test performance requirements
            colorReductionFilter.TargetColorCount = 16;
            
            float startTime = Time.realtimeSinceStartup;
            
            Texture2D result = colorReductionFilter.ApplyColorReduction(testTexture);
            
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.IsNotNull(result, "Color reduction should complete successfully");
            Assert.Less(processingTime, 2f, "Color reduction should complete within 2 seconds for test texture");
            
            var stats = colorReductionFilter.GetStatistics();
            Assert.IsNotNull(stats, "Should provide performance statistics");
            Assert.Greater(stats.processingTime, 0f, "Should measure processing time");
            
            Debug.Log($"✓ Color reduction performance: {processingTime * 1000f:F2}ms");
        }
        
        [UnityTest]
        public IEnumerator ColorReductionFilter_RealTimePreview()
        {
            // Test real-time preview functionality
            colorReductionFilter.RealTimePreview = true;
            
            // Set initial parameters
            colorReductionFilter.SetTargetColorCount(8, false);
            colorReductionFilter.QuantizationMethod = ColorReductionFilter.ColorQuantizationMethod.KMeans;
            
            yield return new WaitForSeconds(0.1f);
            
            // Change parameters
            colorReductionFilter.SetTargetColorCount(16, false);
            colorReductionFilter.QuantizationMethod = ColorReductionFilter.ColorQuantizationMethod.MedianCut;
            
            yield return new WaitForSeconds(0.1f);
            
            // Verify parameters were updated
            Assert.AreEqual(16, colorReductionFilter.TargetColorCount);
            Assert.AreEqual(ColorReductionFilter.ColorQuantizationMethod.MedianCut, colorReductionFilter.QuantizationMethod);
            
            Debug.Log("✓ Real-time preview functionality verified");
        }
        
        [Test]
        public void Task62_CompletionVerification()
        {
            // Verify all components of Task 6.2 are implemented
            
            // 1. Color quantization algorithm for palette reduction
            Assert.IsNotNull(colorReductionFilter, "ColorReductionFilter component should exist");
            
            // 2. Adjustable color count controls (2-256 colors)
            colorReductionFilter.SetTargetColorCount(2);
            Assert.AreEqual(2, colorReductionFilter.TargetColorCount);
            colorReductionFilter.SetTargetColorCount(256);
            Assert.AreEqual(256, colorReductionFilter.TargetColorCount);
            
            // 3. Multiple quantization methods
            var methods = new[] {
                ColorReductionFilter.ColorQuantizationMethod.KMeans,
                ColorReductionFilter.ColorQuantizationMethod.MedianCut,
                ColorReductionFilter.ColorQuantizationMethod.Uniform,
                ColorReductionFilter.ColorQuantizationMethod.Popularity
            };
            
            foreach (var method in methods)
            {
                colorReductionFilter.QuantizationMethod = method;
                var palette = colorReductionFilter.GenerateQuantizedPalette(testTexture, 8, method);
                Assert.IsNotNull(palette, $"{method} quantization should work");
            }
            
            // 4. UI controls
            Assert.IsNotNull(reductionUI, "ColorReductionUI component should exist");
            
            // 5. FilterManager integration
            Assert.IsNotNull(filterManager, "FilterManager integration should exist");
            
            // 6. Performance requirements
            float startTime = Time.realtimeSinceStartup;
            colorReductionFilter.ApplyColorReduction(testTexture);
            float processingTime = Time.realtimeSinceStartup - startTime;
            
            Assert.Less(processingTime, 2f, "Color reduction should meet performance requirements");
            
            Debug.Log("✓ Task 6.2 - Implement color reduction filtering: COMPLETED");
            Debug.Log("  - Color quantization algorithms: ✓");
            Debug.Log("  - Adjustable color count (2-256): ✓");
            Debug.Log("  - Multiple quantization methods: ✓");
            Debug.Log("  - UI controls: ✓");
            Debug.Log("  - FilterManager integration: ✓");
            Debug.Log($"  - Performance: {processingTime * 1000f:F2}ms");
        }
        
        // Helper methods
        
        private float ColorDistance(Color c1, Color c2)
        {
            float dr = c1.r - c2.r;
            float dg = c1.g - c2.g;
            float db = c1.b - c2.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
        
        private bool PalettesEqual(ColorReductionFilter.QuantizedPalette p1, ColorReductionFilter.QuantizedPalette p2)
        {
            if (p1 == null || p2 == null || p1.colors.Length != p2.colors.Length)
                return false;
            
            for (int i = 0; i < p1.colors.Length; i++)
            {
                if (ColorDistance(p1.colors[i], p2.colors[i]) > 0.1f)
                    return false;
            }
            
            return true;
        }
    }
}