using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DaVinciEye.ImageOverlay;

namespace DaVinciEye.Tests.ImageOverlay
{
    /// <summary>
    /// Tests for ImageAdjustmentProcessor functionality
    /// Validates image processing algorithms, cropping, exposure, contrast, and performance
    /// </summary>
    public class ImageAdjustmentProcessorTests
    {
        private GameObject testObject;
        private ImageAdjustmentProcessor processor;
        private Texture2D testTexture;
        private Texture2D gradientTexture;
        private ImageAdjustments testAdjustments;
        
        [SetUp]
        public void SetUp()
        {
            // Create test object with processor
            testObject = new GameObject("TestImageAdjustmentProcessor");
            processor = testObject.AddComponent<ImageAdjustmentProcessor>();
            
            // Create test textures
            CreateTestTextures();
            
            // Create test adjustments
            testAdjustments = new ImageAdjustments();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testTexture != null)
            {
                Object.DestroyImmediate(testTexture);
            }
            
            if (gradientTexture != null)
            {
                Object.DestroyImmediate(gradientTexture);
            }
            
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        /// <summary>
        /// Creates test textures for processing tests
        /// </summary>
        private void CreateTestTextures()
        {
            // Create solid color test texture
            testTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.gray; // 50% gray
            }
            testTexture.SetPixels(pixels);
            testTexture.Apply();
            
