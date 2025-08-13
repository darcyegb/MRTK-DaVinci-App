using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DaVinciEye.ImageOverlay;

namespace DaVinciEye.Tests.ImageOverlay
{
    /// <summary>
    /// Tests for CropController functionality
    /// Validates crop area definition, hand gesture integration, and real-time preview
    /// </summary>
    public class CropControllerTests
    {
        private GameObject testObject;
        private CropController cropController;
        private ImageOverlayManager imageOverlay;
        private Texture2D testTexture;
        
        [SetUp]
        public void SetUp()
        {
            // Create test object with required components
            testObject = new GameObject("TestCropController");
            cropController = testObject.AddComponent<CropController>();
            imageOverlay = testObject.AddComponent<ImageOverlayManager>();
            
            // Create test texture
            testTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[512 * 512];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
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
        
        [Test]
        public void CropController_Initialize_SetsDefaultValues()
        {
            // Act & Assert
            Assert.IsNotNull(cropController);
            Assert.IsFalse(cropController.IsCropping);
            Assert.AreEqual(new Rect(0, 0, 1, 1), cropController.CurrentCropArea);
            Assert.IsFalse(cropController.HasCropArea);
        }
        
        [Test]
        public void StartCropping_WithoutImage_LogsWarning()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "CropController: Cannot start cropping - no image loaded");
            
            // Act
            cropController.StartCropping();
            
            // Assert
            Assert.IsFalse(cropController.IsCropping);
        }
        
        [UnityTest]
        public IEnumerator StartCropping_WithImage_EnablesCroppingMode()
        {
            // Arrange
            yield return LoadTestImage();
            
            bool cropStartedEventFired = false;
            cropController.OnCropStarted += () => cropStartedEventFired = true;
            
            // Act
            cropController.StartCropping();
            
            // Assert
            Assert.IsTrue(cropController.IsCropping);
            Assert.IsTrue(cropStartedEventFired);
        }
        
        [Test]
        public void CancelCropping_WhenCropping_DisablesCroppingMode()
        {
            // Arrange
            SetupCroppingMode();
            
            bool cropCancelledEventFired = false;
            cropController.OnCropCancelled += () => cropCancelledEventFired = true;
            
            // Act
            cropController.CancelCropping();
            
            // Assert
            Assert.IsFalse(cropController.IsCropping);
            Assert.IsTrue(cropCancelledEventFired);
        }
        
        [Test]
        public void SetCropArea_ValidArea_UpdatesCropArea()
        {
            // Arrange
            Rect testCropArea = new Rect(0.2f, 0.3f, 0.4f, 0.5f);
            
            bool cropAreaChangedEventFired = false;
            Rect eventCropArea = new Rect();
            cropController.OnCropAreaChanged += (area) => 
            {
                cropAreaChangedEventFired = true;
                eventCropArea = area;
            };
            
            // Act
            cropController.SetCropArea(testCropArea);
            
            // Assert
            Assert.AreEqual(testCropArea, cropController.CurrentCropArea);
            Assert.IsTrue(cropController.HasCropArea);
            Assert.IsTrue(cropAreaChangedEventFired);
            Assert.AreEqual(testCropArea, eventCropArea);
        }
        
        [Test]
        public void SetCropArea_InvalidArea_LogsWarning()
        {
            // Arrange
            Rect invalidCropArea = new Rect(-0.1f, 0.2f, 0.5f, 0.6f); // Negative x
            LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {invalidCropArea}"));
            
            // Act
            cropController.SetCropArea(invalidCropArea);
            
            // Assert
            Assert.AreNotEqual(invalidCropArea, cropController.CurrentCropArea);
        }
        
        [Test]
        public void SetCropArea_TooSmallArea_LogsWarning()
        {
            // Arrange
            Rect tooSmallArea = new Rect(0.2f, 0.3f, 0.05f, 0.05f); // Smaller than minimum
            LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {tooSmallArea}"));
            
            // Act
            cropController.SetCropArea(tooSmallArea);
            
            // Assert
            Assert.AreNotEqual(tooSmallArea, cropController.CurrentCropArea);
        }
        
