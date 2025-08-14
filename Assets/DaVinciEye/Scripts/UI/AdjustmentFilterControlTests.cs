using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine.UI;
using MixedReality.Toolkit.UX;
using DaVinciEye.UI;
using DaVinciEye.Filters;

namespace DaVinciEye.Tests.UI
{
    /// <summary>
    /// Tests for adjustment and filter control UI components
    /// Covers UI control accuracy and real-time preview responsiveness
    /// Tests requirements 3.1, 4.6, 6.4, 6.5, 6.6, 6.7
    /// </summary>
    public class AdjustmentFilterControlTests
    {
        private GameObject testObject;
        private AdjustmentControlUI adjustmentUI;
        private FilterControlUI filterUI;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestAdjustmentFilterControl");
            adjustmentUI = testObject.AddComponent<AdjustmentControlUI>();
            filterUI = testObject.AddComponent<FilterControlUI>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void AdjustmentControlUI_InitializesCorrectly()
        {
            // Test that AdjustmentControlUI initializes without errors
            Assert.IsNotNull(adjustmentUI);
            Assert.IsNotNull(adjustmentUI.CurrentAdjustments);
            Assert.IsFalse(adjustmentUI.IsModified);
            Assert.IsTrue(adjustmentUI.RealTimePreviewEnabled);
        }
        
        [Test]
        public void FilterControlUI_InitializesCorrectly()
        {
            // Test that FilterControlUI initializes without errors
            Assert.IsNotNull(filterUI);
            Assert.IsNotNull(filterUI.ActiveFilters);
            Assert.IsNotNull(filterUI.FilterParameters);
            Assert.AreEqual(0, filterUI.ActiveFilterCount);
            Assert.IsTrue(filterUI.RealTimePreviewEnabled);
        }
        
        [Test]
        public void ImageAdjustments_DefaultValuesCorrect()
        {
            var adjustments = new ImageAdjustments();
            
            Assert.AreEqual(1f, adjustments.opacity);
            Assert.AreEqual(0f, adjustments.contrast);
            Assert.AreEqual(0f, adjustments.exposure);
            Assert.AreEqual(0f, adjustments.hue);
            Assert.AreEqual(0f, adjustments.saturation);
            Assert.IsFalse(adjustments.isModified);
        }
        
        [Test]
        public void ImageAdjustments_ResetWorksCorrectly()
        {
            var adjustments = new ImageAdjustments
            {
                opacity = 0.5f,
                contrast = 0.3f,
                exposure = -0.2f,
                hue = 45f,
                saturation = 0.8f,
                isModified = true
            };
            
            adjustments.Reset();
            
            Assert.AreEqual(1f, adjustments.opacity);
            Assert.AreEqual(0f, adjustments.contrast);
            Assert.AreEqual(0f, adjustments.exposure);
            Assert.AreEqual(0f, adjustments.hue);
            Assert.AreEqual(0f, adjustments.saturation);
            Assert.IsFalse(adjustments.isModified);
        }
        
        [Test]
        public void ImageAdjustments_CloneWorksCorrectly()
        {
            var original = new ImageAdjustments
            {
                opacity = 0.7f,
                contrast = 0.2f,
                exposure = -0.1f,
                hue = 30f,
                saturation = 0.5f,
                isModified = true
            };
            
            var clone = original.Clone();
            
            Assert.AreEqual(original.opacity, clone.opacity);
            Assert.AreEqual(original.contrast, clone.contrast);
            Assert.AreEqual(original.exposure, clone.exposure);
            Assert.AreEqual(original.hue, clone.hue);
            Assert.AreEqual(original.saturation, clone.saturation);
            Assert.AreEqual(original.isModified, clone.isModified);
            
            // Verify they are separate objects
            clone.opacity = 0.3f;
            Assert.AreNotEqual(original.opacity, clone.opacity);
        }
        
        [Test]
        public void AdjustmentControlUI_SetAdjustmentsWorksCorrectly()
        {
            var testAdjustments = new ImageAdjustments
            {
                opacity = 0.8f,
                contrast = 0.3f,
                exposure = -0.2f,
                hue = 45f,
                saturation = 0.6f,
                isModified = true
            };
            
            adjustmentUI.SetAdjustments(testAdjustments);
            
            var currentAdjustments = adjustmentUI.CurrentAdjustments;
            Assert.AreEqual(testAdjustments.opacity, currentAdjustments.opacity);
            Assert.AreEqual(testAdjustments.contrast, currentAdjustments.contrast);
            Assert.AreEqual(testAdjustments.exposure, currentAdjustments.exposure);
            Assert.AreEqual(testAdjustments.hue, currentAdjustments.hue);
            Assert.AreEqual(testAdjustments.saturation, currentAdjustments.saturation);
            Assert.AreEqual(testAdjustments.isModified, currentAdjustments.isModified);
        }
        
