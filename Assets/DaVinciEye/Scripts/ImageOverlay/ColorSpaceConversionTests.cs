using NUnit.Framework;
using UnityEngine;
using DaVinciEye.ImageOverlay;

namespace DaVinciEye.Tests.ImageOverlay
{
    /// <summary>
    /// Tests for color space conversion accuracy and performance
    /// Validates HSV color space manipulation algorithms used in image adjustments
    /// </summary>
    public class ColorSpaceConversionTests
    {
        private GameObject testObject;
        private HueSaturationController hueSaturationController;
        
        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestColorSpaceConversion");
            hueSaturationController = testObject.AddComponent<HueSaturationController>();
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
        public void RGBToHSVToRGB_PrimaryColors_MaintainsAccuracy()
        {
            // Test primary colors for conversion accuracy
            var primaryColors = new[]
            {
                Color.red,
                Color.green,
                Color.blue,
                Color.cyan,
                Color.magenta,
                Color.yellow,
                Color.white,
                Color.black,
                Color.gray
            };
            
            foreach (Color originalColor in primaryColors)
            {
                // Convert RGB to HSV and back to RGB
                Color.RGBToHSV(originalColor, out float h, out float s, out float v);
                Color convertedColor = Color.HSVToRGB(h, s, v);
                
                // Assert colors are nearly identical (allowing for floating point precision)
                Assert.AreEqual(originalColor.r, convertedColor.r, 0.01f, 
                    $"Red component mismatch for {originalColor}");
                Assert.AreEqual(originalColor.g, convertedColor.g, 0.01f, 
                    $"Green component mismatch for {originalColor}");
                Assert.AreEqual(originalColor.b, convertedColor.b, 0.01f, 
                    $"Blue component mismatch for {originalColor}");
            }
        }
        
        [Test]
        public void HueShift_PrimaryColors_ProducesExpectedResults()
        {
            // Test hue shifts on primary colors
            var testCases = new[]
            {
                new { 
                    input = Color.red, 
                    hueShift = 120f, 
                    expectedDominant = "green",
                    description = "Red shifted 120° should become green-ish"
                },
                new { 
                    input = Color.green, 
                    hueShift = 120f, 
                    expectedDominant = "blue",
                    description = "Green shifted 120° should become blue-ish"
                },
                new { 
                    input = Color.blue, 
                    hueShift = 120f, 
                    expectedDominant = "red",
                    description = "Blue shifted 120° should become red-ish"
                },
                new { 
                    input = Color.red, 
                    hueShift = 60f, 
                    expectedDominant = "yellow",
                    description = "Red shifted 60° should become yellow-ish"
                }
            };
            
            foreach (var testCase in testCases)
            {
                // Apply hue shift
                hueSaturationController.SetHue(testCase.hueShift);
                Color result = hueSaturationController.ApplyAdjustmentsToColor(testCase.input);
                
                // Verify the expected dominant color component
                switch (testCase.expectedDominant)
                {
                    case "red":
                        Assert.Greater(result.r, result.g, testCase.description);
                        Assert.Greater(result.r, result.b, testCase.description);
                        break;
                    case "green":
                        Assert.Greater(result.g, result.r, testCase.description);
                        Assert.Greater(result.g, result.b, testCase.description);
                        break;
                    case "blue":
                        Assert.Greater(result.b, result.r, testCase.description);
                        Assert.Greater(result.b, result.g, testCase.description);
                        break;
                    case "yellow":
                        Assert.Greater(result.r, result.b, testCase.description);
                        Assert.Greater(result.g, result.b, testCase.description);
                        break;
                }
                
                // Reset for next test
                hueSaturationController.ResetAdjustments();
            }
        }
        
        [Test]
        public void SaturationAdjustment_GrayscaleColors_RemainsGrayscale()
        {
            // Grayscale colors should remain grayscale regardless of saturation adjustment
            var grayscaleColors = new[]
            {
                Color.black,
                Color.gray,
                Color.white,
                new Color(0.25f, 0.25f, 0.25f, 1f),
                new Color(0.75f, 0.75f, 0.75f, 1f)
            };
            
            float[] saturationAdjustments = { -1f, -0.5f, 0.5f, 1f };
            
            foreach (Color grayscaleColor in grayscaleColors)
            {
                foreach (float saturationAdjust in saturationAdjustments)
                {
                    hueSaturationController.SetSaturation(saturationAdjust);
                    Color result = hueSaturationController.ApplyAdjustmentsToColor(grayscaleColor);
                    
                    // RGB components should remain equal (grayscale)
                    Assert.AreEqual(result.r, result.g, 0.01f, 
                        $"Grayscale color {grayscaleColor} with saturation {saturationAdjust} should maintain equal RGB");
                    Assert.AreEqual(result.g, result.b, 0.01f, 
                        $"Grayscale color {grayscaleColor} with saturation {saturationAdjust} should maintain equal RGB");
                    
                    hueSaturationController.ResetAdjustments();
                }
            }
        }
        
