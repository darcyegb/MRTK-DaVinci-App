using UnityEngine;
using System.Collections.Generic;

namespace DaVinciEye.Canvas
{
    /// <summary>
    /// Component for persistent visual outline rendering of canvas boundaries
    /// Implements MRTK shader-based boundary line rendering with customizable appearance
    /// </summary>
    public class CanvasBoundaryVisualizer : MonoBehaviour
    {
        [Header("Boundary Visualization")]
        [SerializeField] private Material boundaryLineMaterial;
        [SerializeField] private float lineWidth = 0.005f; // 5mm line width
        [SerializeField] private Color boundaryColor = Color.cyan;
        [SerializeField] private bool showCornerMarkers = true;
        [SerializeField] private bool animateBoundary = true;
        
        [Header("Corner Markers")]
        [SerializeField] private GameObject cornerMarkerPrefab;
        [SerializeField] private float cornerMarkerSize = 0.02f; // 2cm markers
        [SerializeField] private Color cornerMarkerColor = Color.yellow;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationSpeed = 2.0f;
        [SerializeField] private float pulseIntensity = 0.3f;
        
        // Visualization components
        private LineRenderer[] boundaryLines;
        private GameObject[] cornerMarkers;
        private CanvasData currentCanvas;
        private bool isVisible = true;
        
        // Animation state
        private float animationTime = 0f;
        private Color originalBoundaryColor;
        private Color originalCornerColor;
        
        private void Awake()
        {
            InitializeBoundaryVisualization();
        }
        
        private void Update()
        {
            if (animateBoundary && isVisible)
            {
                UpdateBoundaryAnimation();
            }
        }
        
        private void InitializeBoundaryVisualization()
        {
            // Store original colors for animation
            originalBoundaryColor = boundaryColor;
            originalCornerColor = cornerMarkerColor;
            
            // Create boundary line material if not assigned
            if (boundaryLineMaterial == null)
            {
                CreateBoundaryMaterial();
            }
            
            Debug.Log("CanvasBoundaryVisualizer: Boundary visualization initialized");
        }
        
        private void CreateBoundaryMaterial()
        {
            // Create MRTK-compatible material for boundary lines
            boundaryLineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            boundaryLineMaterial.name = "CanvasBoundaryMaterial";
            
            // Configure material properties
            boundaryLineMaterial.color = boundaryColor;
            boundaryLineMaterial.SetFloat("_Surface", 1); // Transparent surface
            boundaryLineMaterial.SetFloat("_Blend", 0); // Alpha blend
            boundaryLineMaterial.SetFloat("_Metallic", 0.0f);
            boundaryLineMaterial.SetFloat("_Smoothness", 0.5f);
            
            // Enable emission for better visibility
            boundaryLineMaterial.EnableKeyword("_EMISSION");
            boundaryLineMaterial.SetColor("_EmissionColor", boundaryColor * 0.5f);
            
            Debug.Log("CanvasBoundaryVisualizer: Boundary material created");
        }
        
        public void SetCanvasData(CanvasData canvasData)
        {
            if (canvasData == null || !canvasData.isValid)
            {
                Debug.LogWarning("CanvasBoundaryVisualizer: Invalid canvas data provided");
                HideBoundary();
                return;
            }
            
            currentCanvas = canvasData;
            CreateBoundaryVisualization();
            ShowBoundary();
            
            Debug.Log($"CanvasBoundaryVisualizer: Canvas boundary set - Area: {canvasData.area:F2} m²");
        }
        
        private void CreateBoundaryVisualization()
        {
            if (currentCanvas == null) return;
            
            // Clear existing visualization
            ClearBoundaryVisualization();
            
            // Create boundary lines (4 lines for rectangle)
            CreateBoundaryLines();
            
            // Create corner markers if enabled
            if (showCornerMarkers)
            {
                CreateCornerMarkers();
            }
        }
        
        private void CreateBoundaryLines()
        {
            boundaryLines = new LineRenderer[4];
            
            for (int i = 0; i < 4; i++)
            {
                // Create line renderer for each boundary edge
                var lineObject = new GameObject($"BoundaryLine_{i}");
                lineObject.transform.SetParent(transform);
                
                var lineRenderer = lineObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(lineRenderer);
                
                // Set line positions (connect corners)
                var startCorner = currentCanvas.corners[i];
                var endCorner = currentCanvas.corners[(i + 1) % 4];
                
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, startCorner);
                lineRenderer.SetPosition(1, endCorner);
                
                boundaryLines[i] = lineRenderer;
            }
            
            Debug.Log("CanvasBoundaryVisualizer: Boundary lines created");
        }
        
        private void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            lineRenderer.material = boundaryLineMaterial;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 100; // Render on top
            
