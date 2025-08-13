using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DaVinciEye.ImageOverlay.Tests
{
    /// <summary>
    /// Unit tests for image loading and format validation functionality
    /// </summary>
    public class ImageLoadingTests
    {
        private string testImageDirectory;
        private string validJpegPath;
        private string validPngPath;
        private string invalidPath;
        private string unsupportedFormatPath;
        
        [SetUp]
        public void Setup()
        {
            // Create test directory
            testImageDirectory = Path.Combine(Application.temporaryCachePath, "TestImages");
            Directory.CreateDirectory(testImageDirectory);
            
            // Create test file paths
            validJpegPath = Path.Combine(testImageDirectory, "test.jpg");
            validPngPath = Path.Combine(testImageDirectory, "test.png");
            invalidPath = Path.Combine(testImageDirectory, "nonexistent.jpg");
            unsupportedFormatPath = Path.Combine(testImageDirectory, "test.txt");
            
            // Create test images
            CreateTestImage(validJpegPath, TextureFormat.RGB24);
            CreateTestImage(validPngPath, TextureFormat.RGBA32);
            
            // Create unsupported format file
            File.WriteAllText(unsupportedFormatPath, "This is not an image");
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test directory
            if (Directory.Exists(testImageDirectory))
            {
                Directory.Delete(testImageDirectory, true);
            }
        }
        
        /// <summary>
        /// Creates a test image file for testing purposes
        /// </summary>
        private void CreateTestImage(string path, TextureFormat format)
        {
            // Create a simple test texture
            Texture2D testTexture = new Texture2D(64, 64, format, false);
            
            // Fill with a simple pattern
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(
                    (i % 64) / 64f,
                    (i / 64) / 64f,
                    0.5f,
                    1.0f
                );
            }
            
            testTexture.SetPixels(pixels);
            testTexture.Apply();
            
            // Encode and save
            byte[] imageData;
            if (path.EndsWith(".png"))
            {
                imageData = testTexture.EncodeToPNG();
            }
            else
            {
                imageData = testTexture.EncodeToJPG();
            }
            
            File.WriteAllBytes(path, imageData);
            
            // Clean up
            Object.DestroyImmediate(testTexture);
        }
        
        [Test]
        public void GetSupportedExtensions_ReturnsExpectedFormats()
        {
            // Act
            string[] extensions = ImageLoader.GetSupportedExtensions();
            
            // Assert
            Assert.IsNotNull(extensions);
            Assert.Contains(".jpg", extensions);
            Assert.Contains(".jpeg", extensions);
            Assert.Contains(".png", extensions);
            Assert.Contains(".bmp", extensions);
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_ValidJpegFile_ReturnsTexture()
        {
            // Arrange
            Task<Texture2D> loadTask = ImageLoader.LoadImageAsync(validJpegPath);
            
            // Act
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            Assert.IsNotNull(loadTask.Result);
            Assert.Greater(loadTask.Result.width, 0);
            Assert.Greater(loadTask.Result.height, 0);
            
            // Clean up
            if (loadTask.Result != null)
            {
                Object.DestroyImmediate(loadTask.Result);
            }
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_ValidPngFile_ReturnsTexture()
        {
            // Arrange
            Task<Texture2D> loadTask = ImageLoader.LoadImageAsync(validPngPath);
            
            // Act
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            Assert.IsNotNull(loadTask.Result);
            Assert.Greater(loadTask.Result.width, 0);
            Assert.Greater(loadTask.Result.height, 0);
            
            // Clean up
            if (loadTask.Result != null)
            {
                Object.DestroyImmediate(loadTask.Result);
            }
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_NonexistentFile_ReturnsNull()
        {
            // Arrange
            Task<Texture2D> loadTask = ImageLoader.LoadImageAsync(invalidPath);
            
            // Act
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            Assert.IsNull(loadTask.Result);
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_UnsupportedFormat_ReturnsNull()
        {
            // Arrange
            Task<Texture2D> loadTask = ImageLoader.LoadImageAsync(unsupportedFormatPath);
            
            // Act
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            Assert.IsNull(loadTask.Result);
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_NullPath_ReturnsNull()
        {
            // Arrange
            Task<Texture2D> loadTask = ImageLoader.LoadImageAsync(null);
            
            // Act
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            Assert.IsNull(loadTask.Result);
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_EmptyPath_ReturnsNull()
        {
            // Arrange
            Task<Texture2D> loadTask = ImageLoader.LoadImageAsync("");
            
            // Act
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.IsCompletedSuccessfully);
            Assert.IsNull(loadTask.Result);
        }
        
        [Test]
        public void CreatePlaceholderTexture_DefaultSize_ReturnsValidTexture()
        {
            // Act
            Texture2D placeholder = ImageLoader.CreatePlaceholderTexture();
            
            // Assert
            Assert.IsNotNull(placeholder);
            Assert.AreEqual(512, placeholder.width);
            Assert.AreEqual(512, placeholder.height);
            
            // Clean up
            Object.DestroyImmediate(placeholder);
        }
        
        [Test]
        public void CreatePlaceholderTexture_CustomSize_ReturnsCorrectSize()
        {
            // Arrange
            int width = 256;
            int height = 128;
            
            // Act
            Texture2D placeholder = ImageLoader.CreatePlaceholderTexture(width, height);
            
            // Assert
            Assert.IsNotNull(placeholder);
            Assert.AreEqual(width, placeholder.width);
            Assert.AreEqual(height, placeholder.height);
            
            // Clean up
            Object.DestroyImmediate(placeholder);
        }
    }
    
    /// <summary>
    /// Integration tests for ImageOverlayManager
    /// </summary>
    public class ImageOverlayManagerTests
    {
        private GameObject testGameObject;
        private ImageOverlayManager overlayManager;
        private string testImagePath;
        
        [SetUp]
        public void Setup()
        {
            // Create test GameObject
            testGameObject = new GameObject("TestImageOverlayManager");
            overlayManager = testGameObject.AddComponent<ImageOverlayManager>();
            
            // Create test image
            string testDir = Path.Combine(Application.temporaryCachePath, "OverlayTests");
            Directory.CreateDirectory(testDir);
            testImagePath = Path.Combine(testDir, "test.png");
            
            // Create simple test image
            Texture2D testTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.red;
            }
            testTexture.SetPixels(pixels);
            testTexture.Apply();
            
            byte[] imageData = testTexture.EncodeToPNG();
            File.WriteAllBytes(testImagePath, imageData);
            
            Object.DestroyImmediate(testTexture);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
            
            // Clean up test files
            string testDir = Path.Combine(Application.temporaryCachePath, "OverlayTests");
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
        
        [Test]
        public void ImageOverlayManager_Initialization_SetsDefaultValues()
        {
            // Assert
            Assert.IsNotNull(overlayManager);
            Assert.AreEqual(1.0f, overlayManager.Opacity);
            Assert.IsTrue(overlayManager.IsVisible);
            Assert.IsNull(overlayManager.CurrentImage);
            Assert.IsNotNull(overlayManager.CurrentAdjustments);
        }
        
        [UnityTest]
        public IEnumerator LoadImageAsync_ValidImage_LoadsSuccessfully()
        {
            // Arrange
            bool imageLoaded = false;
            Texture2D loadedImage = null;
            
            overlayManager.OnImageLoaded += (texture) =>
            {
                imageLoaded = true;
                loadedImage = texture;
            };
            
            // Act
            Task<bool> loadTask = overlayManager.LoadImageAsync(testImagePath);
            yield return new WaitUntil(() => loadTask.IsCompleted);
            
            // Assert
            Assert.IsTrue(loadTask.Result);
            Assert.IsTrue(imageLoaded);
            Assert.IsNotNull(loadedImage);
            Assert.IsNotNull(overlayManager.CurrentImage);
        }
        
        [Test]
        public void SetOpacity_ValidValue_UpdatesOpacity()
        {
            // Arrange
            float testOpacity = 0.5f;
            bool opacityChanged = false;
            float changedValue = 0f;
            
            overlayManager.OnOpacityChanged += (opacity) =>
            {
                opacityChanged = true;
                changedValue = opacity;
            };
            
            // Act
            overlayManager.SetOpacity(testOpacity);
            
            // Assert
            Assert.AreEqual(testOpacity, overlayManager.Opacity);
            Assert.IsTrue(opacityChanged);
            Assert.AreEqual(testOpacity, changedValue);
        }
        
        [Test]
        public void SetOpacity_OutOfRange_ClampsValue()
        {
            // Act & Assert
            overlayManager.SetOpacity(-0.5f);
            Assert.AreEqual(0f, overlayManager.Opacity);
            
            overlayManager.SetOpacity(1.5f);
            Assert.AreEqual(1f, overlayManager.Opacity);
        }
        
        [Test]
        public void ResetToOriginal_ResetsAdjustments()
        {
            // Arrange
            overlayManager.CurrentAdjustments.contrast = 0.5f;
            overlayManager.CurrentAdjustments.UpdateModifiedState();
            
            // Act
            overlayManager.ResetToOriginal();
            
            // Assert
            Assert.AreEqual(0f, overlayManager.CurrentAdjustments.contrast);
            Assert.IsFalse(overlayManager.CurrentAdjustments.isModified);
        }
    }
}