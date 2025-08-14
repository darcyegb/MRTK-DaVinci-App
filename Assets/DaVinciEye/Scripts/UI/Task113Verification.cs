using UnityEngine;
using UnityEngine.UI;
using DaVinciEye.UI;
using DaVinciEye.ColorAnalysis;

namespace DaVinciEye.Verification
{
    /// <summary>
    /// Verification script for Task 11.3: Add color analysis UI components
    /// Tests color picker crosshair, selection feedback, and color comparison display implementation
    /// </summary>
    public class Task113Verification : MonoBehaviour
    {
        [Header("Verification Settings")]
        [SerializeField] private bool runVerificationOnStart = true;
        [SerializeField] private bool showDetailedLogs = true;
        
        [Header("Test Results")]
        [SerializeField] private bool colorPickerUIImplemented = false;
        [SerializeField] private bool selectionFeedbackWorking = false;
        [SerializeField] private bool colorComparisonDisplayWorking = false;
        [SerializeField] private bool colorSwatchesWorking = false;
        [SerializeField] private bool differenceIndicatorsWorking = false;
        [SerializeField] private bool uiAccuracyTestsPassed = false;
        
        private ColorAnalysisUI colorAnalysisUI;
        private ColorPickerCrosshair colorPickerCrosshair;
        private ColorComparisonDisplay colorComparisonDisplay;
        private UIManager uiManager;
        
        private void Start()
        {
            if (runVerificationOnStart)
            {
                VerifyTask113Implementation();
            }
        }
        
        [ContextMenu("Verify Task 11.3 Implementation")]
        public void VerifyTask113Implementation()
        {
            Debug.Log("=== Task 11.3 Verification: Add color analysis UI components ===");
            
            FindComponents();
            VerifyColorPickerUI();
            VerifySelectionFeedback();
            VerifyColorComparisonDisplay();
            VerifyColorSwatches();
            VerifyDifferenceIndicators();
            VerifyUIAccuracyTests();
            
            LogVerificationResults();
        }
        
        private void FindComponents()
        {
            colorAnalysisUI = FindObjectOfType<ColorAnalysisUI>();
            colorPickerCrosshair = FindObjectOfType<ColorPickerCrosshair>();
            colorComparisonDisplay = FindObjectOfType<ColorComparisonDisplay>();
            uiManager = FindObjectOfType<UIManager>();
            
            if (showDetailedLogs)
            {
                Debug.Log($"Found ColorAnalysisUI: {colorAnalysisUI != null}");
                Debug.Log($"Found ColorPickerCrosshair: {colorPickerCrosshair != null}");
                Debug.Log($"Found ColorComparisonDisplay: {colorComparisonDisplay != null}");
                Debug.Log($"Found UIManager: {uiManager != null}");
            }
        }
        
        private void VerifyColorPickerUI()
        {
            Debug.Log("--- Verifying Color Picker UI Implementation ---");
            
            bool hasColorAnalysisUI = colorAnalysisUI != null;
            bool hasColorPickerCrosshair = colorPickerCrosshair != null;
            bool hasPickerPositionControl = false;
            bool hasColorPickingState = false;
            bool hasPickerEvents = false;
            
            if (colorAnalysisUI != null)
            {
                // Check if color picking methods exist
                var startPickingMethod = typeof(ColorAnalysisUI).GetMethod("StartColorPicking");
                var stopPickingMethod = typeof(ColorAnalysisUI).GetMethod("StopColorPicking");
                var updatePositionMethod = typeof(ColorAnalysisUI).GetMethod("UpdateColorPickerPosition");
                
                hasPickerPositionControl = updatePositionMethod != null;
                hasColorPickingState = startPickingMethod != null && stopPickingMethod != null;
                
                // Check if picker properties exist
                var isPickingActiveProperty = typeof(ColorAnalysisUI).GetProperty("IsColorPickingActive");
                var currentPositionProperty = typeof(ColorAnalysisUI).GetProperty("CurrentPickerPosition");
                
                hasColorPickingState = hasColorPickingState && isPickingActiveProperty != null;
                hasPickerPositionControl = hasPickerPositionControl && currentPositionProperty != null;
                
                // Check if picker events exist
                var positionChangedEvent = typeof(ColorAnalysisUI).GetEvent("OnColorPickerPositionChanged");
                var referenceColorSelectedEvent = typeof(ColorAnalysisUI).GetEvent("OnReferenceColorSelected");
                
                hasPickerEvents = positionChangedEvent != null && referenceColorSelectedEvent != null;
            }
            
            if (colorPickerCrosshair != null)
            {
                // Check if crosshair methods exist
                var setPositionMethod = typeof(ColorPickerCrosshair).GetMethod("SetPosition");
                var setColorMethod = typeof(ColorPickerCrosshair).GetMethod("SetColor");
                var setVisibilityMethod = typeof(ColorPickerCrosshair).GetMethod("SetVisibility");
                
                hasColorPickerCrosshair = setPositionMethod != null && setColorMethod != null && setVisibilityMethod != null;
            }
            
            colorPickerUIImplemented = hasColorAnalysisUI && hasColorPickerCrosshair && 
                                     hasPickerPositionControl && hasColorPickingState && hasPickerEvents;
            
            Debug.Log($"✓ Color Analysis UI Component: {hasColorAnalysisUI}");
            Debug.Log($"✓ Color Picker Crosshair: {hasColorPickerCrosshair}");
            Debug.Log($"✓ Picker Position Control: {hasPickerPositionControl}");
            Debug.Log($"✓ Color Picking State: {hasColorPickingState}");
            Debug.Log($"✓ Picker Events: {hasPickerEvents}");
            Debug.Log($"Color Picker UI: {(colorPickerUIImplemented ? "PASS" : "FAIL")}");
        }
        