        [Test]
        public void SaturationReduction_ColoredPixels_ReducesColorfulness()
        {
            // Test that reducing saturation makes colors less colorful (closer to grayscale)
            var coloredPixels = new[]
            {
                Color.red,
                Color.green,
                Color.blue,
                new Color(1f, 0.5f, 0.2f, 1f), // Orange
                new Color(0.8f, 0.2f, 0.9f, 1f) // Purple
            };
            
            foreach (Color originalColor in coloredPixels)
            {
                // Calculate original color range (difference between max and min RGB)
                float originalRange = Mathf.Max(originalColor.r, originalColor.g, originalColor.b) - 
                                    Mathf.Min(originalColor.r, originalColor.g, originalColor.b);
                
                // Apply saturation reduction
                hueSaturationController.SetSaturation(-0.8f);
                Color desaturatedColor = hueSaturationController.ApplyAdjustmentsToColor(originalColor);
                
                // Calculate desaturated color range
                float desaturatedRange = Mathf.Max(desaturatedColor.r, desaturatedColor.g, desaturatedColor.b) - 
                                       Mathf.Min(desaturatedColor.r, desaturatedColor.g, desaturatedColor.b);
                
                // Desaturated color should have smaller range (closer to grayscale)
                Assert.Less(desaturatedRange, originalRange, 
                    $"Desaturated color {originalColor} should have smaller RGB range");
                
                hueSaturationController.ResetAdjustments();
            }
        }
        
        [Test]
        public void SaturationIncrease_ColoredPixels_IncreasesColorfulness()
        {
            // Test that increasing saturation makes colors more colorful
            var partiallyColoredPixels = new[]
            {
                new Color(0.8f, 0.6f, 0.6f, 1f), // Light red
                new Color(0.6f, 0.8f, 0.6f, 1f), // Light green
                new Color(0.6f, 0.6f, 0.8f, 1f), // Light blue
                new Color(0.7f, 0.7f, 0.5f, 1f)  // Yellowish
            };
            
            foreach (Color originalColor in partiallyColoredPixels)
            {
                // Calculate original color range
                float originalRange = Mathf.Max(originalColor.r, originalColor.g, originalColor.b) - 
                                    Mathf.Min(originalColor.r, originalColor.g, originalColor.b);
                
                // Apply saturation increase
                hueSaturationController.SetSaturation(0.5f);
                Color saturatedColor = hueSaturationController.ApplyAdjustmentsToColor(originalColor);
                
                // Calculate saturated color range
                float saturatedRange = Mathf.Max(saturatedColor.r, saturatedColor.g, saturatedColor.b) - 
                                     Mathf.Min(saturatedColor.r, saturatedColor.g, saturatedColor.b);
                
                // Saturated color should have larger range (more colorful)
                Assert.GreaterOrEqual(saturatedRange, originalRange, 
                    $"Saturated color {originalColor} should have larger or equal RGB range");
                
                hueSaturationController.ResetAdjustments();
            }
        }
        
        [Test]
        public void HueShift_360Degrees_ReturnsToOriginal()
        {
            // Test that shifting hue by 360 degrees returns to original color
            var testColors = new[]
            {
                Color.red,
                Color.green,
                Color.blue,
                new Color(0.8f, 0.3f, 0.6f, 1f)
            };
            
            foreach (Color originalColor in testColors)
            {
                // Apply 360-degree hue shift (should return to original)
                // Note: Our controller clamps to ±180, so we test with multiple 120° shifts
                hueSaturationController.SetHue(120f);
                Color shifted120 = hueSaturationController.ApplyAdjustmentsToColor(originalColor);
                
                hueSaturationController.SetHue(120f);
                Color shifted240 = hueSaturationController.ApplyAdjustmentsToColor(shifted120);
                
                hueSaturationController.SetHue(120f);
                Color shifted360 = hueSaturationController.ApplyAdjustmentsToColor(shifted240);
                
                // Should be close to original (allowing for floating point precision)
                Assert.AreEqual(originalColor.r, shifted360.r, 0.1f, 
                    $"360° hue shift should return to original red component for {originalColor}");
                Assert.AreEqual(originalColor.g, shifted360.g, 0.1f, 
                    $"360° hue shift should return to original green component for {originalColor}");
                Assert.AreEqual(originalColor.b, shifted360.b, 0.1f, 
                    $"360° hue shift should return to original blue component for {originalColor}");
                
                hueSaturationController.ResetAdjustments();
            }
        }
        