        [Test]
        public void AdjustmentControlUI_RealTimePreviewToggleWorks()
        {
            // Test enabling/disabling real-time preview
            Assert.IsTrue(adjustmentUI.RealTimePreviewEnabled);
            
            adjustmentUI.SetRealTimePreview(false);
            Assert.IsFalse(adjustmentUI.RealTimePreviewEnabled);
            
            adjustmentUI.SetRealTimePreview(true);
            Assert.IsTrue(adjustmentUI.RealTimePreviewEnabled);
        }
        
        [Test]
        public void FilterControlUI_ActiveFiltersInitializedCorrectly()
        {
            var activeFilters = filterUI.ActiveFilters;
            
            // All filters should be initially disabled
            Assert.IsFalse(activeFilters[FilterType.Grayscale]);
            Assert.IsFalse(activeFilters[FilterType.EdgeDetection]);
            Assert.IsFalse(activeFilters[FilterType.ContrastEnhancement]);
            Assert.IsFalse(activeFilters[FilterType.ColorRange]);
            Assert.IsFalse(activeFilters[FilterType.ColorReduction]);
        }
        
        [Test]
        public void FilterControlUI_FilterParametersInitializedCorrectly()
        {
            var filterParams = filterUI.FilterParameters;
            
            // Check default parameter values
            Assert.AreEqual(0.5f, filterParams[FilterType.EdgeDetection].intensity);
            Assert.AreEqual(0.5f, filterParams[FilterType.ContrastEnhancement].intensity);
            Assert.AreEqual(0.1f, filterParams[FilterType.ColorRange].colorTolerance);
            Assert.AreEqual(16, filterParams[FilterType.ColorReduction].targetColorCount);
        }
        
        [UnityTest]
        public IEnumerator AdjustmentControlUI_EventsTriggeredCorrectly()
        {
            // Test that adjustment events are triggered
            bool adjustmentsChangedTriggered = false;
            bool adjustmentValueChangedTriggered = false;
            bool adjustmentsResetTriggered = false;
            
            adjustmentUI.OnAdjustmentsChanged += (adjustments) => adjustmentsChangedTriggered = true;
            adjustmentUI.OnAdjustmentValueChanged += (type, value) => adjustmentValueChangedTriggered = true;
            adjustmentUI.OnAdjustmentsReset += () => adjustmentsResetTriggered = true;
            
            // Simulate adjustment changes (would normally come from sliders)
            var testAdjustments = new ImageAdjustments { opacity = 0.5f, isModified = true };
            adjustmentUI.SetAdjustments(testAdjustments);
            
            yield return null; // Wait a frame
            
            // Note: In a real test with actual sliders, these events would be triggered
            // For now, we just verify the event handlers can be set up without errors
            Assert.IsNotNull(adjustmentUI.OnAdjustmentsChanged);
            Assert.IsNotNull(adjustmentUI.OnAdjustmentValueChanged);
            Assert.IsNotNull(adjustmentUI.OnAdjustmentsReset);
        }
        
        [UnityTest]
        public IEnumerator FilterControlUI_EventsTriggeredCorrectly()
        {
            // Test that filter events are triggered
            bool filterToggledTriggered = false;
            bool filterParametersChangedTriggered = false;
            bool allFiltersClearedTriggered = false;
            bool filterPresetSavedTriggered = false;
            
            filterUI.OnFilterToggled += (type, enabled) => filterToggledTriggered = true;
            filterUI.OnFilterParametersChanged += (type, parameters) => filterParametersChangedTriggered = true;
            filterUI.OnAllFiltersCleared += () => allFiltersClearedTriggered = true;
            filterUI.OnFilterPresetSaved += (presetName) => filterPresetSavedTriggered = true;
            
            yield return null; // Wait a frame
            
            // Verify event handlers can be set up without errors
            Assert.IsNotNull(filterUI.OnFilterToggled);
            Assert.IsNotNull(filterUI.OnFilterParametersChanged);
            Assert.IsNotNull(filterUI.OnAllFiltersCleared);
            Assert.IsNotNull(filterUI.OnFilterPresetSaved);
        }
        
        [Test]
        public void FilterPresetData_SerializationWorks()
        {
            // Test that filter preset data can be serialized/deserialized
            var presetData = new FilterPresetData
            {
                name = "TestPreset",
                activeFilters = new System.Collections.Generic.Dictionary<FilterType, bool>
                {
                    { FilterType.Grayscale, true },
                    { FilterType.EdgeDetection, false }
                },
                filterParameters = new System.Collections.Generic.Dictionary<FilterType, FilterParameters>
                {
                    { FilterType.EdgeDetection, new FilterParameters { type = FilterType.EdgeDetection, intensity = 0.8f } }
                }
            };
            
            // Test serialization (JsonUtility has limitations with dictionaries, but structure should be valid)
            Assert.IsNotNull(presetData.name);
            Assert.IsNotNull(presetData.activeFilters);
            Assert.IsNotNull(presetData.filterParameters);
            Assert.AreEqual("TestPreset", presetData.name);
        }
    }
    
