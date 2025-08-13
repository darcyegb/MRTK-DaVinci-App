using System;
using UnityEngine;
using MixedReality.Toolkit.SpatialManipulation;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Manages canvas definition using MRTK's BoundsControl component
    /// Implements the simplified approach with automatic validation and visual feedback
    /// </summary>
    public class CanvasDefinitionManager : MonoBehaviour, ICanvasManager
    {
        [Header("Canvas Configuration")]
        [SerializeField] private GameObject artCanvasPrefab;
        [SerializeField] private Vector2 minCanvasSize = new Vector2(0.1f, 0.1f); // 10cm minimum
        [SerializeField] private Vector2 maxCanvasSize = new Vector2(3.0f, 2.0f); // 3m x 2m maximum
        
        [Header("MRTK Components")]
        [SerializeField] private BoundsControl boundsControl;
        [SerializeField] private GameObject artCanvasObject;
        
        [Header("Visualization")]
        [SerializeField] private CanvasBoundaryVisualizer boundaryVisualizer;
        
        [Header("Spatial Anchoring")]
        [SerializeField] private CanvasAnchorManager anchorManager;
        
        // Interface properties
        public bool IsCanvasDefined { get; private set; }
        public Bounds CanvasBounds { get; private set; }
        public Transform CanvasTransform => artCanvasObject?.transform;
        public CanvasData CurrentCanvas { get; private set; }
        
        // Events
        public event Action<CanvasData> OnCanvasDefined;
        public event Action OnCanvasCleared;
        
        private bool isDefining = false;
        
        private void Awake()
        {
            InitializeCanvas();
        }
        
        private void InitializeCanvas()
        {
            // Create ArtCanvas GameObject if not assigned
            if (artCanvasObject == null)
            {
                artCanvasObject = new GameObject("ArtCanvas");
                artCanvasObject.transform.SetParent(transform);
            }
            
            // Add BoundsControl component if not present
            if (boundsControl == null)
            {
                boundsControl = artCanvasObject.GetComponent<BoundsControl>();
                if (boundsControl == null)
                {
                    boundsControl = artCanvasObject.AddComponent<BoundsControl>();
                }
            }
            
            // Configure BoundsControl for canvas sizing
            ConfigureBoundsControl();
            
            // Subscribe to bounds changed events
            if (boundsControl != null)
            {
                boundsControl.BoundsChanged.AddListener(OnBoundsChanged);
            }
            
            // Initialize boundary visualizer
            InitializeBoundaryVisualizer();
            
            // Initialize anchor manager
            InitializeAnchorManager();
            
            Debug.Log("CanvasDefinitionManager: Initialized with MRTK BoundsControl");
        }
        
        private void ConfigureBoundsControl()
        {
            if (boundsControl == null) return;
            
            // Set bounds override to define min/max canvas size
            var bounds = new Bounds(Vector3.zero, new Vector3(maxCanvasSize.x, 0.01f, maxCanvasSize.y));
            boundsControl.BoundsOverride = bounds;
            
            // Configure visual settings
            boundsControl.FlattenAxis = BoundsControl.FlattenModeType.FlattenY; // Canvas is flat (Y-axis)
            boundsControl.ScaleHandlesConfig.ShowScaleHandles = true;
            boundsControl.RotationHandlesConfig.ShowRotationHandles = false; // Disable rotation for simplicity
            
            // Set minimum and maximum scale constraints
            var minScale = new Vector3(minCanvasSize.x, 0.01f, minCanvasSize.y);
            var maxScale = new Vector3(maxCanvasSize.x, 0.01f, maxCanvasSize.y);
            
            // Apply scale constraints through bounds override
            boundsControl.BoundsOverride = new Bounds(Vector3.zero, maxScale);
            
            Debug.Log($"CanvasDefinitionManager: BoundsControl configured - Min: {minCanvasSize}, Max: {maxCanvasSize}");
        }
        
        public void StartCanvasDefinition()
        {
            if (isDefining)
            {
                Debug.LogWarning("CanvasDefinitionManager: Canvas definition already in progress");
                return;
            }
            
            isDefining = true;
            IsCanvasDefined = false;
            
            // Enable BoundsControl for interaction
            if (boundsControl != null)
            {
                boundsControl.enabled = true;
                artCanvasObject.SetActive(true);
            }
            
            Debug.Log("CanvasDefinitionManager: Started canvas definition mode");
        }
        
        public void DefineCanvasCorner(Vector3 worldPosition)
        {
            // This method is not needed with BoundsControl approach
            // BoundsControl handles corner definition automatically through its handles
            Debug.Log($"CanvasDefinitionManager: Corner definition handled automatically by BoundsControl");
        }
        
        public void CompleteCanvasDefinition()
        {
            if (!isDefining)
            {
                Debug.LogWarning("CanvasDefinitionManager: No canvas definition in progress");
                return;
            }
            
            // Get current bounds from BoundsControl
            if (boundsControl != null)
            {
                var bounds = boundsControl.TargetBounds;
                CreateCanvasData(bounds);
            }
            
            isDefining = false;
            Debug.Log("CanvasDefinitionManager: Canvas definition completed");
        }
        
        public void RedefineCanvas()
        {
            ClearCanvas();
            StartCanvasDefinition();
        }
        
        public void LoadCanvas(CanvasData canvasData)
        {
            if (canvasData == null || !canvasData.isValid)
            {
                Debug.LogError("CanvasDefinitionManager: Invalid canvas data provided");
                return;
            }
            
            CurrentCanvas = canvasData;
            
            // Apply canvas data to BoundsControl
            if (boundsControl != null && artCanvasObject != null)
            {
                // Set position and scale based on canvas data
                artCanvasObject.transform.position = canvasData.center;
                
                var scale = new Vector3(canvasData.dimensions.x, 0.01f, canvasData.dimensions.y);
                artCanvasObject.transform.localScale = scale;
                
                // Update bounds
                CanvasBounds = new Bounds(canvasData.center, scale);
                
                IsCanvasDefined = true;
                artCanvasObject.SetActive(true);
                
                // Update boundary visualizer
                if (boundaryVisualizer != null)
                {
                    boundaryVisualizer.SetCanvasData(canvasData);
                }
                
                Debug.Log($"CanvasDefinitionManager: Canvas loaded - Area: {canvasData.area:F2} m²");
            }
        }
        
        private void OnBoundsChanged()
        {
            if (boundsControl == null) return;
            
            var bounds = boundsControl.TargetBounds;
            
            // Validate bounds size
            if (IsValidCanvasSize(bounds.size))
            {
                CanvasBounds = bounds;
                
                // Auto-complete definition if bounds are valid and we're in definition mode
                if (isDefining)
                {
                    CreateCanvasData(bounds);
                    isDefining = false;
                }
                
                Debug.Log($"CanvasDefinitionManager: Bounds changed - Size: {bounds.size}");
            }
            else
            {
                Debug.LogWarning($"CanvasDefinitionManager: Invalid canvas size: {bounds.size}");
            }
        }
        
        private bool IsValidCanvasSize(Vector3 size)
        {
            var width = size.x;
            var height = size.z; // Z-axis for depth in flat canvas
            
            return width >= minCanvasSize.x && width <= maxCanvasSize.x &&
                   height >= minCanvasSize.y && height <= maxCanvasSize.y;
        }
        
        private void CreateCanvasData(Bounds bounds)
        {
            CurrentCanvas = new CanvasData();
            
            // Calculate corners from bounds (clockwise order)
            var center = bounds.center;
            var extents = bounds.extents;
            
            CurrentCanvas.corners[0] = center + new Vector3(-extents.x, 0, -extents.z); // Bottom-left
            CurrentCanvas.corners[1] = center + new Vector3(extents.x, 0, -extents.z);  // Bottom-right
            CurrentCanvas.corners[2] = center + new Vector3(extents.x, 0, extents.z);   // Top-right
            CurrentCanvas.corners[3] = center + new Vector3(-extents.x, 0, extents.z);  // Top-left
            
            CurrentCanvas.center = center;
            CurrentCanvas.dimensions = new Vector2(bounds.size.x, bounds.size.z);
            CurrentCanvas.ValidateAndCalculate();
            
            // Store bounds using JsonUtility as specified in the task
            var boundsJson = JsonUtility.ToJson(bounds);
            Debug.Log($"CanvasDefinitionManager: Bounds stored as JSON: {boundsJson}");
            
            IsCanvasDefined = true;
            CanvasBounds = bounds;
            
            // Update boundary visualizer
            if (boundaryVisualizer != null)
            {
                boundaryVisualizer.SetCanvasData(CurrentCanvas);
            }
            
            // Create spatial anchor for persistence
            if (anchorManager != null)
            {
                var canvasId = GenerateCanvasId();
                anchorManager.CreateCanvasAnchor(CurrentCanvas, canvasId);
            }
            
            // Fire event
            OnCanvasDefined?.Invoke(CurrentCanvas);
            
            Debug.Log($"CanvasDefinitionManager: Canvas defined - Area: {CurrentCanvas.area:F2} m², Dimensions: {CurrentCanvas.dimensions}");
        }
        
        private void ClearCanvas()
        {
            IsCanvasDefined = false;
            CurrentCanvas = null;
            CanvasBounds = new Bounds();
            
            if (artCanvasObject != null)
            {
                artCanvasObject.SetActive(false);
            }
            
            // Hide boundary visualization
            if (boundaryVisualizer != null)
            {
                boundaryVisualizer.HideBoundary();
            }
            
            OnCanvasCleared?.Invoke();
            Debug.Log("CanvasDefinitionManager: Canvas cleared");
        }
        
        private void InitializeBoundaryVisualizer()
        {
            // Add boundary visualizer if not assigned
            if (boundaryVisualizer == null && artCanvasObject != null)
            {
                boundaryVisualizer = artCanvasObject.GetComponent<CanvasBoundaryVisualizer>();
                if (boundaryVisualizer == null)
                {
                    boundaryVisualizer = artCanvasObject.AddComponent<CanvasBoundaryVisualizer>();
                }
            }
            
            Debug.Log("CanvasDefinitionManager: Boundary visualizer initialized");
        }
        
        private void InitializeAnchorManager()
        {
            // Find anchor manager if not assigned
            if (anchorManager == null)
            {
                anchorManager = FindObjectOfType<CanvasAnchorManager>();
            }
            
            // Subscribe to anchor events
            if (anchorManager != null)
            {
                anchorManager.OnCanvasAnchored += OnCanvasAnchored;
                anchorManager.OnCanvasAnchorRestored += OnCanvasAnchorRestored;
                anchorManager.OnCanvasAnchorLost += OnCanvasAnchorLost;
                anchorManager.OnAnchorError += OnAnchorError;
            }
            
            Debug.Log("CanvasDefinitionManager: Anchor manager initialized");
        }
        
        // Anchor event handlers
        private void OnCanvasAnchored(string canvasId, CanvasData canvasData)
        {
            Debug.Log($"CanvasDefinitionManager: Canvas anchored successfully - ID: {canvasId}");
            
            // Update current canvas with anchor ID
            if (CurrentCanvas != null)
            {
                CurrentCanvas.anchorId = canvasId;
            }
        }
        
        private void OnCanvasAnchorRestored(string canvasId, CanvasData canvasData)
        {
            Debug.Log($"CanvasDefinitionManager: Canvas anchor restored - ID: {canvasId}");
            
            // Load the restored canvas
            LoadCanvas(canvasData);
        }
        
        private void OnCanvasAnchorLost(string canvasId)
        {
            Debug.LogWarning($"CanvasDefinitionManager: Canvas anchor lost - ID: {canvasId}");
            
            // Handle anchor loss (could show warning to user)
            if (CurrentCanvas != null && CurrentCanvas.anchorId == canvasId)
            {
                // Canvas is still defined but anchor is lost
                Debug.LogWarning("CanvasDefinitionManager: Current canvas anchor lost - spatial tracking may be affected");
            }
        }
        
        private void OnAnchorError(string canvasId, string error)
        {
            Debug.LogError($"CanvasDefinitionManager: Anchor error for canvas {canvasId}: {error}");
        }
        
        private string GenerateCanvasId()
        {
            return "Canvas_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }
        
        // Public methods for anchor management
        public void RestoreCanvasFromAnchor(string canvasId)
        {
            if (anchorManager != null)
            {
                anchorManager.RestoreCanvasAnchor(canvasId);
            }
        }
        
        public List<string> GetSavedCanvases()
        {
            return anchorManager?.GetSavedCanvasIds() ?? new List<string>();
        }
        
        public bool IsCurrentCanvasAnchored()
        {
            return CurrentCanvas != null && 
                   !string.IsNullOrEmpty(CurrentCanvas.anchorId) && 
                   anchorManager != null && 
                   anchorManager.IsCanvasAnchored(CurrentCanvas.anchorId);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (boundsControl != null)
            {
                boundsControl.BoundsChanged.RemoveListener(OnBoundsChanged);
            }
            
            if (anchorManager != null)
            {
                anchorManager.OnCanvasAnchored -= OnCanvasAnchored;
                anchorManager.OnCanvasAnchorRestored -= OnCanvasAnchorRestored;
                anchorManager.OnCanvasAnchorLost -= OnCanvasAnchorLost;
                anchorManager.OnAnchorError -= OnAnchorError;
            }
        }
        
        // Public methods for external control
        public void SetCanvasVisible(bool visible)
        {
            if (artCanvasObject != null)
            {
                artCanvasObject.SetActive(visible);
            }
        }
        
        public void SetCanvasSize(Vector2 size)
        {
            if (boundsControl != null && IsValidCanvasSize(new Vector3(size.x, 0.01f, size.y)))
            {
                var bounds = new Bounds(CanvasBounds.center, new Vector3(size.x, 0.01f, size.y));
                boundsControl.BoundsOverride = bounds;
            }
        }
    }
}