        [Test]
        public void SetCropArea_AreaOutOfBounds_LogsWarning()
        {
            // Arrange
            Rect outOfBoundsArea = new Rect(0.8f, 0.8f, 0.5f, 0.5f); // Extends beyond 1.0
            LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {outOfBoundsArea}"));
            
            // Act
            cropController.SetCropArea(outOfBoundsArea);
            
            // Assert
            Assert.AreNotEqual(outOfBoundsArea, cropController.CurrentCropArea);
        }
        
        [UnityTest]
        public IEnumerator ApplyCrop_ValidCropArea_AppliesCropToImageOverlay()
        {
            // Arrange
            yield return LoadTestImage();
            SetupCroppingMode();
            
            Rect testCropArea = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
            cropController.SetCropArea(testCropArea);
            
            bool cropAppliedEventFired = false;
            Rect appliedCropArea = new Rect();
            cropController.OnCropAreaApplied += (area) => 
            {
                cropAppliedEventFired = true;
                appliedCropArea = area;
            };
            
            // Act
            cropController.ApplyCrop();
            
            // Assert
            Assert.IsFalse(cropController.IsCropping); // Should exit cropping mode
            Assert.IsTrue(cropAppliedEventFired);
            Assert.AreEqual(testCropArea, appliedCropArea);
            Assert.AreEqual(testCropArea, imageOverlay.CurrentAdjustments.cropArea);
        }
        
