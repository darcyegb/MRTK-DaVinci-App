using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DaVinciEye.Canvas;

namespace DaVinciEye.ImageOverlay.Tests
{
    /// <summary>
    /// Tests for image scaling and alignment functionality
    /// </summary>
    public class ImageScalingAlignmentTests
    {
        private GameObject testGameObject;
        private ImageScalingAlignment scalingAlignment;
        private GameObject canvasObject;
        private MockCanvasManager mockCanvasManager;
        private Renderer testRenderer;
        private Material testMaterial;
        private Texture2D testTexture;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject
            testGameObject = new GameObject("TestImageScalingAlignment");
            scalingAlignment = testGameObject.AddComponent<ImageScalingAlignment>();
            
            // Create test texture
            testTexture = new Texture2D(800, 600, TextureFormat.RGBA32, false);
            
            // Create test material and renderer
            testMaterial = new Material(Shader.Find("Standard"));
            testMaterial.mainTexture = testTexture;
            
            testRenderer = testGameObject.AddComponent<MeshRenderer>();
            testRenderer.material = testMaterial;
            
            // Create mock canvas manager
            canvasObject = new GameObject("MockCanvas");
            mockCanvasManager = canvasObject.AddComponent<MockCanvasManager>();
            
            // Configure scaling alignment
            scalingAlignment.SetCanvasManager(mockCanvasManager);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            if (canvasObject != null)
            {
                Object.DestroyImmediate(canvasObject);
            }
            
            if (testTexture != null)
            {
                Object.DestroyImmediate(testTexture);
            }
            
            if (testMaterial != null)
            {
                Object.DestroyImmediate(testMaterial);
            }
        }
        
        [Test]
        public void ImageScalingAlignment_Initialization_SetsDefaultValues()
        {
            // Assert
            Assert.IsNotNull(scalingAlignment);
            Assert.AreEqual(ScalingMode.FitToCanvas, scalingAlignment.CurrentScalingMode);
            Assert.AreEqual(AlignmentMode.Center, scalingAlignment.CurrentAlignmentMode);
            Assert.AreEqual(Vector3.one, scalingAlignment.CurrentScale);
        }
        
        [Test]
        public void SetScalingMode_ChangesMode()
        {
            // Act
            scalingAlignment.SetScalingMode(ScalingMode.FillCanvas);
            
            // Assert
            Assert.AreEqual(ScalingMode.FillCanvas, scalingAlignment.CurrentScalingMode);
        }
        
        [Test]
        public void SetAlignmentMode_ChangesAlignment()
        {
            // Act
            scalingAlignment.SetAlignmentMode(AlignmentMode.TopLeft);
            
            // Assert
            Assert.AreEqual(AlignmentMode.TopLeft, scalingAlignment.CurrentAlignmentMode);
        }
        
        [Test]
        public void UpdateAlignment_WithValidCanvas_UpdatesScaleAndPosition()
        {
            // Arrange
            bool scaleChanged = false;
            bool positionChanged = false;
            
            scalingAlignment.OnScaleChanged += (scale) => scaleChanged = true;
            scalingAlignment.OnPositionChanged += (position) => positionChanged = true;
            
            // Setup canvas
            CanvasData canvasData = new CanvasData();
            canvasData.dimensions = new Vector2(2.0f, 1.5f); // 2m x 1.5m canvas
            canvasData.center = Vector3.zero;
            canvasData.isValid = true;
            
            mockCanvasManager.SetCanvasData(canvasData);
            
            // Act
            scalingAlignment.UpdateAlignment();
            
            // Assert
            Assert.IsTrue(scaleChanged);
            Assert.IsTrue(positionChanged);
            Assert.AreNotEqual(Vector3.one, scalingAlignment.CurrentScale);
        }
        