        [Test]
        public void ColorSpaceConversion_EdgeCases_HandledGracefully()
        {
            // Test edge cases that might cause issues in color space conversion
            var edgeCaseColors = new[]
            {
                new Color(0f, 0f, 0f, 1f),     // Pure black
                new Color(1f, 1f, 1f, 1f),     // Pure white
                new Color(1f, 0f, 0f, 1f),     // Pure red
                new Color(0f, 1f, 0f, 1f),     // Pure green
                new Color(0f, 0f, 1f, 1f),     // Pure blue
                new Color(0.001f, 0f, 0f, 1f), // Nearly black red
                new Color(0.999f, 1f, 1f, 1f), // Nearly white
                new Color(0.5f, 0.5f, 0.5f, 1f) // Perfect gray
            };
            
            float[] extremeAdjustments = { -180f, -1f, 0f, 1f, 180f };
            
            foreach (Color edgeColor in edgeCaseColors)
            {
                foreach (float hueAdjust in extremeAdjustments)
                {
                    foreach (float satAdjust in extremeAdjustments)
                    {
                        if (satAdjust >= -1f && satAdjust <= 1f) // Valid saturation range
                        {
                            hueSaturationController.SetAdjustments(hueAdjust, satAdjust);
                            Color result = hueSaturationController.ApplyAdjustmentsToColor(edgeColor);
                            
                            // Verify no NaN or invalid values
                            Assert.IsFalse(float.IsNaN(result.r), 
                                $"Red component should not be NaN for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.IsFalse(float.IsNaN(result.g), 
                                $"Green component should not be NaN for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.IsFalse(float.IsNaN(result.b), 
                                $"Blue component should not be NaN for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            
                            // Verify values are in valid range
                            Assert.GreaterOrEqual(result.r, 0f, 
                                $"Red component should be >= 0 for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.LessOrEqual(result.r, 1f, 
                                $"Red component should be <= 1 for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.GreaterOrEqual(result.g, 0f, 
                                $"Green component should be >= 0 for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.LessOrEqual(result.g, 1f, 
                                $"Green component should be <= 1 for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.GreaterOrEqual(result.b, 0f, 
                                $"Blue component should be >= 0 for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                            Assert.LessOrEqual(result.b, 1f, 
                                $"Blue component should be <= 1 for {edgeColor} with adjustments H:{hueAdjust} S:{satAdjust}");
                        }
                    }
                }
                
                hueSaturationController.ResetAdjustments();
            }
        }
        
        [Test]
        public void ColorDifference_Calculation_ReflectsVisualDifference()
        {
            // Test that color difference calculation correlates with visual perception
            var colorPairs = new[]
            {
                new { color1 = Color.red, color2 = Color.red, expectedDiff = 0f, description = "Identical colors" },
                new { color1 = Color.red, color2 = Color.blue, expectedDiff = 1.4f, description = "Opposite colors" },
                new { color1 = Color.red, color2 = new Color(0.9f, 0f, 0f, 1f), expectedDiff = 0.1f, description = "Similar reds" },
                new { color1 = Color.black, color2 = Color.white, expectedDiff = 1.7f, description = "Black to white" }
            };
            
            foreach (var pair in colorPairs)
            {
                float difference = hueSaturationController.CalculateColorDifference(pair.color1, pair.color2);
                
                if (pair.expectedDiff == 0f)
                {
                    Assert.AreEqual(pair.expectedDiff, difference, 0.01f, pair.description);
                }
                else
                {
                    Assert.AreEqual(pair.expectedDiff, difference, 0.3f, pair.description);
                }
            }
        }
        
        [Test]
        public void ColorAdjustment_Performance_CompletesWithinReasonableTime()
        {
            // Performance test: adjust a large number of colors
            const int colorCount = 1000;
            Color[] testColors = new Color[colorCount];
            
            // Generate test colors
            for (int i = 0; i < colorCount; i++)
            {
                testColors[i] = new Color(
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    Random.Range(0f, 1f),
                    1f
                );
            }
            
            // Set some adjustments
            hueSaturationController.SetAdjustments(45f, 0.3f);
            
            // Measure time
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Apply adjustments to all colors
            for (int i = 0; i < colorCount; i++)
            {
                Color adjustedColor = hueSaturationController.ApplyAdjustmentsToColor(testColors[i]);
                
                // Verify result is valid
                Assert.IsFalse(float.IsNaN(adjustedColor.r));
                Assert.IsFalse(float.IsNaN(adjustedColor.g));
                Assert.IsFalse(float.IsNaN(adjustedColor.b));
            }
            
            stopwatch.Stop();
            
            // Should complete within reasonable time (less than 100ms for 1000 colors)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, 
                $"Color adjustment should complete within 100ms, took {stopwatch.ElapsedMilliseconds}ms");
            
            Debug.Log($"ColorSpaceConversionTests: Processed {colorCount} colors in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}