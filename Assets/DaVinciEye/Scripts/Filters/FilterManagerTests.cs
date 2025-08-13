using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DaVinciEye.Filters.Tests
{
    /// <summary>
    /// Unit tests for FilterManager functionality
    /// </summary>
    public class FilterManagerTests
    {
        private GameObject testGameObject;
        private FilterManager filterManager;
        private Volume testVolume;
        
        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with FilterManager
            testGameObject = new GameObject("TestFilterManager");
            filterManager = testGameObject.AddComponent<FilterManager>();
            testVolume = testGameObject.GetComponent<Volume>();
            
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
        }
        
        [Test]
        public void FilterManager_InitializesCorrectly()
        {
            // Assert
            Assert.IsNotNull(filterManager);
            Assert.IsNotNull(testVolume);
            Assert.IsNotNull(testVolume.profile);
            Assert.IsTrue(testVolume.isGlobal);
            Assert.AreEqual(1f, testVolume.weight);
        }
        
        [Test]
        public void FilterManager_StartsWithNoActiveFilters()
        {
            // Assert
            Assert.AreEqual(0, filterManager.ActiveFilters.Count);
        }
        
        [Test]
        public void ApplyFilter_GrayscaleFilter_AddsToActiveFilters()
        {
            // Arrange
            var parameters = new FilterParameters(FilterType.Grayscale);
            parameters.intensity = 0.8f;
            
            // Act
            filterManager.ApplyFilter(FilterType.Grayscale, parameters);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
            Assert.AreEqual(FilterType.Grayscale, filterManager.ActiveFilters[0].type);
            Assert.AreEqual(0.8f, filterManager.ActiveFilters[0].parameters.intensity, 0.01f);
        }
        
        [Test]
        public void ApplyFilter_ContrastFilter_AddsToActiveFilters()
        {
            // Arrange
            var parameters = new FilterParameters(FilterType.ContrastEnhancement);
            parameters.intensity = 0.6f;
            
            // Act
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, parameters);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
            Assert.AreEqual(FilterType.ContrastEnhancement, filterManager.ActiveFilters[0].type);
        }
        
        [Test]
        public void ApplyFilter_EdgeDetectionFilter_AddsToActiveFilters()
        {
            // Arrange
            var parameters = new FilterParameters(FilterType.EdgeDetection);
            parameters.intensity = 0.5f;
            parameters.customParameters["threshold"] = 0.2f;
            
            // Act
            filterManager.ApplyFilter(FilterType.EdgeDetection, parameters);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
            Assert.AreEqual(FilterType.EdgeDetection, filterManager.ActiveFilters[0].type);
        }
        
        [Test]
        public void ApplyFilter_SameTypeMultipleTimes_ReplacesExistingFilter()
        {
            // Arrange
            var parameters1 = new FilterParameters(FilterType.Grayscale) { intensity = 0.5f };
            var parameters2 = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
            
            // Act
            filterManager.ApplyFilter(FilterType.Grayscale, parameters1);
            filterManager.ApplyFilter(FilterType.Grayscale, parameters2);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
            Assert.AreEqual(0.8f, filterManager.ActiveFilters[0].parameters.intensity, 0.01f);
        }
        
        [Test]
        public void ApplyFilter_MultipleFilterTypes_AddsAllToActiveFilters()
        {
            // Arrange
            var grayscaleParams = new FilterParameters(FilterType.Grayscale);
            var contrastParams = new FilterParameters(FilterType.ContrastEnhancement);
            
            // Act
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, contrastParams);
            
            // Assert
            Assert.AreEqual(2, filterManager.ActiveFilters.Count);
        }
        
        [Test]
        public void RemoveFilter_ExistingFilter_RemovesFromActiveFilters()
        {
            // Arrange
            var parameters = new FilterParameters(FilterType.Grayscale);
            filterManager.ApplyFilter(FilterType.Grayscale, parameters);
            
            // Act
            filterManager.RemoveFilter(FilterType.Grayscale);
            
            // Assert
            Assert.AreEqual(0, filterManager.ActiveFilters.Count);
        }
        
        [Test]
        public void RemoveFilter_NonExistentFilter_DoesNothing()
        {
            // Arrange
            var parameters = new FilterParameters(FilterType.Grayscale);
            filterManager.ApplyFilter(FilterType.Grayscale, parameters);
            
            // Act
            filterManager.RemoveFilter(FilterType.ContrastEnhancement);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
        }
        
        [Test]
        public void ClearAllFilters_WithActiveFilters_RemovesAllFilters()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            // Act
            filterManager.ClearAllFilters();
            
            // Assert
            Assert.AreEqual(0, filterManager.ActiveFilters.Count);
        }
        
        [Test]
        public void UpdateFilterParameters_ExistingFilter_UpdatesParameters()
        {
            // Arrange
            var originalParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.5f };
            var updatedParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.9f };
            filterManager.ApplyFilter(FilterType.Grayscale, originalParams);
            
            // Act
            filterManager.UpdateFilterParameters(FilterType.Grayscale, updatedParams);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
            Assert.AreEqual(0.9f, filterManager.ActiveFilters[0].parameters.intensity, 0.01f);
        }
        
        [Test]
        public void SaveFilterSettings_WithActiveFilters_ReturnsValidJson()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            
            // Act
            string json = filterManager.SaveFilterSettings();
            
            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("Grayscale"));
        }
        
        [Test]
        public void LoadFilterSettings_ValidJson_RestoresFilters()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            string json = filterManager.SaveFilterSettings();
            filterManager.ClearAllFilters();
            
            // Act
            filterManager.LoadFilterSettings(json);
            
            // Assert
            Assert.AreEqual(1, filterManager.ActiveFilters.Count);
            Assert.AreEqual(FilterType.Grayscale, filterManager.ActiveFilters[0].type);
        }
        
        [UnityTest]
        public IEnumerator FilterManager_EventsFireCorrectly()
        {
            // Arrange
            bool filterAppliedFired = false;
            bool filterRemovedFired = false;
            bool filtersClearedFired = false;
            
            filterManager.OnFilterApplied += (texture) => filterAppliedFired = true;
            filterManager.OnFilterRemoved += (type) => filterRemovedFired = true;
            filterManager.OnFiltersCleared += () => filtersClearedFired = true;
            
            // Act & Assert
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            yield return null;
            Assert.IsTrue(filterAppliedFired);
            
            filterManager.RemoveFilter(FilterType.Grayscale);
            yield return null;
            Assert.IsTrue(filterRemovedFired);
            
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ClearAllFilters();
            yield return null;
            Assert.IsTrue(filtersClearedFired);
        }
        
        [Test]
        public void SetSourceTexture_ValidTexture_UpdatesProcessedTexture()
        {
            // Arrange
            var testTexture = new Texture2D(256, 256);
            
            // Act
            filterManager.SetSourceTexture(testTexture);
            
            // Assert
            Assert.IsNotNull(filterManager.ProcessedTexture);
            
            // Cleanup
            Object.DestroyImmediate(testTexture);
        }
        
        #region Standard Filter Tests
        
        [Test]
        public void GrayscaleFilter_VariousIntensities_ProducesCorrectSaturationValues()
        {
            // Test different intensity values
            float[] intensities = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            
            foreach (float intensity in intensities)
            {
                // Arrange
                var parameters = new FilterParameters(FilterType.Grayscale) { intensity = intensity };
                
                // Act
                filterManager.ApplyFilter(FilterType.Grayscale, parameters);
                
                // Assert
                var volume = testGameObject.GetComponent<Volume>();
                Assert.IsTrue(volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments));
                
                float expectedSaturation = -100f * intensity;
                Assert.AreEqual(expectedSaturation, colorAdjustments.saturation.value, 0.01f, 
                    $"Intensity {intensity} should produce saturation {expectedSaturation}");
                
                // Cleanup for next iteration
                filterManager.ClearAllFilters();
            }
        }
        
        [Test]
        public void ContrastFilter_VariousIntensities_ProducesCorrectContrastValues()
        {
            // Test different intensity values
            float[] intensities = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            
            foreach (float intensity in intensities)
            {
                // Arrange
                var parameters = new FilterParameters(FilterType.ContrastEnhancement) { intensity = intensity };
                
                // Act
                filterManager.ApplyFilter(FilterType.ContrastEnhancement, parameters);
                
                // Assert
                var volume = testGameObject.GetComponent<Volume>();
                Assert.IsTrue(volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments));
                
                float expectedContrast = (intensity - 0.5f) * 100f;
                expectedContrast = Mathf.Clamp(expectedContrast, -50f, 50f);
                Assert.AreEqual(expectedContrast, colorAdjustments.contrast.value, 0.01f,
                    $"Intensity {intensity} should produce contrast {expectedContrast}");
                
                // Cleanup for next iteration
                filterManager.ClearAllFilters();
            }
        }
        
        [Test]
        public void EdgeDetectionFilter_VariousIntensities_ProducesCorrectBloomValues()
        {
            // Test different intensity values
            float[] intensities = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            
            foreach (float intensity in intensities)
            {
                // Arrange
                var parameters = new FilterParameters(FilterType.EdgeDetection) { intensity = intensity };
                parameters.customParameters["threshold"] = 0.2f;
                
                // Act
                filterManager.ApplyFilter(FilterType.EdgeDetection, parameters);
                
                // Assert
                var volume = testGameObject.GetComponent<Volume>();
                Assert.IsTrue(volume.profile.TryGet<Bloom>(out var bloom));
                
                float expectedIntensity = intensity * 3f;
                expectedIntensity = Mathf.Clamp(expectedIntensity, 0f, 5f);
                Assert.AreEqual(expectedIntensity, bloom.intensity.value, 0.01f,
                    $"Intensity {intensity} should produce bloom intensity {expectedIntensity}");
                Assert.AreEqual(0.2f, bloom.threshold.value, 0.01f);
                
                // Cleanup for next iteration
                filterManager.ClearAllFilters();
            }
        }
        
        [Test]
        public void StandardFilters_RealTimeIntensityUpdate_UpdatesCorrectly()
        {
            // Arrange
            var grayscaleParams = new FilterParameters(FilterType.Grayscale) { intensity = 0.5f };
            filterManager.ApplyFilter(FilterType.Grayscale, grayscaleParams);
            
            // Act - Update intensity
            grayscaleParams.intensity = 0.8f;
            filterManager.UpdateFilterParameters(FilterType.Grayscale, grayscaleParams);
            
            // Assert
            var volume = testGameObject.GetComponent<Volume>();
            Assert.IsTrue(volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments));
            Assert.AreEqual(-80f, colorAdjustments.saturation.value, 0.01f);
        }
        
        [Test]
        public void StandardFilters_LayeredApplication_AllFiltersActive()
        {
            // Arrange & Act
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale) { intensity = 0.5f });
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.7f });
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection) { intensity = 0.3f });
            
            // Assert
            Assert.AreEqual(3, filterManager.ActiveFilters.Count);
            
            var volume = testGameObject.GetComponent<Volume>();
            Assert.IsTrue(volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments));
            Assert.IsTrue(volume.profile.TryGet<Bloom>(out var bloom));
            
            Assert.IsTrue(colorAdjustments.active);
            Assert.IsTrue(bloom.active);
            Assert.AreEqual(-50f, colorAdjustments.saturation.value, 0.01f); // Grayscale
            Assert.AreEqual(20f, colorAdjustments.contrast.value, 0.01f); // Contrast (0.7-0.5)*100
            Assert.AreEqual(0.9f, bloom.intensity.value, 0.01f); // Edge detection 0.3*3
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void BenchmarkFilterPerformance_GrayscaleFilter_ReturnsValidMetrics()
        {
            // Arrange
            var parameters = new FilterParameters(FilterType.Grayscale) { intensity = 0.8f };
            
            // Act
            var performanceData = filterManager.BenchmarkFilterPerformance(FilterType.Grayscale, parameters, 10);
            
            // Assert
            Assert.IsNotNull(performanceData);
            Assert.AreEqual(FilterType.Grayscale, performanceData.filterType);
            Assert.AreEqual(10, performanceData.iterations);
            Assert.Greater(performanceData.averageTime, 0f);
            Assert.Greater(performanceData.fps, 0f);
            Assert.LessOrEqual(performanceData.minTime, performanceData.averageTime);
            Assert.GreaterOrEqual(performanceData.maxTime, performanceData.averageTime);
        }
        
        [Test]
        public void GetPerformanceMetrics_WithActiveFilters_ReturnsCorrectMetrics()
        {
            // Arrange
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale));
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement));
            
            // Act
            var metrics = filterManager.GetPerformanceMetrics();
            
            // Assert
            Assert.IsNotNull(metrics);
            Assert.AreEqual(2, metrics.activeFilterCount);
            Assert.Greater(metrics.memoryUsage, 0f);
            Assert.Greater(metrics.fps, 0f);
        }
        
        [Test]
        public void FilterPerformance_TargetFrameRate_MaintainsAcceptablePerformance()
        {
            // Arrange
            var testTexture = new Texture2D(512, 512);
            filterManager.SetSourceTexture(testTexture);
            
            // Act - Apply multiple filters and measure performance
            var startTime = Time.realtimeSinceStartup;
            
            filterManager.ApplyFilter(FilterType.Grayscale, new FilterParameters(FilterType.Grayscale) { intensity = 1f });
            filterManager.ApplyFilter(FilterType.ContrastEnhancement, new FilterParameters(FilterType.ContrastEnhancement) { intensity = 0.8f });
            filterManager.ApplyFilter(FilterType.EdgeDetection, new FilterParameters(FilterType.EdgeDetection) { intensity = 0.6f });
            
            var endTime = Time.realtimeSinceStartup;
            var processingTime = endTime - startTime;
            
            // Assert - Should complete within acceptable time for 60 FPS (16.67ms)
            Assert.Less(processingTime, 0.0167f, "Filter processing should complete within 16.67ms for 60 FPS target");
            
            var metrics = filterManager.GetPerformanceMetrics();
            Assert.IsTrue(metrics.IsPerformanceGood || metrics.fps >= 30f, "Should maintain at least 30 FPS with multiple filters");
            
            // Cleanup
            Object.DestroyImmediate(testTexture);
        }
        
        #endregion
        
        #region Real-Time Preview Tests
        
        [UnityTest]
        public IEnumerator RealTimePreview_EnabledAndDisabled_WorksCorrectly()
        {
            // Arrange
            var testTexture = new Texture2D(256, 256);
            filterManager.SetSourceTexture(testTexture);
            
            // Act - Enable real-time preview
            filterManager.EnableRealTimePreview(true);
            yield return new WaitForSeconds(0.1f);
            
            // Assert - Should be updating
            var metrics = filterManager.GetPerformanceMetrics();
            Assert.IsTrue(metrics.isRealTimeEnabled);
            
            // Act - Disable real-time preview
            filterManager.EnableRealTimePreview(false);
            yield return new WaitForSeconds(0.1f);
            
            // Assert - Should not be updating
            metrics = filterManager.GetPerformanceMetrics();
            Assert.IsFalse(metrics.isRealTimeEnabled);
            
            // Cleanup
            Object.DestroyImmediate(testTexture);
        }
        
        #endregion
    }
}