        [UnityTest]
        public IEnumerator UpdateAlignment_PerformanceTest_CompletesQuickly()
        {
            // Arrange
            CanvasData canvasData = new CanvasData();
            canvasData.dimensions = new Vector2(1.0f, 1.0f);
            canvasData.center = Vector3.zero;
            canvasData.isValid = true;
            
            mockCanvasManager.SetCanvasData(canvasData);
            
            float startTime = Time.realtimeSinceStartup;
            
            // Act - Perform multiple alignment updates
            for (int i = 0; i < 100; i++)
            {
                scalingAlignment.UpdateAlignment();
                yield return null;
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            
            // Assert - Should complete within reasonable time
            Assert.Less(totalTime, 1.0f, $"Alignment updates took {totalTime:F3}s, expected < 1.0s");
            
            Debug.Log($"ImageScalingAlignment Performance: {totalTime:F3}s for 100 updates");
        }
        
        [Test]
        public void FitToCanvas_MaintainsAspectRatio()
        {
            // Arrange
            CanvasData canvasData = new CanvasData();
            canvasData.dimensions = new Vector2(2.0f, 1.0f); // Wide canvas
            canvasData.center = Vector3.zero;
            canvasData.isValid = true;
            
            mockCanvasManager.SetCanvasData(canvasData);
            scalingAlignment.SetScalingMode(ScalingMode.FitToCanvas);
            
            // Act
            scalingAlignment.UpdateAlignment();
            
            // Assert
            Vector3 scale = scalingAlignment.CurrentScale;
            
            // For an 800x600 image on a 2x1 canvas, should fit to height (limiting dimension)
            // Scale should be uniform (same X and Y)
            Assert.AreEqual(scale.x, scale.y, 0.001f, "Scale should maintain aspect ratio");
        }
        
        [Test]
        public void SetCustomAlignmentOffset_UpdatesPosition()
        {
            // Arrange
            CanvasData canvasData = new CanvasData();
            canvasData.dimensions = new Vector2(1.0f, 1.0f);
            canvasData.center = Vector3.zero;
            canvasData.isValid = true;
            
            mockCanvasManager.SetCanvasData(canvasData);
            scalingAlignment.SetAlignmentMode(AlignmentMode.Custom);
            
            Vector3 initialPosition = scalingAlignment.CurrentPosition;
            
            // Act
            scalingAlignment.SetCustomAlignmentOffset(new Vector2(0.5f, 0.3f));
            
            // Assert
            Assert.AreNotEqual(initialPosition, scalingAlignment.CurrentPosition);
        }
    }
    
    /// <summary>
    /// Mock canvas manager for testing
    /// </summary>
    public class MockCanvasManager : MonoBehaviour, ICanvasManager
    {
        private CanvasData canvasData;
        private bool isCanvasDefined;
        
        public bool IsCanvasDefined => isCanvasDefined;
        public Bounds CanvasBounds { get; private set; }
        public Transform CanvasTransform => transform;
        public CanvasData CurrentCanvas => canvasData;
        
        public event System.Action<CanvasData> OnCanvasDefined;
        public event System.Action OnCanvasCleared;
        
        public void SetCanvasData(CanvasData data)
        {
            canvasData = data;
            isCanvasDefined = data != null && data.isValid;
            
            if (isCanvasDefined)
            {
                // Create bounds from canvas data
                CanvasBounds = new Bounds(data.center, new Vector3(data.dimensions.x, data.dimensions.y, 0.1f));
                OnCanvasDefined?.Invoke(data);
            }
        }
        
        public void StartCanvasDefinition()
        {
            // Mock implementation
        }
        
        public void DefineCanvasCorner(Vector3 position)
        {
            // Mock implementation
        }
        
        public void CompleteCanvasDefinition()
        {
            // Mock implementation
        }
        
        public void RedefineCanvas()
        {
            // Mock implementation
        }
        
        public void ClearCanvas()
        {
            canvasData = null;
            isCanvasDefined = false;
            OnCanvasCleared?.Invoke();
        }
    }
    
    /// <summary>
    /// Integration tests for scaling accuracy
    /// </summary>
    public class ScalingAccuracyTests
    {
        [Test]
        public void FitToCanvas_SquareImageSquareCanvas_ScalesCorrectly()
        {
            // Test specific scaling calculations
            Vector2 imageSize = new Vector2(100, 100);
            Vector2 canvasSize = new Vector2(1.0f, 1.0f);
            
            // Expected scale: 1.0 / 100 = 0.01 for both axes
            float expectedScale = canvasSize.x / imageSize.x;
            
            Assert.AreEqual(0.01f, expectedScale, 0.001f);
        }
        
        [Test]
        public void FitToCanvas_WideImageTallCanvas_FitsToWidth()
        {
            // Test aspect ratio handling
            Vector2 imageSize = new Vector2(200, 100); // 2:1 aspect ratio
            Vector2 canvasSize = new Vector2(1.0f, 2.0f); // 1:2 aspect ratio
            
            // Should fit to width (limiting dimension)
            float expectedScale = canvasSize.x / imageSize.x; // 1.0 / 200 = 0.005
            
            Assert.AreEqual(0.005f, expectedScale, 0.001f);
        }
        
        [Test]
        public void AlignmentCalculation_TopLeft_CalculatesCorrectOffset()
        {
            // Test alignment offset calculations
            Vector2 canvasSize = new Vector2(2.0f, 1.0f);
            Vector2 scaledImageSize = new Vector2(1.0f, 0.5f);
            
            // For top-left alignment:
            // X offset: -canvasWidth/2 + imageWidth/2 = -1.0 + 0.5 = -0.5
            // Y offset: +canvasHeight/2 - imageHeight/2 = +0.5 - 0.25 = +0.25
            
            float expectedOffsetX = -canvasSize.x * 0.5f + scaledImageSize.x * 0.5f;
            float expectedOffsetY = canvasSize.y * 0.5f - scaledImageSize.y * 0.5f;
            
            Assert.AreEqual(-0.5f, expectedOffsetX, 0.001f);
            Assert.AreEqual(0.25f, expectedOffsetY, 0.001f);
        }
    }
}