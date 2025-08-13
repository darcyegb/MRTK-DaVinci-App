using System;
using System.Threading.Tasks;
using UnityEngine;
using DaVinciEye.Core;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Manages image overlay functionality including loading, display, and basic adjustments
    /// </summary>
    public class ImageOverlayManager : MonoBehaviour, IImageOverlay
    {
        [Header("Display Settings")]
        [SerializeField] private Material overlayMaterial;
        [SerializeField] private Renderer overlayRenderer;
        [SerializeField] private Transform overlayTransform;
        
        [Header("Opacity Control")]
        [SerializeField] private OpacityController opacityController;
        
        [Header("Scaling and Alignment")]
        [SerializeField] private ImageScalingAlignment scalingAlignment;
        
        [Header("Image Adjustments")]
        [SerializeField] private CropController cropController;
        [SerializeField] private ImageAdjustmentProcessor adjustmentProcessor;
        
        [Header("Configuration")]
        [SerializeField] private bool autoCreateOverlayQuad = true;
        [SerializeField] private string overlayShaderName = "Universal Render Pipeline/Lit";
        
        // Private fields
        private Texture2D currentImage;
        private Texture2D originalImage;
        private float opacity = 1.0f;
        private bool isVisible = true;
        private ImageAdjustments currentAdjustments;
        private SessionData sessionData;
        private bool isInitialized = false;
        
        // Events
        public event Action<Texture2D> OnImageLoaded;
        public event Action<float> OnOpacityChanged;
        public event Action<ImageAdjustments> OnAdjustmentsApplied;
        
        // Properties
        public Texture2D CurrentImage => currentImage;
        public float Opacity 
        { 
            get => opacity; 
            set => SetOpacity(value); 
        }
        public bool IsVisible 
        { 
            get => isVisible; 
            set => SetVisibility(value); 
        }
        public ImageAdjustments CurrentAdjustments => currentAdjustments;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            LoadSessionData();
        }
        
        /// <summary>
        /// Initializes the image overlay system
        /// </summary>
        private void Initialize()
        {
            try
            {
                // Initialize adjustments
                currentAdjustments = new ImageAdjustments();
                
                // Load session data
                sessionData = SessionData.Load();
                
                // Create overlay quad if needed
                if (autoCreateOverlayQuad && overlayRenderer == null)
                {
                    CreateOverlayQuad();
                }
                
                // Setup material
                if (overlayMaterial == null && overlayRenderer != null)
                {
                    CreateOverlayMaterial();
                }
                
                // Setup opacity controller
                SetupOpacityController();
                
                // Setup scaling and alignment
                SetupScalingAlignment();
                
                // Setup crop controller
                SetupCropController();
                
                // Setup adjustment processor
                SetupAdjustmentProcessor();
                
                isInitialized = true;
                Debug.Log("ImageOverlayManager: Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"ImageOverlayManager: Initialization failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a quad for displaying the overlay image
        /// </summary>
        private void CreateOverlayQuad()
        {
            GameObject overlayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlayQuad.name = "ImageOverlayQuad";
            overlayQuad.transform.SetParent(transform);
            overlayQuad.transform.localPosition = Vector3.zero;
            overlayQuad.transform.localRotation = Quaternion.identity;
            overlayQuad.transform.localScale = Vector3.one;
            
            overlayRenderer = overlayQuad.GetComponent<Renderer>();
            overlayTransform = overlayQuad.transform;
            
            // Remove collider as we don't need physics
            Collider collider = overlayQuad.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
            
            Debug.Log("ImageOverlayManager: Created overlay quad");
        }
        
        /// <summary>
        /// Creates the material for the overlay
        /// </summary>
        private void CreateOverlayMaterial()
        {
            Shader overlayShader = Shader.Find(overlayShaderName);
            if (overlayShader == null)
            {
                Debug.LogWarning($"ImageOverlayManager: Shader '{overlayShaderName}' not found, using default");
                overlayShader = Shader.Find("Standard");
            }
            
            overlayMaterial = new Material(overlayShader);
            overlayMaterial.name = "ImageOverlayMaterial";
            
            // Configure material for transparency
            if (overlayMaterial.HasProperty("_Mode"))
            {
                overlayMaterial.SetFloat("_Mode", 3); // Transparent mode
            }
            if (overlayMaterial.HasProperty("_SrcBlend"))
            {
                overlayMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }
            if (overlayMaterial.HasProperty("_DstBlend"))
            {
                overlayMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (overlayMaterial.HasProperty("_ZWrite"))
            {
                overlayMaterial.SetFloat("_ZWrite", 0);
            }
            
            overlayRenderer.material = overlayMaterial;
            
            Debug.Log("ImageOverlayManager: Created overlay material");
        }
        
        /// <summary>
        /// Sets up the opacity controller integration
        /// </summary>
        private void SetupOpacityController()
        {
            // Find opacity controller if not assigned
            if (opacityController == null)
            {
                opacityController = GetComponentInChildren<OpacityController>();
                if (opacityController == null)
                {
                    opacityController = FindObjectOfType<OpacityController>();
                }
            }
            
            // Configure opacity controller
            if (opacityController != null)
            {
                opacityController.SetImageOverlay(this);
                opacityController.SetTargetMaterial(overlayMaterial);
                opacityController.SetTargetRenderer(overlayRenderer);
                
                // Subscribe to opacity changes
                opacityController.OnOpacityChanged += OnOpacityControllerChanged;
                
                Debug.Log("ImageOverlayManager: Opacity controller configured");
            }
            else
            {
                Debug.LogWarning("ImageOverlayManager: No opacity controller found");
            }
        }
        
        /// <summary>
        /// Handles opacity changes from the opacity controller
        /// </summary>
        private void OnOpacityControllerChanged(float newOpacity)
        {
            // Update internal opacity without triggering controller again
            opacity = newOpacity;
            
            // Update session data
            if (sessionData != null)
            {
                sessionData.currentOpacity = opacity;
                sessionData.Save();
            }
            
            // Fire event
            OnOpacityChanged?.Invoke(opacity);
        }
        
        /// <summary>
        /// Sets up the scaling and alignment system integration
        /// </summary>
        private void SetupScalingAlignment()
        {
            // Find scaling alignment component if not assigned
            if (scalingAlignment == null)
            {
                scalingAlignment = GetComponent<ImageScalingAlignment>();
                if (scalingAlignment == null)
                {
                    scalingAlignment = GetComponentInChildren<ImageScalingAlignment>();
                }
                if (scalingAlignment == null)
                {
                    // Create scaling alignment component
                    scalingAlignment = gameObject.AddComponent<ImageScalingAlignment>();
                }
            }
            
            // Configure scaling alignment if available
            if (scalingAlignment != null)
            {
                // Subscribe to scaling events
                scalingAlignment.OnScaleChanged += OnImageScaleChanged;
                scalingAlignment.OnPositionChanged += OnImagePositionChanged;
                scalingAlignment.OnAlignmentUpdated += OnImageAlignmentUpdated;
                
                Debug.Log("ImageOverlayManager: Scaling and alignment configured");
            }
            else
            {
                Debug.LogWarning("ImageOverlayManager: No scaling alignment component found");
            }
        }
        
        /// <summary>
        /// Handles image scale changes from the scaling alignment system
        /// </summary>
        private void OnImageScaleChanged(Vector3 newScale)
        {
            Debug.Log($"ImageOverlayManager: Image scale changed to {newScale}");
        }
        
        /// <summary>
        /// Handles image position changes from the scaling alignment system
        /// </summary>
        private void OnImagePositionChanged(Vector3 newPosition)
        {
            Debug.Log($"ImageOverlayManager: Image position changed to {newPosition}");
        }
        
        /// <summary>
        /// Handles alignment updates from the scaling alignment system
        /// </summary>
        private void OnImageAlignmentUpdated(Vector2 imageSize, Vector2 canvasSize)
        {
            Debug.Log($"ImageOverlayManager: Alignment updated - Image: {imageSize}, Canvas: {canvasSize}");
            
            // Update scaling alignment when image is loaded
            if (scalingAlignment != null)
            {
                scalingAlignment.UpdateAlignment();
            }
        }
        
        /// <summary>
        /// Loads session data and restores previous state
        /// </summary>
        private void LoadSessionData()
        {
            if (sessionData != null && !string.IsNullOrEmpty(sessionData.currentImagePath))
            {
                // Attempt to reload the previous image
                _ = LoadImageAsync(sessionData.currentImagePath);
                SetOpacity(sessionData.currentOpacity);
                SetVisibility(sessionData.isImageVisible);
            }
        }
        
        /// <summary>
        /// Loads an image asynchronously from the specified path
        /// </summary>
        public async Task<bool> LoadImageAsync(string imagePath)
        {
            if (!isInitialized)
            {
                Debug.LogError("ImageOverlayManager: Not initialized");
                return false;
            }
            
            try
            {
                Debug.Log($"ImageOverlayManager: Loading image from {imagePath}");
                
                // Load image using ImageLoader
                Texture2D loadedTexture = await ImageLoader.LoadImageAsync(imagePath);
                
                if (loadedTexture == null)
                {
                    Debug.LogError($"ImageOverlayManager: Failed to load image from {imagePath}");
                    
                    // Use placeholder texture as fallback
                    loadedTexture = ImageLoader.CreatePlaceholderTexture();
                    Debug.Log("ImageOverlayManager: Using placeholder texture as fallback");
                }
                
                // Store original and current textures
                if (originalImage != null)
                {
                    DestroyImmediate(originalImage);
                }
                if (currentImage != null && currentImage != originalImage)
                {
                    DestroyImmediate(currentImage);
                }
                
                originalImage = loadedTexture;
                currentImage = DuplicateTexture(originalImage);
                
                // Apply to material
                if (overlayMaterial != null)
                {
                    overlayMaterial.mainTexture = currentImage;
                }
                
                // Update session data
                if (sessionData != null)
                {
                    sessionData.currentImagePath = imagePath;
                    sessionData.isImageVisible = true;
                    sessionData.Save();
                }
                
                // Reset adjustments
                currentAdjustments.Reset();
                
                // Update scaling and alignment for new image
                if (scalingAlignment != null)
                {
                    scalingAlignment.UpdateAlignment();
                }
                
                // Set up adjustment processor with new image
                if (adjustmentProcessor != null)
                {
                    adjustmentProcessor.SetOriginalTexture(originalImage);
                }
                
                // Fire event
                OnImageLoaded?.Invoke(currentImage);
                
                Debug.Log($"ImageOverlayManager: Successfully loaded image {System.IO.Path.GetFileName(imagePath)}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ImageOverlayManager: Exception loading image: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets the opacity of the overlay
        /// </summary>
        public void SetOpacity(float newOpacity)
        {
            opacity = Mathf.Clamp01(newOpacity);
            
            if (overlayMaterial != null)
            {
                Color color = overlayMaterial.color;
                color.a = opacity;
                overlayMaterial.color = color;
                
                // Also set alpha property if available
                if (overlayMaterial.HasProperty("_Alpha"))
                {
                    overlayMaterial.SetFloat("_Alpha", opacity);
                }
            }
            
            // Update opacity controller if available
            if (opacityController != null)
            {
                opacityController.SetOpacity(opacity);
            }
            
            // Update session data
            if (sessionData != null)
            {
                sessionData.currentOpacity = opacity;
                sessionData.Save();
            }
            
            OnOpacityChanged?.Invoke(opacity);
        }
        
        /// <summary>
        /// Sets the visibility of the overlay
        /// </summary>
        public void SetVisibility(bool visible)
        {
            isVisible = visible;
            
            if (overlayRenderer != null)
            {
                overlayRenderer.enabled = visible;
            }
            
            // Update session data
            if (sessionData != null)
            {
                sessionData.isImageVisible = visible;
                sessionData.Save();
            }
        }
        
        /// <summary>
        /// Applies image adjustments using the adjustment processor
        /// </summary>
        public void ApplyAdjustments(ImageAdjustments adjustments)
        {
            if (adjustments == null || originalImage == null)
            {
                return;
            }
            
            currentAdjustments = adjustments;
            currentAdjustments.UpdateModifiedState();
            
            // Use adjustment processor if available
            if (adjustmentProcessor != null)
            {
                adjustmentProcessor.ApplyAdjustments(currentAdjustments);
            }
            else
            {
                // Fallback: just copy the original image
                if (currentImage != null && currentImage != originalImage)
                {
                    DestroyImmediate(currentImage);
                }
                
                currentImage = DuplicateTexture(originalImage);
                
                if (overlayMaterial != null)
                {
                    overlayMaterial.mainTexture = currentImage;
                }
            }
            
            OnAdjustmentsApplied?.Invoke(currentAdjustments);
        }
        
        /// <summary>
        /// Sets the crop area and applies it to the image
        /// </summary>
        public void SetCropArea(Rect cropRect)
        {
            currentAdjustments.cropArea = cropRect;
            currentAdjustments.isCropped = cropRect != new Rect(0, 0, 1, 1);
            currentAdjustments.UpdateModifiedState();
            
            // Apply adjustments
            ApplyAdjustments(currentAdjustments);
        }
        
        /// <summary>
        /// Resets the image to its original state
        /// </summary>
        public void ResetToOriginal()
        {
            if (originalImage == null)
            {
                return;
            }
            
            currentAdjustments.Reset();
            
            if (currentImage != null && currentImage != originalImage)
            {
                DestroyImmediate(currentImage);
            }
            
            currentImage = DuplicateTexture(originalImage);
            
            if (overlayMaterial != null)
            {
                overlayMaterial.mainTexture = currentImage;
            }
            
            OnAdjustmentsApplied?.Invoke(currentAdjustments);
        }
        
        /// <summary>
        /// Sets up the crop controller integration
        /// </summary>
        private void SetupCropController()
        {
            // Find crop controller if not assigned
            if (cropController == null)
            {
                cropController = GetComponent<CropController>();
                if (cropController == null)
                {
                    cropController = GetComponentInChildren<CropController>();
                }
                if (cropController == null)
                {
                    // Create crop controller component
                    cropController = gameObject.AddComponent<CropController>();
                }
            }
            
            // Configure crop controller if available
            if (cropController != null)
            {
                // Subscribe to crop events
                cropController.OnCropAreaApplied += OnCropAreaApplied;
                cropController.OnCropAreaChanged += OnCropAreaChanged;
                
                Debug.Log("ImageOverlayManager: Crop controller configured");
            }
            else
            {
                Debug.LogWarning("ImageOverlayManager: No crop controller found");
            }
        }
        
        /// <summary>
        /// Sets up the image adjustment processor integration
        /// </summary>
        private void SetupAdjustmentProcessor()
        {
            // Find adjustment processor if not assigned
            if (adjustmentProcessor == null)
            {
                adjustmentProcessor = GetComponent<ImageAdjustmentProcessor>();
                if (adjustmentProcessor == null)
                {
                    adjustmentProcessor = GetComponentInChildren<ImageAdjustmentProcessor>();
                }
                if (adjustmentProcessor == null)
                {
                    // Create adjustment processor component
                    adjustmentProcessor = gameObject.AddComponent<ImageAdjustmentProcessor>();
                }
            }
            
            // Configure adjustment processor if available
            if (adjustmentProcessor != null)
            {
                // Subscribe to processing events
                adjustmentProcessor.OnImageProcessed += OnImageProcessed;
                adjustmentProcessor.OnProcessingProgress += OnProcessingProgress;
                
                Debug.Log("ImageOverlayManager: Adjustment processor configured");
            }
            else
            {
                Debug.LogWarning("ImageOverlayManager: No adjustment processor found");
            }
        }
        
        /// <summary>
        /// Handles crop area applied events from the crop controller
        /// </summary>
        private void OnCropAreaApplied(Rect cropArea)
        {
            Debug.Log($"ImageOverlayManager: Crop area applied: {cropArea}");
            SetCropArea(cropArea);
        }
        
        /// <summary>
        /// Handles crop area changed events from the crop controller
        /// </summary>
        private void OnCropAreaChanged(Rect cropArea)
        {
            Debug.Log($"ImageOverlayManager: Crop area changed: {cropArea}");
            // Update preview if needed
        }
        
        /// <summary>
        /// Handles image processed events from the adjustment processor
        /// </summary>
        private void OnImageProcessed(Texture2D processedTexture)
        {
            if (processedTexture != null)
            {
                // Clean up previous current image if it's different from original
                if (currentImage != null && currentImage != originalImage)
                {
                    DestroyImmediate(currentImage);
                }
                
                currentImage = processedTexture;
                
                // Apply to material
                if (overlayMaterial != null)
                {
                    overlayMaterial.mainTexture = currentImage;
                }
                
                Debug.Log("ImageOverlayManager: Applied processed image");
            }
        }
        
        /// <summary>
        /// Handles processing progress events from the adjustment processor
        /// </summary>
        private void OnProcessingProgress(float progress)
        {
            Debug.Log($"ImageOverlayManager: Processing progress: {progress:P0}");
        }
        
        /// <summary>
        /// Creates a duplicate of a texture
        /// </summary>
        private Texture2D DuplicateTexture(Texture2D source)
        {
            if (source == null) return null;
            
            Texture2D duplicate = new Texture2D(source.width, source.height, source.format, false);
            duplicate.SetPixels(source.GetPixels());
            duplicate.Apply();
            
            return duplicate;
        }
        
        private void OnDestroy()
        {
            // Clean up opacity controller events
            if (opacityController != null)
            {
                opacityController.OnOpacityChanged -= OnOpacityControllerChanged;
            }
            
            // Clean up scaling alignment events
            if (scalingAlignment != null)
            {
                scalingAlignment.OnScaleChanged -= OnImageScaleChanged;
                scalingAlignment.OnPositionChanged -= OnImagePositionChanged;
                scalingAlignment.OnAlignmentUpdated -= OnImageAlignmentUpdated;
            }
            
            // Clean up crop controller events
            if (cropController != null)
            {
                cropController.OnCropAreaApplied -= OnCropAreaApplied;
                cropController.OnCropAreaChanged -= OnCropAreaChanged;
            }
            
            // Clean up adjustment processor events
            if (adjustmentProcessor != null)
            {
                adjustmentProcessor.OnImageProcessed -= OnImageProcessed;
                adjustmentProcessor.OnProcessingProgress -= OnProcessingProgress;
            }
            
            // Clean up textures
            if (currentImage != null && currentImage != originalImage)
            {
                DestroyImmediate(currentImage);
            }
            if (originalImage != null)
            {
                DestroyImmediate(originalImage);
            }
            
            // Clean up material
            if (overlayMaterial != null)
            {
                DestroyImmediate(overlayMaterial);
            }
        }
    }
}