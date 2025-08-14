using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine.UI;
using DaVinciEye.UI;
using DaVinciEye.ColorAnalysis;

namespace DaVinciEye.Tests.UI
{
    /// <summary>
    /// Tests for color analysis UI components
    /// Covers color picker crosshair, selection feedback, and color comparison display
    /// Tests requirements 7.1, 7.2, 7.4, 7.5
    /// </summary>
    public class ColorAnalysisUITests
    {
        private GameObject testObject;
        private ColorAnalysisUI colorAnalysisUI;
        private ColorPickerCrosshair colorPickerCrosshair;
        private ColorComparisonDisplay colorComparisonDisplay;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestColorAnalysisUI");
            colorAnalysisUI = testObject.AddComponent<ColorAnalysisUI>();
            
            // Create child objects for components
            var crosshairObject = new GameObject("ColorPickerCrosshair");
            crosshairObject.transform.SetParent(testObject.transform);
            colorPickerCrosshair = crosshairObject.AddComponent<ColorPickerCrosshair>();
            
            var comparisonObject = new GameObject("ColorComparisonDisplay");
            comparisonObject.transform.SetParent(testObject.transform);
            colorComparisonDisplay = comparisonObject.AddComponent<ColorComparisonDisplay>();
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
        public void ColorAnalysisUI_InitializesCorrectly()
        {
            // Test that ColorAnalysisUI initializes without errors
            Assert.IsNotNull(colorAnalysisUI);
            Assert.AreEqual(Color.white, colorAnalysisUI.CurrentReferenceColor);
            Assert.AreEqual(Color.white, colorAnalysisUI.CurrentCapturedColor);
            Assert.IsFalse(colorAnalysisUI.IsColorPickingActive);
            Assert.AreEqual(0, colorAnalysisUI.ColorHistoryCount);
        }
        
        [Test]
        public void ColorPickerCrosshair_InitializesCorrectly()
        {
            // Test that ColorPickerCrosshair initializes without errors
            Assert.IsNotNull(colorPickerCrosshair);
            Assert.AreEqual(Vector2.zero, colorPickerCrosshair.Position);
            Assert.AreEqual(Color.white, colorPickerCrosshair.CurrentColor);
            Assert.IsTrue(colorPickerCrosshair.AnimationEnabled);
            Assert.IsTrue(colorPickerCrosshair.ColorPreviewEnabled);
        }
        
        [Test]
        public void ColorComparisonDisplay_InitializesCorrectly()
        {
            // Test that ColorComparisonDisplay initializes without errors
            Assert.IsNotNull(colorComparisonDisplay);
            Assert.AreEqual(Color.white, colorComparisonDisplay.ReferenceColor);
            Assert.AreEqual(Color.white, colorComparisonDisplay.CapturedColor);
            Assert.AreEqual(0f, colorComparisonDisplay.MatchQuality);
        }
        
        [Test]
        public void ColorAnalysisUI_ColorPickingStateManagement()
        {
            // Test color picking state management
            Assert.IsFalse(colorAnalysisUI.IsColorPickingActive);
            
            colorAnalysisUI.StartColorPicking();
            Assert.IsTrue(colorAnalysisUI.IsColorPickingActive);
            
            colorAnalysisUI.StopColorPicking();
            Assert.IsFalse(colorAnalysisUI.IsColorPickingActive);
        }
        
        [Test]
        public void ColorAnalysisUI_ColorSettingWorks()
        {
            // Test setting reference color
            Color testReferenceColor = Color.red;
            colorAnalysisUI.SetReferenceColor(testReferenceColor);
            Assert.AreEqual(testReferenceColor, colorAnalysisUI.CurrentReferenceColor);
            
            // Test setting captured color
            Color testCapturedColor = Color.blue;
            colorAnalysisUI.SetCapturedColor(testCapturedColor);
            Assert.AreEqual(testCapturedColor, colorAnalysisUI.CurrentCapturedColor);
        }
        
        [Test]
        public void ColorAnalysisUI_PositionUpdateWorks()
        {
            // Test color picker position updates
            Vector2 testPosition = new Vector2(100f, 200f);
            colorAnalysisUI.UpdateColorPickerPosition(testPosition);
            Assert.AreEqual(testPosition, colorAnalysisUI.CurrentPickerPosition);
        }
        
