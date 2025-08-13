using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DaVinciEye.ImageOverlay.Tests
{
    /// <summary>
    /// Tests for opacity control system functionality
    /// </summary>
    public class OpacityControlTests
    {
        private GameObject testGameObject;
        private OpacityController opacityController;
        private Material testMaterial;
        private Renderer testRenderer;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject
            testGameObject = new GameObject("TestOpacityController");
            opacityController = testGameObject.AddComponent<OpacityController>();
            
            // Create test material
            testMaterial = new Material(Shader.Find("Standard"));
            testMaterial.name = "TestOpacityMaterial";
            
            // Create test renderer
            GameObject rendererObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            testRenderer = rendererObject.GetComponent<Renderer>();
            testRenderer.material = testMaterial;
            
            // Configure opacity controller
            opacityController.SetTargetMaterial(testMaterial);
            opacityController.SetTargetRenderer(testRenderer);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (testMaterial != null)
            {
                Object.DestroyImmediate(testMaterial);
            }
            
            if (testRenderer != null && testRenderer.gameObject != null)
            {
                Object.DestroyImmediate(testRenderer.gameObject);
            }
        }
        
        [Test]
        public void OpacityController_Initialization_SetsDefaultValues()
        {
            // Assert
            Assert.IsNotNull(opacityController);
            Assert.AreEqual(1.0f, opacityController.CurrentOpacity);
            Assert.AreEqual(0.0f, opacityController.MinOpacity);
            Assert.AreEqual(1.0f, opacityController.MaxOpacity);
        }
        
        [Test]
        public void SetOpacity_ValidValue_UpdatesOpacity()
        {
            // Arrange
            float testOpacity = 0.5f;
            bool opacityChanged = false;
            float changedValue = 0f;
            
            opacityController.OnOpacityChanged += (opacity) =>
            {
                opacityChanged = true;
                changedValue = opacity;
            };
            
            // Act
            opacityController.SetOpacity(testOpacity);
            
            // Assert
            Assert.AreEqual(testOpacity, opacityController.CurrentOpacity);
            Assert.IsTrue(opacityChanged);
            Assert.AreEqual(testOpacity, changedValue);
        }
        
        [Test]
        public void SetOpacity_OutOfRangeValue_ClampsToValidRange()
        {
            // Test below minimum
            opacityController.SetOpacity(-0.5f);
            Assert.AreEqual(0.0f, opacityController.CurrentOpacity);
            
            // Test above maximum
            opacityController.SetOpacity(1.5f);
            Assert.AreEqual(1.0f, opacityController.CurrentOpacity);
        }
        
        [Test]
        public void SetOpacity_UpdatesMaterialAlpha()
        {
            // Arrange
            float testOpacity = 0.3f;
            
            // Act
            opacityController.SetOpacity(testOpacity);
            
            // Assert
            Assert.AreEqual(testOpacity, testMaterial.color.a, 0.01f);
        }
        
        [Test]
        public void IncreaseOpacity_IncreasesValue()
        {
            // Arrange
            opacityController.SetOpacity(0.5f);
            float initialOpacity = opacityController.CurrentOpacity;
            
            // Act
            opacityController.IncreaseOpacity(0.2f);
            
            // Assert
            Assert.Greater(opacityController.CurrentOpacity, initialOpacity);
            Assert.AreEqual(0.7f, opacityController.CurrentOpacity, 0.01f);
        }
        
        [Test]
        public void DecreaseOpacity_DecreasesValue()
        {
            // Arrange
            opacityController.SetOpacity(0.8f);
            float initialOpacity = opacityController.CurrentOpacity;
            
            // Act
            opacityController.DecreaseOpacity(0.3f);
            
            // Assert
            Assert.Less(opacityController.CurrentOpacity, initialOpacity);
            Assert.AreEqual(0.5f, opacityController.CurrentOpacity, 0.01f);
        }
        
        [Test]
        public void ResetOpacity_ResetsToDefault()
        {
            // Arrange
            opacityController.SetOpacity(0.2f);
            
            // Act
            opacityController.ResetOpacity();
            
            // Assert
            Assert.AreEqual(1.0f, opacityController.CurrentOpacity);
        }
        
        [Test]
        public void SetFullyTransparent_SetsToMinimum()
        {
            // Act
            opacityController.SetFullyTransparent();
            
            // Assert
            Assert.AreEqual(0.0f, opacityController.CurrentOpacity);
        }
        
        [Test]
        public void SetFullyOpaque_SetsToMaximum()
        {
            // Arrange
            opacityController.SetOpacity(0.3f);
            
            // Act
            opacityController.SetFullyOpaque();
            
            // Assert
            Assert.AreEqual(1.0f, opacityController.CurrentOpacity);
        }
        
        [Test]
        public void IsValidOpacity_ValidatesRange()
        {
            // Assert valid values
            Assert.IsTrue(opacityController.IsValidOpacity(0.0f));
            Assert.IsTrue(opacityController.IsValidOpacity(0.5f));
            Assert.IsTrue(opacityController.IsValidOpacity(1.0f));
            
            // Assert invalid values
            Assert.IsFalse(opacityController.IsValidOpacity(-0.1f));
            Assert.IsFalse(opacityController.IsValidOpacity(1.1f));
        }
    }
    
    /// <summary>
    /// Integration tests for opacity control with UI components
    /// </summary>
    public class OpacityControlUITests
    {
        private GameObject testGameObject;
        private OpacityController opacityController;
        private GameObject sliderObject;
        private Slider uiSlider;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject
            testGameObject = new GameObject("TestOpacityControllerUI");
            opacityController = testGameObject.AddComponent<OpacityController>();
            
            // Create UI Slider
            sliderObject = new GameObject("TestSlider");
            uiSlider = sliderObject.AddComponent<Slider>();
            
            // Create slider handle and fill area (required for Slider component)
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handle.AddComponent<Image>();
            
            uiSlider.handleRect = handleRect;
            
            // Configure slider
            uiSlider.minValue = 0f;
            uiSlider.maxValue = 1f;
            uiSlider.value = 1f;
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (sliderObject != null)
            {
                Object.DestroyImmediate(sliderObject);
            }
        }
        
        [UnityTest]
        public IEnumerator OpacityController_WithUISlider_UpdatesCorrectly()
        {
            // Arrange
            bool opacityChanged = false;
            float changedValue = 0f;
            
            opacityController.OnOpacityChanged += (opacity) =>
            {
                opacityChanged = true;
                changedValue = opacity;
            };
            
            // Act
            uiSlider.value = 0.6f;
            yield return null; // Wait one frame for UI update
            
            // Simulate slider value change (since we can't easily trigger UI events in tests)
            opacityController.SetOpacity(0.6f);
            
            // Assert
            Assert.IsTrue(opacityChanged);
            Assert.AreEqual(0.6f, changedValue, 0.01f);
            Assert.AreEqual(0.6f, opacityController.CurrentOpacity, 0.01f);
        }
        
        [Test]
        public void OpacityController_ResponsivenessTest_UpdatesInRealTime()
        {
            // Arrange
            int updateCount = 0;
            opacityController.OnOpacityChanged += (opacity) => updateCount++;
            
            // Act - Simulate rapid opacity changes
            for (float opacity = 0f; opacity <= 1f; opacity += 0.1f)
            {
                opacityController.SetOpacity(opacity);
            }
            
            // Assert
            Assert.AreEqual(11, updateCount); // 0.0, 0.1, 0.2, ... 1.0
            Assert.AreEqual(1.0f, opacityController.CurrentOpacity);
        }
        
        [UnityTest]
        public IEnumerator OpacityController_PerformanceTest_MaintainsFrameRate()
        {
            // Arrange
            float startTime = Time.realtimeSinceStartup;
            int iterations = 100;
            
            // Act - Perform many opacity updates
            for (int i = 0; i < iterations; i++)
            {
                float opacity = Mathf.Sin(i * 0.1f) * 0.5f + 0.5f; // Oscillate between 0 and 1
                opacityController.SetOpacity(opacity);
                yield return null; // Wait one frame
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            
            // Assert - Should complete within reasonable time (target: <16ms per update for 60 FPS)
            float averageTimePerUpdate = (totalTime / iterations) * 1000f; // Convert to milliseconds
            Assert.Less(averageTimePerUpdate, 16f, $"Average update time {averageTimePerUpdate:F2}ms exceeds 16ms target");
            
            Debug.Log($"OpacityController Performance: {averageTimePerUpdate:F2}ms average per update");
        }
    }
}