            // Configure for better visibility in MR
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }
        
        private void CreateCornerMarkers()
        {
            cornerMarkers = new GameObject[4];
            
            for (int i = 0; i < 4; i++)
            {
                GameObject marker;
                
                if (cornerMarkerPrefab != null)
                {
                    marker = Instantiate(cornerMarkerPrefab, transform);
                }
                else
                {
                    // Create default sphere marker
                    marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.transform.SetParent(transform);
                    
                    // Configure marker appearance
                    var renderer = marker.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var markerMaterial = new Material(boundaryLineMaterial);
                        markerMaterial.color = cornerMarkerColor;
                        markerMaterial.SetColor("_EmissionColor", cornerMarkerColor * 0.5f);
                        renderer.material = markerMaterial;
                    }
                    
                    // Remove collider (visual only)
                    var collider = marker.GetComponent<Collider>();
                    if (collider != null)
                    {
                        DestroyImmediate(collider);
                    }
                }
                
                marker.name = $"CornerMarker_{i}";
                marker.transform.position = currentCanvas.corners[i];
                marker.transform.localScale = Vector3.one * cornerMarkerSize;
                
                cornerMarkers[i] = marker;
            }
            
            Debug.Log("CanvasBoundaryVisualizer: Corner markers created");
        }
        
        private void UpdateBoundaryAnimation()
        {
            animationTime += Time.deltaTime * animationSpeed;
            
            // Calculate pulse effect
            float pulse = 1.0f + Mathf.Sin(animationTime) * pulseIntensity;
            
            // Animate boundary lines
            if (boundaryLines != null)
            {
                var animatedColor = originalBoundaryColor * pulse;
                
                foreach (var line in boundaryLines)
                {
                    if (line != null && line.material != null)
                    {
                        line.material.color = animatedColor;
                        line.material.SetColor("_EmissionColor", animatedColor * 0.5f);
                    }
                }
            }
            
            // Animate corner markers
            if (cornerMarkers != null && showCornerMarkers)
            {
                var animatedCornerColor = originalCornerColor * pulse;
                var animatedScale = Vector3.one * cornerMarkerSize * pulse;
                
                foreach (var marker in cornerMarkers)
                {
                    if (marker != null)
                    {
                        marker.transform.localScale = animatedScale;
                        
                        var renderer = marker.GetComponent<Renderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            renderer.material.color = animatedCornerColor;
                            renderer.material.SetColor("_EmissionColor", animatedCornerColor * 0.5f);
                        }
                    }
                }
            }
        }
        
        public void ShowBoundary()
        {
            isVisible = true;
            SetVisualizationActive(true);
            Debug.Log("CanvasBoundaryVisualizer: Boundary shown");
        }
        
        public void HideBoundary()
        {
            isVisible = false;
            SetVisualizationActive(false);
            Debug.Log("CanvasBoundaryVisualizer: Boundary hidden");
        }
        
        public void ToggleBoundaryVisibility()
        {
            if (isVisible)
            {
                HideBoundary();
            }
            else
            {
                ShowBoundary();
            }
        }
        
        private void SetVisualizationActive(bool active)
        {
            // Set boundary lines active/inactive
            if (boundaryLines != null)
            {
                foreach (var line in boundaryLines)
                {
                    if (line != null)
                    {
                        line.gameObject.SetActive(active);
                    }
                }
            }
            
            // Set corner markers active/inactive
            if (cornerMarkers != null)
            {
                foreach (var marker in cornerMarkers)
                {
                    if (marker != null)
                    {
                        marker.SetActive(active);
                    }
                }
            }
        }
        
        private void ClearBoundaryVisualization()
        {
            // Destroy existing boundary lines
            if (boundaryLines != null)
            {
                foreach (var line in boundaryLines)
                {
                    if (line != null)
                    {
                        DestroyImmediate(line.gameObject);
                    }
                }
                boundaryLines = null;
            }
            
            // Destroy existing corner markers
            if (cornerMarkers != null)
            {
                foreach (var marker in cornerMarkers)
                {
                    if (marker != null)
                    {
                        DestroyImmediate(marker);
                    }
                }
                cornerMarkers = null;
            }
        }
        
        // Public configuration methods
        public void SetBoundaryColor(Color color)
        {
            boundaryColor = color;
            originalBoundaryColor = color;
            
            if (boundaryLineMaterial != null)
            {
                boundaryLineMaterial.color = color;
                boundaryLineMaterial.SetColor("_EmissionColor", color * 0.5f);
            }
        }
        
        public void SetLineWidth(float width)
        {
            lineWidth = width;
            
            if (boundaryLines != null)
            {
                foreach (var line in boundaryLines)
                {
                    if (line != null)
                    {
                        line.startWidth = width;
                        line.endWidth = width;
                    }
                }
            }
        }
        
        public void SetCornerMarkersVisible(bool visible)
        {
            showCornerMarkers = visible;
            
            if (cornerMarkers != null)
            {
                foreach (var marker in cornerMarkers)
                {
                    if (marker != null)
                    {
                        marker.SetActive(visible && isVisible);
                    }
                }
            }
        }
        
        public void SetAnimationEnabled(bool enabled)
        {
            animateBoundary = enabled;
            
            if (!enabled)
            {
                // Reset to original colors
                SetBoundaryColor(originalBoundaryColor);
            }
        }
        
        private void OnDestroy()
        {
            ClearBoundaryVisualization();
        }
        
        // Validation method for testing
        public bool ValidateBoundaryVisualization()
        {
            bool isValid = currentCanvas != null && 
                          boundaryLines != null && 
                          boundaryLines.Length == 4;
            
            if (showCornerMarkers)
            {
                isValid = isValid && cornerMarkers != null && cornerMarkers.Length == 4;
            }
            
            return isValid;
        }
    }
}