using System;
using UnityEngine;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Processes image adjustments including cropping, contrast, exposure, hue, and saturation
    /// Provides real-time image processing capabilities
    /// </summary>
    public class ImageAdjustmentProcessor : MonoBehaviour
    {
        [Header("Processing Settings")]
        [SerializeField] private bool enableRealTimeProcessing = true;
        [SerializeField] private bool enableGPUProcessing = true;
        [SerializeField] private int maxTextureSize = 2048;
        
        [Header("Quality Settings")]
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
        [SerializeField] private TextureFormat textureFormat = TextureFormat.RGBA32;
        
        // Private fields
        private Texture2D originalTexture;
        private Texture2D processedTexture;
        private ImageAdjustments currentAdjustments;
        private bool isProcessing = false;
        
        // Shader for GPU processing
        private Material processingMaterial;
        private RenderTexture processingRenderTexture;
        
        // Events
        public event Action<Texture2D> OnImageProcessed;
        public event Action<float> OnProcessingProgress;
        
        // Properties
        public Texture2D ProcessedTexture => processedTexture;
        public bool IsProcessing => isProcessing;
        public ImageAdjustments CurrentAdjustments => currentAdjustments;
        
        private void Awake()
        {
            Initialize();
        }
        
        /// <summary>
        /// Initializes the image adjustment processor
        /// </summary>
        private void Initialize()
        {
            currentAdjustments = new ImageAdjustments();
            
            if (enableGPUProcessing)
            {
                SetupGPUProcessing();
            }
            
            Debug.Log("ImageAdjustmentProcessor: Initialized");
        }
        
        /// <summary>
        /// Sets up GPU processing materials and render textures
        /// </summary>
        private void SetupGPUProcessing()
        {
            // Create processing material with a simple shader
            Shader processingShader = Shader.Find("Hidden/ImageAdjustment");
            if (processingShader == null)
            {
                // Fallback to unlit shader
                processingShader = Shader.Find("Unlit/Texture");
            }
            
            if (processingShader != null)
            {
                processingMaterial = new Material(processingShader);
                Debug.Log("ImageAdjustmentProcessor: GPU processing material created");
            }
            else
            {
                Debug.LogWarning("ImageAdjustmentProcessor: No suitable shader found, falling back to CPU processing");
                enableGPUProcessing = false;
            }
        }
        
        /// <summary>
        /// Sets the original texture to process
        /// </summary>
        public void SetOriginalTexture(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogWarning("ImageAdjustmentProcessor: Cannot set null texture");
                return;
            }
            
            originalTexture = texture;
            
            // Clean up previous processed texture
            if (processedTexture != null && processedTexture != originalTexture)
            {
                DestroyImmediate(processedTexture);
            }
            
            // Create initial processed texture copy
            processedTexture = DuplicateTexture(originalTexture);
            
            Debug.Log($"ImageAdjustmentProcessor: Set original texture {texture.width}x{texture.height}");
        }
        
        /// <summary>
        /// Applies image adjustments to the texture
        /// </summary>
        public void ApplyAdjustments(ImageAdjustments adjustments)
        {
            if (originalTexture == null)
            {
                Debug.LogWarning("ImageAdjustmentProcessor: No original texture set");
                return;
            }
            
            if (adjustments == null)
            {
                Debug.LogWarning("ImageAdjustmentProcessor: Adjustments cannot be null");
                return;
            }
            
            currentAdjustments = adjustments;
            
            if (enableRealTimeProcessing)
            {
                ProcessImageAsync();
            }
        }
        
        /// <summary>
        /// Processes the image with current adjustments
        /// </summary>
        public void ProcessImage()
        {
            ProcessImageAsync();
        }
        
        /// <summary>
        /// Processes the image asynchronously
        /// </summary>
        private async void ProcessImageAsync()
        {
            if (isProcessing || originalTexture == null)
            {
                return;
            }
            
            isProcessing = true;
            OnProcessingProgress?.Invoke(0f);
            
            try
            {
                Texture2D result;
                
                if (enableGPUProcessing && processingMaterial != null)
                {
                    result = await ProcessImageGPU();
                }
                else
                {
                    result = await ProcessImageCPU();
                }
                
                if (result != null)
                {
                    // Clean up previous processed texture
                    if (processedTexture != null && processedTexture != originalTexture)
                    {
                        DestroyImmediate(processedTexture);
                    }
                    
                    processedTexture = result;
                    OnImageProcessed?.Invoke(processedTexture);
                }
                
                OnProcessingProgress?.Invoke(1f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ImageAdjustmentProcessor: Processing failed: {ex.Message}");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Processes image using GPU (faster for real-time adjustments)
        /// </summary>
        private async System.Threading.Tasks.Task<Texture2D> ProcessImageGPU()
        {
            // For now, implement basic GPU processing
            // This would be expanded with custom shaders for full adjustment support
            
            Texture2D result = DuplicateTexture(originalTexture);
            
            // Apply cropping first
            if (currentAdjustments.isCropped)
            {
                result = ApplyCropping(result, currentAdjustments.cropArea);
            }
            
            // Apply color adjustments using material properties
            if (processingMaterial != null)
            {
                // Set material properties for adjustments
                if (processingMaterial.HasProperty("_Contrast"))
                {
                    processingMaterial.SetFloat("_Contrast", currentAdjustments.contrast);
                }
                if (processingMaterial.HasProperty("_Exposure"))
                {
                    processingMaterial.SetFloat("_Exposure", currentAdjustments.exposure);
                }
                if (processingMaterial.HasProperty("_Hue"))
                {
                    processingMaterial.SetFloat("_Hue", currentAdjustments.hue);
                }
                if (processingMaterial.HasProperty("_Saturation"))
                {
                    processingMaterial.SetFloat("_Saturation", currentAdjustments.saturation);
                }
            }
            
            await System.Threading.Tasks.Task.Yield(); // Simulate async processing
            
            return result;
        }
        
        /// <summary>
        /// Processes image using CPU (more compatible but slower)
        /// </summary>
        private async System.Threading.Tasks.Task<Texture2D> ProcessImageCPU()
        {
            Texture2D result = DuplicateTexture(originalTexture);
            
            // Apply cropping first
            if (currentAdjustments.isCropped)
            {
                OnProcessingProgress?.Invoke(0.2f);
                result = ApplyCropping(result, currentAdjustments.cropArea);
            }
            
            // Apply color adjustments
            if (HasColorAdjustments())
            {
                OnProcessingProgress?.Invoke(0.5f);
                result = ApplyColorAdjustments(result);
            }
            
            await System.Threading.Tasks.Task.Yield(); // Allow other operations
            
            return result;
        }
        
        /// <summary>
        /// Applies cropping to the texture
        /// </summary>
        private Texture2D ApplyCropping(Texture2D source, Rect cropArea)
        {
            if (source == null || cropArea.width <= 0 || cropArea.height <= 0)
            {
                return source;
            }
            
            // Calculate pixel coordinates
            int startX = Mathf.RoundToInt(cropArea.x * source.width);
            int startY = Mathf.RoundToInt(cropArea.y * source.height);
            int width = Mathf.RoundToInt(cropArea.width * source.width);
            int height = Mathf.RoundToInt(cropArea.height * source.height);
            
            // Clamp to texture bounds
            startX = Mathf.Clamp(startX, 0, source.width - 1);
            startY = Mathf.Clamp(startY, 0, source.height - 1);
            width = Mathf.Clamp(width, 1, source.width - startX);
            height = Mathf.Clamp(height, 1, source.height - startY);
            
            // Create cropped texture
            Texture2D croppedTexture = new Texture2D(width, height, source.format, false);
            
            // Copy pixels
            Color[] sourcePixels = source.GetPixels(startX, startY, width, height);
            croppedTexture.SetPixels(sourcePixels);
            croppedTexture.Apply();
            
            // Clean up source if it's not the original
            if (source != originalTexture)
            {
                DestroyImmediate(source);
            }
            
            Debug.Log($"ImageAdjustmentProcessor: Applied cropping {width}x{height} from {startX},{startY}");
            
            return croppedTexture;
        }
        
        /// <summary>
        /// Applies color adjustments to the texture
        /// </summary>
        private Texture2D ApplyColorAdjustments(Texture2D source)
        {
            if (source == null || !HasColorAdjustments())
            {
                return source;
            }
            
            Texture2D result = DuplicateTexture(source);
            Color[] pixels = result.GetPixels();
            
            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                
                // Apply exposure adjustment
                if (Mathf.Abs(currentAdjustments.exposure) > 0.01f)
                {
                    float exposureFactor = Mathf.Pow(2f, currentAdjustments.exposure);
                    pixel.r *= exposureFactor;
                    pixel.g *= exposureFactor;
                    pixel.b *= exposureFactor;
                }
                
                // Apply contrast adjustment
                if (Mathf.Abs(currentAdjustments.contrast) > 0.01f)
                {
                    float contrastFactor = 1f + currentAdjustments.contrast;
                    pixel.r = ((pixel.r - 0.5f) * contrastFactor) + 0.5f;
                    pixel.g = ((pixel.g - 0.5f) * contrastFactor) + 0.5f;
                    pixel.b = ((pixel.b - 0.5f) * contrastFactor) + 0.5f;
                }
                
                // Apply hue and saturation adjustments
                if (Mathf.Abs(currentAdjustments.hue) > 0.01f || Mathf.Abs(currentAdjustments.saturation) > 0.01f)
                {
                    pixel = ApplyHueSaturation(pixel, currentAdjustments.hue, currentAdjustments.saturation);
                }
                
                // Clamp values
                pixel.r = Mathf.Clamp01(pixel.r);
                pixel.g = Mathf.Clamp01(pixel.g);
                pixel.b = Mathf.Clamp01(pixel.b);
                
                pixels[i] = pixel;
            }
            
            result.SetPixels(pixels);
            result.Apply();
            
            // Clean up source if it's not the original
            if (source != originalTexture && source != result)
            {
                DestroyImmediate(source);
            }
            
            Debug.Log("ImageAdjustmentProcessor: Applied color adjustments");
            
            return result;
        }
        
        /// <summary>
        /// Applies hue and saturation adjustments to a color
        /// </summary>
        private Color ApplyHueSaturation(Color color, float hueShift, float saturationAdjust)
        {
            // Convert RGB to HSV
            Color.RGBToHSV(color, out float h, out float s, out float v);
            
            // Apply hue shift
            if (Mathf.Abs(hueShift) > 0.01f)
            {
                h += hueShift / 360f; // Convert degrees to 0-1 range
                h = h - Mathf.Floor(h); // Wrap around
            }
            
            // Apply saturation adjustment
            if (Mathf.Abs(saturationAdjust) > 0.01f)
            {
                s = Mathf.Clamp01(s + saturationAdjust);
            }
            
            // Convert back to RGB
            return Color.HSVToRGB(h, s, v);
        }
        
        /// <summary>
        /// Checks if any color adjustments are applied
        /// </summary>
        private bool HasColorAdjustments()
        {
            return Mathf.Abs(currentAdjustments.contrast) > 0.01f ||
                   Mathf.Abs(currentAdjustments.exposure) > 0.01f ||
                   Mathf.Abs(currentAdjustments.hue) > 0.01f ||
                   Mathf.Abs(currentAdjustments.saturation) > 0.01f;
        }
        
        /// <summary>
        /// Creates a duplicate of a texture
        /// </summary>
        private Texture2D DuplicateTexture(Texture2D source)
        {
            if (source == null) return null;
            
            // Limit texture size for performance
            int width = Mathf.Min(source.width, maxTextureSize);
            int height = Mathf.Min(source.height, maxTextureSize);
            
            Texture2D duplicate = new Texture2D(width, height, textureFormat, false);
            duplicate.filterMode = filterMode;
            
            // Scale if necessary
            if (width != source.width || height != source.height)
            {
                // Use Graphics.CopyTexture for better performance if possible
                RenderTexture rt = RenderTexture.GetTemporary(width, height);
                Graphics.Blit(source, rt);
                
                RenderTexture.active = rt;
                duplicate.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                duplicate.Apply();
                RenderTexture.active = null;
                
                RenderTexture.ReleaseTemporary(rt);
            }
            else
            {
                duplicate.SetPixels(source.GetPixels());
                duplicate.Apply();
            }
            
            return duplicate;
        }
        
        /// <summary>
        /// Resets all adjustments to default values
        /// </summary>
        public void ResetAdjustments()
        {
            currentAdjustments.Reset();
            
            if (originalTexture != null)
            {
                ProcessImageAsync();
            }
        }
        
        /// <summary>
        /// Gets the current crop area in pixel coordinates
        /// </summary>
        public Rect GetCropAreaPixels()
        {
            if (originalTexture == null)
            {
                return new Rect(0, 0, 0, 0);
            }
            
            Rect cropArea = currentAdjustments.cropArea;
            return new Rect(
                cropArea.x * originalTexture.width,
                cropArea.y * originalTexture.height,
                cropArea.width * originalTexture.width,
                cropArea.height * originalTexture.height
            );
        }
        
        private void OnDestroy()
        {
            // Clean up textures
            if (processedTexture != null && processedTexture != originalTexture)
            {
                DestroyImmediate(processedTexture);
            }
            
            // Clean up render texture
            if (processingRenderTexture != null)
            {
                processingRenderTexture.Release();
                DestroyImmediate(processingRenderTexture);
            }
            
            // Clean up material
            if (processingMaterial != null)
            {
                DestroyImmediate(processingMaterial);
            }
        }
    }
}