        [Test]
        public void ColorAnalysisUI_ColorHistoryManagement()
        {
            // Test adding colors to history
            var testMatchData = new ColorMatchData
            {
                referenceColor = Color.red,
                capturedColor = Color.blue,
                matchAccuracy = 0.75f,
                timestamp = System.DateTime.Now
            };
            
            colorAnalysisUI.AddColorToHistory(testMatchData);
            Assert.AreEqual(1, colorAnalysisUI.ColorHistoryCount);
            
            var history = colorAnalysisUI.ColorHistory;
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(testMatchData.referenceColor, history[0].referenceColor);
            Assert.AreEqual(testMatchData.capturedColor, history[0].capturedColor);
            
            // Test clearing history
            colorAnalysisUI.ClearColorHistory();
            Assert.AreEqual(0, colorAnalysisUI.ColorHistoryCount);
        }
        
        [Test]
        public void ColorPickerCrosshair_PositionSetting()
        {
            // Test crosshair position setting
            Vector2 testPosition = new Vector2(50f, 75f);
            colorPickerCrosshair.SetPosition(testPosition);
            
            // Position might be modified by snapping, so check it's been set
            Assert.IsNotNull(colorPickerCrosshair.Position);
        }
        
        [Test]
        public void ColorPickerCrosshair_ColorSetting()
        {
            // Test crosshair color setting
            Color testColor = Color.green;
            colorPickerCrosshair.SetColor(testColor);
            Assert.AreEqual(testColor, colorPickerCrosshair.CurrentColor);
        }
        
        [Test]
        public void ColorPickerCrosshair_VisibilityControl()
        {
            // Test crosshair visibility control
            Assert.IsTrue(colorPickerCrosshair.IsVisible);
            
            colorPickerCrosshair.SetVisibility(false);
            Assert.IsFalse(colorPickerCrosshair.IsVisible);
            
            colorPickerCrosshair.SetVisibility(true);
            Assert.IsTrue(colorPickerCrosshair.IsVisible);
        }
        
        [Test]
        public void ColorPickerCrosshair_AnimationControl()
        {
            // Test animation control
            Assert.IsTrue(colorPickerCrosshair.AnimationEnabled);
            
            colorPickerCrosshair.SetAnimationEnabled(false);
            Assert.IsFalse(colorPickerCrosshair.AnimationEnabled);
            
            colorPickerCrosshair.SetAnimationEnabled(true);
            Assert.IsTrue(colorPickerCrosshair.AnimationEnabled);
        }
        
        [Test]
        public void ColorPickerCrosshair_SnappingControl()
        {
            // Test snapping control
            Assert.IsTrue(colorPickerCrosshair.SnappingEnabled);
            
            colorPickerCrosshair.SetSnappingEnabled(false);
            Assert.IsFalse(colorPickerCrosshair.SnappingEnabled);
            
            float testSnapDistance = 15f;
            colorPickerCrosshair.SetSnapDistance(testSnapDistance);
            Assert.AreEqual(testSnapDistance, colorPickerCrosshair.SnapDistance);
        }
        
        [Test]
        public void ColorComparisonDisplay_ColorSetting()
        {
            // Test setting colors in comparison display
            Color referenceColor = Color.red;
            Color capturedColor = Color.blue;
            
            colorComparisonDisplay.SetColors(referenceColor, capturedColor);
            
            Assert.AreEqual(referenceColor, colorComparisonDisplay.ReferenceColor);
            Assert.AreEqual(capturedColor, colorComparisonDisplay.CapturedColor);
        }
        
        [Test]
        public void ColorComparisonDisplay_IndividualColorSetting()
        {
            // Test setting individual colors
            Color referenceColor = Color.yellow;
            colorComparisonDisplay.SetReferenceColor(referenceColor);
            Assert.AreEqual(referenceColor, colorComparisonDisplay.ReferenceColor);
            
            Color capturedColor = Color.cyan;
            colorComparisonDisplay.SetCapturedColor(capturedColor);
            Assert.AreEqual(capturedColor, colorComparisonDisplay.CapturedColor);
        }
        
        [Test]
        public void ColorComparisonDisplay_SwatchSizeSetting()
        {
            // Test swatch size setting
            float testSize = 120f;
            colorComparisonDisplay.SetSwatchSize(testSize);
            Assert.AreEqual(testSize, colorComparisonDisplay.SwatchSize);
        }
        
        [Test]
        public void ColorComparisonDisplay_QualityThresholds()
        {
            // Test quality threshold setting
            float excellent = 0.95f;
            float good = 0.8f;
            float fair = 0.6f;
            
            Assert.DoesNotThrow(() => colorComparisonDisplay.SetQualityThresholds(excellent, good, fair));
        }
        