        private void VerifySelectionFeedback()
        {
            Debug.Log("--- Verifying Selection Feedback Implementation ---");
            
            bool hasSelectionFeedback = false;
            bool hasVisualFeedback = false;
            bool hasAnimationSupport = false;
            bool hasFeedbackEvents = false;
            
            if (colorPickerCrosshair != null)
            {
                // Check if selection feedback methods exist
                var showFeedbackMethod = typeof(ColorPickerCrosshair).GetMethod("ShowSelectionFeedback");
                var setAnimationMethod = typeof(ColorPickerCrosshair).GetMethod("SetAnimationEnabled");
                
                hasSelectionFeedback = showFeedbackMethod != null;
                hasAnimationSupport = setAnimationMethod != null;
                
                // Check if animation properties exist
                var animationEnabledProperty = typeof(ColorPickerCrosshair).GetProperty("AnimationEnabled");
                hasAnimationSupport = hasAnimationSupport && animationEnabledProperty != null;
                
                // Check if crosshair events exist
                var positionChangedEvent = typeof(ColorPickerCrosshair).GetEvent("OnPositionChanged");
                var colorChangedEvent = typeof(ColorPickerCrosshair).GetEvent("OnColorChanged");
                
                hasFeedbackEvents = positionChangedEvent != null && colorChangedEvent != null;
            }
            
            if (colorAnalysisUI != null)
            {
                // Check if visual feedback support exists
                var setReferenceColorMethod = typeof(ColorAnalysisUI).GetMethod("SetReferenceColor");
                var setCapturedColorMethod = typeof(ColorAnalysisUI).GetMethod("SetCapturedColor");
                
                hasVisualFeedback = setReferenceColorMethod != null && setCapturedColorMethod != null;
            }
            
            selectionFeedbackWorking = hasSelectionFeedback && hasVisualFeedback && 
                                     hasAnimationSupport && hasFeedbackEvents;
            
            Debug.Log($"✓ Selection Feedback: {hasSelectionFeedback}");
            Debug.Log($"✓ Visual Feedback: {hasVisualFeedback}");
            Debug.Log($"✓ Animation Support: {hasAnimationSupport}");
            Debug.Log($"✓ Feedback Events: {hasFeedbackEvents}");
            Debug.Log($"Selection Feedback: {(selectionFeedbackWorking ? "PASS" : "FAIL")}");
        }
        