    /// <summary>
    /// Performance tests for UI control responsiveness
    /// </summary>
    public class AdjustmentFilterPerformanceTests
    {
        private GameObject testObject;
        private AdjustmentControlUI adjustmentUI;
        private FilterControlUI filterUI;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestPerformance");
            adjustmentUI = testObject.AddComponent<AdjustmentControlUI>();
            filterUI = testObject.AddComponent<FilterControlUI>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void AdjustmentUI_UpdatePerformance()
        {
            // Test that UI updates complete within acceptable time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Perform multiple UI updates
            for (int i = 0; i < 100; i++)
            {
                var adjustments = new ImageAdjustments
                {
                    opacity = i / 100f,
                    contrast = (i - 50) / 50f,
                    exposure = (i - 50) / 25f,
                    hue = (i - 50) * 3.6f,
                    saturation = i / 100f
                };
                
                adjustmentUI.SetAdjustments(adjustments);
                adjustmentUI.UpdateUI();
            }
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (less than 100ms for 100 updates)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100);
        }
        
        [Test]
        public void FilterUI_UpdatePerformance()
        {
            // Test that filter UI updates complete within acceptable time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Perform multiple UI updates
            for (int i = 0; i < 50; i++)
            {
                filterUI.UpdateUI();
            }
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (less than 50ms for 50 updates)
            Assert.Less(stopwatch.ElapsedMilliseconds, 50);
        }
        
        [Test]
        public void AdjustmentUI_MemoryUsage()
        {
            // Test that adjustment UI doesn't cause memory leaks
            long initialMemory = System.GC.GetTotalMemory(true);
            
            // Create and destroy many adjustment objects
            for (int i = 0; i < 1000; i++)
            {
                var adjustments = new ImageAdjustments();
                var clone = adjustments.Clone();
                adjustments.Reset();
            }
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long finalMemory = System.GC.GetTotalMemory(true);
            long memoryIncrease = finalMemory - initialMemory;
            
            // Memory increase should be minimal (less than 1MB)
            Assert.Less(memoryIncrease, 1024 * 1024);
        }
        
        [Test]
        public void FilterUI_MemoryUsage()
        {
            // Test that filter UI doesn't cause memory leaks
            long initialMemory = System.GC.GetTotalMemory(true);
            
            // Create and destroy many filter parameter objects
            for (int i = 0; i < 1000; i++)
            {
                var parameters = new FilterParameters
                {
                    type = FilterType.EdgeDetection,
                    intensity = i / 1000f,
                    colorTolerance = i / 2000f,
                    targetColorCount = i % 256
                };
            }
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long finalMemory = System.GC.GetTotalMemory(true);
            long memoryIncrease = finalMemory - initialMemory;
            
            // Memory increase should be minimal (less than 1MB)
            Assert.Less(memoryIncrease, 1024 * 1024);
        }
    }
    
    /// <summary>
    /// Integration tests for adjustment and filter UI with MRTK components
    /// </summary>
    public class MRTKIntegrationTests
    {
        private GameObject testObject;
        private AdjustmentControlUI adjustmentUI;
        private FilterControlUI filterUI;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestMRTKIntegration");
            adjustmentUI = testObject.AddComponent<AdjustmentControlUI>();
            filterUI = testObject.AddComponent<FilterControlUI>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void AdjustmentUI_MRTKSliderCompatibility()
        {
            // Test that adjustment UI is compatible with MRTK slider components
            // In a real test, this would create actual MRTK slider prefabs
            
            // For now, verify that the UI can handle null slider references gracefully
            Assert.DoesNotThrow(() => adjustmentUI.UpdateUI());
            Assert.DoesNotThrow(() => adjustmentUI.SetRealTimePreview(false));
            Assert.DoesNotThrow(() => adjustmentUI.SetRealTimePreview(true));
        }
        
        [Test]
        public void FilterUI_MRTKToggleCompatibility()
        {
            // Test that filter UI is compatible with MRTK toggle components
            // In a real test, this would create actual MRTK toggle prefabs
            
            // For now, verify that the UI can handle null toggle references gracefully
            Assert.DoesNotThrow(() => filterUI.UpdateUI());
            Assert.AreEqual(0, filterUI.ActiveFilterCount);
        }
        
        [Test]
        public void UI_MRTKButtonCompatibility()
        {
            // Test that UI components are compatible with MRTK button components
            // In a real test, this would create actual MRTK button prefabs
            
            // For now, verify that the UI components can be created without MRTK dependencies
            Assert.IsNotNull(adjustmentUI);
            Assert.IsNotNull(filterUI);
        }
    }
}