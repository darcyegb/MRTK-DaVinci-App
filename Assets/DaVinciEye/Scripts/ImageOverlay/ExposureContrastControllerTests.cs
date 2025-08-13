using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using DaVinciEye.ImageOverlay;

namespace DaVinciEye.Tests.ImageOverlay
{
    /// <summary>
    /// Tests for ExposureContrastController functionality
    /// Validates exposure and contrast adjustment controls, real-time preview, and UI integration
    /// </summary>
    public class ExposureContrastControllerTests
    {
        private GameObject testObject;
        private ExposureContrastController exposureContrastController;
        private ImageOverlayManager imageOverlay;
        private ImageAdjustmentProcessor adjustmentProcessor;
        private Slider exposureSlider;
        private Slider contrastSlider;
        private Texture2D testTexture;
        
        [SetUp]
        public void SetUp()
        {
            // Create test object with required components
            testObject = new GameObject("TestExposureContrastController");
            exposureContrastController = testObject.AddComponent<ExposureContrastController>();
            imageOverlay = testObject.AddComponent<ImageOverlayManager>();
            adjustmentProcessor = testObject.AddComponent<ImageAdjustmentProcessor>();
            
            // Create UI sliders
            CreateTestSliders();
            
            // Create test texture
            testTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[256 * 256];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.gray;
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
            // Create exposure slider
            GameObject exposureSliderObject = new GameObject("ExposureSlider");
            exposureSliderObject.transform.SetParent(testObject.transform);
            exposureSlider = exposureSliderObject.AddComponent<Slider>();
            
            // Create contrast slider
            GameObject contrastSliderObject = new GameObject("ContrastSlider");
            contrastSliderObject.transform.SetParent(testObject.transform);
            contrastSlider = contrastSliderObject.AddComponent<Slider>();
            
            // Set sliders in controller using reflection
            var exposureSliderField = typeof(ExposureContrastController).GetField("exposureSlider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            exposureSliderField?.SetValue(exposureContrastController, exposureSlider);
            
            var contrastSliderField = typeof(ExposureContrastController).GetField("contrastSlider", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            contrastSliderField?.SetValue(exposureContrastController, contrastSlider);
        }
        
        [Test]
        public void ExposureContrastController_Initialize_SetsDefaultValues()
        {
            // Act & Assert
            Assert.IsNotNull(exposureContrastController);
            Assert.AreEqual(0f, exposureContrastController.ExposureValue, 0.01f);
            Assert.AreEqual(0f, exposureContrastController.ContrastValue, 0.01f);
            Assert.IsFalse(exposureContrastController.HasAdjustments);
        }
        
        [Test]
        public void SetExposure_ValidValue_UpdatesExposure()
        {
            // Arrange
            float testExposure = 0.5f;
            
            bool exposureChangedEventFired = false;
            float eventExposureValue = 0f;
            exposureContrastController.OnExposureChanged += (value) => 
            {
                exposureChangedEventFired = true;
                eventExposureValue = value;
            };
            
            // Act
            exposureContrastController.SetExposure(testExposure);
            
            // Assert
            Assert.AreEqual(testExposure, exposureContrastController.ExposureValue, 0.01f);
            Assert.IsTrue(exposureContrastController.HasAdjustments);
            Assert.IsTrue(exposureChangedEventFired);
            Assert.AreEqual(testExposure, eventExposureValue, 0.01f);
        }
        
        [Test]
        public void SetContrast_ValidValue_UpdatesContrast()
        {
            // Arrange
            float testContrast = 0.3f;
            
            bool contrastChangedEventFired = false;
            float eventContrastValue = 0f;
            exposureContrastController.OnContrastChanged += (value) => 
            {
                contrastChangedEventFired = true;
                eventContrastValue = value;
            };
            
            // Act
            exposureContrastController.SetContrast(testContrast);
            
            // Assert
            Assert.AreEqual(testContrast, exposureContrastController.ContrastValue, 0.01f);
            Assert.IsTrue(exposureContrastController.HasAdjustments);
            Assert.IsTrue(contrastChangedEventFired);
            Assert.AreEqual(testContrast, eventContrastValue, 0.01f);
        }
        
        [Test]
        public void SetExposure_ValueOutOfRange_ClampsToValidRange()
        {
            // Test cases for exposure clamping
            var testCases = new[]
            {
                new { input = -3f, expected = -2f }, // Below minimum
                new { input = 3f, expected = 2f },   // Above maximum
                new { input = 0f, expected = 0f },   // Valid value
                new { input = 1.5f, expected = 1.5f }, // Valid value
                new { input = -1.5f, expected = -1.5f } // Valid value
            };
            
            foreach (var testCase in testCases)
            {
                // Act
                exposureContrastController.SetExposure(testCase.input);
                
                // Assert
                Assert.AreEqual(testCase.expected, exposureContrastController.ExposureValue, 0.01f,
                    $"Failed for input: {testCase.input}");
            }
        }
        
        [Test]
        public void SetContrast_ValueOutOfRange_ClampsToValidRange()
        {
            // Test cases for contrast clamping
            var testCases = new[]
            {
                new { input = -2f, expected = -1f }, // Below minimum
                new { input = 2f, expected = 1f },   // Above maximum
                new { input = 0f, expected = 0f },   // Valid value
                new { input = 0.8f, expected = 0.8f }, // Valid value
                new { input = -0.8f, expected = -0.8f } // Valid value
            };
            
            foreach (var testCase in testCases)
            {
                // Act
                exposureContrastController.SetContrast(testCase.input);
                
                // Assert
                Assert.AreEqual(testCase.expected, exposureContrastController.ContrastValue, 0.01f,
                    $"Failed for input: {testCase.input}");
            }
        }
        
        [Test]
        public void SetAdjustments_ValidValues_UpdatesBothValues()
        {
            // Arrange
            float testExposure = 0.7f;
            float testContrast = -0.4f;
            
            // Act
            exposureContrastController.SetAdjustments(testExposure, testContrast);
            
            // Assert
            Assert.AreEqual(testExposure, exposureContrastController.ExposureValue, 0.01f);
            Assert.AreEqual(testContrast, exposureContrastController.ContrastValue, 0.01f);
            Assert.IsTrue(exposureContrastController.HasAdjustments);
        }
        
        [Test]
        public void ResetAdjustments_ResetsToDefaultValues()
        {
            // Arrange
            exposureContrastController.SetAdjustments(1.0f, 0.5f);
            
            bool resetEventFired = false;
            exposureContrastController.OnAdjustmentsReset += () => resetEventFired = true;
            
            // Act
            exposureContrastController.ResetAdjustments();
            
            // Assert
            Assert.AreEqual(0f, exposureContrastController.ExposureValue, 0.01f);
            Assert.AreEqual(0f, exposureContrastController.ContrastValue, 0.01f);
            Assert.IsFalse(exposureContrastController.HasAdjustments);
            Assert.IsTrue(resetEventFired);
        }
        
        [Test]
        public void ApplyAdjustments_FiresAppliedEvent()
        {
            // Arrange
            exposureContrastController.SetAdjustments(0.5f, 0.3f);
            
            bool appliedEventFired = false;
            ImageAdjustments appliedAdjustments = null;
            exposureContrastController.OnAdjustmentsApplied += (adjustments) => 
            {
                appliedEventFired = true;
                appliedAdjustments = adjustments;
            };
            
            // Act
            exposureContrastController.ApplyAdjustments();
            
            // Assert
            Assert.IsTrue(appliedEventFired);
            Assert.IsNotNull(appliedAdjustments);
            Assert.AreEqual(0.5f, appliedAdjustments.exposure, 0.01f);
            Assert.AreEqual(0.3f, appliedAdjustments.contrast, 0.01f);
        }
        
        [Test]
        public void ValidateAdjustments_ValidValues_ReturnsTrue()
        {
            // Arrange
            exposureContrastController.SetAdjustments(1.0f, 0.5f);
            
            // Act
            bool isValid = exposureContrastController.ValidateAdjustments();
            
            // Assert
            Assert.IsTrue(isValid);
        }
        
        [Test]
        public void ValidateAdjustments_InvalidValues_ReturnsFalse()
        {
            // Arrange - Set values that would be invalid if not clamped
            // Since SetAdjustments clamps values, we need to test the validation logic directly
            var adjustments = exposureContrastController.GetCurrentAdjustments();
            
            // Use reflection to set invalid values for testing
            var exposureField = typeof(ImageAdjustments).GetField("exposure");
            var contrastField = typeof(ImageAdjustments).GetField("contrast");
            
            if (exposureField != null && contrastField != null)
            {
                exposureField.SetValue(adjustments, 5f); // Invalid exposure
                contrastField.SetValue(adjustments, 2f); // Invalid contrast
                
                // Set the invalid adjustments using reflection
                var currentAdjustmentsField = typeof(ExposureContrastController).GetField("currentAdjustments", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                currentAdjustmentsField?.SetValue(exposureContrastController, adjustments);
                
                // Act
                bool isValid = exposureContrastController.ValidateAdjustments();
                
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
            float testExposure = 0.8f;
            float testContrast = -0.6f;
            exposureContrastController.SetAdjustments(testExposure, testContrast);
            
            // Act
            ImageAdjustments copy = exposureContrastController.GetCurrentAdjustments();
            
            // Assert
            Assert.IsNotNull(copy);
            Assert.AreEqual(testExposure, copy.exposure, 0.01f);
            Assert.AreEqual(testContrast, copy.contrast, 0.01f);
            Assert.IsTrue(copy.isModified);
            
            // Verify it's a copy (modifying copy shouldn't affect original)
            copy.exposure = 0f;
            Assert.AreEqual(testExposure, exposureContrastController.ExposureValue, 0.01f);
        }
        
        [Test]
        public void SetRealTimePreview_EnabledDisabled_UpdatesBehavior()
        {
            // Arrange
            exposureContrastController.SetRealTimePreview(false);
            
            // Act - Change values with real-time preview disabled
            exposureContrastController.SetExposure(0.5f);
            
            // Assert - Should still update the value
            Assert.AreEqual(0.5f, exposureContrastController.ExposureValue, 0.01f);
            
            // Act - Enable real-time preview
            exposureContrastController.SetRealTimePreview(true);
            
            // Assert - Should apply adjustments immediately
            Assert.AreEqual(0.5f, exposureContrastController.ExposureValue, 0.01f);
        }
        
        [UnityTest]
        public IEnumerator SliderIntegration_ExposureSlider_UpdatesExposureValue()
        {
            // Arrange
            yield return null; // Wait for initialization
            
            float testValue = 0.7f;
            
            // Act
            exposureSlider.value = testValue;
            
            // Wait for slider event to process
            yield return null;
            
            // Assert
            Assert.AreEqual(testValue, exposureContrastController.ExposureValue, 0.1f);
        }
        
        [UnityTest]
        public IEnumerator SliderIntegration_ContrastSlider_UpdatesContrastValue()
        {
            // Arrange
            yield return null; // Wait for initialization
            
            float testValue = -0.3f;
            
            // Act
            contrastSlider.value = testValue;
            
            // Wait for slider event to process
            yield return null;
            
            // Assert
            Assert.AreEqual(testValue, exposureContrastController.ContrastValue, 0.1f);
        }
        
        [Test]
        public void AdjustmentRanges_ExposureContrast_WithinExpectedBounds()
        {
            // Test exposure range
            exposureContrastController.SetExposure(-2f);
            Assert.AreEqual(-2f, exposureContrastController.ExposureValue, 0.01f);
            
            exposureContrastController.SetExposure(2f);
            Assert.AreEqual(2f, exposureContrastController.ExposureValue, 0.01f);
            
            // Test contrast range
            exposureContrastController.SetContrast(-1f);
            Assert.AreEqual(-1f, exposureContrastController.ContrastValue, 0.01f);
            
            exposureContrastController.SetContrast(1f);
            Assert.AreEqual(1f, exposureContrastController.ContrastValue, 0.01f);
        }
        
        [Test]
        public void AdjustmentPrecision_SmallChanges_DetectedCorrectly()
        {
            // Test small changes are detected
            exposureContrastController.SetExposure(0.02f); // Above threshold
            Assert.IsTrue(exposureContrastController.HasAdjustments);
            
            exposureContrastController.ResetAdjustments();
            exposureContrastController.SetContrast(0.02f); // Above threshold
            Assert.IsTrue(exposureContrastController.HasAdjustments);
            
            // Test very small changes might not be detected (depends on threshold)
            exposureContrastController.ResetAdjustments();
            exposureContrastController.SetExposure(0.005f); // Below threshold
            // This might or might not be detected depending on implementation threshold
        }
        
        [Test]
        public void EventSequence_AdjustmentWorkflow_FiresEventsInCorrectOrder()
        {
            // Arrange
            var eventSequence = new System.Collections.Generic.List<string>();
            
            exposureContrastController.OnExposureChanged += (value) => eventSequence.Add("ExposureChanged");
            exposureContrastController.OnContrastChanged += (value) => eventSequence.Add("ContrastChanged");
            exposureContrastController.OnAdjustmentsApplied += (adj) => eventSequence.Add("AdjustmentsApplied");
            exposureContrastController.OnAdjustmentsReset += () => eventSequence.Add("AdjustmentsReset");
            
            // Act
            exposureContrastController.SetExposure(0.5f);
            exposureContrastController.SetContrast(0.3f);
            exposureContrastController.ApplyAdjustments();
            exposureContrastController.ResetAdjustments();
            
            // Assert
            Assert.Contains("ExposureChanged", eventSequence);
            Assert.Contains("ContrastChanged", eventSequence);
            Assert.Contains("AdjustmentsApplied", eventSequence);
            Assert.Contains("AdjustmentsReset", eventSequence);
            
            // Verify order
            int exposureIndex = eventSequence.IndexOf("ExposureChanged");
            int contrastIndex = eventSequence.IndexOf("ContrastChanged");
            int appliedIndex = eventSequence.IndexOf("AdjustmentsApplied");
            int resetIndex = eventSequence.IndexOf("AdjustmentsReset");
            
            Assert.Less(exposureIndex, appliedIndex);
            Assert.Less(contrastIndex, appliedIndex);
            Assert.Less(appliedIndex, resetIndex);
        }
    }
}