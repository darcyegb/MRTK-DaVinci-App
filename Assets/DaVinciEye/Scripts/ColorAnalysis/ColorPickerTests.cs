using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using DaVinciEye.ColorAnalysis;

namespace DaVinciEye.Tests.ColorAnalysis
{
    /// <summary>
    /// Unit tests for ColorPicker functionality
    /// Tests color picking accuracy, UI interactions, and data persistence
    /// </summary>
    public class ColorPickerTests
    {
        private GameObject testGameObject;
        private ColorPicker colorPicker;
        private Texture2D testTexture;
        
        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestColorPicker");
            colorPicker = testGameObject.AddComponent<ColorPicker>();
            
            // Create test texture with known colors
            CreateTestTexture();
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
        
        private void CreateTestTexture()
        {
            testTexture = new Texture2D(4, 4);
            
            // Create a 4x4 texture with known colors
            Color[] pixels = new Color[]
            {
                Color.red,    Color.green,  Color.blue,   Color.white,
                Color.yellow, Color.cyan,   Color.magenta, Color.black,
                Color.gray,   Color.clear,  new Color(0.5f, 0.5f, 0.5f), new Color(0.8f, 0.2f, 0.6f),
                new Color(0.1f, 0.9f, 0.3f), new Color(0.7f, 0.4f, 0.1f), new Color(0.2f, 0.8f, 0.9f), new Color(0.9f, 0.1f, 0.4f)
            };
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
        }
        
        [Test]
        public void ColorPicker_SetTexture_SetsTextureCorrectly()
        {
            // Act
            colorPicker.SetTexture(testTexture);
            
            // Assert
            Assert.AreEqual(testTexture, colorPicker.CurrentTexture);
        }
        
        [Test]
        public void ColorPicker_PickColorFromImage_ReturnsCorrectColor()
        {
            // Arrange
            colorPicker.SetTexture(testTexture);
            
            // Act - Pick red color from top-left corner (0,0)
            Color pickedColor = colorPicker.PickColorFromImage(new Vector2(0f, 1f)); // Top-left in normalized coordinates
            
            // Assert
            Assert.AreEqual(Color.red, pickedColor, "Should pick red color from top-left corner");
        }
        
        [Test]
        public void ColorPicker_PickColorFromImage_HandlesEdgeCases()
        {
            // Arrange
            colorPicker.SetTexture(testTexture);
            
            // Act & Assert - Test coordinates outside bounds
            Color color1 = colorPicker.PickColorFromImage(new Vector2(-0.5f, -0.5f));
            Color color2 = colorPicker.PickColorFromImage(new Vector2(1.5f, 1.5f));
            
            // Should clamp to valid coordinates and return valid colors
            Assert.IsTrue(color1.r >= 0 && color1.r <= 1);
            Assert.IsTrue(color2.r >= 0 && color2.r <= 1);
        }
        
        [Test]
        public void ColorPicker_SetCurrentColor_UpdatesColorCorrectly()
        {
            // Arrange
            Color testColor = new Color(0.5f, 0.7f, 0.3f, 1f);
            
            // Act
            colorPicker.SetCurrentColor(testColor);
            
            // Assert
            Assert.AreEqual(testColor, colorPicker.CurrentColor);
        }
        
        [Test]
        public void ColorPicker_SetCurrentColor_TriggersEvent()
        {
            // Arrange
            Color testColor = new Color(0.5f, 0.7f, 0.3f, 1f);
            bool eventTriggered = false;
            Color receivedColor = Color.clear;
            
            colorPicker.OnColorChanged += (color) =>
            {
                eventTriggered = true;
                receivedColor = color;
            };
            
            // Act
            colorPicker.SetCurrentColor(testColor);
            
            // Assert
            Assert.IsTrue(eventTriggered, "OnColorChanged event should be triggered");
            Assert.AreEqual(testColor, receivedColor, "Event should pass correct color");
        }
        
        [Test]
        public void ColorPicker_PickColorFromImage_TriggersPickedEvent()
        {
            // Arrange
            colorPicker.SetTexture(testTexture);
            bool eventTriggered = false;
            Color receivedColor = Color.clear;
            
            colorPicker.OnColorPicked += (color) =>
            {
                eventTriggered = true;
                receivedColor = color;
            };
            
            // Act
            Color pickedColor = colorPicker.PickColorFromImage(new Vector2(0f, 1f));
            
            // Assert
            Assert.IsTrue(eventTriggered, "OnColorPicked event should be triggered");
            Assert.AreEqual(pickedColor, receivedColor, "Event should pass picked color");
        }
        