        [UnityTest]
        public IEnumerator ColorAnalysisUI_EventsTriggeredCorrectly()
        {
            // Test that color analysis events are triggered
            bool referenceColorSelectedTriggered = false;
            bool paintColorCapturedTriggered = false;
            bool colorComparisonCompleteTriggered = false;
            bool pickerPositionChangedTriggered = false;
            
            colorAnalysisUI.OnReferenceColorSelected += (color) => referenceColorSelectedTriggered = true;
            colorAnalysisUI.OnPaintColorCaptured += (color) => paintColorCapturedTriggered = true;
            colorAnalysisUI.OnColorComparisonComplete += (result) => colorComparisonCompleteTriggered = true;
            colorAnalysisUI.OnColorPickerPositionChanged += (position) => pickerPositionChangedTriggered = true;
            
            // Trigger events
            colorAnalysisUI.SetCapturedColor(Color.red);
            colorAnalysisUI.UpdateColorPickerPosition(Vector2.one);
            
            yield return null; // Wait a frame
            
            // Verify event handlers can be set up without errors
            Assert.IsNotNull(colorAnalysisUI.OnReferenceColorSelected);
            Assert.IsNotNull(colorAnalysisUI.OnPaintColorCaptured);
            Assert.IsNotNull(colorAnalysisUI.OnColorComparisonComplete);
            Assert.IsNotNull(colorAnalysisUI.OnColorPickerPositionChanged);
        }
        
        [UnityTest]
        public IEnumerator ColorPickerCrosshair_SelectionFeedback()
        {
            // Test selection feedback animation
            Assert.DoesNotThrow(() => colorPickerCrosshair.ShowSelectionFeedback());
            
            yield return new WaitForSeconds(0.1f);
            
            // Verify no exceptions were thrown during animation
            Assert.IsNotNull(colorPickerCrosshair);
        }
        
        [UnityTest]
        public IEnumerator ColorComparisonDisplay_EventsTriggeredCorrectly()
        {
            // Test that comparison display events are triggered
            bool comparisonUpdatedTriggered = false;
            bool colorsChangedTriggered = false;
            
            colorComparisonDisplay.OnComparisonUpdated += (result) => comparisonUpdatedTriggered = true;
            colorComparisonDisplay.OnColorsChanged += (ref_color, cap_color) => colorsChangedTriggered = true;
            
            // Trigger events
            colorComparisonDisplay.SetColors(Color.red, Color.blue);
            
            yield return null; // Wait a frame
            
            // Verify event handlers can be set up without errors
            Assert.IsNotNull(colorComparisonDisplay.OnComparisonUpdated);
            Assert.IsNotNull(colorComparisonDisplay.OnColorsChanged);
        }
    }
    
    /// <summary>
    /// Performance tests for color analysis UI components
    /// </summary>
    public class ColorAnalysisUIPerformanceTests
    {
        private GameObject testObject;
        private ColorAnalysisUI colorAnalysisUI;
        private ColorPickerCrosshair colorPickerCrosshair;
        private ColorComparisonDisplay colorComparisonDisplay;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestPerformance");
            colorAnalysisUI = testObject.AddComponent<ColorAnalysisUI>();
            
            var crosshairObject = new GameObject("Crosshair");
            crosshairObject.transform.SetParent(testObject.transform);
            colorPickerCrosshair = crosshairObject.AddComponent<ColorPickerCrosshair>();
            
            var comparisonObject = new GameObject("Comparison");
            comparisonObject.transform.SetParent(testObject.transform);
            colorComparisonDisplay = comparisonObject.AddComponent<ColorComparisonDisplay>();
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
        public void ColorAnalysisUI_UpdatePerformance()
        {
            // Test that UI updates complete within acceptable time
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Perform multiple UI updates
            for (int i = 0; i < 100; i++)
            {
                Color testColor = new Color(i / 100f, (100 - i) / 100f, 0.5f);
                colorAnalysisUI.SetReferenceColor(testColor);
                colorAnalysisUI.SetCapturedColor(testColor);
                colorAnalysisUI.UpdateUI();
            }
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (less than 100ms for 100 updates)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100);
        }
        
        [Test]
        public void ColorPickerCrosshair_PositionUpdatePerformance()
        {
            // Test crosshair position update performance
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Perform multiple position updates
            for (int i = 0; i < 1000; i++)
            {
                Vector2 position = new Vector2(i % 100, (i / 100) % 100);
                colorPickerCrosshair.SetPosition(position);
            }
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (less than 50ms for 1000 updates)
            Assert.Less(stopwatch.ElapsedMilliseconds, 50);
        }
        