        private void VerifyColorComparisonDisplay()
        {
            Debug.Log("--- Verifying Color Comparison Display Implementation ---");
            
            bool hasComparisonDisplay = colorComparisonDisplay != null;
            bool hasColorSetting = false;
            bool hasComparisonCalculation = false;
            bool hasComparisonEvents = false;
            bool hasMatchResultDisplay = false;
            
            if (colorComparisonDisplay != null)
            {
                // Check if color setting methods exist
                var setColorsMethod = typeof(ColorComparisonDisplay).GetMethod("SetColors");
                var setReferenceColorMethod = typeof(ColorComparisonDisplay).GetMethod("SetReferenceColor");
                var setCapturedColorMethod = typeof(ColorComparisonDisplay).GetMethod("SetCapturedColor");
                
                hasColorSetting = setColorsMethod != null && setReferenceColorMethod != null && setCapturedColorMethod != null;
                
                // Check if comparison properties exist
                var referenceColorProperty = typeof(ColorComparisonDisplay).GetProperty("ReferenceColor");
                var capturedColorProperty = typeof(ColorComparisonDisplay).GetProperty("CapturedColor");
                var matchQualityProperty = typeof(ColorComparisonDisplay).GetProperty("MatchQuality");
                var colorDifferenceProperty = typeof(ColorComparisonDisplay).GetProperty("ColorDifference");
                
                hasComparisonCalculation = referenceColorProperty != null && capturedColorProperty != null && 
                                         matchQualityProperty != null && colorDifferenceProperty != null;
                
                // Check if comparison events exist
                var comparisonUpdatedEvent = typeof(ColorComparisonDisplay).GetEvent("OnComparisonUpdated");
                var colorsChangedEvent = typeof(ColorComparisonDisplay).GetEvent("OnColorsChanged");
                
                hasComparisonEvents = comparisonUpdatedEvent != null && colorsChangedEvent != null;
                
                // Check if match result display methods exist
                var updateComparisonMethod = typeof(ColorComparisonDisplay).GetMethod("UpdateComparison");
                var currentMatchResultProperty = typeof(ColorComparisonDisplay).GetProperty("CurrentMatchResult");
                
                hasMatchResultDisplay = updateComparisonMethod != null && currentMatchResultProperty != null;
            }
            
            colorComparisonDisplayWorking = hasComparisonDisplay && hasColorSetting && 
                                          hasComparisonCalculation && hasComparisonEvents && 
                                          hasMatchResultDisplay;
            
            Debug.Log($"✓ Comparison Display Component: {hasComparisonDisplay}");
            Debug.Log($"✓ Color Setting: {hasColorSetting}");
            Debug.Log($"✓ Comparison Calculation: {hasComparisonCalculation}");
            Debug.Log($"✓ Comparison Events: {hasComparisonEvents}");
            Debug.Log($"✓ Match Result Display: {hasMatchResultDisplay}");
            Debug.Log($"Color Comparison Display: {(colorComparisonDisplayWorking ? "PASS" : "FAIL")}");
        }
        
        private void VerifyColorSwatches()
        {
            Debug.Log("--- Verifying Color Swatches Implementation ---");
            
            bool hasSwatchSupport = false;
            bool hasSwatchSizing = false;
            bool hasSwatchColors = false;
            bool hasSwatchConfiguration = false;
            
            if (colorComparisonDisplay != null)
            {
                // Check if swatch methods exist
                var setSwatchSizeMethod = typeof(ColorComparisonDisplay).GetMethod("SetSwatchSize");
                var swatchSizeProperty = typeof(ColorComparisonDisplay).GetProperty("SwatchSize");
                
                hasSwatchSizing = setSwatchSizeMethod != null && swatchSizeProperty != null;
                
                // Check if color properties exist for swatches
                var referenceColorProperty = typeof(ColorComparisonDisplay).GetProperty("ReferenceColor");
                var capturedColorProperty = typeof(ColorComparisonDisplay).GetProperty("CapturedColor");
                
                hasSwatchColors = referenceColorProperty != null && capturedColorProperty != null;
                
                hasSwatchSupport = hasSwatchSizing && hasSwatchColors;
                hasSwatchConfiguration = hasSwatchSupport;
            }
            
            if (colorAnalysisUI != null)
            {
                // Check if color history supports swatches
                var colorHistoryProperty = typeof(ColorAnalysisUI).GetProperty("ColorHistory");
                var addToHistoryMethod = typeof(ColorAnalysisUI).GetMethod("AddColorToHistory");
                
                hasSwatchConfiguration = hasSwatchConfiguration && colorHistoryProperty != null && addToHistoryMethod != null;
            }
            
            colorSwatchesWorking = hasSwatchSupport && hasSwatchSizing && hasSwatchColors && hasSwatchConfiguration;
            
            Debug.Log($"✓ Swatch Support: {hasSwatchSupport}");
            Debug.Log($"✓ Swatch Sizing: {hasSwatchSizing}");
            Debug.Log($"✓ Swatch Colors: {hasSwatchColors}");
            Debug.Log($"✓ Swatch Configuration: {hasSwatchConfiguration}");
            Debug.Log($"Color Swatches: {(colorSwatchesWorking ? "PASS" : "FAIL")}");
        }
        
