using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DaVinciEye.ImageOverlay;

namespace DaVinciEye.Tests.ImageOverlay
{
    /// <summary>
    /// Tests for HueSaturationController functionality
    /// Validates HSV color space manipulation, real-time preview, and UI integration
    /// </summary>
    public class HueSaturationControllerTests
    {
        private GameObject testObject;
        private HueSaturationController hueSaturationController;
        private ImageOverlayManager imageOverlay;
        private ImageAdjustmentProcessor adjustmentProcessor;
        private Slider hueSlider;
        private Slider saturationSlider;
        private Texture2D testTexture;
        
        [SetUp]
        public void SetUp()
        {
            // Create test object with required components
            testObject = new GameObject("TestHueSaturationController");
            hueSaturationController = testObject.AddComponent<HueSaturationController>();
            imageOverlay = testObject.AddComponent<ImageOverlayManager>();
            adjustmentProcessor = testObject.AddComponent<ImageAdjustmentProcessor>();
            
            // Create UI sliders
            CreateTestSliders();
            
            // Create test texture
            testTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[256 * 256];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red; // Pure red for hue testing
            }
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testTexture != null)
            {
                Object.DestroyImmediate(testTexture);
            }
            
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        /// <summary>
        /// Creates test UI sliders for testing
        /// </summary>
        private void CreateTestSliders()
        {
            // Create hue slider
            GameObject hueSliderObject = new GameObject("HueSlider");
            hueSliderObject.transform.SetParent(testObject.transform);
            hueSlider = hueSliderObject.AddComponent<Slider>();
            
            // Create saturation slider
            GameObject saturationSliderObject = new GameObject("SaturationSlider");
            saturationSliderObject.transform.SetParent(testObject.transform);
            saturationSlider = saturationSliderObject.AddComponent<Slider>();
            
            // Set sliders in controller using reflection
            var hueSliderField = typeof(HueSaturationController).GetField("hueSlider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hueSliderField?.SetValue(hueSaturationController, hueSlider);
            
            var saturationSliderField = typeof(HueSaturationController).GetField("saturationSlider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            saturationSliderField?.SetValue(hueSaturationController, saturationSlider);
        }
        
        [Test]
        public void HueSaturationController_Initialize_SetsDefaultValues()
        {
            // Act & Assert
            Assert.IsNotNull(hueSaturationController);
            Assert.AreEqual(0f, hueSaturationController.HueValue, 0.01f);
            Assert.AreEqual(0f, hueSaturationController.SaturationValue, 0.01f);
            Assert.IsFalse(hueSaturationController.HasAdjustments);
        }
        
        [Test]
        public void SetHue_ValidValue_UpdatesHue()
        {
            // Arrange
            float testHue = 120f; // Green
            
            bool hueChangedEventFired = false;
            float eventHueValue = 0f;
            hueSaturationController.OnHueChanged += (value) => 
            {
                hueChangedEventFired = true;
                eventHueValue = value;
            };
            
            // Act
            hueSaturationController.SetHue(testHue);
            
            // Assert
            Assert.AreEqual(testHue, hueSaturationController.HueValue, 0.01f);
            Assert.IsTrue(hueSaturationController.HasAdjustments);
            Assert.IsTrue(hueChangedEventFired);
            Assert.AreEqual(testHue, eventHueValue, 0.01f);
        }
        
        [Test]
        public void SetSaturation_ValidValue_UpdatesSaturation()
        {
            // Arrange
            float testSaturation = 0.5f;
            
            bool saturationChangedEventFired = false;
            float eventSaturationValue = 0f;
            hueSaturationController.OnSaturationChanged += (value) => 
            {
                saturationChangedEventFired = true;
                eventSaturationValue = value;
            };
            
            // Act
            hueSaturationController.SetSaturation(testSaturation);
            
            // Assert
            Assert.AreEqual(testSaturation, hueSaturationController.SaturationValue, 0.01f);
            Assert.IsTrue(hueSaturationController.HasAdjustments);
            Assert.IsTrue(saturationChangedEventFired);
            Assert.AreEqual(testSaturation, eventSaturationValue, 0.01f);
        }
        
        [Test]
        public void SetHue_ValueOutOfRange_ClampsToValidRange()
        {
            // Test cases for hue clamping
            var testCases = new[]
            {
                new { input = -200f, expected = -180f }, // Below minimum
                new { input = 200f, expected = 180f },   // Above maximum
                new { input = 0f, expected = 0f },       // Valid value
                new { input = 90f, expected = 90f },     // Valid value
                new { input = -90f, expected = -90f }    // Valid value
            };
            
            foreach (var testCase in testCases)
            {
                // Act
                hueSaturationController.SetHue(testCase.input);
                
                // Assert
                Assert.AreEqual(testCase.expected, hueSaturationController.HueValue, 0.01f,
                    $"Failed for input: {testCase.input}");
            }
        }
        
        [Test]
        public void SetSaturation_ValueOutOfRange_ClampsToValidRange()
        {
            // Test cases for saturation clamping
            var testCases = new[]
            {
                new { input = -2f, expected = -1f },   // Below minimum
                new { input = 2f, expected = 1f },     // Above maximum
                new { input = 0f, expected = 0f },     // Valid value
                new { input = 0.5f, expected = 0.5f }, // Valid value
                new { input = -0.5f, expected = -0.5f } // Valid value
            };
            
            foreach (var testCase in testCases)
            {
                // Act
                hueSaturationController.SetSaturation(testCase.input);
                
                // Assert
                Assert.AreEqual(testCase.expected, hueSaturationController.SaturationValue, 0.01f,
                    $"Failed for input: {testCase.input}");
            }
        }
        
        [Test]
        public void SetAdjustments_ValidValues_UpdatesBothValues()
        {
            // Arrange
            float testHue = 60f;
            float testSaturation = -0.3f;
            
            // Act
            hueSaturationController.SetAdjustments(testHue, testSaturation);
            
            // Assert
            Assert.AreEqual(testHue, hueSaturationController.HueValue, 0.01f);
            Assert.AreEqual(testSaturation, hueSaturationController.SaturationValue, 0.01f);
            Assert.IsTrue(hueSaturationController.HasAdjustments);
        }
        
        [Test]
        public void ResetAdjustments_ResetsToDefaultValues()
        {
            // Arrange
            hueSaturationController.SetAdjustments(120f, 0.8f);
            
            bool resetEventFired = false;
            hueSaturationController.OnAdjustmentsReset += () => resetEventFired = true;
            
            // Act
            hueSaturationController.ResetAdjustments();
            
            // Assert
            Assert.AreEqual(0f, hueSaturationController.HueValue, 0.01f);
            Assert.AreEqual(0f, hueSaturationController.SaturationValue, 0.01f);
            Assert.IsFalse(hueSaturationController.HasAdjustments);
            Assert.IsTrue(resetEventFired);
        }
        
        [Test]
        public void ApplyAdjustments_FiresAppliedEvent()
        {
            // Arrange
            hueSaturationController.SetAdjustments(90f, 0.4f);
            
            bool appliedEventFired = false;
            ImageAdjustments appliedAdjustments = null;
            hueSaturationController.OnAdjustmentsApplied += (adjustments) => 
            {
                appliedEventFired = true;
                appliedAdjustments = adjustments;
            };
            
            // Act
            hueSaturationController.ApplyAdjustments();
            
            // Assert
            Assert.IsTrue(appliedEventFired);
            Assert.IsNotNull(appliedAdjustments);
            Assert.AreEqual(90f, appliedAdjustments.hue, 0.01f);
            Assert.AreEqual(0.4f, appliedAdjustments.saturation, 0.01f);
        }
        
        [Test]
        public void ValidateAdjustments_ValidValues_ReturnsTrue()
        {
            // Arrange
            hueSaturationController.SetAdjustments(45f, 0.2f);
            
            // Act
            bool isValid = hueSaturationController.ValidateAdjustments();
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void ValidateAdjustments_InvalidValues_ReturnsFalse()
        {
            // Arrange - Set values that would be invalid if not clamped
            // Since SetAdjustments clamps values, we need to test the validation logic directly
            var adjustments = hueSaturationController.GetCurrentAdjustments();
            
            // Use reflection to set invalid values for testing
            var hueField = typeof(ImageAdjustments).GetField("hue");
            var saturationField = typeof(ImageAdjustments).GetField("saturation");
            
            if (hueField != null && saturationField != null)
            {
                hueField.SetValue(adjustments, 300f); // Invalid hue
                saturationField.SetValue(adjustments, 5f); // Invalid saturation
                
                // Set the invalid adjustments using reflection
                var currentAdjustmentsField = typeof(HueSaturationController).GetField("currentAdjustments", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                currentAdjustmentsField?.SetValue(hueSaturationController, adjustments);
                
                // Act
                bool isValid = hueSaturationController.ValidateAdjustments();
                
                // Assert
                Assert.IsFalse(isValid);
            }
            else
            {
                Assert.Inconclusive("Could not access adjustment fields for testing");
            }
        }
        
        [Test]
        public void GetCurrentAdjustments_ReturnsCorrectCopy()
        {
            // Arrange
            float testHue = 150f;
            float testSaturation = -0.7f;
            hueSaturationController.SetAdjustments(testHue, testSaturation);
            
            // Act
            ImageAdjustments copy = hueSaturationController.GetCurrentAdjustments();
            
            // Assert
            Assert.IsNotNull(copy);
            Assert.AreEqual(testHue, copy.hue, 0.01f);
            Assert.AreEqual(testSaturation, copy.saturation, 0.01f);
            Assert.IsTrue(copy.isModified);
            
            // Verify it's a copy (modifying copy shouldn't affect original)
            copy.hue = 0f;
            Assert.AreEqual(testHue, hueSaturationController.HueValue, 0.01f);
        }
        
        [Test]
        public void ApplyAdjustmentsToColor_HueShift_ChangesColorHue()
        {
            // Arrange
            Color inputColor = Color.red; // Pure red (hue = 0)
            hueSaturationController.SetHue(120f); // Shift to green
            
            // Act
            Color adjustedColor = hueSaturationController.ApplyAdjustmentsToColor(inputColor);
            
            // Assert
            // After 120-degree hue shift, red should become green-ish
            Assert.Greater(adjustedColor.g, adjustedColor.r);
            Assert.Greater(adjustedColor.g, adjustedColor.b);
        }
        
        [Test]
        public void ApplyAdjustmentsToColor_SaturationAdjustment_ChangesSaturation()
        {
            // Arrange
            Color inputColor = new Color(1f, 0.5f, 0.5f, 1f); // Light red (saturated)
            hueSaturationController.SetSaturation(-0.8f); // Reduce saturation significantly
            
            // Act
            Color adjustedColor = hueSaturationController.ApplyAdjustmentsToColor(inputColor);
            
            // Assert
            // With reduced saturation, RGB values should be closer together (more gray)
            float colorRange = Mathf.Max(adjustedColor.r, adjustedColor.g, adjustedColor.b) - 
                              Mathf.Min(adjustedColor.r, adjustedColor.g, adjustedColor.b);
            Assert.Less(colorRange, 0.3f); // Should be less saturated
        }
        
        [Test]
        public void ApplyAdjustmentsToColor_NoAdjustments_ReturnsOriginalColor()
        {
            // Arrange
            Color inputColor = new Color(0.7f, 0.3f, 0.9f, 1f);
            // No adjustments set (default values are 0)
            
            // Act
            Color adjustedColor = hueSaturationController.ApplyAdjustmentsToColor(inputColor);
            
            // Assert
            Assert.AreEqual(inputColor.r, adjustedColor.r, 0.01f);
            Assert.AreEqual(inputColor.g, adjustedColor.g, 0.01f);
            Assert.AreEqual(inputColor.b, adjustedColor.b, 0.01f);
            Assert.AreEqual(inputColor.a, adjustedColor.a, 0.01f);
        }
        
        [Test]
        public void GetNormalizedHue_ConvertsToZeroOneRange()
        {
            // Test cases for hue normalization
            var testCases = new[]
            {
                new { hue = -180f, expected = 0f },   // Minimum maps to 0
                new { hue = 0f, expected = 0.5f },    // Center maps to 0.5
                new { hue = 180f, expected = 1f },    // Maximum maps to 1
                new { hue = 90f, expected = 0.75f },  // Quarter way from center to max
                new { hue = -90f, expected = 0.25f }  // Quarter way from center to min
            };
            
            foreach (var testCase in testCases)
            {
                // Arrange
                hueSaturationController.SetHue(testCase.hue);
                
                // Act
                float normalizedHue = hueSaturationController.GetNormalizedHue();
                
                // Assert
                Assert.AreEqual(testCase.expected, normalizedHue, 0.01f,
                    $"Failed for hue: {testCase.hue}");
            }
        }
        
        [Test]
        public void GetNormalizedSaturation_ConvertsToZeroOneRange()
        {
            // Test cases for saturation normalization
            var testCases = new[]
            {
                new { saturation = -1f, expected = 0f },   // Minimum maps to 0
                new { saturation = 0f, expected = 0.5f },  // Center maps to 0.5
                new { saturation = 1f, expected = 1f },    // Maximum maps to 1
                new { saturation = 0.5f, expected = 0.75f }, // Quarter way from center to max
                new { saturation = -0.5f, expected = 0.25f } // Quarter way from center to min
            };
            
            foreach (var testCase in testCases)
            {
                // Arrange
                hueSaturationController.SetSaturation(testCase.saturation);
                
                // Act
                float normalizedSaturation = hueSaturationController.GetNormalizedSaturation();
                
                // Assert
                Assert.AreEqual(testCase.expected, normalizedSaturation, 0.01f,
                    $"Failed for saturation: {testCase.saturation}");
            }
        }
        
        [Test]
        public void CalculateColorDifference_SameColors_ReturnsZero()
        {
            // Arrange
            Color color1 = Color.blue;
            Color color2 = Color.blue;
            
            // Act
            float difference = hueSaturationController.CalculateColorDifference(color1, color2);
            
            // Assert
            Assert.AreEqual(0f, difference, 0.01f);
        }
        
        [Test]
        public void CalculateColorDifference_DifferentColors_ReturnsPositiveValue()
        {
            // Arrange
            Color color1 = Color.red;
            Color color2 = Color.blue;
            
            // Act
            float difference = hueSaturationController.CalculateColorDifference(color1, color2);
            
            // Assert
            Assert.Greater(difference, 0f);
        }
        
        [Test]
        public void CalculateColorDifference_WithAdjustments_ReflectsAdjustedColor()
        {
            // Arrange
            Color originalColor = Color.red;
            Color targetColor = Color.green;
            
            // Calculate difference without adjustments
            float differenceWithoutAdjustments = hueSaturationController.CalculateColorDifference(originalColor, targetColor);
            
            // Apply hue shift towards green
            hueSaturationController.SetHue(120f); // Red to green shift
            
            // Act
            float differenceWithAdjustments = hueSaturationController.CalculateColorDifference(originalColor, targetColor);
            
            // Assert
            Assert.Less(differenceWithAdjustments, differenceWithoutAdjustments);
        }
        
        [Test]
        public void SetRealTimePreview_EnabledDisabled_UpdatesBehavior()
        {
            // Arrange
            hueSaturationController.SetRealTimePreview(false);
            
            // Act - Change values with real-time preview disabled
            hueSaturationController.SetHue(90f);
            
            // Assert - Should still update the value
            Assert.AreEqual(90f, hueSaturationController.HueValue, 0.01f);
            
            // Act - Enable real-time preview
            hueSaturationController.SetRealTimePreview(true);
            
            // Assert - Should apply adjustments immediately
            Assert.AreEqual(90f, hueSaturationController.HueValue, 0.01f);
        }
        
        [UnityTest]
        public IEnumerator SliderIntegration_HueSlider_UpdatesHueValue()
        {
            // Arrange
            yield return null; // Wait for initialization
            
            float testValue = 45f;
            
            // Act
            hueSlider.value = testValue;
            
            // Wait for slider event to process
            yield return null;
            
            // Assert
            Assert.AreEqual(testValue, hueSaturationController.HueValue, 5f); // Allow some tolerance for slider precision
        }
        
        [UnityTest]
        public IEnumerator SliderIntegration_SaturationSlider_UpdatesSaturationValue()
        {
            // Arrange
            yield return null; // Wait for initialization
            
            float testValue = 0.6f;
            
            // Act
            saturationSlider.value = testValue;
            
            // Wait for slider event to process
            yield return null;
            
            // Assert
            Assert.AreEqual(testValue, hueSaturationController.SaturationValue, 0.1f);
        }
        
        [Test]
        public void HueWrapAround_ExtremeValues_HandledCorrectly()
        {
            // Test hue wrap-around behavior
            Color testColor = Color.red;
            
            // Test 360-degree rotation (should return to original)
            hueSaturationController.SetHue(360f); // This should be clamped to 180f
            Color adjusted360 = hueSaturationController.ApplyAdjustmentsToColor(testColor);
            
            // Test negative wrap-around
            hueSaturationController.SetHue(-180f);
            Color adjustedNeg180 = hueSaturationController.ApplyAdjustmentsToColor(testColor);
            
            // Both should be valid colors (no NaN or invalid values)
            Assert.IsFalse(float.IsNaN(adjusted360.r));
            Assert.IsFalse(float.IsNaN(adjusted360.g));
            Assert.IsFalse(float.IsNaN(adjusted360.b));
            
            Assert.IsFalse(float.IsNaN(adjustedNeg180.r));
            Assert.IsFalse(float.IsNaN(adjustedNeg180.g));
            Assert.IsFalse(float.IsNaN(adjustedNeg180.b));
        }
        
        [Test]
        public void EventSequence_AdjustmentWorkflow_FiresEventsInCorrectOrder()
        {
            // Arrange
            var eventSequence = new System.Collections.Generic.List<string>();
            
            hueSaturationController.OnHueChanged += (value) => eventSequence.Add("HueChanged");
            hueSaturationController.OnSaturationChanged += (value) => eventSequence.Add("SaturationChanged");
            hueSaturationController.OnAdjustmentsApplied += (adj) => eventSequence.Add("AdjustmentsApplied");
            hueSaturationController.OnAdjustmentsReset += () => eventSequence.Add("AdjustmentsReset");
            
            // Act
            hueSaturationController.SetHue(60f);
            hueSaturationController.SetSaturation(0.5f);
            hueSaturationController.ApplyAdjustments();
            hueSaturationController.ResetAdjustments();
            
            // Assert
            Assert.Contains("HueChanged", eventSequence);
            Assert.Contains("SaturationChanged", eventSequence);
            Assert.Contains("AdjustmentsApplied", eventSequence);
            Assert.Contains("AdjustmentsReset", eventSequence);
            
            // Verify order
            int hueIndex = eventSequence.IndexOf("HueChanged");
            int saturationIndex = eventSequence.IndexOf("SaturationChanged");
            int appliedIndex = eventSequence.IndexOf("AdjustmentsApplied");
            int resetIndex = eventSequence.IndexOf("AdjustmentsReset");
            
            Assert.Less(hueIndex, appliedIndex);
            Assert.Less(saturationIndex, appliedIndex);
            Assert.Less(appliedIndex, resetIndex);
        }
    }
}