using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Threading.Tasks;

namespace DaVinciEye.ColorAnalysis
{
    /// <summary>
    /// Simplified camera color capture component for HoloLens integration
    /// Provides easy-to-use interface for paint color analysis
    /// </summary>
    public class CameraColorCapture : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private PaintColorAnalyzer paintAnalyzer;
        [SerializeField] private ARCameraManager arCameraManager;
        
        [Header("Capture Settings")]
        [SerializeField] private LayerMask captureLayerMask = -1;
        [SerializeField] private float maxCaptureDistance = 2f;
        [SerializeField] private bool showCaptureIndicator = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject captureIndicatorPrefab;
        [SerializeField] private LineRenderer aimingRay;
        
        // Properties
        public bool IsReady => paintAnalyzer != null && paintAnalyzer.IsCameraReady;
        public Color LastCapturedColor { get; private set; } = Color.white;
        
        // Events
        public System.Action<Color, Vector3> OnColorCaptured;
        public System.Action<string> OnCaptureError;
        
        // Private fields
        private GameObject currentIndicator;
        private Camera playerCamera;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            SetupVisualFeedback();
        }
        
        private void InitializeComponents()
        {
            if (paintAnalyzer == null)
                paintAnalyzer = GetComponent<PaintColorAnalyzer>();
            
            if (arCameraManager == null)
                arCameraManager = FindObjectOfType<ARCameraManager>();
            
            playerCamera = Camera.main;
        }
        
        private void SetupVisualFeedback()
        {
            if (aimingRay == null && showCaptureIndicator)
            {
                GameObject rayGO = new GameObject("AimingRay");
                rayGO.transform.SetParent(transform);
                aimingRay = rayGO.AddComponent<LineRenderer>();
                
                aimingRay.material = CreateRayMaterial();
                aimingRay.startWidth = 0.01f;
                aimingRay.endWidth = 0.005f;
                aimingRay.positionCount = 2;
                aimingRay.enabled = false;
            }
        }
        
        private Material CreateRayMaterial()
        {
            Material rayMat = new Material(Shader.Find("Sprites/Default"));
            rayMat.color = Color.cyan;
            return rayMat;
        }
        
        /// <summary>
        /// Capture color at the center of the camera view
        /// </summary>
        public async Task<Color> CaptureColorAtCenter()
        {
            if (!IsReady)
            {
                OnCaptureError?.Invoke("Camera not ready for color capture");
                return Color.white;
            }
            
            // Get center of screen in world coordinates
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Ray ray = playerCamera.ScreenPointToRay(screenCenter);
            
            return await CaptureColorAtRay(ray);
        }
        
        /// <summary>
        /// Capture color at a specific world position
        /// </summary>
        public async Task<Color> CaptureColorAtPosition(Vector3 worldPosition)
        {
            if (!IsReady)
            {
                OnCaptureError?.Invoke("Camera not ready for color capture");
                return Color.white;
            }
            
            try
            {
                Color capturedColor = await paintAnalyzer.AnalyzePaintColorAsync(worldPosition);
                LastCapturedColor = capturedColor;
                
                OnColorCaptured?.Invoke(capturedColor, worldPosition);
                
                if (showCaptureIndicator)
                {
                    ShowCaptureIndicator(worldPosition, capturedColor);
                }
                
                return capturedColor;
            }
            catch (System.Exception e)
            {
                OnCaptureError?.Invoke($"Color capture failed: {e.Message}");
                return Color.white;
            }
        }
        
        /// <summary>
        /// Capture color using a ray cast
        /// </summary>
        public async Task<Color> CaptureColorAtRay(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, maxCaptureDistance, captureLayerMask))
            {
                return await CaptureColorAtPosition(hit.point);
            }
            else
            {
                // If no hit, use ray direction at max distance
                Vector3 targetPosition = ray.origin + ray.direction * maxCaptureDistance;
                return await CaptureColorAtPosition(targetPosition);
            }
        }
        
        /// <summary>
        /// Start continuous color capture mode (for real-time feedback)
        /// </summary>
        public void StartContinuousCapture()
        {
            if (IsReady)
            {
                InvokeRepeating(nameof(ContinuousCaptureUpdate), 0f, 0.1f); // 10 FPS
                
                if (aimingRay != null)
                    aimingRay.enabled = true;
            }
        }
        
        /// <summary>
        /// Stop continuous color capture mode
        /// </summary>
        public void StopContinuousCapture()
        {
            CancelInvoke(nameof(ContinuousCaptureUpdate));
            
            if (aimingRay != null)
                aimingRay.enabled = false;
            
            HideCaptureIndicator();
        }
        
        private async void ContinuousCaptureUpdate()
        {
            if (!IsReady) return;
            
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Ray ray = playerCamera.ScreenPointToRay(screenCenter);
            
            // Update aiming ray visual
            if (aimingRay != null && aimingRay.enabled)
            {
                aimingRay.SetPosition(0, ray.origin);
                aimingRay.SetPosition(1, ray.origin + ray.direction * maxCaptureDistance);
            }
            
            // Capture color (but don't trigger events for continuous mode)
            try
            {
                if (Physics.Raycast(ray, out RaycastHit hit, maxCaptureDistance, captureLayerMask))
                {
                    Color color = await paintAnalyzer.AnalyzePaintColorAsync(hit.point);
                    LastCapturedColor = color;
                    
                    // Update indicator color
                    if (currentIndicator != null)
                    {
                        Renderer indicatorRenderer = currentIndicator.GetComponent<Renderer>();
                        if (indicatorRenderer != null)
                        {
                            indicatorRenderer.material.color = color;
                        }
                    }
                }
            }
            catch
            {
                // Silently handle errors in continuous mode
            }
        }
        
        private void ShowCaptureIndicator(Vector3 position, Color color)
        {
            HideCaptureIndicator();
            
            if (captureIndicatorPrefab != null)
            {
                currentIndicator = Instantiate(captureIndicatorPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create simple sphere indicator
                currentIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                currentIndicator.transform.position = position;
                currentIndicator.transform.localScale = Vector3.one * 0.05f;
                
                Renderer renderer = currentIndicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
            
            // Auto-hide after 2 seconds
            Destroy(currentIndicator, 2f);
        }
        
        private void HideCaptureIndicator()
        {
            if (currentIndicator != null)
            {
                Destroy(currentIndicator);
                currentIndicator = null;
            }
        }
        
        /// <summary>
        /// Calibrate the paint color analyzer
        /// </summary>
        public async Task<bool> CalibrateColorCapture(Color[] knownColors, Vector3[] positions)
        {
            if (!IsReady)
            {
                OnCaptureError?.Invoke("Camera not ready for calibration");
                return false;
            }
            
            return await paintAnalyzer.CalibrateColorCapture(knownColors, positions);
        }
        
        /// <summary>
        /// Set capture parameters
        /// </summary>
        public void SetCaptureParameters(float maxDistance, int samplingRadius, bool showIndicator)
        {
            maxCaptureDistance = maxDistance;
            showCaptureIndicator = showIndicator;
            
            if (paintAnalyzer != null)
            {
                paintAnalyzer.SetSamplingRadius(samplingRadius);
            }
        }
        
        private void OnDestroy()
        {
            StopContinuousCapture();
            HideCaptureIndicator();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw capture range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxCaptureDistance);
        }
    }
}