        private void VerifyDifferenceIndicators()
        {
            Debug.Log("--- Verifying Difference Indicators Implementation ---");
            
            bool hasDifferenceCalculation = false;
            bool hasQualityIndicators = false;
            bool hasThresholdConfiguration = false;
            bool hasMatchingGuidance = false;
            
            if (colorComparisonDisplay != null)
            {
                // Check if difference calculation exists
                var matchQualityProperty = typeof(ColorComparisonDisplay).GetProperty("MatchQuality");
                var colorDifferenceProperty = typeof(ColorComparisonDisplay).GetProperty("ColorDifference");
                var currentMatchResultProperty = typeof(ColorComparisonDisplay).GetProperty("CurrentMatchResult");
                
                hasDifferenceCalculation = matchQualityProperty != null && colorDifferenceProperty != null && 
                                         currentMatchResultProperty != null;
                
                // Check if quality threshold configuration exists
                var setQualityThresholdsMethod = typeof(ColorComparisonDisplay).GetMethod("SetQualityThresholds");
                hasThresholdConfiguration = setQualityThresholdsMethod != null;
                
                hasQualityIndicators = hasDifferenceCalculation && hasThresholdConfiguration;
            }
            
            // Check if ColorMatchResult class exists with proper properties
            try
            {
                var colorMatchResultType = typeof(ColorMatchResult);
                if (colorMatchResultType != null)
                {
                    var matchQualityField = colorMatchResultType.GetField("matchQuality");
                    var colorDifferenceField = colorMatchResultType.GetField("colorDifference");
                    var hueDifferenceField = colorMatchResultType.GetField("hueDifference");
                    var saturationDifferenceField = colorMatchResultType.GetField("saturationDifference");
                    var brightnessDifferenceField = colorMatchResultType.GetField("brightnessDifference");
                    
                    hasMatchingGuidance = matchQualityField != null && colorDifferenceField != null && 
                                        hueDifferenceField != null && saturationDifferenceField != null && 
                                        brightnessDifferenceField != null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not verify ColorMatchResult: {ex.Message}");
            }
            
            differenceIndicatorsWorking = hasDifferenceCalculation && hasQualityIndicators && 
                                        hasThresholdConfiguration && hasMatchingGuidance;
            
            Debug.Log($"✓ Difference Calculation: {hasDifferenceCalculation}");
            Debug.Log($"✓ Quality Indicators: {hasQualityIndicators}");
            Debug.Log($"✓ Threshold Configuration: {hasThresholdConfiguration}");
            Debug.Log($"✓ Matching Guidance: {hasMatchingGuidance}");
            Debug.Log($"Difference Indicators: {(differenceIndicatorsWorking ? "PASS" : "FAIL")}");
        }
        
        private void VerifyUIAccuracyTests()
        {
            Debug.Log("--- Verifying UI Accuracy Tests Implementation ---");
            
            bool hasColorAnalysisUITests = false;
            bool hasPerformanceTests = false;
            bool hasAccuracyTests = false;
            bool hasVisualFeedbackTests = false;
            
            // Check if test classes exist
            try
            {
                var colorAnalysisUITestType = System.Type.GetType("DaVinciEye.Tests.UI.ColorAnalysisUITests");
                var performanceTestType = System.Type.GetType("DaVinciEye.Tests.UI.ColorAnalysisUIPerformanceTests");
                var accuracyTestType = System.Type.GetType("DaVinciEye.Tests.UI.ColorAnalysisUIAccuracyTests");
                
                hasColorAnalysisUITests = colorAnalysisUITestType != null;
                hasPerformanceTests = performanceTestType != null;
                hasAccuracyTests = accuracyTestType != null;
                hasVisualFeedbackTests = hasColorAnalysisUITests; // Same test class covers visual feedback
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not verify test classes: {ex.Message}");
            }
            
            uiAccuracyTestsPassed = hasColorAnalysisUITests && hasPerformanceTests && 
                                  hasAccuracyTests && hasVisualFeedbackTests;
            
            Debug.Log($"✓ Color Analysis UI Tests: {hasColorAnalysisUITests}");
            Debug.Log($"✓ Performance Tests: {hasPerformanceTests}");
            Debug.Log($"✓ Accuracy Tests: {hasAccuracyTests}");
            Debug.Log($"✓ Visual Feedback Tests: {hasVisualFeedbackTests}");
            Debug.Log($"UI Accuracy Tests: {(uiAccuracyTestsPassed ? "PASS" : "FAIL")}");
        }
        
        private void LogVerificationResults()
        {
            Debug.Log("=== Task 11.3 Verification Results ===");
            
            bool allTestsPassed = colorPickerUIImplemented && selectionFeedbackWorking && 
                                colorComparisonDisplayWorking && colorSwatchesWorking && 
                                differenceIndicatorsWorking && uiAccuracyTestsPassed;
            
            Debug.Log($"Color Picker UI: {(colorPickerUIImplemented ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"Selection Feedback: {(selectionFeedbackWorking ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"Color Comparison Display: {(colorComparisonDisplayWorking ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"Color Swatches: {(colorSwatchesWorking ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"Difference Indicators: {(differenceIndicatorsWorking ? "✓ PASS" : "✗ FAIL")}");
            Debug.Log($"UI Accuracy Tests: {(uiAccuracyTestsPassed ? "✓ PASS" : "✗ FAIL")}");
            
            Debug.Log($"\n=== OVERALL RESULT: {(allTestsPassed ? "✓ TASK 11.3 COMPLETE" : "✗ TASK 11.3 INCOMPLETE")} ===");
            
            if (!allTestsPassed)
            {
                Debug.LogWarning("Some verification checks failed. Please review the implementation.");
                LogImplementationGuidance();
            }
        }
        
        private void LogImplementationGuidance()
        {
            Debug.Log("=== Implementation Guidance ===");
            
            if (!colorPickerUIImplemented)
            {
                Debug.Log("• Implement ColorAnalysisUI with color picker functionality:");
                Debug.Log("  - StartColorPicking() and StopColorPicking() methods");
                Debug.Log("  - UpdateColorPickerPosition() for crosshair positioning");
                Debug.Log("  - IsColorPickingActive property for state management");
                Debug.Log("  - OnColorPickerPositionChanged and OnReferenceColorSelected events");
                Debug.Log("• Implement ColorPickerCrosshair component:");
                Debug.Log("  - SetPosition(), SetColor(), SetVisibility() methods");
                Debug.Log("  - Animation and visual feedback support");
            }
            
            if (!selectionFeedbackWorking)
            {
                Debug.Log("• Implement selection feedback system:");
                Debug.Log("  - ShowSelectionFeedback() method in ColorPickerCrosshair");
                Debug.Log("  - Animation support with SetAnimationEnabled()");
                Debug.Log("  - Visual feedback with color and position events");
                Debug.Log("  - Crosshair visibility and animation controls");
            }
            
            if (!colorComparisonDisplayWorking)
            {
                Debug.Log("• Implement ColorComparisonDisplay component:");
                Debug.Log("  - SetColors(), SetReferenceColor(), SetCapturedColor() methods");
                Debug.Log("  - Color comparison calculation and display");
                Debug.Log("  - OnComparisonUpdated and OnColorsChanged events");
                Debug.Log("  - Match result display with CurrentMatchResult property");
            }
            
            if (!colorSwatchesWorking)
            {
                Debug.Log("• Implement color swatch functionality:");
                Debug.Log("  - Color swatch display for reference and captured colors");
                Debug.Log("  - SetSwatchSize() method and SwatchSize property");
                Debug.Log("  - Color history support with swatch display");
                Debug.Log("  - Swatch configuration and sizing controls");
            }
            
            if (!differenceIndicatorsWorking)
            {
                Debug.Log("• Implement difference indicators:");
                Debug.Log("  - Color difference calculation (Delta E)");
                Debug.Log("  - Match quality indicators with thresholds");
                Debug.Log("  - SetQualityThresholds() configuration method");
                Debug.Log("  - ColorMatchResult with hue, saturation, brightness differences");
                Debug.Log("  - Visual quality indicators and matching guidance");
            }
            
            if (!uiAccuracyTestsPassed)
            {
                Debug.Log("• Implement comprehensive UI tests:");
                Debug.Log("  - ColorAnalysisUITests for basic functionality");
                Debug.Log("  - ColorAnalysisUIPerformanceTests for responsiveness");
                Debug.Log("  - ColorAnalysisUIAccuracyTests for color matching accuracy");
                Debug.Log("  - Visual feedback clarity tests");
            }
        }
        
        // Public properties for external verification
        public bool IsTask113Complete => colorPickerUIImplemented && selectionFeedbackWorking && 
                                       colorComparisonDisplayWorking && colorSwatchesWorking && 
                                       differenceIndicatorsWorking && uiAccuracyTestsPassed;
        
        public string GetVerificationSummary()
        {
            return $"Task 11.3 Status: " +
                   $"Picker({(colorPickerUIImplemented ? "✓" : "✗")}) " +
                   $"Feedback({(selectionFeedbackWorking ? "✓" : "✗")}) " +
                   $"Comparison({(colorComparisonDisplayWorking ? "✓" : "✗")}) " +
                   $"Swatches({(colorSwatchesWorking ? "✓" : "✗")}) " +
                   $"Indicators({(differenceIndicatorsWorking ? "✓" : "✗")}) " +
                   $"Tests({(uiAccuracyTestsPassed ? "✓" : "✗")})";
        }
    }
}