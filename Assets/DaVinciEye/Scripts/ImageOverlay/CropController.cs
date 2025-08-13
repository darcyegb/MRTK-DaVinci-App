using System;
using UnityEngine;
using MixedReality.Toolkit.Input;
using MixedReality.Toolkit;

namespace DaVinciEye.ImageOverlay
{
    /// <summary>
    /// Handles rectangular crop area definition using hand gestures
    /// Provides real-time crop preview and application system
    /// </summary>
    public class CropController : MonoBehaviour
    {
        [Header("Crop Visualization")]
        [SerializeField] private LineRenderer cropBoundaryRenderer;
        [SerializeField] private Material cropBoundaryMaterial;
        [SerializeField] private Color cropBoundaryColor = Color.yellow;
        [SerializeField] private float cropBoundaryWidth = 0.005f;
        
        [Header("Interaction Settings")]
        [SerializeField] private float minCropSize = 0.1f;
        [SerializeField] private bool enableRealTimePreview = true;
        [SerializeField] private LayerMask imageLayerMask = -1;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject cropHandlesPrefab;
        [SerializeField] private Transform cropHandlesParent;
        
        // Private fields
        private ImageOverlayManager imageOverlay;
        private Camera mainCamera;
        private bool isCropping = false;
        private bool isDragging = false;
        private Vector2 cropStartPoint;
        private Vector2 cropEndPoint;
        private Rect currentCropArea;
        private Rect previewCropArea;
        private GameObject[] cropHandles = new GameObject[4];
        private Vector3[] cropBoundaryPoints = new Vector3[5]; // 5 points to close the rectangle
        
        // Hand tracking
        private bool isLeftHandTracked = false;
        private bool isRightHandTracked = false;
        private Vector3 leftHandPosition;
        private Vector3 rightHandPosition;
        
        // Events
        public event Action<Rect> OnCropAreaChanged;
        public event Action<Rect> OnCropAreaApplied;
        public event Action OnCropStarted;
        public event Action OnCropCancelled;
        
        // Properties
        public bool IsCropping => isCropping;
        public Rect CurrentCropArea => currentCropArea;
        public bool HasCropArea => currentCropArea.width > 0 && currentCropArea.height > 0;
        
        private void Awake()
        {
            Initialize();
        }
        
        private void Start()
        {
            SetupComponents();
        }
        
        private void Update()
        {
            if (isCropping)
            {
                UpdateHandTracking();
                UpdateCropArea();
                UpdateVisualization();
            }
        }
        
        /// <summary>
        /// Initializes the crop controller
        /// </summary>
        private void Initialize()
        {
            // Get main camera
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            
            // Initialize crop area
            currentCropArea = new Rect(0, 0, 1, 1);
            previewCropArea = currentCropArea;
            
            Debug.Log("CropController: Initialized");
        }
        
        /// <summary>
        /// Sets up required components
        /// </summary>
        private void SetupComponents()
        {
            // Find image overlay manager
            imageOverlay = GetComponent<ImageOverlayManager>();
            if (imageOverlay == null)
            {
                imageOverlay = FindObjectOfType<ImageOverlayManager>();
            }
            
            // Setup crop boundary renderer
            SetupCropBoundaryRenderer();
            
            // Setup crop handles
            SetupCropHandles();
            
            Debug.Log("CropController: Components setup complete");
        }
        
        /// <summary>
        /// Sets up the line renderer for crop boundary visualization
        /// </summary>
        private void SetupCropBoundaryRenderer()
        {
            if (cropBoundaryRenderer == null)
            {
                GameObject boundaryObject = new GameObject("CropBoundary");
                boundaryObject.transform.SetParent(transform);
                cropBoundaryRenderer = boundaryObject.AddComponent<LineRenderer>();
            }
            
            // Configure line renderer
            cropBoundaryRenderer.material = cropBoundaryMaterial ?? CreateDefaultBoundaryMaterial();
            cropBoundaryRenderer.color = cropBoundaryColor;
            cropBoundaryRenderer.startWidth = cropBoundaryWidth;
            cropBoundaryRenderer.endWidth = cropBoundaryWidth;
            cropBoundaryRenderer.positionCount = 5;
            cropBoundaryRenderer.useWorldSpace = true;
            cropBoundaryRenderer.enabled = false;
            
            Debug.Log("CropController: Crop boundary renderer configured");
        }
        