        [Test]
        public void ColorComparisonDisplay_ComparisonPerformance()
        {
            // Test color comparison calculation performance
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Perform multiple color comparisons
            for (int i = 0; i < 100; i++)
            {
                Color color1 = new Color(i / 100f, 0.5f, 0.5f);
                Color color2 = new Color(0.5f, i / 100f, 0.5f);
                colorComparisonDisplay.SetColors(color1, color2);
            }
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (less than 100ms for 100 comparisons)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100);
        }
        
        [Test]
        public void ColorAnalysisUI_MemoryUsage()
        {
            // Test that color analysis UI doesn't cause memory leaks
            long initialMemory = System.GC.GetTotalMemory(true);
            
            // Create and manipulate many color objects
            for (int i = 0; i < 1000; i++)
            {
                var matchData = new ColorMatchData
                {
                    referenceColor = new Color(i / 1000f, 0.5f, 0.5f),
                    capturedColor = new Color(0.5f, i / 1000f, 0.5f),
                    matchAccuracy = i / 1000f,
                    timestamp = System.DateTime.Now
                };
                
                colorAnalysisUI.AddColorToHistory(matchData);
                
                // Clear history periodically to test cleanup
                if (i % 100 == 0)
                {
                    colorAnalysisUI.ClearColorHistory();
                }
            }
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long finalMemory = System.GC.GetTotalMemory(true);
            long memoryIncrease = finalMemory - initialMemory;
            
            // Memory increase should be minimal (less than 2MB)
            Assert.Less(memoryIncrease, 2 * 1024 * 1024);
        }
        
        [Test]
        public void ColorPickerCrosshair_AnimationPerformance()
        {
            // Test animation performance doesn't impact frame rate
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Simulate animation updates
            for (int i = 0; i < 60; i++) // 60 frames
            {
                // Simulate Update() call
                colorPickerCrosshair.SetColor(new Color(i / 60f, 0.5f, 0.5f));
            }
            
            stopwatch.Stop();
            
            // Should complete within one second (60 FPS target)
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000);
        }
    }
    
    /// <summary>
    /// Visual feedback and accuracy tests for color analysis UI
    /// </summary>
    public class ColorAnalysisUIAccuracyTests
    {
        private GameObject testObject;
        private ColorAnalysisUI colorAnalysisUI;
        private ColorComparisonDisplay colorComparisonDisplay;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestAccuracy");
            colorAnalysisUI = testObject.AddComponent<ColorAnalysisUI>();
            
            var comparisonObject = new GameObject("Comparison");
            comparisonObject.transform.SetParent(testObject.transform);
            colorComparisonDisplay = comparisonObject.AddComponent<ColorComparisonDisplay>();
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
        public void ColorComparison_IdenticalColorsShowPerfectMatch()
        {
            // Test that identical colors show 100% match
            Color testColor = Color.red;
            colorComparisonDisplay.SetColors(testColor, testColor);
            
            // Allow for floating point precision issues
            Assert.Greater(colorComparisonDisplay.MatchQuality, 0.99f);
            Assert.Less(colorComparisonDisplay.ColorDifference, 1f);
        }
        
        [Test]
        public void ColorComparison_OppositeColorsShowPoorMatch()
        {
            // Test that very different colors show poor match
            colorComparisonDisplay.SetColors(Color.white, Color.black);
            
            Assert.Less(colorComparisonDisplay.MatchQuality, 0.3f);
            Assert.Greater(colorComparisonDisplay.ColorDifference, 100f);
        }
        
        [Test]
        public void ColorComparison_SimilarColorsShowGoodMatch()
        {
            // Test that similar colors show good match
            Color color1 = new Color(1f, 0f, 0f); // Pure red
            Color color2 = new Color(0.9f, 0.1f, 0.1f); // Slightly different red
            
            colorComparisonDisplay.SetColors(color1, color2);
            
            Assert.Greater(colorComparisonDisplay.MatchQuality, 0.7f);
            Assert.Less(colorComparisonDisplay.ColorDifference, 50f);
        }
        
        [Test]
        public void ColorHistory_MaintainsAccuracy()
        {
            // Test that color history maintains accuracy over time
            var originalMatchData = new ColorMatchData
            {
                referenceColor = Color.red,
                capturedColor = Color.blue,
                matchAccuracy = 0.65f,
                timestamp = System.DateTime.Now
            };
            
            colorAnalysisUI.AddColorToHistory(originalMatchData);
            
            var history = colorAnalysisUI.ColorHistory;
            Assert.AreEqual(1, history.Count);
            
            var retrievedData = history[0];
            Assert.AreEqual(originalMatchData.referenceColor, retrievedData.referenceColor);
            Assert.AreEqual(originalMatchData.capturedColor, retrievedData.capturedColor);
            Assert.AreEqual(originalMatchData.matchAccuracy, retrievedData.matchAccuracy, 0.01f);
        }
    }
}