        [Test]
        public void ApplyCrop_NotInCroppingMode_LogsWarning()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "CropController: Cannot apply crop - not in cropping mode");
            
            // Act
            cropController.ApplyCrop();
            
            // Assert - Warning should be logged
        }
        
        [Test]
        public void ClearCrop_ResetsToFullImage()
        {
            // Arrange
            Rect testCropArea = new Rect(0.2f, 0.3f, 0.4f, 0.5f);
            cropController.SetCropArea(testCropArea);
            
            bool cropAreaChangedEventFired = false;
            Rect eventCropArea = new Rect();
            cropController.OnCropAreaChanged += (area) => 
            {
                cropAreaChangedEventFired = true;
                eventCropArea = area;
            };
            
            // Act
            cropController.ClearCrop();
            
            // Assert
            Assert.AreEqual(new Rect(0, 0, 1, 1), cropController.CurrentCropArea);
            Assert.IsFalse(cropController.HasCropArea);
            Assert.IsTrue(cropAreaChangedEventFired);
            Assert.AreEqual(new Rect(0, 0, 1, 1), eventCropArea);
        }
        
        [Test]
        public void CropArea_BoundaryValidation_ClampsToValidRange()
        {
            // Test various boundary conditions
            var testCases = new[]
            {
                new { input = new Rect(0.1f, 0.1f, 0.8f, 0.8f), expected = new Rect(0.1f, 0.1f, 0.8f, 0.8f), valid = true },
                new { input = new Rect(0f, 0f, 1f, 1f), expected = new Rect(0f, 0f, 1f, 1f), valid = true },
                new { input = new Rect(0.5f, 0.5f, 0.5f, 0.5f), expected = new Rect(0.5f, 0.5f, 0.5f, 0.5f), valid = true },
                new { input = new Rect(-0.1f, 0.2f, 0.5f, 0.6f), expected = new Rect(0, 0, 1, 1), valid = false }, // Negative x
                new { input = new Rect(0.2f, -0.1f, 0.5f, 0.6f), expected = new Rect(0, 0, 1, 1), valid = false }, // Negative y
                new { input = new Rect(0.8f, 0.8f, 0.5f, 0.5f), expected = new Rect(0, 0, 1, 1), valid = false }, // Extends beyond bounds
            };
            
            foreach (var testCase in testCases)
            {
                // Arrange
                cropController.ClearCrop(); // Reset to default
                
                if (!testCase.valid)
                {
                    LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {testCase.input}"));
                }
                
                // Act
                cropController.SetCropArea(testCase.input);
                
                // Assert
                Assert.AreEqual(testCase.expected, cropController.CurrentCropArea, 
                    $"Failed for input: {testCase.input}");
            }
        }
        
        [Test]
        public void CropArea_MinimumSizeValidation_EnforcesMinimumSize()
        {
            // Arrange
            float minSize = 0.1f; // Assuming default minimum size
            Rect tooSmallWidth = new Rect(0.2f, 0.3f, 0.05f, 0.5f);
            Rect tooSmallHeight = new Rect(0.2f, 0.3f, 0.5f, 0.05f);
            Rect tooSmallBoth = new Rect(0.2f, 0.3f, 0.05f, 0.05f);
            
            // Act & Assert
            LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {tooSmallWidth}"));
            cropController.SetCropArea(tooSmallWidth);
            Assert.AreNotEqual(tooSmallWidth, cropController.CurrentCropArea);
            
            LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {tooSmallHeight}"));
            cropController.SetCropArea(tooSmallHeight);
            Assert.AreNotEqual(tooSmallHeight, cropController.CurrentCropArea);
            
            LogAssert.Expect(LogType.Warning, System.Text.RegularExpressions.Regex.Escape($"CropController: Invalid crop area: {tooSmallBoth}"));
            cropController.SetCropArea(tooSmallBoth);
            Assert.AreNotEqual(tooSmallBoth, cropController.CurrentCropArea);
        }
        
        [Test]
        public void CropController_EventSequence_FiresEventsInCorrectOrder()
        {
            // Arrange
            var eventSequence = new System.Collections.Generic.List<string>();
            
            cropController.OnCropStarted += () => eventSequence.Add("CropStarted");
            cropController.OnCropAreaChanged += (area) => eventSequence.Add("CropAreaChanged");
            cropController.OnCropAreaApplied += (area) => eventSequence.Add("CropAreaApplied");
            cropController.OnCropCancelled += () => eventSequence.Add("CropCancelled");
            
            // Act - Test cancel sequence
            SetupCroppingMode();
            cropController.StartCropping();
            cropController.SetCropArea(new Rect(0.2f, 0.3f, 0.4f, 0.5f));
            cropController.CancelCropping();
            
            // Assert
            Assert.Contains("CropStarted", eventSequence);
            Assert.Contains("CropAreaChanged", eventSequence);
            Assert.Contains("CropCancelled", eventSequence);
            
            // Verify order
            int startIndex = eventSequence.IndexOf("CropStarted");
            int changeIndex = eventSequence.IndexOf("CropAreaChanged");
            int cancelIndex = eventSequence.IndexOf("CropCancelled");
            
            Assert.Less(startIndex, changeIndex);
            Assert.Less(changeIndex, cancelIndex);
        }
        
        /// <summary>
        /// Helper method to load a test image into the image overlay
        /// </summary>
        private IEnumerator LoadTestImage()
        {
            // Wait for components to initialize
            yield return null;
            
            // Simulate loading an image
            var loadTask = imageOverlay.LoadImageAsync("test_image.png");
            
            // Wait for the task to complete or timeout
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!loadTask.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Manually set the texture for testing
            if (imageOverlay.CurrentImage == null)
            {
                // Use reflection to set the current image for testing
                var field = typeof(ImageOverlayManager).GetField("currentImage", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(imageOverlay, testTexture);
            }
        }
        
        /// <summary>
        /// Helper method to setup cropping mode for tests
        /// </summary>
        private void SetupCroppingMode()
        {
            // Simulate having an image loaded
            var field = typeof(ImageOverlayManager).GetField("currentImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(imageOverlay, testTexture);
        }
    }
}