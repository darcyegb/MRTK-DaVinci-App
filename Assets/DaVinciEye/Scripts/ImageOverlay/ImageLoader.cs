using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Handles loading images from various sources with format validation and memory management
    /// </summary>
    public static class ImageLoader
    {
        private const int MAX_TEXTURE_SIZE = 2048;
        private static readonly string[] SUPPORTED_EXTENSIONS = { ".jpg", ".jpeg", ".png", ".bmp" };
        
        /// <summary>
        /// Loads an image asynchronously from the specified file path
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <returns>Loaded texture or null if loading failed</returns>
        public static async Task<Texture2D> LoadImageAsync(string imagePath)
        {
            try
            {
                // Validate file path
                if (string.IsNullOrEmpty(imagePath))
                {
                    Debug.LogError("ImageLoader: Image path is null or empty");
                    return null;
                }
                
                // Check if file exists
                if (!File.Exists(imagePath))
                {
                    Debug.LogError($"ImageLoader: File not found at path: {imagePath}");
                    return null;
                }
                
                // Validate file extension
                if (!IsValidImageFormat(imagePath))
                {
                    Debug.LogError($"ImageLoader: Unsupported image format: {Path.GetExtension(imagePath)}");
                    return null;
                }
                
                // Load image data
                byte[] imageData = await LoadImageDataAsync(imagePath);
                if (imageData == null || imageData.Length == 0)
                {
                    Debug.LogError($"ImageLoader: Failed to load image data from: {imagePath}");
                    return null;
                }
                
                // Create texture from image data
                Texture2D texture = await CreateTextureFromDataAsync(imageData);
                if (texture == null)
                {
                    Debug.LogError($"ImageLoader: Failed to create texture from image data: {imagePath}");
                    return null;
                }
                
                // Apply size constraints
                texture = ResizeTextureIfNeeded(texture);
                
                Debug.Log($"ImageLoader: Successfully loaded image {Path.GetFileName(imagePath)} - {texture.width}x{texture.height}");
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ImageLoader: Exception loading image {imagePath}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Loads image data from file asynchronously
        /// </summary>
        private static async Task<byte[]> LoadImageDataAsync(string imagePath)
        {
            try
            {
                // Use UnityWebRequest for cross-platform file loading
                string fileUri = "file://" + imagePath;
                using (UnityWebRequest request = UnityWebRequest.Get(fileUri))
                {
                    var operation = request.SendWebRequest();
                    
                    // Wait for completion
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"ImageLoader: UnityWebRequest failed: {request.error}");
                        return null;
                    }
                    
                    return request.downloadHandler.data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ImageLoader: Exception loading image data: {ex.Message}");
                
                // Fallback to System.IO.File for local files
                try
                {
                    return await Task.Run(() => File.ReadAllBytes(imagePath));
                }
                catch (Exception fallbackEx)
                {
                    Debug.LogError($"ImageLoader: Fallback loading also failed: {fallbackEx.Message}");
                    return null;
                }
            }
        }
        
        /// <summary>
        /// Creates a texture from image data asynchronously
        /// </summary>
        private static async Task<Texture2D> CreateTextureFromDataAsync(byte[] imageData)
        {
            try
            {
                // Create temporary texture for loading
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                
                // Use ImageConversion for automatic format detection
                await Task.Run(() =>
                {
                    if (!ImageConversion.LoadImage(texture, imageData))
                    {
                        throw new Exception("ImageConversion.LoadImage failed");
                    }
                });
                
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ImageLoader: Exception creating texture: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Resizes texture if it exceeds maximum size constraints
        /// </summary>
        private static Texture2D ResizeTextureIfNeeded(Texture2D originalTexture)
        {
            if (originalTexture.width <= MAX_TEXTURE_SIZE && originalTexture.height <= MAX_TEXTURE_SIZE)
            {
                return originalTexture;
            }
            
            // Calculate new dimensions maintaining aspect ratio
            float aspectRatio = (float)originalTexture.width / originalTexture.height;
            int newWidth, newHeight;
            
            if (originalTexture.width > originalTexture.height)
            {
                newWidth = MAX_TEXTURE_SIZE;
                newHeight = Mathf.RoundToInt(MAX_TEXTURE_SIZE / aspectRatio);
            }
            else
            {
                newHeight = MAX_TEXTURE_SIZE;
                newWidth = Mathf.RoundToInt(MAX_TEXTURE_SIZE * aspectRatio);
            }
            
            // Create resized texture
            Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            
            // Use Graphics.CopyTexture for efficient resizing
            RenderTexture renderTexture = RenderTexture.GetTemporary(newWidth, newHeight);
            Graphics.Blit(originalTexture, renderTexture);
            
            RenderTexture.active = renderTexture;
            resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            resizedTexture.Apply();
            RenderTexture.active = null;
            
            RenderTexture.ReleaseTemporary(renderTexture);
            
            // Clean up original texture
            UnityEngine.Object.DestroyImmediate(originalTexture);
            
            Debug.Log($"ImageLoader: Resized texture to {newWidth}x{newHeight} (max size: {MAX_TEXTURE_SIZE})");
            return resizedTexture;
        }
        
        /// <summary>
        /// Validates if the file has a supported image format
        /// </summary>
        private static bool IsValidImageFormat(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            foreach (string supportedExt in SUPPORTED_EXTENSIONS)
            {
                if (extension == supportedExt)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Creates a default placeholder texture for fallback scenarios
        /// </summary>
        public static Texture2D CreatePlaceholderTexture(int width = 512, int height = 512)
        {
            Texture2D placeholder = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            // Create a simple checkerboard pattern
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isWhite = ((x / 32) + (y / 32)) % 2 == 0;
                    pixels[y * width + x] = isWhite ? Color.white : Color.gray;
                }
            }
            
            placeholder.SetPixels(pixels);
            placeholder.Apply();
            
            return placeholder;
        }
        
        /// <summary>
        /// Gets supported image format extensions
        /// </summary>
        public static string[] GetSupportedExtensions()
        {
            return (string[])SUPPORTED_EXTENSIONS.Clone();
        }
    }
}