            // Create gradient test texture
            gradientTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] gradientPixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float intensity = (float)x / 63f; // Horizontal gradient
                    gradientPixels[y * 64 + x] = new Color(intensity, intensity, intensity, 1f);
                }
            }
            gradientTexture.SetPixels(gradientPixels);
            gradientTexture.Apply();
        }
        
        [Test]
        public void ImageAdjustmentProcessor_Initialize_SetsDefaultState()
        {
            // Act & Assert
            Assert.IsNotNull(processor);
            Assert.IsNull(processor.ProcessedTexture);
            Assert.IsFalse(processor.IsProcessing);
            Assert.IsNotNull(processor.CurrentAdjustments);
        }
        
        [Test]
        public void SetOriginalTexture_ValidTexture_SetsTexture()
        {
            // Act
            processor.SetOriginalTexture(testTexture);
            
            // Assert
            Assert.IsNotNull(processor.ProcessedTexture);
            Assert.AreEqual(testTexture.width, processor.ProcessedTexture.width);
            Assert.AreEqual(testTexture.height, processor.ProcessedTexture.height);
        }
        
        [Test]
        public void SetOriginalTexture_NullTexture_LogsWarning()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "ImageAdjustmentProcessor: Cannot set null texture");
            
            // Act
            processor.SetOriginalTexture(null);
            
            // Assert - Warning should be logged
        }
        
        [UnityTest]
        public IEnumerator ApplyAdjustments_NoTexture_LogsWarning()
        {
            // Arrange
            LogAssert.Expect(LogType.Warning, "ImageAdjustmentProcessor: No original texture set");
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for async processing
            yield return new WaitForSeconds(0.1f);
            
            // Assert - Warning should be logged
        }
        
        [UnityTest]
        public IEnumerator ApplyAdjustments_NullAdjustments_LogsWarning()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            LogAssert.Expect(LogType.Warning, "ImageAdjustmentProcessor: Adjustments cannot be null");
            
            // Act
            processor.ApplyAdjustments(null);
            
            // Wait for async processing
            yield return new WaitForSeconds(0.1f);
            
            // Assert - Warning should be logged
        }
        
        [UnityTest]
        public IEnumerator ProcessImage_WithCropping_ProducesCroppedTexture()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            
            testAdjustments.cropArea = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
            testAdjustments.isCropped = true;
            
            bool imageProcessedEventFired = false;
            Texture2D processedTexture = null;
            processor.OnImageProcessed += (texture) => 
            {
                imageProcessedEventFired = true;
                processedTexture = texture;
            };
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(imageProcessedEventFired);
            Assert.IsNotNull(processedTexture);
            
            // Check cropped dimensions
            int expectedWidth = Mathf.RoundToInt(testTexture.width * 0.5f);
            int expectedHeight = Mathf.RoundToInt(testTexture.height * 0.5f);
            
            Assert.AreEqual(expectedWidth, processedTexture.width);
            Assert.AreEqual(expectedHeight, processedTexture.height);
        }
        
        [UnityTest]
        public IEnumerator ProcessImage_WithExposureAdjustment_ChangesPixelValues()
        {
            // Arrange
            processor.SetOriginalTexture(gradientTexture);
            
            testAdjustments.exposure = 1f; // Double brightness
            
            bool imageProcessedEventFired = false;
            Texture2D processedTexture = null;
            processor.OnImageProcessed += (texture) => 
            {
                imageProcessedEventFired = true;
                processedTexture = texture;
            };
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(imageProcessedEventFired);
            Assert.IsNotNull(processedTexture);
            
            // Check that pixels are brighter
            Color[] originalPixels = gradientTexture.GetPixels();
            Color[] processedPixels = processedTexture.GetPixels();
            
            // Sample a few pixels to verify exposure effect
            for (int i = 0; i < Mathf.Min(10, originalPixels.Length); i += originalPixels.Length / 10)
            {
                // Processed pixels should be brighter (but clamped to 1.0)
                Assert.GreaterOrEqual(processedPixels[i].r, originalPixels[i].r);
                Assert.GreaterOrEqual(processedPixels[i].g, originalPixels[i].g);
                Assert.GreaterOrEqual(processedPixels[i].b, originalPixels[i].b);
            }
        }
        
        [UnityTest]
        public IEnumerator ProcessImage_WithContrastAdjustment_ChangesPixelContrast()
        {
            // Arrange
            processor.SetOriginalTexture(gradientTexture);
            
            testAdjustments.contrast = 0.5f; // Increase contrast
            
            bool imageProcessedEventFired = false;
            Texture2D processedTexture = null;
            processor.OnImageProcessed += (texture) => 
            {
                imageProcessedEventFired = true;
                processedTexture = texture;
            };
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(imageProcessedEventFired);
            Assert.IsNotNull(processedTexture);
            
            // Check that contrast has changed
            Color[] processedPixels = processedTexture.GetPixels();
            
            // For a gradient with increased contrast, dark areas should be darker and bright areas brighter
            // Sample left side (should be darker) and right side (should be brighter)
            Color leftPixel = processedPixels[32 * 64 + 10]; // Left side
            Color rightPixel = processedPixels[32 * 64 + 54]; // Right side
            
            // With increased contrast, the difference should be more pronounced
            float contrastDifference = rightPixel.r - leftPixel.r;
            Assert.Greater(contrastDifference, 0.3f); // Should have significant contrast
        }
        
        [UnityTest]
        public IEnumerator ProcessImage_WithHueAdjustment_ChangesPixelHue()
        {
            // Arrange
            // Create a colored test texture
            Texture2D coloredTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] coloredPixels = new Color[32 * 32];
            for (int i = 0; i < coloredPixels.Length; i++)
            {
                coloredPixels[i] = Color.red; // Pure red
            }
            coloredTexture.SetPixels(coloredPixels);
            coloredTexture.Apply();
            
            processor.SetOriginalTexture(coloredTexture);
            
            testAdjustments.hue = 120f; // Shift hue by 120 degrees (red -> green)
            
            bool imageProcessedEventFired = false;
            Texture2D processedTexture = null;
            processor.OnImageProcessed += (texture) => 
            {
                imageProcessedEventFired = true;
                processedTexture = texture;
            };
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(imageProcessedEventFired);
            Assert.IsNotNull(processedTexture);
            
            // Check that hue has shifted
            Color[] processedPixels = processedTexture.GetPixels();
            Color samplePixel = processedPixels[0];
            
            // After 120-degree hue shift, red should become green-ish
            Assert.Greater(samplePixel.g, samplePixel.r); // Green should be dominant
            
            // Clean up
            Object.DestroyImmediate(coloredTexture);
        }
        
        [UnityTest]
        public IEnumerator ProcessImage_WithSaturationAdjustment_ChangesPixelSaturation()
        {
            // Arrange
            // Create a colored test texture
            Texture2D coloredTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] coloredPixels = new Color[32 * 32];
            for (int i = 0; i < coloredPixels.Length; i++)
            {
                coloredPixels[i] = new Color(1f, 0.5f, 0.5f, 1f); // Light red
            }
            coloredTexture.SetPixels(coloredPixels);
            coloredTexture.Apply();
            
            processor.SetOriginalTexture(coloredTexture);
            
            testAdjustments.saturation = -0.5f; // Reduce saturation
            
            bool imageProcessedEventFired = false;
            Texture2D processedTexture = null;
            processor.OnImageProcessed += (texture) => 
            {
                imageProcessedEventFired = true;
                processedTexture = texture;
            };
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(imageProcessedEventFired);
            Assert.IsNotNull(processedTexture);
            
            // Check that saturation has decreased (colors should be more gray)
            Color[] processedPixels = processedTexture.GetPixels();
            Color samplePixel = processedPixels[0];
            
            // With reduced saturation, RGB values should be closer together
            float colorRange = Mathf.Max(samplePixel.r, samplePixel.g, samplePixel.b) - 
                              Mathf.Min(samplePixel.r, samplePixel.g, samplePixel.b);
            Assert.Less(colorRange, 0.3f); // Should be less saturated
            
            // Clean up
            Object.DestroyImmediate(coloredTexture);
        }
        
        [UnityTest]
        public IEnumerator ProcessImage_WithMultipleAdjustments_AppliesAllCorrectly()
        {
            // Arrange
            processor.SetOriginalTexture(gradientTexture);
            
            testAdjustments.cropArea = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
            testAdjustments.isCropped = true;
            testAdjustments.exposure = 0.5f;
            testAdjustments.contrast = 0.3f;
            
            bool imageProcessedEventFired = false;
            Texture2D processedTexture = null;
            processor.OnImageProcessed += (texture) => 
            {
                imageProcessedEventFired = true;
                processedTexture = texture;
            };
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(imageProcessedEventFired);
            Assert.IsNotNull(processedTexture);
            
            // Check that cropping was applied
            int expectedWidth = Mathf.RoundToInt(gradientTexture.width * 0.5f);
            int expectedHeight = Mathf.RoundToInt(gradientTexture.height * 0.5f);
            
            Assert.AreEqual(expectedWidth, processedTexture.width);
            Assert.AreEqual(expectedHeight, processedTexture.height);
            
            // Check that color adjustments were applied
            Color[] processedPixels = processedTexture.GetPixels();
            
            // Pixels should be brighter due to exposure adjustment
            // and have more contrast due to contrast adjustment
            Assert.Greater(processedPixels[0].r, 0.1f); // Should be brighter than very dark
        }
        
        [Test]
        public void ResetAdjustments_ResetsToOriginalImage()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            
            testAdjustments.exposure = 1f;
            processor.ApplyAdjustments(testAdjustments);
            
            // Act
            processor.ResetAdjustments();
            
            // Assert
            ImageAdjustments currentAdjustments = processor.CurrentAdjustments;
            Assert.AreEqual(0f, currentAdjustments.exposure, 0.01f);
            Assert.AreEqual(0f, currentAdjustments.contrast, 0.01f);
            Assert.AreEqual(0f, currentAdjustments.hue, 0.01f);
            Assert.AreEqual(0f, currentAdjustments.saturation, 0.01f);
            Assert.IsFalse(currentAdjustments.isCropped);
        }
        
        [Test]
        public void GetCropAreaPixels_ValidCropArea_ReturnsCorrectPixelCoordinates()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            
            testAdjustments.cropArea = new Rect(0.25f, 0.25f, 0.5f, 0.5f);
            testAdjustments.isCropped = true;
            processor.ApplyAdjustments(testAdjustments);
            
            // Act
            Rect pixelCropArea = processor.GetCropAreaPixels();
            
            // Assert
            float expectedX = 0.25f * testTexture.width;
            float expectedY = 0.25f * testTexture.height;
            float expectedWidth = 0.5f * testTexture.width;
            float expectedHeight = 0.5f * testTexture.height;
            
            Assert.AreEqual(expectedX, pixelCropArea.x, 0.1f);
            Assert.AreEqual(expectedY, pixelCropArea.y, 0.1f);
            Assert.AreEqual(expectedWidth, pixelCropArea.width, 0.1f);
            Assert.AreEqual(expectedHeight, pixelCropArea.height, 0.1f);
        }
        
        [Test]
        public void GetCropAreaPixels_NoTexture_ReturnsZeroRect()
        {
            // Act
            Rect pixelCropArea = processor.GetCropAreaPixels();
            
            // Assert
            Assert.AreEqual(new Rect(0, 0, 0, 0), pixelCropArea);
        }
        
        [UnityTest]
        public IEnumerator ProcessingProgress_ReportsProgress()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            
            bool progressEventFired = false;
            float lastProgress = -1f;
            processor.OnProcessingProgress += (progress) => 
            {
                progressEventFired = true;
                lastProgress = progress;
            };
            
            testAdjustments.exposure = 0.5f;
            
            // Act
            processor.ApplyAdjustments(testAdjustments);
            
            // Wait for processing to complete
            yield return new WaitForSeconds(0.5f);
            
            // Assert
            Assert.IsTrue(progressEventFired);
            Assert.AreEqual(1f, lastProgress, 0.01f); // Should complete at 100%
        }
        
        [Test]
        public void CropArea_BoundaryValidation_HandlesEdgeCases()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            
            var testCases = new[]
            {
                new Rect(0f, 0f, 1f, 1f),     // Full image
                new Rect(0f, 0f, 0.1f, 0.1f), // Small crop at origin
                new Rect(0.9f, 0.9f, 0.1f, 0.1f), // Small crop at corner
                new Rect(0.5f, 0.5f, 0.5f, 0.5f), // Half image from center
            };
            
            foreach (var cropArea in testCases)
            {
                // Act
                testAdjustments.cropArea = cropArea;
                testAdjustments.isCropped = true;
                
                // Should not throw exceptions
                Assert.DoesNotThrow(() => processor.ApplyAdjustments(testAdjustments));
            }
        }
        
        [Test]
        public void AdjustmentValues_ExtremeValues_HandledGracefully()
        {
            // Arrange
            processor.SetOriginalTexture(testTexture);
            
            // Test extreme values
            testAdjustments.exposure = 10f;   // Very high exposure
            testAdjustments.contrast = -5f;   // Very low contrast
            testAdjustments.hue = 720f;       // Multiple rotations
            testAdjustments.saturation = 5f;  // Very high saturation
            
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => processor.ApplyAdjustments(testAdjustments));
        }
    }
}