        /// <summary>
        /// Creates a default material for the crop boundary
        /// </summary>
        private Material CreateDefaultBoundaryMaterial()
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.color = cropBoundaryColor;
            return material;
        }
        
        /// <summary>
        /// Sets up crop handles for visual feedback
        /// </summary>
        private void SetupCropHandles()
        {
            if (cropHandlesParent == null)
            {
                GameObject handlesParent = new GameObject("CropHandles");
                handlesParent.transform.SetParent(transform);
                cropHandlesParent = handlesParent.transform;
            }
            
            // Create corner handles if prefab is available
            if (cropHandlesPrefab != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    cropHandles[i] = Instantiate(cropHandlesPrefab, cropHandlesParent);
                    cropHandles[i].name = $"CropHandle_{i}";
                    cropHandles[i].SetActive(false);
                }
            }
            
            Debug.Log("CropController: Crop handles setup complete");
        }
        
        /// <summary>
        /// Starts crop area definition mode
        /// </summary>
        public void StartCropping()
        {
            if (imageOverlay == null || imageOverlay.CurrentImage == null)
            {
                Debug.LogWarning("CropController: Cannot start cropping - no image loaded");
                return;
            }
            
            isCropping = true;
            isDragging = false;
            
            // Reset crop area
            previewCropArea = new Rect(0.25f, 0.25f, 0.5f, 0.5f); // Start with center crop
            
            // Enable visualization
            if (cropBoundaryRenderer != null)
            {
                cropBoundaryRenderer.enabled = true;
            }
            
            // Show crop handles
            ShowCropHandles(true);
            
            OnCropStarted?.Invoke();
            
            Debug.Log("CropController: Started cropping mode");
        }
        
        /// <summary>
        /// Cancels crop area definition
        /// </summary>
        public void CancelCropping()
        {
            isCropping = false;
            isDragging = false;
            
            // Disable visualization
            if (cropBoundaryRenderer != null)
            {
                cropBoundaryRenderer.enabled = false;
            }
            
            // Hide crop handles
            ShowCropHandles(false);
            
            OnCropCancelled?.Invoke();
            
            Debug.Log("CropController: Cancelled cropping mode");
        }
        
        /// <summary>
        /// Applies the current crop area
        /// </summary>
        public void ApplyCrop()
        {
            if (!isCropping)
            {
                Debug.LogWarning("CropController: Cannot apply crop - not in cropping mode");
                return;
            }
            
            // Validate crop area
            if (!ValidateCropArea(previewCropArea))
            {
                Debug.LogWarning("CropController: Invalid crop area");
                return;
            }
            
            // Apply crop to image overlay
            currentCropArea = previewCropArea;
            
            if (imageOverlay != null)
            {
                imageOverlay.SetCropArea(currentCropArea);
            }
            
            // Exit cropping mode
            isCropping = false;
            
            // Disable visualization
            if (cropBoundaryRenderer != null)
            {
                cropBoundaryRenderer.enabled = false;
            }
            
            // Hide crop handles
            ShowCropHandles(false);
            
            OnCropAreaApplied?.Invoke(currentCropArea);
            
            Debug.Log($"CropController: Applied crop area: {currentCropArea}");
        }
        
        /// <summary>
        /// Clears the current crop area
        /// </summary>
        public void ClearCrop()
        {
            currentCropArea = new Rect(0, 0, 1, 1);
            previewCropArea = currentCropArea;
            
            if (imageOverlay != null)
            {
                imageOverlay.SetCropArea(currentCropArea);
            }
            
            OnCropAreaChanged?.Invoke(currentCropArea);
            
            Debug.Log("CropController: Cleared crop area");
        }
        
        /// <summary>
        /// Updates hand tracking data
        /// </summary>
        private void UpdateHandTracking()
        {
            // Get hand tracking data from MRTK
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Left, out MixedRealityPose leftPose))
            {
                isLeftHandTracked = true;
                leftHandPosition = leftPose.Position;
            }
            else
            {
                isLeftHandTracked = false;
            }
            
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, Handedness.Right, out MixedRealityPose rightPose))
            {
                isRightHandTracked = true;
                rightHandPosition = rightPose.Position;
            }
            else
            {
                isRightHandTracked = false;
            }
        }
        
        /// <summary>
        /// Updates the crop area based on hand gestures
        /// </summary>
        private void UpdateCropArea()
        {
            if (!isLeftHandTracked && !isRightHandTracked)
            {
                return;
            }
            
            // Check for pinch gesture to start/end dragging
            bool leftPinching = isLeftHandTracked && IsHandPinching(Handedness.Left);
            bool rightPinching = isRightHandTracked && IsHandPinching(Handedness.Right);
            
            if ((leftPinching || rightPinching) && !isDragging)
            {
                // Start dragging
                StartDragging();
            }
            else if (!(leftPinching || rightPinching) && isDragging)
            {
                // End dragging
                EndDragging();
            }
            
            if (isDragging)
            {
                UpdateDragging();
            }
        }
        
        /// <summary>
        /// Checks if a hand is performing a pinch gesture
        /// </summary>
        private bool IsHandPinching(Handedness handedness)
        {
            // Use MRTK's hand interaction to detect pinch
            if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, handedness, out MixedRealityPose thumbPose) &&
                HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, handedness, out MixedRealityPose indexPose))
            {
                float distance = Vector3.Distance(thumbPose.Position, indexPose.Position);
                return distance < 0.03f; // 3cm threshold for pinch
            }
            
            return false;
        }
        
        /// <summary>
        /// Starts dragging operation
        /// </summary>
        private void StartDragging()
        {
            isDragging = true;
            
            // Convert hand position to image coordinates
            Vector3 handPos = isLeftHandTracked ? leftHandPosition : rightHandPosition;
            cropStartPoint = WorldToImageCoordinates(handPos);
            
            Debug.Log($"CropController: Started dragging at {cropStartPoint}");
        }
        
        /// <summary>
        /// Ends dragging operation
        /// </summary>
        private void EndDragging()
        {
            isDragging = false;
            
            Debug.Log("CropController: Ended dragging");
        }
        
        /// <summary>
        /// Updates dragging operation
        /// </summary>
        private void UpdateDragging()
        {
            // Convert current hand position to image coordinates
            Vector3 handPos = isLeftHandTracked ? leftHandPosition : rightHandPosition;
            cropEndPoint = WorldToImageCoordinates(handPos);
            
            // Calculate crop rectangle
            float minX = Mathf.Min(cropStartPoint.x, cropEndPoint.x);
            float minY = Mathf.Min(cropStartPoint.y, cropEndPoint.y);
            float maxX = Mathf.Max(cropStartPoint.x, cropEndPoint.x);
            float maxY = Mathf.Max(cropStartPoint.y, cropEndPoint.y);
            
            previewCropArea = new Rect(minX, minY, maxX - minX, maxY - minY);
            
            // Clamp to valid range
            previewCropArea.x = Mathf.Clamp01(previewCropArea.x);
            previewCropArea.y = Mathf.Clamp01(previewCropArea.y);
            previewCropArea.width = Mathf.Clamp01(previewCropArea.width);
            previewCropArea.height = Mathf.Clamp01(previewCropArea.height);
            
            // Ensure minimum size
            if (previewCropArea.width < minCropSize)
            {
                previewCropArea.width = minCropSize;
            }
            if (previewCropArea.height < minCropSize)
            {
                previewCropArea.height = minCropSize;
            }
            
            OnCropAreaChanged?.Invoke(previewCropArea);
        }
        
        /// <summary>
        /// Converts world position to normalized image coordinates
        /// </summary>
        private Vector2 WorldToImageCoordinates(Vector3 worldPosition)
        {
            if (imageOverlay == null || imageOverlay.transform == null)
            {
                return Vector2.zero;
            }
            
            // Convert world position to local position relative to image overlay
            Vector3 localPosition = imageOverlay.transform.InverseTransformPoint(worldPosition);
            
            // Convert to normalized coordinates (0-1)
            Vector2 imageCoords = new Vector2(
                (localPosition.x + 0.5f), // Assuming quad is centered at origin with size 1
                (localPosition.y + 0.5f)
            );
            
            // Clamp to valid range
            imageCoords.x = Mathf.Clamp01(imageCoords.x);
            imageCoords.y = Mathf.Clamp01(imageCoords.y);
            
            return imageCoords;
        }
        
        /// <summary>
        /// Updates crop area visualization
        /// </summary>
        private void UpdateVisualization()
        {
            if (cropBoundaryRenderer == null || imageOverlay == null)
            {
                return;
            }
            
            // Convert normalized crop area to world coordinates
            Rect cropRect = enableRealTimePreview ? previewCropArea : currentCropArea;
            Vector3[] worldPoints = ImageCoordsToWorldPoints(cropRect);
            
            // Update line renderer points
            for (int i = 0; i < 5; i++)
            {
                cropBoundaryPoints[i] = worldPoints[i % 4]; // Close the rectangle
            }
            
            cropBoundaryRenderer.SetPositions(cropBoundaryPoints);
            
            // Update crop handles
            UpdateCropHandles(worldPoints);
        }
        
        /// <summary>
        /// Converts normalized image coordinates to world points
        /// </summary>
        private Vector3[] ImageCoordsToWorldPoints(Rect cropRect)
        {
            if (imageOverlay == null || imageOverlay.transform == null)
            {
                return new Vector3[4];
            }
            
            Transform imageTransform = imageOverlay.transform;
            
            // Calculate corner positions in local space
            Vector3[] localPoints = new Vector3[4];
            localPoints[0] = new Vector3(cropRect.xMin - 0.5f, cropRect.yMin - 0.5f, 0); // Bottom-left
            localPoints[1] = new Vector3(cropRect.xMax - 0.5f, cropRect.yMin - 0.5f, 0); // Bottom-right
            localPoints[2] = new Vector3(cropRect.xMax - 0.5f, cropRect.yMax - 0.5f, 0); // Top-right
            localPoints[3] = new Vector3(cropRect.xMin - 0.5f, cropRect.yMax - 0.5f, 0); // Top-left
            
            // Convert to world space
            Vector3[] worldPoints = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                worldPoints[i] = imageTransform.TransformPoint(localPoints[i]);
            }
            
            return worldPoints;
        }
        
        /// <summary>
        /// Updates crop handle positions
        /// </summary>
        private void UpdateCropHandles(Vector3[] worldPoints)
        {
            if (cropHandles == null || worldPoints == null)
            {
                return;
            }
            
            for (int i = 0; i < cropHandles.Length && i < worldPoints.Length; i++)
            {
                if (cropHandles[i] != null)
                {
                    cropHandles[i].transform.position = worldPoints[i];
                }
            }
        }
        
        /// <summary>
        /// Shows or hides crop handles
        /// </summary>
        private void ShowCropHandles(bool show)
        {
            if (cropHandles == null)
            {
                return;
            }
            
            foreach (GameObject handle in cropHandles)
            {
                if (handle != null)
                {
                    handle.SetActive(show);
                }
            }
        }
        
        /// <summary>
        /// Validates a crop area
        /// </summary>
        private bool ValidateCropArea(Rect cropArea)
        {
            return cropArea.width >= minCropSize && 
                   cropArea.height >= minCropSize &&
                   cropArea.x >= 0 && cropArea.y >= 0 &&
                   cropArea.x + cropArea.width <= 1 &&
                   cropArea.y + cropArea.height <= 1;
        }
        
        /// <summary>
        /// Sets the crop area programmatically
        /// </summary>
        public void SetCropArea(Rect cropArea)
        {
            if (!ValidateCropArea(cropArea))
            {
                Debug.LogWarning($"CropController: Invalid crop area: {cropArea}");
                return;
            }
            
            currentCropArea = cropArea;
            previewCropArea = cropArea;
            
            OnCropAreaChanged?.Invoke(currentCropArea);
        }
        
        private void OnDestroy()
        {
            // Clean up crop handles
            if (cropHandles != null)
            {
                foreach (GameObject handle in cropHandles)
                {
                    if (handle != null)
                    {
                        DestroyImmediate(handle);
                    }
                }
            }
        }
    }
}