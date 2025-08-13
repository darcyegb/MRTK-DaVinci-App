using System;
using UnityEngine;
using DaVinciEye.Canvas;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Handles automatic image scaling and alignment relative to physical canvas boundaries
    /// </summary>
    public class ImageScalingAlignment : MonoBehaviour
    {
        [Header("Canvas References")]
        [SerializeField] private ICanvasManager canvasManager;
        [SerializeField] private Transform canvasTransform;
        
        [Header("Image References")]
        [SerializeField] private Transform imageTransform;
        [SerializeField] private Renderer imageRenderer;
        
        [Header("Scaling Configuration")]
        [SerializeField] private ScalingMode scalingMode = ScalingMode.FitToCanvas;
        [SerializeField] private bool maintainAspectRatio = true;
        [SerializeField] private float paddingPercent = 0.05f; // 5% padding by default
        [SerializeField] private Vector2 minScale = new Vector2(0.1f, 0.1f);
        [SerializeField] private Vector2 maxScale = new Vector2(10f, 10f);
        
        [Header("Alignment Configuration")]
        [SerializeField] private AlignmentMode alignmentMode = AlignmentMode.Center;
        [SerializeField] private Vector2 alignmentOffset = Vector2.zero;
        [SerializeField] private bool autoUpdateAlignment = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Private fields
        private Vector2 currentImageSize;
        private Vector2 currentCanvasSize;
        private Vector3 currentScale;
        private Vector3 currentPosition;
        private bool isInitialized = false;
        
        // Events
        public event Action<Vector3> OnScaleChanged;
        public event Action<Vector3> OnPositionChanged;
        public event Action<Vector2, Vector2> OnAlignmentUpdated; // imageSize, canvasSize
        
        // Properties
        public ScalingMode CurrentScalingMode => scalingMode;
        public AlignmentMode CurrentAlignmentMode => alignmentMode;
        public Vector3 CurrentScale => currentScale;
        public Vector3 CurrentPosition => currentPosition;
        public Vector2 CurrentImageSize => currentImageSize;
        public Vector2 CurrentCanvasSize => currentCanvasSize;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            SetupCanvasIntegration();
        }
        
        /// <summary>
        /// Initializes the scaling and alignment system
        /// </summary>
        private void Initialize()
        {
            // Find canvas manager if not assigned
            if (canvasManager == null)
            {
                var canvasManagerComponent = FindObjectOfType<CanvasDefinitionManager>();
                if (canvasManagerComponent != null)
                {
                    canvasManager = canvasManagerComponent;
                }
            }
            
            // Find image transform if not assigned
            if (imageTransform == null)
            {
                imageTransform = transform;
            }
            
            // Find image renderer if not assigned
            if (imageRenderer == null)
            {
                imageRenderer = GetComponent<Renderer>();
                if (imageRenderer == null)
                {
                    imageRenderer = GetComponentInChildren<Renderer>();
                }
            }
            
            isInitialized = true;
            Debug.Log("ImageScalingAlignment: Initialized successfully");
        }
        
        /// <summary>
        /// Sets up integration with canvas management system
        /// </summary>
        private void SetupCanvasIntegration()
        {
            if (canvasManager != null)
            {
                // Subscribe to canvas events
                canvasManager.OnCanvasDefined += OnCanvasDefined;
                canvasManager.OnCanvasCleared += OnCanvasCleared;
                
                // Update alignment if canvas is already defined
                if (canvasManager.IsCanvasDefined)
                {
                    UpdateAlignment();
                }
            }
        }
        
        /// <summary>
        /// Handles canvas definition events
        /// </summary>
        private void OnCanvasDefined(CanvasData canvasData)
        {
            if (canvasData != null && canvasData.isValid)
            {
                currentCanvasSize = canvasData.dimensions;
                canvasTransform = canvasManager.CanvasTransform;
                
                if (autoUpdateAlignment)
                {
                    UpdateAlignment();
                }
                
                Debug.Log($"ImageScalingAlignment: Canvas defined - {currentCanvasSize.x:F2}x{currentCanvasSize.y:F2}m");
            }
        }
        
        /// <summary>
        /// Handles canvas cleared events
        /// </summary>
        private void OnCanvasCleared()
        {
            currentCanvasSize = Vector2.zero;
            canvasTransform = null;
            Debug.Log("ImageScalingAlignment: Canvas cleared");
        }
        
        /// <summary>
        /// Updates image scaling and alignment based on current settings
        /// </summary>
        public void UpdateAlignment()
        {
            if (!isInitialized || imageTransform == null)
            {
                Debug.LogWarning("ImageScalingAlignment: Not properly initialized");
                return;
            }
            
            // Get current image size
            UpdateImageSize();
            
            // Calculate and apply scaling
            Vector3 newScale = CalculateScale();
            ApplyScale(newScale);
            
            // Calculate and apply positioning
            Vector3 newPosition = CalculatePosition();
            ApplyPosition(newPosition);
            
            // Fire events
            OnAlignmentUpdated?.Invoke(currentImageSize, currentCanvasSize);
            
            if (showDebugInfo)
            {
                Debug.Log($"ImageScalingAlignment: Updated - Scale: {currentScale}, Position: {currentPosition}");
            }
        }
        
        /// <summary>
        /// Updates the current image size from the renderer or texture
        /// </summary>
        private void UpdateImageSize()
        {
            if (imageRenderer != null && imageRenderer.material != null && imageRenderer.material.mainTexture != null)
            {
                Texture texture = imageRenderer.material.mainTexture;
                currentImageSize = new Vector2(texture.width, texture.height);
            }
            else
            {
                // Default size if no texture available
                currentImageSize = Vector2.one;
            }
        }
        
        /// <summary>
        /// Calculates the appropriate scale based on scaling mode and canvas size
        /// </summary>
        private Vector3 CalculateScale()
        {
            if (currentCanvasSize == Vector2.zero || currentImageSize == Vector2.zero)
            {
                return Vector3.one;
            }
            
            Vector3 scale = Vector3.one;
            
            switch (scalingMode)
            {
                case ScalingMode.FitToCanvas:
                    scale = CalculateFitToCanvasScale();
                    break;
                    
                case ScalingMode.FillCanvas:
                    scale = CalculateFillCanvasScale();
                    break;
                    
                case ScalingMode.StretchToCanvas:
                    scale = CalculateStretchToCanvasScale();
                    break;
                    
                case ScalingMode.OriginalSize:
                    scale = CalculateOriginalSizeScale();
                    break;
                    
                case ScalingMode.Custom:
                    // Keep current scale for custom mode
                    scale = imageTransform.localScale;
                    break;
            }
            
            // Apply padding
            if (paddingPercent > 0 && scalingMode != ScalingMode.Custom)
            {
                float paddingFactor = 1.0f - (paddingPercent * 2.0f);
                scale *= paddingFactor;
            }
            
            // Clamp to min/max scale
            scale.x = Mathf.Clamp(scale.x, minScale.x, maxScale.x);
            scale.y = Mathf.Clamp(scale.y, minScale.y, maxScale.y);
            scale.z = 1.0f; // Keep Z scale at 1 for 2D images
            
            return scale;
        }
        
        /// <summary>
        /// Calculates scale to fit image within canvas while maintaining aspect ratio
        /// </summary>
        private Vector3 CalculateFitToCanvasScale()
        {
            float canvasAspect = currentCanvasSize.x / currentCanvasSize.y;
            float imageAspect = currentImageSize.x / currentImageSize.y;
            
            float scaleX, scaleY;
            
            if (maintainAspectRatio)
            {
                if (imageAspect > canvasAspect)
                {
                    // Image is wider than canvas - fit to width
                    scaleX = currentCanvasSize.x / currentImageSize.x;
                    scaleY = scaleX;
                }
                else
                {
                    // Image is taller than canvas - fit to height
                    scaleY = currentCanvasSize.y / currentImageSize.y;
                    scaleX = scaleY;
                }
            }
            else
            {
                scaleX = currentCanvasSize.x / currentImageSize.x;
                scaleY = currentCanvasSize.y / currentImageSize.y;
                float minScale = Mathf.Min(scaleX, scaleY);
                scaleX = scaleY = minScale;
            }
            
            return new Vector3(scaleX, scaleY, 1.0f);
        }
        
        /// <summary>
        /// Calculates scale to fill entire canvas while maintaining aspect ratio
        /// </summary>
        private Vector3 CalculateFillCanvasScale()
        {
            float scaleX = currentCanvasSize.x / currentImageSize.x;
            float scaleY = currentCanvasSize.y / currentImageSize.y;
            
            if (maintainAspectRatio)
            {
                float maxScale = Mathf.Max(scaleX, scaleY);
                scaleX = scaleY = maxScale;
            }
            
            return new Vector3(scaleX, scaleY, 1.0f);
        }
        
        /// <summary>
        /// Calculates scale to stretch image to exactly match canvas dimensions
        /// </summary>
        private Vector3 CalculateStretchToCanvasScale()
        {
            float scaleX = currentCanvasSize.x / currentImageSize.x;
            float scaleY = currentCanvasSize.y / currentImageSize.y;
            
            return new Vector3(scaleX, scaleY, 1.0f);
        }
        
        /// <summary>
        /// Calculates scale to maintain original image size in world units
        /// </summary>
        private Vector3 CalculateOriginalSizeScale()
        {
            // Assume 1 pixel = 1mm for original size calculation
            float pixelsPerMeter = 1000f;
            float scaleX = currentImageSize.x / pixelsPerMeter;
            float scaleY = currentImageSize.y / pixelsPerMeter;
            
            return new Vector3(scaleX, scaleY, 1.0f);
        }
        
        /// <summary>
        /// Calculates the position based on alignment mode and canvas position
        /// </summary>
        private Vector3 CalculatePosition()
        {
            if (canvasTransform == null)
            {
                return imageTransform.position;
            }
            
            Vector3 canvasPosition = canvasTransform.position;
            Vector3 offset = Vector3.zero;
            
            // Calculate alignment offset based on canvas size and image scale
            Vector2 scaledImageSize = new Vector2(
                currentImageSize.x * currentScale.x,
                currentImageSize.y * currentScale.y
            );
            
            switch (alignmentMode)
            {
                case AlignmentMode.Center:
                    // No offset needed for center alignment
                    break;
                    
                case AlignmentMode.TopLeft:
                    offset.x = -currentCanvasSize.x * 0.5f + scaledImageSize.x * 0.5f;
                    offset.y = currentCanvasSize.y * 0.5f - scaledImageSize.y * 0.5f;
                    break;
                    
                case AlignmentMode.TopCenter:
                    offset.y = currentCanvasSize.y * 0.5f - scaledImageSize.y * 0.5f;
                    break;
                    
                case AlignmentMode.TopRight:
                    offset.x = currentCanvasSize.x * 0.5f - scaledImageSize.x * 0.5f;
                    offset.y = currentCanvasSize.y * 0.5f - scaledImageSize.y * 0.5f;
                    break;
                    
                case AlignmentMode.MiddleLeft:
                    offset.x = -currentCanvasSize.x * 0.5f + scaledImageSize.x * 0.5f;
                    break;
                    
                case AlignmentMode.MiddleRight:
                    offset.x = currentCanvasSize.x * 0.5f - scaledImageSize.x * 0.5f;
                    break;
                    
                case AlignmentMode.BottomLeft:
                    offset.x = -currentCanvasSize.x * 0.5f + scaledImageSize.x * 0.5f;
                    offset.y = -currentCanvasSize.y * 0.5f + scaledImageSize.y * 0.5f;
                    break;
                    
                case AlignmentMode.BottomCenter:
                    offset.y = -currentCanvasSize.y * 0.5f + scaledImageSize.y * 0.5f;
                    break;
                    
                case AlignmentMode.BottomRight:
                    offset.x = currentCanvasSize.x * 0.5f - scaledImageSize.x * 0.5f;
                    offset.y = -currentCanvasSize.y * 0.5f + scaledImageSize.y * 0.5f;
                    break;
                    
                case AlignmentMode.Custom:
                    offset.x = alignmentOffset.x;
                    offset.y = alignmentOffset.y;
                    break;
            }
            
            // Transform offset to world space
            Vector3 worldOffset = canvasTransform.TransformDirection(offset);
            
            return canvasPosition + worldOffset;
        }
        
        /// <summary>
        /// Applies the calculated scale to the image transform
        /// </summary>
        private void ApplyScale(Vector3 scale)
        {
            if (imageTransform != null)
            {
                currentScale = scale;
                imageTransform.localScale = scale;
                OnScaleChanged?.Invoke(scale);
            }
        }
        
        /// <summary>
        /// Applies the calculated position to the image transform
        /// </summary>
        private void ApplyPosition(Vector3 position)
        {
            if (imageTransform != null)
            {
                currentPosition = position;
                imageTransform.position = position;
                OnPositionChanged?.Invoke(position);
            }
        }
        
        /// <summary>
        /// Sets the scaling mode and updates alignment
        /// </summary>
        public void SetScalingMode(ScalingMode mode)
        {
            scalingMode = mode;
            if (autoUpdateAlignment)
            {
                UpdateAlignment();
            }
        }
        
        /// <summary>
        /// Sets the alignment mode and updates alignment
        /// </summary>
        public void SetAlignmentMode(AlignmentMode mode)
        {
            alignmentMode = mode;
            if (autoUpdateAlignment)
            {
                UpdateAlignment();
            }
        }
        
        /// <summary>
        /// Sets custom alignment offset for custom alignment mode
        /// </summary>
        public void SetCustomAlignmentOffset(Vector2 offset)
        {
            alignmentOffset = offset;
            if (alignmentMode == AlignmentMode.Custom && autoUpdateAlignment)
            {
                UpdateAlignment();
            }
        }
        
        /// <summary>
        /// Sets the canvas manager reference
        /// </summary>
        public void SetCanvasManager(ICanvasManager manager)
        {
            // Unsubscribe from old manager
            if (canvasManager != null)
            {
                canvasManager.OnCanvasDefined -= OnCanvasDefined;
                canvasManager.OnCanvasCleared -= OnCanvasCleared;
            }
            
            canvasManager = manager;
            SetupCanvasIntegration();
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (canvasManager != null)
            {
                canvasManager.OnCanvasDefined -= OnCanvasDefined;
                canvasManager.OnCanvasCleared -= OnCanvasCleared;
            }
        }
    }
    
    /// <summary>
    /// Scaling modes for image display
    /// </summary>
    public enum ScalingMode
    {
        FitToCanvas,        // Fit image within canvas maintaining aspect ratio
        FillCanvas,         // Fill entire canvas maintaining aspect ratio (may crop)
        StretchToCanvas,    // Stretch to exactly match canvas (may distort)
        OriginalSize,       // Display at original pixel size
        Custom              // Use manually set scale
    }
    
    /// <summary>
    /// Alignment modes for image positioning
    /// </summary>
    public enum AlignmentMode
    {
        Center,
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Custom
    }
}