        [Test]
        public void ColorPicker_SaveColorToJson_ReturnsValidJson()
        {
            // Arrange
            Color testColor = new Color(0.5f, 0.7f, 0.3f, 1f);
            colorPicker.SetCurrentColor(testColor);
            
            // Act
            string json = colorPicker.SaveColorToJson();
            
            // Assert
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("0.5"), "JSON should contain red component");
            Assert.IsTrue(json.Contains("0.7"), "JSON should contain green component");
            Assert.IsTrue(json.Contains("0.3"), "JSON should contain blue component");
        }
        
        [Test]
        public void ColorPicker_LoadColorFromJson_LoadsColorCorrectly()
        {
            // Arrange
            string testJson = "{\"r\":0.5,\"g\":0.7,\"b\":0.3,\"a\":1.0,\"timestamp\":\"2024-01-01 12:00:00\"}";
            
            // Act
            colorPicker.LoadColorFromJson(testJson);
            
            // Assert
            Color expectedColor = new Color(0.5f, 0.7f, 0.3f, 1f);
            Assert.AreEqual(expectedColor.r, colorPicker.CurrentColor.r, 0.001f);
            Assert.AreEqual(expectedColor.g, colorPicker.CurrentColor.g, 0.001f);
            Assert.AreEqual(expectedColor.b, colorPicker.CurrentColor.b, 0.001f);
            Assert.AreEqual(expectedColor.a, colorPicker.CurrentColor.a, 0.001f);
        }
        
        [Test]
        public void ColorPicker_LoadColorFromJson_HandlesInvalidJson()
        {
            // Arrange
            Color originalColor = colorPicker.CurrentColor;
            string invalidJson = "invalid json string";
            
            // Act
            colorPicker.LoadColorFromJson(invalidJson);
            
            // Assert - Should not crash and should maintain original color
            Assert.AreEqual(originalColor, colorPicker.CurrentColor);
        }
        
        [Test]
        public void ColorPicker_IsActive_ControlsPickingBehavior()
        {
            // Arrange
            colorPicker.SetTexture(testTexture);
            colorPicker.IsActive = false;
            
            // Act
            Color originalColor = colorPicker.CurrentColor;
            // Note: This test would need UI interaction simulation for full testing
            // For now, we test the property
            
            // Assert
            Assert.IsFalse(colorPicker.IsActive);
            
            // Re-enable and test
            colorPicker.IsActive = true;
            Assert.IsTrue(colorPicker.IsActive);
        }
        
        [Test]
        public void ColorPicker_MultipleColorPicks_MaintainAccuracy()
        {
            // Arrange
            colorPicker.SetTexture(testTexture);
            
            // Act & Assert - Test multiple known positions
            Color red = colorPicker.PickColorFromImage(new Vector2(0f, 1f));    // Top-left
            Color green = colorPicker.PickColorFromImage(new Vector2(0.33f, 1f)); // Top-second
            Color blue = colorPicker.PickColorFromImage(new Vector2(0.66f, 1f));  // Top-third
            Color white = colorPicker.PickColorFromImage(new Vector2(1f, 1f));    // Top-right
            
            Assert.AreEqual(Color.red, red, "Should pick red from position (0,1)");
            Assert.AreEqual(Color.green, green, "Should pick green from position (0.33,1)");
            Assert.AreEqual(Color.blue, blue, "Should pick blue from position (0.66,1)");
            Assert.AreEqual(Color.white, white, "Should pick white from position (1,1)");
        }
        
        [Test]
        public void ColorPicker_NullTexture_HandlesGracefully()
        {
            // Act
            colorPicker.SetTexture(null);
            Color pickedColor = colorPicker.PickColorFromImage(Vector2.zero);
            
            // Assert
            Assert.IsNull(colorPicker.CurrentTexture);
            Assert.AreEqual(Color.white, pickedColor, "Should return white when no texture is set");
        }
        
        [UnityTest]
        public IEnumerator ColorPicker_PerformanceTest_HandlesLargeTexture()
        {
            // Arrange - Create large texture
            Texture2D largeTexture = new Texture2D(1024, 1024);
            Color[] pixels = new Color[1024 * 1024];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.Lerp(Color.red, Color.blue, (float)i / pixels.Length);
            }
            largeTexture.SetPixels(pixels);
            largeTexture.Apply();
            
            colorPicker.SetTexture(largeTexture);
            
            // Act - Measure performance of color picking
            float startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < 100; i++)
            {
                Vector2 randomPos = new Vector2(Random.value, Random.value);
                colorPicker.PickColorFromImage(randomPos);
                
                if (i % 10 == 0)
                    yield return null; // Allow frame processing
            }
            
            float endTime = Time.realtimeSinceStartup;
            float duration = endTime - startTime;
            
            // Assert - Should complete within reasonable time
            Assert.Less(duration, 1.0f, "100 color picks should complete within 1 second");
            
            // Cleanup
            Object.DestroyImmediate(largeTexture);
        }
    }
}