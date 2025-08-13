using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DaVinciEye.Filters;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Performance tests for filter layering and interaction
    /// Ensures the system meets the 60 FPS target with multiple filters active
    /// </summary>
    public class FilterPerformanceTests
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
            
            // Create larger test texture for performance testing
            testTexture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
            Color[] pixels = new Color[1024 * 1024];
            
            // Create a more complex test pattern
            for (int y = 0; y < 1024; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    float r = Mathf.Sin(x * 0.01f) * 0.5f + 0.5f;
                    float g = Mathf.Sin(y * 0.01f) * 0.5f + 0.5f;
                    float b = Mathf.Sin((x + y) * 0.005f) * 0.5f + 0.5f;
                    pixels[y * 1024 + x] = new Color(r, g, b, 1f);
                }
            }
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
            
            filterManager.SetSourceTexture(testTexture);
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
        /// Test performance with single filter application
        /// </summary>
        [Test]
        public void TestSingleFilterPerformance()
        {
            // Test each filter type individually
            var filterTypes = new[] { FilterType.Grayscale, FilterType.ContrastEnhancement, FilterType.EdgeDetection };
            
            foreach (var filterType in filterTypes)
            {
                // Arrange
                var parameters = new FilterParameters(filterType);
                
                // Act & Measure
                float startTime = Time.realtimeSinceStartup;
                filterManager.ApplyFilter(filterType, parameters);
                float endTime = Time.realtimeSinceStartup;
                
                float applicationTime = endTime - startTime;
                
                // Assert
                Assert.Less(applicationTime, 0.016f, // 16ms = 60 FPS target
                    $"{filterType} filter application should complete within 16ms (was {applicationTime * 1000f:F2}ms)");
                
                // Clean up
                filterManager.RemoveFilter(filterType);
            }
        }
        
        /// <summary>
        /// Test performance with multiple filters layered
        /// </summary>
        [Test]
        public void TestMultipleFilterLayerPerformance()
        {
            // Arrange
            var filters = new[]
            {
                new { Type = FilterType.Grayscale, Params = new FilterParameters(FilterType.Grayscale) },
                new { Type = FilterType.ContrastEnhancement, Params = new FilterParameters(FilterType.ContrastEnhancement) },
                new { Type = FilterType.EdgeDetection, Params = new FilterParameters(FilterType.EdgeDetection) }
            };
            
            // Act - Apply filters sequentially and measure cumulative time
            float totalStartTime = Time.realtimeSinceStartup;
            
            foreach (var filter in filters)
            {
                float filterStartTime = Time.realtimeSinceStartup;
                filterManager.ApplyFilter(filter.Type, filter.Params);
                float filterEndTime = Time.realtimeSinceStartup;
                
                float filterTime = filterEndTime - filterStartTime;
                Assert.Less(filterTime, 0.020f, // Allow slightly more time for layered filters
                    $"Filter {filter.Type} application with existing layers should complete within 20ms");
            }
            
            float totalEndTime = Time.realtimeSinceStartup;
            float totalTime = totalEndTime - totalStartTime;
            
            // Assert
            Assert.Less(totalTime, 0.050f, // 50ms total for all filters
                $"Total time for applying 3 filters should be under 50ms (was {totalTime * 1000f:F2}ms)");
            
            Assert.AreEqual(3, filterManager.ActiveFilters.Count, "All filters should be active");
        }
        
        /// <summary>
        /// Test performance of filter reordering operations
        /// </summary>
        [Test]
        public void TestFilterReorderingPerformance()
        {
            // Arrange - Apply multiple filters
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            
            // Act & Measure reordering operations
            float startTime = Time.realtimeSinceStartup;
            
            // Perform multiple reordering operations
            filterManager.MoveFilterUp(FilterType.Grayscale);
            filterManager.MoveFilterDown(FilterType.EdgeDetection);
            filterManager.MoveFilterUp(FilterType.ContrastEnhancement);
            
            float endTime = Time.realtimeSinceStartup;
            float reorderTime = endTime - startTime;
            
            // Assert
            Assert.Less(reorderTime, 0.010f, // 10ms for reordering operations
                $"Filter reordering should complete within 10ms (was {reorderTime * 1000f:F2}ms)");
        }
        
        /// <summary>
        /// Test performance of filter parameter updates
        /// </summary>
        [Test]
        public void TestFilterParameterUpdatePerformance()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            // Act - Update parameters multiple times
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 10; i++)
            {
                var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = i * 0.1f };
                var contrastParams = new FilterParameters(FilterType.ContrastEnhancement) { intensity = i * 0.1f };
                
                filterManager.UpdateFilterParameters(FilterType.Grayscale, grayscaleParams);
                filterManager.UpdateFilterParameters(FilterType.ContrastEnhancement, contrastParams);
            }
            
            float endTime = Time.realtimeSinceStartup;
            float updateTime = endTime - startTime;
            
            // Assert
            Assert.Less(updateTime, 0.100f, // 100ms for 20 parameter updates
                $"Parameter updates should complete within 100ms (was {updateTime * 1000f:F2}ms)");
        }
        
        /// <summary>
        /// Test memory usage with multiple filters
        /// </summary>
        [Test]
        public void TestMemoryUsageWithMultipleFilters()
        {
            // Arrange - Apply multiple filters
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            
            // Act - Get performance metrics
            var metrics = filterManager.GetPerformanceMetrics();
            
            // Assert
            Assert.Less(metrics.memoryUsage, 50f, // 50MB limit for 1024x1024 texture with filters
                $"Memory usage should be under 50MB (was {metrics.memoryUsage:F2}MB)");
            
            Assert.AreEqual(3, metrics.activeFilterCount, "Should report correct number of active filters");
        }
        
        /// <summary>
        /// Test performance of session save/load operations
        /// </summary>
        [Test]
        public void TestSessionSaveLoadPerformance()
        {
            // Arrange - Apply multiple filters with custom parameters
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale) { intensity = 0.8f });
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.6f });
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection) { intensity = 0.4f });
            
            // Act & Measure save operation
            float saveStartTime = Time.realtimeSinceStartup;
            filterManager.SaveSession();
            float saveEndTime = Time.realtimeSinceStartup;
            
            float saveTime = saveEndTime - saveStartTime;
            
            // Clear filters
            filterManager.ClearAllFilters(false);
            
            // Act & Measure load operation
            float loadStartTime = Time.realtimeSinceStartup;
            filterManager.LoadSession();
            float loadEndTime = Time.realtimeSinceStartup;
            
            float loadTime = loadEndTime - loadStartTime;
            
            // Assert
            Assert.Less(saveTime, 0.005f, // 5ms for save operation
                $"Session save should complete within 5ms (was {saveTime * 1000f:F2}ms)");
            
            Assert.Less(loadTime, 0.020f, // 20ms for load operation (includes filter application)
                $"Session load should complete within 20ms (was {loadTime * 1000f:F2}ms)");
            
            Assert.AreEqual(3, filterManager.ActiveFilters.Count, "All filters should be restored");
        }
        
        /// <summary>
        /// Test performance of clear all filters operation
        /// </summary>
        [Test]
        public void TestClearAllFiltersPerformance()
        {
            // Arrange - Apply many filters
            for (int i = 0; i < 5; i++)
            {
                filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
                filterManager.RemoveFilter(FilterType.Grayscale); // Remove and re-add to create history
            }
            
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection));
            
            // Act & Measure
            float startTime = Time.realtimeSinceStartup;
            filterManager.ClearAllFilters(true); // With backup
            float endTime = Time.realtimeSinceStartup;
            
            float clearTime = endTime - startTime;
            
            // Assert
            Assert.Less(clearTime, 0.010f, // 10ms for clear operation
                $"Clear all filters should complete within 10ms (was {clearTime * 1000f:F2}ms)");
            
            Assert.AreEqual(0, filterManager.ActiveFilters.Count, "All filters should be cleared");
        }
        
        /// <summary>
        /// Benchmark filter processing performance
        /// </summary>
        [Test]
        public void BenchmarkFilterProcessing()
        {
            var filterTypes = new[] { FilterType.Grayscale, FilterType.ContrastEnhancement, FilterType.EdgeDetection };
            
            foreach (var filterType in filterTypes)
            {
                var parameters = new FilterParameters(filterType);
                
                // Run benchmark
                var benchmarkData = filterManager.BenchmarkFilterPerformance(filterType, parameters, 50);
                
                // Assert performance targets
                Assert.Greater(benchmarkData.fps, 60f, 
                    $"{filterType} should maintain >60 FPS (achieved {benchmarkData.fps:F1} FPS)");
                
                Assert.Less(benchmarkData.averageTime, 0.016f, 
                    $"{filterType} average time should be <16ms (was {benchmarkData.averageTime * 1000f:F2}ms)");
                
                Debug.Log($"Benchmark {filterType}: {benchmarkData}");
            }
        }
        
        /// <summary>
        /// Test real-time preview performance
        /// </summary>
        [UnityTest]
        public IEnumerator TestRealTimePreviewPerformance()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            // Act - Enable real-time preview
            filterManager.EnableRealTimePreview(true);
            
            // Measure performance over time
            float totalFrameTime = 0f;
            int frameCount = 0;
            float testDuration = 1f; // 1 second test
            float startTime = Time.realtimeSinceStartup;
            
            while (Time.realtimeSinceStartup - startTime < testDuration)
            {
                float frameStart = Time.realtimeSinceStartup;
                yield return null;
                float frameEnd = Time.realtimeSinceStartup;
                
                totalFrameTime += (frameEnd - frameStart);
                frameCount++;
            }
            
            // Disable real-time preview
            filterManager.EnableRealTimePreview(false);
            
            // Calculate average FPS
            float averageFrameTime = totalFrameTime / frameCount;
            float averageFPS = 1f / averageFrameTime;
            
            // Assert
            Assert.Greater(averageFPS, 30f, 
                $"Real-time preview should maintain >30 FPS with multiple filters (achieved {averageFPS:F1} FPS)");
            
            Debug.Log($"Real-time preview performance: {averageFPS:F1} FPS over {frameCount} frames");
        }
    }
}